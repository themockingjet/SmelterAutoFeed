---
name: valheim-mod-release
description: Build, package, deploy, remove, or validate a Valheim mod release using its existing repository tooling. Use for release artifacts, Thunderstore metadata, test-server deployment, and release verification; do not use for gameplay implementation, Harmony target research, or reference-cache setup.
---

# Valheim Mod Build, Package, Deploy, and Release

## Scope and precedence

Read the target mod's README, `.github/copilot-instructions.md`, release
scripts, and Thunderstore metadata before running a command. The target mod's
documented commands and exact archive-content rules override this skill.

Do not add a second build system. Do not assume every mod has a Makefile or
that every checked-in script is independently runnable; inspect the repository's
documented entry point and prerequisites first.

## Build and package sequence

Use this exact order whenever the target repository supplies corresponding
commands:

1. Run its preflight or build validation command.
2. Run its Release build command.
3. Run its package command.
4. Run its release-verification command against the generated ZIP.

The starter repository includes these script entry points:

```bash
scripts/build.sh
scripts/package.sh
scripts/verify-release.sh
```

They require the build validation targets and dependencies defined by that
repository. If the validation target or prerequisite is missing, report the
specific missing target or tool; do not bypass validation with a hand-written
`dotnet build` command.

Build against local references only:

```bash
source "$HOME/.config/valheim-dev/env.sh"
```

If the reference paths are missing, use the `valheim-reference-cache` skill;
do not copy DLLs into the mod repository.

## Required release archive contents

Validate the produced ZIP, not just the staging directory. It must include the
target mod's required root-level Thunderstore metadata:

```text
manifest.json
README.md
CHANGELOG.md
icon.png
```

It must contain the built plugin DLL and only runtime DLLs explicitly approved
by the target mod's release process. It must not contain:

```text
assembly_*.dll
UnityEngine*.dll
BepInEx.dll
0Harmony.dll
```

Do not publish a placeholder icon, unverified DLL, local reference assembly,
Steam content, `bin/`, `obj/`, or generated archive unless the target
repository explicitly tracks it.

## Metadata and documentation gates

Before packaging a behavior or version change:

1. Set the intended version in `Thunderstore/manifest.json`.
2. Update `Thunderstore/CHANGELOG.md` in the target repository's established
   format, with user-visible behavior and compatibility changes.
3. Update the README, architecture, and compatibility documentation when
   behavior, configuration, multiplayer authority, or dependencies change.
4. Confirm the icon meets the target mod's required size and non-placeholder
   policy.

## Test-server deployment

Use the repository's deployment script only after release verification passes.
For repositories based on the template:

```bash
TEST_SERVER=local scripts/deploy-test-server.sh
TEST_SERVER=user@host scripts/deploy-test-server.sh
TEST_SERVER=local scripts/deploy-test-server.sh --remove
```

The deployment target must use a `local-` plugin directory unless
`TEST_ALLOW_NONLOCAL_PLUGIN_DIR=1` is explicitly supplied. Do not enable that
override by default. The deployment script installs or removes the release DLLs
only; it does not restart the server or change its maintenance timer.

## Completion requirements

1. Record the exact build, package, verification, and deployment commands run.
2. Confirm release verification passed after the final archive was generated.
3. Confirm the deployed test plugin directory is the intended `local-` path.
4. Do not claim a release is publishable if a required validation command did
   not run or failed.
