using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class SourceSnapshot
{
    private Dictionary<string, int>? _matchingContainerItemCounts;
    private ContainerDiscovery _containerDiscovery = null!;
    private GroundItemDiscovery _groundItemDiscovery = null!;
    private bool _leaveLastItem;
    private Vector3 _targetPosition;
    private float _rangeSquared;
    private float _groundRangeSquared;
    private IReadOnlyList<long> _accessPlayerIds = Array.Empty<long>();

    internal IReadOnlyList<Container> Containers { get; private set; } = Array.Empty<Container>();

    internal IReadOnlyList<ItemDrop> GroundItems { get; private set; } = Array.Empty<ItemDrop>();

    internal bool LeaveLastItem => _leaveLastItem;

    internal FeedDiagnostics Diagnostics { get; private set; } = null!;

    internal void Initialize(
        ContainerDiscovery containerDiscovery,
        GroundItemDiscovery groundItemDiscovery,
        IReadOnlyList<Container> containers,
        IReadOnlyList<ItemDrop> groundItems,
        bool leaveLastItem,
        Vector3 targetPosition,
        float rangeSquared,
        IReadOnlyList<long> accessPlayerIds,
        float groundRangeSquared,
        FeedDiagnostics diagnostics)
    {
        _matchingContainerItemCounts?.Clear();
        _containerDiscovery = containerDiscovery;
        _groundItemDiscovery = groundItemDiscovery;
        Diagnostics = diagnostics;
        Containers = containers;
        GroundItems = groundItems;
        _leaveLastItem = leaveLastItem;
        _targetPosition = targetPosition;
        _rangeSquared = rangeSquared;
        _groundRangeSquared = groundRangeSquared;
        _accessPlayerIds = accessPlayerIds;
    }

    internal bool IsContainerUsable(Container container)
    {
        return _containerDiscovery.IsUsable(container, _targetPosition, _rangeSquared, _accessPlayerIds);
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
                if (IsContainerUsable(container))
                {
                    Inventory? inventory = container.GetInventory();
                    if (inventory is not null)
                    {
                        count += CountMatchingItems(inventory.GetAllItems(), prefab);
                    }
                }
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
