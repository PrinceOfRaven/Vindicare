using System;
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    public static PlayerLevel Instance { get; private set; }

    [Header("Кривая опыта: XP до следующего уровня = baseXP + level * perLevelXP")]
    [SerializeField, Min(1)] private int _baseXP = 5;
    [SerializeField, Min(1)] private int _perLevelXP = 3;

    private int _currentXP;
    private int _currentLevel = 1;
    private int _xpToNext;

    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;

    public int Level => _currentLevel;
    public int CurrentXP => _currentXP;
    public int XPToNext => _xpToNext;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _xpToNext = ComputeXPRequirement(_currentLevel);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private int ComputeXPRequirement(int level)
    {
        return _baseXP + (level - 1) * _perLevelXP;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        _currentXP += amount;

        while (_currentXP >= _xpToNext)
        {
            _currentXP -= _xpToNext;
            _currentLevel++;
            _xpToNext = ComputeXPRequirement(_currentLevel);

            OnLevelUp?.Invoke(_currentLevel);

            CyberpunkFX.Shake(0.18f, 0.25f);

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OfferUpgrades();
        }

        OnXPChanged?.Invoke(_currentXP, _xpToNext);
    }
}
