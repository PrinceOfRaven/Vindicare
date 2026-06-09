using UnityEngine;

/// <summary>
/// Открывает активные способности игрока по мере роста уровня — чтобы старт был
/// простым, а получение новой способности ощущалось как награда.
/// Бомба доступна с самого начала (отдельный компонент), здесь — остальные.
/// Вешается на игрока в PlayerMovement.Start.
/// </summary>
public class AbilityUnlocker : MonoBehaviour
{
    private bool _dash, _shield, _overdrive, _turret;

    private void Start()
    {
        if (PlayerLevel.Instance != null)
        {
            PlayerLevel.Instance.OnLevelUp -= OnLevelUp;
            PlayerLevel.Instance.OnLevelUp += OnLevelUp;
            CheckUnlocks(PlayerLevel.Instance.Level);
        }
    }

    private void OnDestroy()
    {
        if (PlayerLevel.Instance != null)
            PlayerLevel.Instance.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int level) => CheckUnlocks(level);

    private void CheckUnlocks(int level)
    {
        if (!_dash && level >= 2)      { _dash = true;      Unlock<DashAbility>("РЫВОК", "SHIFT"); }
        if (!_shield && level >= 3)    { _shield = true;    Unlock<ShieldAbility>("ЩИТ", "Q"); }
        if (!_overdrive && level >= 5) { _overdrive = true; Unlock<OverdriveAbility>("ОВЕРДРАЙВ", "R"); }
        if (!_turret && level >= 7)    { _turret = true;    Unlock<TurretAbility>("ТУРЕЛЬ", "F"); }
    }

    private void Unlock<T>(string abilityName, string key) where T : PlayerAbility
    {
        if (GetComponent<T>() == null) gameObject.AddComponent<T>();
        if (HUD.Instance != null) HUD.Instance.OnAbilityUnlocked(abilityName, key);
    }
}
