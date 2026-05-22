using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField] private float _duration = 0.07f;

    SpriteRenderer[] _renderers;
    Color[] _baseColors;
    float _timer;
    bool _flashing;

    void Awake()
    {
        Cache();
    }

    void Cache()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _baseColors[i] = _renderers[i].color;
    }

    public void Flash()
    {
        if (_renderers == null || _renderers.Length == 0) Cache();
        _timer = _duration;
        _flashing = true;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _flashColor;
    }

    void Update()
    {
        if (!_flashing) return;
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _flashing = false;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].color = _baseColors[i];
    }
}
