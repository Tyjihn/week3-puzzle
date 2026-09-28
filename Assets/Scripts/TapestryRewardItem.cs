using UnityEngine;

[DisallowMultipleComponent]
public class TapestryRewardItem : MonoBehaviour
{
    [Header("Reward")]
    public TapestryReward rewardType = TapestryReward.None;

    [Tooltip("The permanent choice controller on the SecretCompartment.")]
    public TapestryRewardChoice rewardChoice;

    [Header("Selection Feedback")]
    [Min(1f)]
    public float selectedScaleMultiplier = 1.18f;

    public bool IsSelected { get; private set; }

    private Vector3 originalLocalScale;

    void Awake()
    {
        originalLocalScale = transform.localScale;

        if (rewardChoice == null)
            rewardChoice = GetComponentInParent<TapestryRewardChoice>();
    }

    public bool TrySelect()
    {
        if (rewardChoice == null)
            rewardChoice = GetComponentInParent<TapestryRewardChoice>();

        return rewardChoice != null && rewardChoice.TryChoose(this);
    }

    internal void SetSelectedVisual()
    {
        if (IsSelected)
            return;

        IsSelected = true;
        transform.localScale = originalLocalScale * selectedScaleMultiplier;
    }
}
