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

    protected virtual void Awake()
    {
        _cooldown = DefaultCooldown;
    }

    public bool IsReady => Time.time >= _nextReadyTime;

    /// <summary>Полный кулдаун в секундах (для отображения таймера).</summary>
    public float CooldownSeconds => _cooldown;

    public float CooldownRemaining01
    {
        get
        {
            if (_cooldown <= 0f) return 0f;
            return Mathf.Clamp01((_nextReadyTime - Time.time) / _cooldown);
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
            _nextReadyTime = Time.time + _cooldown;
        }
    }

    /// <summary>Сработать. Вызывается, только когда способность готова.</summary>
    protected abstract void Activate();
}
