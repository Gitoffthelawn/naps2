using Eto.Drawing;
using Eto.Forms;
using NAPS2.EtoForms.Layout;

namespace NAPS2.EtoForms;

public class FormStateController
{
    private readonly Window _window;
    private readonly Naps2Config _config;
    private FormState? _formState;
    private bool _hasSetSize;
    private Size _minimumClientSize;
    private Size _maximumClientSize;

    public FormStateController(Window window, Naps2Config config)
    {
        _window = window;
        _config = config;
        window.SizeChanged += OnResize;
        window.LocationChanged += OnMove;
        window.PreLoad += OnLoadInternal;
        window.Shown += OnShownInternal;
        window.Closed += OnClosed;
    }

    public bool SaveFormState { get; set; } = true;

    public bool RestoreFormState { get; set; } = true;

    public bool AutoLayoutSize { get; set; } = true;

    public SizeF DefaultExtraLayoutSize { get; set; }

    public SizeF DefaultClientSize { get; set; }

    public bool FixedHeightLayout { get; set; }

    public bool Resizable { get; set; } = true;

    public bool Loaded { get; private set; }

    public bool Shown { get; private set; }

    public string FormName => _window.GetType().Name;

    public void UpdateLayoutSize(LayoutController layoutController)
    {
        if (AutoLayoutSize)
        {
            var scale = EtoPlatform.Current.GetLayoutScaleFactor(_window);
            _minimumClientSize = layoutController.GetLayoutSize();
            var oldDefaultClientSize = DefaultClientSize;
            var oldMaximumClientSize = _maximumClientSize;
            DefaultClientSize = (SizeF) _minimumClientSize / scale + DefaultExtraLayoutSize;
            _maximumClientSize = FixedHeightLayout || !Resizable ? new Size(0, _minimumClientSize.Height) : Size.Empty;

            if (Loaded)
            {
                // Dynamic re-sizing because the layout contents have changed (not just a normal resize/maximize etc).
                var size = EtoPlatform.Current.GetClientSize(_window);
                if (oldMaximumClientSize.Height > 0)
                {
                    size.Height = Math.Min(size.Height, oldMaximumClientSize.Height);
                }
                // TODO: Maybe we can add a flag to do this behavior? It makes it so that changes to the layout size
                // after the form is loaded cause the form size to change proportionally (even if we're still within
                // our min/max bounds). This is causing problems for dynamically sized images/labels etc. but maybe
                // there's a world where we want to re-enable this for some forms.
                // size += DefaultClientSize - oldDefaultClientSize;
                size = Size.Max(size, _minimumClientSize);
                if (_maximumClientSize.Height > 0)
                {
                    size.Height = Math.Min(size.Height, _maximumClientSize.Height);
                }
                EtoPlatform.Current.SetMinimumClientSize(_window, _minimumClientSize);
                EtoPlatform.Current.SetClientSize(_window, size);
            }
        }
    }

    private void OnLoadInternal(object? sender, EventArgs eventArgs)
    {
        if (RestoreFormState || SaveFormState)
        {
            var formStates = _config.Get(c => c.FormStates);
            _formState = formStates.SingleOrDefault(x => x.Name == FormName) ?? new FormState { Name = FormName };
        }

        if (RestoreFormState)
        {
            DoRestoreFormState();
        }
        if (!_hasSetSize && !DefaultClientSize.IsEmpty)
        {
            // TODO: Use size delta to re-center
            var scale = EtoPlatform.Current.GetLayoutScaleFactor(_window);
            EtoPlatform.Current.SetClientSize(_window, Size.Round(DefaultClientSize * scale));
        }
        Loaded = true;
    }

    private void OnShownInternal(object? sender, EventArgs e)
    {
        if (!_minimumClientSize.IsEmpty)
        {
            EtoPlatform.Current.SetMinimumClientSize(_window, _minimumClientSize);
        }
        _window.Resizable = Resizable;
        Shown = true;
    }

    protected void DoRestoreFormState()
    {
        if (_formState == null)
        {
            throw new InvalidOperationException();
        }
        var location = new Point(_formState.Location.X, _formState.Location.Y);
        if (!location.IsZero &&
            // Restoring location causes DPI mismatches if there are multiple monitors with different DPI (WinForms bug)
            !FixWinFormsDpiIssues &&
            // Only move to the specified location if it's onscreen
            // It might be offscreen if the user has disconnected a monitor
            Screen.Screens.Any(x => x.WorkingArea.Contains(location)))
        {
            EtoPlatform.Current.SetFormLocation(_window, location);
        }
        var scale = FixWinFormsDpiIssues ? 1f : EtoPlatform.Current.GetLayoutScaleFactor(_window);
        var size = new Size(
            (int) Math.Round(_formState.Size.Width * scale),
            (int) Math.Round(_formState.Size.Height * scale));
        if (!size.IsEmpty && Resizable)
        {
            if (!_minimumClientSize.IsEmpty)
            {
                size = Size.Max(size, _minimumClientSize);
            }
            if (_maximumClientSize.Height > 0)
            {
                size.Height = Math.Min(size.Height, _maximumClientSize.Height);
            }
            EtoPlatform.Current.SetClientSize(_window, size);
            _hasSetSize = true;
        }
        if (_formState.Maximized && _window.Resizable)
        {
            _window.WindowState = WindowState.Maximized;
            // TODO: Setting WindowState immediately doesn't work on WinForms when we've already created a window handle
            // Theoretically we could move this code around earlier before the handle is created (e.g. when we're
            // doing layouting to determine the width of the sidebar), but it's hard to get all the interdependencies
            // between layouting and form state working correctly. This workaround of setting it again later works but
            // results in an animation for the maximization.
            Invoker.Current.InvokeDispatch(() => _window.WindowState = WindowState.Maximized);
        }
    }

    private bool FixWinFormsDpiIssues => EtoPlatform.Current.IsWinForms && DifferentMonitorScales;

    private bool DifferentMonitorScales => Screen.Screens.Select(x => x.RealDPI).Distinct().Count() > 1;

    private void OnResize(object? sender, EventArgs eventArgs)
    {
        if (Shown && _formState != null && SaveFormState)
        {
            _formState.Maximized = (_window.WindowState == WindowState.Maximized);
            if (_window.WindowState == WindowState.Normal)
            {
                var size = EtoPlatform.Current.GetClientSize(_window);
                var scale = FixWinFormsDpiIssues ? 1f : EtoPlatform.Current.GetLayoutScaleFactor(_window);
                _formState.Size = new FormState.FormSize(
                    (int) Math.Round(size.Width / scale),
                    (int) Math.Round(size.Height / scale));
            }
        }
    }

    private void OnMove(object? sender, EventArgs eventArgs)
    {
        if (Shown && _formState != null && SaveFormState)
        {
            if (_window.WindowState == WindowState.Normal)
            {
                _formState.Location = new FormState.FormLocation(_window.Location.X, _window.Location.Y);
            }
        }
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        DoSaveFormState();
    }

    public void DoSaveFormState()
    {
        if (SaveFormState && _formState != null)
        {
            var formStates = _config.Get(c => c.FormStates);
            formStates = formStates.RemoveAll(fs => fs.Name == FormName).Add(_formState);
            _config.User.Set(c => c.FormStates, formStates);
        }
    }
}