using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TapestryRewardCompartment : MonoBehaviour
{
    [Header("Rise Animation")]
    public float riseDistance = 1.2f;
    public float riseDuration = 0.6f;

    [Header("Opening Animation")]
    public float openDuration = 0.5f;
    public Vector3 openLocalEulerAngles = new Vector3(0f, 0f, 100f);

    public bool IsOpen { get; private set; }

    private Transform chestRoot;
    private Transform compartmentDoor;
    private GameObject rewardsRoot;
    private GameObject clueText;

    private Vector3 chestRaisedLocalPosition;
    private Vector3 chestHiddenLocalPosition;

    private Vector3 closedDoorLocalPosition;
    private Quaternion closedDoorLocalRotation;

    private Coroutine openCoroutine;

    void Awake()
    {
        // This script is attached to SecretCompartment,
        // so SecretCompartment itself is the chest root.
        chestRoot = transform;

        // Auto-find children
        compartmentDoor = transform.Find("CompartmentDoor");

        Transform rewards = transform.Find("Rewards");
        if (rewards != null)
            rewardsRoot = rewards.gameObject;

        // TapInscription is a sibling of SecretCompartment,
        // so look for it under the same parent.
        if (transform.parent != null)
        {
            Transform clue = transform.parent.Find("TapInscription");

            if (clue != null)
                clueText = clue.gameObject;
        }

        // Remember final chest position
        chestRaisedLocalPosition = chestRoot.localPosition;

        // Start chest underground
        chestHiddenLocalPosition =
            chestRaisedLocalPosition - Vector3.up * riseDistance;

        chestRoot.localPosition = chestHiddenLocalPosition;

        // Remember closed lid position/rotation
        if (compartmentDoor != null)
        {
            closedDoorLocalPosition = compartmentDoor.localPosition;
            closedDoorLocalRotation = compartmentDoor.localRotation;
        }
        else
        {
            Debug.LogWarning(
                "Could not find CompartmentDoor under SecretCompartment.",
                this
            );
        }

        // Rewards hidden until chest opens
        if (rewardsRoot != null)
            rewardsRoot.SetActive(false);
        else
            Debug.LogWarning(
                "Could not find Rewards under SecretCompartment.",
                this
            );
    }

    public void Open()
    {
        if (IsOpen)
            return;

        IsOpen = true;

        // Hide inscription as soon as puzzle is solved
        if (clueText != null)
            clueText.SetActive(false);

        Debug.Log("Tapestry reward chest rising and opening", this);

        openCoroutine = StartCoroutine(RiseAndOpenRoutine());
    }

    IEnumerator RiseAndOpenRoutine()
    {
        // 1. Rise out of ground
        if (riseDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;

                float amount = Mathf.Clamp01(elapsed / riseDuration);
                amount = Mathf.SmoothStep(0f, 1f, amount);

                chestRoot.localPosition = Vector3.Lerp(
                    chestHiddenLocalPosition,
                    chestRaisedLocalPosition,
                    amount
                );

                yield return null;
            }
        }

        chestRoot.localPosition = chestRaisedLocalPosition;

        yield return new WaitForSeconds(0.15f);

        // 2. Make rewards visible inside chest
        if (rewardsRoot != null)
            rewardsRoot.SetActive(true);

        // 3. Open lid normally
        if (compartmentDoor != null && openDuration > 0f)
        {
            Quaternion openRotation =
                closedDoorLocalRotation *
                Quaternion.Euler(openLocalEulerAngles);

            float elapsed = 0f;

            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;

                float amount = Mathf.Clamp01(elapsed / openDuration);
                amount = Mathf.SmoothStep(0f, 1f, amount);

                compartmentDoor.localPosition = closedDoorLocalPosition;

                compartmentDoor.localRotation = Quaternion.Slerp(
                    closedDoorLocalRotation,
                    openRotation,
                    amount
                );

                yield return null;
            }

            compartmentDoor.localRotation = openRotation;
        }

        FinishOpening();
    }

    void FinishOpening()
    {
        chestRoot.localPosition = chestRaisedLocalPosition;

        openCoroutine = null;

        Debug.Log("Tapestry reward chest opened", this);
    }
}