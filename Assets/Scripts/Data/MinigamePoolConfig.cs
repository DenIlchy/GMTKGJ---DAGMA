using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/MinigamePoolConfig")]
public class MinigamePoolConfig : ScriptableObject
{
    [Tooltip("Weight assigned to a minigame right after it has been played.")]
    [SerializeField] private float selectedWeightDrop = 0.2f;
    [Tooltip("Weight recovered by unselected minigames per Red Light round.")]
    [SerializeField] private float weightRecoveryPerRound = 0.25f;
    [Tooltip("Weight every minigame starts each session with. Runtime weights are reset to this on Start, so stale values serialized into a scene are ignored.")]
    [SerializeField] private float initialWeight = 1f;
    [Tooltip("Maximum weight a minigame can recover to.")]
    [SerializeField] private float maxWeight = 1f;
    [Tooltip("Public delay before starting the minigame camera arc sweep transition (editable in Inspector for balance).")]
    [SerializeField] private float preTransitionDelay = 0f;
    [Tooltip("Grace period duration in seconds when Green Light starts before the minigame is auto-closed.")]
    [SerializeField] private float gracePeriodDuration = 2f;

    public float SelectedWeightDrop => selectedWeightDrop;
    public float WeightRecoveryPerRound => weightRecoveryPerRound;
    public float InitialWeight => initialWeight;
    public float MaxWeight => maxWeight;
    public float PreTransitionDelay => preTransitionDelay;
    public float GracePeriodDuration => gracePeriodDuration;
}
