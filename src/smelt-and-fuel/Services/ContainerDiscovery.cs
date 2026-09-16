using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class ContainerDiscovery
{
    private const float SpatialCellSize = 25f;

    private static readonly Func<Container, long, bool> CheckAccess =
        NativeMethodDelegate.Create<Func<Container, long, bool>>(
            typeof(Container),
            "CheckAccess",
            new[] { typeof(long) });

    private readonly AutoFeedSettings _settings;
    private Container[] _containers = Array.Empty<Container>();
    private Dictionary<SpatialCell, List<Container>> _containersByCell = new();
    private List<Container> _movingContainers = new();
    private float _nextRefresh;

    internal ContainerDiscovery(AutoFeedSettings settings)
    {
        _settings = settings;
    }

    internal void FillUsableContainers(
        Vector3 targetPosition,
        float range,
        long playerId,
        float now,
        List<Container> results)
    {
        results.Clear();
        Refresh(now);

        float rangeSquared = range * range;
        int minCellX = Mathf.FloorToInt((targetPosition.x - range) / SpatialCellSize);
        int maxCellX = Mathf.FloorToInt((targetPosition.x + range) / SpatialCellSize);
        int minCellZ = Mathf.FloorToInt((targetPosition.z - range) / SpatialCellSize);
        int maxCellZ = Mathf.FloorToInt((targetPosition.z + range) / SpatialCellSize);

        for (int cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (int cellZ = minCellZ; cellZ <= maxCellZ; cellZ++)
            {
                if (!_containersByCell.TryGetValue(new SpatialCell(cellX, cellZ), out List<Container>? containers))
                {
                    continue;
                }

                foreach (Container container in containers)
                {
                    if (IsUsable(container, targetPosition, rangeSquared, playerId))
                    {
                        results.Add(container);
                    }
                }
            }
        }

        foreach (Container container in _movingContainers)
        {
            if (IsUsable(container, targetPosition, rangeSquared, playerId))
            {
                results.Add(container);
            }
        }
    }

    internal bool IsUsable(Container container, Vector3 targetPosition, float rangeSquared, long playerId)
    {
        return container != null &&
               container.IsOwner() &&
               !container.IsInUse() &&
               (container.transform.position - targetPosition).sqrMagnitude <= rangeSquared &&
               CheckAccess(container, playerId);
    }

    private void Refresh(float now)
    {
        if (now < _nextRefresh)
        {
            return;
        }

        _containers = UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None);
        Dictionary<SpatialCell, List<Container>> containersByCell = new();
        List<Container> movingContainers = new();
        foreach (Container container in _containers)
        {
            if (container == null)
            {
                continue;
            }

            if (container.m_wagon != null)
            {
                movingContainers.Add(container);
                continue;
            }

            Vector3 position = container.transform.position;
            SpatialCell cell = new(
                Mathf.FloorToInt(position.x / SpatialCellSize),
                Mathf.FloorToInt(position.z / SpatialCellSize));
            if (!containersByCell.TryGetValue(cell, out List<Container>? containers))
            {
                containers = new List<Container>();
                containersByCell.Add(cell, containers);
            }

            containers.Add(container);
        }

        _containersByCell = containersByCell;
        _movingContainers = movingContainers;
        _nextRefresh = now + _settings.ContainerRefreshInterval.Value;
    }

    private readonly struct SpatialCell : IEquatable<SpatialCell>
    {
        internal SpatialCell(int x, int z)
        {
            X = x;
            Z = z;
        }

        private int X { get; }

        private int Z { get; }

        public bool Equals(SpatialCell other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is SpatialCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }
    }
}
