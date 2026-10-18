using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// One-time migration: splits Char.prefab / Rival.prefab into a logic-base + character-visual
// prefab pair per character, under Assets/Prefabs/Characters/.
//
// Incremental and resumable: it never modifies or deletes anything that already exists
// (pre-existing targets are ADOPTED and validated instead), writes each new asset as it goes,
// and can be re-run safely to fill in whatever is missing after a failure.
// Run "Tools/DAGMA/Character Prefabs/Dry Run" first — it previews exactly what Generate does.
public static class CharacterPrefabMigration
{
    // ---- Inputs (guids are stable references into the project, not paths) ----
    private const string CharPrefabGuid = "01d2422167e8c294f8d241beebe1e2e2";
    private const string RivalPrefabGuid = "bcd2d6f6acbc7954c938114754bdb69d";
    private const string PlayerMovementConfigGuid = "c0c6507e3018713b63590068187a8827";
    private const string MicrowaveModelGuid = "017ebd7899083e84288c8d04d3df76ac";
    private const string MicrowaveControllerGuid = "c6178f67004c5e64f96c350b12869bef";

    // ---- Outputs ----
    private const string BaseDir = "Assets/Prefabs/Characters";
    private const string PlayerBasePath = BaseDir + "/PlayerCharacter_Base.prefab";
    private const string RivalBasePath = BaseDir + "/Rival_Base.prefab";
    private const string CVMicrowavePath = BaseDir + "/Microwave/CV_Microwave.prefab";
    private const string PlayerMicrowavePath = BaseDir + "/Microwave/PlayerCharacter_Microwave.prefab";
    private const string RivalMicrowavePath = BaseDir + "/Microwave/Rival_Microwave.prefab";

    // Baked offsets inside Char.prefab — the reason normalisation is needed.
    private static readonly Vector3 CharColliderCenter = new Vector3(4.5f, 1.5f, -9f);
    private static readonly Vector3 CharModelChildOffset = new Vector3(4.9f, 0f, -9.2f);
    private static readonly Vector3 NormalizedColliderCenter = new Vector3(0f, 1.5f, 0f);

