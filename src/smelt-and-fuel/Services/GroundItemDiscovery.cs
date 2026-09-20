using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class GroundItemDiscovery
{
    private readonly AutoFeedSettings _settings;
    private readonly FeedDiagnostics _diagnostics;
    private readonly Dictionary<ItemDrop, ZNetView?> _itemViews = new();
    private ItemDrop[] _items = Array.Empty<ItemDrop>();
    private float _nextRefresh;

    internal GroundItemDiscovery(AutoFeedSettings settings, FeedDiagnostics diagnostics)
    {
        _settings = settings;
        _diagnostics = diagnostics;
    }

    internal void FillUsableItems(Vector3 targetPosition, float now, List<ItemDrop> results)
    {
        results.Clear();
        Refresh(now);

        float range = _settings.GroundItems.Range.Value;
        float rangeSquared = range * range;
        foreach (ItemDrop itemDrop in _items)
        {
            if (IsUsable(itemDrop, targetPosition, rangeSquared))
            {
                results.Add(itemDrop);
            }
        }
    }

    internal bool IsUsable(ItemDrop itemDrop, Vector3 targetPosition, float rangeSquared)
    {
        if (itemDrop == null ||
            itemDrop.m_itemData is null ||
            (itemDrop.transform.position - targetPosition).sqrMagnitude > rangeSquared)
        {
            return false;
        }

        if (!_itemViews.TryGetValue(itemDrop, out ZNetView? itemView))
        {
            itemView = itemDrop.GetComponent<ZNetView>();
            _itemViews[itemDrop] = itemView;
        }

        return itemView != null && itemView.IsValid() && itemView.IsOwner();
    }

    private void Refresh(float now)
    {
        if (now < _nextRefresh)
        {
            return;
        }

        long startTicks = _diagnostics.StartTiming();
        _items = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
        _itemViews.Clear();
        _nextRefresh = now + _settings.ContainerRefreshInterval.Value;
        _diagnostics.RecordGroundRefresh(startTicks, _items.Length);
    }
}
