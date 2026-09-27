using UnityEngine;

// Feeds movement info from the CharacterController into the Animator(s)
[RequireComponent(typeof(CharacterController))]
public class PlayerAnimation : MonoBehaviour
{
    [Tooltip("Smooths the Speed value so animations blend instead of snapping")]
    public float speedDampTime = 0.1f;

    [Tooltip("Short grace period so tiny bumps don't trigger the jump animation")]
    public float groundedGrace = 0.1f;

    [Tooltip("Start the landing animation this many seconds before touching down, so its impact lines up with the real one")]
    public float landingLeadTime = 0.15f;

    private CharacterController controller;
    private PlayerMovement movement;
    private Animator[] animators;
    private float lastGroundedTime;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int CrouchingHash = Animator.StringToHash("Crouching");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        // Finds both the visible body and the shadow-only copy made by HideHead
        animators = GetComponentsInChildren<Animator>();

        // PlayerMovement already moves the player, so ignore movement baked into the clips
        foreach (Animator animator in animators)
        {
            animator.applyRootMotion = false;
        }
    }

    void Update()
    {
        // Horizontal speed drives Idle -> Walk -> Jog -> Sprint
        Vector3 horizontal = controller.velocity;
        horizontal.y = 0f;

        // controller.velocity keeps its last value while movement is switched off (e.g. using the padlock)
        if (movement != null && !movement.enabled) horizontal = Vector3.zero;

        // Moving upward means we just jumped, even if the grace period hasn't run out
        if (controller.isGrounded)
        {
            lastGroundedTime = Time.time;
        }
        bool grounded = Time.time - lastGroundedTime <= groundedGrace && controller.velocity.y <= 0.1f;

        // Report "grounded" slightly early while falling, so the landing is already blending in at touchdown
        grounded = grounded || GroundIsClose();

        bool crouching = movement != null && movement.IsCrouching;

        // Same values to every body so the shadow stays in sync with the visible one
        foreach (Animator animator in animators)
        {
            animator.SetFloat(SpeedHash, horizontal.magnitude, speedDampTime, Time.deltaTime);
            animator.SetBool(GroundedHash, grounded);
            animator.SetBool(CrouchingHash, crouching);
        }
    }

    // True if we're falling and will hit the ground within landingLeadTime
    bool GroundIsClose()
    {
        float fallSpeed = -controller.velocity.y;
        if (fallSpeed < 1f) return false;

        // Cast the bottom of the capsule downward; it starts inside our own collider, so that's ignored
        float radius = controller.radius * 0.95f;
        Vector3 bottomSphere = transform.TransformPoint(controller.center) + Vector3.down * (controller.height / 2f - controller.radius);
        float distance = fallSpeed * landingLeadTime + controller.skinWidth;
        return Physics.SphereCast(bottomSphere, radius, Vector3.down, out _, distance, ~0, QueryTriggerInteraction.Ignore);
    }
}
