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

    [Header("Уникальная механика оружия")]
    [Tooltip("Сквозное пробитие: сколько врагов пуля прошивает насквозь (0 = стоп на первом). Снайперка = railgun.")]
    [SerializeField, Min(0)] private int _pierceCount = 0;

    [Tooltip("Рикошет: к скольким новым целям пуля отскакивает после попадания. Винтовка = цепная молния.")]
    [SerializeField, Min(0)] private int _ricochetCount = 0;

    [Tooltip("Радиус поиска цели для рикошета.")]
    [SerializeField, Min(0f)] private float _ricochetRadius = 5f;

    [Tooltip("Сила отбрасывания врага. Дробовик = мощный толчок. 0 = взять дефолт пули.")]
    [SerializeField, Min(0f)] private float _knockbackForce = 0f;

    [Tooltip("Множитель урона в упор у дула. Спадает до 1 на дистанции pointBlankRange. Дробовик > 1.")]
    [SerializeField, Min(1f)] private float _pointBlankDamageMult = 1f;

    [Tooltip("Дистанция, на которой бонус урона в упор сходит на нет.")]
    [SerializeField, Min(0.1f)] private float _pointBlankRange = 4f;

    [Tooltip("Множитель скорости полёта пули. Снайперка летит быстрее.")]
    [SerializeField, Min(0.1f)] private float _bulletSpeedMult = 1f;

    [Tooltip("Цвет следа/света/искр пули. Помогает различать оружие визуально.")]
    [SerializeField] private Color _bulletColor = new Color(0f, 1f, 0.88f, 1f);

    [Header("Railgun (hitscan-луч)")]
    [Tooltip("Снайперка: мгновенный луч вместо пули, прошивает всех врагов на линии.")]
    [SerializeField] private bool _hitscan = false;

    [Tooltip("Дальность луча railgun.")]
    [SerializeField, Min(1f)] private float _hitscanRange = 30f;

    [Tooltip("Толщина луча railgun (радиус проверки попаданий и ширина визуала).")]
    [SerializeField, Min(0.05f)] private float _hitscanWidth = 0.45f;

    [Header("Отдача и тряска при выстреле")]
    [Tooltip("Амплитуда тряски камеры при выстреле. 0 = выкл.")]
    [SerializeField, Min(0f)] private float _fireShake = 0f;

    [Tooltip("Радиус ударной волны у дула при выстреле (дробовик). 0 = выкл.")]
    [SerializeField, Min(0f)] private float _muzzleShockwave = 0f;

    [Tooltip("Хит-стоп при выстреле, сек (мощные пушки). 0 = выкл.")]
    [SerializeField, Min(0f)] private float _fireHitStop = 0f;

    [Tooltip("Отдача ствола: насколько он уходит назад при выстреле (мировые единицы).")]
    [SerializeField, Min(0f)] private float _recoilKick = 0.12f;

    [Tooltip("Скорость, с которой ствол возвращается после отдачи.")]
    [SerializeField, Min(0.1f)] private float _recoilReturn = 10f;

    [Tooltip("Отдача камеры назад вдоль выстрела (тяжёлые пушки). 0 = выкл.")]
    [SerializeField, Min(0f)] private float _cameraKick = 0f;

    [Tooltip("Масштаб вспышки у дула. 1 = база, больше = мощнее.")]
    [SerializeField, Min(0.1f)] private float _muzzleFlashScale = 1f;

    public enum FireSoundType { Rifle, Shotgun, Sniper }
    [Tooltip("Звук выстрела этого оружия.")]
    [SerializeField] private FireSoundType _fireSound = FireSoundType.Rifle;

    [Header("Настройки камеры")]
    [SerializeField] private Camera _camera;
    [SerializeField] private float _spriteAngleOffset = 0f;

    [Header("Поворот пушки")]
    [SerializeField] private Transform _playerSpriteToRotate;
    [SerializeField] private float _playerSpriteAngleOffset = 0f;

    private PlayerActionsControl _playerActionsControl;
    private Vector3 _MouseWorldPos;
    private bool _hasMouseData = false;
    private bool _isFiring = false;
    private float _nextFireTime = 0f;

    private Transform _playerTransform;

    private Vector3 _baseLocalPos;
    private float _recoilOffset;

    private void Awake()
    {
        _playerActionsControl = new PlayerActionsControl();
        _playerActionsControl.Player.Attack.started += OnFireStarted;
        _playerActionsControl.Player.Attack.canceled += OnFireCanceled;

        _playerTransform = GetComponentInParent<PlayerMovement>()?.transform;
        if (_playerTransform == null && PlayerMovement.Instance != null) _playerTransform = PlayerMovement.Instance.transform;
        if (_camera == null) _camera = Camera.main;
        if (_bulletPool == null) _bulletPool = FindAnyObjectByType<BulletPool>();

        _baseLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (_camera == null || Mouse.current == null) return;
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(screenPos.x, screenPos.y,_camera.WorldToScreenPoint(transform.position).z);
        _MouseWorldPos = _camera.ScreenToWorldPoint(mouseScreen);
        _hasMouseData = true;

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
        if (Time.timeScale == 0f) return;
        if (!_hasMouseData || _camera == null) return;

        // Базовая (без отдачи) позиция ствола в мире.
        Vector3 basePos = transform.parent != null
            ? transform.parent.TransformPoint(_baseLocalPos)
            : _baseLocalPos;

        Vector2 direction = ((Vector2)_MouseWorldPos - (Vector2)basePos).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + _spriteAngleOffset);

        // Отдача: ствол ушёл назад и упруго возвращается.
        _recoilOffset = Mathf.Lerp(_recoilOffset, 0f, 1f - Mathf.Exp(-_recoilReturn * Time.deltaTime));
        transform.position = basePos - (Vector3)(direction * _recoilOffset);
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

        float damageMult = PlayerStats.Instance != null ? PlayerStats.Instance.DamageMultiplier : 1f;
        int totalBullets = _bulletCount + (PlayerStats.Instance != null ? PlayerStats.Instance.ExtraProjectiles : 0);

        PlayFireSound();
        CyberpunkFX.MuzzleFlash(_muzzle.position, _bulletColor, baseAngle, _muzzleFlashScale);

        // Общий «вес» выстрела: тряска, кик камеры, отдача ствола, волна, хит-стоп.
        if (_fireShake > 0f) CyberpunkFX.Shake(_fireShake, 0.12f);
        if (_cameraKick > 0f) CyberpunkFX.Kick(-aimDirection, _cameraKick);
        if (_muzzleShockwave > 0f) CyberpunkFX.Shockwave(_muzzle.position, _muzzleShockwave, _bulletColor);
        if (_fireHitStop > 0f) CyberpunkFX.HitStop(_fireHitStop);
        _recoilOffset = _recoilKick;

        if (_hitscan)
        {
            FireHitscan(baseAngle, damageMult);
            return;
        }

        PooledBullet.Modifiers mods = new PooledBullet.Modifiers
        {
            pierce = _pierceCount,
            ricochet = _ricochetCount,
            ricochetRadius = _ricochetRadius,
            knockback = _knockbackForce,
            pointBlankMult = _pointBlankDamageMult,
            pointBlankRange = _pointBlankRange,
            speedMult = _bulletSpeedMult,
            color = _bulletColor
        };

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
                curBullet.Initialize(_bulletPool, direction, _damage * damageMult, mods);
            }
        }
    }

    // Railgun: мгновенный луч прошивает всех врагов на линии. Останавливается о стену.
    private void FireHitscan(float baseAngle, float damageMult)
    {
        Vector2 origin = _muzzle.position;
        Vector2 dir = Quaternion.Euler(0f, 0f, baseAngle) * Vector2.right;
        Vector2 end = origin + dir * _hitscanRange;

        float dmg = _damage * damageMult;
        float knock = _knockbackForce > 0f ? _knockbackForce : 6f;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, _hitscanWidth, dir, _hitscanRange);
        // CircleCastAll отдаёт попадания по возрастанию дистанции.
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i].collider;
            if (col == null) continue;
            if (col.CompareTag("Bullet") || col.CompareTag("Player")) continue;

            if (col.CompareTag("Enemy"))
            {
                var unit = col.GetComponentInParent<UnitsBase>();
                if (unit != null)
                {
                    unit.TakeDamage(dmg);
                    if (unit is EnemyBase enemy) enemy.ApplyKnockback(dir, knock);
                }
                CyberpunkFX.SpawnHitSpark(hits[i].point, _bulletColor);
                continue;
            }

            // Непробиваемое препятствие — луч обрывается здесь.
            end = hits[i].point;
            break;
        }

        CyberpunkFX.Beam(origin, end, _bulletColor, _hitscanWidth * 1.3f, 0.14f);
    }

    private void PlayFireSound()
    {
        switch (_fireSound)
        {
            case FireSoundType.Shotgun: AudioFX.ShootShotgun(); break;
            case FireSoundType.Sniper:  AudioFX.ShootSniper();  break;
            default:                    AudioFX.ShootRifle();   break;
        }
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
