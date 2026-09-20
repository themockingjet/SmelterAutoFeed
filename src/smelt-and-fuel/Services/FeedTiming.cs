using System;
using System.Collections.Generic;

namespace SmeltAndFuel;

internal sealed class FeedTiming
{
    private const float MaxNoSourceRetryInterval = 10f;
    private const float TimingEntryTtl = 300f;
    private const float TimingPurgeInterval = 30f;

    private readonly AutoFeedSettings _settings;
    private readonly FeedDiagnostics _diagnostics;
    private readonly Dictionary<ZDOID, Entry> _entries = new();
    private readonly List<ZDOID> _purgeBuffer = new();
    private float _nextPurge;

    internal FeedTiming(AutoFeedSettings settings, FeedDiagnostics diagnostics)
    {
        _settings = settings;
        _diagnostics = diagnostics;
    }

    internal bool IsDue(ZNetView targetView, float now)
    {
        return !_entries.TryGetValue(targetView.GetZDO().m_uid, out Entry entry) ||
               now >= entry.NextFeedTime;
    }

    internal bool TryBegin(ZNetView targetView, float now)
    {
        PurgeStaleEntries(now);

        ZDOID targetId = targetView.GetZDO().m_uid;
        if (_entries.TryGetValue(targetId, out Entry entry))
        {
            entry.LastSeenTime = now;
            _entries[targetId] = entry;
            if (now < entry.NextFeedTime)
            {
                return false;
            }
        }

        _entries[targetId] = new Entry
        {
            NextFeedTime = now + _settings.FeedInterval.Value,
            LastSeenTime = now
        };
        return true;
    }

    internal void RecordResult(ZNetView targetView, float now, bool fed)
    {
        ZDOID targetId = targetView.GetZDO().m_uid;
        Entry entry = _entries.TryGetValue(targetId, out Entry existing)
            ? existing
            : new Entry();

        entry.LastSeenTime = now;
        if (fed)
        {
            entry.RetryInterval = 0f;
            entry.NextFeedTime = now + _settings.FeedInterval.Value;
            _entries[targetId] = entry;
            return;
        }

        float previousInterval = entry.RetryInterval > 0f
            ? entry.RetryInterval
            : _settings.FeedInterval.Value;
        entry.RetryInterval = Math.Min(
            MaxNoSourceRetryInterval,
            Math.Max(_settings.FeedInterval.Value, previousInterval * 2f));
        entry.NextFeedTime = now + entry.RetryInterval;
        _entries[targetId] = entry;
        _diagnostics.RecordRetry();
    }

    private void PurgeStaleEntries(float now)
    {
        if (now < _nextPurge)
        {
            return;
        }

        _nextPurge = now + TimingPurgeInterval;
        _purgeBuffer.Clear();
        foreach (KeyValuePair<ZDOID, Entry> pair in _entries)
        {
            if (now - pair.Value.LastSeenTime >= TimingEntryTtl)
            {
                _purgeBuffer.Add(pair.Key);
            }
        }

        foreach (ZDOID targetId in _purgeBuffer)
        {
            _entries.Remove(targetId);
        }
    }

    private struct Entry
    {
        internal float NextFeedTime;
        internal float RetryInterval;
        internal float LastSeenTime;
    }
}
