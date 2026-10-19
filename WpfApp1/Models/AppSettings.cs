using System.Text.Json.Serialization;

namespace SafeEyes.Models;

public class AppSettings
{
    // General
    public bool StartWithWindows { get; set; } = false;
    public bool StartSchedulerOnLaunch { get; set; } = true;
    public bool LaunchMinimizedToTray { get; set; } = false;
    public bool NotificationsEnabled { get; set; } = true;
    public string Language { get; set; } = "en";

    // Timing
    public int WorkIntervalMinutes { get; set; } = 20;
    public int ShortBreakSeconds { get; set; } = 20;
    public int LongBreakMinutes { get; set; } = 5;
    public int WorkIntervalsBetweenLongBreaks { get; set; } = 4;

    // Behavior
    public bool AllowSkip { get; set; } = true;
    public bool StrictBreakMode { get; set; } = false;
    public bool PauseOnIdle { get; set; } = true;
    public int IdleThresholdSeconds { get; set; } = 180;
    public bool PauseOnFullScreen { get; set; } = true;
    public bool PauseOnLockOrSleep { get; set; } = true;
    public bool AllMonitors { get; set; } = true;

    // Appearance
    public double OverlayDarkness { get; set; } = 0.90;
    public bool ShowCountdown { get; set; } = true;
    public double InstructionFontSize { get; set; } = 32.0;
    public bool EnableTransitions { get; set; } = true;

    // Instructions
    public List<string> ShortBreakExercises { get; set; } = new()
    {
        "Tightly close your eyes",
        "Lean back at your seat and relax",
        "Look away from the screen",
        "Blink your eyes gently",
        "Stretch your shoulders and neck"
    };

    public List<string> LongBreakExercises { get; set; } = new()
    {
        "Stand up and move around",
        "Take a short walk and drink some water",
        "Stretch your arms, back, and legs"
    };

    public void ValidateAndSanitize()
    {
        if (WorkIntervalMinutes < 1) WorkIntervalMinutes = 1;
        if (WorkIntervalMinutes > 240) WorkIntervalMinutes = 240;

        if (ShortBreakSeconds < 5) ShortBreakSeconds = 5;
        if (ShortBreakSeconds > 600) ShortBreakSeconds = 600;

        if (LongBreakMinutes < 1) LongBreakMinutes = 1;
        if (LongBreakMinutes > 120) LongBreakMinutes = 120;

        if (WorkIntervalsBetweenLongBreaks < 1) WorkIntervalsBetweenLongBreaks = 1;
        if (WorkIntervalsBetweenLongBreaks > 50) WorkIntervalsBetweenLongBreaks = 50;

        if (IdleThresholdSeconds < 30) IdleThresholdSeconds = 30;
        if (IdleThresholdSeconds > 3600) IdleThresholdSeconds = 3600;

        if (OverlayDarkness < 0.40) OverlayDarkness = 0.40;
        if (OverlayDarkness > 0.98) OverlayDarkness = 0.98;

        if (InstructionFontSize < 18.0) InstructionFontSize = 18.0;
        if (InstructionFontSize > 60.0) InstructionFontSize = 60.0;

        if (ShortBreakExercises == null || ShortBreakExercises.Count == 0)
        {
            ShortBreakExercises = new List<string>
            {
                "Tightly close your eyes",
                "Lean back at your seat and relax",
                "Look away from the screen",
                "Blink your eyes gently",
                "Stretch your shoulders and neck"
            };
        }

        if (LongBreakExercises == null || LongBreakExercises.Count == 0)
        {
            LongBreakExercises = new List<string>
            {
                "Stand up and move around",
                "Take a short walk and drink some water",
                "Stretch your arms, back, and legs"
            };
        }
    }

    public AppSettings Clone()
    {
        return new AppSettings
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
            ShortBreakExercises = new List<string>(ShortBreakExercises),
            LongBreakExercises = new List<string>(LongBreakExercises)
        };
    }
}
