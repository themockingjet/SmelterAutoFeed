# SmeltAndFuel

SmeltAndFuel is a BepInEx 5 Valheim mod that automatically feeds compatible
inputs and fuel from nearby accessible containers into production stations and
selected fireplaces. It uses Valheim's native discovery, inventory, ownership,
and RPC behavior rather than maintaining its own game data or production state.

The detailed player-facing behavior and configuration reference is in
[Thunderstore/README.md](Thunderstore/README.md). See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and
[docs/COMPATIBILITY.md](docs/COMPATIBILITY.md) before changing runtime code.

## Development

The project targets .NET Framework 4.8 and uses the shared Valheim reference
environment. Load that environment, then run the standard validation and
release sequence:

Make targets automatically load `$HOME/.config/valheim-dev/env.sh` when it
exists. Source that file manually only when using the variables from direct
shell commands outside Make.

```bash
make preflight
make build
make package
make verify-release
```

The package is written to `release/SmeltAndFuel-0.5.13.zip` and contains only
the Thunderstore metadata files and `SmeltAndFuel.dll` at the ZIP root.

## Deploy to a test server

Build, verify, and install the local package into the active BepInEx release:

```bash
make deploy-test-server TEST_SERVER=local
```

For an SSH test server, use `TEST_SERVER=user@host`. The helper installs the
root-level DLLs into a separate `local-*` directory under
`/opt/valheim/modpack/current/BepInEx/plugins`, preserves the server and
maintenance-timer state without starting or stopping either one, and leaves
the managed Hexium manifest unchanged.
Use `TEST_SERVER_SSH_OPTIONS="-p 2222"` for a non-default SSH port. Remove
the temporary install with:

```bash
make remove-test-server TEST_SERVER=local
```

The installer never calls `systemctl`. To batch-install several mods and
restart once, stop the server and timer yourself, run this command from each
mod repository, then start them once:

```bash
sudo systemctl stop valheim-restart.timer
sudo systemctl stop valheim.service
make deploy-test-server TEST_SERVER=local
# Repeat from each mod repository.
sudo systemctl start valheim.service
sudo systemctl start valheim-restart.timer
```

Set `TEST_SERVER_SUDO=` when running directly as root. Use
`TEST_PLUGIN_DIR=local-OtherName` to keep multiple local builds separate.

If the shared references are not installed yet:

```bash
make setup-references
source "$HOME/.config/valheim-dev/env.sh"
```

## Project layout

- `src/smelt-and-fuel/`: plugin source and SDK-style project.
- `Thunderstore/`: package metadata, player README, changelog, and icon.
- `docs/`: architecture, compatibility, and icon documentation.
- `scripts/`: reference setup, build, package, and release verification.
- `release/`: generated local build and package output; do not commit it.

## Generated and private files

Do not commit Valheim game DLLs, BepInEx reference DLLs, `bin/`, `obj/`, or
generated release archives. The local reference cache belongs under
`$HOME/valheim-dev`, not in this repository.
