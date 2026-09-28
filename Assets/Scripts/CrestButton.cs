using UnityEngine;

[DisallowMultipleComponent]
public class CrestButton : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Human-readable name used in logs, such as Lion or Stag.")]
    public string crestName;

    [Tooltip("Lion=0, Serpent=1, Raven=2, Stag=3")]
    public int crestIndex;

    [Header("Puzzle")]
    [Tooltip("The CrestPuzzleManager on TapestryStation.")]
    public CrestPuzzleManager puzzleManager;

    [Header("Visual Feedback")]
    [Min(1f)]
    public float selectedScaleMultiplier = 1.08f;

    [Range(0.5f, 1f)]
    public float wrongScaleMultiplier = 0.94f;

    public bool IsSelected { get; private set; }

    private Vector3 originalLocalScale;
    private bool originalStateCaptured;

    void Awake()
    {
        CaptureOriginalState();
    }

    void Start()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<CrestPuzzleManager>();

        if (string.IsNullOrWhiteSpace(crestName))
            crestName = name.Replace("Shield_", string.Empty);
    }

    public void Press()
    {
        if (puzzleManager != null)
            puzzleManager.RegisterPress(this);
    }

    internal void SetSelectedVisual()
    {
        CaptureOriginalState();
        IsSelected = true;
        transform.localScale = originalLocalScale * selectedScaleMultiplier;
    }

    internal void SetWrongVisual()
    {
        CaptureOriginalState();
        transform.localScale = originalLocalScale * wrongScaleMultiplier;
    }

    internal void ResetVisual()
    {
        CaptureOriginalState();
        IsSelected = false;
        transform.localScale = originalLocalScale;
    }

    void CaptureOriginalState()
    {
        if (originalStateCaptured)
            return;

        originalLocalScale = transform.localScale;
        originalStateCaptured = true;
    }

}
