# SmeltAndFuel

SmeltAndFuel is an independently implemented BepInEx 5 mod for current Valheim that automatically moves compatible inputs and fuel from nearby containers into production stations and selected fireplaces.

## Why existing smelters work

SmeltAndFuel does not cache smelters or containers when they are built. Instead, it patches Valheim's `Smelter.UpdateSmelter` method. Every loaded smelter therefore participates as soon as its normal update runs, including smelters that existed before the mod was installed or before a player entered the area.

The loaded-container snapshot is refreshed on a configurable interval, so containers and eligible fireplaces are also discovered without construction or rebuilding.

## Multiplayer and safety

- Transfers run only on the network owner of the production station or fireplace.
- A source container must also be locally owned, not in use, and pass Valheim's native `Container.CheckAccess` check.
- SmeltAndFuel removes exactly one item, then invokes Valheim's native RPC. A feed pass may transfer one fuel item and one input item.
- The mod uses the smelter's native `FindCookableItem`, `GetQueueSize`, `GetFuel`, and RPC paths; it does not maintain a separate conversion table or queue.
- Fireplace fuel is read from its authoritative Valheim ZDO state and added through its owner-checked native `RPC_AddFuel` endpoint.

SmeltAndFuel can run on player clients, on a dedicated server, or on both. It
uses native Valheim ownership: only the current owner of a target may mutate
it. When the server is configured with a server-ownership provider, server-side
automation can centralize active targets. `Server Automation > Require Nearby
Player` prevents server-side feeding when no player is near the target.
Participating peers should use the same mod version and matching gameplay
configuration values because this mod does not synchronize configuration
between clients.

When using MultiUserChest, AzuCraftyBoxes, or AzuAutoStore, source inventories
can change between discovery and removal. SmeltAndFuel rechecks ownership,
access, and in-use state and sends a station RPC only after native item
removal succeeds. Failed concurrent removals are retried using the normal
backoff rather than being forced.

## Configuration

The generated BepInEx configuration file has these settings:

| Setting | Default | Meaning |
| --- | ---: | --- |
| `Container Search Radius` | 10 | Maximum distance in meters from a production station to a source container. |
| `Feed Interval` | 0.5 | Minimum seconds between automatic feed passes for an individual station or fireplace. |
| `Container Refresh Interval` | 2 | Seconds between loaded-container discovery passes. |
| `Performance Logging` | `false` | The **Diagnostics** section enables rate-limited performance counters for troubleshooting. Leave disabled during normal play. |
| `Require Nearby Player` | `true` | The **Server Automation** section prevents dedicated-server feeding when no connected player is within the presence radius. It does not transfer ownership. |
| `Presence Radius` | `64` | Maximum distance in meters from a target to a connected player for dedicated-server automation. |
| `Feed Ore / Inputs` | `true` | Enables input-queue feeding for every enabled station. This includes ore, kiln wood, grain, flax, and refinery ingredients. |
| `Feed Fuel` | `true` | Enables fuel-slot feeding for every enabled station. The Frigid Kiln uses this setting to consume Ice. |
| `Unlimited Fuel` | `false` | Maintains enabled supported targets at full fuel without consuming fuel items or triggering the normal refuel RPC/effects. Fireplace hover and interaction remain available. The Frigid Kiln is excluded so that it always consumes Ice. Input feeding remains enabled. |
| `Leave Last Item` | `true` | Keeps one matching item for each prefab across all eligible nearby containers. Ground items are always eligible for consumption. |
| `Use Ground Items` | `false` | Allows compatible dropped items on the ground to be used as an automatic source. When `Leave Last Item` is enabled, matching ground drops are preferred so a final matching container item remains protected. |
| `Ground Item Range` | `5` | Maximum distance in meters from a station or fireplace to a dropped ground item. |
| `Fuel Priority` | `Wood` | Comma-separated fireplace fuel prefab names, evaluated left to right. Production stations always use their native fuel. |
| `Fuel Disallow Types` | `RoundLog,FineWood` | Comma-separated fuel prefab names never used by automatic refueling. This always overrides `Fuel Priority`. |
| Production station names | `true` | The **Production Stations** section contains enable switches for Smelters, Blast Furnaces, Charcoal Kilns, Windmills, Spinning Wheels, Eitr Refineries, Frigid Kilns, and Ovens. Oven applies to unlimited fuel only. |
| `<Station> - Ore / Input Target` | `100` | The **Production Targets** section contains this input target for every supported station. |
| `<Station> - Fuel Target` | `100` | The **Production Targets** section contains this fuel target for every supported station. |
| `Frigid Kiln - Ice Target` | `100` | Maximum Ice to keep in an enabled Frigid Kiln. This is its native fuel slot and is always consumed, including when `Unlimited Fuel` is enabled. |
| `Auto Empty Windmill Output` | `true` | Releases completed Barley Flour in batches through Valheim's native Windmill output action, preventing full output from stopping production. |
| `Windmill Output Batch Size` | `40` | Number of completed Flour to release together, from 1 to 40. Any smaller completed batch releases when the Windmill's Barley queue becomes empty. |
| `Fireplace Range` | `5` | Maximum distance in meters from a fireplace to a source container. |
| Fireplace category names | `true` | The **Fireplaces** section contains clean enable switches for Standing Torches, Braziers, Hot Tubs, Wall Torches, Fire Pits, Hearths, and Bonfires. |
| `Fireplace - Fuel Target` | `100` | Maximum fuel items to keep in each enabled fireplace. |

All input and fuel target values accept 1–100. SmeltAndFuel always caps a target to the station's native Valheim capacity and waits for room for one complete fuel item before refueling, preventing fractional fuel values from exceeding the configured cap.

The Frigid Kiln stores and consumes Ice through its native fuel slot to produce Liquid Frost. It is intentionally excluded from `Unlimited Fuel`, so Ice is always removed from eligible sources through Valheim's native fuel RPC.

Stone Oven unlimited fuel is independently controlled by `Production Stations > Oven`. It maintains the oven's native fuel without changing cooking slots or adding cooking ingredients.

The plug-in uses `str.smeltandfuel` as its BepInEx configuration identifier. Copy any preferred values from configuration files created before the GUID rename into the newly generated file. Version 0.4.2 and later automatically migrate the older CamelCase labels within an existing `str.smeltandfuel` configuration file.
