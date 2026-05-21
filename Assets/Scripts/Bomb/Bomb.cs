using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bomb : MonoBehaviour
{
    [Header("Параметры бомбы")]
    [SerializeField, Min(0.1f)] private float _speed = 8f;
    [SerializeField, Min(0.1f)] private float _fuseTime = 1.5f;
    [SerializeField] private float _explosionRadius = 2f;
    [SerializeField] private float _damage = 30f;
    [SerializeField] private LayerMask _targetLayers = -1;

    [Header("Эффекты")]
    [SerializeField] private GameObject _explosionPrefab;

    private bool _exploded;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        // Яркая пульсирующая магента-метка
        CyberpunkFX.AttachLight(transform, CyberpunkFX.Magenta, intensity: 2.0f, outerRadius: 2.2f);
    }

    public void Launch(Vector2 direction)
    {
        _rb.linearVelocity = direction.normalized * _speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        StartCoroutine(DetonationTimer());
    }

    private IEnumerator DetonationTimer()
    {
        yield return new WaitForSeconds(_fuseTime);
        Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        float dmgMult = PlayerStats.Instance != null ? PlayerStats.Instance.BombDamageMultiplier : 1f;
        float finalDmg = _damage * dmgMult;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _targetLayers);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out UnitsBase unit) && unit.IsAlive)
            {
                unit.TakeDamage(finalDmg);
                if (unit is EnemyBase enemy)
                {
                    Vector2 push = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    enemy.ApplyKnockback(push, 10f);
                }
            }
        }

        CyberpunkFX.SpawnExplosion(transform.position, _explosionRadius, CyberpunkFX.Magenta);
        CyberpunkFX.Shake(0.5f, 0.4f);

        if (_explosionPrefab != null)
        {
            Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}