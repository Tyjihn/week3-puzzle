using UnityEngine;

public enum PotionChoice
{
    None,
    ExtraFlame,
    GateHint
}

public class PuzzleGameState : MonoBehaviour
{
    public static PuzzleGameState Instance { get; private set; }

    [Header("Flames")]
    [SerializeField] private int flames = 3;
    public int Flames => flames;

    [Header("Puzzle Progress")]
    public bool Puzzle1Completed { get; private set; }
    public bool Puzzle2Completed { get; private set; }

    [Header("Final Area")]
    public GameObject gateStation;

    [Header("Tapestry Reward")]
    public bool HasIronKey { get; private set; }
    public bool HasGuardianCharm { get; private set; }
    public bool GuardianCharmAvailable { get; private set; }

    [Header("Potion Choice")]
    public PotionChoice ChosenPotionUse { get; private set; } = PotionChoice.None;

    public bool HasGateHint =>
        ChosenPotionUse == PotionChoice.GateHint;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gateStation != null)
            gateStation.SetActive(false);
    }

    public void CompletePuzzle1()
    {
        Puzzle1Completed = true;
        CheckFinalArea();
    }

    public void CompletePuzzle2()
    {
        Puzzle2Completed = true;
        CheckFinalArea();
    }

    void CheckFinalArea()
    {
        if (Puzzle1Completed && Puzzle2Completed)
        {
            if (gateStation != null)
            {
                gateStation.SetActive(true);
                Debug.Log("Both puzzles completed. Final Gate Station revealed.");
            }
        }
    }

    public void ChooseIronKey()
    {
        HasIronKey = true;
        HasGuardianCharm = false;
        GuardianCharmAvailable = false;

        Debug.Log("Game State: Iron Key acquired");
    }

    public void ChooseGuardianCharm()
    {
        HasIronKey = false;
        HasGuardianCharm = true;
        GuardianCharmAvailable = true;

        Debug.Log("Game State: Guardian Charm acquired");
    }

    public bool ChoosePotionUse(PotionChoice choice)
    {
        if (ChosenPotionUse != PotionChoice.None)
            return false;

        if (choice == PotionChoice.None)
            return false;

        ChosenPotionUse = choice;

        if (choice == PotionChoice.ExtraFlame)
        {
            flames = Mathf.Min(flames + 1, 4);
            Debug.Log("Potion used on brazier. Flames: " + flames);
        }
        else
        {
            Debug.Log("Potion reserved for gate hints");
        }

        return true;
    }

    public bool SpendFlames(int amount)
    {
        if (flames < amount)
            return false;

        flames -= amount;

        Debug.Log(
            "Spent " + amount +
            " flame(s). Remaining: " + flames
        );

        return true;
    }

    public void LoseFlameForWrongAnswer()
    {
        if (GuardianCharmAvailable)
        {
            GuardianCharmAvailable = false;

            Debug.Log(
                "Guardian Charm absorbed the flame loss"
            );

            return;
        }

        flames = Mathf.Max(0, flames - 1);

        Debug.Log(
            "Wrong answer. Flames remaining: " + flames
        );
    }
}