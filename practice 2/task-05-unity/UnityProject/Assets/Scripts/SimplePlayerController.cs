using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class SimplePlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 6f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
    [SerializeField, Min(0f)] private float gravity = 20f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float verticalSpeed;

    public Vector3 HorizontalVelocity { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        var input = Vector2.zero;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        var forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
        var right = cameraTransform != null ? cameraTransform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        var moveDirection = forward * input.y + right * input.x;
        HorizontalVelocity = moveDirection * moveSpeed;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            var targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            verticalSpeed = -1f;
            if (keyboard.spaceKey.wasPressedThisFrame)
                verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * gravity);
        }
        else
        {
            verticalSpeed -= gravity * Time.deltaTime;
        }

        controller.Move((HorizontalVelocity + Vector3.up * verticalSpeed) * Time.deltaTime);
    }
}
