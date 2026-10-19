using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SafeEyes.Models;

namespace SafeEyes.Services;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsFilePath { get; }
    public AppSettings Settings { get; private set; }

    public event Action<AppSettings>? SettingsChanged;

    public SettingsService(ILogger<SettingsService> logger, string? customFilePath = null)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            SettingsFilePath = customFilePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "Safe Eye");
            var legacyFolder = Path.Combine(appData, "SafeEyesWindows");

            if (!Directory.Exists(folder) && Directory.Exists(legacyFolder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                    var legacyFile = Path.Combine(legacyFolder, "settings.json");
                    var targetFile = Path.Combine(folder, "settings.json");
                    if (File.Exists(legacyFile) && !File.Exists(targetFile))
                    {
                        File.Copy(legacyFile, targetFile, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to migrate legacy settings from {LegacyFolder}", legacyFolder);
                }
            }

            SettingsFilePath = Path.Combine(folder, "settings.json");
        }

        Settings = LoadSettings();
    }

    public AppSettings LoadSettings()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        loaded.ValidateAndSanitize();
                        _logger.LogInformation("Loaded settings from {Path}", SettingsFilePath);
                        Settings = loaded;
                        return Settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load settings file at {Path}. Falling back to default settings.", SettingsFilePath);
            }

            var defaultSettings = new AppSettings();
            defaultSettings.ValidateAndSanitize();
            Settings = defaultSettings;
            // Attempt to write defaults if file doesn't exist
            try
            {
                SaveSettingsInternal(defaultSettings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write initial default settings to {Path}", SettingsFilePath);
            }

            return Settings;
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_lock)
        {
            settings.ValidateAndSanitize();
            SaveSettingsInternal(settings);
            Settings = settings.Clone();
        }

        _logger.LogInformation("Saved settings to {Path}", SettingsFilePath);
        SettingsChanged?.Invoke(Settings);
    }

    private void SaveSettingsInternal(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        var tempFilePath = SettingsFilePath + ".tmp";
        File.WriteAllText(tempFilePath, json);

        if (File.Exists(SettingsFilePath))
        {
            File.Replace(tempFilePath, SettingsFilePath, null);
        }
        else
        {
            File.Move(tempFilePath, SettingsFilePath);
        }
    }

    public void ResetToDefaults()
    {
        var defaults = new AppSettings();
        defaults.ValidateAndSanitize();
        SaveSettings(defaults);
    }
}
