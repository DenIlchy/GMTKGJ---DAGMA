using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum GameState
{
    Intro,
    GreenLight,
    RedLightWarning,
    RedLight,
    PenaltyFeedback,
    Victory,
    GameOver
}

public class GameSys : MonoBehaviour
{
    private static GameSys _instance;
    public static GameSys Instance
    {
        get
        {
            if (_instance != null && _instance.gameObject == null)
                _instance = null;
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Config (preferred) — overrides all Legacy values below")]
    [SerializeField] private PhaseTimingConfig phaseTiming;
    [SerializeField] private RaceRulesConfig raceRules;

    [Header("Phase Durations (Legacy)")]
    [Tooltip("Random range (min, max) for the total Green Light duration in seconds.")]
    [SerializeField] private Vector2 greenLightDurationRange = new Vector2(4f, 10f);
    [Tooltip("The last N seconds of the Green Light are the Red Light warning.")]
    [SerializeField] private float warningDuration = 3f;
    [Tooltip("Random range (min, max) for the Red Light duration in seconds.")]
    [SerializeField] private Vector2 redLightDurationRange = new Vector2(4f, 8f);
    [SerializeField] private Vector2 postRedLightDelayRange = new Vector2(0f, 3f);
    [Tooltip("How long the 'You moved!' feedback is shown before the penalty is applied.")]
    [SerializeField] private float penaltyFeedbackDuration = 1f;

    [Header("Red Light Timer Adjustment (Legacy)")]
    [Tooltip("Public delay (in seconds) added before the Red Light timer starts counting down on screen. Adjust in Inspector to perfectly match camera transition duration.")]
    public float redLightTimerStartDelay = 0.85f;

    [Header("Intro Duration (Legacy)")]
    [Tooltip("Delay before the first Green Light starts (Intro state).")]
    [SerializeField] private float introDuration = 2f;
    [Header("Level")]
    [Tooltip("Panel shown before the intro. The intro starts only after this panel is deactivated.")]
    [SerializeField] private GameObject startPanel;

    [Header("Penalty (Legacy)")]
    [Tooltip("Absolute speed above which a mover is considered violating during Red Light.")]
    [SerializeField] private float speedThreshold = 0.5f;
    [Tooltip("Distance a violator is pushed back.")]
    [SerializeField] private float pushBackDistance = 2f;

    private Vector2 GreenLightDurationRange => phaseTiming != null ? phaseTiming.GreenLightDurationRange : greenLightDurationRange;
    private float WarningDuration => phaseTiming != null ? phaseTiming.WarningDuration : warningDuration;
    private Vector2 RedLightDurationRange => phaseTiming != null ? phaseTiming.RedLightDurationRange : redLightDurationRange;
    private Vector2 PostRedLightDelayRange => phaseTiming != null ? phaseTiming.PostRedLightDelayRange : postRedLightDelayRange;
    private float PenaltyFeedbackDuration => phaseTiming != null ? phaseTiming.PenaltyFeedbackDuration : penaltyFeedbackDuration;
    private float IntroDuration => phaseTiming != null ? phaseTiming.IntroDuration : introDuration;
    private float RedLightTimerStartDelay => phaseTiming != null ? phaseTiming.RedLightTimerStartDelay : redLightTimerStartDelay;
    private float SpeedThreshold => raceRules != null ? raceRules.SpeedThreshold : speedThreshold;
    private float PushBackDistance => raceRules != null ? raceRules.PushBackDistance : pushBackDistance;

    public GameState CurrentState { get; private set; } = GameState.Intro;
    public bool IsWaitingForStartPanel => startPanel != null && startPanel.activeInHierarchy;
    public int CurrentCycle { get; private set; } = 0;

    public event Action<GameState> OnStateChanged;
    public event Action<float> OnGreenLightStarted;
    public event Action<float> OnRedLightWarningStarted;
    public event Action<float> OnRedLightStarted;
    public event Action<List<IMovable>> OnPenaltyFeedbackStarted;
    public event Action<List<IMovable>> OnPenaltyApplied;
    public event Action OnVictory;
    public event Action OnGameOver;
    public event Action<float> OnPhaseTimerUpdated;

    private readonly List<IMovable> movers = new List<IMovable>();
    private Coroutine gameLoopCoroutine;
    private bool gameEnded;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (startPanel == null)
            Debug.LogWarning("[GameSys] Start Panel is not assigned in the Inspector — the game will start immediately without waiting for it.");
        else if (!startPanel.activeInHierarchy)
            Debug.LogWarning("[GameSys] Start Panel is assigned but already inactive at scene start — the game will start immediately. Make sure the panel is active in the scene.");

        RegisterSceneMovers();
        gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    private void RegisterSceneMovers()
    {
        movers.Clear();
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is IMovable movable && !movers.Contains(movable))
            {
                movers.Add(movable);
            }
        }
    }

