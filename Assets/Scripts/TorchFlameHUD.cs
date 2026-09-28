using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TorchFlameHUD : MonoBehaviour
{
    private const int MaxTorches = 4;

    [SerializeField] private Texture2D torchTexture;
    [SerializeField] private Vector2 iconSize = new Vector2(20f, 50f);
    [SerializeField] private float spacing = 8f;

    private readonly Image[] torchImages = new Image[MaxTorches];
    private Sprite torchSprite;
    private int displayedFlames = -1;

    void Awake()
    {
        BuildIcons();
    }

    void Update()
    {
        if (PuzzleGameState.Instance == null)
            return;

        int flames = PuzzleGameState.Instance.Flames;

        if (flames != displayedFlames)
            SetFlameCount(flames);
    }

    public void SetFlameCount(int flameCount)
    {
        BuildIcons();

        int visibleTorches = Mathf.Clamp(flameCount, 0, MaxTorches);

        for (int i = 0; i < torchImages.Length; i++)
        {
            if (torchImages[i] != null)
                torchImages[i].gameObject.SetActive(i < visibleTorches);
        }

        displayedFlames = flameCount;
    }

    void BuildIcons()
    {
        if (torchImages[0] != null || torchTexture == null)
            return;

        Rect crop = new Rect(
            torchTexture.width * 0.335f,
            torchTexture.height * 0.008f,
            torchTexture.width * 0.335f,
            torchTexture.height * 0.984f
        );

        torchSprite = Sprite.Create(
            torchTexture,
            crop,
            new Vector2(0.5f, 0.5f),
            100f
        );

        torchSprite.name = "TorchFlameHUD_RuntimeSprite";

        for (int i = 0; i < MaxTorches; i++)
        {
            GameObject iconObject = new GameObject(
                "TorchIcon_" + (i + 1),
                typeof(RectTransform),
                typeof(Image)
            );

            iconObject.transform.SetParent(transform, false);

            RectTransform iconTransform =
                iconObject.GetComponent<RectTransform>();

            iconTransform.anchorMin = new Vector2(0f, 1f);
            iconTransform.anchorMax = new Vector2(0f, 1f);
            iconTransform.pivot = new Vector2(0f, 1f);
            iconTransform.sizeDelta = iconSize;
            iconTransform.anchoredPosition = new Vector2(
                i * (iconSize.x + spacing),
                0f
            );

            Image image = iconObject.GetComponent<Image>();
            image.sprite = torchSprite;
            image.raycastTarget = false;
            image.preserveAspect = false;

            torchImages[i] = image;
            iconObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (torchSprite != null)
            Destroy(torchSprite);
    }
}
