using UnityEngine;

/// <summary>
/// Кристалл опыта. Дропают враги при смерти.
/// Когда игрок в радиусе PlayerStats.PickupRadius — летит к нему,
/// при касании — добавляет опыт.
/// </summary>
public class ExperienceOrb : MonoBehaviour
{
    [Header("Сколько опыта даёт")]
    [SerializeField, Min(1)] private int _xpAmount = 1;

    [Header("Полёт к игроку")]
    [SerializeField] private float _startSpeed = 4f;
    [SerializeField] private float _acceleration = 25f;
    [SerializeField] private float _collectDistance = 0.3f;

    private Transform _player;
    private float _currentSpeed;
    private bool _collected;

    private void Start()
    {
        _currentSpeed = _startSpeed;
        CyberpunkFX.AttachLight(transform, CyberpunkFX.Lime, intensity: 1.2f, outerRadius: 0.9f);
    }

    private void LateUpdate()
    {
        // Лёгкое вращение, не трогает position чтобы не конфликтовать с чейзом
        transform.Rotate(0f, 0f, 140f * Time.deltaTime, Space.Self);
    }

    private void Update()
    {
        if (_collected) return;
        if (PlayerMovement.Instance == null) return;
        _player = PlayerMovement.Instance.transform;

        float dist = Vector2.Distance(transform.position, _player.position);
        float pickupRadius = PlayerStats.Instance != null
            ? PlayerStats.Instance.PickupRadius
            : 1.5f;

        // Слишком далеко — лежим и ждём
        if (dist > pickupRadius) return;

        // Подбор по контакту
        if (dist <= _collectDistance)
        {
            Collect();
            return;
        }

        // Летим к игроку, ускоряясь
        Vector3 dir = (_player.position - transform.position).normalized;
        _currentSpeed += _acceleration * Time.deltaTime;
        transform.position += dir * _currentSpeed * Time.deltaTime;
    }

    private void Collect()
    {
        _collected = true;
        CyberpunkFX.SpawnPickupPop(transform.position, CyberpunkFX.Lime);
        if (PlayerLevel.Instance != null)
            PlayerLevel.Instance.AddExperience(_xpAmount);
        Destroy(gameObject);
    }
}
