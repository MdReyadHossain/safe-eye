using SafeEyes.Models;

namespace SafeEyes.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    event Action<AppSettings>? SettingsChanged;
    AppSettings LoadSettings();
    void SaveSettings(AppSettings settings);
    void ResetToDefaults();
    string SettingsFilePath { get; }
}
