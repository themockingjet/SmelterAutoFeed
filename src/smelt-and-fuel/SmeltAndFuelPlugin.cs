using BepInEx;
using HarmonyLib;

namespace SmeltAndFuel;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class SmeltAndFuelPlugin : BaseUnityPlugin
{
    internal const string PluginGuid = "str.smeltandfuel";
    private const string PluginName = "SmeltAndFuel";
    private const string PluginVersion = "0.6.3";

    private void Awake()
    {
        AutoFeedSettings settings = AutoFeedSettings.Create(Config);
        PlayerPresenceService presence = new(settings);
        AutoFeedService.Configure(
            settings,
            presence,
            exception => Logger.LogError(exception),
            message => Logger.LogInfo(message));
        UnlimitedFuelService.Configure(settings, presence);
        new Harmony(PluginGuid).PatchAll();
    }

    private void Update()
    {
        AutoFeedService.ProcessPending();
    }
}
