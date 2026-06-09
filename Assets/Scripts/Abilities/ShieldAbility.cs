using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Щит (Q): полная неуязвимость на несколько секунд + визуальный купол.</summary>
public class ShieldAbility : PlayerAbility
{
    private const float _shieldDuration = 3f;

    public override string DisplayName => "ЩИТ";
    public override string KeyLabel => "Q";
    public override Key Hotkey => Key.Q;
    public override Color ThemeColor => new Color(0.2f, 0.7f, 1f);
    protected override float DefaultCooldown => 12f;

    protected override void Activate()
    {
        var player = PlayerMovement.Instance;
        if (player == null) return;

        player.SetInvulnerable(_shieldDuration);
        AudioFX.Shield();
        StartCoroutine(ShieldVisual(player.transform, _shieldDuration));
    }

    private IEnumerator ShieldVisual(Transform player, float duration)
    {
        var light = CyberpunkFX.AttachLight(player, ThemeColor, intensity: 1.6f, outerRadius: 3.2f, innerRadius: 1.2f);

        var bubble = new GameObject("ShieldBubble");
        bubble.transform.SetParent(player, false);
        var sr = bubble.AddComponent<SpriteRenderer>();
        sr.sprite = ShieldSprite();
        sr.color = new Color(ThemeColor.r, ThemeColor.g, ThemeColor.b, 0.30f);
        sr.sortingOrder = 50;
        bubble.transform.localScale = Vector3.one * 2.2f;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float pulse = 1f + 0.06f * Mathf.Sin(t * 10f);
            bubble.transform.localScale = Vector3.one * (2.2f * pulse);
            float fade = t > duration - 0.5f ? Mathf.Clamp01((duration - t) / 0.5f) : 1f;
            var c = sr.color; c.a = 0.30f * fade; sr.color = c;
            yield return null;
        }

        if (light != null) Destroy(light.gameObject);
        Destroy(bubble);
    }

    private static Sprite _shieldSprite;
    private static Sprite ShieldSprite()
    {
        if (_shieldSprite != null) return _shieldSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float d = (uv - center).magnitude * 2f;
                // Кольцо: ярче ближе к краю круга, прозрачно в центре и снаружи.
                float ring = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.95f, d))
                           * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.95f, 1.05f, d)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, ring));
            }
        }
        tex.Apply();
        _shieldSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _shieldSprite;
    }
}
