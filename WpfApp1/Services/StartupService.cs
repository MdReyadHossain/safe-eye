using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace SafeEyes.Services;

public class StartupService : IStartupService
{
    private const string AppName = "Safe Eye";
    private const string LegacyAppName = "SafeEyesWindows";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly ILogger<StartupService> _logger;

    public StartupService(ILogger<StartupService> logger)
    {
        _logger = logger;
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            var value = (key?.GetValue(AppName) ?? key?.GetValue(LegacyAppName)) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read startup registry entry.");
            return false;
        }
    }

    public bool SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null)
            {
                _logger.LogWarning("Unable to open HKCU Run registry key for writing.");
                return false;
            }

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    _logger.LogWarning("Unable to determine executable path for startup registration.");
                    return false;
                }

                var command = $"\"{exePath}\" --minimized";
                key.SetValue(AppName, command);
                if (key.GetValue(LegacyAppName) != null)
                {
                    key.DeleteValue(LegacyAppName, false);
                }
                _logger.LogInformation("Added {AppName} to startup registry: {Command}", AppName, command);
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                    _logger.LogInformation("Removed {AppName} from startup registry.", AppName);
                }
                if (key.GetValue(LegacyAppName) != null)
                {
                    key.DeleteValue(LegacyAppName, false);
                    _logger.LogInformation("Removed legacy {LegacyAppName} from startup registry.", LegacyAppName);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update startup registry setting (enable={Enable}).", enable);
            return false;
        }
    }
}
