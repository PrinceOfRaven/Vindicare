using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private Slider _hpSlider;
    [SerializeField] private TMP_Text _hpText;

    [Header("XP")]
    [SerializeField] private Slider _xpSlider;
    [SerializeField] private TMP_Text _levelText;

    [Header("Прочее")]
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _killsText;

    private float _runTime;
    private int _kills;

    public float RunTime => _runTime;
    public int Kills => _kills;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (PlayerLevel.Instance != null)
            PlayerLevel.Instance.OnXPChanged -= UpdateXP;
    }

    private void Start()
    {
        if (PlayerLevel.Instance != null)
        {
            PlayerLevel.Instance.OnXPChanged += UpdateXP;
            UpdateXP(PlayerLevel.Instance.CurrentXP, PlayerLevel.Instance.XPToNext);
        }
    }

    private void Update()
    {
        _runTime += Time.deltaTime;

        if (PlayerMovement.Instance != null)
        {
            int hp = PlayerMovement.Instance.Health;
            int max = PlayerMovement.Instance.MaxHealth;
            if (_hpSlider != null) { _hpSlider.maxValue = max; _hpSlider.value = hp; }
            if (_hpText != null) _hpText.text = $"{hp} / {max}";
        }

        if (PlayerLevel.Instance != null && _levelText != null)
            _levelText.text = $"LVL {PlayerLevel.Instance.Level}";

        if (_timerText != null)
        {
            int min = (int)(_runTime / 60);
            int sec = (int)(_runTime % 60);
            _timerText.text = $"{min:00}:{sec:00}";
        }

        if (_killsText != null)
            _killsText.text = $"Убито: {_kills}";
    }

    private void UpdateXP(int current, int needed)
    {
        if (_xpSlider != null) { _xpSlider.maxValue = needed; _xpSlider.value = current; }
    }

    public void RegisterKill()
    {
        _kills++;
    }
}