using UnityEngine;

public enum AlchemyColor
{
    Gold,
    Black,
    Green,
    Red
}

public class AlchemyBottle : MonoBehaviour
{
    public AlchemyColor bottleColor;
    public AlchemyPuzzleManager puzzleManager;

    private Vector3 originalScale;
    private bool selected;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void SelectBottle()
    {
        if (selected || puzzleManager == null)
            return;

        if (puzzleManager.RegisterBottle(this))
        {
            selected = true;
            transform.localScale = originalScale * 1.12f;
        }
    }

    public void ResetBottle()
    {
        selected = false;
        transform.localScale = originalScale;
    }
}