# SmeltAndFuel Architecture

## Purpose

SmeltAndFuel adds owner-authoritative automation around Valheim's existing
production stations and selected fireplaces. It discovers loaded source
containers (and optionally dropped items), asks Valheim which items are valid,
removes one source item, and then invokes the target's native RPC or API.
Production queues, fuel values, item definitions, ownership, and drop behavior
remain owned by Valheim.

## Lifecycle and runtime flow

1. [`SmeltAndFuelPlugin.cs`](../src/smelt-and-fuel/SmeltAndFuelPlugin.cs) is
   loaded by BepInEx. Its `Awake` method binds the existing configuration,
   configures the feed services, and applies Harmony patches using the
   preserved `str.smeltandfuel` GUID.
2. [`SmelterUpdatePatch.cs`](../src/smelt-and-fuel/Patches/SmelterUpdatePatch.cs)
   runs the unlimited-fuel prefix and the automatic feed postfix around every
   loaded `Smelter.UpdateSmelter` call.
3. [`CookingStationUpdatePatch.cs`](../src/smelt-and-fuel/Patches/CookingStationUpdatePatch.cs)
   maintains unlimited Stone Oven fuel in the
   `CookingStation.UpdateCooking` prefix. It does not change cooking slots or
   recipes.
4. [`FireplaceUpdatePatch.cs`](../src/smelt-and-fuel/Patches/FireplaceUpdatePatch.cs)
   maintains unlimited fireplace fuel before and after the native update, then
   performs normal fireplace refueling through the native `RPC_AddFuel` path
   when unlimited fuel is disabled.
5. [`AutoFeedService.cs`](../src/smelt-and-fuel/Services/AutoFeedService.cs)
   applies enablement, target, ownership, access, and interval checks. It
   removes one item only after a valid source has been selected, then invokes
   Valheim's native ore, fuel, or windmill-output RPC.

The service instances are configured once during plugin startup. Discovery
snapshots are refreshed on the configured interval, and per-target feed times
are keyed by the target `ZDOID`. There is no custom scene or network state to
persist. When the plugin or game process is unloaded, Unity and Harmony release
the patched object graph and the static service state is recreated on the next
plugin load; no ownership claims or background workers require explicit
shutdown.

## Components

| Component | Responsibility | Native Valheim boundary |
| --- | --- | --- |
| [`SmeltAndFuelPlugin`](../src/smelt-and-fuel/SmeltAndFuelPlugin.cs) | Plugin identity, configuration startup, and patch registration. | BepInEx `BaseUnityPlugin`, Harmony `PatchAll`. |
| [`AutoFeedSettings`](../src/smelt-and-fuel/Configuration/AutoFeedSettings.cs) | Existing BepInEx keys, defaults, ranges, and legacy-key migration. | BepInEx `ConfigFile`, `ConfigEntry<T>`, and `ConfigFile.Save`. |
| [`SmelterUpdatePatch`](../src/smelt-and-fuel/Patches/SmelterUpdatePatch.cs) | Integrates input, fuel, and windmill-output automation with loaded production stations. | `Smelter.UpdateSmelter`. |
| [`CookingStationUpdatePatch`](../src/smelt-and-fuel/Patches/CookingStationUpdatePatch.cs) | Integrates optional Stone Oven unlimited fuel. | `CookingStation.UpdateCooking`. |
| [`FireplaceUpdatePatch`](../src/smelt-and-fuel/Patches/FireplaceUpdatePatch.cs) | Integrates fireplace unlimited fuel and normal refueling. | `Fireplace.UpdateFireplace`, `Fireplace.RPC_AddFuel`. |
| [`AutoFeedService`](../src/smelt-and-fuel/Services/AutoFeedService.cs) | Feed decisions, aggregate source counts, target caps, removal, and native RPC calls. | `FindCookableItem`, `IsItemAllowed`, queue/fuel APIs, `RPC_AddOre`, `RPC_AddFuel`, and `RPC_EmptyProcessed`. |
| [`UnlimitedFuelService`](../src/smelt-and-fuel/Services/UnlimitedFuelService.cs) | Owner-authoritative native fuel maintenance for supported stations and the oven; existing authoritative fireplace fuel restoration. | `GetFuel`, `SetFuel`, and the fireplace `fuel` ZDO value. |
| [`StationClassifier`](../src/smelt-and-fuel/Services/StationClassifier.cs) | Maps stable Valheim object names to configured station and fireplace categories. | Valheim object names and native station fields. |
| [`ContainerDiscovery`](../src/smelt-and-fuel/Services/ContainerDiscovery.cs) | Throttled loaded-container discovery and source eligibility checks. | `Container.IsOwner`, `Container.IsInUse`, `Container.GetInventory`, and native `Container.CheckAccess(long)`. |
| [`GroundItemDiscovery`](../src/smelt-and-fuel/Services/GroundItemDiscovery.cs) | Throttled dropped-item discovery and source ownership checks. | `ItemDrop`, `ZNetView`, and `ItemDrop.RemoveOne`. |
| [`FuelRules`](../src/smelt-and-fuel/Services/FuelRules.cs) | Live parsing of ordered fireplace fuel priorities and the denylist. | Item prefab names supplied to the native fuel flow. |
| [`NativeMethodDelegate`](../src/smelt-and-fuel/Services/NativeMethodDelegate.cs) | Typed, cached access to non-public Valheim methods with explicit signatures. | Harmony `AccessTools.Method` and `MethodDelegate`. |

