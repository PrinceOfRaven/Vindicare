using UnityEngine;

public class Spin2D : MonoBehaviour
{
    public float spinSpeed = 90f;
    public float bobAmplitude = 0.1f;
    public float bobSpeed = 3f;

    Vector3 _start;
    float _phase;

    void Start()
    {
        _start = transform.localPosition;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
        float y = Mathf.Sin(Time.time * bobSpeed + _phase) * bobAmplitude;
        transform.localPosition = _start + new Vector3(0f, y, 0f);
    }
}
