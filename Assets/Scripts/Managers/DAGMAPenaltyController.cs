using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DAGMAPenaltyController : MonoBehaviour
{
    [Header("Config (preferred) — overrides all Legacy values below")]
    [SerializeField] private PenaltyConfig penaltyConfig;

    [Header("Penalty Settings (Legacy)")]
    [Tooltip("Duration of the target stun animation in seconds. Does not delay the minigame.")]
    public float stunDelay = 2.0f;

    [Tooltip("Simulated shoot animation duration before impact event fires.")]
    public float shootAnimationDuration = 0.3f;

    private float StunDelay => penaltyConfig != null ? penaltyConfig.StunDelay : stunDelay;
    private float ShootAnimationDuration => penaltyConfig != null ? penaltyConfig.ShootAnimationDuration : shootAnimationDuration;

    [Header("Animation Event Hookup")]
    [Tooltip("UnityEvent triggered when DAGMA shoots. Attach animation events here when new model arrives.")]
    public UnityEvent OnDAGMAShoot;

    public static DAGMAPenaltyController Instance { get; private set; }

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

    /// <summary>
    /// Executes the DAGMA shoot animation -> Pushback -> Target Stun Animation sequence.
    /// </summary>
    public void ExecutePenaltySequence(List<IMovable> violators, float pushBackDistance, Action onComplete)
    {
        StartCoroutine(PenaltySequenceCoroutine(violators, pushBackDistance, onComplete));
    }

    private IEnumerator PenaltySequenceCoroutine(List<IMovable> violators, float pushBackDistance, Action onComplete)
    {
        if (violators == null || violators.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[DAGMAPenaltyController] DAGMA targeting naughty players...");

        // Step 1: Trigger DAGMA Shoot Animation Event
        OnDAGMAShoot?.Invoke();

        if (ShootAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(ShootAnimationDuration);
        }

        // Step 2: Impact -> Apply Pushback & Stun Animation to all violators
        Debug.Log($"[DAGMAPenaltyController] DAGMA SHOOTS! Applying pushback ({pushBackDistance}m) and stun animation ({StunDelay}s)...");

        bool playerIsViolator = System.Linq.Enumerable.Any(violators, v => v != null && v.IsPlayer);

        foreach (var violator in violators)
        {
            if (violator == null) continue;

            violator.PushBack(pushBackDistance);

            // Trigger target stun animation on each violator
            violator.ApplyStunAnimation(StunDelay);
        }

        // The minigame must open during Red Light; waiting out the stun (tunable, can exceed
        // the red light) pushed it into the next phase.
        Debug.Log("[DAGMAPenaltyController] Impact applied — opening minigame without waiting for stun.");
        onComplete?.Invoke();

        // Step 3: Wait for stun animation delay before resetting watcher visuals ONLY IF local player violated
        if (playerIsViolator && StunDelay > 0f)
        {
            Debug.Log($"[DAGMAPenaltyController] Player violated! Waiting out player stun ({StunDelay}s) before resetting watcher visuals...");
            yield return new WaitForSeconds(StunDelay);
        }
        else
        {
            Debug.Log("[DAGMAPenaltyController] Player is innocent! Resetting watcher visuals immediately.");
        }

        if (WatcherVisuals.Instance != null)
        {
            WatcherVisuals.Instance.ResetViolatorParameters();
        }

        Debug.Log("[DAGMAPenaltyController] Penalty sequence complete.");
    }

    /// <summary>
    /// Animation Event Callback method. Can be called directly from an Animator Animation Event on DAGMA model.
    /// </summary>
    public void OnDAGMAShootAnimationEvent()
    {
        Debug.Log("[DAGMAPenaltyController] Animation Event Received: OnDAGMAShootAnimationEvent");
        OnDAGMAShoot?.Invoke();
    }
}
