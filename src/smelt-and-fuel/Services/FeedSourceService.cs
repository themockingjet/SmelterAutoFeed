using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class FeedSourceService
{
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

    private readonly AutoFeedSettings _settings;
    private readonly ContainerDiscovery _containers;
    private readonly GroundItemDiscovery _groundItems;
    private readonly FuelRules _fuelRules;
    private readonly FeedDiagnostics _diagnostics;
    private readonly PlayerPresenceService _presence;
    private readonly List<Container> _containerBuffer = new();
    private readonly List<ItemDrop> _groundItemBuffer = new();
    private readonly List<long> _accessPlayerIds = new();
    private readonly SourceSnapshot _sourceSnapshot = new();

    internal FeedSourceService(
        AutoFeedSettings settings,
        FeedDiagnostics diagnostics,
        PlayerPresenceService presence)
    {
        _settings = settings;
        _diagnostics = diagnostics;
        _presence = presence;
        _containers = new ContainerDiscovery(settings, diagnostics);
        _groundItems = new GroundItemDiscovery(settings, diagnostics);
        _fuelRules = new FuelRules(settings);
    }

    internal SourceSnapshot GetSnapshot(Vector3 position, float range, float now)
    {
        _diagnostics.RecordSourceQuery();
        _presence.FillAccessPlayerIds(position, now, _accessPlayerIds);
        _containers.FillUsableContainers(position, range, _accessPlayerIds, now, _containerBuffer);
        _groundItemBuffer.Clear();
        if (_settings.GroundItems.Enabled.Value)
        {
            _groundItems.FillUsableItems(position, now, _groundItemBuffer);
        }

        _sourceSnapshot.Initialize(
            _containers,
            _groundItems,
            _containerBuffer,
            _settings.GroundItems.Enabled.Value ? _groundItemBuffer : Array.Empty<ItemDrop>(),
            _settings.LeaveLastItem.Value,
            position,
            range * range,
            _accessPlayerIds,
            _settings.GroundItems.Range.Value * _settings.GroundItems.Range.Value,
            _diagnostics);
        return _sourceSnapshot;
    }

    internal FeedDiagnostics Diagnostics => _diagnostics;

    internal ItemDrop.ItemData? TryTakeOre(Smelter smelter, SourceSnapshot sources)
    {
        if (_settings.LeaveLastItem.Value)
        {
            ItemDrop.ItemData? groundItem = TryTakeGroundItem(
                sources,
                item => IsItemAllowed(smelter, item));
            if (groundItem is not null)
            {
                return groundItem;
            }
        }

        ItemDrop.ItemData? containerItem = TryTakeContainerItem(
            sources,
            inventory => FindConsumableCookableItem(smelter, inventory, sources),
            item => !string.IsNullOrEmpty(item.m_dropPrefab?.name));
        if (containerItem is not null)
        {
            return containerItem;
        }

        return !_settings.LeaveLastItem.Value
            ? TryTakeGroundItem(sources, item => IsItemAllowed(smelter, item))
            : null;
    }

    internal bool TryTakeFuelByName(string fuelPrefabName, SourceSnapshot sources)
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

        ItemDrop.ItemData? containerItem = TryTakeContainerItem(
            sources,
            inventory => FindFuelItem(inventory, fuelPrefabName, sources));
        if (containerItem is not null)
        {
            return true;
        }

        return !_settings.LeaveLastItem.Value &&
               TryTakeGroundFuelByName(fuelPrefabName, sources);
    }

    internal bool TryTakePriorityFuel(SourceSnapshot sources)
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

    private ItemDrop.ItemData? TryTakeContainerItem(
        SourceSnapshot sources,
        Func<Inventory, ItemDrop.ItemData?> selector,
        Func<ItemDrop.ItemData, bool>? validator = null)
    {
        foreach (Container container in sources.Containers)
        {
            if (!sources.IsContainerUsable(container))
            {
                continue;
            }

            Inventory? inventory = container.GetInventory();
            if (inventory is null)
            {
                continue;
            }

            ItemDrop.ItemData? item = selector(inventory);
            if (item is null ||
                item.m_dropPrefab is null ||
                (validator is not null && !validator(item)) ||
                !sources.IsContainerUsable(container))
            {
                continue;
            }

            bool removed = inventory.RemoveItem(item, 1);
            _diagnostics.RecordContainerRemoval(removed);
            if (!removed)
            {
                continue;
            }

            sources.RecordContainerRemoval(item.m_dropPrefab);
            return item;
        }

        return null;
    }

    private static ItemDrop.ItemData? TryTakeGroundItem(
        SourceSnapshot sources,
        Func<ItemDrop.ItemData, bool> predicate)
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
                !predicate(item))
            {
                continue;
            }

            bool removed = itemDrop.RemoveOne();
            sources.Diagnostics.RecordGroundRemoval(removed);
            if (!removed)
            {
                continue;
            }

            return item;
        }

        return null;
    }

    private static ItemDrop.ItemData? FindConsumableCookableItem(
        Smelter smelter,
        Inventory inventory,
        SourceSnapshot sources)
    {
        ItemDrop.ItemData? nativeMatch = FindCookableItem(smelter, inventory);
        if (nativeMatch is not null &&
            nativeMatch.m_dropPrefab is not null &&
            sources.CanConsumeContainerItem(nativeMatch.m_dropPrefab))
        {
            return nativeMatch;
        }

        if (!sources.LeaveLastItem)
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

    private static ItemDrop.ItemData? FindFuelItem(
        Inventory inventory,
        string fuelPrefabName,
        SourceSnapshot sources)
    {
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item.m_dropPrefab is not null &&
                string.Equals(item.m_dropPrefab.name, fuelPrefabName, StringComparison.OrdinalIgnoreCase) &&
                sources.CanConsumeContainerItem(item.m_dropPrefab))
            {
                return item;
            }
        }

        return null;
    }

    private bool TryTakeGroundFuelByName(string fuelPrefabName, SourceSnapshot sources)
    {
        return TryTakeGroundItem(
            sources,
            item => string.Equals(
                item.m_dropPrefab!.name,
                fuelPrefabName,
                StringComparison.OrdinalIgnoreCase)) is not null;
    }
}
