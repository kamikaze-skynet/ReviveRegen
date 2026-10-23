using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ReviveRegen;

/// <summary>
/// Revives regenerate back up to the starting number over time.
///
/// Vanilla: when the shared revive pool is empty, a timer runs and earns one revive back (each earned revive makes
/// the next timer 10s longer). This mod uses that same timer, progress ring and networking, but keeps earning
/// revives until the pool is back to the number the run started with.
///
/// Only the host needs this mod: the host adds the revives and the count syncs to everyone.
/// Players who also have it see the progress ring while revives refill.
/// </summary>
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "girthy_mods.ReviveRegen";
    public const string Name = "ReviveRegen";
    public const string Version = "1.0.0";

    internal static new ManualLogSource Log;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<float> SecondsPerRevive;
    internal static ConfigEntry<float> ExtraSecondsAfterEachRevive;
    internal static ConfigEntry<int> ExtraAboveStart;

    public override void Load()
    {
        Log = base.Log;

        Enabled = Config.Bind("Regeneration", "Enabled", true,
            "Revives refill one at a time until you're back to the number the run started with. Off = vanilla (only earns one back when you have none).");
        SecondsPerRevive = Config.Bind("Regeneration", "SecondsPerRevive", 0f,
            new ConfigDescription("Seconds to earn each revive back. 0 = use the game's own timer.",
                new AcceptableValueRange<float>(0f, 900f)));
        ExtraSecondsAfterEachRevive = Config.Bind("Regeneration", "ExtraSecondsAfterEachRevive", 10f,
            new ConfigDescription("After each revive earned back, the next one takes this much longer (vanilla: 10). 0 = always the same time.",
                new AcceptableValueRange<float>(0f, 300f)));
        ExtraAboveStart = Config.Bind("Regeneration", "ExtraAboveStart", 0,
            new ConfigDescription("Let revives refill this many above the starting number. 0 = stop at the starting number.",
                new AcceptableValueRange<int>(0, 10)));

        new Harmony(Guid).PatchAll(typeof(RegenPatches));
        Log.LogInfo($"{Name} {Version} ready.");
    }
}
