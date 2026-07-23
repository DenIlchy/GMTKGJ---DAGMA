using System;
using UnityEngine;

/// <summary>
/// Abstract base class for all 2D UI minigames.
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    public event Action OnMinigameCompleted;
    public event Action OnMinigameFailed;

    public bool IsCompleted { get; protected set; }
    public bool IsActive => gameObject.activeInHierarchy;

    /// <summary>
    /// Called when the minigame is opened and initialized.
    /// </summary>
    public virtual void StartMinigame()
    {
        IsCompleted = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Called to close and clean up the minigame UI.
    /// </summary>
    public virtual void CloseMinigame()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Call when the player successfully solves the minigame.
    /// </summary>
    protected void CompleteMinigame()
    {
        if (IsCompleted) return;

        IsCompleted = true;
        Debug.Log($"[Minigame] {gameObject.name} SOLVED!");
        OnMinigameCompleted?.Invoke();
    }

    /// <summary>
    /// Call if the minigame is failed/reset.
    /// </summary>
    protected void FailMinigame()
    {
        Debug.Log($"[Minigame] {gameObject.name} FAILED!");
        OnMinigameFailed?.Invoke();
    }
}
