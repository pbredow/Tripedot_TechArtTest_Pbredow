using UnityEngine;

public class UISoundPlayer : MonoBehaviour
{
    public static UISoundPlayer Instance { get; private set; }

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clickSound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayClick()
    {
        if (audioSource == null || clickSound == null) return;
        audioSource.PlayOneShot(clickSound);
    }
}