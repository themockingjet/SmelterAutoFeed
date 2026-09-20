# Changelog

## [Unreleased]

## 0.6.3

### Added

- Added optional dedicated-server presence gating for server-owned targets.
- Added server-side source access evaluation using nearby connected player IDs
  instead of a `0` player-ID fallback.

### Changed

- Documented client/server blending with an external server-ownership provider.

## 0.6.2

- Reduced automatic queue churn by skipping disabled or ineligible targets and
  by waiting until each target's feed interval is due before re-enqueueing it.
- Reused the per-pass source snapshot and its matching-item cache to reduce
  temporary allocations during automatic feeding.

## 0.6.1

- Reduced multiplayer overhead by limiting automation callbacks, queueing, and
  pending-work processing to the local owner of each networked station,
  fireplace, or oven.

## 0.6.0

- Added spatially indexed container discovery with moving wagon-container
  fallback to reduce source scans in dense bases.
- Reused discovery buffers, bounded feed timing state, and spread automatic
  feed work across frames with a fixed internal time budget.
- Revalidated source ownership, access, range, and validity immediately before
  inventory or ground-item mutation.

## 0.5.13

- Changed `Leave Last Item` to protect matching items in eligible containers
  only; matching ground drops can always be consumed.
- Recognized `piece_groundtorch_*` prefabs as standing torches for unlimited
  fuel and automatic refueling.
- Recognized campfires as fire pits for unlimited fuel and automatic refueling.
- Restored normal fireplace fuel degradation after disabling `Unlimited Fuel`.
- Fixed repeated null-reference errors when cached ground items are destroyed
  before the next discovery refresh.
- Changed automatic Windmill output release from every completed Flour to configurable batches, defaulting to 40.
- Release a smaller remaining Flour batch when the native Barley queue is empty.

## 0.5.12

- Added default-enabled `Auto Empty Windmill Output`, which releases completed Barley Flour through Valheim's native Windmill output action.
- Corrected the `Leave Last Item` in-game setting description to state that it applies across all eligible nearby sources.

## 0.5.11

- Changed `Leave Last Item` to preserve one matching item across all eligible nearby containers and ground drops, rather than leaving one in each separate container.

## 0.5.10

- Changed `Leave Last Item` to preserve one matching item across split stacks in an eligible container instead of leaving one item in every slot.
- Apply the same aggregate rule to matching eligible ground drops.

## 0.5.9

- Prevented automatic fuel-slot feeds from exceeding a station's configured or native capacity when Valheim reports fractional fuel consumption.
- Fixed Frigid Kilns showing `Ice 26/25` by requiring room for one complete Ice item before it is removed from a source.

## 0.5.8

- Added independently configurable Stone Oven support for `Unlimited Fuel`.
- Use the Oven's native `CookingStation` fuel API without changing cooking slots or recipe inputs.

## 0.5.7

- Excluded the Frigid Kiln from `Unlimited Fuel` because its native fuel slot holds consumable Ice.
- Keep Frigid Kiln Ice feeding active through its native fuel RPC when `Unlimited Fuel` is enabled, so Ice is removed from nearby eligible sources normally.
- Replaced the Frigid Kiln's misleading fuel target label with `Frigid Kiln - Ice Target` and migrate the previous target labels.

## 0.5.6

- Restored normal fireplace hovering and interaction under `Unlimited Fuel`, while keeping displayed fireplace fuel at maximum without the native refill RPC or its effects.
- Normalized station prefab identities before classification, fixing Frigid Kiln input automation for Valheim's mixed-case `piece_FrostKiln` prefab.

## 0.5.5

- Fixed Spinning Wheel input automation by recognizing Valheim's `piece_spinningwheel` prefab identity, allowing Flax to enter its native input queue.

## 0.5.4

- Changed unlimited fireplace fuel to use Valheim's native infinite-fuel state, preventing repeated automatic-refuel effects.
- Added separately configurable Bonfire refueling and unlimited fuel support.
- Added separately configurable Frigid Kiln input feeding for Ice-to-Liquid-Frost production.

## 0.5.3

- Added the default-enabled `Leave Last Item` setting, which preserves one item in every matching container or ground-item stack.

## 0.5.2

- Fixed the startup `MissingMethodException` for `Container.CheckAccess(long)`.
- Resolved all parameterized native methods with their explicit Valheim signatures, including the unlimited-fuel setters.

## 0.5.1

- Fixed the startup `AmbiguousMatchException` by resolving the overloaded native smelter item-compatibility method with its exact item parameter type.

## 0.5.0

- Added optional dropped ground items as an automatic source for compatible production inputs and fuel.
- Added `Use Ground Items` and `Ground Item Range` settings. Ground items are disabled by default and use a 5-meter default range when enabled.

## 0.4.3

- Fixed automatic inputs being removed from containers without reaching kilns, smelters, or other production queues by invoking Valheim's registered `RPC_AddOre` endpoint.
- Fixed production fuel transfers to invoke the registered `RPC_AddFuel` endpoint.
- Require automatic fuel transfers to match the target station's native fuel prefab, preventing incompatible priority items from being consumed.

## 0.4.2

- Replaced internal CamelCase configuration keys with readable setting names and clearer setting sections.
- Automatically migrate existing configuration values to the readable labels and remove the obsolete entries.

## 0.4.1

- Changed the BepInEx plug-in GUID and configuration prefix to `str.smeltandfuel`.

## 0.4.0

- Renamed the mod to SmeltAndFuel to reflect production-station and fireplace support.
- Added the `UnlimitedFuel` option, which maintains fuel at native maximum for enabled supported stations and fireplaces without consuming fuel items.
- Added compatibility with external fireplace infinite-fuel flags.

## 0.3.0

- Split the plug-in into focused configuration, patch, discovery, fuel-rule, and feed-service files.
- Added `FuelPriority`, which defaults to `RoundLog,Wood` and selects fuel in that order across eligible nearby containers.
- Changed the default fuel denylist to `FineWood`; denylisted fuel always overrides fuel priority.
- Added the source-map architecture guide.

## 0.2.0

- Added automatic refueling for standing torches, braziers, hot tubs, wall torches, fire pits, and hearths.
- Added independent fireplace-category toggles, a 5-meter default fireplace range, and a fuel denylist that defaults to `RoundLog,FineWood`.
- Added independent 1–100 input and fuel target settings for every supported production station and fireplaces.

## 0.1.0

- Added independent station switches for smelters, blast furnaces, charcoal kilns, windmills, spinning wheels, and eitr refineries.
- Added separate global `FeedOre` and `FeedFuel` switches.
