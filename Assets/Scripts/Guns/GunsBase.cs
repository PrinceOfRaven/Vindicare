using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunsBase : MonoBehaviour
{
    [Header("Характеристики оружия")]
    [SerializeField] private BulletPool _bulletPool;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private int _bulletCount;
    [SerializeField] private float _firingSpread;
    [SerializeField] private float _firingRate;
    [SerializeField] private Camera _camera;
    private PlayerActionsControl _playerActionsControl;
    private Coroutine _shootCoroutine;

    private void Awake()
    {
        _playerActionsControl = new PlayerActionsControl();
    }

    private void OnEnable() => _playerActionsControl.Player.Enable();
    private void OnDisable() => _playerActionsControl.Player.Disable();

    private void Shoot() 
    {
        if (_shootCoroutine == null) 
        {
            _shootCoroutine = StartCoroutine(ShootLoop());
        }
    }

    private void StopShoot() 
    {
        if ( _shootCoroutine != null) 
        {
            StopCoroutine(_shootCoroutine);
            _shootCoroutine = null;
        }
    }

    private IEnumerator ShootLoop() 
    {
        WaitForSeconds wait = new WaitForSeconds(1f / _firingRate);

        while (true) 
        {
            FireBurst();
            yield return wait;
        }
    }

    private void FireBurst() 
    {
        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = _camera.WorldToScreenPoint(_muzzle.position).z;
        Vector3 mouseWorld = _camera.WorldToScreenPoint(mouseScreen);

        Vector2 aimDirection = (mouseWorld - _muzzle.position).normalized;
        float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        for (int i = 0; i < _bulletCount;i++) 
        {
            float angleOffset = (_bulletCount > 1) ? -_firingSpread / 2f * (_firingSpread / (_bulletCount - 1)) : 0f;
            float finalAngle = baseAngle + angleOffset;
            Quaternion rotation = Quaternion.Euler(finalAngle, 0f, 0f);

            GameObject bullet = _bulletPool.Get();
            bullet.transform.SetPositionAndRotation(_muzzle.position,rotation);

            if (bullet.TryGetComponent(out PooledBullet curBullet)) 
            {
                Vector2 direction = rotation * Vector2.right;
                curBullet.Initialize(_bulletPool,direction);
            }
        }

    }
}
