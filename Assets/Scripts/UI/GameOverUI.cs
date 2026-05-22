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


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (_content != null) _content.SetActive(false);

        if (_restartButton != null) _restartButton.onClick.AddListener(Restart);
        if (_menuButton != null) _menuButton.onClick.AddListener(ToMenu);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show()
    {
        if (_content != null) _content.SetActive(true);

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

        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToMenu()
    {
        Time.timeScale = 1f;
        CallTransit.Instance.LoadScene("MainMenu");
    }
}