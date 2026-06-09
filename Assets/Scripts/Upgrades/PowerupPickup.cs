using UnityEngine;

/// <summary>
/// Предмет с пола (магнит / заморозка / нюк). Строится в коде, подбирается по дистанции.
/// </summary>
public class PowerupPickup : MonoBehaviour
{
    public enum Kind { Magnet, Freeze, Nuke }

    private const float DropChance = 0.025f;
    private const float PickupRadius = 1.15f;
    private const float Lifetime = 22f;
    private const float FreezeDuration = 3f;

    private Kind _kind;
    private float _dieTime;
    private bool _collected;

    public static bool RollDrop() => Random.value < DropChance;

    public static void SpawnRandom(Vector3 pos) => Spawn(pos, (Kind)Random.Range(0, 3));

    public static void Spawn(Vector3 pos, Kind kind)
    {
        var go = new GameObject("Powerup_" + kind);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.6f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = IconSprite();
        sr.color = ColorOf(kind);
        sr.sortingOrder = 7;

        CyberpunkFX.AttachLight(go.transform, ColorOf(kind), intensity: 1.7f, outerRadius: 2.1f);

        var p = go.AddComponent<PowerupPickup>();
        p._kind = kind;
        p._dieTime = Time.time + Lifetime;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, 60f * Time.deltaTime);

        // Мигание перед исчезновением.
        float left = _dieTime - Time.time;
        if (left <= 0f) { Destroy(gameObject); return; }
        if (left < 4f)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var c = sr.color;
                c.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 8f));
                sr.color = c;
            }
        }

        var player = PlayerMovement.Instance;
        if (player == null || _collected) return;

        float sqr = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;
        if (sqr <= PickupRadius * PickupRadius) Collect();
    }

    private void Collect()
    {
        _collected = true;
        Color col = ColorOf(_kind);

        switch (_kind)
        {
            case Kind.Magnet:
                ExperienceOrb.MagnetizeAll();
                Toast("МАГНИТ — весь опыт собран", col);
                break;
            case Kind.Freeze:
                EnemyBase.FreezeUntilTime = Time.time + FreezeDuration;
                CyberpunkFX.Shockwave(transform.position, 16f, col);
                Toast("ЗАМОРОЗКА", col);
                break;
            case Kind.Nuke:
                Nuke(col);
                Toast("НЮК", col);
                break;
        }

        AudioFX.Pickup();
        CyberpunkFX.SpawnPickupPop(transform.position, col);
        Destroy(gameObject);
    }

    private void Nuke(Color col)
    {
        var enemies = Object.FindObjectsOfType<EnemyBase>();
        foreach (var e in enemies)
        {
            if (e == null || !e.IsAlive) continue;
            if (e is Boss) e.TakeDamage(300f);  // боссу — крупный урон, но не инстакилл
            else e.Kill();                       // обычных — мгновенно, без числа
        }

        CyberpunkFX.SpawnExplosion(transform.position, 6f, col);
        CyberpunkFX.Shake(0.6f, 0.45f);
        CyberpunkFX.HitStop(0.08f);
        AudioFX.Explosion();
    }

    private static void Toast(string text, Color color)
    {
        if (HUD.Instance != null) HUD.Instance.FlashMessage(text, color);
    }

    private static Color ColorOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.Magnet: return CyberpunkFX.Cyan;
            case Kind.Freeze: return new Color(0.4f, 0.7f, 1f);
            default:          return CyberpunkFX.HotRed;
        }
    }

    private static Sprite _icon;
    private static Sprite IconSprite()
    {
        if (_icon != null) return _icon;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Ромб с мягкой кромкой.
                float d = (Mathf.Abs(x + 0.5f - c.x) + Mathf.Abs(y + 0.5f - c.y)) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.75f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _icon = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _icon;
    }
}
