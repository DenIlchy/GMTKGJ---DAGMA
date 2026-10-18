using UnityEngine;

/// <summary>
/// Put this on a playable character root and assign that character's pool asset.
/// The minigames themselves are expected under this character's canvas child.
/// </summary>
public class CharacterMinigames : MonoBehaviour
{
    [SerializeField] private CharacterMinigamePool pool;
    public CharacterMinigamePool Pool => pool;
}
