using UnityEngine;

[CreateAssetMenu(fileName = "Upgrade_", menuName = "Roguelike/Upgrade")]
public class UpgradeData : ScriptableObject
{
    public enum UpgradeType
    {
        Damage,
        FireRate,
        MoveSpeed,
        MaxHealth,
        PickupRadius,
        ProjectileCount,
        BombDamage,
        Crit,
        Lifesteal,
        XpGain,
        CooldownReduction,
        Dodge,
        Armor,
        Regen,
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
