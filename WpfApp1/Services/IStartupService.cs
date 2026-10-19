namespace SafeEyes.Services;

public interface IStartupService
{
    bool IsStartupEnabled();
    bool SetStartup(bool enable);
}
