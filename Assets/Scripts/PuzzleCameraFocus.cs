using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Glides the main camera to a centered, head-on view of a puzzle while it is being inspected,
// then glides back to the player's view on exit. The camera keeps its eye-level height.
// Interaction scripts add this automatically; add it manually to tweak the settings per puzzle.
[DefaultExecutionOrder(100)] // After PlayerMovement.LateUpdate, so the pinned camera position wins.
public class PuzzleCameraFocus : MonoBehaviour
{
    public enum FrontAxis { LocalPositiveZ, LocalNegativeZ, LocalPositiveX, LocalNegativeX }

    [Tooltip("Which side of this object is the puzzle's front. The camera always views the puzzle from this side, regardless of where the player stood.")]
    public FrontAxis front = FrontAxis.LocalNegativeZ;

    [Tooltip("What the camera centers on. Leave empty to use the combined bounds of this object's renderers.")]
    public Transform focusPoint;

    [Tooltip("Distance from the puzzle. 0 = fit the puzzle on screen automatically.")]
    [Min(0f)]
    public float viewDistance = 0f;

    [Tooltip("Extra room around the puzzle when the distance is automatic.")]
    [Min(1f)]
    public float framingPadding = 1.25f;

    [Min(0.1f)]
    public float minDistance = 0.8f;

    [Tooltip("Upper limit for the automatic distance, so the camera doesn't back through walls.")]
    [Min(0.1f)]
    public float maxDistance = 2.5f;

    [Min(0.01f)]
    public float glideDuration = 0.6f;

    [Tooltip("Hide the player's body while focused so it can't block the view.")]
    public bool hidePlayerBody = true;

    public bool IsFocused => state == State.Focusing || state == State.Focused;

    private enum State { Idle, Focusing, Focused, Returning }

    private State state = State.Idle;
    private Camera cam;
    private Transform camParent;
    private Vector3 homeLocalPosition;
    private Quaternion homeLocalRotation;

    private Vector3 fromPosition;
    private Quaternion fromRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float t;

    private Renderer[] hiddenRenderers;
    private PlayerMovement player;

    // Free-look while focused, kept as yaw/pitch so the view never rolls.
    private float lookYaw;
    private float lookPitch;

