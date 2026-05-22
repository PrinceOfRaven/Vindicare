using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : UnitsBase
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Спрайт игрока")]
    [SerializeField] private SpriteRenderer _sr;
    [SerializeField] private Sprite _frontSprite;
    [SerializeField] private Sprite _backSprite;

    private PlayerActionsControl _actions;
    private Vector2 _moveInput;


    private float _lastFacingX = 1f;
    private float _lastFacingY = -1f; 

    public Vector2 FacingDirection => new Vector2(_lastFacingX, _lastFacingY);
    public SpriteRenderer BodySprite => _sr;
    public Vector2 MoveInput => _moveInput;

    protected override void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        _actions = new PlayerActionsControl();
        CacheMaxHealth();

        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        EnsureChildSprite();
    }

    /// <summary>
    /// Если спрайт игрока висит на корне (там физика и коллайдер), переносим
    /// рендер на дочерний объект — тогда его можно анимировать масштабом/позицией,
    /// не трогая физику. Дальше PlayerMovement работает уже с дочерним спрайтом.
    /// </summary>
    private void EnsureChildSprite()
    {
        if (_sr == null || _sr.transform != transform) return;

        var rootSr = _sr;
        var child = new GameObject("Body");
        child.transform.SetParent(transform, false);
        child.layer = gameObject.layer;

        var childSr = child.AddComponent<SpriteRenderer>();
        childSr.sprite          = rootSr.sprite;
        childSr.sharedMaterial  = rootSr.sharedMaterial;
        childSr.color           = rootSr.color;
        childSr.flipX           = rootSr.flipX;
        childSr.flipY           = rootSr.flipY;
        childSr.sortingLayerID  = rootSr.sortingLayerID;
        childSr.sortingOrder    = rootSr.sortingOrder;
        childSr.maskInteraction = rootSr.maskInteraction;
        childSr.spriteSortPoint = rootSr.spriteSortPoint;

        rootSr.enabled = false;  // рендер с корня убираем — рисует дочерний
        _sr = childSr;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable() => _actions.Player.Enable();
    private void OnDisable() => _actions.Player.Disable();

    private void Update()
    {
        _moveInput = _actions.Player.Move.ReadValue<Vector2>();

        if (_moveInput.sqrMagnitude > 1)
        {
            _moveInput.Normalize();
        }

        UpdateFacing();
    }

    private void UpdateFacing()
    {
        const float deadzone = 0.3f;

        if (Mathf.Abs(_moveInput.x) > deadzone)
            _lastFacingX = Mathf.Sign(_moveInput.x);

        if (Mathf.Abs(_moveInput.y) > deadzone)
            _lastFacingY = Mathf.Sign(_moveInput.y);

        if (_sr == null) return;

        if (_lastFacingY > 0f && _backSprite != null)
        {
            _sr.sprite = _backSprite;  
        }
        else if (_frontSprite != null)
        {
            _sr.sprite = _frontSprite; 
        }
        _sr.flipX = _lastFacingX < 0f;
    }

    private void FixedUpdate()
    {
        float speedMult = PlayerStats.Instance != null
            ? PlayerStats.Instance.MoveSpeedMultiplier
            : 1f;
        rb.linearVelocity = _moveInput * _speed * speedMult;
    }

    public void HealByMaxHealthUpgrade(int bonusHP)
    {
        _maxHealth += bonusHP;
        _health = Mathf.Min(_health + bonusHP, _maxHealth);
    }

    /// <summary>Восстановить здоровье (от аптечки). Не превышает максимум.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || !IsAlive) return;
        _health = Mathf.Min(_health + amount, _maxHealth);
        CyberpunkFX.DamagePopup(transform.position, amount, CyberpunkFX.Lime);
    }

    protected override void onObjectDeath()
    {
        RaiseDeath();
        CyberpunkFX.SpawnDeathBurst(transform.position, CyberpunkFX.HotRed);
        CyberpunkFX.Shake(0.35f, 0.5f);
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
        else
            Debug.LogError("[PlayerMovement] GameOverUI.Instance == null! Проверь что GameOverPanel в сцене активен.");
    }

    public override bool TakeDamage(float amount)
    {
        bool died = base.TakeDamage(amount);
        if (!died)
        {
            CyberpunkFX.Shake(0.20f, 0.18f);
            CyberpunkFX.HitStop(0.05f);
        }
        return died;
    }

    private void Start()
    {
        CyberpunkFX.AttachLight(transform, CyberpunkFX.Amber, intensity: 1.1f, outerRadius: 4.5f, innerRadius: 0.3f);

        // Анимация ходьбы (покачивание + squash/stretch) — без правок сцены
        if (GetComponent<PlayerWalkAnimation>() == null)
            gameObject.AddComponent<PlayerWalkAnimation>();
    }
}