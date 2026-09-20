---
name: valheim-harmony-research
description: Research a Valheim game API target and implement or review a BepInEx 5 Harmony patch. Use when a task names a game class, method, patch, prefix, postfix, transpiler, or plugin lifecycle hook; do not use for reference-cache maintenance, release packaging, or multiplayer authority decisions.
---

# Valheim Harmony Research and Patching

## Preconditions

Read the target mod's `.github/copilot-instructions.md`, architecture and
compatibility documentation, and existing patch code before editing. Those
files override this skill.

Before writing a patch, establish all of the following from source,
documentation, or verified local assembly evidence:

1. Target assembly name.
2. Full target type name.
3. Exact target method name and parameter types.
4. Whether the method is static or instance.
5. Patch intent: observe, reject, alter arguments, alter a return value, or
   replace behavior.
6. The smallest patch type that implements that intent.

Do not infer an overload from its name. Do not use an unverified string target,
invent a game API, or write code that assumes an undocumented side effect.

## Assembly-search boundary

This skill does not provide an assembly browser, decompiler, index, or MCP
tool. `rg` cannot search compiled DLL metadata.

If the required type or method cannot be verified from checked-in source or a
local inspection tool already available to the developer:

1. Do not create the patch.
2. State the unresolved assembly, type, method, and parameter signature.
3. Request or perform a focused local assembly inspection using a separately
   installed read-only tool.
4. Resume only after recording the confirmed signature in the task context or
   the target mod's appropriate documentation.

Do not download or publish game assemblies to perform research.

## Patch selection rules

| Intent | Required patch choice |
| --- | --- |
| Observe completed behavior | Postfix |
| Validate before native code runs | Prefix that returns `true` unless a documented rejection is required |
| Change method arguments | Prefix using only confirmed argument names/types |
| Change a computed return | Postfix using `ref __result` only for a confirmed return type |
| Replace native behavior | Prefix returning `false`; require explicit justification and complete preservation of required native flows |
| Change an IL-only behavior | Transpiler only after a prefix/postfix cannot work and the exact IL anchor is verified |

Prefer a typed `[HarmonyPatch(typeof(TargetType), nameof(TargetType.Method))]`
declaration when the target is compile-time accessible. Use explicit parameter
type arrays only to disambiguate a confirmed overload. Avoid string-based
targets unless the type cannot be referenced after publicizing and the exact
signature has been verified.

## BepInEx and Harmony conventions

- Preserve the target mod's plugin GUID, assembly name, namespace, configuration
  keys, and lifecycle hooks.
- Keep patch classes in the mod's established patch location and naming scheme.
- Make a patch narrow: one native responsibility per patch.
- Use `__instance`, `__result`, and Harmony argument injection only when their
  types and names are confirmed.
- Do not catch broad exceptions around a patch. Let the mod's established
  logging/error pattern surface failures with target context.
- Do not add a transpiler to avoid understanding native behavior.
- Do not patch a method solely because its name appears related; verify its
  callers and side effects first.

## Required validation

1. Build with the target mod's existing build entry point.
2. Confirm the patch compiles against the current local reference cache.
3. Exercise the native action that reaches the patch in the appropriate test
   environment.
4. Confirm the expected behavior and at least one unchanged neighboring native
   behavior.
5. Document any version-sensitive target signature in the mod's compatibility
   notes when the patch depends on it.
