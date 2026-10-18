using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Config (preferred) — overrides all Legacy values below")]
    [SerializeField] private CameraTransitionConfig cameraConfig;

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera arcApexCamera;
    [SerializeField] private CinemachineCamera minigameCamera;

    [Header("Waypoint Transition Settings (Legacy)")]
    [Tooltip("Duration of each transition leg (Gameplay -> Arc Apex -> Minigame).")]
    [SerializeField] private float stepDuration = 0.4f;

    [Tooltip("Small extra pause after the camera comes to a complete rest before opening the UI popup.")]
    [SerializeField] private float uiPopupDelay = 0.05f;

    [Header("Debug Testing")]
    [Tooltip("Enable key presses (1/2) and on-screen UI buttons for testing.")]
    [SerializeField] private bool enableDebugHotkeys = true;

    private float StepDuration => cameraConfig != null ? cameraConfig.StepDuration : stepDuration;
    private float UIPopupDelay => cameraConfig != null ? cameraConfig.UIPopupDelay : uiPopupDelay;
    private int GameplayActivePriority => cameraConfig != null ? cameraConfig.GameplayActivePriority : 20;
    private int GameplayIdlePriority => cameraConfig != null ? cameraConfig.GameplayIdlePriority : 5;
    private int ApexActivePriority => cameraConfig != null ? cameraConfig.ApexActivePriority : 15;
    private int ApexIdlePriority => cameraConfig != null ? cameraConfig.ApexIdlePriority : 5;
    private int MinigameActivePriority => cameraConfig != null ? cameraConfig.MinigameActivePriority : 20;
    private int MinigameLockedPriority => cameraConfig != null ? cameraConfig.MinigameLockedPriority : 10;
    private int MinigameIdlePriority => cameraConfig != null ? cameraConfig.MinigameIdlePriority : 5;

    private Coroutine transitionCoroutine;
    private CinemachineBrain mainCameraBrain;

    public static CameraManager Instance { get; private set; }

    public float GetStepDuration() => StepDuration;
    public float GetTotalTransitionDuration() => (StepDuration * 2f) + UIPopupDelay;

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

    private void Start()
    {
        mainCameraBrain = Object.FindFirstObjectByType<CinemachineBrain>();

        // Default to 3rd-person gameplay camera view on start
        SwitchToGameplayCameraDirect();
    }

    private void Update()
    {
        if (enableDebugHotkeys && Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                SwitchToGameplayCamera();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                SwitchToMinigameCamera();
            }
        }
    }

    private void OnGUI()
    {
        if (!enableDebugHotkeys) return;

        GUILayout.BeginArea(new Rect(20, 20, 220, 100));
        if (GUILayout.Button("1: Gameplay View (3rd Person)"))
        {
            SwitchToGameplayCamera();
        }
        if (GUILayout.Button("2: Minigame View (Arc Sweep)"))
        {
            SwitchToMinigameCamera();
        }
        GUILayout.EndArea();
    }

    /// <summary>
    /// Switches back to 3rd-person gameplay camera using the reverse arc sweep (Minigame -> Arc Apex -> Gameplay).
    /// </summary>
    [ContextMenu("Test Gameplay Camera (Reverse Arc Sweep)")]
    public void SwitchToGameplayCamera()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(ReverseArcWaypointSequence());
    }

    /// <summary>
    /// Direct instant priority reset to gameplay camera without playing reverse sweep.
    /// </summary>
    public void SwitchToGameplayCameraDirect()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        SetPriorities(gameplayPriority: GameplayActivePriority, apexPriority: ApexIdlePriority, minigamePriority: MinigameIdlePriority);
    }

    /// <summary>
    /// Triggers the forward 3-camera waypoint sequence: Gameplay -> Arc Apex -> Minigame Close-Up.
    /// </summary>
    [ContextMenu("Test Minigame Camera (Forward Arc Sweep)")]
    public void SwitchToMinigameCamera()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(ForwardArcWaypointSequence());
    }

    private IEnumerator ForwardArcWaypointSequence()
    {
        // Step 1: Blend from Gameplay to Arc Apex (Over-The-Shoulder Peak)
        SetPriorities(gameplayPriority: GameplayIdlePriority, apexPriority: ApexActivePriority, minigamePriority: MinigameIdlePriority);
        yield return new WaitForSeconds(StepDuration);

        // Step 2: Blend from Arc Apex to Minigame Close-Up (Front View)
        SetPriorities(gameplayPriority: GameplayIdlePriority, apexPriority: ApexIdlePriority, minigamePriority: MinigameActivePriority);
        yield return new WaitForSeconds(StepDuration);

        // Lock priority on minigame camera cleanly
        SetPriorities(gameplayPriority: GameplayIdlePriority, apexPriority: ApexIdlePriority, minigamePriority: MinigameLockedPriority);

        // Dynamically wait until CinemachineBrain finishes blending completely
        if (mainCameraBrain != null)
        {
            yield return new WaitForEndOfFrame();
            while (mainCameraBrain.IsBlending)
            {
                yield return null;
            }
        }

        if (UIPopupDelay > 0f)
        {
            yield return new WaitForSeconds(UIPopupDelay);
        }

        transitionCoroutine = null;

        // Open the minigame UI popup ONLY after the camera has come to a 100% complete stop!
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.ShowMinigame();
        }
    }

    private IEnumerator ReverseArcWaypointSequence()
    {
        // Step 1: Blend from Minigame Close-Up back to Arc Apex (Over-The-Shoulder Peak)
        SetPriorities(gameplayPriority: GameplayIdlePriority, apexPriority: ApexActivePriority, minigamePriority: MinigameIdlePriority);
        yield return new WaitForSeconds(StepDuration);

        // Step 2: Blend from Arc Apex back to 3rd-Person Gameplay View
        SetPriorities(gameplayPriority: GameplayActivePriority, apexPriority: ApexIdlePriority, minigamePriority: MinigameIdlePriority);
        yield return new WaitForSeconds(StepDuration);

        // Dynamically wait until CinemachineBrain finishes blending completely
        if (mainCameraBrain != null)
        {
            yield return new WaitForEndOfFrame();
            while (mainCameraBrain.IsBlending)
            {
                yield return null;
            }
        }

        transitionCoroutine = null;
        Debug.Log("[CameraManager] Reverse arc sweep complete. Returned to 3rd-person gameplay view.");
    }

    private void SetPriorities(int gameplayPriority, int apexPriority, int minigamePriority)
    {
        if (gameplayCamera != null) gameplayCamera.Priority = gameplayPriority;
        if (arcApexCamera != null) arcApexCamera.Priority = apexPriority;
        if (minigameCamera != null) minigameCamera.Priority = minigamePriority;
    }
}
