using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Look at the padlock and press E to zoom in on it. A/D pick a dial, W/S turn it, E/Esc back out.
// Put this on the "Combination PadLock" object, next to its MoveRuller and PadLockPassword scripts.
[RequireComponent(typeof(MoveRuller), typeof(PadLockPassword))]
public class PadlockInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("How close (m) the player must be to inspect the padlock")]
    public float interactDistance = 2f;

    [Header("Zoom")]
    [Tooltip("Where the camera sits while zoomed, relative to the padlock (matches the asset's demo camera)")]
    public Vector3 zoomOffset = new Vector3(0f, 0.11f, 0.59f);
    public float zoomDuration = 0.5f;

    [Header("Unlock")]
    [Tooltip("The shackle that pops up when unlocked (found automatically)")]
    public Transform shackle;
    [Tooltip("How far the shackle lifts, in the padlock's local units")]
    public float shackleLift = 0.06f;
    [Tooltip("Chest to open when unlocked (found automatically if the padlock is a child of it)")]
    public TreasureChest chest;
    [Tooltip("Anything else that should happen when the padlock opens")]
    public UnityEvent onUnlocked;

    private MoveRuller dials;
    private PadLockPassword password;
    private PlayerMovement player;
    private Camera cam;

    private bool zoomedIn;
    private bool busy;
    private bool unlocked;
    private bool lookingAtLock;

    private Vector3 camStartLocalPos;
    private Quaternion camStartLocalRot;

    void Start()
    {
        dials = GetComponent<MoveRuller>();
        password = GetComponent<PadLockPassword>();
        player = FindAnyObjectByType<PlayerMovement>();
        cam = Camera.main;

        if (shackle == null) shackle = transform.Find("PadlockRing");
        if (chest == null) chest = GetComponentInParent<TreasureChest>();

        password.onCorrect.AddListener(() => StartCoroutine(Unlock()));
    }

    void Update()
    {
        if (unlocked || busy || Keyboard.current == null) return;

        if (zoomedIn)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                StartCoroutine(ZoomOut());
            }
            return;
        }

        lookingAtLock = IsPlayerLookingAtLock();
        if (lookingAtLock && Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(ZoomIn());
        }
    }

    bool IsPlayerLookingAtLock()
    {
        if (cam == null) return false;

        // The ray starts inside the player's own collider, so it doesn't hit the player
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        return Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore)
            && hit.transform.IsChildOf(transform);
    }

    IEnumerator ZoomIn()
    {
        busy = true;
        lookingAtLock = false;

        // Freeze the player so W/A/S/D only turn the dials
        if (player != null) player.enabled = false;

        camStartLocalPos = cam.transform.localPosition;
        camStartLocalRot = cam.transform.localRotation;

        // Face the numbers: sit at the offset, looking back at the padlock
        Vector3 targetPos = transform.TransformPoint(zoomOffset);
        Quaternion targetRot = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        yield return MoveCamera(targetPos, targetRot);

        dials.SetActive(true);
        zoomedIn = true;
        busy = false;
    }

    IEnumerator ZoomOut()
    {
        busy = true;
        zoomedIn = false;
        dials.SetActive(false);

        // Back to where the camera was inside the player
        Transform parent = cam.transform.parent;
        yield return MoveCamera(parent.TransformPoint(camStartLocalPos), parent.rotation * camStartLocalRot);
        cam.transform.localPosition = camStartLocalPos;
        cam.transform.localRotation = camStartLocalRot;

        if (player != null) player.enabled = true;
        busy = false;
    }

    IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;

        for (float t = 0f; t < 1f; t += Time.deltaTime / zoomDuration)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, targetPos, k), Quaternion.Slerp(fromRot, targetRot, k));
            yield return null;
        }
        cam.transform.SetPositionAndRotation(targetPos, targetRot);
    }

    IEnumerator Unlock()
    {
        unlocked = true;
        busy = true;
        dials.SetActive(false);

        // Pop the shackle up while the player is still looking at it
        if (shackle != null)
        {
            Vector3 closed = shackle.localPosition;
            Vector3 open = closed + Vector3.up * shackleLift;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.3f)
            {
                shackle.localPosition = Vector3.Lerp(closed, open, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            shackle.localPosition = open;
        }

        yield return new WaitForSeconds(0.5f);
        yield return ZoomOut();

        if (chest != null) chest.Open();
        onUnlocked.Invoke();
    }

    void OnGUI()
    {
        if (unlocked) return;

        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        style.normal.textColor = Color.white;

        if (zoomedIn)
        {
            GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 30),
                "A / D  choose dial     W / S  turn dial     E / Esc  back", style);
        }
        else if (lookingAtLock && !busy)
        {
            GUI.Label(new Rect(0, Screen.height / 2f + 30, Screen.width, 30), "Press E to inspect padlock", style);
        }
    }
}
