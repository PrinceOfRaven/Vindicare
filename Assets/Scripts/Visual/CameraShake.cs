using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    float _amplitude;
    float _duration;
    float _frequency;
    float _time;

    public void Shake(float amplitude, float duration, float frequency = 28f)
    {
        float remaining = _amplitude * Mathf.Max(0f, 1f - _time / Mathf.Max(_duration, 0.001f));
        _amplitude = Mathf.Max(remaining, amplitude);
        _duration = duration;
        _frequency = frequency;
        _time = 0f;
    }

    void LateUpdate()
    {
        if (_time >= _duration || _amplitude <= 0f) return;
        _time += Time.unscaledDeltaTime;

        float falloff = 1f - Mathf.Clamp01(_time / Mathf.Max(_duration, 0.001f));
        float t = Time.unscaledTime * _frequency;
        float x = (Mathf.Sin(t * 1.3f) + Mathf.Sin(t * 2.7f) * 0.5f) * falloff * _amplitude;
        float y = (Mathf.Cos(t * 1.7f) + Mathf.Sin(t * 3.1f) * 0.5f) * falloff * _amplitude;
        transform.position += new Vector3(x, y, 0f);
    }
}
