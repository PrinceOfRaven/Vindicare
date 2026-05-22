using UnityEngine;

/// <summary>
/// Анимация ходьбы без спрайт-листов: при движении спрайт игрока покачивается
/// вверх-вниз. Анимируется сам спрайт — никаких фейковых конечностей.
/// PlayerMovement добавляет этот компонент сам — вешать вручную не требуется.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerWalkAnimation : MonoBehaviour
{
    [Header("Анимация")]
    [Tooltip("Высота покачивания, доля высоты спрайта")]
    [SerializeField] private float _bobFraction  = 0.06f;
    [Tooltip("Радиан фазы на единицу пройденного пути — больше = чаще прыжки")]
    [SerializeField] private float _phasePerUnit = 2.2f;
    [Tooltip("Скорость плавного старта/остановки")]
    [SerializeField] private float _blendSpeed   = 12f;

    private PlayerMovement _player;
    private SpriteRenderer _sr;
    private Transform _spriteTr;
    private Vector3 _restPos;
    private float _restScaleY = 1f;

    private float _phase;
    private float _walkBlend;
    private Vector3 _lastPos;

    private void Start()
    {
        _player = GetComponent<PlayerMovement>();
        _sr = _player.BodySprite;
        // Анимируем только дочерний спрайт: двигать корень нельзя — там физика.
        if (_sr == null || _sr.transform == transform) { enabled = false; return; }

        _spriteTr   = _sr.transform;
        _restPos    = _spriteTr.localPosition;
        _restScaleY = _spriteTr.localScale.y;
        _lastPos    = transform.position;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector3 pos = transform.position;
        float dist = new Vector2(pos.x - _lastPos.x, pos.y - _lastPos.y).magnitude;
        _lastPos = pos;

        bool moving = dt > 0f
                      && dist / dt > 0.05f
                      && _player.MoveInput.sqrMagnitude > 0.02f;

        _walkBlend = Mathf.MoveTowards(_walkBlend, moving ? 1f : 0f, _blendSpeed * dt);
        // Фаза идёт по пройденному пути — нет «скольжения», ускоряется с баффами скорости.
        if (moving) _phase += dist * _phasePerUnit;

        float lift = Mathf.Abs(Mathf.Sin(_phase));
        float spriteH = (_sr.sprite != null ? _sr.sprite.bounds.size.y : 1f) * _restScaleY;
        float bob = lift * _bobFraction * spriteH * _walkBlend;

        _spriteTr.localPosition = _restPos + new Vector3(0f, bob, 0f);
    }
}
