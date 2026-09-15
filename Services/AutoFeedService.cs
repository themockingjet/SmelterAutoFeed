using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class AutoFeedService
{
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

    private readonly AutoFeedSettings _settings;
    private readonly ContainerDiscovery _containers;
    private readonly GroundItemDiscovery _groundItems;
    private readonly FuelRules _fuelRules;
    private readonly Dictionary<ZDOID, float> _nextFeedTimes = new();

    private AutoFeedService(AutoFeedSettings settings)
    {
        _settings = settings;
        _containers = new ContainerDiscovery(settings);
        _groundItems = new GroundItemDiscovery(settings);
        _fuelRules = new FuelRules(settings);
    }

    internal static void Configure(AutoFeedSettings settings)
    {
        _instance = new AutoFeedService(settings);
    }

    internal static void TryFeed(Smelter smelter)
    {
        _instance?.TryFeedStation(smelter);
    }

    internal static void TryRefuel(Fireplace fireplace)
    {
        _instance?.TryRefuelFireplace(fireplace);
    }

    private void TryFeedStation(Smelter smelter)
    {
        if (!StationClassifier.TryGetStationSettings(smelter, _settings, out StationSettings stationSettings) ||
            !stationSettings.Enabled.Value)
        {
            return;
        }

        ZNetView? smelterView = smelter.GetComponent<ZNetView>();
        if (smelterView is null || !smelterView.IsValid() || !smelterView.IsOwner() || !CanFeed(smelterView, Time.time))
        {
            return;
        }

        float now = Time.time;
        long playerId = Player.m_localPlayer is null ? 0L : Player.m_localPlayer.GetPlayerID();
        Vector3 position = smelter.transform.position;

        if (_settings.AutoEmptyWindmillOutput.Value &&
            StationClassifier.IsWindmill(smelter) &&
            ShouldEmptyWindmill(smelter))
        {
            smelterView.InvokeRPC("RPC_EmptyProcessed");
        }

        int oreTarget = Math.Min(stationSettings.OreTarget.Value, smelter.m_maxOre);
        if (_settings.FeedOre.Value && GetQueueSize(smelter) < oreTarget)
        {
            TryFeedOre(smelter, smelterView, position, playerId, now);
        }

        int fuelTarget = Math.Min(stationSettings.FuelTarget.Value, smelter.m_maxFuel);
        if ((!_settings.UnlimitedFuel.Value || !StationClassifier.SupportsUnlimitedFuel(smelter)) &&
            _settings.FeedFuel.Value &&
            smelter.m_fuelItem is not null &&
            GetFuel(smelter) + 1f <= fuelTarget)
        {
            TryFeedFuel(
                smelterView,
                smelter.m_fuelItem.gameObject.name,
                position,
                _settings.ContainerSearchRadius.Value,
                playerId,
                now);
        }
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
        if (fireplaceView.GetZDO().GetFloat("fuel", 0f) >= fuelTarget || !CanFeed(fireplaceView, Time.time))
        {
            return;
        }

        float now = Time.time;
        long playerId = Player.m_localPlayer is null ? 0L : Player.m_localPlayer.GetPlayerID();
        if (TryTakePriorityFuel(
                fireplace.transform.position,
                _settings.Fireplaces.Range.Value,
                playerId,
                now))
        {
            fireplaceView.InvokeRPC("RPC_AddFuel");
        }
    }

    private bool CanFeed(ZNetView targetView, float now)
    {
        ZDOID targetId = targetView.GetZDO().m_uid;
        if (_nextFeedTimes.TryGetValue(targetId, out float nextFeedTime) && now < nextFeedTime)
        {
            return false;
        }

        _nextFeedTimes[targetId] = now + _settings.FeedInterval.Value;
        return true;
    }

    private bool ShouldEmptyWindmill(Smelter windmill)
    {
        int completedFlour = GetProcessedQueueSize(windmill);
        return completedFlour > 0 &&
               (completedFlour >= _settings.WindmillOutputBatchSize.Value || GetQueueSize(windmill) == 0);
    }

    private void TryFeedOre(Smelter smelter, ZNetView smelterView, Vector3 position, long playerId, float now)
    {
        List<Container> sourceContainers = GetSourceContainers(
            position,
            _settings.ContainerSearchRadius.Value,
            playerId,
            now);
        List<ItemDrop> sourceGroundItems = GetSourceGroundItems(position, now);

        foreach (Container container in sourceContainers)
        {
            Inventory inventory = container.GetInventory();
            ItemDrop.ItemData? ore = FindConsumableCookableItem(
                smelter,
                inventory,
                sourceContainers,
                sourceGroundItems);
            if (ore is null || ore.m_dropPrefab is null)
            {
                continue;
            }

            string prefabName = ore.m_dropPrefab.name;
            if (string.IsNullOrEmpty(prefabName) || !inventory.RemoveItem(ore, 1))
            {
                continue;
            }

            smelterView.InvokeRPC("RPC_AddOre", prefabName, false);
            return;
        }

        foreach (ItemDrop itemDrop in sourceGroundItems)
        {
            ItemDrop.ItemData item = itemDrop.m_itemData;
            if (item.m_dropPrefab is null ||
                !IsItemAllowed(smelter, item) ||
                !CanConsumeItem(item, sourceContainers, sourceGroundItems) ||
                !itemDrop.RemoveOne())
            {
                continue;
            }

            smelterView.InvokeRPC("RPC_AddOre", item.m_dropPrefab.name, false);
            return;
        }
    }

    private void TryFeedFuel(
        ZNetView smelterView,
        string nativeFuelPrefabName,
        Vector3 position,
        float range,
        long playerId,
        float now)
    {
        if (TryTakeFuelByName(nativeFuelPrefabName, position, range, playerId, now))
        {
            smelterView.InvokeRPC("RPC_AddFuel");
        }
    }

    private bool TryTakePriorityFuel(Vector3 position, float range, long playerId, float now)
    {
        foreach (string priorityFuel in _fuelRules.Priorities)
        {
            if (_fuelRules.IsDisallowed(priorityFuel))
            {
                continue;
            }

            if (TryTakeFuelByName(priorityFuel, position, range, playerId, now))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryTakeFuelByName(string fuelPrefabName, Vector3 position, float range, long playerId, float now)
    {
        if (_fuelRules.IsDisallowed(fuelPrefabName))
        {
            return false;
        }

        List<Container> sourceContainers = GetSourceContainers(position, range, playerId, now);
        List<ItemDrop> sourceGroundItems = GetSourceGroundItems(position, now);

        foreach (Container container in sourceContainers)
        {
            Inventory inventory = container.GetInventory();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item.m_dropPrefab is null ||
                    !string.Equals(item.m_dropPrefab.name, fuelPrefabName, StringComparison.OrdinalIgnoreCase) ||
                    !CanConsumeItem(item, sourceContainers, sourceGroundItems) ||
                    !inventory.RemoveItem(item, 1))
                {
                    continue;
                }

                return true;
            }
        }

        foreach (ItemDrop itemDrop in sourceGroundItems)
        {
            ItemDrop.ItemData item = itemDrop.m_itemData;
            if (item.m_dropPrefab is null ||
                !string.Equals(item.m_dropPrefab.name, fuelPrefabName, StringComparison.OrdinalIgnoreCase) ||
                !CanConsumeItem(item, sourceContainers, sourceGroundItems) ||
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
        List<Container> sourceContainers,
        List<ItemDrop> sourceGroundItems)
    {
        ItemDrop.ItemData? nativeMatch = FindCookableItem(smelter, inventory);
        if (nativeMatch is not null && CanConsumeItem(nativeMatch, sourceContainers, sourceGroundItems))
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
                CanConsumeItem(item, sourceContainers, sourceGroundItems))
            {
                return item;
            }
        }

        return null;
    }

    private List<Container> GetSourceContainers(Vector3 position, float range, long playerId, float now)
    {
        return new List<Container>(_containers.GetUsableContainers(position, range, playerId, now));
    }

    private List<ItemDrop> GetSourceGroundItems(Vector3 position, float now)
    {
        return _settings.GroundItems.Enabled.Value
            ? new List<ItemDrop>(_groundItems.GetUsableItems(position, now))
            : new List<ItemDrop>();
    }

    private bool CanConsumeItem(
        ItemDrop.ItemData candidate,
        List<Container> sourceContainers,
        List<ItemDrop> sourceGroundItems)
    {
        if (!_settings.LeaveLastItem.Value)
        {
            return true;
        }

        return CountMatchingSourceItems(candidate.m_dropPrefab, sourceContainers, sourceGroundItems) > 1;
    }

    private static int CountMatchingSourceItems(
        GameObject? prefab,
        List<Container> sourceContainers,
        List<ItemDrop> sourceGroundItems)
    {
        if (prefab is null)
        {
            return 0;
        }

        int count = 0;
        foreach (Container container in sourceContainers)
        {
            count += CountMatchingItems(container.GetInventory().GetAllItems(), prefab);
        }

        foreach (ItemDrop itemDrop in sourceGroundItems)
        {
            count += CountMatchingItems(new[] { itemDrop.m_itemData }, prefab);
        }

        return count;
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

}
