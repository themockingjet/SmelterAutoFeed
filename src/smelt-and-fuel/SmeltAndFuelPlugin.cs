using BepInEx;
using HarmonyLib;

namespace SmeltAndFuel;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class SmeltAndFuelPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "str.smeltandfuel";
    private const string PluginName = "SmeltAndFuel";
    private const string PluginVersion = "0.6.2";

    private void Awake()
    {
        AutoFeedSettings settings = AutoFeedSettings.Create(Config);
        AutoFeedService.Configure(settings, exception => Logger.LogError(exception));
        UnlimitedFuelService.Configure(settings);
        new Harmony(PluginGuid).PatchAll();
    }

    private void Update()
    {
        AutoFeedService.ProcessPending();
    }
}
