using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceUIController : MonoBehaviour
{
    private enum ChoiceMode
    {
        None,
        TapestryReward,
        PotionUse
    }

    [Header("Popup")]
    public GameObject choicePanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    public Button leftButton;
    public Button rightButton;

    public TMP_Text leftButtonText;
    public TMP_Text rightButtonText;

    [Header("HUD Roots")]
    public GameObject flamesGroup;
    public GameObject inventoryHUD;

    [Header("Inventory HUD")]
    public GameObject keyIndicator;
    public GameObject charmIndicator;
    public GameObject hintIndicator;

    [Header("Gameplay References")]
    public TapestryInteraction tapestryInteraction;
    public GameObject alchemyStation;
    public GameObject gateStation;

    private ChoiceMode currentMode = ChoiceMode.None;

    void Start()
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (flamesGroup != null)
            flamesGroup.SetActive(true);

        if (inventoryHUD != null)
            inventoryHUD.SetActive(true);

        if (keyIndicator != null)
            keyIndicator.SetActive(false);

        if (charmIndicator != null)
            charmIndicator.SetActive(false);

        if (hintIndicator != null)
            hintIndicator.SetActive(false);

        if (gateStation != null)
            gateStation.SetActive(false);

        if (leftButton != null)
            leftButton.onClick.AddListener(ChooseLeft);

        if (rightButton != null)
            rightButton.onClick.AddListener(ChooseRight);

        RefreshHUD();
    }

    // -------------------------
    // PUZZLE 1
    // -------------------------

    public void ShowTapestryChoice()
    {
        currentMode = ChoiceMode.TapestryReward;

        titleText.text = "Choose Your Reward";
        descriptionText.text = "Choose one reward.";

        leftButtonText.text = "Iron Key";
        rightButtonText.text = "Guardian Charm";

        OpenPanel();
    }

    // -------------------------
    // PUZZLE 2
    // -------------------------

    public void ShowPotionChoice()
    {
        currentMode = ChoiceMode.PotionUse;

        titleText.text = "Use the Potion";
        descriptionText.text = "Choose one use for the potion.";

        // No explanation of the effects here.
        // Player 2 / the manual supplies that information.
        leftButtonText.text = "Feed the Brazier";
        rightButtonText.text = "Reveal a Gate Hint";

        OpenPanel();
    }

    void ChooseLeft()
    {
        if (PuzzleGameState.Instance == null)
            return;

        if (currentMode == ChoiceMode.TapestryReward)
        {
            PuzzleGameState.Instance.ChooseIronKey();

            ClosePanel();
            RefreshHUD();
            FinishTapestryChoice();
        }
        else if (currentMode == ChoiceMode.PotionUse)
        {
            bool chosen = PuzzleGameState.Instance.ChoosePotionUse(
                PotionChoice.ExtraFlame
            );

            if (!chosen)
                return;

            ClosePanel();
            RefreshHUD();
            FinishPotionChoice();
        }
    }

    void ChooseRight()
    {
        if (PuzzleGameState.Instance == null)
            return;

        if (currentMode == ChoiceMode.TapestryReward)
        {
            PuzzleGameState.Instance.ChooseGuardianCharm();

            ClosePanel();
            RefreshHUD();
            FinishTapestryChoice();
        }
        else if (currentMode == ChoiceMode.PotionUse)
        {
            bool chosen = PuzzleGameState.Instance.ChoosePotionUse(
                PotionChoice.GateHint
            );

            if (!chosen)
                return;

            ClosePanel();
            RefreshHUD();
            FinishPotionChoice();
        }
    }

    void FinishTapestryChoice()
    {
        if (tapestryInteraction != null)
            tapestryInteraction.ExitInteractionMode();

        // Puzzle 1 is fully complete once the reward is chosen.
        if (PuzzleGameState.Instance != null)
            PuzzleGameState.Instance.CompletePuzzle1();
    }

    void FinishPotionChoice()
    {
        // Puzzle 2 is fully complete once the potion use is chosen.
        if (PuzzleGameState.Instance != null)
            PuzzleGameState.Instance.CompletePuzzle2();
    }
    
    void OpenPanel()
    {
        if (choicePanel != null)
            choicePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerMovement player =
            FindAnyObjectByType<PlayerMovement>();

        if (player != null)
            player.MovementLocked = true;
    }

    void ClosePanel()
    {
        currentMode = ChoiceMode.None;

        if (choicePanel != null)
            choicePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerMovement player =
            FindAnyObjectByType<PlayerMovement>();

        if (player != null)
            player.MovementLocked = false;
    }

    public void RefreshHUD()
    {
        if (PuzzleGameState.Instance == null)
            return;

        if (flamesGroup != null)
            flamesGroup.SetActive(true);

        if (inventoryHUD != null)
            inventoryHUD.SetActive(true);

        if (keyIndicator != null)
            keyIndicator.SetActive(
                PuzzleGameState.Instance.HasIronKey
            );

        if (charmIndicator != null)
            charmIndicator.SetActive(
                PuzzleGameState.Instance.HasGuardianCharm
            );

        if (hintIndicator != null)
            hintIndicator.SetActive(
                PuzzleGameState.Instance.HasGateHint
            );

    }
}
