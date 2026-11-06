using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

// Dead Frequency greybox - the clean comms relay (build step 3c).
// Server-authoritative, NO Listener interference yet (that is step 4):
//   explorer -> CmdSendToOperator -> server queue -> operator's screen
//   operator -> CmdRelay(index, team) -> that team's explorers' inboxes
// Every call is validated on the server from the caller's connection, so a
// client cannot forge a role. This is the pipe the Listener will later be
// allowed to tamper with - the shape does not change, only the server's
// behaviour between receive and deliver.
public class DFCommsRelay : NetworkBehaviour
{
    readonly List<string> _queue = new List<string>(); // server-side operator queue

    static readonly List<DFCommsUser> _users = new List<DFCommsUser>();

    public static void Register(DFCommsUser u) { if (!_users.Contains(u)) _users.Add(u); }
    public static void Unregister(DFCommsUser u) { _users.Remove(u); }

    static DFCommsUser FindUser(NetworkConnection conn)
    {
        foreach (var u in _users) if (u.Owner == conn) return u;
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdSendToOperator(string body, NetworkConnection sender = null)
    {
        var from = FindUser(sender);
        if (from == null || from.IsOperator) return;
        if (string.IsNullOrWhiteSpace(body)) return;
        body = body.Trim();
        if (body.Length > 240) body = body.Substring(0, 240);
        _queue.Add(body);
        foreach (var u in _users)
            if (u.IsOperator) u.TargetQueued(u.Owner, _queue.Count - 1, body);
        Debug.Log("[DF Comms] Queued for Operator: " + body);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdRelay(int queueIndex, int team, NetworkConnection sender = null)
    {
        var from = FindUser(sender);
        if (from == null || !from.IsOperator) return;
        if (queueIndex < 0 || queueIndex >= _queue.Count) return;
        var body = _queue[queueIndex];
        _queue.RemoveAt(queueIndex);
        foreach (var u in _users)
            if (!u.IsOperator && u.Team.Value == team)
                u.TargetDeliver(u.Owner, body);
        Debug.Log("[DF Comms] Relayed to Team " + (team == 0 ? "A" : "B") + ": " + body);
    }
}
