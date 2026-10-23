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

    /// <summary>Most revives the pool refills to (0 = unknown yet).</summary>
    internal static int Limit => startingTotal <= 0 ? 0 : startingTotal + System.Math.Max(0, Plugin.ExtraAboveStart.Value);
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

                // The pool just dropped below full (vanilla only starts the timer when a revive takes it to 0):
                // start counting now. Also covers the count being set from outside (e.g. RunSaves restoring a run),
                // where the old start time would otherwise hand a revive back instantly.
                if (lastCount >= limit && count < limit) start.Value = now;
                lastCount = count;
            }

            if (count >= limit) return false;                    // full: nothing to earn
            if (Raw.TryGetNullable4(__instance, "pauseStartTime", out _)) return false; // game paused (float? field, read raw)

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

    // ---------- taking damage speeds up the refill (host) ----------

    internal static float LastBoostAt = -100f; // for the refill timer's flash
    private static ReviveManager cachedManager;

    // Each player's own game reports damage it took to the host with this RPC; the host runs it (execute stage).
    [HarmonyPatch(typeof(PlayerHealthContainer), nameof(PlayerHealthContainer.NotifyDamageTakenOnServerRpc))]
    [HarmonyPrefix]
    private static void DamageTaken_Prefix(PlayerHealthContainer __instance, float amount)
    {
        try
        {
            if (!Plugin.Enabled.Value || !Plugin.DamageSpeedsUpRecharge.Value || amount <= 0f || startingTotal <= 0) return;
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) return;
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer) return;

            if (cachedManager is null || cachedManager.WasCollected || cachedManager == null)
                cachedManager = UnityEngine.Object.FindObjectOfType<ReviveManager>();
            var rm = cachedManager;
            if (rm == null || rm.CurrentRevives == null || rm.earningTokenStartTime == null || rm.timeUntilReviveTokenEarned == null) return;

            int limit = Limit;
            if (rm.CurrentRevives.Value >= limit) return;                      // nothing to refill
            if (Raw.TryGetNullable4(rm, "pauseStartTime", out _)) return;      // paused

            float max = __instance.MaxHealth;
            if (max <= 0f) return;
            float percent = Math.Min(amount, max) / max * 100f;
            float seconds = percent * Plugin.SecondsPerPercentHealthLost.Value;
            if (seconds <= 0f) return;

            // Move the timer's start back: same as having waited that much longer.
            rm.earningTokenStartTime.Value = rm.earningTokenStartTime.Value - seconds;
            LastBoostAt = UnityEngine.Time.unscaledTime;
        }
        catch (Exception ex) { Plugin.Log.LogError($"Damage speeds up recharge: {ex.Message}"); }
    }
}