    public void RegisterMover(IMovable mover)
    {
        if (!movers.Contains(mover))
            movers.Add(mover);
    }

    public void UnregisterMover(IMovable mover)
    {
        movers.Remove(mover);
    }

    private IEnumerator GameLoop()
    {
        SetAllMovementBlocked(true);

        while (IsWaitingForStartPanel)
            yield return null;

        SetState(GameState.Intro);
        yield return RunPhaseTimer(IntroDuration);

        while (!gameEnded)
        {
            // --- Green Light ---
            CurrentCycle++;
            Debug.Log($"[GameSys] --- Cycle #{CurrentCycle} Started (Green Light) ---");
            float greenDuration = UnityEngine.Random.Range(GreenLightDurationRange.x, GreenLightDurationRange.y);
            float safeDuration = Mathf.Max(0f, greenDuration - WarningDuration);

            SetState(GameState.GreenLight);
            OnGreenLightStarted?.Invoke(greenDuration);
            SetAllMovementBlocked(false);
            yield return RunPhaseTimer(safeDuration, WarningDuration);
            if (gameEnded) yield break;

            // --- Red Light Warning (last 3s of green) ---
            SetState(GameState.RedLightWarning);
            OnRedLightWarningStarted?.Invoke(WarningDuration);
            yield return RunPhaseTimer(WarningDuration);
            if (gameEnded) yield break;

            // --- Red Light: timer is out => speed check ---
            float redDuration = UnityEngine.Random.Range(RedLightDurationRange.x, RedLightDurationRange.y);

            List<IMovable> violators = movers
                .Where(m => m != null && m.GetCurrentSpeed() > SpeedThreshold)
                .ToList();

            // --- Visual feedback, then penalty ---
            if (violators.Count > 0)
            {
                SetState(GameState.PenaltyFeedback);
                OnPenaltyFeedbackStarted?.Invoke(violators);
                yield return new WaitForSeconds(PenaltyFeedbackDuration);
                if (gameEnded) yield break;

                OnPenaltyApplied?.Invoke(violators);
            }

            // --- Block all movement until the red light is over ---
            SetAllMovementBlocked(true);

            // --- Red Light starts only after penalty has been given ---
            SetState(GameState.RedLight);
            OnRedLightStarted?.Invoke(redDuration);

            // Delay Red Light timer countdown until camera transition completes
            float cameraTransitionDelay = CameraManager.Instance != null ? CameraManager.Instance.GetTotalTransitionDuration() : 0.85f;
            float totalTimerDelay = Mathf.Max(cameraTransitionDelay, RedLightTimerStartDelay);

            if (totalTimerDelay > 0f)
            {
                yield return new WaitForSeconds(totalTimerDelay);
            }

            yield return RunPhaseTimer(redDuration);
            if (gameEnded) yield break;

            yield return new WaitForSeconds(UnityEngine.Random.Range(PostRedLightDelayRange.x, PostRedLightDelayRange.y));
            if (gameEnded) yield break;
        }
    }

    private void SetAllMovementBlocked(bool blocked)
    {
        foreach (var mover in movers)
        {
            if (mover != null)
                mover.SetMovementBlocked(blocked);
        }
    }

    private IEnumerator RunPhaseTimer(float duration, float reportOffset = 0f)
    {
        float remaining = duration;
        while (remaining > 0f && !gameEnded)
        {
            remaining -= Time.deltaTime;
            OnPhaseTimerUpdated?.Invoke(Mathf.Max(0f, remaining) + reportOffset);
            yield return null;
        }
    }

    public void ReportFinished(IMovable mover)
    {
        if (gameEnded || mover == null)
            return;

        EndGame(mover.IsPlayer);
    }

    private void EndGame(bool playerWon)
    {
        if (gameEnded)
            return;

        gameEnded = true;
        SetAllMovementBlocked(true);

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.ForceCloseMinigame();
        }

        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
            gameLoopCoroutine = null;
        }

        if (playerWon)
        {
            SetState(GameState.Victory);
            OnVictory?.Invoke();
        }
        else
        {
            SetState(GameState.GameOver);
            OnGameOver?.Invoke();
        }

        Time.timeScale = 0f;
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void DismissStartPanel()
    {
        if (startPanel != null)
            startPanel.SetActive(false);
    }

    public float GetSpeedThreshold() => SpeedThreshold;
    public float GetPushBackDistance() => PushBackDistance;
}
