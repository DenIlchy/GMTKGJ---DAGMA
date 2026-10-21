# Assets/Scripts/Managers — scene singletons

| Type | Role |
| :--- | :--- |
| `GameSys` | Red/Green state machine. The event source everything else listens to. |
| `DAGMAPenaltyController` | Shoot → pushback → stun, as one sequence with a completion callback. |
| `RedLightMinigameManager` | Orchestrates red light: penalty, pool resolution, selection, grace period. |
| `FinishLineTrigger` | Physics trigger → `GameSys.ReportFinished`. Not a singleton. |

## `GameSys` is event-source-only

It raises events and **never calls another manager**. Everything downstream subscribes:

| Event | Subscribers |
| :--- | :--- |
| `OnStateChanged` | `OpponentMovement`, `WatcherVisuals`, `GamePhaseAudio`, `DestroyAfterIntro` |
| `OnGreenLightStarted` | `RedLightMinigameManager`, `GamePhaseAudio` |
| `OnRedLightWarningStarted` | `GamePhaseAudio` |
| `OnRedLightStarted` | `RedLightMinigameManager` |
| `OnPenaltyFeedbackStarted` | `RedLightMinigameManager`, `WatcherVisuals`, `GameUI` |
| `OnPenaltyApplied` | `GameUI` |
| `OnPhaseTimerUpdated` | `GameUI` |
| `OnRaceEnded` | `RedLightMinigameManager` |
| `OnVictory`, `OnGameOver` | **nobody** — see below |

`OnVictory` / `OnGameOver` are raised but have zero subscribers: `GameUI` polls `CurrentState` in
`Update` for the result panel instead of subscribing. They work if you wire them up; just don't
assume existing behaviour flows through them. (`GameUI` is the one view that polls rather than
subscribes — worth fixing if you're in there anyway.)

To make something happen on a phase change, **subscribe** — do not add an outbound call to
`GameSys`. The single deliberate exception is a policy input: `MovementGate` receives a `Func<bool>`
reading `MinigameManager.Instance.IsMinigameActive`, flagged in-code for injection later.

Phase ordering detail that surprises people: the speed check runs at the **end of the warning**, and
the penalty is fully resolved *before* `SetState(RedLight)` / `OnRedLightStarted`. So by the time a
red-light subscriber runs, violators have already been pushed back.

## Singleton lifecycle — the deferred-`Destroy` trap

`Destroy(gameObject)` in `Awake` is deferred to the end of the frame, so **the duplicate still runs
`Start()`**. Every `Start()` here needs:

```csharp
if (Instance != this) return;
```

before touching anything `Awake` built, or the doomed duplicate null-refs and/or starts a second
game loop. `GameSys` goes further and builds `startGate` *before* the duplicate check, because the
duplicate's `Start()` would otherwise dereference it.

`GameSys.Instance` also has a fake-null guard in its getter — don't "simplify" it to an
auto-property.

## `RedLightMinigameManager.ResolvePool`

Resolves the player's per-character minigame pool lazily, on the first red light (not in `Start`:
the player may not have registered yet, and characters can be spawned at runtime).

- **Retries on failure by design.** Cheap, because the subtree walk only runs once a
  `CharacterMinigames` is found.
- **Two independent log latches**, deliberately not merged: `notReadyWarned` for the transient
  "player isn't there yet" case, `poolDiagnosticsReported` for authoring errors (unmatched id,
  duplicate id, minigame with no pool entry). Sharing one latch would let a transient warning
  permanently mask a real authoring mistake.
- `CharacterMinigames` is looked up with `GetComponentInChildren<>(true)`, **not** `GetComponent` —
  in the character prefabs it sits on a child node while `PlayerMovement` is on the root.
- The serialized `minigamePool` list is read-only authored input after `Awake`; the resolved pool is
  never written back into it.

## `DAGMAPenaltyController`

Owns pushback **and** stun as one unit — `GameSys` no longer applies pushback. It only waits out
`StunDelay` when the *player* was among the violators, so an AI-only violation doesn't stall the
round.

`OnDAGMAShoot` is a `UnityEvent` for Inspector wiring. **It currently has zero persistent calls in
`SysUpgrade.unity`** — if you expect a shot SFX/VFX from the penalty sequence, it is not wired up
there yet.
