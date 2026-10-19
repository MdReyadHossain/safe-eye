namespace SafeEyes.Services;

public interface IIdleDetectionService
{
    TimeSpan GetIdleTime();
    bool IsUserIdle(int thresholdSeconds);
}
