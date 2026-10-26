# DAGMA round telemetry — integration contract

> **Status: SPEC ONLY — there is no telemetry code in this project yet.**
> Nothing described below is implemented. This file is the agreed design for a future round
> telemetry system, written up front so the schema and the backend contract are settled before
> anything is built. Do not read it as a description of existing behaviour.
> When the implementation lands, this header must be replaced and an `AGENTS.md` added alongside it.

## What this measures

One record per **round** (a single playthrough of the race), describing how the player played:

- how hard they mashed the alternating keys during each Green Light, and whether that phase ended
  in a penalty;
- which minigames they were given, whether they solved each one, and how long it took;
- how many Red Lights happened;
- where they placed at the end.

The record is accumulated during the round and sent **once, when the race ends** — the player
crossing the finish line, or an opponent crossing it first.

## Transport shape

One record = one row = one `POST`. There is deliberately no batching and no nesting of multiple
players into a single document:

- The game is headed for a multiplayer format where **each client posts its own row** for the round
  it played. A per-player row is already the right shape for that; a whole-match document is not.
- Unity's `JsonUtility` (the only JSON serializer available here — Newtonsoft is not in
  `Packages/manifest.json`) **cannot serialize a `Dictionary`**. Rows are therefore emitted as
  JSON Lines (one self-contained JSON object per line) locally, and as a single object per request
  over HTTP.

Rows from the same race are joined after the fact on `matchId`.

## Schema

### `RoundRecord` — the row

| Field | Type | Meaning |
| :--- | :--- | :--- |
| `characterId` | int | **Assigned by the receiver, not the client.** The server sets it to `n+1` over the existing rows (an auto-increment primary key). The client always sends `0`. |
| `runId` | string | Client-generated GUID. The stable identity of this round and the **idempotency key** for the POST. |
| `matchId` | string | Shared by every player in the same race. Equal to `runId` while the game is single-player. |
| `characterKey` | string | Which character was played (prefab / pool name, e.g. `Microwave`). Diagnostic — not an id. |
| `redLightPhases` | int | How many Red Light phases occurred during the round. |
| `finishPlace` | int | 1-based place. See the caveat under *Reading the data*. |
| `racers` | int | How many movers were in the race when it ended. Gives `finishPlace` a denominator. |
| `won` | bool | True if the player reached the finish line first. |
| `roundDurationSeconds` | float | Wall-clock length of the round, excluding paused time. |
| `greenPhases` | `GreenLightPhaseRecord[]` | One entry per Green Light, in order. |
| `minigames` | `MinigameAttemptRecord[]` | One entry per minigame the player was shown, in order. |

### `GreenLightPhaseRecord` — one Green Light

| Field | Type | Meaning |
| :--- | :--- | :--- |
| `phaseIndex` | int | 1-based, in play order. |
| `leftTaps` | int | Presses of the left key (**Q** by default). |
| `rightTaps` | int | Presses of the right key (**E** by default). |
| `alternatingTaps` | int | Taps that correctly alternated and added momentum. |
| `wrongTaps` | int | Same-key-twice taps, which cost speed. |
| `gotPenalty` | bool | The player was among the violators caught at the end of this phase. |
| `durationSeconds` | float | Length of the phase. |

Notes:

- `leftTaps + rightTaps == alternatingTaps + wrongTaps`. The two pairs slice the same taps two
  different ways: by *which* key, and by whether the tap was *correct*. A mismatch means a bug.
- The keys are configurable (`PlayerMovement.LeftKey` / `RightKey`); Q and E are only the defaults,
  which is why the fields are named left/right rather than q/e.
- The Red Light **warning** is the tail end of the Green Light, not a phase of its own, so taps
  during the warning belong to the Green Light phase they interrupted.
- The final phase may be cut short by the player finishing mid-Green-Light.

### `MinigameAttemptRecord` — one minigame

