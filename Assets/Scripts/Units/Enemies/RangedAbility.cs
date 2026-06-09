using UnityEngine;

/// <summary>Стрелок: держит дистанцию и периодически пускает снаряды в игрока.</summary>
public class RangedAbility : MonoBehaviour, IEnemyAbility
{
    private const float Interval = 1.4f;
    private const float ProjectileSpeed = 7f;
    private static readonly Color Tint = new Color(0.72f, 0.42f, 1f);

    private float _nextShot;

    public float StopRange => 5.5f;

    private void Start()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = Tint;

        // Заметный признак: фиолетовый ореол + чуть крупнее (ореол не стирается HitFlash'ем).
        transform.localScale *= 1.18f;
        CyberpunkFX.AttachLight(transform, Tint, intensity: 1.8f, outerRadius: 2.8f);

        _nextShot = Time.time + Random.Range(0.2f, Interval); // рассинхрон залпов
    }

    public void Act(EnemyBase self, Transform target, float distance)
    {
        if (target == null || Time.time < _nextShot) return;
        _nextShot = Time.time + Interval;

        Vector2 dir = (Vector2)target.position - (Vector2)self.transform.position;
        EnemyProjectile.Spawn(self.transform.position, dir, ProjectileSpeed, self.Damage, Tint);
        CyberpunkFX.MuzzleFlash(self.transform.position, Tint);
    }
}
