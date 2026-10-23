using System;
using TMPro;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace ReviveRegen;

/// <summary>
/// Shows the refill under the game's revive counter: "+1 in 0:42" while a revive is being earned back,
/// and "+1 Revive!" for a moment when one arrives.
/// The game's own refill ring only shows at 0 revives (its animator hides it otherwise), so without this
/// there's no sign that revives are coming back. Reads the same synced values as the regen, so players who
/// aren't the host see it too (if they have the mod).
/// </summary>
public class RefillTimer : MonoBehaviour
{
    public RefillTimer(IntPtr ptr) : base(ptr) { }

    private ReviveManager manager;
    private ReviveManagerDisplay display;
    private TextMeshProUGUI label;
    private int lastCount = -1;
    private float flashUntil, nextFind, nextErrorLog;

    private static bool Alive(UObject o) => o is not null && !o.WasCollected && o != null;

    private void Update()
    {
        try { Tick(); }
        catch (Exception ex)
        {
            if (Time.unscaledTime < nextErrorLog) return;
            nextErrorLog = Time.unscaledTime + 10f;
            Plugin.Log.LogError($"Refill timer: {ex}");
        }
    }

    private void Tick()
    {
        if (!Plugin.ShowRefillTimer.Value || !Plugin.Enabled.Value) { Hide(); return; }

        if ((!Alive(manager) || !Alive(display)) && Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + 1f;
            manager = UObject.FindObjectOfType<ReviveManager>();
            display = UObject.FindObjectOfType<ReviveManagerDisplay>();
            if (!Alive(label)) label = null;
            lastCount = -1;
        }
        if (!Alive(manager) || !Alive(display)) { Hide(); return; }

        var amount = display.reviveAmountText;
        if (!Alive(amount)) { Hide(); return; }
        if (label == null) Build(amount);
        if (label == null) return;

        var current = manager.CurrentRevives;
        var start = manager.earningTokenStartTime;
        var duration = manager.timeUntilReviveTokenEarned;
        var clock = manager.networkTimeGetter;
        if (current == null || start == null || duration == null || clock == null) { Hide(); return; }

        int count = current.Value;
        if (lastCount >= 0 && count > lastCount) flashUntil = Time.unscaledTime + 2.5f;
        lastCount = count;

        // Sit just under the revive number (it moves with the HUD).
        var at = amount.rectTransform;
        float down = at.rect.height * at.lossyScale.y * 0.95f;
        label.rectTransform.position = at.position - new Vector3(0f, down, 0f);

        if (Time.unscaledTime < flashUntil)
        {
            Show("<color=#7CFC7C>+1 Revive!</color>");
            return;
        }

        int limit = RegenPatches.Limit;
        if (limit <= 0 || count >= limit || Raw.TryGetNullable4(manager, "pauseStartTime", out _)) { Hide(); return; }
        float left = duration.Value - (clock.ServerTime - start.Value);
        if (left <= 0f || left > duration.Value + 1f) { Hide(); return; } // not counting (e.g. host without the mod)
        int s = Mathf.CeilToInt(left);
        bool boosted = Time.unscaledTime - RegenPatches.LastBoostAt < 0.6f; // flash when damage sped it up
        Show(boosted ? $"<color=#FF9A3C>+1 in {s / 60}:{s % 60:00}</color>" : $"+1 in {s / 60}:{s % 60:00}");
    }

    private void Build(TMP_Text amount)
    {
        var go = new GameObject("ReviveRegen_Timer");
        var rt = go.AddComponent<RectTransform>();
        go.transform.SetParent(display.transform, false);
        rt.sizeDelta = new Vector2(240f, 40f);
        label = go.AddComponent<TextMeshProUGUI>();
        label.font = amount.font;
        label.fontSize = Mathf.Max(14f, amount.fontSize * 0.5f);
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.richText = true;
        label.raycastTarget = false;
        label.color = new Color(1f, 0.85f, 0.35f);
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(0, 0, 0, 200);
        label.text = "";
    }

    private void Show(string text)
    {
        if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
        if (label.text != text) label.text = text;
    }

    private void Hide()
    {
        if (Alive(label) && label.gameObject.activeSelf) label.gameObject.SetActive(false);
    }
}
