using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Турель (F): ставит на месте игрока авто-турель, бьющую ближайших врагов лучом.</summary>
public class TurretAbility : PlayerAbility
{
    public override string DisplayName => "ТУРЕЛЬ";
    public override string KeyLabel => "F";
    public override Key Hotkey => Key.F;
    public override Color ThemeColor => CyberpunkFX.Lime;
    protected override float DefaultCooldown => 15f;

    protected override void Activate()
    {
        var player = PlayerMovement.Instance;
        if (player == null) return;

        var go = new GameObject("Turret");
        go.transform.position = player.transform.position;
        var turret = go.AddComponent<Turret>();
        turret.Init(ThemeColor);

        AudioFX.UIClick();
        CyberpunkFX.Shockwave(player.transform.position, 1.6f, ThemeColor);
    }
}
