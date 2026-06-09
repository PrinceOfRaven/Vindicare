using UnityEngine;

/// <summary>
/// Глобальная сложность забега. Растёт с номером пройденного круга волн
/// (для эндлесс-режима) и постепенно — с прожитым временем. Враги читают
/// эти множители при спавне, спавнер масштабирует количество.
/// Чисто статический: сбрасывается WaveSpawner'ом в начале сцены.
/// </summary>
public static class RunDifficulty
{
    /// <summary>Сколько полных кругов волн уже пройдено (0 — первый круг).</summary>
    public static int LoopCount { get; set; }

    private static float Minutes =>
        HUD.Instance != null ? HUD.Instance.RunTime / 60f : 0f;

    /// <summary>Множитель здоровья врага.</summary>
    public static float HealthMultiplier => 1f + 0.55f * LoopCount + 0.030f * Minutes;

    /// <summary>Множитель урона врага.</summary>
    public static float DamageMultiplier => 1f + 0.20f * LoopCount + 0.012f * Minutes;

    /// <summary>Множитель скорости врага (с потолком, чтобы оставалось играбельно).</summary>
    public static float SpeedMultiplier =>
        Mathf.Min(1f + 0.06f * LoopCount + 0.004f * Minutes, 1.45f);

    /// <summary>Множитель количества врагов в группе спавна.</summary>
    public static float CountMultiplier => 1f + 0.40f * LoopCount;

    public static void Reset()
    {
        LoopCount = 0;
    }
}
