using System.Collections;
using UnityEngine;

/// <summary>
/// Таран: на средней дистанции телеграфирует и совершает быстрый рывок в игрока,
/// нанося усиленный урон. Между рывками — восстановление.
/// </summary>
public class ChargerAbility : MonoBehaviour, IEnemyAbility
{
    private const float ChargeSpeed = 16f;
    private const float ChargeDuration = 0.45f;
    private const float TelegraphTime = 0.5f;
    private const float RecoverTime = 1.8f;
    private const float DamageMultiplier = 1.6f;
    private const float HitRange = 0.8f;
    private static readonly Color Tint = new Color(1f, 0.30f, 0.22f);

    private bool _busy;

    public float StopRange => 6f;

    private void Start()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Tint;
        transform.localScale *= 1.2f;
        CyberpunkFX.AttachLight(transform, Tint, intensity: 1.9f, outerRadius: 2.8f);
    }

    public void Act(EnemyBase self, Transform target, float distance)
    {
        if (_busy || target == null) return;
        _busy = true;
        StartCoroutine(Charge(self));
    }

    private IEnumerator Charge(EnemyBase self)
    {
        var player = PlayerMovement.Instance;
        var sr = GetComponentInChildren<SpriteRenderer>();

        self.ExternalControl = true;
        if (self.Body != null) self.Body.linearVelocity = Vector2.zero;

        // Телеграф — мигание перед рывком.
        float t = 0f;
        while (t < TelegraphTime && self.IsAlive)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(Tint, Color.white, Mathf.PingPong(t * 12f, 1f));
            yield return null;
        }

        // Рывок: направление фиксируем в момент старта.
        Vector2 dir = player != null
            ? ((Vector2)player.transform.position - (Vector2)self.transform.position).normalized
            : Vector2.right;
        CyberpunkFX.Kick(dir, 0.12f);

        float end = Time.time + ChargeDuration;
        bool hit = false;
        while (Time.time < end && self.IsAlive)
        {
            if (EnemyBase.Frozen)
            {
                if (self.Body != null) self.Body.linearVelocity = Vector2.zero;
                yield return new WaitForFixedUpdate();
                continue;
            }

            if (self.Body != null) self.Body.linearVelocity = dir * ChargeSpeed;

            if (!hit && player != null && player.IsAlive)
            {
                float d = ((Vector2)player.transform.position - (Vector2)self.transform.position).magnitude;
                if (d <= HitRange)
                {
                    player.TakeDamage(self.Damage * DamageMultiplier);
                    CyberpunkFX.SpawnHitSpark(player.transform.position, Tint);
                    hit = true;
                }
            }
            yield return new WaitForFixedUpdate();
        }

        if (self.Body != null) self.Body.linearVelocity = Vector2.zero;
        self.ExternalControl = false;
        if (sr != null) sr.color = Tint;

        yield return new WaitForSeconds(RecoverTime);
        _busy = false;
    }
}
