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
    [Header("Config (preferred) — overrides all Legacy values below")]
    [SerializeField] private MinigamePoolConfig poolConfig;
    [SerializeField] private UIThemeConfig theme;

    [Header("Minigame Pool Configuration")]
    [Tooltip("List of minigames managed during Red Light cycles.")]
    [SerializeField] private List<MinigameWeightEntry> minigamePool = new List<MinigameWeightEntry>();

    [Header("Continuous Weight Parameters (Legacy)")]
    [Tooltip("Weight assigned to a minigame right after it has been played.")]
    [SerializeField] private float selectedWeightDrop = 0.1f;
    [Tooltip("Weight recovered by unselected minigames per Red Light round.")]
    [SerializeField] private float weightRecoveryPerRound = 0.25f;

    [Header("Pre-Transition Delay (Legacy)")]
    [Tooltip("Public delay before starting the minigame camera arc sweep transition (editable in Inspector for balance).")]
    public float preTransitionDelay = 0.0f;

    [Header("Grace Period Duration (Legacy)")]
    [Tooltip("Grace period duration in seconds when Green Light starts before the minigame is auto-closed.")]
    public float gracePeriodDuration = 2.0f;
    [Header("Green Light Grace Period Settings")]
    [Tooltip("Optional text display for status messages (e.g. 'Better luck next time!').")]
    [SerializeField] private TextMeshProUGUI statusMessageText;

    private float SelectedWeightDrop => poolConfig != null ? poolConfig.SelectedWeightDrop : selectedWeightDrop;
    private float WeightRecoveryPerRound => poolConfig != null ? poolConfig.WeightRecoveryPerRound : weightRecoveryPerRound;
    private float InitialWeight => poolConfig != null ? poolConfig.InitialWeight : 1.0f;
    private float MaxWeight => poolConfig != null ? poolConfig.MaxWeight : 1.0f;
    private float PreTransitionDelay => poolConfig != null ? poolConfig.PreTransitionDelay : preTransitionDelay;
    private float GracePeriodDuration => poolConfig != null ? poolConfig.GracePeriodDuration : gracePeriodDuration;
    private string GraceExpiredMessage => theme != null ? theme.GraceExpiredMessage : "Better luck next time!";

    private MinigameSelector selector;
    private MinigameStatusView statusView;
    private GameSys gameSys;
    private Coroutine gracePeriodCoroutine;
    private bool isMinigameActive;
    private List<IMovable> pendingViolators = new List<IMovable>();

    public static RedLightMinigameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            selector = new MinigameSelector(minigamePool);
            statusView = new MinigameStatusView(statusMessageText);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Destroy() in Awake is deferred to end of frame, so a doomed duplicate still reaches
        // Start() — bail before dereferencing collaborators only the real instance built.
        if (Instance != this) return;

        ConnectToGameSys();
        RegisterDefaultMinigames();
        ResetPoolWeights();
    }

    private void ConnectToGameSys()
    {
        gameSys = GameSys.Instance;
        if (gameSys == null) return;

        gameSys.OnPenaltyFeedbackStarted += HandlePenaltyFeedbackStarted;
        gameSys.OnRedLightStarted += HandleRedLightStarted;
        gameSys.OnGreenLightStarted += HandleGreenLightStarted;
        gameSys.OnRaceEnded += HandleRaceEnded;
    }

    private void OnDestroy()
    {
        if (gameSys != null)
        {
            gameSys.OnPenaltyFeedbackStarted -= HandlePenaltyFeedbackStarted;
            gameSys.OnRedLightStarted -= HandleRedLightStarted;
            gameSys.OnGreenLightStarted -= HandleGreenLightStarted;
            gameSys.OnRaceEnded -= HandleRaceEnded;
        }

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnActiveMinigameCompleted -= HandleMinigameCompleted;
        }
    }

    private void HandlePenaltyFeedbackStarted(List<IMovable> violators)
    {
        if (violators != null && violators.Count > 0)
        {
            pendingViolators = new List<IMovable>(violators);
            Debug.Log($"[RedLightMinigameManager] Cached {pendingViolators.Count} violator(s) from PenaltyFeedback.");
        }
        else
        {
            pendingViolators.Clear();
        }
    }

    private void ResetPoolWeights()
    {
        selector.ResetWeights(InitialWeight);
        Debug.Log($"[RedLightMinigameManager] Minigame pool weights initialized to {InitialWeight} for {minigamePool.Count} entries.");
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

        // Use cached violators from penalty feedback if available
        List<IMovable> violators = new List<IMovable>(pendingViolators);
        pendingViolators.Clear();

        float pushBackDist = gameSys != null ? gameSys.GetPushBackDistance() : 2.0f;
        if (DAGMAPenaltyController.Instance != null)
        {
            DAGMAPenaltyController.Instance.ExecutePenaltySequence(violators, pushBackDist, LaunchMinigameSequence);
        }
        else
        {
            Debug.Log("[RedLightMinigameManager] DAGMAPenaltyController missing — applying pushback directly before minigame.");
            foreach (var violator in violators)
            {
                if (violator == null) continue;
                violator.PushBack(pushBackDist);
            }
            LaunchMinigameSequence();
        }
    }

    private void LaunchMinigameSequence()
    {
        StartCoroutine(LaunchMinigameSequenceCoroutine());
    }

    private IEnumerator LaunchMinigameSequenceCoroutine()
    {
        if (PreTransitionDelay > 0f)
        {
            yield return new WaitForSeconds(PreTransitionDelay);
        }

        // Step 1: Select weighted random minigame
        Minigame selectedMinigame = selector.Pick(SelectedWeightDrop, WeightRecoveryPerRound, MaxWeight);

        if (selectedMinigame == null)
        {
            Debug.LogWarning("[RedLightMinigameManager] No minigame available in pool!");
            yield break;
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
            CameraManager.Instance.SwitchToMinigameCamera(() => MinigameManager.Instance?.ShowMinigame());
        }
    }

    private void HandleRaceEnded()
    {
        MinigameManager.Instance?.ForceCloseMinigame();

        if (gracePeriodCoroutine != null)
        {
            StopCoroutine(gracePeriodCoroutine);
            gracePeriodCoroutine = null;
        }

        isMinigameActive = false;
        statusView.Hide();
    }

    private void HandleMinigameCompleted()
    {
        isMinigameActive = false;
        if (gracePeriodCoroutine != null)
        {
            StopCoroutine(gracePeriodCoroutine);
            gracePeriodCoroutine = null;
        }
        statusView.Hide();
    }

    private void HandleGreenLightStarted(float duration)
    {
        pendingViolators.Clear();

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
        Debug.Log($"[RedLightMinigameManager] Green Light started while in minigame! Grace period of {GracePeriodDuration}s active...");
        
        yield return new WaitForSeconds(GracePeriodDuration);

        if (isMinigameActive)
        {
            Debug.Log("[RedLightMinigameManager] Grace period expired! Displaying 'Better luck next time!' and closing minigame.");
            
            statusView.Show(GraceExpiredMessage);

            yield return new WaitForSeconds(0.8f);

            statusView.Hide();
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
}
