using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// One-time migration: splits Char.prefab / Rival.prefab into a logic-base + character-visual
// prefab pair per character, under Assets/Prefabs/Characters/.
//
// Purely additive: nothing existing is modified or deleted. All targets are validated and all
// output paths are checked for conflicts BEFORE anything is written — a failed run writes zero
// assets. Run "Tools/DAGMA/Character Prefabs/Dry Run" first to see the plan.
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
        List<string> conflicts = FindConflicts();

        var report = new StringBuilder();
        report.AppendLine("[CharacterPrefabMigration] DRY RUN — nothing will be written.");
        ReportProblems(report, problems, conflicts);

        if (problems.Count == 0)
        {
            report.AppendLine($"Inputs OK: Char='{AssetDatabase.GetAssetPath(inputs.charPrefab)}', " +
                              $"Rival='{AssetDatabase.GetAssetPath(inputs.rivalPrefab)}', " +
                              $"PlayerMovementConfig='{AssetDatabase.GetAssetPath(inputs.playerMovementConfig)}', " +
                              $"model='{AssetDatabase.GetAssetPath(inputs.microwaveModel)}', " +
                              $"controller='{AssetDatabase.GetAssetPath(inputs.microwaveController)}'.");
            report.AppendLine("Would create:");
            report.AppendLine($"  {PlayerBasePath}   (Regular; root 'PlayerCharacter', Layer 6, PlayerMovement + CapsuleCollider center {NormalizedColliderCenter})");
            report.AppendLine($"  {RivalBasePath}   (Regular; root 'Rival', Layer 7, OpponentMovement [profile null] + BoxCollider)");
            report.AppendLine($"  {CVMicrowavePath}   (Variant of the imported model; adds Animator on root)");
            report.AppendLine($"  {PlayerMicrowavePath}   (Variant of PlayerCharacter_Base + CV_Microwave child)");
            report.AppendLine($"  {RivalMicrowavePath}   (Variant of Rival_Base + CV_Microwave child)");
            AppendOffsetReport(report);
        }

        Debug.Log(report.ToString());
    }

    [MenuItem("Tools/DAGMA/Character Prefabs/Generate")]
    private static void Generate()
    {
        var problems = new List<string>();
        Inputs inputs = ResolveInputs(problems);
        List<string> conflicts = FindConflicts();

        if (problems.Count > 0 || conflicts.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine("[CharacterPrefabMigration] ABORTED — nothing was written.");
            ReportProblems(report, problems, conflicts);
            Debug.LogError(report.ToString());
            return;
        }

        var created = new List<string>();

        // Track every temporary instantiated GameObject so failure paths can clean up.
        var temporaries = new List<GameObject>();
        try
        {
            // --- Base prefabs: plain new GameObjects saved as Regular prefabs ---
            GameObject playerBase = CreatePlayerBase(inputs);
            temporaries.Add(playerBase);
            created.Add(SavePrefab(playerBase, PlayerBasePath));

            GameObject rivalBase = CreateRivalBase();
            temporaries.Add(rivalBase);
            created.Add(SavePrefab(rivalBase, RivalBasePath));

            GameObject cvMicrowave = CreateCVMicrowave(inputs, temporaries);
            created.Add(SavePrefab(cvMicrowave, CVMicrowavePath));

            // --- Variants: InstantiatePrefab on the base, so SaveAsPrefabAsset produces a Variant ---
            GameObject playerMicrowave = CreateVariant(PlayerBasePath, CVMicrowavePath, "PlayerCharacter_Microwave", temporaries);
            created.Add(SavePrefab(playerMicrowave, PlayerMicrowavePath));

            GameObject rivalMicrowave = CreateVariant(RivalBasePath, CVMicrowavePath, "Rival_Microwave", temporaries);
            created.Add(SavePrefab(rivalMicrowave, RivalMicrowavePath));
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

        Debug.Log(BuildFinalReport(created));
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

    private static List<string> FindConflicts()
    {
        var conflicts = new List<string>();
        foreach (string path in new[] { PlayerBasePath, RivalBasePath, CVMicrowavePath, PlayerMicrowavePath, RivalMicrowavePath })
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                conflicts.Add(path);
        }
        return conflicts;
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

    private static GameObject CreatePlayerBase(Inputs inputs)
    {
        var go = new GameObject("PlayerCharacter") { layer = 6 };

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

    private static GameObject CreateRivalBase()
    {
        var go = new GameObject("Rival") { layer = 7 };

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
            throw new System.InvalidOperationException($"[CharacterPrefabMigration] Cannot build '{rootName}': base or CV prefab failed to load after saving.");

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
        cv.transform.localRotation = Quaternion.identity;
        cv.transform.localScale = Vector3.one;

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

    private static string BuildFinalReport(List<string> created)
    {
        var report = new StringBuilder();
        report.AppendLine("[CharacterPrefabMigration] GENERATE complete.");

        foreach (string path in created)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(asset);
            report.AppendLine($"  Created {path}  ->  PrefabAssetType.{type}");
            if (path == CVMicrowavePath || path == PlayerMicrowavePath || path == RivalMicrowavePath)
            {
                if (type != PrefabAssetType.Variant)
                    report.AppendLine($"  !! EXPECTED PrefabAssetType.Variant but got {type} — the prefab link did not take; investigate before swapping scenes.");
            }
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
        report.AppendLine("Note: temporaries were instantiated into the open scene and destroyed again, so that scene is " +
                          "unchanged but may be marked dirty — safe to discard the dirty state.");
    }

    private static void ReportProblems(StringBuilder report, List<string> problems, List<string> conflicts)
    {
        foreach (string problem in problems)
            report.AppendLine($"  MISSING INPUT: {problem}");
        foreach (string conflict in conflicts)
            report.AppendLine($"  OUTPUT EXISTS (will not overwrite): {conflict}");
    }
}
