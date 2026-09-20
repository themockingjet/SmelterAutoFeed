using System;
using System.Diagnostics;

namespace SmeltAndFuel;

internal sealed class FeedDiagnostics
{
    private const float ReportInterval = 10f;

    private readonly bool _enabled;
    private readonly Action<string>? _log;
    private float _nextReport;
    private int _queuedTargets;
    private int _processedTargets;
    private int _containerRefreshes;
    private int _groundRefreshes;
    private int _containerCandidates;
    private int _groundCandidates;
    private int _sourceQueries;
    private int _successfulContainerRemovals;
    private int _failedContainerRemovals;
    private int _successfulGroundRemovals;
    private int _failedGroundRemovals;
    private int _retryPasses;
    private int _oreRpcs;
    private int _fuelRpcs;
    private int _windmillRpcs;
    private int _fireplaceRpcs;
    private long _containerRefreshTicks;
    private long _groundRefreshTicks;

    internal FeedDiagnostics(bool enabled, Action<string>? log)
    {
        _enabled = enabled;
        _log = log;
    }

    internal bool Enabled => _enabled;

    internal long StartTiming()
    {
        return _enabled ? Stopwatch.GetTimestamp() : 0L;
    }

    internal void RecordQueuedTarget()
    {
        if (_enabled)
        {
            _queuedTargets++;
        }
    }

    internal void RecordProcessedTarget()
    {
        if (_enabled)
        {
            _processedTargets++;
        }
    }

    internal void RecordContainerRefresh(long startTicks, int candidateCount)
    {
        if (_enabled)
        {
            _containerRefreshes++;
            _containerCandidates += candidateCount;
            _containerRefreshTicks += Stopwatch.GetTimestamp() - startTicks;
        }
    }

    internal void RecordGroundRefresh(long startTicks, int candidateCount)
    {
        if (_enabled)
        {
            _groundRefreshes++;
            _groundCandidates += candidateCount;
            _groundRefreshTicks += Stopwatch.GetTimestamp() - startTicks;
        }
    }

    internal void RecordSourceQuery()
    {
        if (_enabled)
        {
            _sourceQueries++;
        }
    }

    internal void RecordContainerRemoval(bool succeeded)
    {
        if (!_enabled)
        {
            return;
        }

        if (succeeded)
        {
            _successfulContainerRemovals++;
        }
        else
        {
            _failedContainerRemovals++;
        }
    }

    internal void RecordGroundRemoval(bool succeeded)
    {
        if (!_enabled)
        {
            return;
        }

        if (succeeded)
        {
            _successfulGroundRemovals++;
        }
        else
        {
            _failedGroundRemovals++;
        }
    }

    internal void RecordRetry()
    {
        if (_enabled)
        {
            _retryPasses++;
        }
    }

    internal void RecordOreRpc()
    {
        if (_enabled)
        {
            _oreRpcs++;
        }
    }

    internal void RecordFuelRpc()
    {
        if (_enabled)
        {
            _fuelRpcs++;
        }
    }

    internal void RecordWindmillRpc()
    {
        if (_enabled)
        {
            _windmillRpcs++;
        }
    }

    internal void RecordFireplaceRpc()
    {
        if (_enabled)
        {
            _fireplaceRpcs++;
        }
    }

    internal void Report(float now, int pendingSmelters, int pendingFireplaces)
    {
        if (!_enabled || now < _nextReport)
        {
            return;
        }

        _nextReport = now + ReportInterval;
        double containerMilliseconds = ToMilliseconds(_containerRefreshTicks);
        double groundMilliseconds = ToMilliseconds(_groundRefreshTicks);
        _log?.Invoke(
            $"Performance: queued={_queuedTargets}, processed={_processedTargets}, " +
            $"pending={pendingSmelters + pendingFireplaces}, sourceQueries={_sourceQueries}, " +
            $"containerRefreshes={_containerRefreshes}/{_containerCandidates} ({containerMilliseconds:0.00}ms), " +
            $"groundRefreshes={_groundRefreshes}/{_groundCandidates} ({groundMilliseconds:0.00}ms), " +
            $"containerRemovals={_successfulContainerRemovals}/{_failedContainerRemovals} failed, " +
            $"groundRemovals={_successfulGroundRemovals}/{_failedGroundRemovals} failed, " +
            $"retries={_retryPasses}, rpcOre={_oreRpcs}, rpcFuel={_fuelRpcs}, " +
            $"rpcWindmill={_windmillRpcs}, rpcFireplace={_fireplaceRpcs}");
        ResetCounters();
    }

    private void ResetCounters()
    {
        _queuedTargets = 0;
        _processedTargets = 0;
        _containerRefreshes = 0;
        _groundRefreshes = 0;
        _containerCandidates = 0;
        _groundCandidates = 0;
        _sourceQueries = 0;
        _successfulContainerRemovals = 0;
        _failedContainerRemovals = 0;
        _successfulGroundRemovals = 0;
        _failedGroundRemovals = 0;
        _retryPasses = 0;
        _oreRpcs = 0;
        _fuelRpcs = 0;
        _windmillRpcs = 0;
        _fireplaceRpcs = 0;
        _containerRefreshTicks = 0L;
        _groundRefreshTicks = 0L;
    }

    private static double ToMilliseconds(long ticks)
    {
        return ticks * 1000d / Stopwatch.Frequency;
    }
}