    public void Focus()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null || IsFocused)
            return;

        // Only remember the home pose if we aren't mid-return (it is still valid then).
        if (state == State.Idle)
        {
            camParent = cam.transform.parent;
            homeLocalPosition = cam.transform.localPosition;
            homeLocalRotation = cam.transform.localRotation;
        }

        ComputeTargetPose(out targetPosition, out targetRotation);

        Vector3 euler = targetRotation.eulerAngles;
        lookYaw = euler.y;
        lookPitch = Mathf.DeltaAngle(0f, euler.x);

        // The player's own mouse-look would swing the detached camera around the body, so pause it.
        if (player == null)
            player = FindAnyObjectByType<PlayerMovement>();
        if (player != null)
            player.LookLocked = true;

        BeginGlide(State.Focusing);
        SetPlayerBodyHidden(true);
    }

    public void Unfocus()
    {
        if (cam == null || !IsFocused)
            return;

        BeginGlide(State.Returning);
    }

    void BeginGlide(State newState)
    {
        fromPosition = cam.transform.position;
        fromRotation = cam.transform.rotation;
        t = 0f;
        state = newState;
    }

    void LateUpdate()
    {
        if (state == State.Idle || cam == null)
            return;

        switch (state)
        {
            case State.Focusing:
            {
                float k = Advance();
                cam.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, targetPosition, k),
                    Quaternion.Slerp(fromRotation, targetRotation, k));

                if (t >= 1f)
                    state = State.Focused;
                break;
            }

            case State.Focused:
                UpdateFreeLook();
                cam.transform.SetPositionAndRotation(
                    targetPosition,
                    Quaternion.Euler(lookPitch, lookYaw, 0f));
                break;

            case State.Returning:
            {
                float k = Advance();

                // Recompute home every frame in case the player's head moved.
                Vector3 homePosition = camParent != null
                    ? camParent.TransformPoint(homeLocalPosition)
                    : homeLocalPosition;
                Quaternion homeRotation = camParent != null
                    ? camParent.rotation * homeLocalRotation
                    : homeLocalRotation;

                cam.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, homePosition, k),
                    Quaternion.Slerp(fromRotation, homeRotation, k));

                if (t >= 1f)
                {
                    cam.transform.localPosition = homeLocalPosition;
                    cam.transform.localRotation = homeLocalRotation;
                    state = State.Idle;
                    SetPlayerBodyHidden(false);
                    SetPlayerLookLocked(false);
                }
                break;
            }
        }
    }

    // "Hold to look" locks the cursor; turn the camera in place from the mouse, like the player's own look.
    void UpdateFreeLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
            return;

        float sensitivity = player != null ? player.mouseSensitivity : 0.1f;
        float maxPitch = player != null ? player.maxLookAngle : 80f;

        Vector2 delta = mouse.delta.ReadValue() * sensitivity;
        lookYaw += delta.x;
        lookPitch = Mathf.Clamp(lookPitch - delta.y, -maxPitch, maxPitch);
    }

    void SetPlayerLookLocked(bool locked)
    {
        if (player != null)
            player.LookLocked = locked;
    }

    float Advance()
    {
        t = Mathf.Min(1f, t + Time.unscaledDeltaTime / glideDuration);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    void ComputeTargetPose(out Vector3 position, out Quaternion rotation)
    {
        Bounds bounds = GetFocusBounds();
        Vector3 center = bounds.center;
        Vector3 camPosition = cam.transform.position;

        // Always straight out from the puzzle's front, through its middle.
        Vector3 frontDirection = GetFrontDirection();
        Vector3 right = Vector3.Cross(Vector3.up, frontDirection);

        float distance = viewDistance > 0f
            ? viewDistance
            : Mathf.Clamp(FitDistance(bounds, frontDirection, right), minDistance, maxDistance);

        position = center + frontDirection * distance;
        position.y = camPosition.y; // Stay at eye level.

        Vector3 lookDirection = center - position;
        rotation = lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection, Vector3.up)
            : cam.transform.rotation;
    }

    Vector3 GetFrontDirection()
    {
        Vector3 direction;
        switch (front)
        {
            case FrontAxis.LocalPositiveZ: direction = transform.forward; break;
            case FrontAxis.LocalPositiveX: direction = transform.right; break;
            case FrontAxis.LocalNegativeX: direction = -transform.right; break;
            default: direction = -transform.forward; break;
        }

        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    float FitDistance(Bounds bounds, Vector3 front, Vector3 right)
    {
        Vector3 e = bounds.extents;
        float halfWidth = Mathf.Abs(e.x * right.x) + Mathf.Abs(e.z * right.z);
        float halfDepth = Mathf.Abs(e.x * front.x) + Mathf.Abs(e.z * front.z);
        float halfHeight = e.y;

        float verticalHalfFov = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float horizontalHalfFov = Mathf.Atan(Mathf.Tan(verticalHalfFov) * cam.aspect);

        float fit = Mathf.Max(
            halfHeight / Mathf.Tan(verticalHalfFov),
            halfWidth / Mathf.Tan(horizontalHalfFov));

        return halfDepth + fit * framingPadding;
    }

    Bounds GetFocusBounds()
    {
        if (focusPoint != null)
        {
            Renderer[] pointRenderers = focusPoint.GetComponentsInChildren<Renderer>();
            if (pointRenderers.Length == 0)
                return new Bounds(focusPoint.position, Vector3.zero);

            return Encapsulate(pointRenderers, focusPoint.position);
        }

        return Encapsulate(GetComponentsInChildren<Renderer>(), transform.position);
    }

    static Bounds Encapsulate(Renderer[] renderers, Vector3 fallbackCenter)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(fallbackCenter, Vector3.zero);

        foreach (Renderer r in renderers)
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer)
                continue;

            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return bounds;
    }

    void SetPlayerBodyHidden(bool hidden)
    {
        if (!hidePlayerBody)
            return;

        if (hidden)
        {
            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
            if (player == null)
                return;

            // Keep shadow-only copies (see HideHead) so the player's shadow stays.
            Renderer[] all = player.GetComponentsInChildren<Renderer>();
            var visible = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer r in all)
            {
                if (r.shadowCastingMode != ShadowCastingMode.ShadowsOnly && !r.forceRenderingOff)
                    visible.Add(r);
            }

            hiddenRenderers = visible.ToArray();
            foreach (Renderer r in hiddenRenderers)
                r.forceRenderingOff = true;
        }
        else if (hiddenRenderers != null)
        {
            foreach (Renderer r in hiddenRenderers)
            {
                if (r != null)
                    r.forceRenderingOff = false;
            }

            hiddenRenderers = null;
        }
    }

    void OnDisable()
    {
        if (state != State.Idle && cam != null)
        {
            cam.transform.localPosition = homeLocalPosition;
            cam.transform.localRotation = homeLocalRotation;
            state = State.Idle;
        }

        SetPlayerBodyHidden(false);
        SetPlayerLookLocked(false);
    }
}
