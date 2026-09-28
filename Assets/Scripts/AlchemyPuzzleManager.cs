using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AlchemyPuzzleManager : MonoBehaviour
{
    [Header("Bottles")]
    public AlchemyBottle goldBottle;
    public AlchemyBottle blackBottle;
    public AlchemyBottle greenBottle;
    public AlchemyBottle redBottle;

    [Header("Events")]
    public UnityEvent onSolved;
    public UnityEvent onWrongAnswer;

    private readonly List<AlchemyBottle> selectedBottles = new List<AlchemyBottle>();

    private AlchemyInteraction interaction;

    public bool Solved { get; private set; }
    public bool CheckingAnswer { get; private set; }

    void Awake()
    {
        interaction = GetComponent<AlchemyInteraction>();
    }

    public bool RegisterBottle(AlchemyBottle bottle)
    {
        if (Solved || CheckingAnswer || bottle == null)
            return false;

        if (selectedBottles.Contains(bottle))
            return false;

        if (selectedBottles.Count >= 3)
            return false;

        selectedBottles.Add(bottle);

        Debug.Log("Alchemy selected: " + bottle.bottleColor);

        if (selectedBottles.Count == 3)
            StartCoroutine(CheckSequence());

        return true;
    }

    IEnumerator CheckSequence()
    {
        CheckingAnswer = true;

        // Brief pause so the player can see their third selection.
        yield return new WaitForSeconds(0.35f);

        bool correct =
            selectedBottles[0].bottleColor == AlchemyColor.Black &&
            selectedBottles[1].bottleColor == AlchemyColor.Red &&
            selectedBottles[2].bottleColor == AlchemyColor.Gold;

        if (correct)
        {
            Solved = true;

            Debug.Log("ALCHEMY PUZZLE SOLVED: Black -> Red -> Gold");

            if (interaction != null)
                interaction.ShowSuccess();

            // Let the success cue show briefly.
            yield return new WaitForSeconds(0.6f);

            // Exit the physical puzzle BEFORE opening the reward UI.
            if (interaction != null)
                interaction.ExitInteraction();

            // Now open the reward-choice UI.
            onSolved?.Invoke();

            yield break;
        }

        Debug.Log("Wrong alchemy sequence");

        if (PuzzleGameState.Instance != null)
            PuzzleGameState.Instance.LoseFlameForWrongAnswer();

        RefreshHUD();
        onWrongAnswer?.Invoke();

        if (interaction != null)
            interaction.ShowFailure();

        yield return new WaitForSeconds(0.7f);

        ResetAttempt();

        CheckingAnswer = false;
    }

    public void ResetAttempt()
    {
        foreach (AlchemyBottle bottle in selectedBottles)
        {
            if (bottle != null)
                bottle.ResetBottle();
        }

        selectedBottles.Clear();
    }

    public void EndInteraction()
    {
        if (!Solved)
        {
            ResetAttempt();
            CheckingAnswer = false;
        }
    }

    void RefreshHUD()
    {
        ChoiceUIController ui =
            FindAnyObjectByType<ChoiceUIController>();

        if (ui != null)
            ui.RefreshHUD();

        TorchFlameHUD flameHUD =
            FindAnyObjectByType<TorchFlameHUD>();

        if (flameHUD != null && PuzzleGameState.Instance != null)
            flameHUD.SetFlameCount(PuzzleGameState.Instance.Flames);
    }
}
