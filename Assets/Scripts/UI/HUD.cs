using System.Collections;
using System.Collections.Generic;
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
    private float _xpDisplay;
    private float _xpTarget;
    private bool _xpFlushing;
    private Image _hpFillImg;
    private Image _xpFillImg;

    private TMP_Text _waveText;
    private TMP_Text _bossBanner;
    private Image _bossOverlay;
    private float _bannerTimer;
    private const float BannerDuration = 3.5f;
    private static readonly Color BossColor = new Color(1f, 0.2f, 0.25f);

    private TMP_Text _scoreText;
    private Image _vignette;
    private Image _levelFlash;
    private int _lastHp = -1;
    private float _damageFlash;
    private float _levelFlashAmt;
    private int _score;

    private class AbilitySlotUI
    {
        public IAbilityDisplay Ability;
        public Image CooldownFill;
        public RectTransform Slot;
        public TMP_Text CooldownText;
    }

    private readonly List<AbilitySlotUI> _abilitySlots = new List<AbilitySlotUI>();

    // Босс-бар
    private RectTransform _bossBarRoot;
    private Image _bossBarFill;
    private TMP_Text _bossBarLabel;
    private float _bossHpDisplay;
    private string _currentBossName = "БОСС";

    // Комбо
    private int _combo;
    private float _comboTimer;
    private int _comboBonus;
    private TMP_Text _comboText;
    private const float ComboWindow = 2.5f;

    // Подсказка управления
    private TMP_Text _controlsHint;

    private static Sprite _vignetteSprite;
    private static Sprite _solidSprite;

    public float RunTime => _runTime;
    public int Kills => _kills;
    public int Score => _score;

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
        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.OnWaveStarted -= HandleWaveStarted;
    }

    private void Start()
    {
        if (PlayerLevel.Instance != null)
        {
            PlayerLevel.Instance.OnXPChanged += UpdateXP;
            PlayerLevel.Instance.OnLevelUp   += HandleLevelUp;
            UpdateXP(PlayerLevel.Instance.CurrentXP, PlayerLevel.Instance.XPToNext);
        }

        NormalizeHUD();

        if (_hpSlider != null && _hpSlider.fillRect != null)
            _hpFillImg = _hpSlider.fillRect.GetComponent<Image>();
        if (_xpSlider != null)
        {
            _xpSlider.wholeNumbers = false;
            _xpSlider.minValue = 0f;
            _xpSlider.maxValue = 1f;

            _xpSlider.value = 1f;
            _xpSlider.value = 0f;

            if (_xpSlider.fillRect != null)
            {
                _xpFillImg = _xpSlider.fillRect.GetComponent<Image>();
                if (_xpFillImg != null) _xpFillImg.color = _xpColor;
            }
        }

        _hpHighColor = new Color(1f, 0.15f, 0.20f);
        _hpLowColor  = new Color(0.55f, 0.0f, 0.08f);

        CyberpunkUI.StyleTMP(_hpText,    Color.white,                Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_levelText, new Color(0f, 0.85f, 1f),   Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_timerText, Color.white,                Color.black, 0.25f);
        CyberpunkUI.StyleTMP(_killsText, new Color(0.45f, 1f, 0.3f), Color.black, 0.25f);

        if (_timerText != null) _timerText.fontSize = 30f;
        if (_killsText != null) _killsText.fontSize = 22f;
        if (_levelText != null) _levelText.fontSize = 18f;

        BuildWaveUI();
        BuildExtraUI();
        StartCoroutine(BuildAbilityBarRoutine());
        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.OnWaveStarted += HandleWaveStarted;
    }

    private void BuildExtraUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        var vig = CreateVignetteSprite();

        _vignette = CreateFullscreenImage("DangerVignette", root, vig);
        _vignette.color = new Color(1f, 0f, 0.05f, 0f);
        _vignette.transform.SetAsFirstSibling();

        _levelFlash = CreateFullscreenImage("LevelUpFlash", root, vig);
        _levelFlash.color = new Color(0f, 0.85f, 1f, 0f);
        _levelFlash.transform.SetAsFirstSibling();

        _scoreText = CreateText("ScoreCounter", root,
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            pos: new Vector2(-8f, -44f), size: new Vector2(240f, 26f));
        _scoreText.alignment = TextAlignmentOptions.Right;
        _scoreText.fontSize = 22f;
        _scoreText.text = "ОЧКИ 0";
        CyberpunkUI.StyleTMP(_scoreText, new Color(1f, 0.82f, 0.2f), Color.black, 0.25f);

        BuildBossBar(root);
        BuildComboText(root);
        BuildControlsHint(root);
    }

    private void BuildBossBar(Transform root)
    {
        var go = new GameObject("BossBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _bossBarRoot = (RectTransform)go.transform;
        _bossBarRoot.SetParent(root, false);
        _bossBarRoot.anchorMin = new Vector2(0.5f, 1f);
        _bossBarRoot.anchorMax = new Vector2(0.5f, 1f);
        _bossBarRoot.pivot     = new Vector2(0.5f, 1f);
        _bossBarRoot.anchoredPosition = new Vector2(0f, -112f);
        _bossBarRoot.sizeDelta = new Vector2(900f, 22f);
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.12f, 0f, 0.02f, 0.85f);
        bg.raycastTarget = false;

        _bossBarFill = AddSlotImage("Fill", _bossBarRoot, 2f);
        _bossBarFill.sprite = CreateSolidSprite();
        _bossBarFill.color = new Color(1f, 0.12f, 0.18f);
        _bossBarFill.type = Image.Type.Filled;
        _bossBarFill.fillMethod = Image.FillMethod.Horizontal;
        _bossBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _bossBarFill.fillAmount = 1f;

        CyberpunkUI.AddNeonBorder(_bossBarRoot, new Color(1f, 0.2f, 0.25f) * 2.2f, 2f);

        _bossBarLabel = CreateText("BossLabel", _bossBarRoot,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 0f),
            pos: new Vector2(0f, 3f), size: new Vector2(900f, 24f));
        _bossBarLabel.alignment = TextAlignmentOptions.Center;
        _bossBarLabel.fontSize = 20f;
        CyberpunkUI.StyleTMP(_bossBarLabel, new Color(1f, 0.3f, 0.35f), Color.black, 0.28f);

        _bossBarRoot.gameObject.SetActive(false);
    }

    private void BuildComboText(Transform root)
    {
        _comboText = CreateText("ComboCounter", root,
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            pos: new Vector2(-8f, -74f), size: new Vector2(280f, 34f));
        _comboText.alignment = TextAlignmentOptions.Right;
        _comboText.fontSize = 28f;
        _comboText.text = string.Empty;
        CyberpunkUI.StyleTMP(_comboText, new Color(1f, 0.55f, 0.1f), Color.black, 0.3f);
    }

    private void BuildControlsHint(Transform root)
    {
        _controlsHint = CreateText("ControlsHint", root,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: new Vector2(0f, -160f), size: new Vector2(900f, 120f));
        _controlsHint.alignment = TextAlignmentOptions.Center;
        _controlsHint.fontSize = 24f;
        _controlsHint.text =
            "<b>Движение</b> — WASD     <b>Прицел</b> — мышь\n" +
            "<color=#FF2FD9>E</color> Бомба    " +
            "<color=#00FFE0>Shift</color> Рывок    " +
            "<color=#33B3FF>Q</color> Щит    " +
            "<color=#FFB327>R</color> Овердрайв    " +
            "<color=#73FF4D>F</color> Турель";
        CyberpunkUI.StyleTMP(_controlsHint, Color.white, Color.black, 0.25f);
        StartCoroutine(FadeControlsHint());
    }

    private IEnumerator FadeControlsHint()
    {
        if (_controlsHint == null) yield break;

        // Появление.
        float t = 0f;
        while (t < 0.5f) { t += Time.unscaledDeltaTime; _controlsHint.alpha = Mathf.Clamp01(t / 0.5f); yield return null; }
        _controlsHint.alpha = 1f;

        // Держим.
        yield return new WaitForSecondsRealtime(6f);

        // Угасание.
        t = 0f;
        while (t < 1.2f) { t += Time.unscaledDeltaTime; _controlsHint.alpha = 1f - Mathf.Clamp01(t / 1.2f); yield return null; }

        Destroy(_controlsHint.gameObject);
        _controlsHint = null;
    }

    private static Image CreateFullscreenImage(string objName, Transform parent, Sprite sprite)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite CreateVignetteSprite()
    {
        if (_vignetteSprite != null) return _vignetteSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var center = new Vector2(0.5f, 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                float d = (uv - center).magnitude * 2f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.15f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _vignetteSprite;
    }

    private static Sprite CreateSolidSprite()
    {
        if (_solidSprite != null) return _solidSprite;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _solidSprite;
    }

    private IEnumerator BuildAbilityBarRoutine()
    {
        // Способности навешиваются на игрока в PlayerMovement.Start — ждём, пока появятся.
        float timeout = Time.realtimeSinceStartup + 5f;
        while (PlayerMovement.Instance == null && Time.realtimeSinceStartup < timeout)
            yield return null;
        // Ещё кадр — чтобы все Start() (где добавляются компоненты) точно отработали.
        yield return null;

        BuildAbilityBar();
    }

    private void BuildAbilityBar()
    {
        var abilities = CollectAbilities();
        if (abilities.Count == 0) return;

        var canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        const float slotSize = 56f;
        const float gap = 8f;
        float total = abilities.Count * slotSize + (abilities.Count - 1) * gap;

        var container = new GameObject("AbilityBar", typeof(RectTransform));
        var crt = (RectTransform)container.transform;
        crt.SetParent(root, false);
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot     = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 28f);
        crt.sizeDelta = new Vector2(total, slotSize);

        for (int i = 0; i < abilities.Count; i++)
        {
            float x = i * (slotSize + gap);
            _abilitySlots.Add(BuildSlot(crt, abilities[i], x, slotSize));
        }
    }

    private List<IAbilityDisplay> CollectAbilities()
    {
        var list = new List<IAbilityDisplay>();
        var bomb = FindObjectOfType<PlayerBombThrow>(true);
        if (bomb != null) list.Add(bomb);

        AddIf<DashAbility>(list);
        AddIf<ShieldAbility>(list);
        AddIf<OverdriveAbility>(list);
        AddIf<TurretAbility>(list);
        return list;
    }

    private static void AddIf<T>(List<IAbilityDisplay> list) where T : PlayerAbility
    {
        var a = FindObjectOfType<T>(true);
        if (a != null) list.Add(a);
    }

    private AbilitySlotUI BuildSlot(RectTransform parent, IAbilityDisplay ability, float x, float slotSize)
    {
        var slot = new GameObject("Slot_" + ability.DisplayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var srt = (RectTransform)slot.transform;
        srt.SetParent(parent, false);
        srt.anchorMin = new Vector2(0f, 0.5f);
        srt.anchorMax = new Vector2(0f, 0.5f);
        srt.pivot     = new Vector2(0f, 0.5f);
        srt.anchoredPosition = new Vector2(x, 0f);
        srt.sizeDelta = new Vector2(slotSize, slotSize);
        var bg = slot.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.02f, 0.08f, 0.82f);
        bg.raycastTarget = false;

        var icon = AddSlotImage("Icon", srt, 8f);
        if (ability.Icon != null)
        {
            icon.sprite = ability.Icon;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }
        else
        {
            icon.sprite = CreateSolidSprite();
            var c = ability.ThemeColor; c.a = 0.9f;
            icon.color = c;
        }

        var cooldown = AddSlotImage("Cooldown", srt, 4f);
        cooldown.sprite = CreateSolidSprite();
        cooldown.color = new Color(0f, 0f, 0f, 0.7f);
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;

        CyberpunkUI.AddNeonBorder(srt, ability.ThemeColor * 2.2f, 2f);

        var cdText = CreateText("CooldownText", srt,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: Vector2.zero, size: new Vector2(slotSize, slotSize));
        cdText.alignment = TextAlignmentOptions.Center;
        cdText.fontSize = 22f;
        cdText.text = string.Empty;
        CyberpunkUI.StyleTMP(cdText, Color.white, Color.black, 0.25f);

        var key = CreateText("Key", srt,
            anchor: new Vector2(1f, 0f), pivot: new Vector2(1f, 0f),
            pos: new Vector2(-1f, 1f), size: new Vector2(34f, 16f));
        key.alignment = TextAlignmentOptions.BottomRight;
        key.fontSize = 13f;
        key.text = ability.KeyLabel;
        CyberpunkUI.StyleTMP(key, new Color(0f, 0.85f, 1f), Color.black, 0.22f);

        var nameLabel = CreateText("Name", srt,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 0f),
            pos: new Vector2(0f, 3f), size: new Vector2(110f, 16f));
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.fontSize = 12f;
        nameLabel.text = ability.DisplayName;
        var nc = ability.ThemeColor; nc.a = 1f;
        CyberpunkUI.StyleTMP(nameLabel, nc, Color.black, 0.22f);

        return new AbilitySlotUI { Ability = ability, CooldownFill = cooldown, Slot = srt, CooldownText = cdText };
    }

    private static Image AddSlotImage(string objName, Transform parent, float padding)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private void UpdateAbilityBar()
    {
        for (int i = 0; i < _abilitySlots.Count; i++)
        {
            var s = _abilitySlots[i];
            if (s.Ability == null || s.CooldownFill == null) continue;

            float rem = s.Ability.CooldownRemaining01;
            s.CooldownFill.fillAmount = rem;

            if (s.CooldownText != null)
                s.CooldownText.text = (!s.Ability.IsReady && rem > 0f)
                    ? Mathf.CeilToInt(rem * AbilityCooldownSeconds(s.Ability)).ToString()
                    : string.Empty;

            if (s.Slot != null)
            {
                if (s.Ability.IsReady)
                {
                    float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
                    CyberpunkUI.SetNeonBorderColor(s.Slot, s.Ability.ThemeColor * 2.2f * pulse);
                }
                else
                {
                    CyberpunkUI.SetNeonBorderColor(s.Slot, s.Ability.ThemeColor * 0.7f);
                }
            }
        }
    }

    private static float AbilityCooldownSeconds(IAbilityDisplay a)
    {
        // Оценка полного кулдауна по текущей доле — нужна только для подписи секунд.
        return a is PlayerAbility pa ? Mathf.Max(0.1f, pa.CooldownSeconds) : 2f;
    }

    private void BuildWaveUI()
    {
        var canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.transform : transform;

        _waveText = CreateText("WaveCounter", root,
            anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            pos: new Vector2(0f, -50f), size: new Vector2(260f, 28f));
        _waveText.alignment = TextAlignmentOptions.Center;
        _waveText.fontSize = 20f;
        _waveText.text = string.Empty;
        CyberpunkUI.StyleTMP(_waveText, new Color(0f, 0.85f, 1f), Color.black, 0.25f);

        var overlayGo = new GameObject("BossOverlay",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var ort = (RectTransform)overlayGo.transform;
        ort.SetParent(root, false);
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        ort.SetAsFirstSibling();
        _bossOverlay = overlayGo.GetComponent<Image>();
        _bossOverlay.color = new Color(0.8f, 0f, 0.05f, 0f);
        _bossOverlay.raycastTarget = false;

        _bossBanner = CreateText("BossBanner", root,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: new Vector2(0f, 90f), size: new Vector2(960f, 130f));
        _bossBanner.alignment = TextAlignmentOptions.Center;
        _bossBanner.fontSize = 64f;
        CyberpunkUI.StyleTMP(_bossBanner, BossColor, Color.black, 0.3f);
        _bossBanner.alpha = 0f;
    }

    private static TMP_Text CreateText(string objName, Transform parent, Vector2 anchor,
                                       Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        var text = go.AddComponent<TextMeshProUGUI>();
        text.raycastTarget = false;
        return text;
    }

    private void HandleWaveStarted(int number, WaveSpawner.Wave wave)
    {
        if (_waveText != null)
        {
            int loop = WaveSpawner.Instance != null ? WaveSpawner.Instance.LoopCount : 0;
            _waveText.text = loop > 0 ? $"ВОЛНА {number}  ·  КРУГ {loop + 1}" : $"ВОЛНА {number}";
        }

        if (wave != null && wave.isBossWave)
        {
            _currentBossName = string.IsNullOrEmpty(wave.name) ? "БОСС" : wave.name.ToUpperInvariant();
            if (_bossBanner != null)
            {
                _bossBanner.text = $"БОСС: {wave.name}";
                _bannerTimer = BannerDuration;
            }
        }
    }

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

    private static void AddBarIcon(Slider slider, Sprite icon, Color tint)
    {
        if (slider == null || icon == null) return;
        if (slider.transform.Find("Icon_" + icon.name) != null) return;
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

            if (_lastHp >= 0 && hp < _lastHp) _damageFlash = 1f;
            _lastHp = hp;

            _hpDisplay = Mathf.MoveTowards(_hpDisplay, hp, Mathf.Max(max * 2f, 20f) * Time.deltaTime);
            if (_hpSlider != null) { _hpSlider.maxValue = max; _hpSlider.value = _hpDisplay; }
            if (_hpText != null) _hpText.text = $"{hp} / {max}";

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

        if (_scoreText != null)
        {
            int level = PlayerLevel.Instance != null ? PlayerLevel.Instance.Level : 0;
            int wave  = WaveSpawner.Instance != null ? WaveSpawner.Instance.WaveNumber : 0;
            _score = RunRecords.ComputeScore(_runTime, _kills, level, wave) + _comboBonus;
            _scoreText.text = $"ОЧКИ {_score}";
        }

        if (_xpSlider != null)
        {
            float dt = Time.unscaledDeltaTime;
            if (_xpFlushing)
            {
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

        UpdateBossBanner();
        UpdateOverlays();
        UpdateAbilityBar();
        UpdateBossHealthBar();
        UpdateCombo();
    }

    private void UpdateBossHealthBar()
    {
        if (_bossBarRoot == null) return;

        var boss = Boss.Current;
        bool show = boss != null && boss.IsAlive;
        if (_bossBarRoot.gameObject.activeSelf != show)
            _bossBarRoot.gameObject.SetActive(show);
        if (!show) return;

        float target = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.Health / boss.MaxHealth) : 0f;
        _bossHpDisplay = Mathf.MoveTowards(_bossHpDisplay, target, 1.5f * Time.deltaTime);
        if (_bossBarFill != null) _bossBarFill.fillAmount = _bossHpDisplay;
        if (_bossBarLabel != null) _bossBarLabel.text = _currentBossName;
    }

    private void UpdateCombo()
    {
        if (_comboTimer > 0f)
        {
            _comboTimer -= Time.deltaTime;
            if (_comboTimer <= 0f) _combo = 0;
        }

        if (_comboText == null) return;

        if (_combo >= 3)
        {
            float frac = Mathf.Clamp01(_comboTimer / ComboWindow);
            _comboText.text = $"КОМБО x{_combo}";
            _comboText.alpha = 0.35f + 0.65f * frac;
            float pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 12f);
            _comboText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }
        else if (_comboText.alpha != 0f)
        {
            _comboText.text = string.Empty;
            _comboText.alpha = 0f;
            _comboText.rectTransform.localScale = Vector3.one;
        }
    }

    private void UpdateOverlays()
    {
        float dt = Time.unscaledDeltaTime;
        _damageFlash   = Mathf.MoveTowards(_damageFlash, 0f, dt * 2.5f);
        _levelFlashAmt = Mathf.MoveTowards(_levelFlashAmt, 0f, dt * 1.8f);

        float lowHp = 0f;
        var player = PlayerMovement.Instance;
        if (player != null && player.MaxHealth > 0 && player.IsAlive)
        {
            float ratio = (float)player.Health / player.MaxHealth;
            if (ratio < 0.35f)
            {
                float k = (0.35f - ratio) / 0.35f;
                float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 6f);
                lowHp = k * 0.32f * pulse;
            }
        }

        if (_vignette != null)
        {
            var c = _vignette.color;
            c.a = Mathf.Max(lowHp, _damageFlash * 0.45f);
            _vignette.color = c;
        }
        if (_levelFlash != null)
        {
            var c = _levelFlash.color;
            c.a = _levelFlashAmt * 0.35f;
            _levelFlash.color = c;
        }
    }

    private void UpdateBossBanner()
    {
        if (_bannerTimer <= 0f) return;

        _bannerTimer -= Time.unscaledDeltaTime;
        float elapsed = BannerDuration - _bannerTimer;

        float alpha;
        if (elapsed < 0.35f)         alpha = elapsed / 0.35f;
        else if (_bannerTimer < 0.9f) alpha = Mathf.Max(0f, _bannerTimer / 0.9f);
        else                          alpha = 1f;

        if (_bossBanner != null)
        {
            _bossBanner.alpha = alpha;
            float pulse = 1f + 0.05f * Mathf.Sin(elapsed * 6f);
            _bossBanner.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }

        if (_bossOverlay != null)
        {
            float oa = 0f;
            if (elapsed < 1.6f)
            {
                float k = 1f - elapsed / 1.6f;
                oa = 0.32f * k * k * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(elapsed * 7f)));
            }
            var c = _bossOverlay.color;
            c.a = oa;
            _bossOverlay.color = c;
        }

        if (_bannerTimer <= 0f)
        {
            if (_bossBanner != null)
            {
                _bossBanner.alpha = 0f;
                _bossBanner.rectTransform.localScale = Vector3.one;
            }
            if (_bossOverlay != null)
            {
                var c = _bossOverlay.color;
                c.a = 0f;
                _bossOverlay.color = c;
            }
        }
    }

    private void UpdateXP(int current, int needed)
    {
        _xpTarget = needed > 0 ? Mathf.Clamp01((float)current / needed) : 0f;
    }

    private void HandleLevelUp(int newLevel)
    {
        _xpFlushing = true;
        _levelFlashAmt = 1f;
    }

    public void RegisterKill()
    {
        _kills++;

        _combo++;
        _comboTimer = ComboWindow;
        // Каждое убийство в серии даёт очки, тем больше — чем длиннее комбо.
        _comboBonus += _combo;
    }
}
