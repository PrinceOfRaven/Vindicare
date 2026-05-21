using UnityEngine;
using UnityEngine.InputSystem;

public class GunsBase : MonoBehaviour
{
    [Header("Характеристики оружия")]
    [SerializeField] private BulletPool _bulletPool;
    [SerializeField] private Transform _muzzle;
    [SerializeField, Min(1f)] private float _damage = 10f;
    [SerializeField, Range(1, 100)] private int _bulletCount = 1;
    [SerializeField, Min(0.1f)] private float _firingRate = 5f;
    [SerializeField] private float _firingSpread = 30f;

    [Header("Бомба")]
    [SerializeField] private GameObject _bombPrefab;
    [SerializeField, Min(0.1f)] private float _bombCooldown = 2f;

    [Header("Настройки камеры")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _spriteAngleOffset = 0f;

    [Header("Поворот спрайта игрока вместе с пушкой")]
    [Tooltip("Sprite Renderer игрока или промежуточный Transform со спрайтом. Будет крутиться за мышью.")]
    [SerializeField] private Transform _playerSpriteToRotate;
    [Tooltip("Доп. смещение угла для спрайта (если спрайт смотрит вниз, например, поставь 90)")]
    [SerializeField] private float _playerSpriteAngleOffset = 0f;

    private PlayerActionsControl _playerActionsControl;
    private Vector3 _MouseWorldPos;
    private bool _hasMouseData = false;
    private bool _isFiring = false;
    private float _nextFireTime = 0f;
    private float _nextBombTime = 0f;
    private Vector2 _aimDir = Vector2.right; // актуальное направление прицела, обновляется в LateUpdate

    private Transform _playerTransform;

    private void Awake()
    {
        _playerActionsControl = new PlayerActionsControl();
        _playerActionsControl.Player.Attack.started += OnFireStarted;
        _playerActionsControl.Player.Attack.canceled += OnFireCanceled;
        _playerActionsControl.Player.Interact.started += OnInteract;

        _playerTransform = GetComponentInParent<PlayerMovement>()?.transform;
        if (_playerTransform == null && PlayerMovement.Instance != null)
            _playerTransform = PlayerMovement.Instance.transform;
    }

    private Camera ActiveCamera => _camera != null ? _camera : Camera.main;

    private void Update()
    {
        var cam = ActiveCamera;
        if (cam != null && Mouse.current != null)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 mouseScreen = new Vector3(screenPos.x, screenPos.y,
                cam.WorldToScreenPoint(transform.position).z);
            _MouseWorldPos = cam.ScreenToWorldPoint(mouseScreen);
            _hasMouseData = true;
        }

        if (_isFiring && Time.time >= _nextFireTime)
        {
            float fireRateMult = PlayerStats.Instance != null
                ? PlayerStats.Instance.FireRateMultiplier : 1f;
            float waitTime = 1f / Mathf.Max(_firingRate * fireRateMult, 0.1f);
            _nextFireTime = Time.time + waitTime;
            FireBurst();
        }
    }

    private void LateUpdate()
    {
        if (_hasMouseData)
        {
            _aimDir = (_MouseWorldPos - transform.position).normalized;
        }
        else if (PlayerMovement.Instance != null)
        {
            _aimDir = PlayerMovement.Instance.FacingDirection.normalized;
        }

        float angle = Mathf.Atan2(_aimDir.y, _aimDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + _spriteAngleOffset);
    }

    private void OnEnable() => _playerActionsControl.Player.Enable();
    private void OnDisable() => _playerActionsControl.Player.Disable();
    private void OnFireStarted(InputAction.CallbackContext ctx) => _isFiring = true;
    private void OnFireCanceled(InputAction.CallbackContext ctx) => _isFiring = false;

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (Time.time < _nextBombTime) return;
        if (_bombPrefab == null)
        {
            Debug.LogWarning("Bomb prefab не назначен в GunsBase!");
            return;
        }
        if (_muzzle == null)
        {
            Debug.LogWarning("Muzzle не назначен — бомба не может быть брошена!");
            return;
        }
        ThrowBomb();
        _nextBombTime = Time.time + _bombCooldown;
    }

    private void FireBurst()
    {
        if (_muzzle == null) return;
        float baseAngle = Mathf.Atan2(_aimDir.y, _aimDir.x) * Mathf.Rad2Deg;

        float damageMult = PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : 1f;
        int totalBullets = _bulletCount + (PlayerStats.Instance != null ? PlayerStats.Instance.ExtraProjectiles : 0);

        for (int i = 0; i < totalBullets; i++)
        {
            float angleOffset = (totalBullets > 1) ? -_firingSpread / 2f + i * (_firingSpread / (totalBullets - 1)) : 0f;
            float finalAngle = baseAngle + angleOffset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, finalAngle);
            GameObject bullet = _bulletPool.Get();
            bullet.transform.SetPositionAndRotation(_muzzle.position, rotation);
            if (bullet.TryGetComponent(out PooledBullet curBullet))
            {
                Vector2 direction = rotation * Vector2.right;
                curBullet.Initialize(_bulletPool, direction, _damage * damageMult);
            }
        }
    }

    private void ThrowBomb()
    {
        if (_muzzle == null) return;
        GameObject bomb = Instantiate(_bombPrefab, _muzzle.position, Quaternion.identity);
        if (!bomb.TryGetComponent(out Bomb bombScript)) return;

        if (_hasMouseData)
            bombScript.LaunchToPosition(_MouseWorldPos);
        else
            bombScript.Launch(_aimDir);
    }

    public Vector2 AimDirection
    {
        get
        {
            if (!_hasMouseData) return transform.right;
            return (_MouseWorldPos - transform.position).normalized;
        }
    }
    public Transform Muzzle => _muzzle;
}