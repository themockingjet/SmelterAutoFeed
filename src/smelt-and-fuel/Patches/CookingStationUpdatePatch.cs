using HarmonyLib;

namespace SmeltAndFuel;

[HarmonyPatch(typeof(CookingStation), "UpdateCooking")]
internal static class CookingStationUpdatePatch
{
    [HarmonyPrefix]
    private static void Prefix(CookingStation __instance)
    {
        if (NetworkAuthority.IsOwner(__instance))
        {
            UnlimitedFuelService.TryMaintainFuel(__instance);
        }
    }
}
