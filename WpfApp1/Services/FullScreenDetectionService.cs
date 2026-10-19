using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using SafeEyes.Infrastructure;

namespace SafeEyes.Services;

public class FullScreenDetectionService : IFullScreenDetectionService
{
    private readonly ILogger<FullScreenDetectionService> _logger;

    public FullScreenDetectionService(ILogger<FullScreenDetectionService> logger)
    {
        _logger = logger;
    }

    public bool IsForegroundAppFullScreen()
    {
        try
        {
            var foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;

            var desktop = NativeMethods.GetDesktopWindow();
            var shell = NativeMethods.GetShellWindow();
            if (foreground == desktop || foreground == shell) return false;

            // Check class name to ignore standard desktop elements
            var className = new StringBuilder(256);
            NativeMethods.GetClassName(foreground, className, className.Capacity);
            var cls = className.ToString();
            if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd")
            {
                return false;
            }

            if (!NativeMethods.GetWindowRect(foreground, out var rect))
            {
                return false;
            }

            // Find which screen contains the foreground window
            var screen = Screen.FromHandle(foreground);
            if (screen == null) return false;

            // Check if window bounds cover the screen bounds
            bool isFullScreen = rect.Left <= screen.Bounds.Left &&
                               rect.Top <= screen.Bounds.Top &&
                               rect.Right >= screen.Bounds.Right &&
                               rect.Bottom >= screen.Bounds.Bottom;

            return isFullScreen;
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Error while checking full-screen state.");
            return false;
        }
    }
}
