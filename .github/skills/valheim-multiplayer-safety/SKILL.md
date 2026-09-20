---
name: valheim-multiplayer-safety
description: Design, implement, or review a Valheim mod change that reads or mutates networked gameplay state. Use for ownership, ZNetView, ZDO, RPC, inventory, container access, drops, destruction, durability, or synchronized configuration; do not use for isolated client-only presentation changes.
---

# Valheim Multiplayer Safety

## Mandatory authority classification

Before editing, classify the requested behavior as exactly one of:

| Classification | Meaning | Required implementation boundary |
| --- | --- | --- |
| Client-only | No networked game state changes | Do not write ZDO data, invoke gameplay RPCs, or change authoritative inventories |
| Server-authoritative | The dedicated server decides and applies state | Clients request; server validates and mutates |
| Owner-authoritative | The valid `ZNetView` owner applies state | Non-owners do not mutate; use the native request/RPC path |
| Shared synchronized configuration | Settings must agree between peers | Use the target mod's established ServerSync/ConfigSync integration |

If the requested behavior cannot be classified from the target mod's
documentation and existing code, stop before adding mutation code and resolve
the authority model.

## Prohibited shortcuts

Do not:

- claim remote `ZNetView` ownership;
- mutate a networked object because it exists locally;
- bypass `Container.CheckAccess`, in-use checks, inventory APIs, or native
  interaction validation;
- write direct ZDO or serialized state when a native API or RPC exists;
- invent a new RPC, synchronization protocol, or ServerSync dependency without
  an explicit mod requirement;
- apply a client-side cosmetic result as authoritative gameplay state.

## Required mutation sequence

For every networked state change:

1. Identify the authoritative actor from the classification table.
2. Verify ownership and native access preconditions before reading mutable
   state.
3. Use the native API to consume, add, remove, damage, destroy, spawn, or
   update state.
4. Use the existing native RPC flow when a non-authoritative peer must request
   the action.
5. Preserve related native flows: inventory changes, drops, destruction,
   durability, skills, fuel, crafting, and interaction effects as applicable.
6. Make the operation idempotent or ensure the authoritative path prevents
   duplicate application.

## Configuration synchronization

Only use ServerSync/ConfigSync when the mod requires synchronized settings.
When the mod already uses it:

1. Preserve existing setting names, defaults, ranges, lock behavior, and
   version-handshake requirements.
2. Register new synchronized settings using the mod's existing configuration
   pattern.
3. Document which settings synchronize and which peer is authoritative.

When the mod does not use it, do not add ServerSync, ConfigSync, ILRepack, or a
custom protocol solely for convenience.

## Required multiplayer validation

1. Test or reason through the action as the authority and as a non-authority.
2. Verify denied access and invalid ownership paths perform no mutation.
3. Verify a valid action applies exactly once.
4. Verify native side effects remain intact.
5. Update the target mod's compatibility or architecture documentation with
   the authority classification and any synchronization requirement.
