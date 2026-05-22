using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    [Header("Корень панели (она скрывается, когда не нужна)")]
    [SerializeField] private GameObject _root;

    [Header("Три карточки (порядок не важен)")]
    [SerializeField] private List<UpgradeCard> _cards = new();

    private Action<UpgradeData> _onPicked;
    private UpgradeStatsPanel _statsPanel;

    private void Awake()
    {
        if (_root == null) _root = gameObject;
    }

    public void Show(List<UpgradeData> options, Action<UpgradeData> onPicked)
    {
        _onPicked = onPicked;
        _root.SetActive(true);

        EnsureStatsPanel();
        if (_statsPanel != null) _statsPanel.Refresh();

        for (int i = 0; i < _cards.Count; i++)
        {
            if (i < options.Count)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Bind(options[i], HandleCardClicked);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        if (_root != null) _root.SetActive(false);
        if (_statsPanel != null) _statsPanel.Hide();
    }

    private void EnsureStatsPanel()
    {
        if (_statsPanel != null) return;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null) _statsPanel = UpgradeStatsPanel.Create(canvas.transform);
    }

    private void HandleCardClicked(UpgradeData data)
    {
        _onPicked?.Invoke(data);
    }
}
