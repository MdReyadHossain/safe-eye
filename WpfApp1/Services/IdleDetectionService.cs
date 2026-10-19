using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SafeEyes.Infrastructure;

namespace SafeEyes.Services;

public class IdleDetectionService : IIdleDetectionService
{
    private readonly ILogger<IdleDetectionService> _logger;

    public IdleDetectionService(ILogger<IdleDetectionService> logger)
    {
        _logger = logger;
    }

    public TimeSpan GetIdleTime()
    {
        var lii = new NativeMethods.LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(lii);

        if (NativeMethods.GetLastInputInfo(ref lii))
        {
            uint currentTick = (uint)Environment.TickCount;
            uint idleTicks = currentTick - lii.dwTime;
            return TimeSpan.FromMilliseconds(idleTicks);
        }

        return TimeSpan.Zero;
    }

    public bool IsUserIdle(int thresholdSeconds)
    {
        if (thresholdSeconds <= 0) return false;
        return GetIdleTime().TotalSeconds >= thresholdSeconds;
    }
}
