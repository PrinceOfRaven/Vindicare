using UnityEngine;

/// <summary>
/// Аптечка. С небольшим шансом дропается с убитых врагов (см. EnemyBase).
/// Когда игрок в радиусе PlayerStats.PickupRadius — летит к нему, при касании
/// лечит игрока. Визуал (красный крест + свет) строится целиком в рантайме,
/// поэтому префаб не нужен.
/// </summary>
public class HealthOrb : MonoBehaviour
{
    static readonly Color OrbColor = new Color(1f, 0.28f, 0.34f);

    private int _healAmount = 25;
    private float _startSpeed = 4f;
    private float _acceleration = 25f;
    private float _collectDistance = 0.3f;

    private float _currentSpeed;
    private float _bobTimer;
    private bool _collected;

    /// <summary>Создать аптечку в мире.</summary>
    public static HealthOrb Spawn(Vector3 position, int healAmount)
    {
        var go = new GameObject("HealthOrb");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetCrossSprite();
        sr.color = OrbColor;
        sr.sortingOrder = 5;

        var orb = go.AddComponent<HealthOrb>();
        orb._healAmount = Mathf.Max(1, healAmount);
        return orb;
    }

    private void Start()
    {
        _currentSpeed = _startSpeed;
        CyberpunkFX.AttachLight(transform, CyberpunkFX.HotRed, intensity: 1.3f, outerRadius: 1.0f);
    }

    private void Update()
    {
        // Лёгкая пульсация — чтобы аптечка читалась как подбираемый предмет
        _bobTimer += Time.deltaTime;
        float s = 1f + 0.12f * Mathf.Sin(_bobTimer * 4f);
        transform.localScale = new Vector3(s, s, 1f);

        if (_collected) return;
        if (PlayerMovement.Instance == null) return;

        Transform player = PlayerMovement.Instance.transform;
        float dist = Vector2.Distance(transform.position, player.position);
        float pickupRadius = PlayerStats.Instance != null
            ? PlayerStats.Instance.PickupRadius
            : 1.5f;

        // Слишком далеко — лежим и ждём
        if (dist > pickupRadius) return;

        if (dist <= _collectDistance)
        {
            Collect();
            return;
        }

        // Летим к игроку, ускоряясь
        Vector3 dir = (player.position - transform.position).normalized;
        _currentSpeed += _acceleration * Time.deltaTime;
        transform.position += dir * _currentSpeed * Time.deltaTime;
    }

    private void Collect()
    {
        _collected = true;
        CyberpunkFX.SpawnPickupPop(transform.position, CyberpunkFX.HotRed);
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.Heal(_healAmount);
        Destroy(gameObject);
    }

    // ─── Процедурный спрайт креста-аптечки ───

    static Sprite _crossSprite;

    static Sprite GetCrossSprite()
    {
        if (_crossSprite != null) return _crossSprite;

        const int S = 32;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var clear = new Color(1f, 1f, 1f, 0f);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                bool inVertical   = x >= 12 && x < 20 && y >= 4 && y < 28;
                bool inHorizontal = y >= 12 && y < 20 && x >= 4 && x < 28;
                px[y * S + x] = (inVertical || inHorizontal) ? Color.white : clear;
            }
        }
        tex.SetPixels(px);
        tex.Apply();

        _crossSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 64f);
        return _crossSprite;
    }
}
