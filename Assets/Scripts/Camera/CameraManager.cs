using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera gameplayCamera;
    [SerializeField] private CinemachineCamera arcApexCamera;
    [SerializeField] private CinemachineCamera minigameCamera;

    [Header("Waypoint Transition Settings")]
    [Tooltip("Duration of each transition leg (Gameplay -> Arc Apex -> Minigame).")]
    [SerializeField] private float stepDuration = 0.4f;

    [Tooltip("Small extra pause after the camera comes to a complete rest before opening the UI popup.")]
    [SerializeField] private float uiPopupDelay = 0.05f;

    [Header("Debug Testing")]
    [Tooltip("Enable key presses (1/2) and on-screen UI buttons for testing.")]
    [SerializeField] private bool enableDebugHotkeys = true;

    private Coroutine transitionCoroutine;
    private CinemachineBrain mainCameraBrain;

    public static CameraManager Instance { get; private set; }

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
        SwitchToGameplayCamera();
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
    /// Switches directly back to the 3rd-person gameplay camera view.
    /// </summary>
    [ContextMenu("Test Gameplay Camera")]
    public void SwitchToGameplayCamera()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.HideMinigame();
        }

        SetPriorities(gameplayPriority: 20, apexPriority: 5, minigamePriority: 5);
        Debug.Log("Switched to Gameplay Camera View (3rd Person)");
    }

    /// <summary>
    /// Triggers the 3-camera waypoint sequence: Gameplay -> Arc Apex -> Minigame Close-Up.
    /// </summary>
    [ContextMenu("Test Minigame Camera (Arc Sweep)")]
    public void SwitchToMinigameCamera()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        transitionCoroutine = StartCoroutine(ArcWaypointSequence());
    }

    private IEnumerator ArcWaypointSequence()
    {
        // Hide any active minigame during camera sweep
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.HideMinigame();
        }

        // Step 1: Blend from Gameplay to Arc Apex (Over-The-Shoulder Peak)
        SetPriorities(gameplayPriority: 5, apexPriority: 15, minigamePriority: 5);
        yield return new WaitForSeconds(stepDuration);

        // Step 2: Blend from Arc Apex to Minigame Close-Up (Front View)
        SetPriorities(gameplayPriority: 5, apexPriority: 5, minigamePriority: 20);
        yield return new WaitForSeconds(stepDuration);

        // Lock priority on minigame camera cleanly
        SetPriorities(gameplayPriority: 5, apexPriority: 5, minigamePriority: 10);

        // Dynamically wait until CinemachineBrain finishes blending completely
        if (mainCameraBrain != null)
        {
            yield return new WaitForEndOfFrame();
            while (mainCameraBrain.IsBlending)
            {
                yield return null;
            }
        }

        // Small polish pause before UI pops up
        if (uiPopupDelay > 0f)
        {
            yield return new WaitForSeconds(uiPopupDelay);
        }

        transitionCoroutine = null;

        // Open the minigame UI popup ONLY after the camera has come to a 100% complete stop!
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.ShowMinigame();
        }
    }

    private void SetPriorities(int gameplayPriority, int apexPriority, int minigamePriority)
    {
        if (gameplayCamera != null) gameplayCamera.Priority = gameplayPriority;
        if (arcApexCamera != null) arcApexCamera.Priority = apexPriority;
        if (minigameCamera != null) minigameCamera.Priority = minigamePriority;
    }
}
