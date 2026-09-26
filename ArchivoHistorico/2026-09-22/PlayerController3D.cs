using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerController3D : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 7f;
    public float acceleration = 24f;
    [Range(0f, 1f)] public float airControl = 0.72f;
    public float gravity = -24f;
    [Header("Salto")]
    public float jumpHeight = 2.15f;
    public float coyoteTime = 0.14f;
    public float jumpBuffer = 0.14f;
    public float unlockAfterSeconds = 0f;
    public Transform cameraTransform, spawnPoint;

    CharacterController controller;
    Vector3 planarVelocity, verticalVelocity;
    float lastGroundedTime = -10f, lastJumpPressedTime = -10f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Keyboard keyboard = Keyboard.current;
        bool unlocked = Time.timeSinceLevelLoad >= unlockAfterSeconds;
        bool grounded = controller.isGrounded;
        if (grounded) lastGroundedTime = Time.time;
        if (unlocked && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            lastJumpPressedTime = Time.time;

        Vector2 input = Vector2.zero;
        if (unlocked && keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        }

        Vector3 forward = cameraTransform == null ? Vector3.forward : Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = cameraTransform == null ? Vector3.right : Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 desiredMove = forward * input.y + right * input.x;
        if (desiredMove.sqrMagnitude > 1f) desiredMove.Normalize();
        Vector3 targetVelocity = desiredMove * moveSpeed;
        float control = grounded ? 1f : airControl;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, acceleration * control * dt);

        if (desiredMove.sqrMagnitude > 0.01f)
            transform.forward = Vector3.Slerp(transform.forward, desiredMove, 12f * dt);

        bool bufferedJump = Time.time - lastJumpPressedTime <= jumpBuffer;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (bufferedJump && canJump)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
        }
        else if (grounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * dt;
        controller.Move((planarVelocity + verticalVelocity) * dt);
    }

    public void ResetToSpawn()
    {
        if (spawnPoint == null) return;
        controller.enabled = false;
        transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        controller.enabled = true;
        planarVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        lastGroundedTime = Time.time;
    }
}
