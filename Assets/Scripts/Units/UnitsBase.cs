using System;
using UnityEngine;

public abstract class UnitsBase : MonoBehaviour
{
    [Header("Характеристики")]
    [SerializeField] protected int _health;
    [SerializeField] protected float _speed;
    [SerializeField] protected float _damage;

    protected Rigidbody2D rb;
    protected int _maxHealth;

    public event Action<UnitsBase> OnDeath;

    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public float Damage => _damage;
    public bool IsAlive => _health > 0;

    protected abstract void Awake();

    protected void CacheMaxHealth()
    {
        _maxHealth = _health;
    }

    public virtual bool TakeDamage(float amount)
    {
        if (!IsAlive) return true;

        _health -= Mathf.Max(1, Mathf.RoundToInt(amount));
        if (_health <= 0)
        {
            _health = 0;
            onObjectDeath();
            return true;
        }
        return false;
    }

    protected void RaiseDeath()
    {
        OnDeath?.Invoke(this);
    }

    protected virtual void onObjectDeath()
    {
        RaiseDeath();
        Destroy(gameObject);
    }
}
