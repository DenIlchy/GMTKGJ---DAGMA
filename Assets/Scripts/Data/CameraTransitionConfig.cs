using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/CameraTransitionConfig")]
public class CameraTransitionConfig : ScriptableObject
{
    [Tooltip("Duration of each transition leg (Gameplay -> Arc Apex -> Minigame).")]
    [SerializeField] private float stepDuration = 0.35f;
    [Tooltip("Small extra pause after the camera comes to a complete rest before opening the UI popup.")]
    [SerializeField] private float uiPopupDelay = 0.05f;
    [SerializeField] private int gameplayActivePriority = 20;
    [SerializeField] private int gameplayIdlePriority = 5;
    [SerializeField] private int apexActivePriority = 15;
    [SerializeField] private int apexIdlePriority = 5;
    [SerializeField] private int minigameActivePriority = 20;
    [SerializeField] private int minigameLockedPriority = 10;
    [SerializeField] private int minigameIdlePriority = 5;

    public float StepDuration => stepDuration;
    public float UIPopupDelay => uiPopupDelay;
    public int GameplayActivePriority => gameplayActivePriority;
    public int GameplayIdlePriority => gameplayIdlePriority;
    public int ApexActivePriority => apexActivePriority;
    public int ApexIdlePriority => apexIdlePriority;
    public int MinigameActivePriority => minigameActivePriority;
    public int MinigameLockedPriority => minigameLockedPriority;
    public int MinigameIdlePriority => minigameIdlePriority;
}
