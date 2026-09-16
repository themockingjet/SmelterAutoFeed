using System;

namespace SmeltAndFuel;

internal static class StationClassifier
{
    internal static bool TryGetStationSettings(Smelter smelter, AutoFeedSettings settings, out StationSettings stationSettings)
    {
        switch (GetStableObjectName(smelter.gameObject.name))
        {
            case "smelter":
                stationSettings = settings.Smelter;
                return true;
            case "blastfurnace":
                stationSettings = settings.BlastFurnace;
                return true;
            case "charcoal_kiln":
                stationSettings = settings.CharcoalKiln;
                return true;
            case "windmill":
                stationSettings = settings.Windmill;
                return true;
            case "spinningwheel":
            case "piece_spinningwheel":
                stationSettings = settings.SpinningWheel;
                return true;
            case "eitrrefinery":
                stationSettings = settings.EitrRefinery;
                return true;
            case "frostkiln":
            case "piece_frostkiln":
                stationSettings = settings.FrigidKiln;
                return true;
            default:
                stationSettings = null!;
                return false;
        }
    }

    internal static bool SupportsUnlimitedFuel(Smelter smelter)
    {
        switch (GetStableObjectName(smelter.gameObject.name))
        {
            case "frostkiln":
            case "piece_frostkiln":
                return false;
            default:
                return true;
        }
    }

    internal static bool IsWindmill(Smelter smelter)
    {
        switch (GetStableObjectName(smelter.gameObject.name))
        {
            case "windmill":
            case "piece_windmill":
                return true;
            default:
                return false;
        }
    }

    internal static bool IsOven(CookingStation cookingStation)
    {
        switch (GetStableObjectName(cookingStation.gameObject.name))
        {
            case "oven":
            case "piece_oven":
                return true;
            default:
                return false;
        }
    }

    internal static bool IsRefillEnabled(Fireplace fireplace, FireplaceSettings settings)
    {
        string prefabName = GetStableObjectName(fireplace.gameObject.name);
        string displayName = fireplace.m_name ?? string.Empty;

        if (HasFireplaceName(prefabName, displayName, "standingtorch", "groundtorch"))
        {
            return settings.RefuelStandingTorches.Value;
        }

        if (HasFireplaceName(prefabName, displayName, "brazier"))
        {
            return settings.RefuelBraziers.Value;
        }

        if (HasFireplaceName(prefabName, displayName, "hottub", "hot_tub"))
        {
            return settings.RefuelHotTub.Value;
        }

        if (HasFireplaceName(prefabName, displayName, "walltorch"))
        {
            return settings.RefuelWallTorches.Value;
        }

        if (HasFireplaceName(prefabName, displayName, "firepit", "fire_pit") ||
            HasFireplaceName(prefabName, displayName, "campfire"))
        {
            return settings.RefuelFirePits.Value;
        }

        if (HasFireplaceName(prefabName, displayName, "hearth"))
        {
            return settings.RefuelHearth.Value;
        }

        return HasFireplaceName(prefabName, displayName, "bonfire") && settings.RefuelBonfires.Value;
    }

    private static string GetStableObjectName(string objectName)
    {
        const string cloneSuffix = "(Clone)";
        string prefabName = objectName.EndsWith(cloneSuffix, StringComparison.Ordinal)
            ? objectName.Substring(0, objectName.Length - cloneSuffix.Length)
            : objectName;

        return prefabName.ToLowerInvariant();
    }

    private static bool HasFireplaceName(string prefabName, string displayName, string name)
    {
        return prefabName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0 ||
               displayName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool HasFireplaceName(string prefabName, string displayName, string firstName, string secondName)
    {
        return HasFireplaceName(prefabName, displayName, firstName) ||
               HasFireplaceName(prefabName, displayName, secondName);
    }
}
