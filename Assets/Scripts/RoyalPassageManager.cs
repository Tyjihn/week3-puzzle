using UnityEngine;
using UnityEngine.Events;

public enum RoyalSubmitResult
{
    Correct,
    Wrong,
    CannotAttempt,
    Incomplete
}

public class RoyalPassageManager : MonoBehaviour
{
    [Header("Locks")]
    public RoyalLock dawnLock;
    public RoyalLock duskLock;

    [Header("Door")]
    public GameObject royalDoor;

    [Header("Events")]
    public UnityEvent onSolved;
    public UnityEvent onWrongAnswer;

    public bool Solved { get; private set; }

    public bool CanAttempt(out string reason)
    {
        reason = "";

        if (PuzzleGameState.Instance == null)
        {
            reason = "Game state unavailable.";
            return false;
        }

        if (!PuzzleGameState.Instance.HasIronKey)
        {
            reason = "Requires the Iron Key.";
            return false;
        }

        if (PuzzleGameState.Instance.Flames < 2)
        {
            reason = "Requires 2 Flames.";
            return false;
        }

        return true;
    }

    public RoyalSubmitResult Submit(out string message)
    {
        message = "";

        if (Solved)
        {
            message = "Royal Passage already solved.";
            return RoyalSubmitResult.CannotAttempt;
        }

        if (!CanAttempt(out string reason))
        {
            message = reason;
            return RoyalSubmitResult.CannotAttempt;
        }

        if (dawnLock == null || duskLock == null)
        {
            message = "Royal locks are not assigned.";
            return RoyalSubmitResult.CannotAttempt;
        }

        if (dawnLock.SelectedColor == RoyalLockColor.None ||
            duskLock.SelectedColor == RoyalLockColor.None)
        {
            message = "Set a color on both locks first.";
            return RoyalSubmitResult.Incomplete;
        }

        bool correct =
            dawnLock.SelectedColor == RoyalLockColor.Black &&
            duskLock.SelectedColor == RoyalLockColor.Gold;

        if (correct)
        {
            if (!PuzzleGameState.Instance.SpendFlames(2))
            {
                message = "Not enough flames.";
                return RoyalSubmitResult.CannotAttempt;
            }

            Solved = true;

            message = "Correct! Royal Passage unlocked.";
            Debug.Log("ROYAL PASSAGE SOLVED: Dawn = Black, Dusk = Gold");

            onSolved?.Invoke();

            RefreshHUD();

            return RoyalSubmitResult.Correct;
        }

        PuzzleGameState.Instance.LoseFlameForWrongAnswer();

        message = "Wrong answer. Try again.";
        Debug.Log("Wrong Royal Passage answer.");

        onWrongAnswer?.Invoke();

        dawnLock.ResetLock();
        duskLock.ResetLock();

        RefreshHUD();

        return RoyalSubmitResult.Wrong;
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
