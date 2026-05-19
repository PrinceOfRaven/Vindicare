using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : UnitsBase
{
    public static PlayerMovement Instance { get; private set; }

    private PlayerActionsControl _actions;
    private Vector2 _moveInput;

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

    public override bool TakeDamage(float amount)
    {
        if (!IsAlive) return true;

        _health -= Mathf.Max(1, Mathf.RoundToInt(amount));
        if (_health <= 0)
        {
            _health = 0;
            onObjectDeath();
            return true;
        }
        return false;
    }

    protected override void onObjectDeath()
    {
        RaiseDeath();
        if (GameOverUI.Instance != null)
            GameOverUI.Instance.Show();
    }
}
