using System;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class FireplaceFeedService
{
    private readonly AutoFeedSettings _settings;
    private readonly FeedSourceService _sources;
    private readonly FeedTiming _timing;
    private readonly FeedDiagnostics _diagnostics;
    private readonly PlayerPresenceService _presence;

    internal FireplaceFeedService(
        AutoFeedSettings settings,
        FeedSourceService sources,
        FeedTiming timing,
        PlayerPresenceService presence)
    {
        _settings = settings;
        _sources = sources;
        _timing = timing;
        _diagnostics = sources.Diagnostics;
        _presence = presence;
    }

    internal bool ShouldRefuel(Fireplace fireplace)
    {
        return !_settings.UnlimitedFuel.Value &&
               !fireplace.m_infiniteFuel &&
               fireplace.m_canRefill &&
               fireplace.m_fuelItem is not null &&
               StationClassifier.IsRefillEnabled(fireplace, _settings.Fireplaces);
    }

    internal void Process(Fireplace fireplace)
    {
        if (_settings.UnlimitedFuel.Value ||
            !StationClassifier.IsRefillEnabled(fireplace, _settings.Fireplaces) ||
            fireplace.m_infiniteFuel ||
            !fireplace.m_canRefill ||
            fireplace.m_fuelItem is null)
        {
            return;
        }

        ZNetView? fireplaceView = fireplace.GetComponent<ZNetView>();
        if (fireplaceView is null || !fireplaceView.IsValid() || !fireplaceView.IsOwner())
        {
            return;
        }

        if (!_presence.IsAutomationActive(fireplace.transform.position, Time.time))
        {
            return;
        }

        int fuelTarget = Math.Min(
            _settings.Fireplaces.FuelTarget.Value,
            Mathf.FloorToInt(fireplace.m_maxFuel));
        float now = Time.time;
        if (fireplaceView.GetZDO().GetFloat("fuel", 0f) >= fuelTarget ||
            !_timing.TryBegin(fireplaceView, now))
        {
            return;
        }

        SourceSnapshot sources = _sources.GetSnapshot(
            fireplace.transform.position,
            _settings.Fireplaces.Range.Value,
            now);
        bool refueled = _sources.TryTakePriorityFuel(sources);
        _timing.RecordResult(fireplaceView, now, refueled);
        if (refueled)
        {
            fireplaceView.InvokeRPC("RPC_AddFuel");
            _diagnostics.RecordFireplaceRpc();
        }
    }
}
