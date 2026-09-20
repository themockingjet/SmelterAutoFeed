# SmeltAndFuel Compatibility

## Supported baseline

- Valheim: the current stable game version represented by the shared local
  reference cache used for the build.
- Loader: BepInEx 5, including the Thunderstore dependency
  `denikson-BepInExPack_Valheim-5.4.2202`.
- Runtime: .NET Framework 4.8.
- Development SDK: .NET 8 or later, selected by `global.json`.
- References: `VALHEIM_MANAGED_PATH` and `BEPINEX_PATH` from
  `$HOME/.config/valheim-dev/env.sh`.

The compiled plugin is tied to the Valheim assemblies in that reference cache.
When Valheim changes the signatures used by the Harmony patches or native
delegates, refresh the shared references and re-verify the affected runtime
paths.

## Multiplayer and ownership

SmeltAndFuel is owner-authoritative and has no custom network protocol,
configuration synchronization, or version handshake.

- The plugin may be installed on clients, on the dedicated server, or on both.
- Installing it on the server does not claim ownership by itself. Centralized
  server execution requires an external server-ownership provider.
- A production station or fireplace is processed only by the peer that owns
  its valid `ZNetView`.
- A source container must be locally owned, not in use, and accepted by
  Valheim's native `Container.CheckAccess` check for the local player ID.
- Optional ground items must be valid and locally owned.
- Mutations use Valheim's native inventory removal, drop removal, station
  methods, and registered RPCs. The mod never claims remote ownership.
- Each process has independent feed timing, discovery caches, retry state, and
  configuration. Use the same plugin version and matching gameplay
  configuration on participating peers. Automatic behavior occurs only on a
  peer where the plugin is present and that peer owns the target.

## Dedicated-server behavior

The server may run SmeltAndFuel. It processes only targets whose valid
`ZNetView` is server-owned. With `Server Automation > Require Nearby Player`
enabled, it also requires a connected player within `Presence Radius` before
queueing, processing, or accessing source containers. SmeltAndFuel does not
claim ownership; use an explicit server-ownership provider if centralized
active-zone execution is desired.

Server-side source access is evaluated against nearby connected player IDs
through native `Container.CheckAccess`. With the gate disabled, public
containers may still be considered using the server identity, while private
containers remain unavailable without a matching nearby player. Ground-item
sources may remain unavailable when the external ownership provider only
claims persistent ZDOs.

## Dependencies and conflicts

The only runtime dependency is BepInEx 5 through the Thunderstore manifest.
Harmony and the Valheim/Unity assemblies are build-time references supplied by
the local environment and are not bundled in the release ZIP.

The mod does not depend on ServerSync and must not be built with a ServerSync
reference, ILRepack merge, ConfigSync integration, or version-handshake code.
Potential conflicts are limited to other mods that patch
`Smelter.UpdateSmelter`, `CookingStation.UpdateCooking`, or
`Fireplace.UpdateFireplace`, especially when those patches replace native
ownership, fuel, or RPC behavior.

### Storage and shared-chest mods

- **MultiUserChest:** SmeltAndFuel does not bypass its ownership or inventory
  flow. A chest that is owned by another peer or is in use is skipped, and a
  failed native removal does not trigger a station RPC.
- **AzuCraftyBoxes:** Both mods can inspect or consume the same source item.
  Keep their leave-one-item policies aligned if preserving a final container
  item is important.
- **AzuAutoStore:** Ground items may move into containers while SmeltAndFuel is
  processing them. SmeltAndFuel revalidates the drop and checks `RemoveOne`;
  a failed removal is treated as a retry rather than a successful feed.

These interactions are race-safe at the native removal/RPC boundary, but they
can still cause retries and additional inventory synchronization. The optional
`Diagnostics > Performance Logging` setting can expose failed source removals
and retry counts.

## Verification matrix

Before release, verify the following against the current shared reference
cache:

| Area | Verification |
| --- | --- |
| Build inputs | `make preflight` succeeds after sourcing the shared environment. |
| SDK/project | `make build` produces `src/smelt-and-fuel/bin/Release/net48/SmeltAndFuel.dll` and copies it to `release/`. |
| Package shape | `make package` creates `release/SmeltAndFuel-0.6.3.zip` with metadata and the plugin DLL at the ZIP root. |
| Diagnostic package | `make package-debug` creates `release/debug/SmeltAndFuel-0.6.3-debug.zip` with performance logging enabled by default for new configs; existing config values remain unchanged. |
| Release safety | `make verify-release` rejects missing, extra, game, or loader DLLs and validates the checksum. |
| Single-player | A loaded smelter, cooking station, fireplace, and windmill follow the configured feed and unlimited-fuel behavior. |
| Multiplayer ownership | Only the owner of each station/fireplace mutates it; non-owner peers do not consume source items. |
| Container access | Locally owned, idle, accessible containers feed; in-use, remote, or denied containers do not. |
| Server/client blend | Server-owned active targets are processed by the server when a player is nearby; client-owned targets are processed by their current owner. |
| Ground items | When enabled, only nearby locally owned compatible drops are consumed. |
| Compatibility | Existing native ore, fuel, output, drop, and fireplace interaction/RPC flows remain functional. |
