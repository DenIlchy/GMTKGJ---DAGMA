<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: GMTKGJ---DAGMA
- Unity version: Unity 6000.3.20f1
- Active game object:
  - Name: CinemachineCamera
  - Tag: Untagged
  - Layer: Default
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

## Compile gate

Quick compile check without launching Unity:

```
cd /c/GameDev/GMTKGJ---DAGMA && dotnet build CompileCheck.csproj -v q --nologo 2>&1 | tail -20
```

Caveat: the pipe masks the dotnet exit code — judge success by the output text (`Build succeeded` with `0 Error(s)`, no `error CS` lines), not by `$?`.

## Phase 1 status (ScriptableObject data layer)

- Config SO types live in `Assets/Scripts/Data/`; authored assets in `Assets/GameData/`.
- Components read tuning via `Config != null ? Config.X : legacyX`. Legacy inspector fields are
  retained and grouped under `... (Legacy)` headers; they are used only while the config slot is empty.
- Assigning a config asset in the Inspector makes it authoritative for that component.
