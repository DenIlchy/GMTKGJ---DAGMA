<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: GMTKGJ---DAGMA
- Unity version: Unity 6000.3.20f1
- Active game object:
  - Name: ###########################
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

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

## Phase 1 status (ScriptableObject data layer)

- Config SO types live in `Assets/Scripts/Data/`; authored assets in `Assets/GameData/`.
- Components read tuning via `Config != null ? Config.X : legacyX`. Legacy inspector fields are
  retained and grouped under `... (Legacy)` headers; they are used only while the config slot is empty.
- Assigning a config asset in the Inspector makes it authoritative for that component.