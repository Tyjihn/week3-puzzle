using UnityEngine;
using UnityEngine.InputSystem;

public class TapestryInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("How close the player must be to enter the tapestry puzzle.")]
    public float interactDistance = 3f;

    [Header("Clue")]
    [Tooltip("The TapInscription object shown while the tapestry is being inspected.")]
    public GameObject clueText;

    [Header("Puzzle")]
    [Tooltip("Manager that validates the clicked crest order.")]
    public CrestPuzzleManager puzzleManager;

    [Header("Player")]
    [Tooltip("PlayerMovement owns both movement and mouse-look in this project.")]
    public PlayerMovement playerMovement;

    public bool IsInteracting { get; private set; }

    private Camera cam;
    private bool lookingAtTapestry;
    private Collider[] stationColliders;
    private bool[] stationColliderStates;
    private bool lookingAround;

    void Start()
    {
        cam = Camera.main;

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (puzzleManager == null)
            puzzleManager = GetComponent<CrestPuzzleManager>();

        if (clueText != null)
            clueText.SetActive(false);

        CacheStationColliders();
    }

    void Update()
    {
        if (IsInteracting)
        {
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitInteractionMode();
                return;
            }

            HandleLookAround();
            HandleCrestClick();
            return;
        }

        if (Keyboard.current == null)
            return;

        if (cam == null)
            cam = Camera.main;

        lookingAtTapestry = IsPlayerLookingAtTapestry();

        if (lookingAtTapestry && Keyboard.current.eKey.wasPressedThisFrame)
            EnterInteractionMode();
    }

    void HandleCrestClick()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || lookingAround || !mouse.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePosition = mouse.position.ReadValue();
        Debug.Log($"Tapestry click detected at screen position: {mousePosition}", this);

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            Debug.LogWarning("Tapestry click raycast could not run because no Main Camera was found.", this);
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Debug.Log($"Tapestry ray hit: {hit.collider.gameObject.name}", hit.collider.gameObject);

        TapestryRewardItem rewardItem = hit.transform.GetComponentInParent<TapestryRewardItem>();
        if (rewardItem != null)
        {
            rewardItem.TrySelect();
            return;
        }

        CrestButton crest = hit.transform.GetComponentInParent<CrestButton>();
        if (crest == null)
            return;

        bool wasSelected = crest.IsSelected;

        if (puzzleManager == null)
            puzzleManager = GetComponent<CrestPuzzleManager>();

        if (puzzleManager != null)
            puzzleManager.RegisterPress(crest);

        if (!wasSelected && crest.IsSelected)
        {
            string displayName = string.IsNullOrWhiteSpace(crest.crestName)
                ? crest.name
                : crest.crestName;

            Debug.Log(
                $"Tapestry CrestButton found and selected: {displayName} ({crest.crestIndex})",
                crest
            );
        }
    }

    void HandleLookAround()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            lookingAround = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (mouse.rightButton.wasReleasedThisFrame)
        {
            lookingAround = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void EnterInteractionMode()
    {
        lookingAround = false;

        if (IsInteracting)
            return;

        IsInteracting = true;
        lookingAtTapestry = false;

        if (clueText != null)
            clueText.SetActive(true);

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.MovementLocked = true;
        }

        SetStationCollidersEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (puzzleManager != null)
            puzzleManager.BeginInteraction();
    }

    public void ExitInteractionMode()
    {
        lookingAround = false;

        if (!IsInteracting)
            return;

        IsInteracting = false;

        if (clueText != null)
            clueText.SetActive(false);

        if (puzzleManager != null)
            puzzleManager.EndInteraction();

        RestoreStationColliders();

        if (playerMovement != null)
            playerMovement.MovementLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    bool IsPlayerLookingAtTapestry()
    {
        if (cam == null)
            return false;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        return Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        ) && (hit.transform == transform || hit.transform.IsChildOf(transform));
    }

    void CacheStationColliders()
    {
        stationColliders = GetComponents<Collider>();
        stationColliderStates = new bool[stationColliders.Length];
    }

    void SetStationCollidersEnabled(bool enabled)
    {
        if (stationColliders == null)
            CacheStationColliders();

        for (int i = 0; i < stationColliders.Length; i++)
        {
            Collider stationCollider = stationColliders[i];
            if (stationCollider == null)
                continue;

            stationColliderStates[i] = stationCollider.enabled;
            stationCollider.enabled = enabled;
        }
    }

    void RestoreStationColliders()
    {
        if (stationColliders == null || stationColliderStates == null)
            return;

        for (int i = 0; i < stationColliders.Length; i++)
        {
            if (stationColliders[i] != null)
                stationColliders[i].enabled = stationColliderStates[i];
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying && IsInteracting)
            ExitInteractionMode();
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };

        style.normal.textColor = Color.white;

        if (IsInteracting)
        {
            GUI.Label(
                new Rect(0, Screen.height - 60, Screen.width, 30),
                "Left click crests     Hold Right Mouse to look     E to exit",
                style
            );
        }
        else if (lookingAtTapestry)
        {
            GUI.Label(
                new Rect(0, Screen.height / 2f + 30, Screen.width, 30),
                "Press E to inspect tapestry",
                style
            );
        }
    }
}
