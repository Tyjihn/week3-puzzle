using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Swings the chest lid open when Open() is called (e.g. by PadlockInteraction once the code is right)
public class TreasureChest : MonoBehaviour
{
    [Tooltip("The lid to rotate (found automatically if a child's name contains 'lid')")]
    public Transform lid;
    [Tooltip("How far the lid swings open, in degrees around its hinge. Flip the sign if it opens the wrong way.")]
    public float openAngle = 100f;
    public float openDuration = 1.2f;

    [Tooltip("Runs once the lid is fully open, e.g. to show a win screen or unlock a door")]
    public UnityEvent onOpened;

    public bool IsOpen { get; private set; }

    void Start()
    {
        if (lid == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child != transform && child.name.ToLower().Contains("lid"))
                {
                    lid = child;
                    break;
                }
            }
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        StartCoroutine(OpenLid());
    }

    IEnumerator OpenLid()
    {
        if (lid != null)
        {
            Quaternion closed = lid.localRotation;
            Quaternion open = closed * Quaternion.Euler(openAngle, 0f, 0f);

            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                // Ease in and out so the lid starts and stops gently
                lid.localRotation = Quaternion.Slerp(closed, open, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            lid.localRotation = open;
        }

        onOpened.Invoke();
    }
}
