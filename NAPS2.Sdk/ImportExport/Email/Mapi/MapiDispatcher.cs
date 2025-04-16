using NAPS2.Remoting.Worker;
using NAPS2.Scan;

namespace NAPS2.ImportExport.Email.Mapi;

internal class MapiDispatcher
{
    private readonly ScanningContext _scanningContext;

    public MapiDispatcher(ScanningContext scanningContext)
    {
        _scanningContext = scanningContext;
    }

    /// <summary>
    /// Sends an email described by the given message object.
    /// </summary>
    /// <param name="clientName">The MAPI client name.</param>
    /// <param name="message">The object describing the email message.</param>
    /// <returns>The MAPI return code.</returns>
    public async Task<MapiSendMailReturnCode> SendEmail(string? clientName, EmailMessage message)
    {
        // We always run MAPI in a worker as it can cause weird changes to the application state.
        if (_scanningContext.WorkerFactory == null)
        {
            // TODO: Maybe allow non-worker use for SDK?
            throw new InvalidOperationException(
                "ScanningContext must have a worker set up to use MAPI.");
        }
#if NET6_0_OR_GREATER
        if (!OperatingSystem.IsWindowsVersionAtLeast(7)) throw new InvalidOperationException("Windows-only");
#endif

        if (Environment.Is64BitProcess)
        {
            // Try 64-bit first
            using var worker1 = _scanningContext.CreateWorker(WorkerType.Native)!;
            if (await worker1.Service.CanLoadMapi(clientName))
            {
                return await worker1.Service.SendMapiEmail(clientName, message);
            }
            worker1.Dispose();
        }

        if (PlatformCompat.System.SupportsWinX86Worker)
        {
            // If 64-bit failed, try 32-bit
            using var worker2 = _scanningContext.CreateWorker(WorkerType.WinX86)!;
            if (await worker2.Service.CanLoadMapi(clientName))
            {
                return await worker2.Service.SendMapiEmail(clientName, message);
            }
        }

        throw new Exception($"Could not load MAPI dll: {clientName}");
    }
}