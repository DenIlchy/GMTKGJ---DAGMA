using System;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [Header("Minigame References")]
    [Tooltip("Reference to the active minigame object in the UI Canvas.")]
    [SerializeField] private Minigame currentMinigame;

    [Header("Behavior Settings")]
    [Tooltip("Automatically switch back to 3rd-person gameplay camera when minigame is completed.")]
    [SerializeField] private bool autoReturnToGameplayCam = true;

    public event Action OnActiveMinigameCompleted;

    public static MinigameManager Instance { get; private set; }

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
        if (currentMinigame != null)
        {
            // Ensure minigame starts hidden
            currentMinigame.CloseMinigame();
            SubscribeToMinigame(currentMinigame);
        }
    }

    private void SubscribeToMinigame(Minigame minigame)
    {
        if (minigame == null) return;
        minigame.OnMinigameCompleted += HandleMinigameCompleted;
    }

    private void UnsubscribeFromMinigame(Minigame minigame)
    {
        if (minigame == null) return;
        minigame.OnMinigameCompleted -= HandleMinigameCompleted;
    }

    /// <summary>
    /// Opens and starts the active minigame.
    /// </summary>
    public void ShowMinigame()
    {
        if (currentMinigame != null)
        {
            currentMinigame.StartMinigame();
            Debug.Log("[MinigameManager] Minigame UI Opened.");
        }
        else
        {
            Debug.LogWarning("[MinigameManager] No current minigame assigned!");
        }
    }

    /// <summary>
    /// Closes the active minigame UI.
    /// </summary>
    public void HideMinigame()
    {
        if (currentMinigame != null)
        {
            currentMinigame.CloseMinigame();
            Debug.Log("[MinigameManager] Minigame UI Closed.");
        }
    }

    /// <summary>
    /// Force-closes the minigame (used when Red Light ends or 2s timeout expires).
    /// </summary>
    public void ForceCloseMinigame()
    {
        HideMinigame();
        if (autoReturnToGameplayCam && CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchToGameplayCamera();
        }
    }

    private void HandleMinigameCompleted()
    {
        Debug.Log("[MinigameManager] Player completed minigame successfully!");
        OnActiveMinigameCompleted?.Invoke();

        if (autoReturnToGameplayCam && CameraManager.Instance != null)
        {
            CameraManager.Instance.SwitchToGameplayCamera();
        }

        HideMinigame();
    }

    /// <summary>
    /// Assigns a new minigame object dynamically.
    /// </summary>
    public void SetActiveMinigame(Minigame newMinigame)
    {
        if (currentMinigame != null)
        {
            UnsubscribeFromMinigame(currentMinigame);
        }

        currentMinigame = newMinigame;
        if (currentMinigame != null)
        {
            SubscribeToMinigame(currentMinigame);
        }
    }

    private void OnDestroy()
    {
        if (currentMinigame != null)
        {
            UnsubscribeFromMinigame(currentMinigame);
        }
    }
}
