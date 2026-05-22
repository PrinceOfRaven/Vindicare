using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{

    [SerializeField] private GameObject _pauseMenu;
    private bool _isPaused = false;
    private PlayerActionsControl _inputActions;

    private void Awake()
    {
        _inputActions = new PlayerActionsControl();
        _pauseMenu.SetActive(false);
    }

    private void OnEnable()
    {
        _inputActions.UI.OnPause.performed += ctx => TogglePause();
        _inputActions.UI.Enable();
        SetPauseState(false);
    }

    private void OnDisable()
    {
        _inputActions.UI.OnPause.performed -= ctx => TogglePause();
        _inputActions.UI.Disable();
    }

    private void TogglePause() => SetPauseState(!_isPaused);

    private void SetPauseState(bool paused)
    {
        _isPaused = paused;
        _pauseMenu.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
    }

    public void ReturntoGame() => SetPauseState(false);

    public void OnMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
