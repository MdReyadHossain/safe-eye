using System.Windows.Input;
using SafeEyes.Models;
using SafeEyes.Services;

namespace SafeEyes.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly IBreakOverlayService _overlayService;

    // General
    private bool _startWithWindows;
    private bool _startSchedulerOnLaunch;
    private bool _launchMinimizedToTray;
    private bool _notificationsEnabled;
    private string _language = "en";

    // Timing
    private int _workIntervalMinutes;
    private int _shortBreakSeconds;
    private int _longBreakMinutes;
    private int _workIntervalsBetweenLongBreaks;

    // Behavior
    private bool _allowSkip;
    private bool _strictBreakMode;
    private bool _pauseOnIdle;
    private int _idleThresholdSeconds;
    private bool _pauseOnFullScreen;
    private bool _pauseOnLockOrSleep;
    private bool _allMonitors;

    // Appearance
    private double _overlayDarkness;
    private bool _showCountdown;
    private double _instructionFontSize;
    private bool _enableTransitions;

    // Exercises
    private string _shortExercisesText = string.Empty;
    private string _longExercisesText = string.Empty;

    // Status / Validation
    private string? _statusMessage;
    private bool _isSuccessStatus;
    private string? _validationError;

    // Snackbar / Toaster
    private bool _isSnackbarVisible;
    private string _snackbarTitle = "Success";
    private string _snackbarMessage = string.Empty;
    private string _snackbarType = "Success";
    private string _snackbarIcon = "✓";
    private System.Threading.Timer? _snackbarTimer;

    public event Action? CloseRequested;

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetProperty(ref _startWithWindows, value);
    }

    public bool StartSchedulerOnLaunch
    {
        get => _startSchedulerOnLaunch;
        set => SetProperty(ref _startSchedulerOnLaunch, value);
    }

    public bool LaunchMinimizedToTray
    {
        get => _launchMinimizedToTray;
        set => SetProperty(ref _launchMinimizedToTray, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetProperty(ref _notificationsEnabled, value);
    }

    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public int WorkIntervalMinutes
    {
        get => _workIntervalMinutes;
        set
        {
            if (SetProperty(ref _workIntervalMinutes, value))
            {
                Validate();
            }
        }
    }

    public int ShortBreakSeconds
    {
        get => _shortBreakSeconds;
        set
        {
            if (SetProperty(ref _shortBreakSeconds, value))
            {
                Validate();
            }
        }
    }

    public int LongBreakMinutes
    {
        get => _longBreakMinutes;
        set
        {
            if (SetProperty(ref _longBreakMinutes, value))
            {
                Validate();
            }
        }
    }

    public int WorkIntervalsBetweenLongBreaks
    {
        get => _workIntervalsBetweenLongBreaks;
        set
        {
            if (SetProperty(ref _workIntervalsBetweenLongBreaks, value))
            {
                Validate();
            }
        }
    }

    public bool AllowSkip
    {
        get => _allowSkip;
        set => SetProperty(ref _allowSkip, value);
    }

    public bool StrictBreakMode
    {
        get => _strictBreakMode;
        set => SetProperty(ref _strictBreakMode, value);
    }

    public bool PauseOnIdle
    {
        get => _pauseOnIdle;
        set => SetProperty(ref _pauseOnIdle, value);
    }

    public int IdleThresholdSeconds
    {
        get => _idleThresholdSeconds;
        set
        {
            if (SetProperty(ref _idleThresholdSeconds, value))
            {
                Validate();
            }
        }
    }

    public bool PauseOnFullScreen
    {
        get => _pauseOnFullScreen;
        set => SetProperty(ref _pauseOnFullScreen, value);
    }

    public bool PauseOnLockOrSleep
    {
        get => _pauseOnLockOrSleep;
        set => SetProperty(ref _pauseOnLockOrSleep, value);
    }

    public bool AllMonitors
    {
        get => _allMonitors;
        set => SetProperty(ref _allMonitors, value);
    }

    public double OverlayDarkness
    {
        get => _overlayDarkness;
        set
        {
            if (SetProperty(ref _overlayDarkness, value))
            {
                OnPropertyChanged(nameof(OverlayDarknessPercentText));
            }
        }
    }

    public string OverlayDarknessPercentText => $"{(int)(_overlayDarkness * 100)}%";

    public bool ShowCountdown
    {
        get => _showCountdown;
        set => SetProperty(ref _showCountdown, value);
    }

    public double InstructionFontSize
    {
        get => _instructionFontSize;
        set => SetProperty(ref _instructionFontSize, value);
    }

    public bool EnableTransitions
    {
        get => _enableTransitions;
        set => SetProperty(ref _enableTransitions, value);
    }

    public string ShortExercisesText
    {
        get => _shortExercisesText;
        set => SetProperty(ref _shortExercisesText, value);
    }

    public string LongExercisesText
    {
        get => _longExercisesText;
        set => SetProperty(ref _longExercisesText, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsSuccessStatus
    {
        get => _isSuccessStatus;
        set => SetProperty(ref _isSuccessStatus, value);
    }

    public string? ValidationError
    {
        get => _validationError;
        set
        {
            if (SetProperty(ref _validationError, value))
            {
                OnPropertyChanged(nameof(HasValidationError));
            }
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationError);

    public bool IsSnackbarVisible
    {
        get => _isSnackbarVisible;
        set => SetProperty(ref _isSnackbarVisible, value);
    }

    public string SnackbarTitle
    {
        get => _snackbarTitle;
        set => SetProperty(ref _snackbarTitle, value);
    }

    public string SnackbarMessage
    {
        get => _snackbarMessage;
        set => SetProperty(ref _snackbarMessage, value);
    }

    public string SnackbarType
    {
        get => _snackbarType;
        set => SetProperty(ref _snackbarType, value);
    }

    public string SnackbarIcon
    {
        get => _snackbarIcon;
        set => SetProperty(ref _snackbarIcon, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetDefaultsCommand { get; }
    public ICommand PreviewBreakCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand DismissSnackbarCommand { get; }

    public SettingsViewModel(
        ISettingsService settingsService,
        IStartupService startupService,
        IBreakOverlayService overlayService)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _overlayService = overlayService;

        SaveCommand = new RelayCommand(OnSave);
        ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        PreviewBreakCommand = new RelayCommand(OnPreviewBreak);
        CloseCommand = new RelayCommand(OnClose);
        DismissSnackbarCommand = new RelayCommand(DismissSnackbar);

        LoadFromSettings(_settingsService.Settings);
    }

    public void LoadFromSettings(AppSettings settings)
    {
        StartWithWindows = _startupService.IsStartupEnabled();
        StartSchedulerOnLaunch = settings.StartSchedulerOnLaunch;
        LaunchMinimizedToTray = settings.LaunchMinimizedToTray;
        NotificationsEnabled = settings.NotificationsEnabled;
        Language = settings.Language;

        WorkIntervalMinutes = settings.WorkIntervalMinutes;
        ShortBreakSeconds = settings.ShortBreakSeconds;
        LongBreakMinutes = settings.LongBreakMinutes;
        WorkIntervalsBetweenLongBreaks = settings.WorkIntervalsBetweenLongBreaks;

        AllowSkip = settings.AllowSkip;
        StrictBreakMode = settings.StrictBreakMode;
        PauseOnIdle = settings.PauseOnIdle;
        IdleThresholdSeconds = settings.IdleThresholdSeconds;
        PauseOnFullScreen = settings.PauseOnFullScreen;
        PauseOnLockOrSleep = settings.PauseOnLockOrSleep;
        AllMonitors = settings.AllMonitors;

        OverlayDarkness = settings.OverlayDarkness;
        ShowCountdown = settings.ShowCountdown;
        InstructionFontSize = settings.InstructionFontSize;
        EnableTransitions = settings.EnableTransitions;

        ShortExercisesText = string.Join(Environment.NewLine, settings.ShortBreakExercises);
        LongExercisesText = string.Join(Environment.NewLine, settings.LongBreakExercises);

        StatusMessage = null;
        ValidationError = null;
    }

    private bool Validate()
    {
        if (WorkIntervalMinutes < 1 || WorkIntervalMinutes > 240)
        {
            ValidationError = "Work interval must be between 1 and 240 minutes.";
            return false;
        }

        if (ShortBreakSeconds < 5 || ShortBreakSeconds > 600)
        {
            ValidationError = "Short break duration must be between 5 and 600 seconds.";
            return false;
        }

        if (LongBreakMinutes < 1 || LongBreakMinutes > 120)
        {
            ValidationError = "Long break duration must be between 1 and 120 minutes.";
            return false;
        }

        if (WorkIntervalsBetweenLongBreaks < 1 || WorkIntervalsBetweenLongBreaks > 50)
        {
            ValidationError = "Long break frequency must be between 1 and 50 intervals.";
            return false;
        }

        if (IdleThresholdSeconds < 30 || IdleThresholdSeconds > 3600)
        {
            ValidationError = "Idle threshold must be between 30 and 3600 seconds.";
            return false;
        }

        ValidationError = null;
        return true;
    }

    private void OnSave()
    {
        if (!Validate())
        {
            ShowSnackbar(ValidationError ?? "Please fix the invalid settings values.", "Error");
            return;
        }

        var shortList = ShortExercisesText
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var longList = LongExercisesText
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        var updated = new AppSettings
        {
            StartWithWindows = StartWithWindows,
            StartSchedulerOnLaunch = StartSchedulerOnLaunch,
            LaunchMinimizedToTray = LaunchMinimizedToTray,
            NotificationsEnabled = NotificationsEnabled,
            Language = Language,
            WorkIntervalMinutes = WorkIntervalMinutes,
            ShortBreakSeconds = ShortBreakSeconds,
            LongBreakMinutes = LongBreakMinutes,
            WorkIntervalsBetweenLongBreaks = WorkIntervalsBetweenLongBreaks,
            AllowSkip = AllowSkip,
            StrictBreakMode = StrictBreakMode,
            PauseOnIdle = PauseOnIdle,
            IdleThresholdSeconds = IdleThresholdSeconds,
            PauseOnFullScreen = PauseOnFullScreen,
            PauseOnLockOrSleep = PauseOnLockOrSleep,
            AllMonitors = AllMonitors,
            OverlayDarkness = OverlayDarkness,
            ShowCountdown = ShowCountdown,
            InstructionFontSize = InstructionFontSize,
            EnableTransitions = EnableTransitions,
            ShortBreakExercises = shortList.Count > 0 ? shortList : _settingsService.Settings.ShortBreakExercises,
            LongBreakExercises = longList.Count > 0 ? longList : _settingsService.Settings.LongBreakExercises
        };

        _settingsService.SaveSettings(updated);
        _startupService.SetStartup(StartWithWindows);

        IsSuccessStatus = true;
        StatusMessage = "Settings saved successfully.";
        ShowSnackbar("Your reminder preferences have been saved and applied.", "Success", "Settings saved");
    }

    private void OnResetDefaults()
    {
        _settingsService.ResetToDefaults();
        LoadFromSettings(_settingsService.Settings);
        IsSuccessStatus = true;
        StatusMessage = "Settings reset to defaults.";
        ShowSnackbar("All settings have been restored to default values.", "Warning", "Defaults restored");
    }

    private void OnPreviewBreak()
    {
        _overlayService.PreviewBreak("Tightly close your eyes", 10);
        ShowSnackbar("Launching break screen preview. Press Escape or Skip to dismiss.", "Info", "Preview started");
    }

    private void OnClose()
    {
        CloseRequested?.Invoke();
    }

    public void ShowSnackbar(string message, string type = "Success", string? title = null)
    {
        _snackbarTimer?.Dispose();
        SnackbarMessage = message;
        SnackbarType = type;
        SnackbarTitle = title ?? type switch
        {
            "Error" => "Error",
            "Warning" => "Warning",
            "Info" => "Notice",
            _ => "Success"
        };
        SnackbarIcon = type switch
        {
            "Error" => "✕",
            "Warning" => "⚠",
            "Info" => "ℹ",
            _ => "✓"
        };
        IsSnackbarVisible = true;

        _snackbarTimer = new System.Threading.Timer(_ =>
        {
            var app = System.Windows.Application.Current;
            if (app != null)
            {
                app.Dispatcher.Invoke(() => IsSnackbarVisible = false);
            }
            else
            {
                IsSnackbarVisible = false;
            }
        }, null, 3800, Timeout.Infinite);
    }

    public void DismissSnackbar()
    {
        _snackbarTimer?.Dispose();
        _snackbarTimer = null;
        IsSnackbarVisible = false;
    }
}
