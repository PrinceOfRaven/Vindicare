using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит сколько раз игрок взял каждый тип апгрейда.
/// Любая система (оружие, движение, бомба) обращается сюда,
/// чтобы узнать актуальный множитель.
/// 
/// Кладётся на тот же GameObject что и игрок.
/// </summary>
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

    // Счётчики стаков по каждому типу
    private readonly Dictionary<UpgradeData.UpgradeType, int> _stacks = new();

    // Кто-то взял апгрейд — сообщим. Полезно для UI и оружия (пересчитать).
    public event Action OnStatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Сколько раз взят апгрейд этого типа.</summary>
    public int GetStacks(UpgradeData.UpgradeType type)
    {
        return _stacks.TryGetValue(type, out int v) ? v : 0;
    }

    /// <summary>Можно ли ещё взять этот апгрейд (не превышен ли maxStacks).</summary>
    public bool CanTake(UpgradeData data)
    {
        return GetStacks(data.type) < data.maxStacks;
    }

    /// <summary>Применить апгрейд (увеличить счётчик и оповестить).</summary>
    public void ApplyUpgrade(UpgradeData data)
    {
        if (!CanTake(data)) return;

        int current = GetStacks(data.type);
        _stacks[data.type] = current + 1;

        // Особый случай: MaxHealth не только повышает потолок, но и лечит.
        if (data.type == UpgradeData.UpgradeType.MaxHealth)
        {
            if (PlayerMovement.Instance != null)
                PlayerMovement.Instance.HealByMaxHealthUpgrade(_maxHealthPerStack);
        }

        OnStatsChanged?.Invoke();
        Debug.Log($"[PlayerStats] Взят апгрейд {data.upgradeName}, теперь {_stacks[data.type]}/{data.maxStacks}");
    }

    // --- Геттеры, которыми пользуются другие системы ---

    public float DamageMultiplier => 1f + _damagePerStack * GetStacks(UpgradeData.UpgradeType.Damage);
    public float FireRateMultiplier => 1f + _fireRatePerStack * GetStacks(UpgradeData.UpgradeType.FireRate);
    public float MoveSpeedMultiplier => 1f + _moveSpeedPerStack * GetStacks(UpgradeData.UpgradeType.MoveSpeed);
    public int MaxHealthBonus => _maxHealthPerStack * GetStacks(UpgradeData.UpgradeType.MaxHealth);
    public float PickupRadius => _basePickupRadius * (1f + _pickupRadiusPerStack * GetStacks(UpgradeData.UpgradeType.PickupRadius));
    public int ExtraProjectiles => _projectilesPerStack * GetStacks(UpgradeData.UpgradeType.ProjectileCount);
    public float BombDamageMultiplier => 1f + _bombDamagePerStack * GetStacks(UpgradeData.UpgradeType.BombDamage);
}
