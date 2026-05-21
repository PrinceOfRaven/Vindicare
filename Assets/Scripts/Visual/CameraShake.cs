using UnityEngine;

/// <summary>
/// Лёгкий screen-shake. Накладывает оффсет на позицию камеры в LateUpdate
/// с высоким DefaultExecutionOrder — после того, как Cinemachine выставил позицию.
/// Cinemachine каждый кадр перезаписывает Transform.position, поэтому достаточно
/// просто прибавлять оффсет.
/// </summary>
[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    float _amplitude;
    float _duration;
    float _frequency;
    float _time;

    public void Shake(float amplitude, float duration, float frequency = 28f)
    {
        // Запускаем шейк, берём максимум амплитуды от текущего и нового,
        // чтобы мелкие шейки не "глушили" большие
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
