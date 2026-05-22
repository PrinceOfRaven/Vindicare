using UnityEngine;

public class EliteEnemy : Enemy
{
    [Header("Элита")]
    [SerializeField, Min(1f)] private float _eliteMultiplier = 1.8f;
    [SerializeField] private Color _eliteTint = new Color(1f, 0.6f, 0.1f, 1f);
    [SerializeField, Min(1f)] private float _scaleMultiplier = 1.3f;
    [SerializeField, Min(1)] private int _bonusXpOrbs = 4;

    protected override void Awake()
    {
        base.Awake();

        _health = Mathf.RoundToInt(_health * _eliteMultiplier);
        _damage *= _eliteMultiplier;
        _speed *= _eliteMultiplier;
        CacheMaxHealth();

        _xpOrbCount += _bonusXpOrbs;

        transform.localScale *= _scaleMultiplier;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = _eliteTint;
    }
}
