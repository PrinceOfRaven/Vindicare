using UnityEngine;

/// <summary>
/// Снаряд врага-стрелка. Полностью строится в коде, летит по прямой и бьёт игрока
/// по дистанции (без зависимости от слоёв физики).
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    private Vector2 _velocity;
    private float _damage;
    private float _dieTime;
    private Color _color;
    private const float HitRadius = 0.45f;

    public static void Spawn(Vector3 pos, Vector2 dir, float speed, float damage, Color color)
    {
        var go = new GameObject("EnemyProjectile");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 3.0f; // ~0.48 мировых единиц — под хитбокс

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DotSprite();
        sr.color = color * 1.4f; // ярче
        sr.sortingOrder = 8;

        CyberpunkFX.AttachLight(go.transform, color, intensity: 1.8f, outerRadius: 2.2f);

        var p = go.AddComponent<EnemyProjectile>();
        p._velocity = dir.normalized * speed;
        p._damage = damage;
        p._color = color;
        p._dieTime = Time.time + 5f;
    }

    private void Update()
    {
        transform.position += (Vector3)(_velocity * Time.deltaTime);

        if (Time.time >= _dieTime) { Destroy(gameObject); return; }

        var player = PlayerMovement.Instance;
        if (player == null) return;

        float sqr = ((Vector2)player.transform.position - (Vector2)transform.position).sqrMagnitude;
        if (sqr <= HitRadius * HitRadius)
        {
            if (player.IsAlive) player.TakeDamage(_damage);
            CyberpunkFX.SpawnHitSpark(transform.position, _color);
            Destroy(gameObject);
        }
    }

    private static Sprite _dot;
    private static Sprite DotSprite()
    {
        if (_dot != null) return _dot;
        const int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var c = new Vector2(size / 2f, size / 2f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (size / 2f);
                float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.6f, 1f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        _dot = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _dot;
    }
}
