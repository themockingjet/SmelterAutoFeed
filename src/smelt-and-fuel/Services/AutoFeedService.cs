using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class AutoFeedService
{
    private const int MaxFeedPassesPerFrame = 4;
    private const double FeedBudgetMilliseconds = 1.5;
    private const float MaxNoSourceRetryInterval = 10f;
    private const float TimingEntryTtl = 300f;
    private const float TimingPurgeInterval = 30f;
    private static readonly long FeedBudgetTicks = Math.Max(
        1L,
        (long)(Stopwatch.Frequency * FeedBudgetMilliseconds / 1000d));

    private static readonly Func<Smelter, int> GetQueueSize =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, int>>(typeof(Smelter), "GetQueueSize");
    private static readonly Func<Smelter, int> GetProcessedQueueSize =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, int>>(typeof(Smelter), "GetProcessedQueueSize");
    private static readonly Func<Smelter, float> GetFuel =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, float>>(typeof(Smelter), "GetFuel");
    private static readonly Func<Smelter, Inventory, ItemDrop.ItemData> FindCookableItem =
        NativeMethodDelegate.Create<Func<Smelter, Inventory, ItemDrop.ItemData>>(
            typeof(Smelter),
            "FindCookableItem",
            new[] { typeof(Inventory) });
    private static readonly Func<Smelter, ItemDrop.ItemData, bool> IsItemAllowed =
        NativeMethodDelegate.Create<Func<Smelter, ItemDrop.ItemData, bool>>(
            typeof(Smelter),
            "IsItemAllowed",
            new[] { typeof(ItemDrop.ItemData) });

    private static AutoFeedService? _instance;
    private static Action<Exception>? _logException;

    private readonly AutoFeedSettings _settings;
    private readonly ContainerDiscovery _containers;
    private readonly GroundItemDiscovery _groundItems;
    private readonly FuelRules _fuelRules;
    private readonly List<Container> _containerBuffer = new();
    private readonly List<ItemDrop> _groundItemBuffer = new();
    private readonly Dictionary<ZDOID, FeedTiming> _feedTimings = new();
    private readonly List<ZDOID> _timingPurgeBuffer = new();
    private readonly Queue<Smelter> _pendingSmelters = new();
    private readonly Queue<Fireplace> _pendingFireplaces = new();
    private readonly HashSet<Smelter> _queuedSmelters = new();
    private readonly HashSet<Fireplace> _queuedFireplaces = new();
    private bool _processSmelterNext;
    private float _nextTimingPurge;

    private AutoFeedService(AutoFeedSettings settings)
    {
        _settings = settings;
        _containers = new ContainerDiscovery(settings);
        _groundItems = new GroundItemDiscovery(settings);
        _fuelRules = new FuelRules(settings);
    }

    internal static void Configure(AutoFeedSettings settings, Action<Exception> logException)
    {
        _instance = new AutoFeedService(settings);
        _logException = logException;
    }

    internal static void QueueFeed(Smelter smelter)
    {
        _instance?.EnqueueFeed(smelter);
    }

    internal static void QueueRefuel(Fireplace fireplace)
    {
        _instance?.EnqueueRefuel(fireplace);
    }

    internal static void ProcessPending()
    {
        _instance?.ProcessPendingPasses();
    }

    private void EnqueueFeed(Smelter smelter)
    {
        if (_queuedSmelters.Add(smelter))
        {
            _pendingSmelters.Enqueue(smelter);
        }
    }

    private void EnqueueRefuel(Fireplace fireplace)
    {
        if (_settings.UnlimitedFuel.Value)
        {
            return;
        }

        if (_queuedFireplaces.Add(fireplace))
        {
            _pendingFireplaces.Enqueue(fireplace);
        }
    }

    private void ProcessPendingPasses()
    {
        long startTicks = Stopwatch.GetTimestamp();
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

    private bool TryProcessOnePending()
    {
        if (_processSmelterNext)
        {
            if (TryProcessSmelter())
            {
                _processSmelterNext = false;
                return true;
            }

            if (TryProcessFireplace())
            {
                _processSmelterNext = true;
                return true;
            }
        }
        else
        {
            if (TryProcessFireplace())
            {
                _processSmelterNext = true;
                return true;
            }

            if (TryProcessSmelter())
            {
                _processSmelterNext = false;
                return true;
            }
        }

        return false;
    }

    private bool TryProcessSmelter()
    {
        if (_pendingSmelters.Count == 0)
        {
            return false;
        }

        Smelter smelter = _pendingSmelters.Dequeue();
        _queuedSmelters.Remove(smelter);
        if (smelter != null)
        {
            try
            {
                TryFeedStation(smelter);
            }
            catch (Exception exception)
            {
                _logException?.Invoke(exception);
            }
        }

        return true;
    }

    private bool TryProcessFireplace()
    {
        if (_pendingFireplaces.Count == 0)
        {
            return false;
        }

        Fireplace fireplace = _pendingFireplaces.Dequeue();
        _queuedFireplaces.Remove(fireplace);
        if (fireplace != null)
        {
            try
            {
                TryRefuelFireplace(fireplace);
            }
            catch (Exception exception)
            {
                _logException?.Invoke(exception);
            }
        }

        return true;
    }

    private void TryFeedStation(Smelter smelter)
    {
        if (!StationClassifier.TryGetStationSettings(smelter, _settings, out StationSettings stationSettings) ||
            !stationSettings.Enabled.Value)
        {
            return;
        }

        ZNetView? smelterView = smelter.GetComponent<ZNetView>();
        float now = Time.time;
        if (smelterView is null || !smelterView.IsValid() || !smelterView.IsOwner() || !CanFeed(smelterView, now))
        {
            return;
        }

        long playerId = Player.m_localPlayer is null ? 0L : Player.m_localPlayer.GetPlayerID();
        Vector3 position = smelter.transform.position;

        if (_settings.AutoEmptyWindmillOutput.Value &&
            StationClassifier.IsWindmill(smelter) &&
            ShouldEmptyWindmill(smelter))
        {
            smelterView.InvokeRPC("RPC_EmptyProcessed");
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

        SourceSnapshot sources = GetSourceSnapshot(
            position,
            _settings.ContainerSearchRadius.Value,
            playerId,
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

        RecordFeedResult(smelterView, now, fed);
    }

    private void TryRefuelFireplace(Fireplace fireplace)
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

        int fuelTarget = Math.Min(_settings.Fireplaces.FuelTarget.Value, Mathf.FloorToInt(fireplace.m_maxFuel));
        float now = Time.time;
        if (fireplaceView.GetZDO().GetFloat("fuel", 0f) >= fuelTarget || !CanFeed(fireplaceView, now))
        {
            return;
        }

        long playerId = Player.m_localPlayer is null ? 0L : Player.m_localPlayer.GetPlayerID();
        SourceSnapshot sources = GetSourceSnapshot(
            fireplace.transform.position,
            _settings.Fireplaces.Range.Value,
            playerId,
            now);
        bool refueled = TryTakePriorityFuel(sources);
        RecordFeedResult(fireplaceView, now, refueled);
        if (refueled)
        {
            fireplaceView.InvokeRPC("RPC_AddFuel");
        }
    }

    private bool CanFeed(ZNetView targetView, float now)
    {
        PurgeStaleTimings(now);
        ZDOID targetId = targetView.GetZDO().m_uid;
        if (_feedTimings.TryGetValue(targetId, out FeedTiming timing))
        {
            timing.LastSeenTime = now;
            _feedTimings[targetId] = timing;
            if (now < timing.NextFeedTime)
            {
                return false;
            }
        }

        _feedTimings[targetId] = new FeedTiming
        {
            NextFeedTime = now + _settings.FeedInterval.Value,
            LastSeenTime = now
        };
        return true;
    }

    private void RecordFeedResult(ZNetView targetView, float now, bool fed)
    {
        ZDOID targetId = targetView.GetZDO().m_uid;
        FeedTiming timing = _feedTimings.TryGetValue(targetId, out FeedTiming existingTiming)
            ? existingTiming
            : new FeedTiming();
        timing.LastSeenTime = now;
        if (fed)
        {
            timing.RetryInterval = 0f;
            timing.NextFeedTime = now + _settings.FeedInterval.Value;
            _feedTimings[targetId] = timing;
            return;
        }

        float previousInterval = timing.RetryInterval > 0f
            ? timing.RetryInterval
            : _settings.FeedInterval.Value;
        timing.RetryInterval = Math.Min(
            MaxNoSourceRetryInterval,
            Math.Max(_settings.FeedInterval.Value, previousInterval * 2f));
        timing.NextFeedTime = now + timing.RetryInterval;
        _feedTimings[targetId] = timing;
    }

    private void PurgeStaleTimings(float now)
    {
        if (now < _nextTimingPurge)
        {
            return;
        }

        _nextTimingPurge = now + TimingPurgeInterval;
        _timingPurgeBuffer.Clear();
        foreach (KeyValuePair<ZDOID, FeedTiming> pair in _feedTimings)
        {
            if (now - pair.Value.LastSeenTime >= TimingEntryTtl)
            {
                _timingPurgeBuffer.Add(pair.Key);
            }
        }

        foreach (ZDOID targetId in _timingPurgeBuffer)
        {
            _feedTimings.Remove(targetId);
        }
    }

    private bool ShouldEmptyWindmill(Smelter windmill)
    {
        int completedFlour = GetProcessedQueueSize(windmill);
        return completedFlour > 0 &&
               (completedFlour >= _settings.WindmillOutputBatchSize.Value || GetQueueSize(windmill) == 0);
    }

    private bool TryFeedOre(Smelter smelter, ZNetView smelterView, SourceSnapshot sources)
    {
        if (_settings.LeaveLastItem.Value &&
            TryFeedGroundOre(smelter, smelterView, sources))
        {
            return true;
        }

        foreach (Container container in sources.Containers)
        {
            if (!sources.IsContainerUsable(container))
            {
                continue;
            }

            Inventory inventory = container.GetInventory();
            ItemDrop.ItemData? ore = FindConsumableCookableItem(
                smelter,
                inventory,
                sources);
            if (ore is null || ore.m_dropPrefab is null)
            {
                continue;
            }

            string prefabName = ore.m_dropPrefab.name;
            if (string.IsNullOrEmpty(prefabName) ||
                !sources.IsContainerUsable(container) ||
                !inventory.RemoveItem(ore, 1))
            {
                continue;
            }

            sources.RecordContainerRemoval(ore.m_dropPrefab);
            smelterView.InvokeRPC("RPC_AddOre", prefabName, false);
            return true;
        }

        return !_settings.LeaveLastItem.Value &&
             TryFeedGroundOre(smelter, smelterView, sources);
    }

    private bool TryFeedGroundOre(
        Smelter smelter,
        ZNetView smelterView,
        SourceSnapshot sources)
    {
        foreach (ItemDrop itemDrop in sources.GroundItems)
        {
            if (!sources.IsGroundItemUsable(itemDrop))
            {
                continue;
            }

            ItemDrop.ItemData? item = itemDrop.m_itemData;
            if (item is null ||
                item.m_dropPrefab is null ||
                !IsItemAllowed(smelter, item) ||
                !itemDrop.RemoveOne())
            {
                continue;
            }

            smelterView.InvokeRPC("RPC_AddOre", item.m_dropPrefab.name, false);
            return true;
        }

        return false;
    }

    private bool TryFeedFuel(
        ZNetView smelterView,
        string nativeFuelPrefabName,
        SourceSnapshot sources)
    {
        if (TryTakeFuelByName(nativeFuelPrefabName, sources))
        {
            smelterView.InvokeRPC("RPC_AddFuel");
            return true;
        }

        return false;
    }

    private bool TryTakePriorityFuel(SourceSnapshot sources)
    {
        foreach (string priorityFuel in _fuelRules.Priorities)
        {
            if (_fuelRules.IsDisallowed(priorityFuel))
            {
                continue;
            }

            if (TryTakeFuelByName(priorityFuel, sources))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryTakeFuelByName(string fuelPrefabName, SourceSnapshot sources)
    {
        if (_fuelRules.IsDisallowed(fuelPrefabName))
        {
            return false;
        }

        if (_settings.LeaveLastItem.Value &&
            TryTakeGroundFuelByName(fuelPrefabName, sources))
        {
            return true;
        }

        foreach (Container container in sources.Containers)
        {
            if (!sources.IsContainerUsable(container))
            {
                continue;
            }

            Inventory inventory = container.GetInventory();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_dropPrefab is null ||
                    !string.Equals(item.m_dropPrefab.name, fuelPrefabName, StringComparison.OrdinalIgnoreCase) ||
                    !sources.CanConsumeContainerItem(item.m_dropPrefab) ||
                    !sources.IsContainerUsable(container) ||
                    !inventory.RemoveItem(item, 1))
                {
                    continue;
                }

                sources.RecordContainerRemoval(item.m_dropPrefab);
                return true;
            }
        }

        return !_settings.LeaveLastItem.Value &&
             TryTakeGroundFuelByName(fuelPrefabName, sources);
    }

    private bool TryTakeGroundFuelByName(
        string fuelPrefabName,
        SourceSnapshot sources)
    {
        foreach (ItemDrop itemDrop in sources.GroundItems)
        {
            if (!sources.IsGroundItemUsable(itemDrop))
            {
                continue;
            }

            ItemDrop.ItemData? item = itemDrop.m_itemData;
            if (item is null ||
                item.m_dropPrefab is null ||
                !string.Equals(item.m_dropPrefab.name, fuelPrefabName, StringComparison.OrdinalIgnoreCase) ||
                !itemDrop.RemoveOne())
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private ItemDrop.ItemData? FindConsumableCookableItem(
        Smelter smelter,
        Inventory inventory,
        SourceSnapshot sources)
    {
        ItemDrop.ItemData? nativeMatch = FindCookableItem(smelter, inventory);
        if (nativeMatch is not null && nativeMatch.m_dropPrefab is not null &&
            sources.CanConsumeContainerItem(nativeMatch.m_dropPrefab))
        {
            return nativeMatch;
        }

        if (!_settings.LeaveLastItem.Value)
        {
            return null;
        }

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_dropPrefab is not null &&
                IsItemAllowed(smelter, item) &&
                sources.CanConsumeContainerItem(item.m_dropPrefab))
            {
                return item;
            }
        }

        return null;
    }

    private SourceSnapshot GetSourceSnapshot(Vector3 position, float range, long playerId, float now)
    {
        _containers.FillUsableContainers(position, range, playerId, now, _containerBuffer);
        _groundItemBuffer.Clear();
        if (_settings.GroundItems.Enabled.Value)
        {
            _groundItems.FillUsableItems(position, now, _groundItemBuffer);
        }

        return new SourceSnapshot(
            _containers,
            _groundItems,
            _containerBuffer,
            _settings.GroundItems.Enabled.Value ? _groundItemBuffer : Array.Empty<ItemDrop>(),
            _settings.LeaveLastItem.Value,
            position,
            range * range,
            playerId,
            _settings.GroundItems.Range.Value * _settings.GroundItems.Range.Value);
    }

    private static int CountMatchingItems(IEnumerable<ItemDrop.ItemData> items, GameObject prefab)
    {
        int count = 0;
        foreach (ItemDrop.ItemData item in items)
        {
            if (item.m_dropPrefab is not null &&
                string.Equals(item.m_dropPrefab.name, prefab.name, StringComparison.OrdinalIgnoreCase))
            {
                count += item.m_stack;
            }
        }

        return count;
    }

    private sealed class SourceSnapshot
    {
        private Dictionary<string, int>? _matchingContainerItemCounts;
        private readonly ContainerDiscovery _containerDiscovery;
        private readonly GroundItemDiscovery _groundItemDiscovery;
        private readonly bool _leaveLastItem;
        private readonly Vector3 _targetPosition;
        private readonly float _rangeSquared;
        private readonly float _groundRangeSquared;
        private readonly long _playerId;

        internal SourceSnapshot(
            ContainerDiscovery containerDiscovery,
            GroundItemDiscovery groundItemDiscovery,
            IReadOnlyList<Container> containers,
            IReadOnlyList<ItemDrop> groundItems,
            bool leaveLastItem,
            Vector3 targetPosition,
            float rangeSquared,
            long playerId,
            float groundRangeSquared)
        {
            _containerDiscovery = containerDiscovery;
            _groundItemDiscovery = groundItemDiscovery;
            Containers = containers;
            GroundItems = groundItems;
            _leaveLastItem = leaveLastItem;
            _targetPosition = targetPosition;
            _rangeSquared = rangeSquared;
            _groundRangeSquared = groundRangeSquared;
            _playerId = playerId;
        }

        internal IReadOnlyList<Container> Containers { get; }

        internal IReadOnlyList<ItemDrop> GroundItems { get; }

        internal bool IsContainerUsable(Container container)
        {
            return _containerDiscovery.IsUsable(container, _targetPosition, _rangeSquared, _playerId);
        }

        internal bool IsGroundItemUsable(ItemDrop itemDrop)
        {
            return _groundItemDiscovery.IsUsable(itemDrop, _targetPosition, _groundRangeSquared);
        }

        internal bool CanConsumeContainerItem(GameObject prefab)
        {
            if (!_leaveLastItem)
            {
                return true;
            }

            _matchingContainerItemCounts ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (!_matchingContainerItemCounts.TryGetValue(prefab.name, out int count))
            {
                foreach (Container container in Containers)
                {
                    count += CountMatchingItems(container.GetInventory().GetAllItems(), prefab);
                }

                _matchingContainerItemCounts[prefab.name] = count;
            }

            return count > 1;
        }

        internal void RecordContainerRemoval(GameObject prefab)
        {
            if (_matchingContainerItemCounts is not null &&
                _matchingContainerItemCounts.TryGetValue(prefab.name, out int count))
            {
                _matchingContainerItemCounts[prefab.name] = Math.Max(0, count - 1);
            }
        }
    }

    private struct FeedTiming
    {
        internal float NextFeedTime;
        internal float RetryInterval;
        internal float LastSeenTime;
    }

}
