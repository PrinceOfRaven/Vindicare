using UnityEngine;

[DefaultExecutionOrder(10000)]
public class CameraShake : MonoBehaviour
{
    float _amplitude;
    float _duration;
    float _frequency;
    float _time;

    Vector2 _kickDir;
    float _kickAmount;
    float _kickTime;
    float _kickDuration;

    public void Shake(float amplitude, float duration, float frequency = 28f)
    {
        float remaining = _amplitude * Mathf.Max(0f, 1f - _time / Mathf.Max(_duration, 0.001f));
        _amplitude = Mathf.Max(remaining, amplitude);
        _duration = duration;
        _frequency = frequency;
        _time = 0f;
    }

    /// <summary>Резкий направленный толчок камеры (отдача оружия). dir — направление выстрела.</summary>
    public void Kick(Vector2 dir, float amount, float duration = 0.12f)
    {
        _kickDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        _kickAmount = Mathf.Max(_kickAmount, amount);
        _kickDuration = duration;
        _kickTime = 0f;
    }

    void LateUpdate()
    {
        Vector3 offset = Vector3.zero;

        if (_time < _duration && _amplitude > 0f)
        {
            _time += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(_time / Mathf.Max(_duration, 0.001f));
            float t = Time.unscaledTime * _frequency;
            float x = (Mathf.Sin(t * 1.3f) + Mathf.Sin(t * 2.7f) * 0.5f) * falloff * _amplitude;
            float y = (Mathf.Cos(t * 1.7f) + Mathf.Sin(t * 3.1f) * 0.5f) * falloff * _amplitude;
            offset += new Vector3(x, y, 0f);
        }

        if (_kickTime < _kickDuration && _kickAmount > 0f)
        {
            _kickTime += Time.unscaledDeltaTime;
            float kp = Mathf.Clamp01(_kickTime / Mathf.Max(_kickDuration, 0.001f));
            // Резкий бросок назад вдоль выстрела и плавный возврат.
            float push = Mathf.Sin(kp * Mathf.PI) * (1f - kp);
            offset += (Vector3)(_kickDir * (_kickAmount * push));
        }

        transform.position += offset;
    }
}
