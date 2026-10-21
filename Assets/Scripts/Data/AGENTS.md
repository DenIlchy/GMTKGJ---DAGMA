# Assets/Scripts/Data — ScriptableObject config layer

Config **types** live here. Authored config **assets** live in `Assets/GameData/`.

| Type | Consumed by | Asset(s) |
| :--- | :--- | :--- |
| `PhaseTimingConfig` | `GameSys` | `PhaseTiming_Default` |
| `RaceRulesConfig` | `GameSys` | `RaceRules_Default` |
| `PenaltyConfig` | `DAGMAPenaltyController` | `Penalty_Default` |
| `PlayerMovementConfig` | `PlayerMovement` | `PlayerMovement_Default` |
| `OpponentProfileConfig` | `OpponentMovement` | `AI_Standard`, `AI_Risky` |
| `CameraTransitionConfig` | `CameraManager` | `CameraTransition_Default` |
| `UIThemeConfig` | `GameUI`, `RedLightMinigameManager` | `UITheme_Default` |
| `MinigamePoolConfig` | `RedLightMinigameManager` | `MinigamePool_Default` |
| `CharacterMinigamePool` | per character, via `CharacterMinigames` | `Characters/MinigamePool_Microwave` |

`OpponentProfileConfig` is the one type that is deliberately authored more than once — it's a
per-opponent profile, not a global default.

## The config / legacy pattern

Every consuming component keeps its original serialized fields and reads through a property:

```csharp
[Header("Config (preferred) — overrides all Legacy values below")]
[SerializeField] private PenaltyConfig penaltyConfig;

[Header("Penalty Settings (Legacy)")]
public float stunDelay = 2.0f;

private float StunDelay => penaltyConfig != null ? penaltyConfig.StunDelay : stunDelay;
```

Rules:

- **Read tuning only through the `PascalCase` property**, never the backing field. A direct field
  read silently ignores the assigned config asset.
- **Do not delete the legacy fields.** They are the fallback while a config slot is empty, which is
  what makes assigning configs incremental rather than a flag day.
- An assigned config asset is authoritative for that component — all of it, not field by field.

## Adding a new config

1. New type here, `[CreateAssetMenu(menuName = "DAGMA/YourConfig")]`, `[SerializeField] private`
   fields with expression-bodied `public` getters.
2. Create the asset via the `DAGMA/` menu into `Assets/GameData/`, named `Your_Default.asset`.
3. In the consumer: add the `[SerializeField]` slot under a `Config (preferred)` header, move the
   existing fields under a `... (Legacy)` header, add the ternary properties, and switch all reads.
4. Assign the asset in the Inspector — on the prefab where one exists, not just the scene instance.

## Gotchas

- These are **assets, not instances**: values changed at runtime in play mode persist to the asset
  in the editor. Don't mutate a config from gameplay code.
- Tooltips on the config fields are what designers see. Keep them, and keep them accurate —
  `PhaseTimingConfig.redLightTimerStartDelay` in particular documents a real coupling to
  `CameraTransitionConfig` (it must be ≥ `stepDuration * 2 + uiPopupDelay`, or the Red Light timer
  starts counting before the camera has settled).
- `CharacterMinigamePool` is not a tuning config — it's content. See
  `Assets/Scripts/Minigames/AGENTS.md`.
