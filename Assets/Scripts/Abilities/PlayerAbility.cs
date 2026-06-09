using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Базовый класс активной способности игрока: хоткей + кулдаун + поллинг клавиатуры.
/// Наследники реализуют <see cref="Activate"/>. Компоненты вешаются на игрока в рантайме.
/// </summary>
public abstract class PlayerAbility : MonoBehaviour, IAbilityDisplay
{
    public abstract string DisplayName { get; }
    public abstract string KeyLabel { get; }
    public abstract Key Hotkey { get; }
    public abstract Color ThemeColor { get; }
    public virtual Sprite Icon => null;

    protected float _cooldown = 6f;

    /// <summary>Кулдаун по умолчанию — выставляется в Awake (компоненты вешаются в рантайме).</summary>
    protected virtual float DefaultCooldown => 6f;

    private float _nextReadyTime;
    private float _lastCooldown;

    protected virtual void Awake()
    {
        _cooldown = DefaultCooldown;
        _lastCooldown = _cooldown;
    }

    public bool IsReady => Time.time >= _nextReadyTime;

    /// <summary>Фактический кулдаун последнего применения (с учётом «Ускорения»).</summary>
    public float CooldownSeconds => _lastCooldown > 0f ? _lastCooldown : _cooldown;

    public float CooldownRemaining01
    {
        get
        {
            float cd = CooldownSeconds;
            if (cd <= 0f) return 0f;
            return Mathf.Clamp01((_nextReadyTime - Time.time) / cd);
        }
    }

    protected virtual void Update()
    {
        if (Time.timeScale == 0f) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (IsReady && kb[Hotkey].wasPressedThisFrame)
        {
            Activate();
            float haste = PlayerStats.Instance != null ? PlayerStats.Instance.AbilityCooldownMultiplier : 1f;
            _lastCooldown = _cooldown * haste;
            _nextReadyTime = Time.time + _lastCooldown;
        }
    }

    /// <summary>Сработать. Вызывается, только когда способность готова.</summary>
    protected abstract void Activate();
}
