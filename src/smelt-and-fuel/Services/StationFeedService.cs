using System;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class StationFeedService
{
    private static readonly Func<Smelter, int> GetQueueSize =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, int>>(typeof(Smelter), "GetQueueSize");
    private static readonly Func<Smelter, int> GetProcessedQueueSize =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, int>>(typeof(Smelter), "GetProcessedQueueSize");
    private static readonly Func<Smelter, float> GetFuel =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, float>>(typeof(Smelter), "GetFuel");

    private readonly AutoFeedSettings _settings;
    private readonly FeedSourceService _sources;
    private readonly FeedTiming _timing;
    private readonly FeedDiagnostics _diagnostics;
    private readonly PlayerPresenceService _presence;

    internal StationFeedService(
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

    internal bool ShouldFeed(Smelter smelter)
    {
        if (!StationClassifier.TryGetStationSettings(smelter, _settings, out StationSettings stationSettings) ||
            !stationSettings.Enabled.Value)
        {
            return false;
        }

        bool canFeedFuel = _settings.FeedFuel.Value &&
            smelter.m_fuelItem is not null &&
            (!_settings.UnlimitedFuel.Value || !StationClassifier.SupportsUnlimitedFuel(smelter));
        return _settings.FeedOre.Value ||
               canFeedFuel ||
               (_settings.AutoEmptyWindmillOutput.Value && StationClassifier.IsWindmill(smelter));
    }

    internal void Process(Smelter smelter)
    {
        if (!StationClassifier.TryGetStationSettings(smelter, _settings, out StationSettings stationSettings) ||
            !stationSettings.Enabled.Value)
        {
            return;
        }

        ZNetView? smelterView = smelter.GetComponent<ZNetView>();
        float now = Time.time;
        Vector3 position = smelter.transform.position;
        if (smelterView is null ||
            !smelterView.IsValid() ||
            !smelterView.IsOwner() ||
            !_presence.IsAutomationActive(position, now) ||
            !_timing.TryBegin(smelterView, now))
        {
            return;
        }

        if (_settings.AutoEmptyWindmillOutput.Value &&
            StationClassifier.IsWindmill(smelter) &&
            ShouldEmptyWindmill(smelter))
        {
            smelterView.InvokeRPC("RPC_EmptyProcessed");
            _diagnostics.RecordWindmillRpc();
        }

        int oreTarget = Math.Min(stationSettings.OreTarget.Value, smelter.m_maxOre);
        int fuelTarget = Math.Min(stationSettings.FuelTarget.Value, smelter.m_maxFuel);
        bool needsOre = _settings.FeedOre.Value && GetQueueSize(smelter) < oreTarget;
        bool needsFuel = (!_settings.UnlimitedFuel.Value || !StationClassifier.SupportsUnlimitedFuel(smelter)) &&
            _settings.FeedFuel.Value &&
            smelter.m_fuelItem is not null &&
            GetFuel(smelter) + 1f <= fuelTarget;
        if (!needsOre && !needsFuel)
        {
            return;
        }

        SourceSnapshot sources = _sources.GetSnapshot(
            position,
            _settings.ContainerSearchRadius.Value,
            now);
        bool fed = false;
        if (needsOre)
        {
            fed |= TryFeedOre(smelter, smelterView, sources);
        }

        if (needsFuel)
        {
            fed |= TryFeedFuel(smelterView, smelter.m_fuelItem!.gameObject.name, sources);
        }

        _timing.RecordResult(smelterView, now, fed);
    }

    private bool ShouldEmptyWindmill(Smelter windmill)
    {
        int completedFlour = GetProcessedQueueSize(windmill);
        return completedFlour > 0 &&
               (completedFlour >= _settings.WindmillOutputBatchSize.Value || GetQueueSize(windmill) == 0);
    }

    private bool TryFeedOre(Smelter smelter, ZNetView smelterView, SourceSnapshot sources)
    {
        ItemDrop.ItemData? ore = _sources.TryTakeOre(smelter, sources);
        if (ore?.m_dropPrefab is null)
        {
            return false;
        }

        smelterView.InvokeRPC("RPC_AddOre", ore.m_dropPrefab.name, false);
        _diagnostics.RecordOreRpc();
        return true;
    }

    private bool TryFeedFuel(
        ZNetView smelterView,
        string nativeFuelPrefabName,
        SourceSnapshot sources)
    {
        if (!_sources.TryTakeFuelByName(nativeFuelPrefabName, sources))
        {
            return false;
        }

        smelterView.InvokeRPC("RPC_AddFuel");
        _diagnostics.RecordFuelRpc();
        return true;
    }
}
