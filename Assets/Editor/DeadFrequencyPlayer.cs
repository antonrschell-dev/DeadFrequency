using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dead Frequency - greybox player build + scene patch (build step 2, slice 2b).
// 1) Builds Assets/Prefabs/PF_Player.prefab (CharacterController + camera head + flashlight).
// 2) Adds DFDoor to the PF_SecurityDoor / PF_HeavySecurityDoor prefab assets (scene instances inherit).
// 3) Opens DF_Greybox: adds MeshColliders to every kit/prop mesh (nothing in the kit ships with
//    colliders - without this the player falls through the floor), retires the overview camera,
//    and places PF_Player at Spawn_TeamA.
// RUN WITH CARE: opens and re-saves DF_Greybox (choose Don't Save for other scenes if prompted).
public static class DeadFrequencyPlayer
{
    [MenuItem("DEAD FREQUENCY/Build Player + Patch Greybox")]
    public static void Build()
    {
        // ---- 1) Player prefab ----
        var root = new GameObject("PF_Player");
        var cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
        root.AddComponent<DFPlayerController>();
        root.AddComponent<DFInteract>();

        var head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        var cam = head.AddComponent<Camera>();
        cam.nearClipPlane = 0.08f;
        head.AddComponent<AudioListener>();
        head.AddComponent<DFFlashlight>();
        head.tag = "MainCamera";

        var spotGo = new GameObject("Flashlight");
        spotGo.transform.SetParent(head.transform, false);
        spotGo.transform.localPosition = new Vector3(0.15f, -0.1f, 0.05f);
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.intensity = 3f;
        spot.range = 18f;
        spot.spotAngle = 55f;
        spot.enabled = false;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/PF_Player.prefab");
        Object.DestroyImmediate(root);

        // ---- 2) Door components on the door prefabs ----
        int doorsUpgraded = 0;
        foreach (var doorPath in new[] { "Assets/Prefabs/PF_SecurityDoor.prefab", "Assets/Prefabs/PF_HeavySecurityDoor.prefab" })
        {
            if (!File.Exists(doorPath)) { Debug.LogWarning("[DF Player] Door prefab missing: " + doorPath); continue; }
            var contents = PrefabUtility.LoadPrefabContents(doorPath);
            if (contents.GetComponent<DFDoor>() == null) contents.AddComponent<DFDoor>();
            PrefabUtility.SaveAsPrefabAsset(contents, doorPath);
            PrefabUtility.UnloadPrefabContents(contents);
            doorsUpgraded++;
        }

        // ---- 3) Patch the greybox scene ----
        var scenePath = "Assets/Scenes/Greybox/DF_Greybox.unity";
        if (!File.Exists(scenePath)) { Debug.LogError("[DF Player] Greybox scene not found: " + scenePath); return; }
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        int colliders = 0;
        Transform structure = null, furnishing = null, spawnA = null;
        bool playerExists = false;
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var tr in go.GetComponentsInChildren<Transform>())
            {
                if (tr.name == "Structure") structure = tr;
                else if (tr.name == "Furnishing") furnishing = tr;
                else if (tr.name == "Spawn_TeamA") spawnA = tr;
                else if (tr.name == "Player (Team A)") playerExists = true;
            }
        }
        foreach (var group in new[] { structure, furnishing })
        {
            if (group == null) continue;
            foreach (var mf in group.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() == null && mf.GetComponent<MeshCollider>() == null)
                { mf.gameObject.AddComponent<MeshCollider>(); colliders++; }
            }
        }

        int camsRetired = 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var c in go.GetComponentsInChildren<Camera>())
            {
                if (c.gameObject != null && c.CompareTag("MainCamera")) { c.gameObject.SetActive(false); camsRetired++; }
            }
        }

        bool playerPlaced = false;
        if (!playerExists && spawnA != null && playerPrefab != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            inst.name = "Player (Team A)";
            inst.transform.position = spawnA.position;
            inst.transform.rotation = spawnA.rotation;
            playerPlaced = true;
        }
        else if (!playerExists) Debug.LogWarning("[DF Player] Spawn_TeamA marker not found - player not placed.");

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DF Player] Player prefab built | Door prefabs upgraded: " + doorsUpgraded + " | Mesh colliders added: " + colliders + " | Overview cameras retired: " + camsRetired + " | Player placed at Spawn_TeamA: " + playerPlaced);
    }
}
