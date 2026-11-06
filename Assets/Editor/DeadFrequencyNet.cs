using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dead Frequency - networking setup (greybox build step 3b).
// One click: makes PF_Player a networked prefab (NetworkObject + NetworkTransform
// + DFNetPlayer + a visible body capsule), drops FishNet's NetworkManager prefab
// into DF_Greybox with the Tugboat transport, wires PlayerSpawner to PF_Player
// and the Team A/B spawn markers, retires the old hand-placed player, adds the
// session-buttons object, and puts the scene in Build Settings (FishNet clients
// can only load scenes that are listed there).
public static class DeadFrequencyNet
{
    const string GreyboxScene = "Assets/Scenes/Greybox/DF_Greybox.unity";
    const string PlayerPrefab = "Assets/Prefabs/PF_Player.prefab";
    const string NetworkManagerPrefab = "Packages/com.firstgeargames.fishnet/Demos/Prefabs/NetworkManager.prefab";

    [MenuItem("DEAD FREQUENCY/Setup Networking (Step 3b)")]
    public static void Setup()
    {
        ConvertPlayerPrefab();

        var scene = EditorSceneManager.OpenScene(GreyboxScene);

        var nmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkManagerPrefab);
        if (nmPrefab == null) { Debug.LogError("[DF Net] NetworkManager prefab not found in the FishNet package - is FishNet embedded?"); return; }

        var nmObj = GameObject.Find("NetworkManager");
        if (nmObj == null)
        {
            nmObj = (GameObject)PrefabUtility.InstantiatePrefab(nmPrefab, scene);
            nmObj.name = "NetworkManager";
        }
        if (nmObj.GetComponent<Tugboat>() == null) nmObj.AddComponent<Tugboat>();

        // The demo NetworkManager nests FishNet's own HUD canvas - remove it so
        // the game has exactly one set of session controls (ours).
        var hudVictims = new System.Collections.Generic.List<GameObject>();
        foreach (var t in nmObj.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains("NetworkHudCanvas")) hudVictims.Add(t.gameObject);
        foreach (var v in hudVictims) Object.DestroyImmediate(v);

        var spawner = nmObj.GetComponent<PlayerSpawner>();
        if (spawner == null) spawner = nmObj.AddComponent<PlayerSpawner>();
        var playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        spawner.SetPlayerPrefab(playerAsset.GetComponent<NetworkObject>());
        // Spawn order IS role order (step 3c): first connection = Operator.
        var spawns = new System.Collections.Generic.List<Transform>();
        foreach (var spawnName in new[] { "Spawn_Operator", "Spawn_TeamA", "Spawn_TeamB" })
        {
            var marker = GameObject.Find(spawnName);
            if (marker != null) spawns.Add(marker.transform);
        }
        spawner.Spawns = spawns.ToArray();
        EditorUtility.SetDirty(spawner);

        var old = GameObject.Find("Player (Team A)");
        if (old != null) Object.DestroyImmediate(old);

        if (GameObject.Find("CommsRelay") == null)
        {
            var relayObj = new GameObject("CommsRelay");
            relayObj.AddComponent<NetworkObject>();
            relayObj.AddComponent<DFCommsRelay>();
        }

        var netUi = GameObject.Find("NetUI");
        if (netUi == null)
        {
            netUi = new GameObject("NetUI");
            netUi.AddComponent<DFSessionButtons>();
        }
        if (netUi.GetComponent<DFCommsUI>() == null) netUi.AddComponent<DFCommsUI>();

        // DF_Greybox must be the build's BOOT scene (index 0): it carries the
        // NetworkManager, session UI and relay. The template SampleScene sitting
        // first makes builds boot into an empty sky (Anton's report, Nov 6) -
        // so the greybox becomes the ONLY listed scene for now.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GreyboxScene, true) };

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[DF Net] Networking ready: PF_Player networked, NetworkManager + Tugboat in scene, PlayerSpawner wired to " + spawns.Count + " spawn marker(s), comms relay live. Press Play, Start Host, then T for comms.");
    }

    static void ConvertPlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
        if (root.GetComponent<NetworkObject>() == null) root.AddComponent<NetworkObject>();
        if (root.GetComponent<NetworkTransform>() == null) root.AddComponent<NetworkTransform>();
        if (root.GetComponent<DFNetPlayer>() == null) root.AddComponent<DFNetPlayer>();
        if (root.GetComponent<DFCommsUser>() == null) root.AddComponent<DFCommsUser>();
        if (root.transform.Find("Body") == null)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            var col = body.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        }
        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
        PrefabUtility.UnloadPrefabContents(root);
    }
}
