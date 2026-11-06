using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dead Frequency - GREYBOX scene build v2 (Greybox Build Specification, build-order step 2).
// A composed loop map on the kit's true module (walls 4.0 / doors 1.0x2.1, floors 4x4,
// corridor clear width 2.4) - the Test Facility layout was an asset display with no
// corridor-to-room circulation, so this map is authored fresh: spine -> Commons hub ->
// Comms Bunker -> Server Room -> Maintenance -> Lab Wing -> Lab Annex, Transit north.
// Blender->Unity conversion as verified: position (bx, bz, by), yaw -rotZ.
//   - PF_SecurityDoor instances in the four kit door openings
//   - the Mid-Station split-key door (PF_HeavySecurityDoor + two PF_AccessPanel, one per wing)
//   - PF_ObjectivePoint markers (Mid-Station + one per wing)
//   - spawn markers: Team A, Team B, Operator (at the comms workstation chair)
//   - zone markers naming the spec's spaces (Comms Bunker, Lab Wing, Server Room,
//     Maintenance, Habitation Commons R1, Transit Mid-Station)
// Scene: Assets/Scenes/Greybox/DF_Greybox.unity - NOT added to build settings yet (step 3).
// RUN WITH CARE: replaces the currently open scene (choose Don't Save if prompted).
public static class DeadFrequencyGreybox
{
    struct Place
    {
        public string Model;
        public float BX, BY, BZ;
        public int RotZ;
        public Place(string model, float bx, float by, float bz, int rotZ)
        { Model = model; BX = bx; BY = by; BZ = bz; RotZ = rotZ; }
    }

    // (prefab, instance name, unity position, unity yaw)
    struct Prop
    {
        public string Prefab, Name;
        public Vector3 Pos;
        public float Yaw;
        public Prop(string prefab, string name, Vector3 pos, float yaw)
        { Prefab = prefab; Name = name; Pos = pos; Yaw = yaw; }
    }

