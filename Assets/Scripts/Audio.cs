using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    public AudioSource clickSource;

    public void PlayClick()
    {
        if (clickSource != null)
            clickSource.Play();
    }
}