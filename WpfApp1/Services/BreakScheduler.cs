using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SafeEyes.Models;

namespace SafeEyes.Services;

public class BreakScheduler : IBreakScheduler
{
    private readonly ISettingsService _settingsService;
    private readonly IIdleDetectionService _idleDetectionService;
    private readonly IFullScreenDetectionService _fullScreenDetectionService;
    private readonly ILogger<BreakScheduler> _logger;

    private readonly object _syncLock = new();
    private System.Threading.Timer? _timer;

    private DateTimeOffset _phaseEndTime;
    private TimeSpan _pausedRemainingTime = TimeSpan.Zero;
    private int _shortExerciseIndex = 0;
    private int _longExerciseIndex = 0;
    private bool _isDisposed;
    private bool _isSystemLockedOrSuspended = false;

    public SchedulerState State { get; private set; } = SchedulerState.Stopped;
    public TimeSpan RemainingTime { get; private set; } = TimeSpan.Zero;
    public int CompletedWorkIntervals { get; private set; } = 0;
    public BreakInfo? CurrentBreak { get; private set; }

    public event Action<SchedulerState>? StateChanged;
    public event Action<TimeSpan>? Tick;
    public event Action<BreakInfo>? BreakStarting;
    public event Action<BreakInfo>? BreakEnded;

    public BreakScheduler(
        ISettingsService settingsService,
        IIdleDetectionService idleDetectionService,
        IFullScreenDetectionService fullScreenDetectionService,
        ILogger<BreakScheduler> logger)
    {
        _settingsService = settingsService;
        _idleDetectionService = idleDetectionService;
        _fullScreenDetectionService = fullScreenDetectionService;
        _logger = logger;

        try
        {
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not register for Windows system events (session switch/power mode).");
        }
    }

    public void Start()
    {
        lock (_syncLock)
        {
            if (State == SchedulerState.Working || State == SchedulerState.InBreak)
            {
                return;
            }

            var workDuration = TimeSpan.FromMinutes(_settingsService.Settings.WorkIntervalMinutes);
            _phaseEndTime = DateTimeOffset.UtcNow + workDuration;
            RemainingTime = workDuration;
            State = SchedulerState.Working;
            CurrentBreak = null;

            EnsureTimerStarted();
        }

        _logger.LogInformation("BreakScheduler started with work interval {Minutes} min.", _settingsService.Settings.WorkIntervalMinutes);
        NotifyStateChanged();
    }

    public void Pause()
    {
        lock (_syncLock)
        {
            if (State == SchedulerState.Paused || State == SchedulerState.Stopped)
            {
                return;
            }

            _pausedRemainingTime = CalculateRemaining();
            if (_pausedRemainingTime < TimeSpan.Zero) _pausedRemainingTime = TimeSpan.Zero;

            State = SchedulerState.Paused;
        }

        _logger.LogInformation("BreakScheduler paused. Remaining time in phase: {Remaining}", _pausedRemainingTime);
        NotifyStateChanged();
    }

    public void Resume()
    {
        lock (_syncLock)
        {
            if (State != SchedulerState.Paused)
            {
                return;
            }

            _phaseEndTime = DateTimeOffset.UtcNow + _pausedRemainingTime;
            RemainingTime = _pausedRemainingTime;

            if (CurrentBreak != null)
            {
                State = SchedulerState.InBreak;
            }
            else
            {
                State = SchedulerState.Working;
            }

            EnsureTimerStarted();
        }

        _logger.LogInformation("BreakScheduler resumed. State: {State}", State);
        NotifyStateChanged();
    }

    public void ResetWorkTimer()
    {
        lock (_syncLock)
        {
            var workDuration = TimeSpan.FromMinutes(_settingsService.Settings.WorkIntervalMinutes);
            _phaseEndTime = DateTimeOffset.UtcNow + workDuration;
            RemainingTime = workDuration;
            CurrentBreak = null;
            State = SchedulerState.Working;

            EnsureTimerStarted();
        }

        _logger.LogInformation("Work timer reset to {Minutes} minutes.", _settingsService.Settings.WorkIntervalMinutes);
        NotifyStateChanged();
    }

