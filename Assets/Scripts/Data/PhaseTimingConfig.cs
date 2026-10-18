using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/PhaseTimingConfig")]
public class PhaseTimingConfig : ScriptableObject
{
    [Tooltip("Random range (min, max) for the total Green Light duration in seconds.")]
    [SerializeField] private Vector2 greenLightDurationRange = new Vector2(5f, 7f);
    [Tooltip("The last N seconds of the Green Light are the Red Light warning.")]
    [SerializeField] private float warningDuration = 2f;
    [Tooltip("Random range (min, max) for the Red Light duration in seconds.")]
    [SerializeField] private Vector2 redLightDurationRange = new Vector2(8f, 10f);
    [SerializeField] private Vector2 postRedLightDelayRange = new Vector2(0f, 2f);
    [Tooltip("How long the 'You moved!' feedback is shown before the penalty is applied.")]
    [SerializeField] private float penaltyFeedbackDuration = 1f;
    [Tooltip("Delay before the first Green Light starts (Intro state).")]
    [SerializeField] private float introDuration = 2f;
    [Tooltip("Public delay (in seconds) added before the Red Light timer starts counting down on screen. Must be greater than or equal to the camera's total transition duration (stepDuration * 2 + uiPopupDelay on CameraTransitionConfig, currently 0.75s), or the Red Light timer will start counting before the camera has settled.")]
    [SerializeField] private float redLightTimerStartDelay = 1.7f;

    public Vector2 GreenLightDurationRange => greenLightDurationRange;
    public float WarningDuration => warningDuration;
    public Vector2 RedLightDurationRange => redLightDurationRange;
    public Vector2 PostRedLightDelayRange => postRedLightDelayRange;
    public float PenaltyFeedbackDuration => penaltyFeedbackDuration;
    public float IntroDuration => introDuration;
    public float RedLightTimerStartDelay => redLightTimerStartDelay;
}