    [MenuItem("Tools/DAGMA/Character Prefabs/Dry Run (report only)")]
    private static void DryRun()
    {
        var problems = new List<string>();
        Inputs inputs = ResolveInputs(problems);

        var report = new StringBuilder();
        report.AppendLine("[CharacterPrefabMigration] DRY RUN — nothing will be written.");
        ReportInputProblems(report, problems);

        if (problems.Count == 0)
        {
            report.AppendLine($"Inputs OK: Char='{AssetDatabase.GetAssetPath(inputs.charPrefab)}', " +
                              $"Rival='{AssetDatabase.GetAssetPath(inputs.rivalPrefab)}', " +
                              $"PlayerMovementConfig='{AssetDatabase.GetAssetPath(inputs.playerMovementConfig)}', " +
                              $"model='{AssetDatabase.GetAssetPath(inputs.microwaveModel)}', " +
                              $"controller='{AssetDatabase.GetAssetPath(inputs.microwaveController)}'.");
            AppendOffsetReport(report);
        }

        report.AppendLine("Plan per target:");
        foreach (string path in new[] { PlayerBasePath, RivalBasePath, CVMicrowavePath, PlayerMicrowavePath, RivalMicrowavePath })
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                report.AppendLine($"  WOULD CREATE  {path}");
            }
            else
            {
                report.AppendLine($"  WOULD ADOPT (exists)  {path}");
                foreach (string warning in ValidateAdopted(path, existing))
                    report.AppendLine($"    WARNING: {warning}");
            }
        }

        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/DAGMA/Character Prefabs/Generate")]
    private static void Generate()
    {
        var problems = new List<string>();
        Inputs inputs = ResolveInputs(problems);
        if (problems.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine("[CharacterPrefabMigration] ABORTED — missing inputs, nothing was attempted.");
            ReportInputProblems(report, problems);
            Debug.LogError(report.ToString());
            return;
        }

        var created = new List<string>();
        var adopted = new List<string>();
        var warnings = new List<string>();
        string failure = null;

        // Track every temporary instantiated GameObject so failure paths can clean up.
        var temporaries = new List<GameObject>();
        try
        {
            created.AddRange(AdoptOrCreate(PlayerBasePath, adopted, warnings,
                () => SavePrefab(CreatePlayerBase(inputs, temporaries), PlayerBasePath)));

            created.AddRange(AdoptOrCreate(RivalBasePath, adopted, warnings,
                () => SavePrefab(CreateRivalBase(temporaries), RivalBasePath)));

            created.AddRange(AdoptOrCreate(CVMicrowavePath, adopted, warnings,
                () => SavePrefab(CreateCVMicrowave(inputs, temporaries), CVMicrowavePath)));

            created.AddRange(AdoptOrCreate(PlayerMicrowavePath, adopted, warnings,
                () => SavePrefab(CreateVariant(PlayerBasePath, CVMicrowavePath, "PlayerCharacter_Microwave", temporaries), PlayerMicrowavePath)));

            created.AddRange(AdoptOrCreate(RivalMicrowavePath, adopted, warnings,
                () => SavePrefab(CreateVariant(RivalBasePath, CVMicrowavePath, "Rival_Microwave", temporaries), RivalMicrowavePath)));
        }
        catch (System.Exception e)
        {
            failure = e.Message;
        }
        finally
        {
            foreach (var temp in temporaries)
            {
                if (temp != null)
                    Object.DestroyImmediate(temp);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string finalReport = BuildFinalReport(created, adopted, warnings, failure);
        if (failure != null)
            Debug.LogError(finalReport);
        else
            Debug.Log(finalReport);
    }

    // Returns the path in `created` if it had to be built, or records it in `adopted` —
    // adopted assets are validated and their discrepancies logged as warnings, never thrown.
    private static List<string> AdoptOrCreate(string path, List<string> adopted, List<string> warnings, System.Func<string> create)
    {
        var result = new List<string>();
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            adopted.Add(path);
            foreach (string warning in ValidateAdopted(path, existing))
                warnings.Add($"{path}: {warning}");
            return result;
        }

        result.Add(create());
        return result;
    }

    private static List<string> ValidateAdopted(string path, GameObject asset)
    {
        var warnings = new List<string>();
        string name = System.IO.Path.GetFileNameWithoutExtension(path);

        switch (name)
        {
            case "PlayerCharacter_Base":
                if (asset.GetComponent<PlayerMovement>() == null)
                    warnings.Add("no PlayerMovement component.");
                var movement = asset.GetComponent<PlayerMovement>();
                if (movement != null)
                {
                    var so = new SerializedObject(movement);
                    WarnIfMissingFields(so, warnings, "movementConfig", "animator");
                    var configProp = so.FindProperty("movementConfig");
                    if (configProp != null && configProp.objectReferenceValue == null)
                        warnings.Add("movementConfig is null — expected PlayerMovement_Default.");
                    if (so.FindProperty("animator")?.objectReferenceValue != null)
                        warnings.Add("animator is assigned — should be null so variants auto-find their own child Animator.");
                }
                ValidateSingleCollider(asset, warnings);
                if (asset.layer != 6)
                    warnings.Add($"layer is {asset.layer}, expected 6.");
                break;

            case "Rival_Base":
                if (asset.GetComponent<OpponentMovement>() == null)
                    warnings.Add("no OpponentMovement component.");
                var opponent = asset.GetComponent<OpponentMovement>();
                if (opponent != null)
                {
                    var so = new SerializedObject(opponent);
                    WarnIfMissingFields(so, warnings, "profile");
                    if (so.FindProperty("profile")?.objectReferenceValue != null)
                        warnings.Add("profile is assigned — should be null; the scene assigns AI_Standard/AI_Risky per instance.");
                }
                ValidateSingleCollider(asset, warnings);
                if (asset.layer != 7)
                    warnings.Add($"layer is {asset.layer}, expected 7.");
                break;

            case "CV_Microwave":
                var animator = asset.GetComponent<Animator>();
                if (animator == null)
                    warnings.Add("no Animator on root.");
                else if (animator.runtimeAnimatorController == null)
                    warnings.Add("Animator has no runtimeAnimatorController.");
                break;

            case "PlayerCharacter_Microwave":
            case "Rival_Microwave":
                if (PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Variant)
                    warnings.Add($"PrefabAssetType is {PrefabUtility.GetPrefabAssetType(asset)}, expected Variant.");
                break;
        }
        return warnings;
    }

    // The [RequireComponent] duplicate trap: exactly one Collider is expected, warn on 0 or >1.
    private static void ValidateSingleCollider(GameObject asset, List<string> warnings)
    {
        int colliderCount = asset.GetComponents<Collider>().Length;
        if (colliderCount != 1)
            warnings.Add($"has {colliderCount} colliders, expected exactly 1.");
    }

    // ------------------------------------------------------------------ inputs

    private class Inputs
    {
        public GameObject charPrefab;
        public GameObject rivalPrefab;
        public PlayerMovementConfig playerMovementConfig;
        public GameObject microwaveModel;
        public RuntimeAnimatorController microwaveController;
    }

    private static Inputs ResolveInputs(List<string> problems)
    {
        var inputs = new Inputs();
        inputs.charPrefab = LoadAtGuid<GameObject>(CharPrefabGuid, "Char.prefab", problems);
        inputs.rivalPrefab = LoadAtGuid<GameObject>(RivalPrefabGuid, "Rival.prefab", problems);
        inputs.playerMovementConfig = LoadAtGuid<PlayerMovementConfig>(PlayerMovementConfigGuid, "PlayerMovement_Default.asset", problems);
        inputs.microwaveModel = LoadAtGuid<GameObject>(MicrowaveModelGuid, "microwave - Idle.blend", problems);
        inputs.microwaveController = LoadAtGuid<RuntimeAnimatorController>(MicrowaveControllerGuid, "MicrowaveController.controller", problems);
        return inputs;
    }

    private static T LoadAtGuid<T>(string guid, string label, List<string> problems) where T : Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            problems.Add($"GUID {guid} ({label}) resolves to no asset path.");
            return null;
        }
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            problems.Add($"Asset at '{path}' ({label}) failed to load as {typeof(T).Name}.");
        }
        return asset;
    }

    // Warns for each field name that no longer exists on the component — validation is
    // warn-only, so a renamed field becomes a warning rather than an abort.
    private static void WarnIfMissingFields(SerializedObject so, List<string> warnings, params string[] names)
    {
        foreach (string name in names)
        {
            if (so.FindProperty(name) == null)
                warnings.Add($"field '{name}' not found on {so.targetObject.GetType().Name} — was it renamed?");
        }
    }

    // Throws naming the field and component type instead of letting a renamed field surface
    // as a bare NullReferenceException — these names are coupled to classes being refactored.
    private static SerializedProperty Prop(SerializedObject so, string name)
    {
        SerializedProperty prop = so.FindProperty(name);
        if (prop == null)
            throw new System.InvalidOperationException($"[CharacterPrefabMigration] Field '{name}' not found on {so.targetObject.GetType().Name}.");
        return prop;
    }

    // ------------------------------------------------------------------ builders

    private static GameObject CreatePlayerBase(Inputs inputs, List<GameObject> temporaries)
    {
        var go = new GameObject("PlayerCharacter") { layer = 6 };
        temporaries.Add(go);

        PlayerMovement movement = go.AddComponent<PlayerMovement>();
        var so = new SerializedObject(movement);

        // movementConfig is wired; animator is deliberately left null — PlayerMovement.Awake
        // auto-finds the Animator in children, so each variant's nested visual supplies it and
        // no fragile cross-prefab reference is baked into every variant.
        Prop(so, "movementConfig").objectReferenceValue = inputs.playerMovementConfig;

        // Legacy fallbacks mirror the values harvested from Char.prefab so the prefab still
        // behaves if the config is ever cleared. intValue writes the real underlying Key value —
        // enumValueIndex is only an index into the name list and coincidentally matched.
        Prop(so, "leftKey").intValue = (int)UnityEngine.InputSystem.Key.Q;
        Prop(so, "rightKey").intValue = (int)UnityEngine.InputSystem.Key.E;
        Prop(so, "accelerationPerTap").floatValue = 0.5f;
        Prop(so, "maxSpeed").floatValue = 8f;
        Prop(so, "deceleration").floatValue = 2f;
        Prop(so, "wrongTapPenalty").floatValue = 0.5f;
        Prop(so, "speedParamName").stringValue = "Speed";
        so.ApplyModifiedPropertiesWithoutUndo();

        // Get-or-add: PlayerMovement's [RequireComponent(typeof(CapsuleCollider))] already
        // auto-added one — a second AddComponent would ship the prefab with two colliders.
        var capsule = go.GetComponent<CapsuleCollider>() ?? go.AddComponent<CapsuleCollider>();
        capsule.radius = 2f;
        capsule.height = 5f;
        capsule.direction = 1;
        capsule.center = NormalizedColliderCenter;
        return go;
    }

    private static GameObject CreateRivalBase(List<GameObject> temporaries)
    {
        var go = new GameObject("Rival") { layer = 7 };
        temporaries.Add(go);

        OpponentMovement movement = go.AddComponent<OpponentMovement>();
        var so = new SerializedObject(movement);
        // profile intentionally left null: the scene assigns AI_Standard / AI_Risky per instance.
        Prop(so, "accelerationPerStep").floatValue = 0.5f;
        Prop(so, "maxSpeed").floatValue = 8f;
        Prop(so, "deceleration").floatValue = 0.2f;
        Prop(so, "stepIntervalRange").vector2Value = new Vector2(0.2f, 0.5f);
        Prop(so, "riskyWarningStepChance").floatValue = 0.265f;
        Prop(so, "warningReactionDelayRange").vector2Value = new Vector2(0.5f, 1f);
        so.ApplyModifiedPropertiesWithoutUndo();

        var box = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        box.size = Vector3.one;
        box.center = Vector3.zero;
        return go;
    }

    private static GameObject CreateCVMicrowave(Inputs inputs, List<GameObject> temporaries)
    {
        // InstantiatePrefab keeps the link to the imported model, so SaveAsPrefabAsset below
        // produces a VARIANT of the model — future model reimports still propagate.
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(inputs.microwaveModel);
        temporaries.Add(instance);
        instance.name = "CV_Microwave";
        instance.transform.localPosition = Vector3.zero;

        // Get-or-add: the imported model root may already carry an Animator depending on
        // import settings (Generic rig) — a second one would be a real duplicate.
        var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = inputs.microwaveController;
        animator.applyRootMotion = false;
        // avatar intentionally left null, matching the original setup on Char.prefab.
        return instance;
    }

    private static GameObject CreateVariant(string basePrefabPath, string cvPrefabPath, string rootName, List<GameObject> temporaries)
    {
        GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        GameObject cvAsset = AssetDatabase.LoadAssetAtPath<GameObject>(cvPrefabPath);
        if (baseAsset == null || cvAsset == null)
            throw new System.InvalidOperationException($"[CharacterPrefabMigration] Cannot build '{rootName}': base or CV prefab failed to load.");

        // InstantiatePrefab (not LoadPrefabContents) keeps the nested prefab links intact, so
        // the saved asset is a proper Variant: base edits propagate, the CV stays a child instance.
        // Both instances are registered immediately so a throw never leaks one into the open scene.
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseAsset);
        temporaries.Add(instance);
        instance.name = rootName;

        var cv = (GameObject)PrefabUtility.InstantiatePrefab(cvAsset);
        temporaries.Add(cv);
        cv.transform.SetParent(instance.transform, false);
        cv.transform.localPosition = Vector3.zero;
        // Rotation is forced to identity because Char.prefab did the same to its model instance.
        // Scale is deliberately NOT touched: neither Char nor Rival overrode it, so the visual
        // keeps whatever root scale its own import produced. Forcing 1 here would silently
        // resize any future model whose root scale is not 1.
        cv.transform.localRotation = Quaternion.identity;

        return instance;
    }

    private static string SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path, out bool success);
        if (!success)
            throw new System.InvalidOperationException($"[CharacterPrefabMigration] SaveAsPrefabAsset failed for '{path}'.");
        return path;
    }

    // ------------------------------------------------------------------ report

    private static string BuildFinalReport(List<string> created, List<string> adopted, List<string> warnings, string failure)
    {
        var report = new StringBuilder();
        report.AppendLine(failure == null
            ? "[CharacterPrefabMigration] GENERATE complete."
            : "[CharacterPrefabMigration] GENERATE failed partway — the run is resumable, see below.");

        foreach (string path in adopted)
        {
            report.AppendLine($"  ADOPTED {path}");
        }
        foreach (string path in created)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(asset);
            report.AppendLine($"  CREATED {path}  ->  PrefabAssetType.{type}");
            if (path == CVMicrowavePath || path == PlayerMicrowavePath || path == RivalMicrowavePath)
            {
                if (type != PrefabAssetType.Variant)
                    report.AppendLine($"  !! EXPECTED PrefabAssetType.Variant but got {type} — the prefab link did not take; investigate before swapping scenes.");
            }
        }
        foreach (string warning in warnings)
            report.AppendLine($"  WARNING: {warning}");

        if (failure != null)
        {
            report.AppendLine($"  FAILED: {failure}");
            if (created.Count > 0)
                report.AppendLine("  The CREATED assets listed above are on disk and valid — nothing needs deleting; re-run Generate to fill in the rest.");
        }

        AppendOffsetReport(report);
        return report.ToString();
    }

    private static void AppendOffsetReport(StringBuilder report)
    {
        report.AppendLine($"Baked offsets found in Char.prefab: CapsuleCollider center {CharColliderCenter}, " +
                          $"model child local position {CharModelChildOffset}. The new structure normalises both to the root.");
        report.AppendLine($"SCENE-FIX DELTA REQUIRED: any existing 'Char' instance swapped to PlayerCharacter_Microwave must have its " +
                          $"root transform moved by (+4.9, 0, -9.2) — the model used to sit at that local offset and now sits at local zero. " +
                          $"Rival instances need no delta: they were already normalised.");
        report.AppendLine("This script changed NO scene and NO existing prefab. Char.prefab and Rival.prefab are intact and " +
                          "still used by the scenes until you swap them.");
        report.AppendLine("Note: temporaries are instantiated into the open scene and destroyed again, so that scene is " +
                          "unchanged but may be marked dirty — safe to discard the dirty state.");
    }

    private static void ReportInputProblems(StringBuilder report, List<string> problems)
    {
        foreach (string problem in problems)
            report.AppendLine($"  MISSING INPUT: {problem}");
    }
}