    public void StartBreakNow(BreakType? breakType = null)
    {
        BreakInfo? breakToStart = null;
        lock (_syncLock)
        {
            if (State == SchedulerState.InBreak)
            {
                return; // Already in a break, prevent duplicate break
            }

            var type = breakType ?? DetermineNextBreakType();
            breakToStart = CreateBreakInfo(type);
            CurrentBreak = breakToStart;
            _phaseEndTime = DateTimeOffset.UtcNow + breakToStart.TotalDuration;
            RemainingTime = breakToStart.TotalDuration;
            State = SchedulerState.InBreak;

            EnsureTimerStarted();
        }

        _logger.LogInformation("Triggered break: {Type}, Duration: {Duration}", breakToStart.Type, breakToStart.TotalDuration);
        NotifyStateChanged();
        BreakStarting?.Invoke(breakToStart);
    }

    public void SkipCurrentBreak()
    {
        BreakInfo? endedBreak = null;
        lock (_syncLock)
        {
            if (State != SchedulerState.InBreak || CurrentBreak == null)
            {
                return;
            }

            if (_settingsService.Settings.StrictBreakMode || !_settingsService.Settings.AllowSkip)
            {
                _logger.LogWarning("Skip requested but skipping is disallowed by settings or strict mode.");
                return;
            }

            endedBreak = CurrentBreak;
            CompleteBreakTransition();
        }

        _logger.LogInformation("Break skipped: {Type}", endedBreak?.Type);
        if (endedBreak != null)
        {
            BreakEnded?.Invoke(endedBreak);
        }
        NotifyStateChanged();
    }

    public void PostponeBreak(TimeSpan postponeDuration)
    {
        lock (_syncLock)
        {
            if (State == SchedulerState.InBreak)
            {
                return;
            }

            _phaseEndTime = DateTimeOffset.UtcNow + postponeDuration;
            RemainingTime = postponeDuration;
            State = SchedulerState.Working;
        }

        _logger.LogInformation("Break postponed by {Duration}", postponeDuration);
        NotifyStateChanged();
    }

    private void EnsureTimerStarted()
    {
        if (_timer == null)
        {
            _timer = new System.Threading.Timer(OnTimerCallback, null, 1000, 1000);
        }
    }

    private void OnTimerCallback(object? state)
    {
        if (_isDisposed) return;

        BreakInfo? startingBreak = null;
        BreakInfo? endingBreak = null;

        lock (_syncLock)
        {
            if (State == SchedulerState.Stopped || State == SchedulerState.Paused)
            {
                return;
            }

            // Check system lock or sleep pause
            if (_isSystemLockedOrSuspended && _settingsService.Settings.PauseOnLockOrSleep)
            {
                return;
            }

            // Check idle detection while in Working state
            if (State == SchedulerState.Working && _settingsService.Settings.PauseOnIdle)
            {
                if (_idleDetectionService.IsUserIdle(_settingsService.Settings.IdleThresholdSeconds))
                {
                    // User is idle: postpone work phase so work time isn't accumulated while away
                    _phaseEndTime = _phaseEndTime.AddSeconds(1);
                    return;
                }
            }

            var remaining = CalculateRemaining();
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            RemainingTime = remaining;

            if (State == SchedulerState.Working)
            {
                if (remaining <= TimeSpan.Zero)
                {
                    // Check full screen detection
                    if (_settingsService.Settings.PauseOnFullScreen && _fullScreenDetectionService.IsForegroundAppFullScreen())
                    {
                        // Postpone break by 1 minute to not interrupt full screen app
                        _phaseEndTime = DateTimeOffset.UtcNow.AddMinutes(1);
                        RemainingTime = TimeSpan.FromMinutes(1);
                        _logger.LogInformation("Full-screen app detected. Postponing break by 1 minute.");
                    }
                    else
                    {
                        // Transition to Break
                        CompletedWorkIntervals++;
                        var type = DetermineNextBreakType();
                        startingBreak = CreateBreakInfo(type);
                        CurrentBreak = startingBreak;
                        _phaseEndTime = DateTimeOffset.UtcNow + startingBreak.TotalDuration;
                        RemainingTime = startingBreak.TotalDuration;
                        State = SchedulerState.InBreak;
                    }
                }
            }
            else if (State == SchedulerState.InBreak)
            {
                if (remaining <= TimeSpan.Zero)
                {
                    // Break finished normally
                    endingBreak = CurrentBreak;
                    CompleteBreakTransition();
                }
                else if (CurrentBreak != null)
                {
                    // Update remaining time in break info
                    CurrentBreak = CurrentBreak with { RemainingTime = remaining };
                }
            }
        }

        // Raise events outside the lock
        Tick?.Invoke(RemainingTime);

        if (startingBreak != null)
        {
            _logger.LogInformation("Work interval completed. Starting {Type} break.", startingBreak.Type);
            NotifyStateChanged();
            BreakStarting?.Invoke(startingBreak);
        }
        else if (endingBreak != null)
        {
            _logger.LogInformation("Break {Type} completed normally.", endingBreak.Type);
            NotifyStateChanged();
            BreakEnded?.Invoke(endingBreak);
        }
    }

