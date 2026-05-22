using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private TMP_Text _stacksText;
    [SerializeField] private Button _button;

    private UpgradeData _data;
    private Action<UpgradeData> _onClick;
    private CanvasGroup _canvasGroup;

    public void Bind(UpgradeData data, Action<UpgradeData> onClick)
    {
        _data = data;
        _onClick = onClick;

        if (_icon != null) { _icon.sprite = data.icon; _icon.enabled = data.icon != null; }
        if (_nameText != null) _nameText.text = data.upgradeName;
        if (_descriptionText != null) _descriptionText.text = data.description;

        if (_stacksText != null && PlayerStats.Instance != null)
        {
            int current = PlayerStats.Instance.GetStacks(data.type);
            _stacksText.text = $"{current + 1}/{data.maxStacks}";
        }

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() =>
        {
            AudioFX.UIClick();
            _onClick?.Invoke(_data);
        });

        ApplyCyberpunkStyle(data);
        PlayEntranceAnimation();
    }

    private void ApplyCyberpunkStyle(UpgradeData data)
    {
        Color borderColor = ColorForType(data.type) * 2.5f;
        var rt = transform as RectTransform;
        if (rt != null) CyberpunkUI.AddNeonBorder(rt, borderColor, 2f);

        if (GetComponent<CardHoverFX>() == null) gameObject.AddComponent<CardHoverFX>();

        if (_nameText != null)
            CyberpunkUI.StyleTMP(_nameText, ColorForType(data.type), Color.black, 0.25f);
        if (_descriptionText != null)
            CyberpunkUI.StyleTMP(_descriptionText, Color.white, Color.black, 0.15f, FontStyles.Normal);
        if (_stacksText != null)
            CyberpunkUI.StyleTMP(_stacksText, new Color(0.85f, 0.85f, 0.85f), Color.black, 0.2f);
    }

    private void PlayEntranceAnimation()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        StopAllCoroutines();
        StartCoroutine(EntranceCR());
    }

    private IEnumerator EntranceCR()
    {
        if (_canvasGroup == null) yield break;

        _canvasGroup.alpha = 0f;

        float delay = transform.GetSiblingIndex() * 0.08f;
        float t = 0f;
        while (t < delay) { t += Time.unscaledDeltaTime; yield return null; }

        float dur = 0.25f;
        t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float s = p * p * (3f - 2f * p);
            _canvasGroup.alpha = s;
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }

    private static Color ColorForType(UpgradeData.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeData.UpgradeType.Damage:          return new Color(1f, 0.25f, 0.30f);
            case UpgradeData.UpgradeType.FireRate:        return new Color(1f, 0.85f, 0.20f);
            case UpgradeData.UpgradeType.MoveSpeed:       return new Color(0f, 0.85f, 1f);
            case UpgradeData.UpgradeType.MaxHealth:       return new Color(0.3f, 1f, 0.55f);
            case UpgradeData.UpgradeType.PickupRadius:    return new Color(0.45f, 1f, 0.3f);
            case UpgradeData.UpgradeType.ProjectileCount: return new Color(1f, 0.18f, 0.80f);
            case UpgradeData.UpgradeType.BombDamage:      return new Color(1f, 0.55f, 0.10f);
            default:                                       return Color.white;
        }
    }
}
