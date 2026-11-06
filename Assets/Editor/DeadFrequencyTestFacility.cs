using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Dead Frequency - one-click Asset Test Facility scene build.
// Source of truth: "19 Asset Test Facility/Test Facility Specification.md" +
// "Test Facility Layout.json" (60 placements, verified in Blender: grid PASS, 0 missing).
// Creates Assets/Scenes/Testing/DF_AssetTestFacility.unity and rebuilds the facility from
// the individual kit/prop models. The scene stays OUT of build settings (testing only).
// Coordinate conversion (Blender Z-up, right-handed -> Unity Y-up, left-handed):
//   position = (bx, bz, by);  yaw = -rotation_z_deg. Derived from the axis mapping and
//   cross-checked against the room layouts in the placement data.
// RUN WITH CARE: this replaces the currently open scene (Unity will offer to save it first -
// choose Don't Save unless you have work in it you want to keep).
public static class DeadFrequencyTestFacility
{
    struct Place
    {
        public string Model;
        public float BX, BY, BZ;
        public int RotZ;
        public Place(string model, float bx, float by, float bz, int rotZ)
        { Model = model; BX = bx; BY = by; BZ = bz; RotZ = rotZ; }
    }

    static readonly Place[] Placements =
    {
        new Place("SM_Kit_Corridor_Straight", 0.00f, 0.00f, 0.00f, 0),
        new Place("SM_Kit_Corridor_Straight", 4.00f, 0.00f, 0.00f, 0),
        new Place("SM_Kit_Corridor_Corner", 7.40f, 0.00f, 0.00f, 180),
        new Place("SM_Kit_Corridor_Straight", 7.40f, -3.40f, 0.00f, 90),
        new Place("SM_Kit_Floor", 7.40f, -7.60f, 0.00f, 0),
        new Place("SM_Kit_Floor", 2.40f, -7.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 2.40f, -5.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 2.40f, -9.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 0.40f, -7.60f, 0.00f, 90),
        new Place("SM_Kit_Wall_Door", 4.40f, -7.60f, 0.00f, 90),
        new Place("SM_Kit_Floor", 12.40f, -7.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 12.40f, -5.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Window", 12.40f, -9.60f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 14.40f, -7.60f, 0.00f, 90),
        new Place("SM_Kit_Wall_Door", 10.40f, -7.60f, 0.00f, 90),
        new Place("SM_Kit_Floor", 2.40f, -12.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Door", 2.40f, -10.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 2.40f, -14.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 0.40f, -12.40f, 0.00f, 90),
        new Place("SM_Kit_Wall_Straight", 4.40f, -12.40f, 0.00f, 90),
        new Place("SM_Kit_Floor", 12.40f, -12.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Door", 12.40f, -10.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 12.40f, -14.40f, 0.00f, 0),
        new Place("SM_Kit_Wall_Straight", 10.40f, -12.40f, 0.00f, 90),
        new Place("SM_Kit_Wall_Straight", 14.40f, -12.40f, 0.00f, 90),
        new Place("SM_Kit_Corridor_T", 20.00f, 4.00f, 0.00f, 0),
        new Place("SM_Kit_Corridor_Cross", 24.40f, 4.00f, 0.00f, 0),
        new Place("SM_Kit_Stairs", 20.00f, 8.40f, 0.00f, 0),
        new Place("SM_Kit_ElevatorEntrance", 24.40f, 8.00f, 0.00f, 0),
        new Place("SM_Kit_Hatch", 28.00f, 4.00f, 0.00f, 0),
        new Place("SM_Kit_Ceiling", 28.00f, 8.40f, 3.20f, 0),
        new Place("SM_Kit_PipeRun", 20.00f, 12.00f, 0.00f, 0),
        new Place("SM_Kit_CableTray", 24.40f, 12.00f, 0.00f, 0),
        new Place("SM_Kit_VentGrille", 28.00f, 12.00f, 0.00f, 0),
        new Place("SM_Kit_WallPanel", 30.00f, 12.00f, 0.00f, 0),
        new Place("SM_Workstation", 1.40f, -8.90f, 0.00f, 0),
        new Place("SM_Chair", 1.40f, -8.20f, 0.00f, 180),
        new Place("SM_Desk", 3.40f, -6.30f, 0.00f, 180),
        new Place("SM_FilingCabinet", 0.80f, -6.20f, 0.00f, 0),
        new Place("SM_Workstation", 12.40f, -8.90f, 0.00f, 0),
        new Place("SM_Chair", 12.40f, -8.20f, 0.00f, 180),
        new Place("SM_Terminal", 11.20f, -6.40f, 0.00f, 180),
        new Place("SM_SecurityCamera", 10.60f, -5.90f, 2.60f, 0),
        new Place("SM_Radio_Handheld", 12.10f, -8.55f, 0.79f, 0),
        new Place("SM_ServerRack", 0.90f, -13.60f, 0.00f, 0),
        new Place("SM_ServerRack", 1.70f, -13.60f, 0.00f, 0),
        new Place("SM_ElectricalPanel", 4.20f, -12.40f, 0.00f, 270),
        new Place("SM_Terminal", 3.60f, -11.00f, 0.00f, 180),
        new Place("SM_Generator", 12.40f, -13.40f, 0.00f, 0),
        new Place("SM_Valve", 10.80f, -11.00f, 0.00f, 0),
        new Place("SM_Shelf", 14.00f, -11.00f, 0.00f, 180),
        new Place("SM_Beacon", 12.40f, -10.60f, 2.90f, 0),
        new Place("SM_Door_Security", 7.40f, -5.45f, 0.00f, 0),
        new Place("SM_AccessPanel", 6.70f, -5.30f, 1.15f, 0),
        new Place("SM_Switch", 6.90f, -1.20f, 1.20f, 90),
        new Place("SM_WallLight_Warning", 4.00f, 1.18f, 2.60f, 0),
        new Place("SM_AlarmUnit", 0.50f, -1.20f, 2.40f, 0),
        new Place("SM_Flashlight", 7.00f, -7.00f, 0.79f, 0),
        new Place("SM_MedicalBed", 28.00f, -4.00f, 0.00f, 0),
        new Place("SM_ControlPanel", 24.40f, -4.00f, 0.00f, 0),
    };

    [MenuItem("DEAD FREQUENCY/Build Test Facility Scene")]
    public static void Build()
    {
        var modelByName = new Dictionary<string, GameObject>();
        foreach (var path in Directory.GetFiles("Assets/Models", "*.fbx", SearchOption.AllDirectories))
        {
            var p = path.Replace('\\', '/');
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) modelByName[Path.GetFileNameWithoutExtension(p)] = go;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Testing")) AssetDatabase.CreateFolder("Assets/Scenes", "Testing");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "DF_AssetTestFacility";

        var root = new GameObject("Test Facility");
        var structure = new GameObject("Structure"); structure.transform.SetParent(root.transform);
        var furnishing = new GameObject("Furnishing"); furnishing.transform.SetParent(root.transform);

        int placed = 0, missing = 0;
        foreach (var pl in Placements)
        {
            GameObject asset;
            if (!modelByName.TryGetValue(pl.Model, out asset))
            { missing++; Debug.LogWarning("[DF Facility] Model not found: " + pl.Model); continue; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            inst.name = pl.Model;
            inst.transform.position = new Vector3(pl.BX, pl.BZ, pl.BY);
            inst.transform.rotation = Quaternion.Euler(0f, -pl.RotZ, 0);
            inst.transform.SetParent(pl.Model.StartsWith("SM_Kit_") ? structure.transform : furnishing.transform, true);
            placed++;
        }

        // Player start: corridor mouth at the facility origin, facing down the corridor (+X).
        var start = new GameObject("PlayerStart");
        start.transform.position = new Vector3(1f, 1.7f, 0);
        start.transform.rotation = Quaternion.Euler(0f, 90f, 0);

        // Overview camera vantage (the default Main Camera): high corner looking at facility centre.
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(16f, 15f, -24f);
            cam.transform.LookAt(new Vector3(16f, 0f, -1f));
        }

        var scenePath = "Assets/Scenes/Testing/DF_AssetTestFacility.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[DF Facility] Placed: " + placed + " | Missing models: " + missing + " | Scene saved: " + scenePath);
    }
}
