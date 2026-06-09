using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Все доступные апгрейды в игре (перетащи сюда ассеты UpgradeData)")]
    [SerializeField] private List<UpgradeData> _allUpgrades = new();

    [Header("Сколько вариантов показывать при левелапе")]
    [SerializeField, Min(1)] private int _choicesCount = 3;

    [Header("UI окно (заполнится автоматически если найдёт UpgradeUI в сцене)")]
    [SerializeField] private UpgradeUI _upgradeUI;

    private readonly List<UpgradeData> _runtimeUpgrades = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        EnsureRuntimeUpgrades();

        if (_upgradeUI == null) _upgradeUI = FindObjectOfType<UpgradeUI>(true);
        if (_upgradeUI != null) _upgradeUI.Hide();
    }

    // Добавляет продвинутые апгрейды в пул в рантайме (чтобы не плодить .asset-файлы и связывание в сцене).
    private void EnsureRuntimeUpgrades()
    {
        AddRuntime(UpgradeData.UpgradeType.Crit,              "Крит. удар",   "+7% шанс крита (×2 урона)",        5);
        AddRuntime(UpgradeData.UpgradeType.Lifesteal,         "Вампиризм",    "+1 HP за каждое убийство",         5);
        AddRuntime(UpgradeData.UpgradeType.XpGain,            "Жажда опыта",  "+15% получаемого опыта",          5);
        AddRuntime(UpgradeData.UpgradeType.CooldownReduction, "Ускорение",    "-12% к перезарядке способностей", 5);
        AddRuntime(UpgradeData.UpgradeType.Dodge,             "Уклонение",    "+5% шанс уклониться от урона",     5);
        AddRuntime(UpgradeData.UpgradeType.Armor,             "Броня",        "-2 к получаемому урону",           5);
        AddRuntime(UpgradeData.UpgradeType.Regen,             "Регенерация",  "+0.5 HP в секунду",               5);
    }

    private void AddRuntime(UpgradeData.UpgradeType type, string name, string desc, int maxStacks)
    {
        foreach (var u in _allUpgrades)
            if (u != null && u.type == type) return; // уже есть как ассет — не дублируем

        var data = ScriptableObject.CreateInstance<UpgradeData>();
        data.type = type;
        data.upgradeName = name;
        data.description = desc;
        data.maxStacks = maxStacks;
        data.icon = null;
        _allUpgrades.Add(data);
        _runtimeUpgrades.Add(data);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        // Чистим рантайм-созданные апгрейды, чтобы не текли при перезапуске сцены.
        foreach (var u in _runtimeUpgrades)
        {
            if (u == null) continue;
            _allUpgrades.Remove(u);
            Destroy(u);
        }
        _runtimeUpgrades.Clear();
    }

    public void OfferUpgrades()
    {
        var available = new List<UpgradeData>();
        foreach (var up in _allUpgrades)
        {
            if (up == null) continue;
            if (PlayerStats.Instance == null || PlayerStats.Instance.CanTake(up))
                available.Add(up);
        }

        if (available.Count == 0)
        {
            return;
        }

        int toPick = Mathf.Min(_choicesCount, available.Count);
        var picked = new List<UpgradeData>();
        for (int i = 0; i < toPick; i++)
        {
            int idx = Random.Range(0, available.Count);
            picked.Add(available[idx]);
            available.RemoveAt(idx);
        }

        Time.timeScale = 0f;
        if (_upgradeUI != null) _upgradeUI.Show(picked, OnUpgradePicked);
    }

    private void OnUpgradePicked(UpgradeData chosen)
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.ApplyUpgrade(chosen);
        if (_upgradeUI != null) _upgradeUI.Hide();
        Time.timeScale = 1f;
    }
}
