using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class GameFlowUIController : MonoBehaviour
{
    [Header("Start Screen")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button enterButton;

    [Header("End Screen")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endSubtitleText;
    [SerializeField] private Button restartButton;
    [SerializeField, Min(0f)] private float restartInputDelay = 0.35f;

    [Header("Gameplay References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private RoyalPassageManager royalPassage;
    [SerializeField] private ServantsPassageManager servantsPassage;
    [SerializeField] private EscapeTimerController escapeTimer;

    private readonly Dictionary<Behaviour, bool> interactionStates =
        new Dictionary<Behaviour, bool>();

    private bool gameplayStarted;
    private bool gameEnded;
    private bool hasWon;
    private bool interactionsBlocked;
    private bool restarting;
    private float restartInputAvailableAt;
    private bool waitForRestartKeyRelease;

    void Awake()
    {
        ResolveReferences();
        WireEvents();

        if (startPanel != null)
            startPanel.SetActive(true);

        if (endPanel != null)
            endPanel.SetActive(false);

        SetGameplayBlocked(true);
    }

    void Start()
    {
        // PlayerMovement locks the cursor in its own Start method, so reassert
        // the menu state after all startup methods have run.
        SetGameplayBlocked(true);
    }

    void Update()
    {
        if (!gameplayStarted && !gameEnded && WasEnterPressed())
        {
            StartGame();
            return;
        }

        if (gameEnded && CanRestartFromKeyboard())
        {
            RestartGame();
            return;
        }

        // This runs after the passage managers. A successful final passage
        // therefore records victory before a final flame spend is evaluated.
        if (gameplayStarted &&
            !gameEnded &&
            PuzzleGameState.Instance != null &&
            PuzzleGameState.Instance.Flames <= 0)
        {
            ShowFailure();
        }
    }

    void LateUpdate()
    {
        if (!gameEnded)
            return;

        // A final passage can have a delayed ExitInteraction queued from the
        // successful submit. Keep the end screen in full control of the player
        // and cursor even if another component changes them later that frame.
        if (playerMovement != null)
            playerMovement.MovementLocked = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        if (gameplayStarted || gameEnded)
            return;

        gameplayStarted = true;

        if (startPanel != null)
            startPanel.SetActive(false);

        if (endPanel != null)
            endPanel.SetActive(false);

        SetGameplayBlocked(false);

        if (escapeTimer != null)
            escapeTimer.StartTimer();
    }

    public void ShowVictory()
    {
        if (gameEnded)
            return;

        hasWon = true;
        gameEnded = true;

        if (escapeTimer != null)
            escapeTimer.StopTimer();

        ShowEndScreen(
            "YOU ESCAPED",
            "The House releases you."
        );
    }

    public void ShowFailure()
    {
        if (hasWon || gameEnded)
            return;

        gameEnded = true;

        if (escapeTimer != null)
            escapeTimer.StopTimer();

        ShowEndScreen(
            "THE FLAME IS GONE",
            "The House claims another."
        );
    }

    public void ShowTimeoutFailure()
    {
        if (hasWon || gameEnded)
            return;

        gameEnded = true;

        if (escapeTimer != null)
            escapeTimer.StopTimerForTimeout();

        ShowEndScreen(
            "YOU RAN OUT OF TIME",
            "The House closes around you."
        );
    }

    public void RestartGame()
    {
        if (restarting)
            return;

        restarting = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ShowEndScreen(string title, string subtitle)
    {
        CancelPendingPassageExits();

        if (endTitleText != null)
            endTitleText.text = title;

        if (endSubtitleText != null)
            endSubtitleText.text = subtitle;

        if (startPanel != null)
            startPanel.SetActive(false);

        if (endPanel != null)
            endPanel.SetActive(true);

        restartInputAvailableAt = Time.unscaledTime + restartInputDelay;
        waitForRestartKeyRelease = IsEnterHeld();

        SetGameplayBlocked(true);
    }

    void CancelPendingPassageExits()
    {
        RoyalPassageInteraction royalInteraction =
            FindIncludingInactive<RoyalPassageInteraction>();

        if (royalInteraction != null)
            royalInteraction.CancelInvoke(
                nameof(RoyalPassageInteraction.ExitInteraction)
            );

        ServantsPassageInteraction servantsInteraction =
            FindIncludingInactive<ServantsPassageInteraction>();

        if (servantsInteraction != null)
            servantsInteraction.CancelInvoke(
                nameof(ServantsPassageInteraction.ExitInteraction)
            );
    }

    bool CanRestartFromKeyboard()
    {
        if (Time.unscaledTime < restartInputAvailableAt)
            return false;

        if (waitForRestartKeyRelease)
        {
            if (IsEnterHeld())
                return false;

            waitForRestartKeyRelease = false;
            return false;
        }

        return WasEnterPressed();
    }

    void SetGameplayBlocked(bool blocked)
    {
        if (playerMovement != null)
            playerMovement.MovementLocked = blocked;

        if (blocked)
            BlockInteractions();
        else
            RestoreInteractions();

        Cursor.lockState = blocked
            ? CursorLockMode.None
            : CursorLockMode.Locked;
        Cursor.visible = blocked;
    }

    void BlockInteractions()
    {
        if (interactionsBlocked)
            return;

        interactionStates.Clear();

        RememberAndDisable<PadlockInteraction>();
        RememberAndDisable<TapestryInteraction>();
        RememberAndDisable<AlchemyInteraction>();
        RememberAndDisable<PotionChoiceManager>();
        RememberAndDisable<RoyalPassageInteraction>();
        RememberAndDisable<ServantsPassageInteraction>();

        interactionsBlocked = true;
    }

    void RestoreInteractions()
    {
        if (!interactionsBlocked)
            return;

        foreach (KeyValuePair<Behaviour, bool> entry in interactionStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }

        interactionStates.Clear();
        interactionsBlocked = false;
    }

    void RememberAndDisable<T>() where T : Behaviour
    {
        T[] behaviours = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (T behaviour in behaviours)
        {
            if (behaviour == null || interactionStates.ContainsKey(behaviour))
                continue;

            interactionStates.Add(behaviour, behaviour.enabled);
            behaviour.enabled = false;
        }
    }

    void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = FindIncludingInactive<PlayerMovement>();

        if (royalPassage == null)
            royalPassage = FindIncludingInactive<RoyalPassageManager>();

        if (servantsPassage == null)
            servantsPassage = FindIncludingInactive<ServantsPassageManager>();

        if (escapeTimer == null)
            escapeTimer = FindIncludingInactive<EscapeTimerController>();
    }

    static T FindIncludingInactive<T>() where T : Object
    {
        T[] matches = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        return matches.Length > 0 ? matches[0] : null;
    }

    void WireEvents()
    {
        if (enterButton != null)
            enterButton.onClick.AddListener(StartGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (royalPassage != null && royalPassage.onSolved != null)
            royalPassage.onSolved.AddListener(ShowVictory);

        if (servantsPassage != null && servantsPassage.onSolved != null)
            servantsPassage.onSolved.AddListener(ShowVictory);
    }

    void OnDestroy()
    {
        if (enterButton != null)
            enterButton.onClick.RemoveListener(StartGame);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        if (royalPassage != null && royalPassage.onSolved != null)
            royalPassage.onSolved.RemoveListener(ShowVictory);

        if (servantsPassage != null && servantsPassage.onSolved != null)
            servantsPassage.onSolved.RemoveListener(ShowVictory);
    }

    static bool WasEnterPressed()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.enterKey.wasPressedThisFrame ||
             keyboard.numpadEnterKey.wasPressedThisFrame);
    }

    static bool IsEnterHeld()
    {
        Keyboard keyboard = Keyboard.current;

        return keyboard != null &&
            (keyboard.enterKey.isPressed ||
             keyboard.numpadEnterKey.isPressed);
    }
}
