using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class MinigameWeightEntry
{
    public Minigame minigame;
    public float currentWeight = 1.0f;
}

public class RedLightMinigameManager : MonoBehaviour
{
    [Header("Minigame Pool Configuration")]
    [Tooltip("List of minigames managed during Red Light cycles.")]
    [SerializeField] private List<MinigameWeightEntry> minigamePool = new List<MinigameWeightEntry>();

    [Header("Continuous Weight Parameters")]
    [Tooltip("Weight assigned to a minigame right after it has been played.")]
    [SerializeField] private float selectedWeightDrop = 0.1f;
    [Tooltip("Weight recovered by unselected minigames per Red Light round.")]
    [SerializeField] private float weightRecoveryPerRound = 0.25f;

    [Header("Green Light Grace Period Settings")]
    [Tooltip("Grace period duration in seconds when Green Light starts before the minigame is auto-closed.")]
    public float gracePeriodDuration = 2.0f;
    [Tooltip("Optional text display for status messages (e.g. 'Better luck next time!').")]
    [SerializeField] private TextMeshProUGUI statusMessageText;

    private GameSys gameSys;
    private Coroutine gracePeriodCoroutine;
    private bool isMinigameActive;

    public static RedLightMinigameManager Instance { get; private set; }

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
        ConnectToGameSys();
        RegisterDefaultMinigames();
    }

    private void ConnectToGameSys()
    {
        gameSys = GameSys.Instance;
        if (gameSys == null) return;

        gameSys.OnRedLightStarted += HandleRedLightStarted;
        gameSys.OnGreenLightStarted += HandleGreenLightStarted;
    }

    private void OnDestroy()
    {
        if (gameSys != null)
        {
            gameSys.OnRedLightStarted -= HandleRedLightStarted;
            gameSys.OnGreenLightStarted -= HandleGreenLightStarted;
        }

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnActiveMinigameCompleted -= HandleMinigameCompleted;
        }
    }

    private void RegisterDefaultMinigames()
    {
        if (minigamePool.Count > 0) return;

        var minigames = FindObjectsByType<Minigame>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mg in minigames)
        {
            minigamePool.Add(new MinigameWeightEntry { minigame = mg, currentWeight = 1.0f });
        }
    }

    private void HandleRedLightStarted(float duration)
    {
        // Subscribe to minigame completion
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnActiveMinigameCompleted -= HandleMinigameCompleted;
            MinigameManager.Instance.OnActiveMinigameCompleted += HandleMinigameCompleted;
        }

        // Gather current violators
        List<IMovable> violators = GetCurrentViolators();

        if (DAGMAPenaltyController.Instance != null)
        {
            float pushBackDist = gameSys != null ? gameSys.GetPushBackDistance() : 2.0f;
            DAGMAPenaltyController.Instance.ExecutePenaltySequence(violators, pushBackDist, LaunchMinigameSequence);
        }
        else
        {
            LaunchMinigameSequence();
        }
    }

    private List<IMovable> GetCurrentViolators()
    {
        List<IMovable> violators = new List<IMovable>();
        if (gameSys == null) return violators;

        float threshold = gameSys.GetSpeedThreshold();
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is IMovable mover && mover.GetCurrentSpeed() > threshold)
            {
                violators.Add(mover);
            }
        }
        return violators;
    }

    private void LaunchMinigameSequence()
    {
        // Step 1: Select weighted random minigame
        Minigame selectedMinigame = PickWeightedMinigame();

        if (selectedMinigame == null)
        {
            Debug.LogWarning("[RedLightMinigameManager] No minigame available in pool!");
            return;
        }

        Debug.Log($"[RedLightMinigameManager] Selected Minigame: {selectedMinigame.gameObject.name}");

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.SetActiveMinigame(selectedMinigame);
        }

        isMinigameActive = true;

        // Step 2: Trigger full 3-point camera arc sweep
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchToMinigameCamera();
        }
    }

    private Minigame PickWeightedMinigame()
    {
        if (minigamePool == null || minigamePool.Count == 0) return null;

        // Calculate total weight
        float totalWeight = 0f;
        foreach (var entry in minigamePool)
        {
            if (entry.minigame != null)
                totalWeight += entry.currentWeight;
        }

        if (totalWeight <= 0f) return minigamePool[0].minigame;

        // Pick random roll
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;
        Minigame selected = null;
        MinigameWeightEntry selectedEntry = null;

        foreach (var entry in minigamePool)
        {
            if (entry.minigame == null) continue;

            currentSum += entry.currentWeight;
            if (roll <= currentSum)
            {
                selected = entry.minigame;
                selectedEntry = entry;
                break;
            }
        }

        if (selected == null && minigamePool.Count > 0)
        {
            selected = minigamePool[0].minigame;
            selectedEntry = minigamePool[0];
        }

        // Apply continuous weight updates
        foreach (var entry in minigamePool)
        {
            if (entry == selectedEntry)
            {
                // Drop selected minigame weight
                entry.currentWeight = selectedWeightDrop;
            }
            else
            {
                // Incremental recovery for unselected minigames (+0.25 up to max 1.0)
                entry.currentWeight = Mathf.Min(1.0f, entry.currentWeight + weightRecoveryPerRound);
            }
            Debug.Log($"[RedLightMinigameManager] Weight table -> {entry.minigame.name}: {entry.currentWeight:F2}");
        }

        return selected;
    }

    private void HandleMinigameCompleted()
    {
        isMinigameActive = false;
        if (gracePeriodCoroutine != null)
        {
            StopCoroutine(gracePeriodCoroutine);
            gracePeriodCoroutine = null;
        }
        HideStatusMessage();
    }

    private void HandleGreenLightStarted(float duration)
    {
        if (!isMinigameActive) return;

        // If player is still inside the minigame when Green Light starts, trigger grace period timer!
        if (gracePeriodCoroutine != null)
        {
            StopCoroutine(gracePeriodCoroutine);
        }
        gracePeriodCoroutine = StartCoroutine(GreenLightGracePeriodRoutine());
    }

    private IEnumerator GreenLightGracePeriodRoutine()
    {
        Debug.Log($"[RedLightMinigameManager] Green Light started while in minigame! Grace period of {gracePeriodDuration}s active...");
        
        yield return new WaitForSeconds(gracePeriodDuration);

        if (isMinigameActive)
        {
            Debug.Log("[RedLightMinigameManager] Grace period expired! Displaying 'Better luck next time!' and closing minigame.");
            
            ShowStatusMessage("Better luck next time!");

            yield return new WaitForSeconds(0.8f);

            HideStatusMessage();
            isMinigameActive = false;

            if (MinigameManager.Instance != null)
            {
                MinigameManager.Instance.ForceCloseMinigame();
            }
            else if (CameraManager.Instance != null)
            {
                CameraManager.Instance.SwitchToGameplayCamera();
            }
        }

        gracePeriodCoroutine = null;
    }

    private void ShowStatusMessage(string msg)
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = msg;
            statusMessageText.gameObject.SetActive(true);
        }
    }

    private void HideStatusMessage()
    {
        if (statusMessageText != null)
        {
            statusMessageText.gameObject.SetActive(false);
        }
    }
}
