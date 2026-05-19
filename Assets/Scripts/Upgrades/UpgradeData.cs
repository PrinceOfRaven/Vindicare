using UnityEngine;

/// <summary>
/// Описание одного апгрейда. Создаётся как файл в проекте:
/// Правый клик → Create → Roguelike → Upgrade.
/// 
/// Тип бонуса задаётся через UpgradeType (enum). Менеджер апгрейдов
/// при выборе игроком увеличит соответствующий счётчик в PlayerStats.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_", menuName = "Roguelike/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public enum UpgradeType
    {
        Damage,         // +урон оружия
        FireRate,       // +скорость стрельбы
        MoveSpeed,      // +скорость движения
        MaxHealth,      // +максимум HP (и сразу лечит)
        PickupRadius,   // +радиус подбора кристаллов
        ProjectileCount,// +1 доп. пуля в очереди
        BombDamage,     // +урон бомбы
    }

    [Header("Описание для UI")]
    public string upgradeName = "Новый апгрейд";
    [TextArea] public string description = "Что делает апгрейд";
    public Sprite icon;

    [Header("Логика")]
    public UpgradeType type;

    [Tooltip("Максимум сколько раз можно взять этот апгрейд. Защита от поломки баланса.")]
    [Min(1)] public int maxStacks = 5;
}
