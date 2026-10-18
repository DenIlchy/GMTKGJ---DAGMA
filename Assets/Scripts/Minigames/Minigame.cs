using System;
using UnityEngine;

/// <summary>
/// Abstract base class for all 2D UI minigames.
/// </summary>
public abstract class Minigame : MonoBehaviour
{
    [Tooltip("Stable id matched against the character's CharacterMinigamePool entries. Leave empty to use the GameObject name; set it only to decouple the id from a rename.")]
    [SerializeField] private string minigameId;

    public string MinigameId => string.IsNullOrWhiteSpace(minigameId) ? gameObject.name : minigameId;

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

        // SetActive only touches this object: a disabled ancestor still leaves the whole subtree
        // inactive. That renders nothing, and anything a subclass instantiates underneath never
        // receives Awake — which surfaces as a confusing NullReferenceException deep inside the
        // subclass rather than here. Name the culprit instead.
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError(
                $"[Minigame] '{name}' was started but is not active in the hierarchy because " +
                $"'{FindDisabledAncestorName()}' is disabled. It will not render, and objects " +
                "instantiated under it will not run Awake.", this);
        }
    }

    private string FindDisabledAncestorName()
    {
        for (Transform t = transform; t != null; t = t.parent)
        {
            if (!t.gameObject.activeSelf)
                return t.name;
        }
        return "<unknown>";
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
