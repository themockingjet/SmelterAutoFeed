# SmeltAndFuel Agent Instructions

## Purpose

SmeltAndFuel is a BepInEx 5 Valheim mod that feeds compatible production
inputs and fuel from nearby accessible containers into owner-authoritative
production stations and selected fireplaces. It also supports optional ground
items, windmill output release, and unlimited fuel while preserving Valheim's
native APIs and RPC flows.

## Compatibility

- Game baseline: the current stable Valheim version represented by the shared
  local reference cache.
- Loader: BepInEx 5.
- Runtime target: .NET Framework 4.8.
- Development SDK: .NET 8 or later.
- Build-time publicizer: `BepInEx.AssemblyPublicizer.MSBuild`.
- Configuration synchronization: none; this mod has no ServerSync dependency,
  ConfigSync integration, or version handshake.
- Multiplayer: owner-authoritative. Only the valid network owner may mutate a
  production station or fireplace, and source containers must also satisfy
  local ownership, in-use, and native access checks.

## Critical game rules

- Keep the plugin GUID `str.smeltandfuel`, assembly name `SmeltAndFuel`, root
  namespace `SmeltAndFuel`, package name `SmeltAndFuel`, and version `0.5.13`.
- Preserve every existing BepInEx configuration section, key, default, range,
  and legacy migration path.
- Mutate networked objects only through valid ownership and Valheim's native
  inventory, fuel, drop, and RPC flows.
- Prefer native Valheim discovery and compatibility APIs over direct state
  edits or hard-coded game data.
- Never claim remote ownership or bypass `Container.CheckAccess`.
- Keep production code below `src/smelt-and-fuel/`, separated into
  `Configuration/`, `Services/`, and `Patches/`.

## Build and release

- Source the shared environment:
  `source "$HOME/.config/valheim-dev/env.sh"`.
- Run `make preflight` before building or releasing.
- Run `make build`.
- Run `make package`.
- Run `make verify-release`.
- Release ZIPs must contain exactly `manifest.json`, `README.md`,
  `CHANGELOG.md`, `icon.png`, and `SmeltAndFuel.dll` at the ZIP root.
- Keep Valheim and BepInEx reference DLLs outside the repository and out of
  release packages.

## Icon generation

- Keep `docs/ICON_BRIEF.md` complete and specific to SmeltAndFuel.
- Use the shared visual system and verify `Thunderstore/icon.png` is exactly
  256x256 before packaging.
- The final icon must be original, unwatermarked, text-free, and clearly
  distinguishable by its smelter-and-feed-hopper silhouette.

## Scope discipline

- Do not modify other repositories.
- Do not commit Valheim game assemblies, BepInEx references, Steam content,
  `bin/`, `obj/`, or generated release archives.
- Do not introduce ServerSync, ILRepack, ConfigSync, or a custom network
  protocol.
- Update this file when compatibility or multiplayer ownership rules change.
