using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeStatsPanel : MonoBehaviour
{
    static readonly Color Accent = new Color(0f, 0.85f, 1f);

    TMP_Text _labels;
    TMP_Text _values;

    public static UpgradeStatsPanel Create(Transform canvas)
    {
        var go = new GameObject("UpgradeStatsPanel", typeof(RectTransform));
        var panel = go.AddComponent<UpgradeStatsPanel>();
        panel.Build(canvas);
        return panel;
    }

    void Build(Transform canvas)
    {
        var rt = (RectTransform)transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -52f);
        rt.sizeDelta = new Vector2(440f, 224f);

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.04f, 0.10f, 0.88f);
        bg.raycastTarget = false;

        CyberpunkUI.AddNeonBorder(rt, Accent * 2.2f, 2f);

        var header = CreateText("Header", rt,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            pos: new Vector2(0f, -6f), size: new Vector2(420f, 26f));
        header.text = "ХАРАКТЕРИСТИКИ";
        header.alignment = TextAlignmentOptions.Center;
        header.fontSize = 18f;
        CyberpunkUI.StyleTMP(header, Accent, Color.black, 0.25f);

        _labels = CreateText("Labels", rt,
            anchor: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f),
            pos: new Vector2(20f, -36f), size: new Vector2(230f, 182f));
        _labels.alignment = TextAlignmentOptions.TopLeft;
        _labels.fontSize = 15f;
        CyberpunkUI.StyleTMP(_labels, new Color(0.78f, 0.82f, 0.9f), Color.black, 0.18f, FontStyles.Normal);

        _values = CreateText("Values", rt,
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            pos: new Vector2(-20f, -36f), size: new Vector2(170f, 182f));
        _values.alignment = TextAlignmentOptions.TopRight;
        _values.fontSize = 15f;
        CyberpunkUI.StyleTMP(_values, Color.white, Color.black, 0.2f);
    }

    static TMP_Text CreateText(string objName, Transform parent, Vector2 anchor,
                               Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    public void Refresh()
    {
        gameObject.SetActive(true);

        var sbL = new StringBuilder();
        var sbV = new StringBuilder();

        void Row(string label, string value)
        {
            sbL.AppendLine(label);
            sbV.AppendLine(value);
        }

        int level = PlayerLevel.Instance != null ? PlayerLevel.Instance.Level : 1;
        Row("Уровень", level.ToString());

        if (PlayerMovement.Instance != null)
            Row("HP", $"{PlayerMovement.Instance.Health} / {PlayerMovement.Instance.MaxHealth}");
        else
            Row("HP", "—");

        var ps = PlayerStats.Instance;
        Row("Урон",            ps != null ? $"×{ps.DamageMultiplier:0.00}"    : "—");
        Row("Скорострельность", ps != null ? $"×{ps.FireRateMultiplier:0.00}"  : "—");
        Row("Скорость",        ps != null ? $"×{ps.MoveSpeedMultiplier:0.00}" : "—");
        Row("Радиус подбора",  ps != null ? $"{ps.PickupRadius:0.0}"          : "—");
        Row("Доп. снаряды",    ps != null ? $"+{ps.ExtraProjectiles}"         : "—");
        Row("Урон бомбы",      ps != null ? $"×{ps.BombDamageMultiplier:0.00}" : "—");

        _labels.text = sbL.ToString();
        _values.text = sbV.ToString();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
