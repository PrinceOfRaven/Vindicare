using UnityEngine;

public class PooledBullet : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _bulletSpeed = 10f;

    [Tooltip("Тег цели, по которой пуля наносит урон. По умолчанию пуля игрока бьёт врагов.")]
    [SerializeField] private string _targetTag = "Enemy";

    private BulletPool _bulletPool;
    private float _timer;
    private bool _isActive;
    private float _damage;

    public void Initialize(BulletPool bulletPool, Vector2 direction, float damage)
    {
        _bulletPool = bulletPool;
        _damage = damage;
        _timer = 0f;
        _isActive = true;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = direction * _bulletSpeed;
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
            if (unit != null) unit.TakeDamage(_damage);
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!_isActive) return;
        _isActive = false;

        if (TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
        }
        _bulletPool.Return(gameObject);
    }
}
