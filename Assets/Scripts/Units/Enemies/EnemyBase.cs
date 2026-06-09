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
    private IEnemyAbility _ability;

    /// <summary>Время, до которого все враги заморожены (предмет «Заморозка»).</summary>
    public static float FreezeUntilTime;
    public static bool Frozen => Time.time < FreezeUntilTime;

    public Transform Target => _target;

    /// <summary>Когда true — движением врага управляет способность (рывок чарджера), а не базовая логика.</summary>
    public bool ExternalControl { get; set; }

    /// <summary>Доступ к Rigidbody для болт-он способностей.</summary>
    public Rigidbody2D Body => rb;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        CacheMaxHealth();
        ApplyDifficulty();
    }

    /// <summary>
    /// Масштабирует базовые характеристики врага под текущую сложность забега.
    /// Вызывается в Awake до любых частных множителей (элита/босс),
    /// чтобы те накладывались поверх.
    /// </summary>
    protected void ApplyDifficulty()
    {
        float hp = RunDifficulty.HealthMultiplier;
        if (hp > 1f)
        {
            _maxHealth = Mathf.Max(1, Mathf.RoundToInt(_maxHealth * hp));
            _health = _maxHealth;
        }
        _damage *= RunDifficulty.DamageMultiplier;
        _speed *= RunDifficulty.SpeedMultiplier;
    }

    protected virtual void Start()
    {
        AcquireTarget();
        if (GetComponent<EnemyHealthBar>() == null)
            gameObject.AddComponent<EnemyHealthBar>();
        _ability = GetComponent<IEnemyAbility>();
    }

    private void AcquireTarget()
    {
        if (PlayerMovement.Instance != null)
            _target = PlayerMovement.Instance.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (Frozen) { rb.linearVelocity = Vector2.zero; return; }
        if (ExternalControl) return; // движение перехватила способность (рывок)

        if (_target == null)
        {
            AcquireTarget();
            if (_target == null) { rb.linearVelocity = Vector2.zero; return; }
        }

        Vector2 toTarget = (Vector2)_target.position - rb.position;
        float dist = toTarget.magnitude;

        float stopRange = (_ability != null && _ability.StopRange > 0f) ? _ability.StopRange : _contactRadius;

        if (dist <= stopRange)
        {
            rb.linearVelocity = Vector2.zero;
            if (_ability != null) _ability.Act(this, _target, dist);
            else TryHitTarget();
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
        CyberpunkFX.HitStopThrottled(0.05f);
        AudioFX.EnemyDeath();

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

        if (PowerupPickup.RollDrop())
            PowerupPickup.SpawnRandom(transform.position);

        if (HUD.Instance != null) HUD.Instance.RegisterKill();

        // Вампиризм: лечим игрока за убийство.
        if (PlayerStats.Instance != null && PlayerMovement.Instance != null)
        {
            int lifesteal = PlayerStats.Instance.LifestealPerKill;
            if (lifesteal > 0) PlayerMovement.Instance.HealSilent(lifesteal);
        }

        base.onObjectDeath();
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb == null || !IsAlive) return;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }
}
