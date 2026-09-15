using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class GroundItemDiscovery
{
    private readonly AutoFeedSettings _settings;
    private ItemDrop[] _items = Array.Empty<ItemDrop>();
    private float _nextRefresh;

    internal GroundItemDiscovery(AutoFeedSettings settings)
    {
        _settings = settings;
    }

    internal IEnumerable<ItemDrop> GetUsableItems(Vector3 targetPosition, float now)
    {
        Refresh(now);

        float range = _settings.GroundItems.Range.Value;
        float rangeSquared = range * range;
        foreach (ItemDrop itemDrop in _items)
        {
            if (itemDrop is null ||
                itemDrop.m_itemData is null ||
                (itemDrop.transform.position - targetPosition).sqrMagnitude > rangeSquared)
            {
                continue;
            }

            ZNetView? itemView = itemDrop.GetComponent<ZNetView>();
            if (itemView is null || !itemView.IsValid() || !itemView.IsOwner())
            {
                continue;
            }

            yield return itemDrop;
        }
    }

    private void Refresh(float now)
    {
        if (now < _nextRefresh)
        {
            return;
        }

        _items = UnityEngine.Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
        _nextRefresh = now + _settings.ContainerRefreshInterval.Value;
    }
}
