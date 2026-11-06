using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Dead Frequency - one-click prefab assembly, PASS 1 (structure).
// Source of truth: "18 Unity Prefabs/Prefab Specifications.md".
// Each prefab: model (nested), Phase 7 materials assigned to renderer slots in spec order,
// a collider fitted to the model's combined renderer bounds, and the spec's named hook
// transforms as empty children at the model origin. Hook POSITIONS, door hinge pivots and
// the AccessPanel sub-parts (Keypad/CardReader) are the detailed visual pass in the Test
// Facility step - per the spec, hooks are integration points only, no gameplay logic.
// Audio families from the spec are NOT wired here: they land with gameplay code.
public static class DeadFrequencyPrefabs
{
    struct PrefabDef
    {
        public string Name, Model, Collider;
        public string[] Materials, Hooks;
        public PrefabDef(string name, string model, string[] materials, string collider, string[] hooks)
        { Name = name; Model = model; Materials = materials; Collider = collider; Hooks = hooks; }
    }

    static readonly PrefabDef[] Defs =
    {
        new PrefabDef("PF_SecurityDoor", "SM_Door_Security", new[] { "M_Dark_Industrial_Steel", "M_Plastic" }, "box", new[] { "DoorLeaf", "AccessPoint" }),
        new PrefabDef("PF_HeavySecurityDoor", "SM_Door_Heavy", new[] { "M_Dark_Industrial_Steel", "M_Rusted_Metal" }, "box", new[] { "DoorLeaf", "AccessPoint", "WarningLamp" }),
        new PrefabDef("PF_AccessPanel", "SM_AccessPanel", new[] { "M_Plastic", "M_Server_Equipment" }, "box", new[] { "Screen", "LampOK", "LampDeny" }),
        new PrefabDef("PF_Flashlight", "SM_Flashlight", new[] { "M_Plastic", "M_Brushed_Steel" }, "none", new[] { "LightOrigin" }),
        new PrefabDef("PF_Radio", "SM_Radio_Handheld", new[] { "M_Plastic", "M_Rubber" }, "none", new[] { "AntennaTip", "PTT" }),
        new PrefabDef("PF_SecurityCamera", "SM_SecurityCamera", new[] { "M_Dark_Industrial_Steel", "M_Dirty_Glass" }, "box", new[] { "Head", "Lens", "LampRecord" }),
        new PrefabDef("PF_CCTVMonitor", "SM_Terminal", new[] { "M_Server_Equipment", "M_Dirty_Glass" }, "box", new[] { "Screen" }),
        new PrefabDef("PF_ElectricalPanel", "SM_ElectricalPanel", new[] { "M_Painted_Industrial_Metal", "M_Cable_Insulation" }, "box", new[] { "Door", "Breaker01", "Breaker02", "Breaker03", "Breaker04", "Breaker05", "Breaker06" }),
        new PrefabDef("PF_FuseBox", "SM_ElectricalPanel", new[] { "M_Painted_Industrial_Metal" }, "box", new[] { "Door", "Breaker01", "Breaker02", "Breaker03" }),
        new PrefabDef("PF_Generator", "SM_Generator", new[] { "M_Rusted_Metal", "M_Dark_Industrial_Steel", "M_Pipes" }, "box", new[] { "Exhaust", "StatusLamp" }),
        new PrefabDef("PF_ServerRack", "SM_ServerRack", new[] { "M_Server_Equipment", "M_Dark_Industrial_Steel" }, "box", new[] { "UnitLamp01", "UnitLamp02", "UnitLamp03", "UnitLamp04", "UnitLamp05", "UnitLamp06", "UnitLamp07", "UnitLamp08", "FanSpin" }),
        new PrefabDef("PF_ComputerTerminal", "SM_Terminal", new[] { "M_Server_Equipment", "M_Plastic" }, "box", new[] { "Screen", "KeyboardTray" }),
        new PrefabDef("PF_InteractiveSwitch", "SM_Switch", new[] { "M_Plastic", "M_Painted_Industrial_Metal" }, "box", new[] { "Lever" }),
        new PrefabDef("PF_Lever", "SM_Switch", new[] { "M_Dark_Industrial_Steel", "M_Rubber" }, "box", new[] { "Arm" }),
        new PrefabDef("PF_Valve", "SM_Valve", new[] { "M_Rusted_Metal", "M_Pipes" }, "capsule", new[] { "Wheel" }),
        new PrefabDef("PF_EmergencyBeacon", "SM_Beacon", new[] { "M_Emergency_Equipment", "M_Dirty_Glass" }, "box", new[] { "Dome" }),
        new PrefabDef("PF_ObjectivePoint", "", new string[0], "trigger", new[] { "InteractionPoint" }),
        new PrefabDef("PF_WarningLight", "SM_WallLight_Warning", new[] { "M_Emergency_Equipment" }, "box", new[] { "Lamp" }),
        new PrefabDef("PF_AlarmUnit", "SM_AlarmUnit", new[] { "M_Emergency_Equipment", "M_Plastic" }, "box", new[] { "Grille", "Lamp" }),
    };

