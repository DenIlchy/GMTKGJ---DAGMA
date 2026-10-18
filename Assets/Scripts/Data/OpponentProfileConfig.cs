using UnityEngine;

/// <summary>
/// Tuning profile for one AI opponent. Intended usage: create one asset per AI personality/difficulty tier.
/// </summary>
[CreateAssetMenu(menuName = "DAGMA/OpponentProfileConfig")]
public class OpponentProfileConfig : ScriptableObject
{
    [SerializeField] private float accelerationPerStep = 0.5f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float deceleration = 0.2f;
    [SerializeField] private Vector2 stepIntervalRange = new Vector2(0.2f, 0.5f);
    [SerializeField] [Range(0f, 1f)] private float riskyWarningStepChance = 0.265f;
    [SerializeField] private Vector2 warningReactionDelayRange = new Vector2(0.5f, 1f);

    public float AccelerationPerStep => accelerationPerStep;
    public float MaxSpeed => maxSpeed;
    public float Deceleration => deceleration;
    public Vector2 StepIntervalRange => stepIntervalRange;
    public float RiskyWarningStepChance => riskyWarningStepChance;
    public Vector2 WarningReactionDelayRange => warningReactionDelayRange;
}
