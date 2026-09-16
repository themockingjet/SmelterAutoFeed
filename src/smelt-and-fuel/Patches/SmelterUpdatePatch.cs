using HarmonyLib;

namespace SmeltAndFuel;

[HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
internal static class SmelterUpdatePatch
{
    [HarmonyPrefix]
    private static void Prefix(Smelter __instance)
    {
        UnlimitedFuelService.TryMaintainFuel(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(Smelter __instance)
    {
        AutoFeedService.QueueFeed(__instance);
    }
}
