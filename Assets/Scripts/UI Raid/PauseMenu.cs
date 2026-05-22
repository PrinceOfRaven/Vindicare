using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _pauseMenu;
    private bool _isPaused = false;
    private bool _styled = false;
    private PlayerActionsControl _inputActions;

    private void Awake()
    {
        _inputActions = new PlayerActionsControl();
        _pauseMenu.SetActive(false);
    }

    private void OnEnable()
    {
        _inputActions.UI.OnPause.performed += OnPauseInput;
        _inputActions.UI.Enable();
        SetPauseState(false);
    }

    private void OnDisable()
    {
        _inputActions.UI.OnPause.performed -= OnPauseInput;
        _inputActions.UI.Disable();
    }

    private void OnPauseInput(InputAction.CallbackContext ctx) => TogglePause();

    private void TogglePause()
    {
        AudioFX.UIClick();
        SetPauseState(!_isPaused);
    }

    private void SetPauseState(bool paused)
    {
        _isPaused = paused;
        _pauseMenu.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;

        if (paused && !_styled)
        {
            StylePauseMenu();
            _styled = true;
        }
    }

    private void StylePauseMenu()
    {
        var panelRt = _pauseMenu.transform as RectTransform;
        if (panelRt == null) return;

        CyberpunkUI.AddNeonBorder(panelRt, new Color(0f, 1f, 0.88f) * 2.2f, 3f);

        foreach (var btn in _pauseMenu.GetComponentsInChildren<Button>(true))
        {
            var rt = btn.transform as RectTransform;
            if (rt != null)
                CyberpunkUI.AddNeonBorder(rt, new Color(1f, 0.18f, 0.80f) * 2.2f, 2f);

            foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true))
            {
                CyberpunkUI.StyleTMP(t, Color.white, Color.black, 0.25f);
                t.raycastTarget = false;
            }
        }

        foreach (var t in _pauseMenu.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.GetComponentInParent<Button>() != null) continue;
            CyberpunkUI.StyleTMP(t, new Color(0f, 1f, 0.88f) * 1.8f, Color.black, 0.3f);
            t.raycastTarget = false;
        }
    }

    public void ReturntoGame()
    {
        AudioFX.UIClick();
        SetPauseState(false);
    }

    public void OnMainMenu()
    {
        AudioFX.UIClick();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
