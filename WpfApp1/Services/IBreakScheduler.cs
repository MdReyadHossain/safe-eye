using SafeEyes.Models;

namespace SafeEyes.Services;

public interface IBreakScheduler : IDisposable
{
    SchedulerState State { get; }
    TimeSpan RemainingTime { get; }
    int CompletedWorkIntervals { get; }
    BreakInfo? CurrentBreak { get; }

    event Action<SchedulerState>? StateChanged;
    event Action<TimeSpan>? Tick;
    event Action<BreakInfo>? BreakStarting;
    event Action<BreakInfo>? BreakEnded;

    void Start();
    void Pause();
    void Resume();
    void ResetWorkTimer();
    void StartBreakNow(BreakType? breakType = null);
    void SkipCurrentBreak();
    void PostponeBreak(TimeSpan postponeDuration);
}
