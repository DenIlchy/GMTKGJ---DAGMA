using System.Collections.Generic;
using UnityEngine;

public class MinigameSelector
{
    private readonly List<MinigameWeightEntry> pool;

    public MinigameSelector(List<MinigameWeightEntry> pool)
    {
        this.pool = pool;
    }

    public void ResetWeights(float initialWeight)
    {
        if (pool == null)
            return;

        foreach (var entry in pool)
        {
            if (entry != null)
                entry.currentWeight = initialWeight;
        }
    }

    public Minigame Pick(float selectedWeightDrop, float recoveryPerRound, float maxWeight)
    {
        if (pool == null || pool.Count == 0) return null;

        // Calculate total weight
        float totalWeight = 0f;
        foreach (var entry in pool)
        {
            if (entry.minigame != null)
                totalWeight += entry.currentWeight;
        }

        if (totalWeight <= 0f) return pool[0].minigame;

        // Pick random roll
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;
        Minigame selected = null;
        MinigameWeightEntry selectedEntry = null;

        foreach (var entry in pool)
        {
            if (entry.minigame == null) continue;

            currentSum += entry.currentWeight;
            if (roll <= currentSum)
            {
                selected = entry.minigame;
                selectedEntry = entry;
                break;
            }
        }

        if (selected == null && pool.Count > 0)
        {
            selected = pool[0].minigame;
            selectedEntry = pool[0];
        }

        // Apply continuous weight updates
        foreach (var entry in pool)
        {
            if (entry == selectedEntry)
            {
                // Drop selected minigame weight
                entry.currentWeight = selectedWeightDrop;
            }
            else
            {
                // Incremental recovery for unselected minigames (+0.25 up to max 1.0)
                entry.currentWeight = Mathf.Min(maxWeight, entry.currentWeight + recoveryPerRound);
            }
            string entryName = entry.minigame != null ? entry.minigame.name : "<missing>";
            Debug.Log($"[RedLightMinigameManager] Weight table -> {entryName}: {entry.currentWeight:F2}");
        }

        return selected;
    }
}
