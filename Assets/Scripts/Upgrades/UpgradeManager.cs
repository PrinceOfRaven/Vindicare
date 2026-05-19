using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Менеджер апгрейдов. Когда игрок получает уровень,
/// выбирает 3 случайных доступных апгрейда из пула,
/// показывает UI, ждёт выбора.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Все доступные апгрейды в игре (перетащи сюда ассеты UpgradeData)")]
    [SerializeField] private List<UpgradeData> _allUpgrades = new();

    [Header("Сколько вариантов показывать при левелапе")]
    [SerializeField, Min(1)] private int _choicesCount = 3;

    [Header("UI окно (заполнится автоматически если найдёт UpgradeUI в сцене)")]
    [SerializeField] private UpgradeUI _upgradeUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (_upgradeUI == null) _upgradeUI = FindObjectOfType<UpgradeUI>(true);
        if (_upgradeUI != null) _upgradeUI.Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Зовёт PlayerLevel при левелапе.</summary>
    public void OfferUpgrades()
    {
        // Собираем апгрейды, которые ещё можно взять (не достигнут maxStacks)
        var available = new List<UpgradeData>();
        foreach (var up in _allUpgrades)
        {
            if (up == null) continue;
            if (PlayerStats.Instance == null || PlayerStats.Instance.CanTake(up))
                available.Add(up);
        }

        if (available.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] Нет доступных апгрейдов!");
            return;
        }

        // Выбираем N случайных без повторов
        int toPick = Mathf.Min(_choicesCount, available.Count);
        var picked = new List<UpgradeData>();
        for (int i = 0; i < toPick; i++)
        {
            int idx = Random.Range(0, available.Count);
            picked.Add(available[idx]);
            available.RemoveAt(idx);
        }

        // Замораживаем игру и показываем UI
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
