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

    [Header("Стилизация (cyberpunk)")]
    [Tooltip("Цвет HP-бара при полном здоровье")]
    [SerializeField] private Color _hpHighColor = new Color(0.0f, 1.0f, 0.66f);
    [Tooltip("Цвет HP-бара при низком здоровье")]
    [SerializeField] private Color _hpLowColor = new Color(1.0f, 0.20f, 0.30f);
    [Tooltip("Цвет XP-бара")]
    [SerializeField] private Color _xpColor = new Color(0.0f, 0.85f, 1.0f);

    private float _runTime;
    private int _kills;
    private float _hpDisplay;
    private float _xpDisplay;
    private Image _hpFillImg;
    private Image _xpFillImg;

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

        // Принудительная нормализация HUD на случай кривых якорей/позиций в сцене
        NormalizeHUD();

        if (_hpSlider != null && _hpSlider.fillRect != null)
            _hpFillImg = _hpSlider.fillRect.GetComponent<Image>();
        if (_xpSlider != null && _xpSlider.fillRect != null)
        {
            _xpFillImg = _xpSlider.fillRect.GetComponent<Image>();
            if (_xpFillImg != null) _xpFillImg.color = _xpColor;
        }

        // Усиливаем читаемость HP-цифр поверх заливки
        if (_hpText != null)
        {
            _hpText.outlineColor = Color.black;
            _hpText.outlineWidth = 0.25f;
        }
    }

    /// <summary>
    /// Принудительно выправляет ректы HP/XP-баров и подложку.
    /// Запускается каждый раз при загрузке сцены, перетирая любой кривой initial state.
    /// </summary>
    private void NormalizeHUD()
    {
        if (_hpSlider != null)
        {
            _hpSlider.enabled = true;
            NormalizeBarRect(_hpSlider,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                pivot:     new Vector2(0f, 1f),
                pos:       new Vector2(20f, -20f),
                size:      new Vector2(320f, 28f));
            StretchSliderInternals(_hpSlider);
            EnsureBackdrop(_hpSlider, new Color(0.05f, 0.02f, 0.08f, 0.7f));
        }

        if (_xpSlider != null)
        {
            _xpSlider.enabled = true;
            NormalizeBarRect(_xpSlider,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot:     new Vector2(0.5f, 0f),
                pos:       new Vector2(0f, 20f),
                size:      new Vector2(-40f, 16f));
            StretchSliderInternals(_xpSlider);
            EnsureBackdrop(_xpSlider, new Color(0.05f, 0.05f, 0.12f, 0.7f));
        }
    }

    private static void NormalizeBarRect(Slider slider, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = slider.transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void StretchSliderInternals(Slider slider)
    {
        var fill = slider.fillRect;
        if (fill != null)
        {
            StretchToParent(fill);
            // Растягиваем и Fill Area (родитель Fill) на всю площадь слайдера
            if (fill.parent is RectTransform fillArea)
                StretchToParent(fillArea);
        }
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EnsureBackdrop(Slider slider, Color color)
    {
        var existing = slider.transform.Find("Backdrop");
        if (existing != null) return;

        var bg = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(slider.transform, false);
        bg.transform.SetAsFirstSibling();
        StretchToParent((RectTransform)bg.transform);
        var img = bg.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void Update()
    {
        _runTime += Time.deltaTime;

        if (PlayerMovement.Instance != null)
        {
            int hp = PlayerMovement.Instance.Health;
            int max = PlayerMovement.Instance.MaxHealth;
            // Плавный лерп слайдера
            _hpDisplay = Mathf.MoveTowards(_hpDisplay, hp, Mathf.Max(max * 2f, 20f) * Time.deltaTime);
            if (_hpSlider != null) { _hpSlider.maxValue = max; _hpSlider.value = _hpDisplay; }
            if (_hpText != null) _hpText.text = $"{hp} / {max}";

            // Градиент: зелёно-неон → красный при низком HP
            if (_hpFillImg != null && max > 0)
            {
                float r = Mathf.Clamp01((float)hp / max);
                _hpFillImg.color = Color.Lerp(_hpLowColor, _hpHighColor, r);
            }
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

        // Плавное движение XP-бара
        if (_xpSlider != null)
        {
            _xpDisplay = Mathf.MoveTowards(_xpDisplay, _xpSlider.value,
                Mathf.Max(_xpSlider.maxValue * 1.5f, 5f) * Time.deltaTime);
        }
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