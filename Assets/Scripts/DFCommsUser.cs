using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Dead Frequency greybox - per-player comms identity (build step 3c).
// The server assigns roles in connection order: the first connection (the host)
// is the OPERATOR at the bunker chair; explorers alternate Team A / Team B.
// Role and Team are SyncVars, so every client can render the right UI while the
// relay validates everything server-side from the caller's connection.
public class DFCommsUser : NetworkBehaviour
{
    public readonly SyncVar<int> Role = new SyncVar<int>(); // 0 = explorer, 1 = operator
    public readonly SyncVar<int> Team = new SyncVar<int>(); // explorers: 0 = A, 1 = B

    static int _joinCounter; // server-side only

    public bool IsOperator { get { return Role.Value == 1; } }

    public override void OnStartServer()
    {
        base.OnStartServer();
        int n = _joinCounter++;
        if (n == 0) { Role.Value = 1; Team.Value = -1; }
        else { Role.Value = 0; Team.Value = (n - 1) % 2; }
        DFCommsRelay.Register(this);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        DFCommsRelay.Unregister(this);
    }

    // A relayed message lands in this explorer's inbox. Per the design's core
    // ruling, the inbox shows the message body ONLY - never who sent it.
    [TargetRpc]
    public void TargetDeliver(NetworkConnection conn, string body)
    {
        DFCommsUI.OnDelivered(body);
    }

    // An explorer's message lands in the operator's queue.
    [TargetRpc]
    public void TargetQueued(NetworkConnection conn, int index, string body)
    {
        DFCommsUI.OnQueued(body);
    }
}
