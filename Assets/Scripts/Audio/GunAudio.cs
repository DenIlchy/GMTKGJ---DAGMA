using UnityEngine;

public class GunAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bangClip;

    public void Bang()
    {
        audioSource.PlayOneShot(bangClip);
    }
}