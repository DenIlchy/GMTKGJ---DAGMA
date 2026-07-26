using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WatcherVisuals : MonoBehaviour
{
    private static readonly int StateParam = Animator.StringToHash("State");
    private static readonly int HasViolatorsParam = Animator.StringToHash("HasViolators");
    private static readonly int TargetTypeParam = Animator.StringToHash("TargetType");

    [Header("References")]
    [Tooltip("Animator driven by State int (0=green, 1=turn, 3=red), HasViolators bool, and TargetType int (0=none, 1=player, 2=bot).")]
    [SerializeField] private Animator animator;
    [Tooltip("Optional renderer whose material color signals the current state.")]
    [SerializeField] private Renderer signalRenderer;

    [Header("Signal Colors")]
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color redColor = Color.red;

    public static WatcherVisuals Instance { get; private set; }

    private GameSys gameSys;

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
        if (animator == null)
            animator = GetComponent<Animator>();

        gameSys = GameSys.Instance;
        if (gameSys == null)
        {
            Debug.LogWarning("WatcherVisuals: no GameSys instance found in the scene.");
            enabled = false;
            return;
        }

        gameSys.OnStateChanged += HandleStateChanged;
        gameSys.OnPenaltyFeedbackStarted += HandlePenaltyFeedbackStarted;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (gameSys != null)
        {
            gameSys.OnStateChanged -= HandleStateChanged;
            gameSys.OnPenaltyFeedbackStarted -= HandlePenaltyFeedbackStarted;
        }
    }

    public void ResetViolatorParameters()
    {
        SetViolators(false);
        SetTargetType(0);
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.GreenLight:
                SetState(0);
                ResetViolatorParameters();
                SetSignalColor(greenColor);
                break;

            case GameState.RedLightWarning:
                SetState(1); // Triggers Turn animation every round!
                ResetViolatorParameters();
                SetSignalColor(redColor);
                break;

            case GameState.RedLight:
                SetState(3);
                ResetViolatorParameters();
                SetSignalColor(redColor);
                break;

            case GameState.PenaltyFeedback:
                SetSignalColor(redColor);
                break;
        }
    }

    private void HandlePenaltyFeedbackStarted(List<IMovable> violators)
    {
        if (violators == null || violators.Count == 0)
        {
            SetViolators(false);
            SetTargetType(0);
            return;
        }

        SetViolators(true);

        bool playerViolated = violators.Any(v => v != null && v.IsPlayer);
        if (playerViolated)
        {
            SetTargetType(1); // 1 = Local Player
            Debug.Log("[WatcherVisuals] Fish targeting Player (TargetType = 1)");
        }
        else
        {
            SetTargetType(2); // 2 = AI Bot
            Debug.Log("[WatcherVisuals] Fish targeting Bot (TargetType = 2)");
        }
    }

    private void SetState(int value)
    {
        if (animator != null)
            animator.SetInteger(StateParam, value);
    }

    private void SetViolators(bool value)
    {
        if (animator != null)
            animator.SetBool(HasViolatorsParam, value);
    }

    private void SetTargetType(int value)
    {
        if (animator != null)
            animator.SetInteger(TargetTypeParam, value);
    }

    private void SetSignalColor(Color color)
    {
        if (signalRenderer != null)
            signalRenderer.material.color = color;
    }
}
