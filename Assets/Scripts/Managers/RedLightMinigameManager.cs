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
    // Runtime pool: resolved per-character, or the serialized minigamePool list. Never written
    // back into the serialized field — it is read-only authored input after Awake.
    private List<MinigameWeightEntry> activePool;
    private bool poolResolvedSuccessfully;
    private bool notReadyWarned;
    private bool poolDiagnosticsReported;
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
            activePool = minigamePool;
            selector = new MinigameSelector(activePool);
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
        Debug.Log($"[RedLightMinigameManager] Minigame pool weights initialized to {InitialWeight} for {activePool.Count} entries.");
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
        ResolvePool();

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

    // Resolves the player's per-character minigame pool on first use (not in Start: the player
    // may not have registered yet and the canvas may activate late). Retrying on failure is
    // deliberate — Phase 3 spawns/activates characters at runtime, possibly after the first red
    // light. Retries are cheap: the GetComponentsInChildren walk only runs once a
    // CharacterMinigames component is found.
    //
    // Two independent log latches, because the two failure kinds mean different things.
    // "Not ready yet" is expected transiently and is latched on its own; the id-join
    // diagnostics are authoring mistakes the user must see. Sharing one latch would let a
    // transient not-ready warning permanently mask a real "this id matches nothing" error.
    private void ResolvePool()
    {
        if (poolResolvedSuccessfully)
            return;

        Transform playerTransform = GameSys.Instance?.GetPlayerMover()?.MoverTransform;
        if (playerTransform == null)
        {
            WarnNotReadyOnce("[RedLightMinigameManager] No player mover registered yet — will retry; using the serialized minigame pool meanwhile.");
            return;
        }

        // Searched across the whole subtree, not just the mover's own GameObject: the character
        // prefabs put CharacterMinigames on a child (e.g. the 'Pool' node) while PlayerMovement
        // lives on the root. includeInactive, since that child may start deactivated.
        CharacterMinigames characterMinigames = playerTransform.GetComponentInChildren<CharacterMinigames>(true);
        if (characterMinigames == null || characterMinigames.Pool == null)
        {
            WarnNotReadyOnce($"[RedLightMinigameManager] '{playerTransform.name}' is missing {(characterMinigames == null ? "a CharacterMinigames component" : "its pool asset")} — will retry; using the serialized minigame pool meanwhile.");
            return;
        }

        // We got far enough to inspect the authored pool, so report join problems — once.
        bool report = !poolDiagnosticsReported;
        poolDiagnosticsReported = true;

        // includeInactive: the minigames sit deactivated under the canvas until shown.
        Minigame[] minigames = playerTransform.GetComponentsInChildren<Minigame>(true);
        var byId = new Dictionary<string, Minigame>();
        foreach (var mg in minigames)
        {
            if (mg == null) continue;
            if (byId.ContainsKey(mg.MinigameId))
            {
                if (report)
                    Debug.LogError($"[RedLightMinigameManager] Duplicate MinigameId '{mg.MinigameId}' under '{playerTransform.name}' — pool join is ambiguous.");
            }
            else
            {
                byId[mg.MinigameId] = mg;
            }
        }

        var resolved = new List<MinigameWeightEntry>();
        var matched = new HashSet<string>();
        foreach (var entry in characterMinigames.Pool.Entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.minigameId)) continue;
            if (byId.TryGetValue(entry.minigameId, out Minigame mg))
            {
                resolved.Add(new MinigameWeightEntry { minigame = mg, currentWeight = entry.baseWeight });
                matched.Add(entry.minigameId);
            }
            else if (report)
            {
                Debug.LogError($"[RedLightMinigameManager] Pool entry '{entry.minigameId}' matches no minigame under '{playerTransform.name}'.");
            }
        }
        if (report)
        {
            foreach (var kvp in byId)
            {
                if (!matched.Contains(kvp.Key))
                    Debug.LogWarning($"[RedLightMinigameManager] Minigame '{kvp.Key}' under '{playerTransform.name}' has no pool entry — it will never be selected.");
            }
        }

        if (resolved.Count == 0)
        {
            if (report)
                Debug.LogWarning($"[RedLightMinigameManager] Pool on '{playerTransform.name}' matched zero minigames — will retry; using the serialized minigame pool meanwhile.");
            return;
        }

        // The resolved entries carry their own authored baseWeight, so InitialWeight is NOT
        // applied to them — it only resets the legacy serialized list in ResetPoolWeights.
        // The serialized minigamePool field is deliberately left untouched (read-only input).
        poolResolvedSuccessfully = true;
        activePool = resolved;
        selector = new MinigameSelector(activePool);
        Debug.Log($"[RedLightMinigameManager] Resolved {resolved.Count} minigame(s) from '{playerTransform.name}'s character pool.");
    }

    // "Not ready yet" is latched separately from the join diagnostics so that retrying every
    // red light does not spam the console, while still never masking an authoring error.
    private void WarnNotReadyOnce(string message)
    {
        if (notReadyWarned)
            return;
        notReadyWarned = true;

        Debug.LogWarning(message);
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
