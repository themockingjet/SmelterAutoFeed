using System;

namespace SmeltAndFuel;

internal static class AutoFeedService
{
    private static FeedScheduler? _scheduler;

    internal static void Configure(
        AutoFeedSettings settings,
        PlayerPresenceService presence,
        Action<Exception> logException,
        Action<string>? logInfo)
    {
        _scheduler = new FeedScheduler(settings, presence, logException, logInfo);
    }

    internal static void QueueFeed(Smelter smelter)
    {
        _scheduler?.QueueFeed(smelter);
    }

    internal static void QueueRefuel(Fireplace fireplace)
    {
        _scheduler?.QueueRefuel(fireplace);
    }

    internal static void ProcessPending()
    {
        _scheduler?.ProcessPending();
    }
}
