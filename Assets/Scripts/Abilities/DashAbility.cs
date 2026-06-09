using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Рывок (Shift): быстрый бросок в сторону движения с краткой неуязвимостью.</summary>
public class DashAbility : PlayerAbility
{
    private const float _dashSpeed = 26f;
    private const float _dashDuration = 0.16f;

    public override string DisplayName => "РЫВОК";
    public override string KeyLabel => "Shift";
    public override Key Hotkey => Key.LeftShift;
    public override Color ThemeColor => CyberpunkFX.Cyan;
    protected override float DefaultCooldown => 3f;

    protected override void Activate()
    {
        var player = PlayerMovement.Instance;
        if (player == null) return;

        Vector2 dir = player.MoveInput.sqrMagnitude > 0.01f
            ? player.MoveInput
            : player.FacingDirection;

        player.Dash(dir, _dashSpeed, _dashDuration);
        AudioFX.Dash();
        CyberpunkFX.Kick(dir, 0.18f);
    }
}
