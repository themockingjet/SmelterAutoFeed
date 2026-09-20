using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class FeedScheduler
{
    private const int MaxFeedPassesPerFrame = 4;
    private const double FeedBudgetMilliseconds = 1.5;
    private static readonly long FeedBudgetTicks = Math.Max(
        1L,
        (long)(Stopwatch.Frequency * FeedBudgetMilliseconds / 1000d));

    private readonly StationFeedService _stations;
    private readonly FireplaceFeedService _fireplaces;
    private readonly FeedTiming _timing;
    private readonly FeedDiagnostics _diagnostics;
    private readonly PlayerPresenceService _presence;
    private readonly Action<Exception>? _logException;
    private readonly Queue<Smelter> _pendingSmelters = new();
    private readonly Queue<Fireplace> _pendingFireplaces = new();
    private readonly HashSet<Smelter> _queuedSmelters = new();
    private readonly HashSet<Fireplace> _queuedFireplaces = new();
    private bool _processSmelterNext;

    internal FeedScheduler(
        AutoFeedSettings settings,
        PlayerPresenceService presence,
        Action<Exception>? logException,
        Action<string>? logInfo)
    {
        _presence = presence;
        _diagnostics = new FeedDiagnostics(settings.PerformanceLogging.Value, logInfo);
        _timing = new FeedTiming(settings, _diagnostics);
        FeedSourceService sources = new(settings, _diagnostics, _presence);
        _stations = new StationFeedService(settings, sources, _timing, _presence);
        _fireplaces = new FireplaceFeedService(settings, sources, _timing, _presence);
        _logException = logException;
    }

    internal bool HasPendingWork =>
        _pendingSmelters.Count > 0 || _pendingFireplaces.Count > 0;

    internal void QueueFeed(Smelter smelter)
    {
        ZNetView? smelterView = smelter.GetComponent<ZNetView>();
        if (smelterView is null ||
            !smelterView.IsValid() ||
            !smelterView.IsOwner() ||
            !_presence.IsAutomationActive(smelter.transform.position, Time.time) ||
            !_timing.IsDue(smelterView, Time.time) ||
            !_stations.ShouldFeed(smelter))
        {
            return;
        }

        if (_queuedSmelters.Add(smelter))
        {
            _pendingSmelters.Enqueue(smelter);
            _diagnostics.RecordQueuedTarget();
        }
    }

    internal void QueueRefuel(Fireplace fireplace)
    {
        if (!_fireplaces.ShouldRefuel(fireplace))
        {
            return;
        }

        ZNetView? fireplaceView = fireplace.GetComponent<ZNetView>();
        if (fireplaceView is null ||
            !fireplaceView.IsValid() ||
            !fireplaceView.IsOwner() ||
            !_presence.IsAutomationActive(fireplace.transform.position, Time.time) ||
            !_timing.IsDue(fireplaceView, Time.time))
        {
            return;
        }

        if (_queuedFireplaces.Add(fireplace))
        {
            _pendingFireplaces.Enqueue(fireplace);
            _diagnostics.RecordQueuedTarget();
        }
    }

    internal void ProcessPending()
    {
        if (!HasPendingWork)
        {
            _diagnostics.Report(Time.time, 0, 0);
            return;
        }

        long startTicks = Stopwatch.GetTimestamp();
        try
        {
            for (int pass = 0; pass < MaxFeedPassesPerFrame; pass++)
            {
                if (pass > 0 && Stopwatch.GetTimestamp() - startTicks >= FeedBudgetTicks)
                {
                    return;
                }

                if (!TryProcessOnePending())
                {
                    return;
                }
            }
        }
        finally
        {
            _diagnostics.Report(Time.time, _pendingSmelters.Count, _pendingFireplaces.Count);
        }
    }

    private bool TryProcessOnePending()
    {
        if (_processSmelterNext)
        {
            if (TryProcessOne(_pendingSmelters, _queuedSmelters, smelter => _stations.Process(smelter)))
            {
                _processSmelterNext = false;
                return true;
            }

            if (TryProcessOne(_pendingFireplaces, _queuedFireplaces, fireplace => _fireplaces.Process(fireplace)))
            {
                _processSmelterNext = true;
                return true;
            }
        }
        else
        {
            if (TryProcessOne(_pendingFireplaces, _queuedFireplaces, fireplace => _fireplaces.Process(fireplace)))
            {
                _processSmelterNext = true;
                return true;
            }

            if (TryProcessOne(_pendingSmelters, _queuedSmelters, smelter => _stations.Process(smelter)))
            {
                _processSmelterNext = false;
                return true;
            }
        }

        return false;
    }

    private bool TryProcessOne<T>(
        Queue<T> pending,
        HashSet<T> queued,
        Action<T> process)
        where T : UnityEngine.Object
    {
        if (pending.Count == 0)
        {
            return false;
        }

        T target = pending.Dequeue();
        queued.Remove(target);
        if (target != null)
        {
            _diagnostics.RecordProcessedTarget();
            try
            {
                process(target);
            }
            catch (Exception exception)
            {
                _logException?.Invoke(exception);
            }
        }

        return true;
    }
}
