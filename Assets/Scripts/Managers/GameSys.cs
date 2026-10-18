using System;
using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Public delay (in seconds) added before the Red Light timer starts counting down on screen. Must be greater than or equal to the camera's total transition duration (stepDuration * 2 + uiPopupDelay on CameraTransitionConfig, currently 0.75s), or the Red Light timer will start counting before the camera has settled.")]
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
    public bool IsWaitingForStartPanel => startGate.IsWaiting;
    public int CurrentCycle { get; private set; } = 0;

    public event Action<GameState> OnStateChanged;
    public event Action<float> OnGreenLightStarted;
    public event Action<float> OnRedLightWarningStarted;
    public event Action<float> OnRedLightStarted;
    public event Action<List<IMovable>> OnPenaltyFeedbackStarted;
    public event Action<List<IMovable>> OnPenaltyApplied;
    public event Action OnVictory;
    public event Action OnGameOver;
    public event Action OnRaceEnded;
    public event Action<float> OnPhaseTimerUpdated;

    private readonly MoverRegistry registry = new MoverRegistry();
    private readonly PhaseTimer phaseTimer = new PhaseTimer();
    private StartGate startGate;
    private bool allMovementBlocked = true;
    private Coroutine gameLoopCoroutine;
    private bool gameEnded;

    private void Awake()
    {
        TimeControl.Resume();

        // Built before the duplicate-instance check: Destroy() here is deferred to the end of the
        // frame, so a doomed duplicate still runs Start() and would dereference a null startGate.
        startGate = new StartGate(startPanel);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        registry.OnMoverRegistered += HandleMoverRegistered;
        phaseTimer.OnTick += t => OnPhaseTimerUpdated?.Invoke(t);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        startGate.LogConfigurationWarnings();

        gameLoopCoroutine = StartCoroutine(GameLoop());
    }

    private void HandleMoverRegistered(IMovable mover)
    {
        // A mover that registers after the loop already blocked everyone must not be left unblocked.
        mover.SetMovementBlocked(allMovementBlocked);
    }

    public void RegisterMover(IMovable mover)
    {
        registry.Register(mover);
    }

    public void UnregisterMover(IMovable mover)
    {
        registry.Unregister(mover);
    }

    private IEnumerator GameLoop()
    {
        SetAllMovementBlocked(true);

        while (IsWaitingForStartPanel)
            yield return null;

        SetState(GameState.Intro);
        yield return phaseTimer.Run(IntroDuration, 0f, () => gameEnded);

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
            yield return phaseTimer.Run(safeDuration, WarningDuration, () => gameEnded);
            if (gameEnded) yield break;

            // --- Red Light Warning (last 3s of green) ---
            SetState(GameState.RedLightWarning);
            OnRedLightWarningStarted?.Invoke(WarningDuration);
            yield return phaseTimer.Run(WarningDuration, 0f, () => gameEnded);
            if (gameEnded) yield break;

            // --- Red Light: timer is out => speed check ---
            float redDuration = UnityEngine.Random.Range(RedLightDurationRange.x, RedLightDurationRange.y);

            List<IMovable> violators = ViolationDetector.FindViolators(registry.Movers, SpeedThreshold);

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

            // Delay Red Light timer countdown so it does not start before the camera has settled
            if (RedLightTimerStartDelay > 0f)
            {
                yield return new WaitForSeconds(RedLightTimerStartDelay);
            }

            yield return phaseTimer.Run(redDuration, 0f, () => gameEnded);
            if (gameEnded) yield break;

            yield return new WaitForSeconds(UnityEngine.Random.Range(PostRedLightDelayRange.x, PostRedLightDelayRange.y));
            if (gameEnded) yield break;
        }
    }

    private void SetAllMovementBlocked(bool blocked)
    {
        allMovementBlocked = blocked;
        foreach (var mover in registry.Movers)
        {
            if (mover != null)
                mover.SetMovementBlocked(blocked);
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

        OnRaceEnded?.Invoke();

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

        TimeControl.Freeze();
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    public void DismissStartPanel()
    {
        startGate.Dismiss();
    }

    public float GetSpeedThreshold() => SpeedThreshold;
    public float GetPushBackDistance() => PushBackDistance;
}
