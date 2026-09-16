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

- A production station or fireplace is processed only by the peer that owns
  its valid `ZNetView`.
- A source container must be locally owned, not in use, and accepted by
  Valheim's native `Container.CheckAccess` check for the local player ID.
- Optional ground items must be valid and locally owned.
- Mutations use Valheim's native inventory removal, drop removal, station
  methods, and registered RPCs. The mod never claims remote ownership.
- Clients and servers can load the plugin independently because it does not
  add serialized state or custom RPCs. Automatic behavior occurs only on a
  peer where the plugin is present and that peer owns the target.

## Dedicated-server behavior

The plugin is safe to load on a dedicated server. A server processes a target
only when it is the valid network owner. Private containers remain unavailable
unless Valheim's native access check accepts the server identity; the mod does
not bypass that privacy boundary. Ground-item automation likewise requires
local network ownership.

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

## Verification matrix

Before release, verify the following against the current shared reference
cache:

| Area | Verification |
| --- | --- |
| Build inputs | `make preflight` succeeds after sourcing the shared environment. |
| SDK/project | `make build` produces `src/smelt-and-fuel/bin/Release/net48/SmeltAndFuel.dll` and copies it to `release/`. |
| Package shape | `make package` creates `release/SmeltAndFuel-0.6.0.zip` with metadata and the plugin DLL at the ZIP root. |
| Release safety | `make verify-release` rejects missing, extra, game, or loader DLLs and validates the checksum. |
| Single-player | A loaded smelter, cooking station, fireplace, and windmill follow the configured feed and unlimited-fuel behavior. |
| Multiplayer ownership | Only the owner of each station/fireplace mutates it; non-owner peers do not consume source items. |
| Container access | Locally owned, idle, accessible containers feed; in-use, remote, or denied containers do not. |
| Dedicated server | Server startup succeeds and native access/privacy checks remain enforced. |
| Ground items | When enabled, only nearby locally owned compatible drops are consumed. |
| Compatibility | Existing native ore, fuel, output, drop, and fireplace interaction/RPC flows remain functional. |
