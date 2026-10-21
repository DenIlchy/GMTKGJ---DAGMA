# Assets/Scripts/Minigames

| Type | Role |
| :--- | :--- |
| `Minigame` | Abstract base. Owns `MinigameId`, `IsCompleted`, `OnMinigameCompleted/Failed`. |
| `MinigameManager` | Show/hide the active minigame, HUD toggle, return-to-gameplay. Singleton. |
| `MinigameSelector` | Weighted random pick + weight bookkeeping. Plain C#. |
| `MinigameStatusView` | The "Better luck next time!" label. Plain C#. |
| `CharacterMinigames` | Component on a character; holds that character's `CharacterMinigamePool`. |
| `KeypadMinigame`, `SimonSaysMinigame`, `TemperatureMinigame`, `PopcornMinigame`, `CleaningMinigame` | The implementations. |

## How a minigame gets picked

1. `RedLightMinigameManager.ResolvePool()` finds `CharacterMinigames` under the player, reads its
   `CharacterMinigamePool` asset, and **joins the authored `minigameId` strings against the
   `Minigame` components under that character** (`GetComponentsInChildren<Minigame>(true)` — they
   sit deactivated under the character's `MinigameCanvas` child).
2. Matches become `MinigameWeightEntry` with the authored `baseWeight`.
3. `MinigameSelector.Pick` does weighted random, then drops the winner's weight to
   `SelectedWeightDrop` and recovers everyone else by `WeightRecoveryPerRound` up to `MaxWeight`.

## The id contract — read this before renaming anything

`Minigame.MinigameId` falls back to `gameObject.name` when the `minigameId` field is empty:

```csharp
public string MinigameId => string.IsNullOrWhiteSpace(minigameId) ? gameObject.name : minigameId;
```

**Every minigame in `MinigameCanvas_Microwave.prefab` currently has an empty `minigameId`**, so all
five ids are GameObject names (`Minigame_Keypad`, `SimonSaysMinigame`, `TemperatureMinigame`,
`PopcornMinigame`, `CleaningMinigame`) and they are matched against the strings in
`Assets/GameData/Characters/MinigamePool_Microwave.asset`.

Consequences:

- **Renaming a minigame GameObject breaks its pool entry.** You get a
  `Pool entry 'X' matches no minigame` error at the first red light, and that minigame never plays.
- The fix when you must rename: fill in the `minigameId` field *first* to pin the id, then rename
  freely.
- Duplicate ids under one character are an error — the join becomes ambiguous and only the first
  wins.

## Adding a minigame

1. Subclass `Minigame`. Override `StartMinigame`/`CloseMinigame` calling `base` first; call the
   protected `CompleteMinigame()` on success and `FailMinigame()` on failure.
2. Build the UI as a child of the character's `MinigameCanvas`, attach the component, leave the
   GameObject **deactivated**.
3. Add an entry to that character's `CharacterMinigamePool` asset with a matching id and a
   `baseWeight`.

A minigame with no pool entry logs a warning and simply never gets selected — that's the expected
signal if yours doesn't show up.

## Gotchas

- `StartMinigame` logs an error naming the **disabled ancestor** if the object isn't active in the
  hierarchy. That error means the canvas (or a parent) is off — objects instantiated underneath
  would never receive `Awake`, which otherwise surfaces as a confusing `NullReferenceException`
  deep inside your subclass.
- Weights are reset on `Start` from `MinigamePoolConfig.InitialWeight`, so values serialized into a
  scene are ignored on purpose. Resolved per-character entries keep their authored `baseWeight`
  instead.
- `MinigameManager` must stay the only thing that calls `StartMinigame`/`CloseMinigame`.
  `CameraManager` fires an `onArrived` callback into it — do not give the camera a direct
  dependency on minigames.
- Grace period: if Green Light starts while a minigame is open, `RedLightMinigameManager` waits
  `GracePeriodDuration` and then force-closes it. Anything you allocate in `StartMinigame` must be
  cleaned up in `CloseMinigame`, because the close can arrive uninvited.
