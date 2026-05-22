using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    const int BgSortingOrder = 45;
    const int FillSortingOrder = 46;
    const float BarHeight = 0.13f;
    const float WidthFactor = 0.95f;
    const float YPadding = 0.22f;

    static readonly Color BgColor = new Color(0f, 0f, 0f, 0.65f);
    static readonly Color HealthyColor = new Color(0.3f, 1f, 0.35f);
    static readonly Color LowColor = new Color(1f, 0.18f, 0.22f);

    static Sprite _whiteSprite;

    UnitsBase _unit;
    Transform _fill;
    SpriteRenderer _fillRenderer;
    SpriteRenderer _bgRenderer;
    float _width;

    void Start()
    {
        _unit = GetComponent<UnitsBase>();
        if (_unit == null) { enabled = false; return; }

        var sr = GetComponentInChildren<SpriteRenderer>();
        float spriteWidth = 1f;
        float yOffset = 0.7f;
        if (sr != null && sr.sprite != null)
        {
            spriteWidth = sr.sprite.bounds.size.x;
            yOffset = sr.sprite.bounds.extents.y + YPadding;
        }
        _width = Mathf.Max(0.4f, spriteWidth * WidthFactor);

        var root = new GameObject("HealthBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, yOffset, 0f);
        root.transform.localRotation = Quaternion.identity;

        _bgRenderer = CreateQuad(root.transform, "HPBar_BG", BgColor, BgSortingOrder);
        _fillRenderer = CreateQuad(root.transform, "HPBar_Fill", HealthyColor, FillSortingOrder);
        _fill = _fillRenderer.transform;
    }

    SpriteRenderer CreateQuad(Transform parent, string objName, Color color, int sortingOrder)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(-_width * 0.5f, 0f, 0f);
        go.transform.localScale = new Vector3(_width, BarHeight, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    void LateUpdate()
    {
        if (_unit == null || _fill == null) return;

        int max = _unit.MaxHealth;
        float frac = max > 0 ? Mathf.Clamp01((float)_unit.Health / max) : 0f;

        var s = _fill.localScale;
        s.x = _width * frac;
        _fill.localScale = s;

        if (_fillRenderer != null) _fillRenderer.color = Color.Lerp(LowColor, HealthyColor, frac);
        if (_bgRenderer != null) _bgRenderer.color = BgColor;
    }

    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        _whiteSprite.name = "HealthBarWhite";
        _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _whiteSprite;
    }
}
