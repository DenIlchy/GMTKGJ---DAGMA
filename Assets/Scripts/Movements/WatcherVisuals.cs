using UnityEngine;

public class WatcherVisuals : MonoBehaviour
{
    private static readonly int StateParam = Animator.StringToHash("State");
    private static readonly int HasViolatorsParam = Animator.StringToHash("HasViolators");

    [Header("References")]
    [Tooltip("Animator driven by State int (0=green, 1=turn, 3=red) and HasViolators bool (turns to shooting).")]
    [SerializeField] private Animator animator;
    [Tooltip("Optional renderer whose material color signals the current state.")]
    [SerializeField] private Renderer signalRenderer;

    [Header("Signal Colors")]
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color redColor = Color.red;

    private GameSys gameSys;

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
    }

    private void OnDestroy()
    {
        if (gameSys != null)
            gameSys.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.GreenLight:
                SetState(0);
                SetViolators(false);
                SetSignalColor(greenColor);
                break;

            case GameState.RedLightWarning:
                SetViolators(false);
                SetSignalColor(redColor);
                break;

            case GameState.PenaltyFeedback:
                SetState(1);
                SetViolators(true);
                SetSignalColor(redColor);
                break;

            case GameState.RedLight:
                SetState(3);
                SetSignalColor(redColor);
                break;
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

    private void SetSignalColor(Color color)
    {
        if (signalRenderer != null)
            signalRenderer.material.color = color;
    }
}
