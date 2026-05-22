using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartRaidMenu : MonoBehaviour
{
    [System.Serializable]
    public class WeaponSlot
    {
        public WeaponData data;
        public Button button;
        public Image iconImage;
        public TMP_Text label;
        public GameObject selectedHighlight;
    }

    [Header("Кнопка старта")]
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _startButtonLabel;
    [SerializeField] private string _startLabelFormat = "Старт: {0}";
    [SerializeField] private string _startLabelEmpty = "Выбери оружие";

    [Header("Слоты оружий")]
    [SerializeField] private List<WeaponSlot> _slots = new List<WeaponSlot>();

    [Header("Авто-выбор")]
    [SerializeField] private int _defaultIndex = 0;

    private int _selectedIndex = -1;

    private void Start()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            int captured = i;
            WeaponSlot slot = _slots[i];

            if (slot.button == null) continue;
            slot.button.onClick.AddListener(() => SelectWeapon(captured));

            if (slot.iconImage != null && slot.data != null && slot.data.icon != null)
                slot.iconImage.sprite = slot.data.icon;

            if (slot.label != null && slot.data != null)
                slot.label.text = slot.data.displayName;

            if (slot.selectedHighlight != null)
                slot.selectedHighlight.SetActive(false);
        }

        if (_defaultIndex >= 0 && _defaultIndex < _slots.Count)
            SelectWeapon(_defaultIndex);
        else
            RefreshStartButton();
    }

    public void SelectWeapon(int index)
    {
        if (index < 0 || index >= _slots.Count) return;
        if (_slots[index].data == null) return;

        _selectedIndex = index;
        SelectedWeapon.Set(_slots[index].data);

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].selectedHighlight != null)
                _slots[i].selectedHighlight.SetActive(i == index);
        }

        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        if (_startButton != null)
            _startButton.interactable = _selectedIndex >= 0;

        if (_startButtonLabel != null)
        {
            _startButtonLabel.text = _selectedIndex >= 0
                ? string.Format(_startLabelFormat, _slots[_selectedIndex].data.displayName)
                : _startLabelEmpty;
        }
    }
}
