using UnityEngine;

public class WatcherVisuals : MonoBehaviour
{
    private static readonly int TimeOutParam = Animator.StringToHash("TimeOut");

    [Header("References")]
    [Tooltip("Animator with the open/closed eyes states and the TimeOut bool parameter.")]
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
                SetTimeOut(false);
                SetSignalColor(greenColor);
                break;
            case GameState.RedLight:
                SetTimeOut(true);
                SetSignalColor(redColor);
                break;
        }
    }

    private void SetTimeOut(bool value)
    {
        if (animator != null)
            animator.SetBool(TimeOutParam, value);
    }

    private void SetSignalColor(Color color)
    {
        if (signalRenderer != null)
            signalRenderer.material.color = color;
    }
}
