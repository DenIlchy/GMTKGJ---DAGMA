# Assets/Scripts/Core — game rules, no engine objects

Plain C# extracted out of `GameSys` so the rules are readable and unit-testable without a scene.

| Type | Responsibility |
| :--- | :--- |
| `MoverRegistry` | Owns the `IMovable` list; `FindPlayer()`; raises `OnMoverRegistered` |
| `MovementGate` | **Sole owner** of "may this mover move right now" |
| `PhaseTimer` | Countdown coroutine; raises `OnTick(remaining)` |
| `StartGate` | Wraps the start panel wait + its misconfiguration warnings |
| `ViolationDetector` | Static speed-threshold check over the registry |
| `TimeControl` | Static wrapper over `Time.timeScale` |

## Rules for this folder

- **No MonoBehaviours, no scene references, no singleton lookups.** Dependencies are passed into
  constructors. Where a type needs to read live state it takes a `Func<>` (see `MovementGate`'s
  `currentState` and `minigameActive`), so the caller owns the coupling, not this folder.
- The only Unity API used here is `Time`, `Mathf` and `Debug`. Keep it that way — it is what makes
  this folder cheap to reason about.

## `MovementGate` is the single owner of movement permission

`CanMove` is the one place the answer is computed:

```csharp
return !PhaseBlocked
    && isGreenLight
    && !mover.IsStunned
    && !(mover.IsPlayer && isMinigameActive);
```

Consequences:

- **Never call `IMovable.SetMovementBlocked` from gameplay code.** Call
  `GameSys.ReevaluateMovement()` and let the gate re-apply to everyone. A stun expiring, a minigame
  closing, and a phase change all go through this same path.
- A mover that registers *after* the loop already blocked everyone must not be left unblocked —
  `GameSys` handles this by applying the gate on `OnMoverRegistered`. Keep that wiring if you touch
  registration.
- Adding a new reason to block movement means adding a term to `CanMove`, not a new flag somewhere
  else.

## Gotchas

- **`TimeControl.Pause()` and `TimeControl.Freeze()` are different.** Both set `timeScale = 0`, but
  `Pause` sets `IsPaused = true` (pause menu open, resumable) and `Freeze` leaves it `false`
  (race over, not resumable). `GameUI.TogglePause` keys off `IsPaused`, so using the wrong one makes
  the pause menu misbehave after a win/loss.
- `TimeControl` guards state that is *already* global (`Time.timeScale`); it is not an excuse to add
  new global state here.
- **`PhaseTimer.Run(duration, reportOffset, shouldAbort)`** — `reportOffset` exists so the Green
  Light countdown can report time remaining until the *end of green* while only running the pre-warning
  leg. Dropping it would make the HUD count down to the warning instead.
- `shouldAbort` must be honoured by every caller (`() => gameEnded`), otherwise a finished race keeps
  ticking phases.
