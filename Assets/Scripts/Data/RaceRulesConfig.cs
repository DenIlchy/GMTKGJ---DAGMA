using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/RaceRulesConfig")]
public class RaceRulesConfig : ScriptableObject
{
    [Tooltip("Absolute speed above which a mover is considered violating during Red Light.")]
    [SerializeField] private float speedThreshold = 1.5f;
    [Tooltip("Distance a violator is pushed back.")]
    [SerializeField] private float pushBackDistance = 4f;

    public float SpeedThreshold => speedThreshold;
    public float PushBackDistance => pushBackDistance;
}
