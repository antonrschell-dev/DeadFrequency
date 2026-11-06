using FishNet.Object;
using UnityEngine;

// Dead Frequency greybox - networked player gate (build step 3b).
// PF_Player instances are spawned by FishNet's PlayerSpawner for each connection.
// Only the OWNER gets a live camera, listener and controls; remote copies are
// puppets driven by NetworkTransform (client-authoritative by default in
// FishNet 4.7.4), with their CharacterController disabled so it cannot fight
// the replicated transform.
public class DFNetPlayer : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        bool mine = IsOwner;

        var head = transform.Find("Head");
        if (head != null)
        {
            var cam = head.GetComponent<Camera>();
            if (cam != null) cam.enabled = mine;
            var lis = head.GetComponent<AudioListener>();
            if (lis != null) lis.enabled = mine;
        }

        var ctl = GetComponent<DFPlayerController>();
        if (ctl != null) ctl.enabled = mine;
        var inter = GetComponent<DFInteract>();
        if (inter != null) inter.enabled = mine;
        var flash = GetComponent<DFFlashlight>();
        if (flash != null) flash.enabled = mine;
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = mine;
    }
}
