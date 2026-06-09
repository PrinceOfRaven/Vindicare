using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PooledBullet : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private string _targetTag = "Enemy";

    [Header("Фидбэк")]
    [SerializeField] private float _knockbackForce = 4f;

    /// <summary>
    /// Уникальные модификаторы поведения, которые оружие передаёт пуле при выстреле.
    /// Значения по умолчанию = обычная прямая пуля.
    /// </summary>
    public struct Modifiers
    {
        public int pierce;            // сколько врагов пуля прошивает насквозь (0 = стоп на первом)
        public int ricochet;          // к скольким новым целям пуля отскакивает после попадания
        public float ricochetRadius;  // радиус поиска цели для рикошета
        public float knockback;       // сила отбрасывания (перекрывает дефолт пули, если > 0)
        public float pointBlankMult;  // множитель урона у дула (спадает до 1 на дистанции pointBlankRange)
        public float pointBlankRange; // дистанция, на которой бонус в упор сходит на нет
        public float speedMult;       // множитель скорости полёта
        public Color color;           // цвет следа/света/искр

        public static Modifiers Default => new Modifiers
        {
            pierce = 0,
            ricochet = 0,
            ricochetRadius = 5f,
            knockback = 0f,
            pointBlankMult = 1f,
            pointBlankRange = 1f,
            speedMult = 1f,
            color = CyberpunkFX.Cyan
        };
    }

    private BulletPool _bulletPool;
    private float _timer;
    private bool _isActive;
    private float _damage;
    private Vector2 _direction;
    private TrailRenderer _trail;
    private Light2D _light;

    private Modifiers _mods;
    private Vector2 _spawnPos;
    private int _pierceLeft;
    private int _ricochetLeft;
    private readonly HashSet<UnitsBase> _hitUnits = new HashSet<UnitsBase>();

    private void Awake()
    {
        _trail = GetComponent<TrailRenderer>();
        if (_trail == null)
        {
            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.material = CyberpunkFX.TrailMat();
            _trail.startColor = CyberpunkFX.Cyan * 2.5f;
            _trail.endColor = new Color(CyberpunkFX.Cyan.r, CyberpunkFX.Cyan.g, CyberpunkFX.Cyan.b, 0f);
            _trail.startWidth = 0.6f;
            _trail.endWidth = 0f;
            _trail.time = 0.18f;
            _trail.minVertexDistance = 0.05f;
            _trail.sortingOrder = 9;
            _trail.emitting = false;
        }
        _light = CyberpunkFX.AttachLight(transform, CyberpunkFX.Cyan, intensity: 1.5f, outerRadius: 1.6f);
    }

    public void Initialize(BulletPool bulletPool, Vector2 direction, float damage)
    {
        Initialize(bulletPool, direction, damage, Modifiers.Default);
    }

    public void Initialize(BulletPool bulletPool, Vector2 direction, float damage, Modifiers mods)
    {
        _bulletPool = bulletPool;
        _damage = damage;
        _timer = 0f;
        _isActive = true;
        _direction = direction;

        _mods = mods;
        _spawnPos = transform.position;
        _pierceLeft = mods.pierce;
        _ricochetLeft = mods.ricochet;
        _hitUnits.Clear();

        ApplyColor(mods.color);

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = direction * (_bulletSpeed * Mathf.Max(mods.speedMult, 0.01f));
        }
        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }
    }

    private void ApplyColor(Color color)
    {
        if (_trail != null)
        {
            _trail.startColor = color * 2.5f;
            _trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }
        if (_light != null) _light.color = color;
    }

    private void Update()
    {
        if (!_isActive) return;

        _timer += Time.deltaTime;
        if (_timer >= _lifeTime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!_isActive) return;
        if (collision.CompareTag("Bullet")) return;
        if (collision.CompareTag("Player")) return;

        if (collision.CompareTag(_targetTag))
        {
            var unit = collision.GetComponentInParent<UnitsBase>();

            // Уже задели этого врага (актуально для пробития/рикошета) — пролетаем дальше.
            if (unit != null && _hitUnits.Contains(unit)) return;

            if (unit != null)
            {
                _hitUnits.Add(unit);

                float dmg = ComputeDamage();
                bool crit = PlayerStats.Instance != null
                            && Random.value < PlayerStats.Instance.CritChance;
                if (crit) dmg *= PlayerStats.Instance.CritMultiplier;

                unit.TakeDamage(dmg);

                if (crit)
                {
                    // Маркер крита — янтарные искры (число и так показывается крупнее базовым попапом).
                    CyberpunkFX.SpawnHitSpark(unit.transform.position, CyberpunkFX.Amber);
                    CyberpunkFX.SpawnHitSpark(unit.transform.position + Vector3.up * 0.25f, CyberpunkFX.Amber);
                }

                if (unit is EnemyBase enemy)
                    enemy.ApplyKnockback(_direction, KnockbackForce());
            }
            CyberpunkFX.SpawnHitSpark(transform.position, _mods.color);

            // Пробитие имеет приоритет: пуля летит сквозь врага дальше по прямой.
            if (_pierceLeft > 0)
            {
                _pierceLeft--;
                return;
            }

            // Рикошет: отскок к ближайшей новой цели.
            if (_ricochetLeft > 0 && TryRicochet())
            {
                _ricochetLeft--;
                return;
            }

            ReturnToPool();
            return;
        }

        // Препятствие (стена и т.п.) — пуля гаснет.
        ReturnToPool();
    }

    private float ComputeDamage()
    {
        if (_mods.pointBlankMult <= 1f) return _damage;

        float dist = Vector2.Distance(transform.position, _spawnPos);
        float t = Mathf.Clamp01(dist / Mathf.Max(_mods.pointBlankRange, 0.01f));
        float mult = Mathf.Lerp(_mods.pointBlankMult, 1f, t);
        return _damage * mult;
    }

    private float KnockbackForce()
    {
        return _mods.knockback > 0f ? _mods.knockback : _knockbackForce;
    }

    private bool TryRicochet()
    {
        UnitsBase best = null;
        float bestSqr = float.MaxValue;
        Vector2 pos = transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, _mods.ricochetRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag(_targetTag)) continue;
            var unit = hits[i].GetComponentInParent<UnitsBase>();
            if (unit == null || !unit.IsAlive || _hitUnits.Contains(unit)) continue;

            float sqr = ((Vector2)unit.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = unit;
            }
        }

        if (best == null) return false;

        // Цепная молния между прежней целью и новой.
        CyberpunkFX.LightningBolt(pos, best.transform.position, _mods.color);

        _direction = ((Vector2)best.transform.position - pos).normalized;
        _timer = 0f;
        if (TryGetComponent(out Rigidbody2D rb))
            rb.linearVelocity = _direction * (_bulletSpeed * Mathf.Max(_mods.speedMult, 0.01f));
        return true;
    }

    private void ReturnToPool()
    {
        if (!_isActive) return;
        _isActive = false;

        if (_trail != null) _trail.emitting = false;
        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
        _bulletPool.Return(gameObject);
    }
}
