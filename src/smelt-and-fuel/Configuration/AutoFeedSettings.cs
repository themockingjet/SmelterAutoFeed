using System.Collections.Generic;
using BepInEx.Configuration;

namespace SmeltAndFuel;

internal sealed class AutoFeedSettings
{
    private AutoFeedSettings(
        ConfigEntry<float> containerSearchRadius,
        ConfigEntry<float> feedInterval,
        ConfigEntry<float> containerRefreshInterval,
        ConfigEntry<bool> feedOre,
        ConfigEntry<bool> feedFuel,
        ConfigEntry<bool> unlimitedFuel,
        ConfigEntry<bool> leaveLastItem,
        GroundItemSettings groundItems,
        ConfigEntry<string> fuelPriority,
        ConfigEntry<string> fuelDisallowTypes,
        StationSettings smelter,
        StationSettings blastFurnace,
        StationSettings charcoalKiln,
        StationSettings windmill,
        StationSettings spinningWheel,
        StationSettings eitrRefinery,
        StationSettings frigidKiln,
        ConfigEntry<bool> oven,
        ConfigEntry<bool> autoEmptyWindmillOutput,
        ConfigEntry<int> windmillOutputBatchSize,
        FireplaceSettings fireplaces)
    {
        ContainerSearchRadius = containerSearchRadius;
        FeedInterval = feedInterval;
        ContainerRefreshInterval = containerRefreshInterval;
        FeedOre = feedOre;
        FeedFuel = feedFuel;
        UnlimitedFuel = unlimitedFuel;
        LeaveLastItem = leaveLastItem;
        GroundItems = groundItems;
        FuelPriority = fuelPriority;
        FuelDisallowTypes = fuelDisallowTypes;
        Smelter = smelter;
        BlastFurnace = blastFurnace;
        CharcoalKiln = charcoalKiln;
        Windmill = windmill;
        SpinningWheel = spinningWheel;
        EitrRefinery = eitrRefinery;
        FrigidKiln = frigidKiln;
        Oven = oven;
        AutoEmptyWindmillOutput = autoEmptyWindmillOutput;
        WindmillOutputBatchSize = windmillOutputBatchSize;
        Fireplaces = fireplaces;
    }

    internal ConfigEntry<float> ContainerSearchRadius { get; }

    internal ConfigEntry<float> FeedInterval { get; }

    internal ConfigEntry<float> ContainerRefreshInterval { get; }

    internal ConfigEntry<bool> FeedOre { get; }

    internal ConfigEntry<bool> FeedFuel { get; }

    internal ConfigEntry<bool> UnlimitedFuel { get; }

    internal ConfigEntry<bool> LeaveLastItem { get; }

    internal GroundItemSettings GroundItems { get; }

    internal ConfigEntry<string> FuelPriority { get; }

    internal ConfigEntry<string> FuelDisallowTypes { get; }

    internal StationSettings Smelter { get; }

    internal StationSettings BlastFurnace { get; }

    internal StationSettings CharcoalKiln { get; }

    internal StationSettings Windmill { get; }

    internal StationSettings SpinningWheel { get; }

    internal StationSettings EitrRefinery { get; }

    internal StationSettings FrigidKiln { get; }

    internal ConfigEntry<bool> Oven { get; }

    internal ConfigEntry<bool> AutoEmptyWindmillOutput { get; }

    internal ConfigEntry<int> WindmillOutputBatchSize { get; }

    internal FireplaceSettings Fireplaces { get; }

