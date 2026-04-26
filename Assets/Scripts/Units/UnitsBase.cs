using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class UnitsBase : MonoBehaviour
{
    [Header("Характеристики")]
    [SerializeField] protected int _health;
    [SerializeField] protected float _speed;
    [SerializeField] protected float _damage;
    protected Rigidbody2D rb;

    protected abstract void Awake();


    protected virtual void onObjectDeath()
    {
        Destroy(gameObject);
    }


}



