using System.Collections;
using UnityEngine;

public enum ServantsBellType
{
    Lion,
    Raven,
    Stag
}

public class ServantsBell : MonoBehaviour
{
    public ServantsBellType bellType;

    private const float RingDuration = 0.16f;
    private const float RingDrop = 0.06f;
    private const float RingScale = 1.06f;

    private Vector3 restingLocalPosition;
    private Vector3 restingLocalScale;
    private Coroutine ringFeedbackCoroutine;

    void Awake()
    {
        restingLocalPosition = transform.localPosition;
        restingLocalScale = transform.localScale;
    }

    public void Ring(ServantsPassageManager manager)
    {
        PlayRingFeedback();

        if (manager == null)
            return;

        manager.RegisterBell(this);

        Debug.Log("Bell rung: " + bellType);
    }

    void PlayRingFeedback()
    {
        if (ringFeedbackCoroutine != null)
            StopCoroutine(ringFeedbackCoroutine);

        RestoreRestingPose();
        ringFeedbackCoroutine = StartCoroutine(RingFeedback());
    }

    IEnumerator RingFeedback()
    {
        float elapsed = 0f;

        while (elapsed < RingDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / RingDuration);
            float pulse = Mathf.Sin(progress * Mathf.PI);

            transform.localPosition =
                restingLocalPosition + Vector3.down * (RingDrop * pulse);
            transform.localScale =
                restingLocalScale * Mathf.Lerp(1f, RingScale, pulse);

            yield return null;
        }

        RestoreRestingPose();
        ringFeedbackCoroutine = null;
    }

    void OnDisable()
    {
        if (ringFeedbackCoroutine != null)
        {
            StopCoroutine(ringFeedbackCoroutine);
            ringFeedbackCoroutine = null;
        }

        RestoreRestingPose();
    }

    void RestoreRestingPose()
    {
        transform.localPosition = restingLocalPosition;
        transform.localScale = restingLocalScale;
    }
}
