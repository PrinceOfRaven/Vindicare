using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("Содержимое")]
    [SerializeField] private GameObject _content;

    [Header("Итоги")]
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _timeText;
    [SerializeField] private TMP_Text _killsText;

    [Header("Кнопки")]
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;
    [SerializeField] private string _mainMenuScene = "MainMenu";

    private TMP_Text _titleText;
    private bool _styled;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
        if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);

    }

    private void StyleContent()
    {
        var contentRt = _content.transform as RectTransform;
        if (contentRt == null) return;

        CyberpunkUI.AddNeonBorder(contentRt, new Color(1f, 0.18f, 0.80f) * 2.2f, 3f);

        var statColor = Color.white;
        StyleStat(_levelText, statColor);
        StyleStat(_timeText,  statColor);
        StyleStat(_killsText, statColor);

        foreach (var t in _content.GetComponentsInChildren<TMP_Text>(true))
        {
            var upper = (t.text ?? string.Empty).ToUpperInvariant();
            if (upper.Contains("GAME OVER") || upper.Contains("ИГРА ОКОНЧЕНА")
                || upper.Contains("КОНЕЦ") || upper.Contains("ПОГИБЛИ") || upper.Contains("СМЕРТЬ"))
            {
                _titleText = t;
                CyberpunkUI.StyleTMP(t, new Color(1f, 0.18f, 0.80f) * 1.8f, Color.black, 0.35f);
                t.raycastTarget = false;
                break;
            }
        }

        StyleButton(_restartButton, new Color(0f, 1f, 0.88f) * 2.2f);
        StyleButton(_menuButton,    new Color(0f, 0.85f, 1f) * 2.2f);
    }

    private static void StyleStat(TMP_Text text, Color color)
    {
        if (text == null) return;
        CyberpunkUI.StyleTMP(text, color, Color.black, 0.25f);
        text.raycastTarget = false;
    }

    private static void StyleButton(Button btn, Color borderColor)
    {
        if (btn == null) return;
        var rt = btn.transform as RectTransform;
        if (rt == null) return;
        CyberpunkUI.AddNeonBorder(rt, borderColor, 2f);
        foreach (var t in btn.GetComponentsInChildren<TMP_Text>(true))
        {
            CyberpunkUI.StyleTMP(t, Color.white, Color.black, 0.25f);
            t.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show()
    {
        if (_content != null) _content.SetActive(true);

        if (!_styled && _content != null)
        {
            StyleContent();
            _styled = true;
        }

        if (PlayerLevel.Instance != null && _levelText != null)
            _levelText.text = $"Уровень: {PlayerLevel.Instance.Level}";

        if (HUD.Instance != null)
        {
            if (_timeText != null)
            {
                int min = (int)(HUD.Instance.RunTime / 60);
                int sec = (int)(HUD.Instance.RunTime % 60);
                _timeText.text = $"Время: {min:00}:{sec:00}";
            }
            if (_killsText != null)
                _killsText.text = $"Убито: {HUD.Instance.Kills}";
        }

        if (_titleText != null && isActiveAndEnabled)
            StartCoroutine(GlitchTitle());

        Time.timeScale = 0f;
    }

    private IEnumerator GlitchTitle()
    {
        var rt = _titleText.transform as RectTransform;
        if (rt == null) yield break;

        Vector2 basePos = rt.anchoredPosition;
        Color baseColor = _titleText.color;
        float dur = 0.45f;
        float t = 0f;
        while (t < dur)
        {
            _titleText.alpha = Random.value < 0.5f ? 0.2f : 1f;
            rt.anchoredPosition = basePos + new Vector2(Random.Range(-3f, 3f), Random.Range(-2f, 2f));
            yield return new WaitForSecondsRealtime(0.04f);
            t += 0.04f;
        }
        rt.anchoredPosition = basePos;
        _titleText.alpha = 1f;
        _titleText.color = baseColor;
    }

    public void Restart()
    {
        AudioFX.UIClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToMenu()
    {
        AudioFX.UIClick();
        Time.timeScale = 1f;
        CallTransit.Instance.LoadScene("MainMenu");
    }
}
