using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/PenaltyConfig")]
public class PenaltyConfig : ScriptableObject
{
    [Tooltip("Duration of the target stun animation & minigame delay in seconds (editable in Inspector for balance).")]
    [SerializeField] private float stunDelay = 2f;
    [Tooltip("Simulated shoot animation duration before impact event fires.")]
    [SerializeField] private float shootAnimationDuration = 0.3f;

    public float StunDelay => stunDelay;
    public float ShootAnimationDuration => shootAnimationDuration;
}
