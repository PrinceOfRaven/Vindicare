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
    [SerializeField] private Color _hpHighColor = new Color(1.0f, 0.15f, 0.20f);
    [Tooltip("Цвет HP-бара при низком здоровье")]
    [SerializeField] private Color _hpLowColor = new Color(0.55f, 0.0f, 0.08f);
    [Tooltip("Цвет XP-бара")]
    [SerializeField] private Color _xpColor = new Color(0.0f, 0.85f, 1.0f);

    [Header("Иконки (опционально — дропни спрайты hivefall_night_heart_256 и hivefall_night_enrgcell_256)")]
    [SerializeField] private Sprite _hpIcon;
    [SerializeField] private Sprite _xpIcon;

    private float _runTime;
    private int _kills;
    private float _hpDisplay;
    private float _xpDisplay;   // отображаемое заполнение бара, 0..1
    private float _xpTarget;    // реальное заполнение бара, 0..1
    private bool _xpFlushing;   // идёт анимация «дозаполнить до конца и сбросить»
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
        {
            PlayerLevel.Instance.OnXPChanged -= UpdateXP;
            PlayerLevel.Instance.OnLevelUp   -= HandleLevelUp;
        }
    }

    private void Start()
    {
        if (PlayerLevel.Instance != null)
        {
            PlayerLevel.Instance.OnXPChanged += UpdateXP;
            PlayerLevel.Instance.OnLevelUp   += HandleLevelUp;
            UpdateXP(PlayerLevel.Instance.CurrentXP, PlayerLevel.Instance.XPToNext);
        }

        // Принудительная нормализация HUD на случай кривых якорей/позиций в сцене
        NormalizeHUD();

        if (_hpSlider != null && _hpSlider.fillRect != null)
            _hpFillImg = _hpSlider.fillRect.GetComponent<Image>();
        if (_xpSlider != null)
        {
            // Бар работает в нормализованной шкале 0..1 — не зависит от меняющегося порога опыта
            _xpSlider.wholeNumbers = false;
            _xpSlider.minValue = 0f;
            _xpSlider.maxValue = 1f;

            // NormalizeHUD растянул fillRect на всю ширину слайдера. Slider не
            // перерисует заливку, пока value реально не изменится, поэтому без
            // этого тоггла бар висит «полным» до получения первого опыта.
            _xpSlider.value = 1f;
            _xpSlider.value = 0f;

            if (_xpSlider.fillRect != null)
            {
                _xpFillImg = _xpSlider.fillRect.GetComponent<Image>();
                if (_xpFillImg != null) _xpFillImg.color = _xpColor;
            }
        }

        // Принудительно перезаписываем цвета HP — SerializeField-значения в сцене
        // могут хранить старые инспекторные значения и перебивать code-defaults.
        _hpHighColor = new Color(1f, 0.15f, 0.20f);
        _hpLowColor  = new Color(0.55f, 0.0f, 0.08f);

        CyberpunkUI.StyleTMP(_hpText,    Color.white,                Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_levelText, new Color(0f, 0.85f, 1f),   Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_timerText, Color.white,                Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_killsText, new Color(0.45f, 1f, 0.3f), Color.black, 0.25f);

        if (_timerText != null) _timerText.fontSize = 30f;
        if (_killsText != null) _killsText.fontSize = 22f;
        if (_levelText != null) _levelText.fontSize = 18f;
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
                pos:       new Vector2(8f, -8f),
                size:      new Vector2(220f, 22f));
            StretchSliderInternals(_hpSlider);
            EnsureBackdrop(_hpSlider, new Color(0.12f, 0.0f, 0.02f, 0.75f));

            // HP-текст — вынести вправо от бара, не накладывать на заливку
            if (_hpText != null)
            {
                var hpTxtRt = _hpText.transform as RectTransform;
                if (hpTxtRt != null)
                {
                    hpTxtRt.anchorMin        = new Vector2(1f, 0.5f);
                    hpTxtRt.anchorMax        = new Vector2(1f, 0.5f);
                    hpTxtRt.pivot            = new Vector2(0f, 0.5f);
                    hpTxtRt.anchoredPosition = new Vector2(8f, 0f);
                    hpTxtRt.sizeDelta        = new Vector2(90f, 22f);
                }
                _hpText.alignment     = TMPro.TextAlignmentOptions.Left;
                _hpText.raycastTarget = false;
            }
        }

        if (_xpSlider != null)
        {
            _xpSlider.enabled = true;
            NormalizeBarRect(_xpSlider,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot:     new Vector2(0.5f, 0f),
                pos:       new Vector2(0f, 6f),
                size:      new Vector2(-16f, 14f));
            StretchSliderInternals(_xpSlider);
            EnsureBackdrop(_xpSlider, new Color(0.05f, 0.05f, 0.12f, 0.7f));
            CyberpunkUI.AddNeonBorder((RectTransform)_xpSlider.transform, new Color(0f, 0.85f, 1f) * 2.2f, 2f);
            AddBarIcon(_xpSlider, _xpIcon, new Color(0f, 0.85f, 1f));
        }

        NormalizeTextRect(_levelText,
            anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),
            pivot:     new Vector2(0f, 0f),
            pos:       new Vector2(8f, 24f),
            size:      new Vector2(120f, 22f));

        NormalizeTextRect(_timerText,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            pivot:     new Vector2(0.5f, 1f),
            pos:       new Vector2(0f, -8f),
            size:      new Vector2(190f, 40f));

        NormalizeTextRect(_killsText,
            anchorMin: new Vector2(1f, 1f),
            anchorMax: new Vector2(1f, 1f),
            pivot:     new Vector2(1f, 1f),
            pos:       new Vector2(-8f, -8f),
            size:      new Vector2(220f, 34f));

    }

    /// <summary>Добавляет дочернюю Image-иконку внутри слайдера у левого края (если спрайт назначен).</summary>
    private static void AddBarIcon(Slider slider, Sprite icon, Color tint)
    {
        if (slider == null || icon == null) return;
        if (slider.transform.Find("Icon_" + icon.name) != null) return; // уже есть
        CyberpunkUI.AddIcon(
            parent: slider.transform,
            sprite: icon,
            color: tint,
            size: new Vector2(22f, 22f),
            anchoredPos: new Vector2(4f, 0f),
            anchorMin: new Vector2(0f, 0.5f),
            anchorMax: new Vector2(0f, 0.5f),
            pivot: new Vector2(0f, 0.5f));
    }

    private static void NormalizeTextRect(TMP_Text text, Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (text == null) return;
        var rt = text.transform as RectTransform;
        if (rt == null) return;
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
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

        // Плавное движение XP-бара (нормализованная шкала 0..1).
        // unscaledDeltaTime — чтобы анимация доигралась даже при Time.timeScale=0
        // во время экрана выбора апгрейда.
        if (_xpSlider != null)
        {
            float dt = Time.unscaledDeltaTime;
            if (_xpFlushing)
            {
                // На левелапе: доводим бар до конца, затем мгновенно сбрасываем в ноль
                _xpDisplay = Mathf.MoveTowards(_xpDisplay, 1f, 5f * dt);
                if (_xpDisplay >= 1f - 0.0001f)
                {
                    _xpDisplay = 0f;
                    _xpFlushing = false;
                }
            }
            else
            {
                _xpDisplay = Mathf.MoveTowards(_xpDisplay, _xpTarget, 2.5f * dt);
            }
            _xpSlider.value = _xpDisplay;
        }
    }

    private void UpdateXP(int current, int needed)
    {
        _xpTarget = needed > 0 ? Mathf.Clamp01((float)current / needed) : 0f;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Запускаем анимацию «дозаполнить до конца → сброс в ноль».
        // Реальный остаток опыта прилетит следом через OnXPChanged/UpdateXP.
        _xpFlushing = true;
    }

    public void RegisterKill()
    {
        _kills++;
    }
}