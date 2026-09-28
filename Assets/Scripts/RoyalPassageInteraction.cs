using UnityEngine;
using UnityEngine.InputSystem;

public class RoyalPassageInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("References")]
    public PlayerMovement playerMovement;
    public RoyalPassageManager puzzleManager;

    public bool IsInteracting { get; private set; }

    private Camera cam;
    private bool lookingAtPassage;
    private bool lookingAround;

    private string statusMessage = "";
    private float statusUntil;

    void Start()
    {
        cam = Camera.main;

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (puzzleManager == null)
            puzzleManager = GetComponent<RoyalPassageManager>();
    }

    void Update()
    {
        if (IsInteracting)
        {
            // E exits
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitInteraction();
                return;
            }

            // Enter submits
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                if (puzzleManager != null)
                {
                    RoyalSubmitResult result =
                        puzzleManager.Submit(out string message);

                    ShowStatus(message);

                    // Leave inspection after successful escape.
                    if (result == RoyalSubmitResult.Correct)
                    {
                        Invoke(nameof(ExitInteraction), 1f);
                    }
                }

                return;
            }

            HandleLookAround();
            HandleLockClick();

            return;
        }

        if (Keyboard.current == null)
            return;

        if (cam == null)
            cam = Camera.main;

        lookingAtPassage = IsLookingAtPassage();

        if (lookingAtPassage &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryEnterInteraction();
        }
    }

    void TryEnterInteraction()
    {
        if (puzzleManager != null &&
            !puzzleManager.CanAttempt(out string reason))
        {
            ShowStatus(reason);
            return;
        }

        IsInteracting = true;
        lookingAtPassage = false;
        lookingAround = false;

        if (playerMovement != null)
            playerMovement.MovementLocked = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitInteraction()
    {
        if (!IsInteracting)
            return;

        IsInteracting = false;
        lookingAround = false;

        if (playerMovement != null)
            playerMovement.MovementLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    void HandleLockClick()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            lookingAround ||
            !mouse.leftButton.wasPressedThisFrame)
            return;

        Camera activeCamera = Camera.main;

        if (activeCamera == null)
            return;

        Ray ray =
            activeCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                ~0,
                QueryTriggerInteraction.Collide))
            return;

        RoyalLock royalLock =
            hit.transform.GetComponentInParent<RoyalLock>();

        if (royalLock != null)
            royalLock.CycleColor();
    }

    bool IsLookingAtPassage()
    {
        if (cam == null)
            return false;

        Ray ray =
            new Ray(cam.transform.position, cam.transform.forward);

        return Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactDistance,
            ~0,
            QueryTriggerInteraction.Collide
        )
        && (
            hit.transform == transform ||
            hit.transform.IsChildOf(transform)
        );
    }

    void ShowStatus(string message)
    {
        statusMessage = message;
        statusUntil = Time.time + 2.5f;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };

        style.normal.textColor = Color.white;

        if (Time.time < statusUntil)
        {
            GUI.Label(
                new Rect(
                    0,
                    Screen.height / 2f + 60,
                    Screen.width,
                    30
                ),
                statusMessage,
                style
            );
        }

        if (IsInteracting)
        {
            string hint = "";

            if (PuzzleGameState.Instance != null &&
                PuzzleGameState.Instance.HasGateHint)
            {
                hint =
                    "\nHint: The inscription tells you the colors. The symbol tells you where they go.";
            }

            GUI.Label(
                new Rect(
                    0,
                    Screen.height - 90,
                    Screen.width,
                    60
                ),
                "Click locks to change color     Enter to submit     Hold Right Mouse to look     E to exit"
                + hint,
                style
            );
        }
        else if (lookingAtPassage &&
                 (puzzleManager == null || !puzzleManager.Solved))
        {
            GUI.Label(
                new Rect(
                    0,
                    Screen.height / 2f + 30,
                    Screen.width,
                    30
                ),
                "Press E to inspect Royal Passage — Cost: 2 Flames",
                style
            );
        }
    }
}