    internal static AutoFeedSettings Create(ConfigFile config)
    {
        HashSet<ConfigDefinition> existingDefinitions = new(config.Keys);
        AutoFeedSettings settings = new(
            config.Bind(
                "General",
                "Container Search Radius",
                10f,
                new ConfigDescription(
                    "Maximum distance, in meters, between a production station and a source container.",
                    new AcceptableValueRange<float>(1f, 50f))),
            config.Bind(
                "General",
                "Feed Interval",
                0.5f,
                new ConfigDescription(
                    "Minimum time, in seconds, between automatic feed passes for one station or fireplace.",
                    new AcceptableValueRange<float>(0.1f, 10f))),
            config.Bind(
                "General",
                "Container Refresh Interval",
                2f,
                new ConfigDescription(
                    "How often, in seconds, SmeltAndFuel re-discovers loaded containers.",
                    new AcceptableValueRange<float>(0.5f, 30f))),
            config.Bind(
                "Feeding",
                "Feed Ore / Inputs",
                true,
                "Automatically feed a station's input queue."),
            config.Bind(
                "Feeding",
                "Feed Fuel",
                true,
                "Automatically feed a station's fuel slot."),
            config.Bind(
                "Feeding",
                "Unlimited Fuel",
                false,
                "Keep fuel at native maximum for enabled supported stations and fireplaces without consuming fuel items."),
            config.Bind(
                "Feeding",
                "Leave Last Item",
                true,
                "Keep one matching item across all eligible nearby containers. Ground items are always eligible for consumption."),
            new GroundItemSettings(
                config.Bind(
                    "Ground Items",
                    "Use Ground Items",
                    false,
                    "Allow compatible dropped items on the ground to be used as an automatic source."),
                config.Bind(
                    "Ground Items",
                    "Ground Item Range",
                    5f,
                    new ConfigDescription(
                        "Maximum distance, in meters, between a station or fireplace and a dropped item.",
                        new AcceptableValueRange<float>(1f, 50f)))),
            config.Bind(
                "Fuel",
                "Fuel Priority",
                "RoundLog,Wood",
                "Comma-separated fireplace fuel prefab names, highest priority first. Production stations always use their native fuel."),
            config.Bind(
                "Fuel",
                "Fuel Disallow Types",
                "FineWood",
                "Comma-separated fuel prefab names that SmeltAndFuel must never use."),
            CreateStationSettings(config, "Smelter", "smelters"),
            CreateStationSettings(config, "Blast Furnace", "blast furnaces"),
            CreateStationSettings(config, "Charcoal Kiln", "charcoal kilns"),
            CreateStationSettings(config, "Windmill", "windmills"),
            CreateStationSettings(config, "Spinning Wheel", "spinning wheels"),
            CreateStationSettings(config, "Eitr Refinery", "eitr refineries"),
            CreateFrigidKilnSettings(config),
            config.Bind("Production Stations", "Oven", true, "Enable unlimited fuel for stone ovens."),
            config.Bind(
                "Production Stations",
                "Auto Empty Windmill Output",
                true,
                "Release completed windmill flour automatically so production can continue."),
            config.Bind(
                "Production Targets",
                "Windmill Output Batch Size",
                40,
                new ConfigDescription(
                    "Release completed windmill flour when this many items are ready. Remaining flour is released when the Barley queue becomes empty.",
                    new AcceptableValueRange<int>(1, 40))),
            CreateFireplaceSettings(config));

        if (MigrateLegacySettings(config, existingDefinitions, settings))
        {
            config.Save();
        }

        return settings;
    }

    private static StationSettings CreateStationSettings(ConfigFile config, string name, string displayName)
    {
        return new StationSettings(
            config.Bind("Production Stations", name, true, "Enable automation for " + displayName + "."),
            CreateTarget(config, name + " - Ore / Input Target", "Maximum input items to keep in each enabled " + displayName + "."),
            CreateTarget(config, name + " - Fuel Target", "Maximum fuel items to keep in each enabled " + displayName + "."));
    }

    private static StationSettings CreateFrigidKilnSettings(ConfigFile config)
    {
        ConfigEntry<int> iceTarget = CreateTarget(
            config,
            "Frigid Kiln - Ice Target",
            "Maximum Ice items to keep in each enabled frigid kiln.");

        return new StationSettings(
            config.Bind("Production Stations", "Frigid Kiln", true, "Enable automation for frigid kilns."),
            iceTarget,
            iceTarget);
    }

