using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DAGMAPenaltyController : MonoBehaviour
{
    [Header("Penalty Settings")]
    [Tooltip("Duration of the target stun animation & minigame delay in seconds (editable in Inspector for balance).")]
    public float stunDelay = 2.0f;

    [Tooltip("Simulated shoot animation duration before impact event fires.")]
    public float shootAnimationDuration = 0.3f;

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

        if (shootAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(shootAnimationDuration);
        }

        // Step 2: Impact -> Apply Pushback & Stun Animation to all violators
        Debug.Log($"[DAGMAPenaltyController] DAGMA SHOOTS! Applying pushback ({pushBackDistance}m) and stun animation ({stunDelay}s)...");

        foreach (var violator in violators)
        {
            if (violator == null) continue;

            // Pushback result of shoot
            violator.PushBack(pushBackDistance);

            // Trigger target stun animation (delays minigame start)
            violator.ApplyStunAnimation(stunDelay);
        }

        // Step 3: Wait for stun animation delay before proceeding
        if (stunDelay > 0f)
        {
            yield return new WaitForSeconds(stunDelay);
        }

        Debug.Log("[DAGMAPenaltyController] Penalty sequence complete.");
        onComplete?.Invoke();
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
