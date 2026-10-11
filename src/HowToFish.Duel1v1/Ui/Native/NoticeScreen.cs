using HowToFish.Duel1v1.Core;
using TMPro;
using UnityEngine;

namespace HowToFish.Duel1v1.Ui.Native;

// Messages ponctuels (adversaire sans le mod, config invalide…) pendant tout le temps passé en combat.
internal sealed class NoticeScreen : NativeScreen
{
    private TextMeshProUGUI? _label;

    protected override bool ShouldShow(CombatScreen screen) => Time.time <= DuelSession.NoticeUntil && DuelSession.Notice.Length > 0;

    protected override string Key() => "notice";

    protected override void Build(RectTransform layer)
    {
        _label = GameUiKit.Label(layer, "", 28, GameInfo.OrangeColor);
        var rect = _label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(1400, 50);
        rect.anchoredPosition = new Vector2(0, -180);
    }

    protected override void Tick()
    {
        if (_label != null) _label.text = DuelSession.Notice;
    }
}
