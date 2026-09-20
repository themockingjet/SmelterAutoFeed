using System.Collections.Generic;
using BepInEx.Configuration;

namespace SmeltAndFuel;

internal static class LegacySettingsMigration
{
    internal static bool Migrate(
        ConfigFile config,
        HashSet<ConfigDefinition> existingDefinitions,
        AutoFeedSettings settings)
    {
        bool migrated = false;

        migrated |= Migrate(config, existingDefinitions, "General", "ContainerSearchRadius", settings.ContainerSearchRadius);
        migrated |= Migrate(config, existingDefinitions, "General", "FeedInterval", settings.FeedInterval);
        migrated |= Migrate(config, existingDefinitions, "General", "ContainerRefreshInterval", settings.ContainerRefreshInterval);
        migrated |= Migrate(config, existingDefinitions, "Feeding", "FeedOre", settings.FeedOre);
        migrated |= Migrate(config, existingDefinitions, "Feeding", "FeedFuel", settings.FeedFuel);
        migrated |= Migrate(config, existingDefinitions, "Feeding", "UnlimitedFuel", settings.UnlimitedFuel);
        migrated |= Migrate(config, existingDefinitions, "Fuel", "FuelPriority", settings.FuelPriority);
        migrated |= Migrate(config, existingDefinitions, "Fuel", "FuelDisallowTypes", settings.FuelDisallowTypes);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableSmelter", settings.Smelter.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableBlastFurnace", settings.BlastFurnace.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableCharcoalKiln", settings.CharcoalKiln.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableWindmill", settings.Windmill.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableSpinningWheel", settings.SpinningWheel.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableEitrRefinery", settings.EitrRefinery.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableFrigidKiln", settings.FrigidKiln.Enabled);
        migrated |= Migrate(config, existingDefinitions, "Stations", "EnableOven", settings.Oven);
        migrated |= Migrate(
            config,
            existingDefinitions,
            "Production Stations",
            "AutoEmptyWindmillOutput",
            settings.AutoEmptyWindmillOutput);
        migrated |= Migrate(config, existingDefinitions, "Targets", "SmelterOreTarget", settings.Smelter.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "SmelterFuelTarget", settings.Smelter.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "BlastFurnaceOreTarget", settings.BlastFurnace.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "BlastFurnaceFuelTarget", settings.BlastFurnace.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "CharcoalKilnOreTarget", settings.CharcoalKiln.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "CharcoalKilnFuelTarget", settings.CharcoalKiln.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "WindmillOreTarget", settings.Windmill.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "WindmillFuelTarget", settings.Windmill.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "SpinningWheelOreTarget", settings.SpinningWheel.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "SpinningWheelFuelTarget", settings.SpinningWheel.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "EitrRefineryOreTarget", settings.EitrRefinery.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "EitrRefineryFuelTarget", settings.EitrRefinery.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "FrigidKilnOreTarget", settings.FrigidKiln.OreTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "FrigidKilnFuelTarget", settings.FrigidKiln.FuelTarget);
        migrated |= Migrate(
            config,
            existingDefinitions,
            "Production Targets",
            "Frigid Kiln - Ore / Input Target",
            settings.FrigidKiln.FuelTarget);
        migrated |= Migrate(
            config,
            existingDefinitions,
            "Production Targets",
            "Frigid Kiln - Fuel Target",
            settings.FrigidKiln.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Targets", "FireplaceFuelTarget", settings.Fireplaces.FuelTarget);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "FireplaceRange", settings.Fireplaces.Range);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelStandingTorches", settings.Fireplaces.RefuelStandingTorches);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelBraziers", settings.Fireplaces.RefuelBraziers);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelHotTub", settings.Fireplaces.RefuelHotTub);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelWallTorches", settings.Fireplaces.RefuelWallTorches);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelFirePits", settings.Fireplaces.RefuelFirePits);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelHearth", settings.Fireplaces.RefuelHearth);
        migrated |= Migrate(config, existingDefinitions, "Fireplaces", "RefuelBonfires", settings.Fireplaces.RefuelBonfires);

        return migrated;
    }

    private static bool Migrate<T>(
        ConfigFile config,
        HashSet<ConfigDefinition> existingDefinitions,
        string legacySection,
        string legacyKey,
        ConfigEntry<T> replacement)
    {
        if (!config.TryGetEntry(legacySection, legacyKey, out ConfigEntry<T> legacyEntry))
        {
            return false;
        }

        if (!existingDefinitions.Contains(replacement.Definition))
        {
            replacement.Value = legacyEntry.Value;
        }

        return config.Remove(legacyEntry.Definition);
    }
}
