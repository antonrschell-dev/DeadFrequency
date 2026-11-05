// DEAD FREQUENCY — Unity Editor asset validator (Phase 12, Unity side).
// STATUS: authored in the asset-production workspace; NOT yet compiled or run in a
// Unity Editor (no Unity installation exists there). First-import step: place in
// Assets/Editor/ and run DEAD FREQUENCY > Validate Assets. Treat compiler errors
// on first import as expected until reviewed — the Python validator
// (validate_assets.py) is the currently-verified half of the system.
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DeadFrequencyAssetValidator
{
    [MenuItem("DEAD FREQUENCY/Validate Assets")]
    public static void Validate()
    {
        var problems = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { problems.Add($"Unreadable prefab: {path}"); continue; }
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                if (mr.sharedMaterial == null) problems.Add($"Missing material: {path} :: {mr.name}");
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh == null) problems.Add($"Missing mesh: {path} :: {mf.name}");
        }
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var s = AssetImporter.GetAtPath(path) as AudioImporter;
            if (s != null && !path.Contains("DF_")) problems.Add($"Audio naming violation: {path}");
        }
        Debug.Log(problems.Count == 0
            ? "[DF Validator] No problems found."
            : $"[DF Validator] {problems.Count} problem(s):\n" + string.Join("\n", problems));
    }
}
#endif
