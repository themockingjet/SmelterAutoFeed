using HarmonyLib;

namespace SmeltAndFuel;

[HarmonyPatch]
internal static class ContainerLifecyclePatch
{
    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    [HarmonyPostfix]
    private static void AwakePostfix(Container __instance)
    {
        ContainerDiscovery.Register(__instance);
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Load))]
    [HarmonyPostfix]
    private static void LoadPostfix(Container __instance)
    {
        ContainerDiscovery.Register(__instance);
    }

    [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
    [HarmonyPostfix]
    private static void DestroyedPostfix(Container __instance)
    {
        ContainerDiscovery.Unregister(__instance);
    }
}
