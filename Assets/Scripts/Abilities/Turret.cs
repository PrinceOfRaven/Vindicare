using UnityEngine;

/// <summary>
/// Авто-турель: живёт несколько секунд, периодически бьёт ближайшего врага в радиусе лучом.
/// Урон масштабируется множителем урона игрока.
/// </summary>
public class Turret : MonoBehaviour
{
    private const float _lifetime = 8f;
    private const float _fireInterval = 0.45f;
    private const float _range = 6.5f;
    private const float _baseDamage = 9f;

    private float _dieTime;
    private float _nextFireTime;
    private Color _color = CyberpunkFX.Lime;
    private static readonly Collider2D[] _hits = new Collider2D[64];

    public void Init(Color color)
    {
        _color = color;
        _dieTime = Time.time + _lifetime;

        var sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = BodySprite();
        sr.color = color;
        sr.sortingOrder = 5;
        transform.localScale = Vector3.one * 0.7f;

        CyberpunkFX.AttachLight(transform, color, intensity: 1.4f, outerRadius: 2.4f);
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);

        if (Time.time >= _dieTime)
        {
            CyberpunkFX.SpawnDeathBurst(transform.position, _color);
            Destroy(gameObject);
            return;
        }

        if (Time.time >= _nextFireTime)
        {
            if (TryFire()) _nextFireTime = Time.time + _fireInterval;
            else _nextFireTime = Time.time + 0.15f;
        }
    }

    private bool TryFire()
    {
        EnemyBase nearest = FindNearestEnemy();
        if (nearest == null) return false;

        float dmgMul = PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : 1f;
        nearest.TakeDamage(_baseDamage * dmgMul);

        CyberpunkFX.Beam(transform.position, nearest.transform.position, _color, 0.18f, 0.08f);
        CyberpunkFX.SpawnHitSpark(nearest.transform.position, _color);
        return true;
    }

    private EnemyBase FindNearestEnemy()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, _range, _hits);
        EnemyBase best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null) continue;
            if (!col.TryGetComponent(out EnemyBase enemy)) continue;
            if (!enemy.IsAlive) continue;

            float sqr = ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = enemy; }
        }
        return best;
    }

    private static Sprite _bodySprite;
    private static Sprite BodySprite()
    {
        if (_bodySprite != null) return _bodySprite;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var center = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Ромб.
                float d = Mathf.Abs(x - center.x) + Mathf.Abs(y - center.y);
                float a = d <= size * 0.42f ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _bodySprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _bodySprite;
    }
}
