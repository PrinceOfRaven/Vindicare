using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerBombThrow : MonoBehaviour
{
    [Header("Бомба")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField, Min(0.1f)] private float _cooldown = 2f;

    [Header("Откуда бросать")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Камера")]
    [SerializeField] private Camera _camera;

    private PlayerActionsControl _actions;
    private float _nextThrowTime;

    private void Awake()
    {
        _actions = new PlayerActionsControl();
        _actions.Player.Interact.performed += OnInteract;
        if (_camera == null) _camera = Camera.main;
    }

    private void OnEnable()  => _actions.Player.Enable();
    private void OnDisable() => _actions.Player.Disable();

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (Time.timeScale == 0f) return;
        if (Time.time < _nextThrowTime) return;

        if (_bombPrefab == null)
        {
            Debug.LogWarning("[PlayerBombThrow] Префаб бомбы не назначен.");
            return;
        }
        if (_camera == null || Mouse.current == null) return;

        Transform origin = _spawnPoint != null ? _spawnPoint : transform;

        Vector2 screenPos  = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(screenPos.x, screenPos.y,
            _camera.WorldToScreenPoint(origin.position).z);
        Vector3 worldPos   = _camera.ScreenToWorldPoint(mouseScreen);
        Vector2 direction  = ((Vector2)(worldPos - origin.position)).normalized;

        GameObject bomb = Instantiate(_bombPrefab, origin.position, Quaternion.identity);
        if (bomb.TryGetComponent(out Bomb bombScript))
        {
            bombScript.Launch(direction);
        }

        _nextThrowTime = Time.time + _cooldown;
    }
}
