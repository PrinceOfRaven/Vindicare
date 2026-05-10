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
    }

    protected void AcquireTarget()
    {
        var go = GameObject.FindGameObjectWithTag(_playerTag);
        if (go != null) _target = go.transform;
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
}
