using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _lerpSpeed = 14f;

    Vector3 _targetScale = Vector3.one;
    RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _targetScale = _rt != null ? _rt.localScale : Vector3.one;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        _targetScale = Vector3.one * _hoverScale;
    }

    public void OnPointerExit(PointerEventData _)
    {
        _targetScale = Vector3.one;
    }

    void Update()
    {
        if (_rt == null) return;
        _rt.localScale = Vector3.Lerp(_rt.localScale, _targetScale, _lerpSpeed * Time.unscaledDeltaTime);
    }
}
