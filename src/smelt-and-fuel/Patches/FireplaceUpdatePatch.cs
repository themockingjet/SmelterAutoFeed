using HarmonyLib;

namespace SmeltAndFuel;

[HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
internal static class FireplaceUpdatePatch
{
    [HarmonyPrefix]
    private static void Prefix(Fireplace __instance)
    {
        UnlimitedFuelService.TryMaintainFuel(__instance);
    }

    [HarmonyPostfix]
    private static void Postfix(Fireplace __instance)
    {
        AutoFeedService.QueueRefuel(__instance);
        UnlimitedFuelService.TryMaintainFuelAfterUpdate(__instance);
    }
}
