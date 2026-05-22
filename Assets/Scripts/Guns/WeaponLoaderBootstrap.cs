using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Автоматически вешает PlayerWeaponLoader на игрока в SampleScene без правки самой сцены.
/// Это нужно, чтобы выбор оружия в PlayerHub применялся на персонажа в рейде,
/// при этом сама сцена в гите остаётся нетронутой.
/// </summary>
public static class WeaponLoaderBootstrap
{
    private const string RaidSceneName = "SampleScene";
    private const string PlayerTag = "Player";
    private const string FallbackWeaponResourcePath = "Weapons/PistolData";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != RaidSceneName) return;

        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        if (player == null)
        {
            Debug.LogWarning($"[WeaponLoaderBootstrap] В сцене '{RaidSceneName}' не найден объект с тегом '{PlayerTag}'.");
            return;
        }
        if (player.GetComponent<PlayerWeaponLoader>() != null) return;

        var loader = player.AddComponent<PlayerWeaponLoader>();
        var fallback = Resources.Load<WeaponData>(FallbackWeaponResourcePath);
        loader.Configure(player.transform, fallback);
    }
}
