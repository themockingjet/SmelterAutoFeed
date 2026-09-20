using System.Collections.Generic;
using UnityEngine;

namespace SmeltAndFuel;

internal sealed class PlayerPresenceService
{
    private const float RefreshInterval = 0.25f;

    private readonly AutoFeedSettings _settings;
    private readonly List<PlayerPresence> _players = new();
    private float _nextRefresh;

    internal PlayerPresenceService(AutoFeedSettings settings)
    {
        _settings = settings;
    }

    internal bool IsServer =>
        ZNet.instance is not null && ZNet.instance.IsServer();

    internal bool IsAutomationActive(Vector3 targetPosition, float now)
    {
        if (!IsServer || !_settings.ServerPresenceGate.Value)
        {
            return true;
        }

        Refresh(now);
        float radiusSquared = _settings.ServerPresenceRadius.Value * _settings.ServerPresenceRadius.Value;
        foreach (PlayerPresence player in _players)
        {
            if (DistanceSquaredXZ(player.Position, targetPosition) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    internal void FillAccessPlayerIds(Vector3 targetPosition, float now, List<long> results)
    {
        results.Clear();
        Refresh(now);

        if (!IsServer)
        {
            Player? localPlayer = Player.m_localPlayer;
            if (localPlayer is not null)
            {
                results.Add(localPlayer.GetPlayerID());
            }

            return;
        }

        float radius = _settings.ServerPresenceRadius.Value;
        float radiusSquared = radius * radius;
        foreach (PlayerPresence player in _players)
        {
            if (DistanceSquaredXZ(player.Position, targetPosition) <= radiusSquared &&
                player.PlayerId != 0L)
            {
                results.Add(player.PlayerId);
            }
        }

        if (results.Count == 0 && !_settings.ServerPresenceGate.Value)
        {
            results.Add(ZNet.GetUID());
        }
    }

    private void Refresh(float now)
    {
        if (now < _nextRefresh)
        {
            return;
        }

        _nextRefresh = now + RefreshInterval;
        _players.Clear();

        Player? localPlayer = Player.m_localPlayer;
        if (localPlayer is not null)
        {
            _players.Add(new PlayerPresence(localPlayer.GetPlayerID(), localPlayer.transform.position));
        }

        if (!IsServer)
        {
            return;
        }

        foreach (ZNetPeer peer in ZNet.instance!.GetPeers())
        {
            if (peer is not null &&
                peer.IsReady() &&
                peer.m_playerID != 0L &&
                !ContainsPlayer(peer.m_playerID))
            {
                _players.Add(new PlayerPresence(peer.m_playerID, peer.GetRefPos()));
            }
        }
    }

    private bool ContainsPlayer(long playerId)
    {
        foreach (PlayerPresence player in _players)
        {
            if (player.PlayerId == playerId)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistanceSquaredXZ(Vector3 first, Vector3 second)
    {
        float x = first.x - second.x;
        float z = first.z - second.z;
        return x * x + z * z;
    }

    private readonly struct PlayerPresence
    {
        internal PlayerPresence(long playerId, Vector3 position)
        {
            PlayerId = playerId;
            Position = position;
        }

        internal long PlayerId { get; }

        internal Vector3 Position { get; }
    }
}