## Ownership and state boundaries

- A production station or fireplace is processed only when its valid `ZNetView`
  is active and the local peer is the network owner.
- A source container must be locally owned, not in use, within range, and pass
  Valheim's native `Container.CheckAccess` for the current local player ID.
  A dedicated server therefore uses private containers only when Valheim's
  own access check accepts the server identity.
- Optional ground sources must have a valid, locally owned `ZNetView`.
- Source inventory changes use `Inventory.RemoveItem`; dropped sources use
  `ItemDrop.RemoveOne`. Each successful removal is followed by the target's
  native RPC, so the normal Valheim queue, fuel, effects, and network
  replication remain authoritative.
- Unlimited production-station and oven fuel uses the target's native
  `GetFuel`/`SetFuel` methods. The unlimited fireplace path restores the
  authoritative `fuel` ZDO value without consuming an item or triggering the
  normal refill RPC/effects. Refuelable configured fireplaces also normalize a
  stale native infinite-fuel flag so hover text and normal fuel degradation
  remain available when the setting is disabled.
- The Frigid Kiln is excluded from unlimited fuel. Its Ice remains a native
  consumable fuel/input and is added through the normal `RPC_AddFuel` flow.

## Feed ordering and limits

For each eligible target, the service first handles configured windmill output
release, then input feeding, then fuel feeding. It caps configured targets to
the station's native capacity and requires room for one complete fuel item
before removing a source item. `Leave Last Item` counts matching stacks across
eligible containers only, preserving one aggregate container item when enabled.
Ground drops are always eligible for consumption, and are preferred before a
matching container item when `Leave Last Item` is enabled.

Native `Smelter.FindCookableItem` and `Smelter.IsItemAllowed` determine input
compatibility. Fireplace priorities are considered left to right, with
`Fuel Disallow Types` taking precedence. No hard-coded conversion, fuel, or
capacity table is maintained by the mod.

## Out of scope

- Claiming ownership of a remote station, fireplace, container, or dropped item.
- Replacing Valheim inventories, queues, fuel APIs, RPCs, item definitions, or
  drop behavior with serialized-state edits or hard-coded game tables.
- Adding a custom network protocol, configuration synchronizer, or version
  handshake.
- Feeding unloaded containers or bypassing Valheim's access/privacy rules.
