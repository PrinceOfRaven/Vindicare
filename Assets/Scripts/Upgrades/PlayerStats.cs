using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Базовые значения мультипликаторов (на сколько растёт каждый стак)")]
    [Tooltip("+20% к урону за каждый стак Damage")]
    [SerializeField] private float _damagePerStack = 0.20f;

    [Tooltip("+15% к скорости стрельбы за каждый стак FireRate")]
    [SerializeField] private float _fireRatePerStack = 0.15f;

    [Tooltip("+10% к скорости движения за каждый стак MoveSpeed")]
    [SerializeField] private float _moveSpeedPerStack = 0.10f;

    [Tooltip("+20 HP за каждый стак MaxHealth")]
    [SerializeField] private int _maxHealthPerStack = 20;

    [Tooltip("+40% к радиусу подбора за каждый стак PickupRadius")]
    [SerializeField] private float _pickupRadiusPerStack = 0.40f;

    [Tooltip("+1 пуля за каждый стак ProjectileCount")]
    [SerializeField] private int _projectilesPerStack = 1;

    [Tooltip("+25% к урону бомбы за каждый стак BombDamage")]
    [SerializeField] private float _bombDamagePerStack = 0.25f;

    [Header("Базовый радиус подбора (мировые единицы)")]
    [SerializeField] private float _basePickupRadius = 1.5f;

    private readonly Dictionary<UpgradeData.UpgradeType, int> _stacks = new();

    public event Action OnStatsChanged;

    private float _buffEndTime;
    private float _buffDamage = 1f;
    private float _buffFireRate = 1f;
    private float _buffMoveSpeed = 1f;

    /// <summary>Активен ли сейчас временный бафф (овердрайв).</summary>
    public bool HasTempBuff => Time.time < _buffEndTime;

    /// <summary>Накладывает временный бафф на duration секунд (множители поверх стаков апгрейдов).</summary>
    public void ApplyTempBuff(float damageMul, float fireRateMul, float moveMul, float duration)
    {
        _buffDamage = damageMul;
        _buffFireRate = fireRateMul;
        _buffMoveSpeed = moveMul;
        _buffEndTime = Time.time + duration;
        OnStatsChanged?.Invoke();
    }

    private void Update()
    {
        if (!HasTempBuff && _buffDamage != 1f)
        {
            _buffDamage = _buffFireRate = _buffMoveSpeed = 1f;
            OnStatsChanged?.Invoke();
        }
    }

    private float BuffDamage   => HasTempBuff ? _buffDamage : 1f;
    private float BuffFireRate => HasTempBuff ? _buffFireRate : 1f;
    private float BuffMoveSpeed => HasTempBuff ? _buffMoveSpeed : 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetStacks(UpgradeData.UpgradeType type)
    {
        return _stacks.TryGetValue(type, out int v) ? v : 0;
    }

    public bool CanTake(UpgradeData data)
    {
        return GetStacks(data.type) < data.maxStacks;
    }

    public void ApplyUpgrade(UpgradeData data)
    {
        if (!CanTake(data)) return;

        int current = GetStacks(data.type);
        _stacks[data.type] = current + 1;

        if (data.type == UpgradeData.UpgradeType.MaxHealth)
        {
            if (PlayerMovement.Instance != null)
                PlayerMovement.Instance.HealByMaxHealthUpgrade(_maxHealthPerStack);
        }

        OnStatsChanged?.Invoke();
    }

    public float DamageMultiplier => (1f + _damagePerStack * GetStacks(UpgradeData.UpgradeType.Damage)) * BuffDamage;
    public float FireRateMultiplier => (1f + _fireRatePerStack * GetStacks(UpgradeData.UpgradeType.FireRate)) * BuffFireRate;
    public float MoveSpeedMultiplier => (1f + _moveSpeedPerStack * GetStacks(UpgradeData.UpgradeType.MoveSpeed)) * BuffMoveSpeed;
    public int MaxHealthBonus => _maxHealthPerStack * GetStacks(UpgradeData.UpgradeType.MaxHealth);
    public float PickupRadius => _basePickupRadius * (1f + _pickupRadiusPerStack * GetStacks(UpgradeData.UpgradeType.PickupRadius));
    public int ExtraProjectiles => _projectilesPerStack * GetStacks(UpgradeData.UpgradeType.ProjectileCount);
    public float BombDamageMultiplier => 1f + _bombDamagePerStack * GetStacks(UpgradeData.UpgradeType.BombDamage);
}
