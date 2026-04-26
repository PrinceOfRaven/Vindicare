using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : UnitsBase
{
    private PlayerActionsControl _actions;
    private Vector2 _moveInput;

    protected override void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _actions = new PlayerActionsControl();
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
        rb.linearVelocity = _moveInput * _speed;
    }
}
