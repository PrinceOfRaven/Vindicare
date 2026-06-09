using System.Collections.Generic;
using UnityEngine;

public class Boss : EnemyBase
{
    /// <summary>Все живые боссы на сцене (для верхней полосы HP на HUD).</summary>
    public static readonly List<Boss> ActiveBosses = new List<Boss>();

    /// <summary>Первый живой босс или null.</summary>
    public static Boss Current
    {
        get
        {
            for (int i = ActiveBosses.Count - 1; i >= 0; i--)
            {
                if (ActiveBosses[i] == null || !ActiveBosses[i].IsAlive) ActiveBosses.RemoveAt(i);
            }
            return ActiveBosses.Count > 0 ? ActiveBosses[0] : null;
        }
    }

    private void OnEnable()
    {
        if (!ActiveBosses.Contains(this)) ActiveBosses.Add(this);
    }

    private void OnDisable()
    {
        ActiveBosses.Remove(this);
    }

    [Header("Босс — фазы")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float _phase2HealthFraction = 0.5f;
    [SerializeField] private float _phase2SpeedMultiplier = 1.6f;
    [SerializeField] private float _phase2SummonRateMultiplier = 0.5f;

    [Header("Босс — призыв миньонов")]
    [Tooltip("Префаб мини-жука, которого призывает босс. Должен иметь EnemyBase-компонент.")]
    [SerializeField] private GameObject _minionPrefab;
    [SerializeField, Min(1)] private int _minionsPerSummon = 6;
    [SerializeField, Min(0.1f)] private float _summonInterval = 6f;
    [SerializeField] private float _summonRadius = 2.5f;

    private float _nextSummonTime;
    private bool _phase2Activated;
    private float _basePhase1Speed;
    private float _basePhase1SummonInterval;

    protected override void Awake()
    {
        base.Awake();
        _basePhase1Speed = _speed;
        _basePhase1SummonInterval = _summonInterval;
        _nextSummonTime = Time.time + _summonInterval;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!_phase2Activated && _maxHealth > 0 &&
            (float)_health / _maxHealth <= _phase2HealthFraction)
        {
            EnterPhase2();
        }

        if (Time.time >= _nextSummonTime && _minionPrefab != null && IsAlive)
        {
            SummonMinions();
            _nextSummonTime = Time.time + _summonInterval;
        }
    }

    private void EnterPhase2()
    {
        _phase2Activated = true;
        _speed = _basePhase1Speed * _phase2SpeedMultiplier;
        _summonInterval = _basePhase1SummonInterval * _phase2SummonRateMultiplier;
    }

    private void SummonMinions()
    {
        for (int i = 0; i < _minionsPerSummon; i++)
        {
            float angle = (360f / _minionsPerSummon) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _summonRadius;
            Vector3 pos = transform.position + (Vector3)offset;

            Instantiate(_minionPrefab, pos, Quaternion.identity);
        }
    }

    [Header("Босс — награда за смерть")]
    [SerializeField, Min(0)] private int _bonusXpShower = 24;
    [SerializeField, Min(0)] private int _bossHealthOrbHeal = 50;

    protected override void onObjectDeath()
    {
        // Ощутимый payoff за победу над боссом.
        CyberpunkFX.SpawnExplosion(transform.position, 4f, CyberpunkFX.Magenta);
        CyberpunkFX.Shockwave(transform.position, 5f, CyberpunkFX.Cyan);
        CyberpunkFX.Shake(0.6f, 0.5f);
        CyberpunkFX.HitStop(0.12f);
        AudioFX.Explosion();

        if (_xpOrbPrefab != null)
        {
            for (int i = 0; i < _bonusXpShower; i++)
            {
                Vector2 offset = Random.insideUnitCircle * 1.8f;
                Instantiate(_xpOrbPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        if (_bossHealthOrbHeal > 0)
            HealthOrb.Spawn(transform.position, _bossHealthOrbHeal);

        base.onObjectDeath();
    }
}
