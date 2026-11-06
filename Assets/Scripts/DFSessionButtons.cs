using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;

// Dead Frequency greybox - session buttons (build step 3b).
// Deliberately primitive OnGUI: no assets, no canvas, works everywhere.
// Host = server + local client. Join connects a client to 127.0.0.1
// (same machine test) - the Tugboat client address can be changed in the
// NetworkManager for LAN play. Keys: H = host, J = join, X = stop.
public class DFSessionButtons : MonoBehaviour
{
    bool _server;
    bool _client;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.hKey.wasPressedThisFrame) Host();
        if (kb.jKey.wasPressedThisFrame) Join();
        if (kb.xKey.wasPressedThisFrame) StopAll();
    }

    void Host()
    {
        var nm = InstanceFinder.NetworkManager;
        if (nm == null) return;
        if (!_server) { nm.ServerManager.StartConnection(); _server = true; }
        if (!_client) { nm.ClientManager.StartConnection(); _client = true; }
    }

    void Join()
    {
        var nm = InstanceFinder.NetworkManager;
        if (nm == null) return;
        if (!_client) { nm.ClientManager.StartConnection(); _client = true; }
    }

    void StopAll()
    {
        var nm = InstanceFinder.NetworkManager;
        if (nm == null) return;
        if (_client) { nm.ClientManager.StopConnection(); _client = false; }
        if (_server) { nm.ServerManager.StopConnection(true); _server = false; }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12, 12, 240, 170));
        if (!_server && !_client)
        {
            if (GUILayout.Button("Start Host  [H]")) Host();
            if (GUILayout.Button("Join 127.0.0.1  [J]")) Join();
        }
        else
        {
            GUILayout.Label(_server ? "HOSTING (server + client)" : "CLIENT (joined)");
            if (GUILayout.Button("Stop  [X]")) StopAll();
        }
        GUILayout.EndArea();
    }
}
