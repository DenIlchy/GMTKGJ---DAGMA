using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Audio Source Settings")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Keypad SFX Placeholder")]
    [SerializeField] private AudioClip keypadClickClip;
    [SerializeField] private float basePitch = 1.0f;
    [SerializeField] private bool enablePitchVariation = false;
    [SerializeField] private float pitchVariationRange = 0.05f;

    public static SoundManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Plays the keypad click sound effect with optional pitch variation.
    /// </summary>
    public void PlayKeypadClickSFX()
    {
        if (sfxAudioSource != null && keypadClickClip != null)
        {
            float pitch = basePitch;
            if (enablePitchVariation)
            {
                pitch += Random.Range(-pitchVariationRange, pitchVariationRange);
            }
            sfxAudioSource.pitch = pitch;
            sfxAudioSource.PlayOneShot(keypadClickClip);
        }
        else
        {
            // Placeholder debug log when no audio clip is assigned yet
            Debug.Log("[SoundManager] *BEEP* Keypad Click SFX Placeholder");
        }
    }
}
