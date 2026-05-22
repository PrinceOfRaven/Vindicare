using UnityEngine;

public class PooledBullet : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _bulletSpeed = 10f;
    [SerializeField] private string _targetTag = "Enemy";

    [Header("Фидбэк")]
    [SerializeField] private float _knockbackForce = 4f;

    private BulletPool _bulletPool;
    private float _timer;
    private bool _isActive;
    private float _damage;
    private Vector2 _direction;
    private TrailRenderer _trail;

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
        CyberpunkFX.AttachLight(transform, CyberpunkFX.Cyan, intensity: 1.5f, outerRadius: 1.6f);
    }

    public void Initialize(BulletPool bulletPool, Vector2 direction, float damage)
    {
        _bulletPool = bulletPool;
        _damage = damage;
        _timer = 0f;
        _isActive = true;
        _direction = direction;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = direction * _bulletSpeed;
        }
        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = true;
        }
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
            if (unit != null)
            {
                unit.TakeDamage(_damage);
                if (unit is EnemyBase enemy)
                    enemy.ApplyKnockback(_direction, _knockbackForce);
            }
            CyberpunkFX.SpawnHitSpark(transform.position, CyberpunkFX.Cyan);
        }

        ReturnToPool();
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
