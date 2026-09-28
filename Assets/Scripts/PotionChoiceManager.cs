using UnityEngine;
using UnityEngine.InputSystem;

public class PotionChoiceManager : MonoBehaviour
{
    [Header("Choice Objects")]
    public GameObject choiceRoot;
    public PotionChoiceOption feedBrazierOption;
    public PotionChoiceOption gateHintOption;

    [Header("Final Gates")]
    public GameObject royalPassage;
    public GameObject servantsPassage;

    private bool choosing;

    void Start()
    {
        if (choiceRoot != null)
            choiceRoot.SetActive(false);

        if (royalPassage != null)
            royalPassage.SetActive(false);

        if (servantsPassage != null)
            servantsPassage.SetActive(false);
    }

    void Update()
    {
        if (!choosing)
            return;

        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                Mathf.Infinity,
                ~0,
                QueryTriggerInteraction.Collide))
            return;

        PotionChoiceOption option =
            hit.transform.GetComponentInParent<PotionChoiceOption>();

        if (option != null)
            option.Choose();
    }

    public void BeginPotionChoice()
    {
        choosing = true;

        if (choiceRoot != null)
            choiceRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerMovement player =
            FindAnyObjectByType<PlayerMovement>();

        if (player != null)
            player.MovementLocked = true;
    }

    public void OnChoiceMade(PotionChoiceOption selected)
    {
        if (!choosing)
            return;

        choosing = false;

        if (feedBrazierOption != null &&
            selected != feedBrazierOption)
        {
            feedBrazierOption.gameObject.SetActive(false);
        }

        if (gateHintOption != null &&
            selected != gateHintOption)
        {
            gateHintOption.gameObject.SetActive(false);
        }

        if (choiceRoot != null)
            choiceRoot.SetActive(false);

        // Activate both final passage areas.
        if (royalPassage != null)
            royalPassage.SetActive(true);

        if (servantsPassage != null)
            servantsPassage.SetActive(true);

        PlayerMovement player =
            FindAnyObjectByType<PlayerMovement>();

        if (player != null)
            player.MovementLocked = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}