using System;
using HarmonyLib;
using Unity.Netcode;

namespace ReviveRegen;

/// <summary>
/// Replaces ReviveManager.HandleReviveTokenEarning (runs every frame on every player's game).
///
/// Vanilla logic, from the game's code:
///   if (CurrentRevives > 0 || paused) return;
///   progress = (ServerTime - earningTokenStartTime) / timeUntilReviveTokenEarned;
///   OnReviveTokenEarningChanged(progress);                       // progress ring, on everyone
///   if (owner/host && progress >= 1) { timeUntilReviveTokenEarned += 10; CurrentRevives += 1; }
/// UseReviveRpc (host) sets earningTokenStartTime = ServerTime when the pool hits 0.
///
/// This version earns while CurrentRevives is below the starting total (plus ExtraAboveStart), restarts the
/// timer after each earned revive, and starts it when the pool first drops below full.
/// All values live in the game's own NetworkVariables, so every player's ring and counter stay in step.
/// </summary>
[HarmonyPatch]
internal static class RegenPatches
{
    private const float VanillaEscalation = 10f; // timeUntilReviveTokenEarned += 10 after each earned revive

    private static int startingTotal;      // from InitializeOnClientsRpc(totalRevives), on host and clients
    private static int lastCount = -1;     // host: previous CurrentRevives, to notice the pool dropping below full
    private static IntPtr durationSetFor;  // host: ReviveManager we've applied SecondsPerRevive to

    // ---------- starting total (host + clients) ----------

    [HarmonyPatch(typeof(ReviveManager), nameof(ReviveManager.InitializeOnClientsRpc))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    private static void InitClients_Prefix(ReviveManager __instance, int totalRevives)
    {
        // Runs for both the send and the execute stage; the value is the same. A later call (e.g. MoreWizards
        // adjusting the pool for big parties) overwrites it, which is what we want.
        if (totalRevives <= 0) return;
        startingTotal = totalRevives;
        lastCount = -1;
        Plugin.Log.LogInfo($"Starting revives: {startingTotal}.");
    }

    [HarmonyPatch(typeof(ReviveManager), nameof(ReviveManager.OnNetworkSpawn))]
    [HarmonyPostfix]
    private static void Spawn()
    {
        startingTotal = 0;
        lastCount = -1;
        durationSetFor = IntPtr.Zero;
    }

    // ---------- the earning loop ----------

    [HarmonyPatch(typeof(ReviveManager), nameof(ReviveManager.HandleReviveTokenEarning))]
    [HarmonyPrefix]
    private static bool Earning_Prefix(ReviveManager __instance)
    {
        if (!Plugin.Enabled.Value || startingTotal <= 0) return true; // vanilla
        try
        {
            var current = __instance.CurrentRevives;
            var start = __instance.earningTokenStartTime;
            var duration = __instance.timeUntilReviveTokenEarned;
            var clock = __instance.networkTimeGetter;
            if (current == null || start == null || duration == null || clock == null) return true;

            bool host = __instance.IsOwner; // the ReviveManager belongs to the host, which owns the timer values
            int count = current.Value;
            int limit = startingTotal + Math.Max(0, Plugin.ExtraAboveStart.Value);
            float now = clock.ServerTime;

            if (host)
            {
                // A fixed time per revive, if configured (applied once per run).
                if (Plugin.SecondsPerRevive.Value > 0f && durationSetFor != __instance.Pointer)
                {
                    durationSetFor = __instance.Pointer;
                    duration.Value = Plugin.SecondsPerRevive.Value;
                }

                // The pool just dropped below full (vanilla only starts the timer at 0): start counting now.
                if (lastCount >= limit && count < limit && count > 0) start.Value = now;
                lastCount = count;
            }

            if (count >= limit) return false;                    // full: nothing to earn
            if (__instance.pauseStartTime.HasValue) return false; // game paused

            float progress = (now - start.Value) / Math.Max(0.01f, duration.Value);
            __instance.OnReviveTokenEarningChanged?.Invoke(progress); // the game's progress ring

            if (!host || progress < 1f) return false;

            // Earn one revive (same as vanilla), then restart the timer if there's more to refill.
            duration.Value = duration.Value + Plugin.ExtraSecondsAfterEachRevive.Value;
            current.Value = count + 1;
            lastCount = count + 1;
            if (count + 1 < limit) start.Value = now;
            Plugin.Log.LogInfo($"Revive earned back: {count + 1}/{limit}. Next takes {duration.Value:0}s.");
            return false;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Revive regen: {ex}");
            return true; // fall back to vanilla
        }
    }
}
