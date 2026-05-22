using UnityEngine;
using UnityEngine.SceneManagement;

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
            return;
        }
        if (player.GetComponent<PlayerWeaponLoader>() != null) return;

        var loader = player.AddComponent<PlayerWeaponLoader>();
        var fallback = Resources.Load<WeaponData>(FallbackWeaponResourcePath);
        loader.Configure(player.transform, fallback);
    }
}
