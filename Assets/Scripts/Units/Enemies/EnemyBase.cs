using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : UnitsBase
{
    [Header("Поведение")]
    [Tooltip("Радиус контакта для нанесения урона игроку.")]
    [SerializeField] protected float _contactRadius = 0.6f;

    [Tooltip("Минимальный интервал между ударами в секундах.")]
    [SerializeField] protected float _attackCooldown = 0.8f;

    [Tooltip("Если игрока не нашли по тегу — враг просто стоит. Тег задаётся здесь.")]
    [SerializeField] protected string _playerTag = "Player";

    protected Transform _target;
    protected float _nextAttackTime;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        CacheMaxHealth();
    }

    protected virtual void Start()
    {
        AcquireTarget();
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();
    }

    private void AcquireTarget()
    {
        if (PlayerMovement.Instance != null)
            _target = PlayerMovement.Instance.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (_target == null)
        {
            AcquireTarget();
            if (_target == null) { rb.linearVelocity = Vector2.zero; return; }
        }

        Vector2 toTarget = (Vector2)_target.position - rb.position;
        float dist = toTarget.magnitude;

        if (dist <= _contactRadius)
        {
            rb.linearVelocity = Vector2.zero;
            TryHitTarget();
            return;
        }

        Vector2 dir = toTarget / Mathf.Max(dist, 0.0001f);
        rb.linearVelocity = dir * _speed;
    }

    protected virtual void TryHitTarget()
    {
        if (Time.time < _nextAttackTime) return;
        if (_target == null) return;

        var unit = _target.GetComponent<UnitsBase>();
        if (unit != null && unit.IsAlive)
        {
            unit.TakeDamage(_damage);
            _nextAttackTime = Time.time + _attackCooldown;
        }
    }

    [Header("Дроп опыта")]
    [SerializeField] protected GameObject _xpOrbPrefab;
    [SerializeField, Range(0f, 1f)] protected float _xpDropChance = 1f;
    [SerializeField, Min(1)] protected int _xpOrbCount = 1;

    [Header("Дроп аптечки")]
    [Tooltip("Шанс выронить аптечку при смерти (0..1). Держи маленьким — это редкий бонус.")]
    [SerializeField, Range(0f, 1f)] protected float _healthDropChance = 0.05f;
    [Tooltip("Сколько HP восстанавливает выпавшая аптечка.")]
    [SerializeField, Min(1)] protected int _healthOrbHeal = 25;

    protected override void onObjectDeath()
    {
        CyberpunkFX.SpawnDeathBurst(transform.position, CyberpunkFX.Magenta);
        CyberpunkFX.Shake(0.08f, 0.10f);

        if (_xpOrbPrefab != null && Random.value <= _xpDropChance)
        {
            for (int i = 0; i < _xpOrbCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 0.3f;
                Instantiate(_xpOrbPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        if (Random.value < _healthDropChance)
        {
            Vector2 offset = Random.insideUnitCircle * 0.3f;
            HealthOrb.Spawn(transform.position + (Vector3)offset, _healthOrbHeal);
        }

        if (HUD.Instance != null) HUD.Instance.RegisterKill();
        base.onObjectDeath();
    }

    /// <summary>Толчок от попадания — вызывается из снаряда/взрыва.</summary>
    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb == null || !IsAlive) return;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }
}
