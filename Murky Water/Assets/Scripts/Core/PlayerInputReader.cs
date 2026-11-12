using UnityEngine;
using UnityEngine.InputSystem;

/// Reads the movement vector from an action in the PlayerControls.
/// This component does not move the fish; it only exposes input for PlayerController.

public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    /// Normalized input vector; the Y axis controls vertical swimming.
    public Vector2 MoveInput { get; private set; }

    public float VerticalInput => MoveInput.y;

    private void OnEnable()
    {
        if (moveAction == null || moveAction.action == null)
        {
            Debug.LogError($"{nameof(PlayerInputReader)} on {name}: the Move action from the PlayerControls has not been assigned.", this);
            return;
        }

        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
        moveAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction == null || moveAction.action == null) return;

        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        moveAction.action.Disable();
        MoveInput = Vector2.zero;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
    }
}
