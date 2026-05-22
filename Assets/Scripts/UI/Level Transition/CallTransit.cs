using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CallTransit : MonoBehaviour
{
    private bool _isLoading;
    private static CallTransit _instance;
    private Coroutine _currentCoroutine;

    public static CallTransit Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LevelTransition.Instance.FadeIn(null);
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading && SceneManager.GetActiveScene().name != sceneName)
        {
            StopCoroutine(_currentCoroutine);
        }

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(SceneLoadRoutine(sceneName));
    }

    private IEnumerator SceneLoadRoutine(string sceneName)
    {
        _isLoading = true;

        bool waitFading = true;
        LevelTransition.Instance.FadeOut(() => waitFading = false);
        while (waitFading) yield return null;

        var async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;
        while (async.progress < 0.9f) yield return null;

        Time.timeScale = 1f;

        async.allowSceneActivation = true;
        yield return null;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _isLoading = false;
        waitFading = true;
        LevelTransition.Instance.FadeIn(() => waitFading = false);
    }
}
