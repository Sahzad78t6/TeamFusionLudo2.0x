using UnityEngine;

public class SurvivalAudioPlayer : MonoBehaviour
{
    public static SurvivalAudioPlayer instance;
    public AudioSource audioSource;
    public AudioClip uiClickClip;
    public AudioClip itemPickupClip;
    public AudioClip footstepClip;

    private void Awake()
    {
        if (instance == null) instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayUIClick()
    {
        if (uiClickClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(uiClickClip, 0.6f);
        }
    }

    public void PlayItemPickup()
    {
        if (itemPickupClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(itemPickupClip, 0.8f);
        }
    }
}
