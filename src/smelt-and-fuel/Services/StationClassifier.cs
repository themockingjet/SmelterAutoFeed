using System;

namespace SmeltAndFuel;

internal static class StationClassifier
{
    internal static bool TryGetStationSettings(Smelter smelter, AutoFeedSettings settings, out StationSettings stationSettings)
    {
        string objectName = smelter.gameObject.name;
        if (NameEquals(objectName, "smelter"))
        {
            stationSettings = settings.Smelter;
            return true;
        }

        if (NameEquals(objectName, "blastfurnace"))
        {
            stationSettings = settings.BlastFurnace;
            return true;
        }

        if (NameEquals(objectName, "charcoal_kiln"))
        {
            stationSettings = settings.CharcoalKiln;
            return true;
        }

        if (NameEquals(objectName, "windmill"))
        {
            stationSettings = settings.Windmill;
            return true;
        }

        if (NameEquals(objectName, "spinningwheel") || NameEquals(objectName, "piece_spinningwheel"))
        {
            stationSettings = settings.SpinningWheel;
            return true;
        }

        if (NameEquals(objectName, "eitrrefinery"))
        {
            stationSettings = settings.EitrRefinery;
            return true;
        }

        if (NameEquals(objectName, "frostkiln") || NameEquals(objectName, "piece_frostkiln"))
        {
            stationSettings = settings.FrigidKiln;
            return true;
        }

        stationSettings = null!;
        return false;
    }

    internal static bool SupportsUnlimitedFuel(Smelter smelter)
    {
        if (NameEquals(smelter.gameObject.name, "frostkiln") ||
            NameEquals(smelter.gameObject.name, "piece_frostkiln"))
        {
            return false;
        }

        return true;
    }

    internal static bool IsWindmill(Smelter smelter)
    {
        if (NameEquals(smelter.gameObject.name, "windmill") ||
            NameEquals(smelter.gameObject.name, "piece_windmill"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsOven(CookingStation cookingStation)
    {
        if (NameEquals(cookingStation.gameObject.name, "oven") ||
            NameEquals(cookingStation.gameObject.name, "piece_oven"))
        {
            return true;
        }

        return false;
    }

    internal static bool IsRefillEnabled(Fireplace fireplace, FireplaceSettings settings)
    {
        string prefabName = fireplace.gameObject.name;
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

    private static bool NameEquals(string objectName, string expectedName)
    {
        const string cloneSuffix = "(Clone)";
        int stableLength = objectName.EndsWith(cloneSuffix, StringComparison.Ordinal)
            ? objectName.Length - cloneSuffix.Length
            : objectName.Length;

        return stableLength == expectedName.Length &&
               string.Compare(objectName, 0, expectedName, 0, expectedName.Length, StringComparison.OrdinalIgnoreCase) == 0;
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