    private static FireplaceSettings CreateFireplaceSettings(ConfigFile config)
    {
        return new FireplaceSettings(
            config.Bind(
                "Fireplaces",
                "Fireplace Range",
                5f,
                new ConfigDescription(
                    "Maximum distance, in meters, between a fireplace and a source container.",
                    new AcceptableValueRange<float>(1f, 50f))),
            config.Bind("Fireplaces", "Standing Torches", true, "Enable automatic refueling for standing torches."),
            config.Bind("Fireplaces", "Braziers", true, "Enable automatic refueling for braziers."),
            config.Bind("Fireplaces", "Hot Tubs", true, "Enable automatic refueling for hot tubs."),
            config.Bind("Fireplaces", "Wall Torches", true, "Enable automatic refueling for wall torches."),
            config.Bind("Fireplaces", "Fire Pits", true, "Enable automatic refueling for fire pits."),
            config.Bind("Fireplaces", "Hearths", true, "Enable automatic refueling for hearths."),
            config.Bind("Fireplaces", "Bonfires", true, "Enable automatic refueling for bonfires."),
            CreateTarget(config, "Fireplace - Fuel Target", "Maximum fuel items to keep in each enabled fireplace."));
    }

    private static ConfigEntry<int> CreateTarget(ConfigFile config, string key, string description)
    {
        return config.Bind(
            "Production Targets",
            key,
            100,
            new ConfigDescription(description, new AcceptableValueRange<int>(1, 100)));
    }

    private static bool MigrateLegacySettings(
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

internal sealed class StationSettings
{
    internal StationSettings(ConfigEntry<bool> enabled, ConfigEntry<int> oreTarget, ConfigEntry<int> fuelTarget)
    {
        Enabled = enabled;
        OreTarget = oreTarget;
        FuelTarget = fuelTarget;
    }

    internal ConfigEntry<bool> Enabled { get; }

    internal ConfigEntry<int> OreTarget { get; }

    internal ConfigEntry<int> FuelTarget { get; }
}

internal sealed class GroundItemSettings
{
    internal GroundItemSettings(ConfigEntry<bool> enabled, ConfigEntry<float> range)
    {
        Enabled = enabled;
        Range = range;
    }

    internal ConfigEntry<bool> Enabled { get; }

    internal ConfigEntry<float> Range { get; }
}

internal sealed class FireplaceSettings
{
    internal FireplaceSettings(
        ConfigEntry<float> range,
        ConfigEntry<bool> refuelStandingTorches,
        ConfigEntry<bool> refuelBraziers,
        ConfigEntry<bool> refuelHotTub,
        ConfigEntry<bool> refuelWallTorches,
        ConfigEntry<bool> refuelFirePits,
        ConfigEntry<bool> refuelHearth,
        ConfigEntry<bool> refuelBonfires,
        ConfigEntry<int> fuelTarget)
    {
        Range = range;
        RefuelStandingTorches = refuelStandingTorches;
        RefuelBraziers = refuelBraziers;
        RefuelHotTub = refuelHotTub;
        RefuelWallTorches = refuelWallTorches;
        RefuelFirePits = refuelFirePits;
        RefuelHearth = refuelHearth;
        RefuelBonfires = refuelBonfires;
        FuelTarget = fuelTarget;
    }

    internal ConfigEntry<float> Range { get; }

    internal ConfigEntry<bool> RefuelStandingTorches { get; }

    internal ConfigEntry<bool> RefuelBraziers { get; }

    internal ConfigEntry<bool> RefuelHotTub { get; }

    internal ConfigEntry<bool> RefuelWallTorches { get; }

    internal ConfigEntry<bool> RefuelFirePits { get; }

    internal ConfigEntry<bool> RefuelHearth { get; }

    internal ConfigEntry<bool> RefuelBonfires { get; }

    internal ConfigEntry<int> FuelTarget { get; }
}
