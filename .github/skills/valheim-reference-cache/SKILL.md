---
name: valheim-reference-cache
description: Set up, verify, checksum, or refresh the local Valheim dedicated-server assemblies and BepInEx or ServerSync build references. Use only for local reference-cache maintenance, not mod source, Harmony patch, multiplayer, or release work.
---

# Valheim Reference Cache

## Scope

Use this skill only when the task concerns the local build-reference cache.
Do not use it to modify a mod, create a patch, package a mod, or deploy a
release.

The cache is outside all mod repositories:

```text
$VALHEIM_DEV_ROOT/managed/       Valheim and Unity managed DLLs
$VALHEIM_DEV_ROOT/BepInEx/core/  BepInEx, Harmony, and ServerSync DLLs
$VALHEIM_DEV_ROOT/metadata/      Local reference metadata and checksums
```

Never add cache files, Steam content, or game DLLs to version control.

## Required environment

The setup Makefile uses these defaults:

```text
VALHEIM_PATH=$HOME/valheim-server
VALHEIM_DEV_ROOT=$HOME/valheim-dev
VALHEIM_MANAGED_PATH=$VALHEIM_DEV_ROOT/managed
BEPINEX_PATH=$VALHEIM_DEV_ROOT/BepInEx
SERVERSYNC_PATH=$BEPINEX_PATH/core/ServerSync.dll
CONFIGSYNC_PATH=$BEPINEX_PATH/core/ServerSync.dll
```

For direct use of those variables, load the generated environment file:

```bash
source "$HOME/.config/valheim-dev/env.sh"
```

## Exact maintenance actions

Run commands from `valheim-mod-setup` or use `make -C` with that directory.

| Need | Command | Effect |
| --- | --- | --- |
| Check references | `make verify` | Fails if any required DLL is missing |
| Initial full setup | `make setup` | Installs prerequisites, downloads the server and dependencies, writes the environment file, then verifies |
| Refresh Valheim assemblies | `make update` | Updates the dedicated server, synchronizes managed assemblies, then verifies |
| Refresh BepInEx | `make update-bepinex` | Replaces BepInEx core files and verifies BepInEx |
| Refresh ServerSync/ConfigSync | `make update-serversync` | Replaces `ServerSync.dll` and verifies all references |
| Refresh both dependencies | `make update-dependencies` | Refreshes BepInEx and ServerSync/ConfigSync, then verifies |
| Record hashes | `make checksums` | Writes reference hashes to `$VALHEIM_DEV_ROOT/metadata/checksums.txt` |

`make setup` and `make update` contact Steam and can download or replace local
files. `make setup` also runs privileged package installation commands. Do not
run either command merely because a mod build fails; first run `make verify`
and select the smallest repair action that fixes the reported missing
reference.

To use a non-default server or cache, pass an absolute WSL path:

```bash
make verify VALHEIM_PATH="/path/to/server" VALHEIM_DEV_ROOT="/path/to/cache"
```

## Completion requirements

1. Run `make verify` after every update action.
2. Report the exact command run and whether verification passed.
3. Do not copy cache DLLs into a source tree, release directory, or package.
