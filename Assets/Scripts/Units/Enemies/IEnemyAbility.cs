using UnityEngine;

/// <summary>
/// Болт-он поведение врага. Навешивается на готового врага в рантайме (WaveSpawner),
/// меняет его «дистанцию остановки» и действие при сближении — превращая обычного
/// преследователя в стрелка / камикадзе и т.п. без отдельных префабов.
/// </summary>
public interface IEnemyAbility
{
    /// <summary>Дистанция, на которой враг останавливается и переходит к действию (≤0 — обычный контактный радиус).</summary>
    float StopRange { get; }

    /// <summary>Вызывается каждый FixedUpdate, когда игрок в пределах StopRange.</summary>
    void Act(EnemyBase self, Transform target, float distance);
}
