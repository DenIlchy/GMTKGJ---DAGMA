using System;
using System.Collections;
using UnityEngine;

public class PhaseTimer
{
    public event Action<float> OnTick;

    public IEnumerator Run(float duration, float reportOffset, Func<bool> shouldAbort)
    {
        float remaining = duration;
        while (remaining > 0f && (shouldAbort == null || !shouldAbort()))
        {
            remaining -= Time.deltaTime;
            OnTick?.Invoke(Mathf.Max(0f, remaining) + reportOffset);
            yield return null;
        }
    }
}
