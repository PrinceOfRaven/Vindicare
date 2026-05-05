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

    [Header("Настройки камеры")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _spriteAngleOffset = 0f;

    private PlayerActionsControl _playerActionsControl;
    private Vector3 _MouseWorldPos;
    private bool _hasMouseData = false;
    private bool _isFiring = false;
    private float _nextFireTime = 0f;

    private void Awake()
    {
        _playerActionsControl = new PlayerActionsControl();
        _playerActionsControl.Player.Attack.started += OnFireStarted;
        _playerActionsControl.Player.Attack.canceled += OnFireCanceled;
    }

    private void Update()
    {
        if (_camera == null || Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(screenPos.x, screenPos.y, _camera.WorldToScreenPoint(transform.position).z);
        _MouseWorldPos = _camera.ScreenToWorldPoint(mouseScreen);
        _hasMouseData = true;

        if (_isFiring && Time.time >= _nextFireTime)
        {
            float waitTime = 1f / Mathf.Max(_firingRate, 0.1f);
            _nextFireTime = Time.time + waitTime;
            FireBurst();
        }
    }

    private void LateUpdate()
    {
        if (!_hasMouseData || _camera == null) return;
        Vector2 direction = (_MouseWorldPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnEnable() => _playerActionsControl.Player.Enable();
    private void OnDisable() => _playerActionsControl.Player.Disable();

    private void OnFireStarted(InputAction.CallbackContext ctx) => _isFiring = true;
    private void OnFireCanceled(InputAction.CallbackContext ctx) => _isFiring = false;

    private void FireBurst()
    {
        if (_muzzle == null || !_hasMouseData) return;

        Vector2 aimDirection = (_MouseWorldPos - _muzzle.position).normalized;
        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < _bulletCount; i++)
        {
            float angleOffset = (_bulletCount > 1)? -_firingSpread / 2f + i * (_firingSpread / (_bulletCount - 1)) : 0f;
            float finalAngle = baseAngle + angleOffset;
            Quaternion rotation = Quaternion.Euler(0f, 0f, finalAngle);

            GameObject bullet = _bulletPool.Get();
            bullet.transform.SetPositionAndRotation(_muzzle.position, rotation);

            if (bullet.TryGetComponent(out PooledBullet curBullet))
            {
                Vector2 direction = rotation * Vector2.right;
                curBullet.Initialize(_bulletPool, direction, _damage);
            }
        }
    }
}