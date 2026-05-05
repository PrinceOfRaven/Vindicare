using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class LevelTransition : MonoBehaviour
{
    public static LevelTransition _instance;
    [SerializeField] private Animator fade;
    private const string _path = "UI/Fader";
    public bool isFading { get; private set;  }

    private Action _fadeInCallback;
    private Action _fadeOutCallback;


    public static LevelTransition Instance 
    {
        get 
        {
            if (_instance == null) 
            {
                var prefab = Resources.Load<LevelTransition>(_path);
                _instance = Instantiate(prefab);
                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }

    public void FadeIn(Action fadeInCallBack) 
    {
        isFading = true;
        _fadeInCallback = fadeInCallBack;
        fade.SetBool("faded", true);
    }


    public void FadeOut(Action fadeOutCallBack)
    {
        isFading = true;
        _fadeOutCallback = fadeOutCallBack;
        fade.SetBool("faded", false);
    }

    private void FadeInOver() 
    {
        _fadeInCallback?.Invoke();
        _fadeInCallback = null;
        isFading = false;
    }

    private void FadeOutOver()
    {
        _fadeOutCallback?.Invoke();
        _fadeOutCallback = null;
        isFading = false;
    }

}
