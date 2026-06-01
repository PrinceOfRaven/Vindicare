using UnityEngine;

/// <summary>
/// Короткий «сквош» спрайта при попадании — растягивает по X и сжимает по Y,
/// затем упруго возвращает. Делает попадание ощутимым без анимаций.
/// </summary>
public class HitSquash : MonoBehaviour
{
    Transform _t;
    Vector3 _baseScale;
    float _timer;
    float _duration = 0.14f;
    float _intensity;
    bool _active;

    void Awake()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        _t = sr != null ? sr.transform : transform;
        _baseScale = _t.localScale;
    }

    public void Punch(float intensity = 0.32f)
    {
        if (_t == null) return;
        if (!_active) _baseScale = _t.localScale; // на случай если масштаб сменился
        _intensity = intensity;
        _timer = 0f;
        _active = true;
    }

    void Update()
    {
        if (!_active) return;

        _timer += Time.deltaTime;
        float p = Mathf.Clamp01(_timer / _duration);
        float s = Mathf.Sin(p * Mathf.PI); // 0 → 1 → 0
        float sx = 1f + _intensity * s;
        float sy = 1f - _intensity * 0.7f * s;
        _t.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);

        if (p >= 1f)
        {
            _t.localScale = _baseScale;
            _active = false;
        }
    }
}
