using System;
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
        _button.onClick.AddListener(() => _onClick?.Invoke(_data));
    }
}