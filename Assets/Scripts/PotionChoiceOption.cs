using UnityEngine;

public class PotionChoiceOption : MonoBehaviour
{
    public PotionChoice choice;

    public void Choose()
    {
        if (PuzzleGameState.Instance == null)
            return;

        bool chosen = PuzzleGameState.Instance.ChoosePotionUse(choice);

        if (!chosen)
            return;

        Debug.Log("Potion choice selected: " + choice);

        PotionChoiceManager manager =
            GetComponentInParent<PotionChoiceManager>();

        if (manager != null)
            manager.OnChoiceMade(this);
    }
}