namespace SafeEyes.Services;

public interface ISystemTrayService : IDisposable
{
    void Initialize();
    void UpdateStatus(string statusText);
    void ShowNotification(string title, string message);
    event Action? OpenSettingsRequested;
    event Action? StartBreakRequested;
    event Action? TogglePauseRequested;
    event Action? ResetTimerRequested;
    event Action? ExitRequested;
}
