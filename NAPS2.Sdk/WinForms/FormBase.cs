﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NAPS2.Config;
using NAPS2.Scan;
using NAPS2.Util;

namespace NAPS2.WinForms
{
    // TODO: Remove ConfigScopes.User dependency from reusable forms
    public class FormBase : Form, IInvoker
    {
        private bool loaded;
        private FormState formState;

        public FormBase()
        {
            UpdateRTL();

            RestoreFormState = true;
            SaveFormState = true;

            Load += OnLoadInternal;
            Closed += OnClosed;
            Resize += OnResize;
            Move += OnMove;
        }

        public IFormFactory FormFactory { get; set; }

        public ConfigScopes ConfigScopes { get; set; }

        public ScopeSetConfigProvider<CommonConfig> ConfigProvider { get; set; }

        protected bool RestoreFormState { get; set; }

        protected bool SaveFormState { get; set; }

        #region Helper Methods

        protected void AddEnumItems<T>(ComboBox combo)
        {
            AddEnumItems<T>(combo, Combo_Format);
        }

        protected void AddEnumItems<T>(ComboBox combo, ListControlConvertEventHandler format)
        {
            foreach (object item in Enum.GetValues(typeof(T)))
            {
                combo.Items.Add(item);
            }
            combo.Format += format;
        }

        void Combo_Format(object sender, ListControlConvertEventArgs e)
        {
            e.Value = ((Enum)e.ListItem).Description();
        }

        public void Invoke(Action action)
        {
            ((Control) this).Invoke(action);
        }

        public T InvokeGet<T>(Func<T> func)
        {
            T value = default;
            Invoke(() => value = func());
            return value;
        }

        public void SafeInvoke(Action action)
        {
            try
            {
                Invoke(action);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void SafeInvokeAsync(Action action)
        {
            try
            {
                BeginInvoke(action);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        #endregion

        protected void UpdateRTL()
        {
            bool isRTL = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
            RightToLeft = isRTL ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = isRTL;
        }

        /// <summary>
        /// Descendant forms should override this instead of subscribing to the Load event when logic needs
        /// to be performed before the form is resized (e.g. setting up LayoutManager).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        protected virtual void OnLoad(object sender, EventArgs eventArgs)
        {
        }

        #region Event Handlers

        private void OnLoadInternal(object sender, EventArgs eventArgs)
        {
            OnLoad(sender, eventArgs);
            if (DesignMode)
            {
                RestoreFormState = SaveFormState = false;
            }

            if (RestoreFormState || SaveFormState)
            {
                var formStates = ConfigProvider.Get(c => c.FormStates);
                formState = formStates.SingleOrDefault(x => x.Name == Name) ?? new FormState {Name = Name};
            }

            if (RestoreFormState)
            {
                DoRestoreFormState();
            }
            loaded = true;
        }

        protected void DoRestoreFormState()
        {
            if (!formState.Location.IsEmpty)
            {
                if (Screen.AllScreens.Any(x => x.WorkingArea.Contains(formState.Location)))
                {
                    // Only move to the specified location if it's onscreen
                    // It might be offscreen if the user has disconnected a monitor
                    Location = formState.Location;
                }
            }
            if (!formState.Size.IsEmpty)
            {
                Size = formState.Size;
            }
            if (formState.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void OnResize(object sender, EventArgs eventArgs)
        {
            if (loaded && SaveFormState)
            {
                formState.Maximized = (WindowState == FormWindowState.Maximized);
                if (WindowState == FormWindowState.Normal)
                {
                    formState.Size = Size;
                }
            }
        }

        private void OnMove(object sender, EventArgs eventArgs)
        {
            if (loaded && SaveFormState)
            {
                if (WindowState == FormWindowState.Normal)
                {
                    formState.Location = Location;
                }
            }
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            if (SaveFormState && formState != null)
            {
                var formStates = ConfigProvider.Get(c => c.FormStates);
                formStates = formStates.RemoveAll(fs => fs.Name == Name).Add(formState);
                ConfigScopes.User.Set(c => c.FormStates = formStates);
            }
        }

        #endregion
    }
}