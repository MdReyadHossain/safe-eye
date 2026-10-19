using SafeEyes.Models;

namespace SafeEyes.Services;

public interface IBreakOverlayService : IDisposable
{
    bool IsOverlayVisible { get; }
    void ShowBreak(BreakInfo breakInfo);
    void UpdateCountdown(TimeSpan remainingTime);
    void CloseBreak();
    void PreviewBreak(string instruction, int durationSeconds);
}
