using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public class PooledBullet : MonoBehaviour
{
    [SerializeField] private float _lifeTime;
    [SerializeField] private float _bulletSpeed;

    private BulletPool _bulletPool;
    private float _timer;

    public void Initialize(BulletPool bulletPool, Vector2 direction) 
    {
        _bulletPool = bulletPool;
        _timer = 0;

        if (TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = direction * _bulletSpeed;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifeTime) ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

    private void ReturnToPool() 
    {
        if (_bulletPool != null) 
        {
            _bulletPool.Return(gameObject);
        }

        if (TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
    }
}
