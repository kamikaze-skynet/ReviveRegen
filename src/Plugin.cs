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
/// Players who also have it see the refill timer under the revive counter.
/// </summary>
[BepInPlugin(Guid, Name, Version)]
public class Plugin : BasePlugin
{
    public const string Guid = "girthy_mods.ReviveRegen";
    public const string Name = "ReviveRegen";
    public const string Version = "1.1.1";

    internal static new ManualLogSource Log;

    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<float> SecondsPerRevive;
    internal static ConfigEntry<float> ExtraSecondsAfterEachRevive;
    internal static ConfigEntry<int> ExtraAboveStart;
    internal static ConfigEntry<bool> ShowRefillTimer;
    internal static ConfigEntry<bool> DamageSpeedsUpRecharge;
    internal static ConfigEntry<float> SecondsPerPercentHealthLost;

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

        DamageSpeedsUpRecharge = Config.Bind("Regeneration", "DamageSpeedsUpRecharge", true,
            "Taking damage makes the next revive come back sooner (any player in the party).");
        SecondsPerPercentHealthLost = Config.Bind("Regeneration", "SecondsPerPercentHealthLost", 1f,
            new ConfigDescription("Seconds taken off the refill timer for each 1% of a player's max health they lose. 1 = losing a full health bar saves 100 s.",
                new AcceptableValueRange<float>(0f, 10f)));
        ShowRefillTimer = Config.Bind("Display", "ShowRefillTimer", true,
            "Show \"+1 in 0:42\" under the revive counter while a revive is coming back, and \"+1 Revive!\" when it arrives. (The game's own ring only shows at 0 revives.)");

        new Harmony(Guid).PatchAll(typeof(RegenPatches));
        AddComponent<RefillTimer>();
        Log.LogInfo($"{Name} {Version} ready.");
    }
}
