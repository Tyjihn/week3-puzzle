using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum ServantsSubmitResult
{
    Correct,
    Wrong,
    CannotAttempt,
    Incomplete
}

public class ServantsPassageManager : MonoBehaviour
{
    [Header("Bells")]
    public ServantsBell lionBell;
    public ServantsBell ravenBell;
    public ServantsBell stagBell;

    [Header("Events")]
    public UnityEvent onSolved;
    public UnityEvent onWrongAnswer;

    private readonly List<ServantsBellType> enteredSequence =
        new List<ServantsBellType>();

    public bool Solved { get; private set; }

    public void RegisterBell(ServantsBell bell)
    {
        if (Solved || bell == null)
            return;

        // Correct answer has 4 rings total
        if (enteredSequence.Count >= 4)
            return;

        enteredSequence.Add(bell.bellType);

        Debug.Log(
            "Servants sequence: " +
            string.Join(", ", enteredSequence)
        );
    }

    public bool CanAttempt(out string reason)
    {
        reason = "";

        if (PuzzleGameState.Instance == null)
        {
            reason = "Game state unavailable.";
            return false;
        }

        if (PuzzleGameState.Instance.Flames < 1)
        {
            reason = "Requires 1 Flame.";
            return false;
        }

        return true;
    }

    public ServantsSubmitResult Submit(out string message)
    {
        message = "";

        if (Solved)
        {
            message = "Servants' Passage already solved.";
            return ServantsSubmitResult.CannotAttempt;
        }

        if (!CanAttempt(out string reason))
        {
            message = reason;
            return ServantsSubmitResult.CannotAttempt;
        }

        bool correct =
            enteredSequence.Count == 4 &&
            enteredSequence[0] == ServantsBellType.Stag &&
            enteredSequence[1] == ServantsBellType.Raven &&
            enteredSequence[2] == ServantsBellType.Raven &&
            enteredSequence[3] == ServantsBellType.Lion;

        if (correct)
        {
            if (!PuzzleGameState.Instance.SpendFlames(1))
            {
                message = "Not enough flames.";
                return ServantsSubmitResult.CannotAttempt;
            }

            Solved = true;

            message = "Correct! Servants' Passage unlocked.";

            Debug.Log(
                "SERVANTS PASSAGE SOLVED: Stag -> Raven -> Raven -> Lion"
            );

            onSolved?.Invoke();
            RefreshHUD();

            return ServantsSubmitResult.Correct;
        }

        PuzzleGameState.Instance.LoseFlameForWrongAnswer();

        message = "Wrong sequence. Try again.";

        Debug.Log("Wrong Servants' Passage sequence.");

        onWrongAnswer?.Invoke();

        enteredSequence.Clear();

        RefreshHUD();

        return ServantsSubmitResult.Wrong;
    }

    public void ResetAttempt()
    {
        enteredSequence.Clear();
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
