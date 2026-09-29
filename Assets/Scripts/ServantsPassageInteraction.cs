using UnityEngine;
using UnityEngine.InputSystem;

public class ServantsPassageInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("References")]
    public PlayerMovement playerMovement;
    public ServantsPassageManager puzzleManager;

    public bool IsInteracting { get; private set; }

    private Camera cam;
    private bool lookingAtPassage;
    private bool lookingAround;
    private PuzzleCameraFocus cameraFocus;

    private string statusMessage = "";
    private float statusUntil;

    void Start()
    {
        cam = Camera.main;

        cameraFocus = GetComponent<PuzzleCameraFocus>();
        if (cameraFocus == null)
        {
            cameraFocus = gameObject.AddComponent<PuzzleCameraFocus>();
            cameraFocus.front = PuzzleCameraFocus.FrontAxis.LocalNegativeZ;
        }

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (puzzleManager == null)
            puzzleManager = GetComponent<ServantsPassageManager>();
    }

    void Update()
    {
        if (IsInteracting)
        {
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitInteraction();
                return;
            }

            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                if (puzzleManager != null)
                {
                    ServantsSubmitResult result =
                        puzzleManager.Submit(out string message);

                    ShowStatus(message);

                    if (result == ServantsSubmitResult.Correct)
                    {
                        Invoke(nameof(ExitInteraction), 1f);
                    }
                }

                return;
            }

            HandleLookAround();
            HandleBellClick();

            return;
        }

        if (Keyboard.current == null)
            return;

        if (puzzleManager != null && puzzleManager.Solved)
        {
            lookingAtPassage = false;
            return;
        }

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
        if (IsInteracting || (puzzleManager != null && puzzleManager.Solved))
            return;

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

        if (cameraFocus != null)
            cameraFocus.Focus();
    }

    public void ExitInteraction()
    {
        if (!IsInteracting)
            return;

        IsInteracting = false;
        lookingAround = false;

        if (cameraFocus != null)
            cameraFocus.Unfocus();

        if (puzzleManager != null && !puzzleManager.Solved)
            puzzleManager.ResetAttempt();

        if (playerMovement != null)
            playerMovement.MovementLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void HandleLookAround()
    {
        LookAroundInput.Update(ref lookingAround);
    }

    void HandleBellClick()
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

        ServantsBell bell =
            hit.transform.GetComponentInParent<ServantsBell>();

        if (bell != null)
            bell.Ring(puzzleManager);
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
                    "\nHint: One bell must answer its own call.";
            }

            GUI.Label(
                new Rect(
                    0,
                    Screen.height - 90,
                    Screen.width,
                    60
                ),
                "Click bells     Enter to submit     " + LookAroundInput.PromptText + "     E to exit"
                + hint,
                style
            );
        }
        else if (lookingAtPassage &&
                 (puzzleManager == null || !puzzleManager.Solved))
        {
            HoverPromptGUI.Draw(
                new Rect(
                    0,
                    Screen.height / 2f + 30,
                    Screen.width,
                    30
                ),
                "Press E to inspect Servants' Passage — Cost: 1 Flame",
                style
            );
        }
    }
}