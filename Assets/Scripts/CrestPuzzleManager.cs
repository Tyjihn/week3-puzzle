using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CrestPuzzleManager : MonoBehaviour
{
    [Header("Correct Answer")]
    [Tooltip("Stag, Raven, Serpent, Lion: 3, 2, 1, 0")]
    public List<int> correctOrder = new List<int>() { 3, 2, 1, 0 };

    [Header("Feedback")]
    [Min(0f)]
    [Tooltip("How long a wrong four-crest sequence remains visible before resetting.")]
    public float wrongAnswerDisplayTime = 0.45f;

    [Header("Events")]
    public UnityEvent onSolved = new UnityEvent();
    public UnityEvent onWrongAnswer = new UnityEvent();

    public bool IsInteractionActive { get; private set; }
    public bool IsSolved { get; private set; }
    public bool CanAcceptInput => IsInteractionActive && !IsSolved && resetCoroutine == null;

    private readonly List<int> enteredOrder = new List<int>();
    private readonly HashSet<int> selectedIndices = new HashSet<int>();
    private readonly List<CrestButton> selectedButtons = new List<CrestButton>();

    private CrestButton[] crestButtons;
    private Coroutine resetCoroutine;

    void Awake()
    {
        RefreshButtons();
    }

    public void BeginInteraction()
    {
        IsInteractionActive = true;

        if (!IsSolved)
            ResetAttempt();
    }

    public void EndInteraction()
    {
        IsInteractionActive = false;

        // A solved sequence stays visually selected. Only unfinished attempts reset.
        if (!IsSolved)
            ResetAttempt();
    }

    public void RegisterPress(CrestButton crest)
    {
        if (!CanAcceptInput || crest == null)
            return;

        if (selectedIndices.Contains(crest.crestIndex))
            return;

        if (enteredOrder.Count >= correctOrder.Count)
            return;

        selectedIndices.Add(crest.crestIndex);
        enteredOrder.Add(crest.crestIndex);
        selectedButtons.Add(crest);
        crest.SetSelectedVisual();

        string displayName = string.IsNullOrWhiteSpace(crest.crestName)
            ? crest.name
            : crest.crestName;

        Debug.Log(
            $"Tapestry crest selected: {displayName} ({crest.crestIndex}). " +
            $"Sequence: {string.Join(", ", enteredOrder)}",
            crest
        );

        if (enteredOrder.Count == correctOrder.Count)
            EvaluateSequence();
    }

    // Retained for compatibility with any existing Inspector event that passes an index.
    public void RegisterPress(int crestIndex)
    {
        RefreshButtonsIfNeeded();

        foreach (CrestButton crest in crestButtons)
        {
            if (crest.crestIndex == crestIndex)
            {
                RegisterPress(crest);
                return;
            }
        }
    }

    void EvaluateSequence()
    {
        bool correct = enteredOrder.Count == correctOrder.Count;

        if (correct)
        {
            for (int i = 0; i < correctOrder.Count; i++)
            {
                if (enteredOrder[i] != correctOrder[i])
                {
                    correct = false;
                    break;
                }
            }
        }

        if (correct)
        {
            IsSolved = true;
            Debug.Log("HERALDIC TAPESTRY PUZZLE SOLVED: Stag -> Raven -> Serpent -> Lion", this);
            onSolved.Invoke();
            return;
        }

        Debug.LogWarning(
            $"Wrong heraldic crest sequence: {string.Join(", ", enteredOrder)}. Resetting attempt.",
            this
        );

        if (PuzzleGameState.Instance != null)
            PuzzleGameState.Instance.LoseFlameForWrongAnswer();

        RefreshHUD();
        onWrongAnswer.Invoke();
        resetCoroutine = StartCoroutine(WrongAnswerReset());
    }

    IEnumerator WrongAnswerReset()
    {
        foreach (CrestButton crest in selectedButtons)
            crest.SetWrongVisual();

        yield return new WaitForSeconds(wrongAnswerDisplayTime);

        resetCoroutine = null;
        ResetAttempt();
    }

    public void ResetAttempt()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        RefreshButtonsIfNeeded();

        foreach (CrestButton crest in crestButtons)
            crest.ResetVisual();

        enteredOrder.Clear();
        selectedIndices.Clear();
        selectedButtons.Clear();
    }

    void RefreshButtonsIfNeeded()
    {
        if (crestButtons == null || crestButtons.Length == 0)
            RefreshButtons();
    }

    void RefreshButtons()
    {
        crestButtons = GetComponentsInChildren<CrestButton>(true);

        foreach (CrestButton crest in crestButtons)
        {
            if (crest.puzzleManager == null)
                crest.puzzleManager = this;
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
