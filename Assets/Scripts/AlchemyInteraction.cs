using UnityEngine;
using UnityEngine.InputSystem;

public class AlchemyInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("References")]
    public PlayerMovement playerMovement;
    public AlchemyPuzzleManager puzzleManager;

    [Header("Clue")]
    [Tooltip("The RecipeClue object shown only while inspecting the alchemy puzzle.")]
    public GameObject clueRoot;

    public bool IsInteracting { get; private set; }

    private Camera cam;
    private bool lookingAtStation;
    private bool lookingAround;
    private PuzzleCameraFocus cameraFocus;

    private float successMessageUntil;
    private float failureMessageUntil;

    void Start()
    {
        cam = Camera.main;

        cameraFocus = GetComponent<PuzzleCameraFocus>();
        if (cameraFocus == null)
        {
            cameraFocus = gameObject.AddComponent<PuzzleCameraFocus>();
            cameraFocus.front = PuzzleCameraFocus.FrontAxis.LocalPositiveX;
        }

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (puzzleManager == null)
            puzzleManager = GetComponent<AlchemyPuzzleManager>();

        // Hide the recipe clue until the player inspects the puzzle
        if (clueRoot != null)
            clueRoot.SetActive(false);
    }

    void Update()
    {
        if (IsInteracting)
        {
            // E exits inspection mode
            if (Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitInteraction();
                return;
            }

            HandleLookAround();
            HandleBottleClick();

            return;
        }

        if (Keyboard.current == null)
            return;

        if (puzzleManager != null && puzzleManager.Solved)
        {
            lookingAtStation = false;
            return;
        }

        if (cam == null)
            cam = Camera.main;

        lookingAtStation = IsLookingAtStation();

        if (lookingAtStation &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            EnterInteraction();
        }
    }

    void HandleLookAround()
    {
        LookAroundInput.Update(ref lookingAround);
    }

    void HandleBottleClick()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null ||
            lookingAround ||
            !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        if (puzzleManager != null &&
            (puzzleManager.Solved || puzzleManager.CheckingAnswer))
        {
            return;
        }

        Camera activeCamera = Camera.main;

        if (activeCamera == null)
            return;

        Vector2 mousePosition = mouse.position.ReadValue();

        Ray ray = activeCamera.ScreenPointToRay(mousePosition);

        // Bottle colliders are triggers, so allow trigger hits.
        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                ~0,
                QueryTriggerInteraction.Collide))
        {
            return;
        }

        AlchemyBottle bottle =
            hit.transform.GetComponentInParent<AlchemyBottle>();

        if (bottle != null)
            bottle.SelectBottle();
    }

    public void EnterInteraction()
    {
        if (IsInteracting || (puzzleManager != null && puzzleManager.Solved))
            return;

        IsInteracting = true;
        lookingAtStation = false;
        lookingAround = false;

        // Show the recipe board/clue
        if (clueRoot != null)
            clueRoot.SetActive(true);

        if (playerMovement == null)
            playerMovement = FindAnyObjectByType<PlayerMovement>();

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

        // Hide the recipe board/clue again
        if (clueRoot != null)
            clueRoot.SetActive(false);

        if (puzzleManager != null)
            puzzleManager.EndInteraction();

        if (cameraFocus != null)
            cameraFocus.Unfocus();

        if (playerMovement != null)
            playerMovement.MovementLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ShowSuccess()
    {
        successMessageUntil = Time.time + 1.2f;
        failureMessageUntil = 0f;
    }

    public void ShowFailure()
    {
        failureMessageUntil = Time.time + 0.8f;
        successMessageUntil = 0f;
    }

    bool IsLookingAtStation()
    {
        if (cam == null)
            return false;

        Ray ray = new Ray(
            cam.transform.position,
            cam.transform.forward
        );

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

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16
        };

        style.normal.textColor = Color.white;

        if (Time.time < successMessageUntil)
        {
            GUIStyle successStyle = new GUIStyle(style)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold
            };

            successStyle.normal.textColor = new Color(1f, 0.82f, 0.3f);

            HoverPromptGUI.Draw(
                new Rect(
                    0,
                    Screen.height / 2f + 60,
                    Screen.width,
                    35
                ),
                "Mixture Complete!",
                successStyle
            );

            return;
        }

        if (Time.time < failureMessageUntil)
        {
            GUIStyle failureStyle = new GUIStyle(style)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            failureStyle.normal.textColor = new Color(1f, 0.45f, 0.4f);

            HoverPromptGUI.Draw(
                new Rect(
                    0,
                    Screen.height / 2f + 60,
                    Screen.width,
                    35
                ),
                "The mixture fails. Try again.",
                failureStyle
            );
        }

        if (IsInteracting)
        {
            GUI.Label(
                new Rect(
                    0,
                    Screen.height - 60,
                    Screen.width,
                    30
                ),
                "Left click bottles     " + LookAroundInput.PromptText + "     E to exit",
                style
            );
        }
        else if (lookingAtStation &&
                 (puzzleManager == null || !puzzleManager.Solved))
        {
            HoverPromptGUI.Draw(
                new Rect(
                    0,
                    Screen.height / 2f + 30,
                    Screen.width,
                    30
                ),
                "Press E to inspect alchemist table",
                style
            );
        }
    }
}