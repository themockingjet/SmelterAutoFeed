using System;
using UnityEngine;

namespace SmeltAndFuel;

internal static class UnlimitedFuelService
{
    private static readonly Func<Smelter, float> GetSmelterFuel =
        NativeMethodDelegate.CreateParameterless<Func<Smelter, float>>(typeof(Smelter), "GetFuel");
    private static readonly Action<Smelter, float> SetSmelterFuel =
        NativeMethodDelegate.Create<Action<Smelter, float>>(typeof(Smelter), "SetFuel", new[] { typeof(float) });
    private static readonly Func<CookingStation, float> GetCookingStationFuel =
        NativeMethodDelegate.CreateParameterless<Func<CookingStation, float>>(typeof(CookingStation), "GetFuel");
    private static readonly Action<CookingStation, float> SetCookingStationFuel =
        NativeMethodDelegate.Create<Action<CookingStation, float>>(typeof(CookingStation), "SetFuel", new[] { typeof(float) });
    private static AutoFeedSettings? _settings;
    private static PlayerPresenceService? _presence;

    internal static void Configure(AutoFeedSettings settings, PlayerPresenceService presence)
    {
        _settings = settings;
        _presence = presence;
    }

    internal static void TryMaintainFuel(Smelter smelter)
    {
        AutoFeedSettings? settings = _settings;
        if (settings is null ||
            !settings.UnlimitedFuel.Value ||
            !StationClassifier.TryGetStationSettings(smelter, settings, out StationSettings stationSettings) ||
            !stationSettings.Enabled.Value ||
            !StationClassifier.SupportsUnlimitedFuel(smelter) ||
            smelter.m_maxFuel <= 0)
        {
            return;
        }

        if (_presence is null ||
            !_presence.IsAutomationActive(smelter.transform.position, Time.time))
        {
            return;
        }

        ZNetView? smelterView = smelter.GetComponent<ZNetView>();
        if (smelterView is null || !smelterView.IsValid() || !smelterView.IsOwner() || GetSmelterFuel(smelter) >= smelter.m_maxFuel)
        {
            return;
        }

        SetSmelterFuel(smelter, smelter.m_maxFuel);
    }

    internal static void TryMaintainFuel(Fireplace fireplace)
    {
        AutoFeedSettings? settings = _settings;
        if (settings is null)
        {
            return;
        }

        if (_presence is null ||
            !_presence.IsAutomationActive(fireplace.transform.position, Time.time))
        {
            return;
        }

        if (!settings.UnlimitedFuel.Value && !fireplace.m_infiniteFuel)
        {
            return;
        }

        ZNetView? fireplaceView = fireplace.GetComponent<ZNetView>();
        if (fireplaceView is null ||
            !fireplaceView.IsValid() ||
            !fireplaceView.IsOwner())
        {
            return;
        }

        ZDO fireplaceZdo = fireplaceView.GetZDO();
        if (fireplace.m_canRefill && fireplace.m_maxFuel > 0f && fireplace.m_infiniteFuel)
        {
            fireplace.m_infiniteFuel = false;
        }

        if (!settings.UnlimitedFuel.Value)
        {
            return;
        }

        if (!StationClassifier.IsRefillEnabled(fireplace, settings.Fireplaces) ||
            !fireplace.m_canRefill ||
            fireplace.m_maxFuel <= 0f)
        {
            return;
        }

        if (fireplaceZdo.GetFloat("fuel", 0f) < fireplace.m_maxFuel)
        {
            fireplaceZdo.Set("fuel", fireplace.m_maxFuel);
        }
    }

    internal static void TryMaintainFuelAfterUpdate(Fireplace fireplace)
    {
        AutoFeedSettings? settings = _settings;
        if (settings is not null && settings.UnlimitedFuel.Value)
        {
            TryMaintainFuel(fireplace);
        }
    }

    internal static void TryMaintainFuel(CookingStation cookingStation)
    {
        AutoFeedSettings? settings = _settings;
        if (settings is null ||
            !settings.UnlimitedFuel.Value ||
            !settings.Oven.Value ||
            !StationClassifier.IsOven(cookingStation) ||
            cookingStation.m_maxFuel <= 0)
        {
            return;
        }

        if (_presence is null ||
            !_presence.IsAutomationActive(cookingStation.transform.position, Time.time))
        {
            return;
        }

        ZNetView? cookingStationView = cookingStation.GetComponent<ZNetView>();
        if (cookingStationView is null ||
            !cookingStationView.IsValid() ||
            !cookingStationView.IsOwner() ||
            GetCookingStationFuel(cookingStation) >= cookingStation.m_maxFuel)
        {
            return;
        }

        SetCookingStationFuel(cookingStation, cookingStation.m_maxFuel);
    }
}