| Field | Type | Meaning |
| :--- | :--- | :--- |
| `redLightIndex` | int | Which Red Light this minigame belonged to. |
| `minigameId` | string | The minigame's stable id (`Minigame.MinigameId`), e.g. `Minigame_Keypad`. |
| `outcome` | int enum | See `MinigameOutcome` below. |
| `durationSeconds` | float | From the minigame UI opening to it closing, excluding paused time. |

### `MinigameOutcome`

| Value | Name | Meaning |
| :--- | :--- | :--- |
| `0` | `Solved` | The player completed the minigame. |
| `1` | `GraceExpired` | The player was still in it when the grace period after Green Light ran out. |
| `2` | `RaceEnded` | The race ended while the minigame was open. |

`JsonUtility` serializes enums as their integer value, so the wire format is `"outcome": 1`.

This is an enum rather than a `won` boolean on purpose — `GraceExpired` and `RaceEnded` are both
"not solved", but only the first is a signal about the player. See *Reading the data*.

## Example row

```json
{
  "characterId": 0,
  "runId": "8f1c2b4e-5d3a-4c11-9f20-2b7a6e0d93aa",
  "matchId": "8f1c2b4e-5d3a-4c11-9f20-2b7a6e0d93aa",
  "characterKey": "Microwave",
  "redLightPhases": 3,
  "finishPlace": 2,
  "racers": 4,
  "won": false,
  "roundDurationSeconds": 48.2,
  "greenPhases": [
    { "phaseIndex": 1, "leftTaps": 14, "rightTaps": 13, "alternatingTaps": 25, "wrongTaps": 2, "gotPenalty": false, "durationSeconds": 7.4 },
    { "phaseIndex": 2, "leftTaps": 9,  "rightTaps": 11, "alternatingTaps": 16, "wrongTaps": 4, "gotPenalty": true,  "durationSeconds": 6.1 },
    { "phaseIndex": 3, "leftTaps": 18, "rightTaps": 18, "alternatingTaps": 35, "wrongTaps": 1, "gotPenalty": false, "durationSeconds": 9.8 }
  ],
  "minigames": [
    { "redLightIndex": 1, "minigameId": "Minigame_Keypad",     "outcome": 0, "durationSeconds": 3.9 },
    { "redLightIndex": 2, "minigameId": "SimonSaysMinigame",   "outcome": 1, "durationSeconds": 6.5 },
    { "redLightIndex": 3, "minigameId": "TemperatureMinigame", "outcome": 2, "durationSeconds": 2.2 }
  ]
}
```

## Endpoint contract

```
POST /v1/rounds
Content-Type: application/json
```

Body: exactly one `RoundRecord` as above, with `characterId` set to `0`.

Response `201 Created`:

```json
{ "characterId": 1423, "runId": "8f1c2b4e-5d3a-4c11-9f20-2b7a6e0d93aa" }
```

Requirements on the receiver:

- **Assign `characterId` as `n+1`** over the rows already stored, and return it.
- **Be idempotent on `runId`.** A repeated POST of the same `runId` must return the
  originally-assigned `characterId` with `200 OK` rather than inserting a second row. The client
  may retry, and must never produce a duplicate.
- Reject a row whose `greenPhases` tap totals are inconsistent (see the invariant above) rather
  than storing data that cannot be interpreted.

## Storage sketch

