using UnityEngine;

public class PooledBullet : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private float _bulletSpeed = 10f;

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
        if (collision.CompareTag("Bullet")) return;

        if (_isActive) ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!_isActive) return;
        _isActive = false;
        _bulletPool.Return(gameObject);
    }
}