    [MenuItem("DEAD FREQUENCY/Build Prefabs (Pass 1)")]
    public static void Build()
    {
        // Index models by file name.
        var modelByName = new Dictionary<string, GameObject>();
        foreach (var path in Directory.GetFiles("Assets/Models", "*.fbx", SearchOption.AllDirectories))
        {
            var p = path.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) modelByName[Path.GetFileNameWithoutExtension(p)] = go;
        }
        // Index materials by name.
        var matByName = new Dictionary<string, Material>();
        if (Directory.Exists("Assets/Materials"))
        {
            foreach (var path in Directory.GetFiles("Assets/Materials", "*.mat", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m != null) matByName[Path.GetFileNameWithoutExtension(p)] = m;
            }
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");

        int built = 0, noModel = 0;
        foreach (var d in Defs)
        {
            var root = new GameObject(d.Name);
            GameObject modelInst = null;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool haveBounds = false;

            if (!string.IsNullOrEmpty(d.Model))
            {
                GameObject modelAsset;
                if (modelByName.TryGetValue(d.Model, out modelAsset))
                {
                    modelInst = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                    modelInst.name = "Model";
                    modelInst.transform.SetParent(root.transform, false);

                    // Assign spec materials to renderer slots in order (last material repeats).
                    if (d.Materials.Length > 0)
                    {
                        foreach (var rend in modelInst.GetComponentsInChildren<Renderer>())
                        {
                            var slots = rend.sharedMaterials;
                            for (int i = 0; i < slots.Length; i++)
                            {
                                Material m;
                                var want = d.Materials[Mathf.Min(i, d.Materials.Length - 1)];
                                if (matByName.TryGetValue(want, out m)) slots[i] = m;
                            }
                            rend.sharedMaterials = slots;
                        }
                    }
                    foreach (var rend in modelInst.GetComponentsInChildren<Renderer>())
                    {
                        if (!haveBounds) { bounds = rend.bounds; haveBounds = true; }
                        else bounds.Encapsulate(rend.bounds);
                    }
                }
                else { noModel++; Debug.LogWarning("[DF Prefabs] Model not found: " + d.Model + " for " + d.Name); }
            }

            // Collider fitted to model bounds (root sits at origin, model unrotated).
            if (d.Collider == "box" && haveBounds)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.center = bounds.center; bc.size = bounds.size;
            }
            else if (d.Collider == "capsule" && haveBounds)
            {
                var cc = root.AddComponent<CapsuleCollider>();
                cc.center = bounds.center;
                cc.radius = Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f;
                cc.height = bounds.size.y;
            }
            else if (d.Collider == "trigger")
            {
                var sc = root.AddComponent<SphereCollider>();
                sc.isTrigger = true; sc.radius = 1f;
            }

            foreach (var hook in d.Hooks)
            {
                var h = new GameObject(hook);
                h.transform.SetParent(root.transform, false);
            }

            var path = "Assets/Prefabs/" + d.Name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            built++;
        }

            Debug.Log("[DF Prefabs] Skipped (per spec): PF_BlastDoor (REF - no model yet, spec only)");
            Debug.Log("[DF Prefabs] Skipped (per spec): PF_Backpack (REF - no model yet, spec only)");
            Debug.Log("[DF Prefabs] Skipped (per spec): PF_MedicalKit (REF - no model yet, spec only)");
            Debug.Log("[DF Prefabs] Skipped (per spec): PF_Keypad (sub-part of PF_AccessPanel - detailed pass)");
            Debug.Log("[DF Prefabs] Skipped (per spec): PF_CardReader (sub-part of PF_AccessPanel - detailed pass)");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DF Prefabs] Built: " + built + " | Prefabs with missing model: " + noModel + " | Skipped per spec: 5 (see log)");
    }
}
