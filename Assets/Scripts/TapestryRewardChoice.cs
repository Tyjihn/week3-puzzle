using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum TapestryReward
{
    None,
    IronKey,
    GuardianCharm
}

[DisallowMultipleComponent]
public class TapestryRewardChoice : MonoBehaviour
{
    [Header("Reward Items")]
    public TapestryRewardItem ironKeyItem;
    public TapestryRewardItem guardianCharmItem;

    [Header("Pickup Visual")]
    public float pickupDisplayTime = 1f;

    [Header("Tapestry Interaction")]
    public TapestryInteraction tapestryInteraction;

    [Header("Next Puzzle")]
    [Tooltip("Activated permanently after either reward is chosen.")]
    public GameObject alchemistTable;

    [Header("Events")]
    public UnityEvent onRewardChosen = new UnityEvent();
    public UnityEvent onIronKeySelected = new UnityEvent();
    public UnityEvent onGuardianCharmSelected = new UnityEvent();

    [SerializeField]
    private TapestryReward chosenReward = TapestryReward.None;

    public TapestryReward ChosenReward => chosenReward;
    public bool HasChosenReward => chosenReward != TapestryReward.None;

    void Awake()
    {
        chosenReward = TapestryReward.None;

        if (tapestryInteraction == null)
            tapestryInteraction = GetComponentInParent<TapestryInteraction>();

        if (alchemistTable != null)
            alchemistTable.SetActive(false);
    }

    public bool TryChoose(TapestryRewardItem item)
    {
        if (item == null || HasChosenReward || item.rewardType == TapestryReward.None)
            return false;

        chosenReward = item.rewardType;

        TapestryRewardItem unselectedItem = item == ironKeyItem
            ? guardianCharmItem
            : ironKeyItem;

        // Other reward disappears immediately
        if (unselectedItem != null)
            unselectedItem.gameObject.SetActive(false);

        if (chosenReward == TapestryReward.IronKey)
        {
            Debug.Log("Selected Iron Key", item);

            if (PuzzleGameState.Instance != null)
                PuzzleGameState.Instance.ChooseIronKey();

            onIronKeySelected.Invoke();
        }
        else
        {
            Debug.Log("Selected Guardian Charm", item);

            if (PuzzleGameState.Instance != null)
                PuzzleGameState.Instance.ChooseGuardianCharm();

            onGuardianCharmSelected.Invoke();
        }

        StartCoroutine(PlayPickupVisual(item));

        return true;
    }

    IEnumerator PlayPickupVisual(TapestryRewardItem selectedItem)
    {
        if (selectedItem == null)
            yield break;

        // Enlarge selected reward
        selectedItem.SetSelectedVisual();

        // Let player see what they earned
        yield return new WaitForSeconds(pickupDisplayTime);

        // Remove selected reward
        selectedItem.gameObject.SetActive(false);

        // Properly close the tapestry interaction
        if (tapestryInteraction != null)
            tapestryInteraction.ExitInteractionMode();

        // Activate Puzzle 2
        if (alchemistTable != null)
        {
            alchemistTable.SetActive(true);
            Debug.Log("Alchemist Table activated", alchemistTable);
        }

        onRewardChosen.Invoke();
    }
}