    private void CompleteBreakTransition()
    {
        CurrentBreak = null;
        var workDuration = TimeSpan.FromMinutes(_settingsService.Settings.WorkIntervalMinutes);
        _phaseEndTime = DateTimeOffset.UtcNow + workDuration;
        RemainingTime = workDuration;
        State = SchedulerState.Working;
    }

    private BreakType DetermineNextBreakType()
    {
        var freq = Math.Max(1, _settingsService.Settings.WorkIntervalsBetweenLongBreaks);
        return (CompletedWorkIntervals > 0 && CompletedWorkIntervals % freq == 0)
            ? BreakType.Long
            : BreakType.Short;
    }

    private BreakInfo CreateBreakInfo(BreakType type)
    {
        var settings = _settingsService.Settings;
        string instruction;
        TimeSpan duration;

        if (type == BreakType.Long)
        {
            var exercises = settings.LongBreakExercises;
            if (exercises == null || exercises.Count == 0)
            {
                instruction = "Stand up and move around";
            }
            else
            {
                instruction = exercises[_longExerciseIndex % exercises.Count];
                _longExerciseIndex++;
            }
            duration = TimeSpan.FromMinutes(settings.LongBreakMinutes);
        }
        else
        {
            var exercises = settings.ShortBreakExercises;
            if (exercises == null || exercises.Count == 0)
            {
                instruction = "Tightly close your eyes";
            }
            else
            {
                instruction = exercises[_shortExerciseIndex % exercises.Count];
                _shortExerciseIndex++;
            }
            duration = TimeSpan.FromSeconds(settings.ShortBreakSeconds);
        }

        bool canSkip = settings.AllowSkip && !settings.StrictBreakMode;

        return new BreakInfo(
            Type: type,
            Instruction: instruction,
            TotalDuration: duration,
            RemainingTime: duration,
            CanSkip: canSkip,
            StrictMode: settings.StrictBreakMode
        );
    }

    private TimeSpan CalculateRemaining()
    {
        return _phaseEndTime - DateTimeOffset.UtcNow;
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(State);
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        _logger.LogInformation("Windows session switch event: {Reason}", e.Reason);
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.RemoteDisconnect)
        {
            _isSystemLockedOrSuspended = true;
            if (_settingsService.Settings.PauseOnLockOrSleep)
            {
                Pause();
            }
        }
        else if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.RemoteConnect)
        {
            _isSystemLockedOrSuspended = false;
            if (_settingsService.Settings.PauseOnLockOrSleep && State == SchedulerState.Paused)
            {
                Resume();
            }
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        _logger.LogInformation("Windows power mode changed: {Mode}", e.Mode);
        if (e.Mode == PowerModes.Suspend)
        {
            _isSystemLockedOrSuspended = true;
            if (_settingsService.Settings.PauseOnLockOrSleep)
            {
                Pause();
            }
        }
        else if (e.Mode == PowerModes.Resume)
        {
            _isSystemLockedOrSuspended = false;
            if (_settingsService.Settings.PauseOnLockOrSleep && State == SchedulerState.Paused)
            {
                Resume();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }
        catch { }

        _timer?.Dispose();
        _timer = null;
    }
}
