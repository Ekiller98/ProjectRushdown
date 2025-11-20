using UnityEngine;

public class GlobalUISounds : MonoBehaviour
{
    public static GlobalUISounds Instance;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    private AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayHover()
    {
        audioSource.PlayOneShot(hoverSound, 3f);
    }

    public void PlayClick()
    {
        audioSource.PlayOneShot(clickSound, 4f);
    }
}
