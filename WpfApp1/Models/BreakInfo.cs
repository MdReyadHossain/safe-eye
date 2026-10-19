namespace SafeEyes.Models;

public record BreakInfo(
    BreakType Type,
    string Instruction,
    TimeSpan TotalDuration,
    TimeSpan RemainingTime,
    bool CanSkip,
    bool StrictMode
);