    static readonly Place[] Placements =
    {
        new Place("SM_Kit_Floor", 7.4f, -7.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 7.4f, -7.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Door", 7.4f, -5.5f, 0f, 0),
        new Place("SM_Kit_Wall_Door", 7.4f, -9.5f, 0f, 0),
        new Place("SM_Kit_Wall_Door", 5.4f, -7.4f, 0f, 90),
        new Place("SM_Kit_Wall_Door", 9.4f, -7.4f, 0f, 90),
        new Place("SM_Kit_Floor", 3.4f, -7.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 3.4f, -7.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Straight", 3.4f, -5.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", 1.4f, -7.4f, 0f, 90),
        new Place("SM_Kit_Wall_Door", 3.4f, -9.5f, 0f, 0),
        new Place("SM_Kit_Floor", 3.4f, -11.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 3.4f, -11.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Straight", 3.4f, -13.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", 1.4f, -11.4f, 0f, 90),
        new Place("SM_Kit_Wall_Door", 5.4f, -11.4f, 0f, 90),
        new Place("SM_Kit_Floor", 7.4f, -11.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 7.4f, -11.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Straight", 7.4f, -13.5f, 0f, 0),
        new Place("SM_Kit_Wall_Door", 9.4f, -11.4f, 0f, 90),
        new Place("SM_Kit_Floor", 11.4f, -11.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 11.4f, -11.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Door", 11.4f, -9.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", 11.4f, -13.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", 13.4f, -11.4f, 0f, 90),
        new Place("SM_Kit_Floor", 11.4f, -7.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", 11.4f, -7.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Straight", 11.4f, -5.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", 13.4f, -7.4f, 0f, 90),
        new Place("SM_Kit_Floor", -7.4f, 7.4f, 0f, 0),
        new Place("SM_Kit_Ceiling", -7.4f, 7.4f, 3.2f, 0),
        new Place("SM_Kit_Wall_Straight", -7.4f, 9.5f, 0f, 0),
        new Place("SM_Kit_Wall_Straight", -9.4f, 7.4f, 0f, 90),
        new Place("SM_Kit_Wall_Straight", -5.4f, 7.4f, 0f, 90),
        new Place("SM_Kit_Wall_Door", -7.4f, 5.5f, 0f, 0),
        new Place("SM_Kit_Corridor_Straight", -4f, 0f, 0f, 0),
        new Place("SM_Kit_Corridor_Straight", 0f, 0f, 0f, 0),
        new Place("SM_Kit_Corridor_Straight", 4f, 0f, 0f, 0),
        new Place("SM_Kit_Corridor_Corner", 7.4f, 0f, 0f, 180),
        new Place("SM_Kit_Corridor_Straight", 7.4f, -3.4f, 0f, 90),
        new Place("SM_Kit_Corridor_Corner", -7.4f, 0f, 0f, 0),
        new Place("SM_Kit_Corridor_Straight", -7.4f, 3.4f, 0f, 90),
    };

    static readonly Place[] Furniture =
    {
        new Place("SM_Workstation", 2.4f, -8.7f, 0f, 0),
        new Place("SM_Chair", 2.4f, -8f, 0f, 180),
        new Place("SM_FilingCabinet", 1.8f, -6f, 0f, 0),
        new Place("SM_Desk", 4.4f, -6.1f, 0f, 180),
        new Place("SM_ServerRack", 1.9f, -12.6f, 0f, 0),
        new Place("SM_ServerRack", 2.7f, -12.6f, 0f, 0),
        new Place("SM_Terminal", 4.6f, -10f, 0f, 180),
        new Place("SM_ElectricalPanel", 5.15f, -11.4f, 0f, 270),
        new Place("SM_Generator", 7.4f, -12.4f, 0f, 0),
        new Place("SM_Valve", 5.8f, -10f, 0f, 0),
        new Place("SM_Shelf", 9f, -10f, 0f, 180),
        new Place("SM_Beacon", 7.4f, -9.6f, 2.9f, 0),
        new Place("SM_Desk", 10.4f, -10.3f, 0f, 0),
        new Place("SM_Terminal", 12.6f, -12.6f, 0f, 180),
        new Place("SM_Shelf", 13f, -10.2f, 0f, 180),
        new Place("SM_Workstation", 11.4f, -8.7f, 0f, 0),
        new Place("SM_Chair", 11.4f, -8f, 0f, 180),
        new Place("SM_Terminal", 10.2f, -6.2f, 0f, 180),
        new Place("SM_Radio_Handheld", 11.1f, -8.35f, 0.79f, 0),
        new Place("SM_Desk", 8.6f, -6.2f, 0f, 180),
        new Place("SM_Flashlight", 7f, -7f, 0.79f, 0),
        new Place("SM_Switch", 6.6f, -5.3f, 1.2f, 180),
        new Place("SM_ControlPanel", -7.4f, 6.2f, 0f, 180),
        new Place("SM_Terminal", -8.6f, 8.6f, 0f, 180),
        new Place("SM_SecurityCamera", -5.7f, 5.8f, 2.6f, 180),
        new Place("SM_AlarmUnit", 0.5f, -1.2f, 2.4f, 0),
        new Place("SM_WallLight_Warning", 0f, 1.2f, 2.6f, 180),
        new Place("SM_AccessPanel", 6.6f, -5.7f, 1.15f, 180),
    };

    // Door openings come straight from the layout's SM_Kit_Wall_Door placements:
    // Blender (4.4,-7.6) rot90 / (10.4,-7.6) rot90 / (2.4,-10.4) rot0 / (12.4,-10.4) rot0
    // -> Unity pos (bx, 0, by), yaw -rotZ.
    static readonly Prop[] Props =
    {
        new Prop("PF_SecurityDoor", "Door_Commons_North", new Vector3(7.4f, 0f, -5.5f), 0f),
        new Prop("PF_SecurityDoor", "Door_Commons_South", new Vector3(7.4f, 0f, -9.5f), 0f),
        new Prop("PF_SecurityDoor", "Door_Commons_West",  new Vector3(5.4f, 0f, -7.4f), -90f),
        new Prop("PF_SecurityDoor", "Door_Commons_East",  new Vector3(9.4f, 0f, -7.4f), -90f),
        new Prop("PF_SecurityDoor", "Door_Bunker_South",  new Vector3(3.4f, 0f, -9.5f), 0f),
        new Prop("PF_SecurityDoor", "Door_Server_East",   new Vector3(5.4f, 0f, -11.4f), -90f),
        new Prop("PF_SecurityDoor", "Door_Maintenance_East", new Vector3(9.4f, 0f, -11.4f), -90f),
        new Prop("PF_SecurityDoor", "Door_Lab_North",     new Vector3(11.4f, 0f, -9.5f), 0f),
        new Prop("PF_SecurityDoor", "Door_MidStation_SplitKey", new Vector3(-7.4f, 0f, 5.5f), 0f),
        new Prop("PF_AccessPanel", "Panel_SplitKey_A", new Vector3(-8.2f, 1.15f, 5.3f), 180f),
        new Prop("PF_AccessPanel", "Panel_SplitKey_B", new Vector3(-6.6f, 1.15f, 5.3f), 180f),
        new Prop("PF_ObjectivePoint", "Objective_MidStation", new Vector3(-7.4f, 1f, 7.4f), 0f),
        new Prop("PF_ObjectivePoint", "Objective_LabWing",    new Vector3(11.4f, 1f, -11.4f), 0f),
        new Prop("PF_ObjectivePoint", "Objective_CommsBunker", new Vector3(3.4f, 1f, -7.4f), 0f),
    };

    static void Marker(string name, Vector3 pos, float yaw, Transform parent)
    {
        var m = new GameObject(name);
        m.transform.position = pos;
        m.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        m.transform.SetParent(parent, true);
    }

    [MenuItem("DEAD FREQUENCY/Build Greybox Scene")]
    public static void Build()
    {
        var modelByName = new Dictionary<string, GameObject>();
        foreach (var path in Directory.GetFiles("Assets/Models", "*.fbx", SearchOption.AllDirectories))
        {
            var p = path.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) modelByName[Path.GetFileNameWithoutExtension(p)] = go;
        }
        var prefabByName = new Dictionary<string, GameObject>();
        if (Directory.Exists("Assets/Prefabs"))
        {
            foreach (var path in Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories))
            {
                var p = path.Replace('\\', '/');
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) prefabByName[Path.GetFileNameWithoutExtension(p)] = go;
            }
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Greybox")) AssetDatabase.CreateFolder("Assets/Scenes", "Greybox");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "DF_Greybox";

        var root = new GameObject("Kestrel Deep - Greybox");
        var structure = new GameObject("Structure"); structure.transform.SetParent(root.transform);
        var furnishing = new GameObject("Furnishing"); furnishing.transform.SetParent(root.transform);
        var gameplay = new GameObject("Gameplay"); gameplay.transform.SetParent(root.transform);
        var doors = new GameObject("Doors"); doors.transform.SetParent(gameplay.transform);
        var zones = new GameObject("Zones"); zones.transform.SetParent(gameplay.transform);
        var spawns = new GameObject("Spawns"); spawns.transform.SetParent(gameplay.transform);

        int placed = 0, missing = 0;
        foreach (var pl in Placements)
        {
            GameObject asset;
            if (!modelByName.TryGetValue(pl.Model, out asset))
            { missing++; Debug.LogWarning("[DF Greybox] Model not found: " + pl.Model); continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            inst.name = pl.Model;
            inst.transform.position = new Vector3(pl.BX, pl.BZ, pl.BY);
            inst.transform.rotation = Quaternion.Euler(0f, -pl.RotZ, 0f);
            inst.transform.SetParent(structure.transform, true);
            placed++;
        }

        int furnPlaced = 0;
        foreach (var pl in Furniture)
        {
            GameObject asset;
            if (!modelByName.TryGetValue(pl.Model, out asset))
            { missing++; Debug.LogWarning("[DF Greybox] Furniture model not found: " + pl.Model); continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            inst.name = pl.Model;
            inst.transform.position = new Vector3(pl.BX, pl.BZ, pl.BY);
            inst.transform.rotation = Quaternion.Euler(0f, -pl.RotZ, 0f);
            inst.transform.SetParent(furnishing.transform, true);
            furnPlaced++;
        }

        int propsPlaced = 0, propsMissing = 0;
        foreach (var prop in Props)
        {
            GameObject asset;
            if (!prefabByName.TryGetValue(prop.Prefab, out asset))
            { propsMissing++; Debug.LogWarning("[DF Greybox] Prefab not found: " + prop.Prefab); continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            inst.name = prop.Name;
            inst.transform.position = prop.Pos;
            inst.transform.rotation = Quaternion.Euler(0f, prop.Yaw, 0f);
            // Measure-after-place register: centre the leaf itself on the aperture
            // using the placed instance's real leaf position (import-proof).
            foreach (var tr in inst.GetComponentsInChildren<Transform>())
            {
                if (tr.name == "DoorLeaf")
                {
                    var lp = tr.position;
                    inst.transform.position += new Vector3(prop.Pos.x - lp.x, 0f, prop.Pos.z - lp.z);
                    break;
                }
            }
            inst.transform.SetParent(doors.transform, true);
            propsPlaced++;
        }

        // Zones (markers at room centres).
        Marker("Zone_Spine",          new Vector3(0f, 0f, 0f),        0f, zones.transform);
        Marker("Zone_Commons",        new Vector3(7.4f, 0f, -7.4f),   0f, zones.transform);
        Marker("Zone_CommsBunker",    new Vector3(3.4f, 0f, -7.4f),   0f, zones.transform);
        Marker("Zone_ServerRoom",     new Vector3(3.4f, 0f, -11.4f),  0f, zones.transform);
        Marker("Zone_Maintenance",    new Vector3(7.4f, 0f, -11.4f),  0f, zones.transform);
        Marker("Zone_LabWing",        new Vector3(11.4f, 0f, -11.4f), 0f, zones.transform);
        Marker("Zone_LabAnnex",       new Vector3(11.4f, 0f, -7.4f),  0f, zones.transform);
        Marker("Zone_TransitMidStation", new Vector3(-7.4f, 0f, 7.4f), 0f, zones.transform);

        // Spawns.
        Marker("Spawn_TeamA",   new Vector3(7.4f, 1.7f, -6.4f),  0f, spawns.transform);   // Commons, facing the spine door
        Marker("Spawn_TeamB",   new Vector3(11.4f, 1.7f, -7.4f), -90f, spawns.transform); // Lab Annex, facing the Commons door
        Marker("Spawn_Operator", new Vector3(2.4f, 1.7f, -8.0f), 180f, spawns.transform); // comms chair, facing workstation

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(2f, 24f, -28f);
            cam.transform.LookAt(new Vector3(2f, 0f, -2f));
        }

        var scenePath = "Assets/Scenes/Greybox/DF_Greybox.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[DF Greybox] Architecture: " + placed + " | Furniture: " + furnPlaced + " | Gameplay props: " + propsPlaced + " | Missing: " + (missing + propsMissing) + " | Scene saved: " + scenePath);
    }
}
