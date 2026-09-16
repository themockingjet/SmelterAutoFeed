using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class ContainerDiscovery
{
    private static readonly Func<Container, long, bool> CheckAccess =
        NativeMethodDelegate.Create<Func<Container, long, bool>>(
            typeof(Container),
            "CheckAccess",
            new[] { typeof(long) });

    private readonly AutoFeedSettings _settings;
    private Container[] _containers = Array.Empty<Container>();
    private float _nextRefresh;

    internal ContainerDiscovery(AutoFeedSettings settings)
    {
        _settings = settings;
    }

    internal IEnumerable<Container> GetUsableContainers(Vector3 targetPosition, float range, long playerId, float now)
    {
        Refresh(now);

        float rangeSquared = range * range;
        foreach (Container container in _containers)
        {
            if (container is null ||
                !container.IsOwner() ||
                container.IsInUse() ||
                (container.transform.position - targetPosition).sqrMagnitude > rangeSquared ||
                !CheckAccess(container, playerId))
            {
                continue;
            }

            yield return container;
        }
    }

    private void Refresh(float now)
    {
        if (now < _nextRefresh)
        {
            return;
        }

        _containers = UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None);
        _nextRefresh = now + _settings.ContainerRefreshInterval.Value;
    }
}
