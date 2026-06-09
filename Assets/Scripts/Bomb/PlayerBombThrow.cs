using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerBombThrow : MonoBehaviour, IAbilityDisplay
{
    public string DisplayName => "БОМБА";
    public string KeyLabel => "E";
    public Color ThemeColor => new Color(1f, 0.18f, 0.80f);
    public Sprite Icon => AbilityIcon;

    [Header("Бомба")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField, Min(0.1f)] private float _cooldown = 2f;

    [Header("Откуда бросать")]
    [SerializeField] private Transform _spawnPoint;

    [Header("Камера")]
    [SerializeField] private Camera _camera;

    private PlayerActionsControl _actions;
    private float _nextThrowTime;

    /// <summary>Готова ли способность к применению.</summary>
    public bool IsReady => Time.time >= _nextThrowTime;

    /// <summary>Доля оставшейся перезарядки: 1 — только что использована, 0 — готова.</summary>
    public float CooldownRemaining01
    {
        get
        {
            if (_cooldown <= 0f) return 0f;
            return Mathf.Clamp01((_nextThrowTime - Time.time) / _cooldown);
        }
    }

    /// <summary>Иконка способности — берётся из спрайта префаба бомбы.</summary>
    public Sprite AbilityIcon
    {
        get
        {
            if (_bombPrefab == null) return null;
            var sr = _bombPrefab.GetComponentInChildren<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }
    }

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
