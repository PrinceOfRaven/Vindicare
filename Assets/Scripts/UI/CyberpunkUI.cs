using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class CyberpunkUI
{
    const string BorderRoot = "NeonBorder";

    public static void AddNeonBorder(RectTransform parent, Color color, float thickness = 2f)
    {
        if (parent == null) return;
        var existing = parent.Find(BorderRoot);
        if (existing != null)
        {
            SetNeonBorderColor(parent, color);
            return;
        }

        var root = new GameObject(BorderRoot, typeof(RectTransform));
        var rrt = (RectTransform)root.transform;
        rrt.SetParent(parent, false);
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        rrt.SetAsLastSibling();

        CreateEdge(rrt, "Top",    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, thickness), color);
        CreateEdge(rrt, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, thickness), color);
        CreateEdge(rrt, "Left",   new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(thickness, 0), color);
        CreateEdge(rrt, "Right",  new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(thickness, 0), color);
    }

    public static void SetNeonBorderColor(RectTransform parent, Color color)
    {
        if (parent == null) return;
        var root = parent.Find(BorderRoot);
        if (root == null) return;
        foreach (Transform child in root)
        {
            if (child.TryGetComponent(out Image img)) img.color = color;
        }
    }

    static void CreateEdge(RectTransform parent, string name, Vector2 aMin, Vector2 aMax,
                           Vector2 pivot, Vector2 sizeDelta, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    public static void StyleTMP(TMP_Text text, Color faceColor, Color outlineColor,
                                 float outlineWidth = 0.25f, FontStyles style = FontStyles.Bold)
    {
        if (text == null) return;
        text.color = faceColor;
        text.outlineColor = outlineColor;
        text.outlineWidth = outlineWidth;
        text.fontStyle = style;
    }

    public static Image AddIcon(Transform parent, Sprite sprite, Color color,
                                 Vector2 size, Vector2 anchoredPos,
                                 Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        if (parent == null || sprite == null) return null;
        var go = new GameObject("Icon_" + sprite.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return img;
    }
}
