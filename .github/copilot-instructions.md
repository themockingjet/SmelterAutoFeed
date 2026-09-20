# ValheimModName Agent Instructions

## Purpose

Describe what this mod does, who it is for, and which native Valheim
behaviour it extends.

## Implementation priorities

Approach feature work with decisive, inventive problem-solving. Explore
unconventional designs when they provide a clearer, more effective solution;
do not default to conservative implementations just because they are familiar.

Make decisions in this order:

1. Solve the user or gameplay problem completely and directly.
2. Optimize the resulting design for runtime performance, allocation behavior,
  and repeated game-loop work.
3. Evaluate compatibility last, adapting integrations where needed without
  weakening the feature or compromising the critical game rules below.

Keep production code current and intentional. Do not accumulate obsolete
paths, commented-out implementations, unused abstractions, or dead code. When
a feature supersedes code, remove the obsolete code and keep responsibilities
split across focused files rather than dumping implementation into one file.

## Compatibility

- Target Valheim version: current stable version used by the local reference cache.
- Loader: BepInEx 5.
- Runtime target: .NET Framework 4.8.
- Development SDK: .NET 8 or later.
- Build-time publicizer: `BepInEx.AssemblyPublicizer.MSBuild`.
- Config synchronization: ServerSync `ConfigSync`, when multiplayer settings
  require it.
- Multiplayer: document whether behaviour is client-side, server-side, or
  owner-authoritative.

## Critical game rules

- Only mutate networked objects through their valid ownership and RPC flows.
- Prefer native Valheim APIs over direct ZDO or serialized-state edits.
- Do not add hard-coded game data when the native API can discover it.
- Do not claim remote ownership without an explicit, approved ownership flow.

## Source organization

Treat the plugin entry point as an orchestration layer, not a feature
container. Do not implement a non-trivial mod in `<AssemblyName>Plugin.cs`.

- `<AssemblyName>Plugin.cs` may contain only plugin identity, BepInEx
  lifecycle methods, construction/initialization of collaborators, Harmony
  registration, cleanup, and minimal startup logging.
- Place `Config.Bind`, `ConfigSync` setup, configuration change handling, and
  typed configuration values in `Configuration/`.
- Place each Harmony patch type in `Patches/`; group only closely related
  lifecycle patches in one file.
- Place game decisions, native API coordination, discovery, caching, and
  mutable feature state in focused types under `Services/`.
- Place a feature-specific state or value type in its own file when it has
  independent lifecycle, serialization, or test concerns; otherwise keep it
  with its owning service.
- Prefer one primary class or patch type per source file. Do not place
  `[HarmonyPatch]` types or feature implementation in the plugin entry file.
- Create the required directories and concrete source files before filling in
  a non-trivial feature. Never create empty placeholder files.
- Before implementation, add the planned file-to-responsibility mapping to
  `docs/ARCHITECTURE.md`. Update it in the same change when the design evolves.
- If the plugin entry file grows beyond roughly 150 lines, stop and split its
  non-lifecycle responsibilities into the directories above before continuing.

## Build and release

- Source the shared environment:
  `source "$HOME/.config/valheim-dev/env.sh"`.
- Run `make preflight` before building or releasing.
- Run `make build`.
- Run `make package`.
- Run `make verify-release`.
- Keep `ServerSync.dll` outside the repository; release builds merge it into
  the plugin with ILRepack.
- Release ZIPs must contain the plugin DLL, runtime DLL dependencies,
  `manifest.json`, `README.md`, `CHANGELOG.md`, and `icon.png`.

## Changelog

- Keep `Thunderstore/CHANGELOG.md` in a Keep a Changelog-style format with
  the newest release first.
- Use an `[Unreleased]` section while work is in progress. When publishing,
  rename it to the exact version in `Thunderstore/manifest.json` and start a
  new `[Unreleased]` section.
- Record concise, user-visible changes under `Added`, `Changed`, `Fixed`, or
  `Removed`. Include compatibility changes, configuration migrations, and
  multiplayer behavior changes when they affect users; do not list routine
  internal refactors or build noise.
- If the mod uses ServerSync or ConfigSync, add a `ServerSync/ConfigSync`
  entry that explains which settings are synchronized, whether the version
  handshake or minimum required version changed, and whether ServerSync is a
  runtime dependency or merged into the release DLL. Do not imply that
  settings are synchronized when the mod has no such integration; omit the
  section when it is not applicable.
- Do not rewrite or remove previous release notes. Update the changelog in
  the same change that updates the manifest version or user-facing behavior.

## Icon generation

- Fill in `docs/ICON_BRIEF.md` with this mod's subject/palette before
  requesting an icon.
- Use the `icon-generator` agent (`.github/agents/icon-generator.md`)
  to assemble a brand-consistent prompt and generate `Thunderstore/icon.png`.
- The shared brand system lives in the `valheim-mod-brand` repo/folder;
  do not invent a different frame, palette set, or lighting scheme.
- The generated subject must pass the anti-sameness checklist in
  `valheim-mod-brand/ICON_SYSTEM.md` — no plain recolored placeholder
  shapes as final art.

## Scope discipline

- Do not modify other repositories.
- Do not commit Valheim game assemblies or Steam content.
- Do not publish a ZIP containing a placeholder or unverified DLL.
- Update this file when compatibility or multiplayer rules change.