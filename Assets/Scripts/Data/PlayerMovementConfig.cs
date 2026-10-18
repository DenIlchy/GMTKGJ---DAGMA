using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "DAGMA/PlayerMovementConfig")]
public class PlayerMovementConfig : ScriptableObject
{
    [SerializeField] private Key leftKey = Key.Q;
    [SerializeField] private Key rightKey = Key.E;
    [Tooltip("Speed added per successful alternating tap.")]
    [SerializeField] private float accelerationPerTap = 0.5f;
    [Tooltip("Maximum movement speed.")]
    [SerializeField] private float maxSpeed = 8f;
    [Tooltip("How fast speed decays NATURALLY while the player is actively tapping.")]
    [SerializeField] private float deceleration = 3f;
    [Tooltip("Reduce speed by this amount when the same key is pressed twice.")]
    [SerializeField] private float wrongTapPenalty = 0.5f;
    [Tooltip("How fast speed decays when the player intentionally stops tapping.")]
    [SerializeField] private float brakingDeceleration = 6f;
    [Tooltip("Time (in seconds) without input before aggressive braking kicks in.")]
    [SerializeField] private float brakingDelay = 0.2f;
    [SerializeField] private float footstepPitchVariation = 0.05f;
    [SerializeField] private float minSpeedForFootstep = 0.01f;
    [Tooltip("Float parameter name in Animator Blend Tree (0.0 = idle/0%, 1.0 = max speed/100%).")]
    [SerializeField] private string speedParamName = "Speed";

    public Key LeftKey => leftKey;
    public Key RightKey => rightKey;
    public float AccelerationPerTap => accelerationPerTap;
    public float MaxSpeed => maxSpeed;
    public float Deceleration => deceleration;
    public float WrongTapPenalty => wrongTapPenalty;
    public float BrakingDeceleration => brakingDeceleration;
    public float BrakingDelay => brakingDelay;
    public float FootstepPitchVariation => footstepPitchVariation;
    public float MinSpeedForFootstep => minSpeedForFootstep;
    public string SpeedParamName => speedParamName;
}
