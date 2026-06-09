using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Овердрайв (R): временный буст урона, скорострельности и скорости движения.</summary>
public class OverdriveAbility : PlayerAbility
{
    private const float _duration = 5f;
    private const float _damageMul = 1.5f;
    private const float _fireRateMul = 1.8f;
    private const float _moveMul = 1.3f;

    public override string DisplayName => "ОВЕРДРАЙВ";
    public override string KeyLabel => "R";
    public override Key Hotkey => Key.R;
    public override Color ThemeColor => CyberpunkFX.Amber;
    protected override float DefaultCooldown => 14f;

    protected override void Activate()
    {
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.ApplyTempBuff(_damageMul, _fireRateMul, _moveMul, _duration);
        AudioFX.LevelUp();

        var player = PlayerMovement.Instance;
        if (player != null) StartCoroutine(OverdriveVisual(player.transform, _duration));
    }

    private IEnumerator OverdriveVisual(Transform player, float duration)
    {
        var light = CyberpunkFX.AttachLight(player, ThemeColor, intensity: 1.7f, outerRadius: 5f, innerRadius: 0.4f);
        CyberpunkFX.Shockwave(player.position, 2.5f, ThemeColor);

        var sr = PlayerMovement.Instance != null ? PlayerMovement.Instance.BodySprite : null;
        Color baseColor = sr != null ? sr.color : Color.white;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(t * 14f);
                sr.color = Color.Lerp(baseColor, ThemeColor, 0.35f * k);
            }
            yield return null;
        }

        if (sr != null) sr.color = baseColor;
        if (light != null) Destroy(light.gameObject);
    }
}
