# SmeltAndFuel source map

This document maps each source file to its responsibility. Start with the entry point, then follow the links below when changing a specific behavior.

## Runtime flow

1. [`SmeltAndFuelPlugin.cs`](../SmeltAndFuelPlugin.cs) is the BepInEx entry point. It creates configuration settings, initializes the feed services, and applies Harmony patches.
2. [`Patches/SmelterUpdatePatch.cs`](../Patches/SmelterUpdatePatch.cs) invokes the service after each loaded smelter's native update.
3. [`Patches/FireplaceUpdatePatch.cs`](../Patches/FireplaceUpdatePatch.cs) invokes the service after each loaded fireplace's native update.
4. [`Services/AutoFeedService.cs`](../Services/AutoFeedService.cs) checks ownership, per-type enablement, configured targets, and then performs native ore/fuel RPCs after removing one matching source item. It releases completed Windmill output through the native empty-processed RPC only when the configured batch is ready or its Barley queue is empty.

## File responsibilities

| File | Purpose |
| --- | --- |
| [`SmeltAndFuelPlugin.cs`](../SmeltAndFuelPlugin.cs) | Plug-in metadata and startup only. |
| [`Configuration/AutoFeedSettings.cs`](../Configuration/AutoFeedSettings.cs) | All BepInEx configuration keys, defaults, ranges, and typed settings groups. |
| [`Patches/SmelterUpdatePatch.cs`](../Patches/SmelterUpdatePatch.cs) | Prefix/postfix integration points for production stations backed by `Smelter`. |
| [`Patches/CookingStationUpdatePatch.cs`](../Patches/CookingStationUpdatePatch.cs) | Prefix integration point for Stone Oven unlimited fuel. |
| [`Patches/FireplaceUpdatePatch.cs`](../Patches/FireplaceUpdatePatch.cs) | Postfix integration point for supported fireplaces. |
| [`Services/AutoFeedService.cs`](../Services/AutoFeedService.cs) | Feed decisions, ownership checks, target caps, inventory removal, and native RPC calls. |
| [`Services/UnlimitedFuelService.cs`](../Services/UnlimitedFuelService.cs) | Owner-authoritative native fuel maintenance when `UnlimitedFuel` is enabled. |
| [`Services/StationClassifier.cs`](../Services/StationClassifier.cs) | Shared mapping from Valheim object names to station settings and fireplace-category switches. |
| [`Services/ContainerDiscovery.cs`](../Services/ContainerDiscovery.cs) | Throttled loaded-container discovery plus container ownership, in-use, range, and access checks. |
| [`Services/GroundItemDiscovery.cs`](../Services/GroundItemDiscovery.cs) | Throttled dropped-item discovery plus range and network-ownership checks. |
| [`Services/FuelRules.cs`](../Services/FuelRules.cs) | Live parsing of the ordered fuel priority and fuel denylist configuration. |
| [`Services/NativeMethodDelegate.cs`](../Services/NativeMethodDelegate.cs) | One typed helper for cached delegates to Valheim's non-public native APIs. |
| [`manifest.json`](../manifest.json) | Thunderstore package metadata and runtime dependency declaration. |
| [`scripts/package-release.sh`](../scripts/package-release.sh) | Reproducible Release build and Thunderstore ZIP creation. |

## Fuel selection

`Fuel Priority` is a comma-separated list of fireplace fuel prefab names, evaluated from left to right. The default is `RoundLog,Wood`, so a nearby `RoundLog` is consumed before `Wood` even when the wood is in a different eligible container. Production stations always use their native fuel item, such as coal for a smelter.

`Fuel Disallow Types` is evaluated first. Any listed prefab is excluded even if it also appears in `Fuel Priority` or is a station's native fuel. The default is `FineWood`.

Production-station aliases include Valheim build-piece identities where they differ from a station's gameplay name. For example, the Spinning Wheel uses `piece_spinningwheel` and receives Flax through the same native `Smelter.RPC_AddOre` path as other input stations.

Production fuel values can be fractional while Valheim is consuming them. Before moving one source item, `AutoFeedService` requires room for that whole item, so automatic feeding cannot exceed a station's configured or native fuel capacity.

## Ground items

`Use Ground Items` is disabled by default. When enabled, the mod first checks eligible containers, then searches for compatible dropped items within `Ground Item Range` (default: 5 meters). It uses Valheim's native `ItemDrop.RemoveOne()` method only when the local peer owns the dropped item. This removes one item from the world stack and persists the change before the station or fireplace invokes its native add-item RPC.

`Leave Last Item` is enabled by default. `AutoFeedService` snapshots all eligible nearby containers and ground drops, then totals source items with the same prefab before every native removal. It keeps one item across all split stacks and accessible sources combined. Disable it only when sources may be fully consumed.

Fuel priorities only control automatic refueling. They do not alter Valheim's item definitions, fuel values, or manual interactions.

## Unlimited fuel

When `UnlimitedFuel` is enabled, supported enabled production stations and the Stone Oven are kept at their native fuel maximum by their owner. Fireplaces have their authoritative fuel ZDO value restored to the native maximum before and after their normal update. This keeps their normal hover and interaction available, makes the fuel display full, and avoids the refuel RPC/effects. No fuel item is removed from a container, except for the Frigid Kiln: its Ice is stored in Valheim's fuel slot but is a production input, so the service always uses its native `RPC_AddFuel` path and consumes Ice normally. Input/ore automation remains independent and continues when `FeedOre` is enabled.

If another mod has already marked a fireplace as `m_infiniteFuel`, SmeltAndFuel leaves it unchanged.

## Safety constraints

The service runs only on the network owner of the target. It uses only locally owned, non-active containers that pass Valheim's native access check. It removes exactly one source item before invoking the appropriate native RPC. No queue, fuel, or container state is written directly by the mod.