```sql
CREATE TABLE rounds (
    character_id           INTEGER PRIMARY KEY,   -- n+1, assigned on insert
    run_id                 TEXT    NOT NULL UNIQUE,
    match_id               TEXT    NOT NULL,
    character_key          TEXT    NOT NULL,
    red_light_phases       INTEGER NOT NULL,
    finish_place           INTEGER NOT NULL,
    racers                 INTEGER NOT NULL,
    won                    BOOLEAN NOT NULL,
    round_duration_seconds REAL    NOT NULL,
    received_at            TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE green_phases (
    character_id     INTEGER NOT NULL REFERENCES rounds(character_id) ON DELETE CASCADE,
    phase_index      INTEGER NOT NULL,
    left_taps        INTEGER NOT NULL,
    right_taps       INTEGER NOT NULL,
    alternating_taps INTEGER NOT NULL,
    wrong_taps       INTEGER NOT NULL,
    got_penalty      BOOLEAN NOT NULL,
    duration_seconds REAL    NOT NULL,
    PRIMARY KEY (character_id, phase_index)
);

CREATE TABLE minigame_attempts (
    character_id     INTEGER NOT NULL REFERENCES rounds(character_id) ON DELETE CASCADE,
    red_light_index  INTEGER NOT NULL,
    minigame_id      TEXT    NOT NULL,
    outcome          INTEGER NOT NULL,  -- 0 Solved, 1 GraceExpired, 2 RaceEnded
    duration_seconds REAL    NOT NULL,
    PRIMARY KEY (character_id, red_light_index)
);

CREATE INDEX idx_rounds_match ON rounds(match_id);
```

## Reading the data

Two traps, both of which come from how the game actually works:

**Minigame win rate must exclude `RaceEnded`.** A minigame cut off because the round ended says
nothing about the player's skill at it. The correct denominator is solved plus grace-expired:

```sql
SELECT minigame_id,
       COUNT(*) FILTER (WHERE outcome = 0)::float / COUNT(*) AS win_rate
FROM   minigame_attempts
WHERE  outcome IN (0, 1)
GROUP  BY minigame_id;
```

**`finishPlace` is a position snapshot, not a finish order.** The race stops the instant the first
racer crosses the line, so only one racer ever actually finishes. Everyone else's place is their
standing along the track at that moment. It is a fair measure of how far behind the player was; it
is *not* a record of who crossed the line in what order, because nobody else crossed it.

## Local capture before a backend exists

Rows are written to `Application.persistentDataPath/dagma_telemetry.jsonl`, one JSON object per
line, and logged to the console with a `[Telemetry]` prefix.

The local file emulates the server's id rule by stamping `characterId = line count + 1`. **These
local ids are not the server's ids** — they renumber the moment a real backend starts assigning
them. `runId` is the identity that survives; join on that, never on `characterId`, when reconciling
local captures with server rows.

The file is written **before** the HTTP POST is attempted, so a failed or disabled request still
leaves durable local data.

## Known gaps

- **No retry queue.** A POST dropped while offline is lost from the server's point of view; the
  local file still has the row, but nothing re-sends it on the next launch.
- **Abandoned rounds send nothing.** Quitting to the menu or restarting mid-race discards the
  partially-accumulated record, by design — sending happens at the finish line only.
- **`characterId` is only unique per sink**, as described above.

## Implementation notes

Constraints that are easy to trip over when this is finally built:

- **`LangVersion` is 9.0** (see `CompileCheckAll.csproj`). `record struct` and
  `readonly record struct` are C# 10 and unavailable.
- **`JsonUtility` serializes fields only — it ignores properties entirely.** A `record` with
  `init` properties serializes to `{}`. Every type on the wire must be a `[Serializable]` class
  with public fields.
- **A mutable `struct` in a `List<T>` does not work** — the indexer returns a copy, so
  `list[i].leftTaps++` will not compile. The per-phase and per-minigame records are mutated as
  they accumulate, so they must be classes. A `readonly struct` is appropriate for the per-tap
  event payload, which is never stored.
- **Distinguishing `GraceExpired` from `RaceEnded` must not rely on `GameSys.OnRaceEnded`.**
  `RedLightMinigameManager.HandleRaceEnded` subscribes to that same event and is what force-closes
  the minigame, so invocation order would depend on `Start()` order. Read a race-over flag instead:
  `GameSys` sets `gameEnded` at the top of `EndGame`, before any event fires, so exposing it as a
  read-only accessor gives an order-independent answer at close time.
- Durations should come from `Time.time`, not `Time.unscaledTime`, so paused time is excluded.
  Anything that *waits* after the race ends must use `WaitForSecondsRealtime`, because
  `TimeControl.Freeze()` has already set `timeScale` to 0.
