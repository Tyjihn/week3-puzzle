using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    [Tooltip("Speed while holding Left Shift (8 plays the full sprint animation)")]
    public float sprintSpeed = 8.0f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    [Tooltip("The child object holding the camera (tilts up/down)")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float maxLookAngle = 80f;

    [Header("Crouch")]
    public float crouchSpeed = 2.0f;
    [Tooltip("Collision capsule height while crouched")]
    public float crouchHeight = 1.2f;
    [Tooltip("How far the camera lowers while crouched")]
    public float crouchCameraDrop = 0.6f;
    [Tooltip("How quickly you move between standing and crouching")]
    public float crouchTransitionSpeed = 10f;

    [Header("Head Bob")]
    [Tooltip("Roughly how far (m) the camera dips when you jump")]
    public float jumpBob = 0.06f;
    [Tooltip("Landing dip (m) per 1 m/s of fall speed, so bigger falls hit harder")]
    public float landBobPerSpeed = 0.025f;
    [Tooltip("Largest landing dip (m), however far you fall")]
    public float maxLandBob = 0.25f;
    [Tooltip("Falls slower than this (m/s) don't bob, e.g. stepping off a small ledge")]
    public float minLandSpeed = 2f;
    [Tooltip("How snappy the bob is (higher = quicker)")]
    public float bobStiffness = 120f;
    [Tooltip("How quickly the bob settles (lower = more bounce)")]
    public float bobDamping = 14f;

    // Read by PlayerAnimation to pick the crouch animations
    public bool IsCrouching { get; private set; }

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch;

    private float standingHeight;
    private float feetOffset;
    private float standingCameraY;
    private float cameraBaseY;

    private float bobOffset;
    private float bobVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            cameraPivot = transform.Find("CameraPivot");
        }

        // Remember the standing shape so crouching can shrink and restore it
        standingHeight = controller.height;
        feetOffset = controller.center.y - controller.height / 2f;
        if (cameraPivot != null)
        {
            standingCameraY = cameraPivot.localPosition.y;
        }
        cameraBaseY = standingCameraY;

        LockCursor(true);
    }

    void Update()
    {
        Look();
        Crouch();

        // Ground check
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // --- NEW INPUT SYSTEM CODE ---
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        }
        // ------------------------------

        // Hold Left Shift to sprint (crouching overrides it)
        bool sprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float speed = IsCrouching ? crouchSpeed : sprinting ? sprintSpeed : moveSpeed;
        Vector3 move = (transform.right * x + transform.forward * z).normalized * speed;

        // Jump (not while crouched)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && controller.isGrounded && !IsCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            AddBob(jumpBob);
        }

        // Apply Gravity
        velocity.y += gravity * Time.deltaTime;

        // Remember this before moving, to spot the frame we touch down
        bool wasGrounded = controller.isGrounded;
        float fallSpeed = -velocity.y;

        // Single Move per frame so isGrounded stays reliable
        move.y = velocity.y;
        controller.Move(move * Time.deltaTime);

        // Landing bob, bigger the faster we were falling
        if (!wasGrounded && controller.isGrounded && fallSpeed > minLandSpeed)
        {
            AddBob(Mathf.Min(fallSpeed * landBobPerSpeed, maxLandBob));
        }
    }

    // Kicks the camera downward; the spring in UpdateBob pulls it back up
    void AddBob(float amount)
    {
        // Scaled so the dip's depth comes out close to 'amount'
        bobVelocity -= amount * Mathf.Sqrt(bobStiffness) * 2.7f;
    }

    void UpdateBob()
    {
        float dt = Time.deltaTime;
        bobVelocity += (-bobStiffness * bobOffset - bobDamping * bobVelocity) * dt;
        bobOffset += bobVelocity * dt;
    }

    void Crouch()
    {
        // Hold C or Left Ctrl to crouch
        bool wantsCrouch = Keyboard.current != null &&
            (Keyboard.current.cKey.isPressed || Keyboard.current.leftCtrlKey.isPressed);

        if (wantsCrouch)
        {
            IsCrouching = true;
        }
        else if (IsCrouching && CanStandUp())
        {
            IsCrouching = false;
        }

        // Smoothly shrink/grow the capsule, keeping the feet in place
        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        float height = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        if (Mathf.Abs(height - targetHeight) < 0.001f) height = targetHeight;
        if (height != controller.height)
        {
            controller.height = height;
            controller.center = new Vector3(controller.center.x, feetOffset + height / 2f, controller.center.z);
        }

        // Lower the camera with the body (applied in LateUpdate)
        float targetY = IsCrouching ? standingCameraY - crouchCameraDrop : standingCameraY;
        cameraBaseY = Mathf.Lerp(cameraBaseY, targetY, crouchTransitionSpeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (cameraPivot == null) return;

        UpdateBob();

        // Final camera height = crouch/stand height + head bob
        Vector3 pos = cameraPivot.localPosition;
        pos.y = cameraBaseY + bobOffset;
        cameraPivot.localPosition = pos;
    }

    // Stay crouched if something is overhead, so you can't stand up into a ceiling
    bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        Vector3 topSphere = transform.position + Vector3.up * (feetOffset + controller.height - controller.radius);
        float distance = standingHeight - controller.height;
        return !Physics.SphereCast(topSphere, radius, Vector3.up, out _, distance, ~0, QueryTriggerInteraction.Ignore);
    }

    void Look()
    {
        // Escape frees the cursor, clicking in the game view locks it again
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            LockCursor(false);
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor(true);
        }

        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

        // Mouse delta is already per-frame, so no Time.deltaTime here
        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // Left/right turns the whole body, so WASD follows the view
        transform.Rotate(Vector3.up * delta.x);

        // Up/down only tilts the camera, clamped so you can't flip over
        pitch = Mathf.Clamp(pitch - delta.y, -maxLookAngle, maxLookAngle);
        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}