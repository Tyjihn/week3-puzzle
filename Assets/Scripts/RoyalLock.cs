using UnityEngine;

public enum RoyalLockColor
{
    None,
    Gold,
    Black,
    Green,
    Red
}

public class RoyalLock : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Renderer for the small color-selection area, not the whole lock.")]
    public Renderer colorRenderer;

    public RoyalLockColor SelectedColor { get; private set; } = RoyalLockColor.None;

    private Material runtimeMaterial;

    void Awake()
    {
        if (colorRenderer != null)
            runtimeMaterial = colorRenderer.material;

        ApplyColor();
    }

    public void CycleColor()
    {
        SelectedColor = SelectedColor switch
        {
            RoyalLockColor.None => RoyalLockColor.Gold,
            RoyalLockColor.Gold => RoyalLockColor.Black,
            RoyalLockColor.Black => RoyalLockColor.Green,
            RoyalLockColor.Green => RoyalLockColor.Red,
            _ => RoyalLockColor.Gold
        };

        ApplyColor();

        Debug.Log(name + " selected: " + SelectedColor);
    }

    public void ResetLock()
    {
        SelectedColor = RoyalLockColor.None;
        ApplyColor();
    }

    void ApplyColor()
    {
        if (runtimeMaterial == null)
            return;

        Color color = SelectedColor switch
        {
            RoyalLockColor.Gold => new Color(0.95f, 0.68f, 0.12f),
            RoyalLockColor.Black => new Color(0.03f, 0.03f, 0.03f),
            RoyalLockColor.Green => new Color(0.12f, 0.55f, 0.20f),
            RoyalLockColor.Red => new Color(0.70f, 0.08f, 0.08f),
            _ => new Color(0.20f, 0.20f, 0.20f)
        };

        runtimeMaterial.color = color;

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", color);
    }
}