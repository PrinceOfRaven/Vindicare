using System.Collections;
using UnityEngine;

/// <summary>Камикадзе: добежав до игрока, телеграфирует и взрывается по площади.</summary>
public class BomberAbility : MonoBehaviour, IEnemyAbility
{
    private const float ExplodeRadius = 1.9f;
    private const float DamageMultiplier = 2.0f;
    private const float FuseTime = 0.4f;
    private static readonly Color Tint = new Color(1f, 0.5f, 0.12f);

    private bool _armed;

    public float StopRange => 1.05f;

    private void Start()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Tint;

        // Заметный признак: оранжевый ореол + чуть крупнее.
        transform.localScale *= 1.15f;
        CyberpunkFX.AttachLight(transform, Tint, intensity: 2.0f, outerRadius: 2.6f);
    }

    public void Act(EnemyBase self, Transform target, float distance)
    {
        if (_armed) return;
        _armed = true;
        StartCoroutine(Detonate(self));
    }

    private IEnumerator Detonate(EnemyBase self)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        float t = 0f;
        while (t < FuseTime)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = Color.Lerp(Tint, Color.white, Mathf.PingPong(t * 10f, 1f));
            yield return null;
        }

        var player = PlayerMovement.Instance;
        if (player != null && player.IsAlive)
        {
            float d = ((Vector2)player.transform.position - (Vector2)self.transform.position).magnitude;
            if (d <= ExplodeRadius) player.TakeDamage(self.Damage * DamageMultiplier);
        }

        CyberpunkFX.SpawnExplosion(self.transform.position, ExplodeRadius, Tint);
        CyberpunkFX.Shake(0.3f, 0.25f);
        AudioFX.Explosion();

        if (self.IsAlive) self.Kill(); // погибает со всеми дропами, без числа урона
    }
}
