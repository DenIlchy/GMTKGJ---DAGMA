using UnityEngine;

public class WatcherVisuals : MonoBehaviour
{
    private static readonly int StateParam = Animator.StringToHash("State");

    [Header("References")]
    [Tooltip("Animator with GreenLight/RedLight/Scene states and the State int parameter (0 = Green, 1 = Red).")]
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
                SetSignalColor(greenColor);
                break;
            case GameState.RedLightWarning:
            case GameState.RedLight:
                SetState(1);
                SetSignalColor(redColor);
                break;
        }
    }

    private void SetState(int value)
    {
        if (animator != null)
            animator.SetInteger(StateParam, value);
    }

    private void SetSignalColor(Color color)
    {
        if (signalRenderer != null)
            signalRenderer.material.color = color;
    }
}
