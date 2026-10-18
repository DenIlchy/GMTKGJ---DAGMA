using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One asset per playable character declaring which minigames it can be assigned and with what
/// base weight. The minigames themselves live on that character's canvas child — this asset
/// holds only ids and weights, matched to Minigame.MinigameId at runtime.
/// </summary>
[CreateAssetMenu(menuName = "DAGMA/Character Minigame Pool")]
public class CharacterMinigamePool : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Must match Minigame.MinigameId on a minigame under this character's canvas.")]
        public string minigameId;
        [Min(0f)] public float baseWeight = 1f;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    public IReadOnlyList<Entry> Entries => entries;
}
