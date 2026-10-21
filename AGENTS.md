<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: GMTKGJ---DAGMA
- Unity version: Unity 6000.3.20f1
- Active game object:
  - Name: GameManager
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

# DAGMA — agent guide

Red Light / Green Light racing game. Players build momentum by alternating taps; a watcher entity
("DAGMA") catches movers during Red Light, applies a penalty, and drops the player into a 2D UI
minigame.

Current play scene: `Assets/Scenes/SysUpgrade.unity`.

## Compile gate

Quick compile check without launching Unity:

```
cd /c/GameDev/GMTKGJ---DAGMA && dotnet build CompileCheckAll.csproj -v q --nologo 2>&1 | tail -20
```

Caveat: the pipe masks the dotnet exit code — judge success by the output text (`Build succeeded` with `0 Error(s)`, no `error CS` lines), not by `$?`.

Both `CompileCheck*.csproj` files are generated, gitignored copies of Unity's own csprojs with the
explicit `<Compile Include>` list swapped for a glob, so they pick up newly added files (Unity's
generated csprojs list files individually and go stale). Regenerate them if package references change.

- `CompileCheckAll.csproj` — derived from `Assembly-CSharp-Editor.csproj`; globs **all** of
  `Assets/**/*.cs` including `Editor/` folders, with `UnityEditor` available. Prefer this one.
- `CompileCheck.csproj` — runtime only, excludes `Editor/`. Kept for a faster runtime-only check,
  but it will silently skip editor scripts.

There are no automated tests. The compile gate plus a manual play-through is the whole safety net,
so be conservative with changes you cannot verify.

## Where things live

| Path | What's in it | Has its own AGENTS.md |
| :--- | :--- | :--- |
| `Assets/Scripts/Core/` | Plain C# game rules — no MonoBehaviours | yes |
| `Assets/Scripts/Data/` | `ScriptableObject` config types | yes |
| `Assets/Scripts/Managers/` | Scene singletons: `GameSys`, penalty, red-light orchestration | yes |
| `Assets/Scripts/Minigames/` | `Minigame` base + the five implementations + selection | yes |
| `Assets/Scripts/Movements/` | `IMovable`, `PlayerMovement`, `OpponentMovement`, `WatcherVisuals` | no |
| `Assets/Scripts/Camera/` | `CameraManager` (Cinemachine arc sweep), picture-in-picture | no |
| `Assets/Scripts/UI/` | `GameUI` (HUD, pause, result panel) | no |
| `Assets/Scripts/Audio/` | `GamePhaseAudio`, `SoundManager`, `GunAudio` | no |
| `Assets/Scripts/Editor/` | `CharacterPrefabMigration` — one-time prefab split tool | no |
| `Assets/GameData/` | Authored config assets (`*_Default.asset`, per-character pools) | no |
| `Assets/Prefabs/Characters/` | Character prefabs: `*_Base` logic + `CV_*` visuals + composed | no |

Read the nearest `AGENTS.md` to the file you are editing — the closest one wins.

## Architecture invariants

Break these only deliberately, and update this file when you do.

1. **`GameSys` is event-source-only.** It raises events (`OnStateChanged`, `OnRedLightStarted`,
   `OnPenaltyFeedbackStarted`, …) and never calls another manager. To make something happen on a
   phase change, subscribe to an event — do not add an outbound call to `GameSys`.
   The one deliberate exception is a *policy input*: `MovementGate` is handed a `Func<bool>` that
   reads `MinigameManager.Instance.IsMinigameActive`.

2. **`MovementGate.CanMove` is the single owner of movement permission.** Gameplay code must not
   call `IMovable.SetMovementBlocked` directly; call `GameSys.ReevaluateMovement()` and let the gate
   decide. See `Assets/Scripts/Core/AGENTS.md`.

3. **`Assets/Scripts/Core/` contains no MonoBehaviours** and no scene references. Dependencies come
   in through constructors (`Func<>` delegates where it needs to read live state).

4. **`CameraManager` knows nothing about minigames.** `SwitchToMinigameCamera(Action onArrived)`
   takes a callback. The only `MinigameManager` reference left is a debug hotkey fenced behind
   `#if UNITY_EDITOR` — keep it that way.

5. **Tuning lives in ScriptableObjects, not in the Inspector.** Every tuned component keeps its old
   serialized fields under a `... (Legacy)` header and reads through a property that prefers the
   config asset. See `Assets/Scripts/Data/AGENTS.md`.

6. **Minigames are matched to characters by string id**, resolved at runtime. See
   `Assets/Scripts/Minigames/AGENTS.md`.

## Conventions

- Singletons expose `public static X Instance { get; private set; }` and `Destroy(gameObject)` on a
  duplicate in `Awake`. **`Destroy` is deferred to the end of the frame, so the doomed duplicate
  still runs `Start()`.** Every such `Start()` needs an `if (Instance != this) return;` guard before
  touching anything `Awake` built. This has bitten this codebase more than once.
- Log with a `[ClassName]` prefix: `Debug.Log("[RedLightMinigameManager] ...")`.
- Comments explain **why**, not what. The existing "why" comments (deferred-`Destroy` guards, the
  two-latch logging in `ResolvePool`, the fake-null note in the migration tool) are load-bearing —
  do not strip them.
- Unity fake-null: a destroyed/unassigned `UnityEngine.Object` is not `null` under `??` or
  `is null`. Always compare with `== null` / `!= null`.

## Keeping these docs honest

These files are read by every teammate's coding agent, and **a stale doc is worse than no doc** —
an agent cannot tell a wrong statement from a right one. Two rules:

- **Document pointers and invariants, not values.** If an agent can grep it in five seconds (tuning
  numbers, the scene hierarchy, the list of minigames), leave it out — it will rot. Write down the
  things grep cannot tell you: why a thing is shaped the way it is, and what not to do.
- **Use exact type names in prose** (`MovementGate`, not "the movement gate") so a grep for a symbol
  surfaces both the code and the doc that explains it.

`SYSTEMS_DOCUMENTATION.md` is **historical** — it describes the pre-2026-10-18 architecture and is
wrong about several systems. Do not trust it; see the header on that file.
