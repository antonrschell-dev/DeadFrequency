using System.Collections.Generic;
using UnityEngine;

// Dead Frequency greybox - door behaviour (build step 2), v4 hardware hinge.
// The kit leaf's origin is its box CENTRE, not the hinge - rotating the node alone
// sweeps it like a turnstile (Anton's playtest, Nov 6). The hinge line is read from
// the model's own Hinge_T / Hinge_B hardware: their world positions, converted into
// leaf-parent space, are immune to whatever axis layout the FBX import produced.
// (Fallback: leaf centre minus HingeOffset on local x - the v2 approximation.)
// The prefab's "Frame" is a single solid slab in the kit data and would keep the
// doorway blocked forever, so its renderer is disabled - the Wall_Door kit piece
// already provides the visible frame.
public class DFDoor : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public float OpenAngle = -105f;
    public float Speed = 2.2f;
    public float HingeOffset = 0.47f; // leaf centre distance from the hinge line

    static readonly string[] MovingNames = { "DoorLeaf", "Porthole", "HandleBar", "Wheel", "Stripe" };

    class Part
    {
        public Transform T;
        public Vector3 P0;
        public Quaternion Q0;
    }

    readonly List<Part> _parts = new List<Part>();
    BoxCollider _blocker;
    Vector3 _hinge;
    bool _hasHinge;
    float _t; // 0 = closed, 1 = open

    void Awake()
    {
        Transform leaf = null;
        foreach (var tr in GetComponentsInChildren<Transform>())
        {
            if (tr.name == "Frame")
            {
                var mr = tr.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
            if (System.Array.IndexOf(MovingNames, tr.name) >= 0)
            {
                _parts.Add(new Part { T = tr, P0 = tr.localPosition, Q0 = tr.localRotation });
                if (tr.name == "DoorLeaf") leaf = tr;
            }
        }
        Transform hingeMark = null;
        foreach (var tr in GetComponentsInChildren<Transform>())
            if (tr.name == "Hinge_T" || tr.name == "Hinge_B") { hingeMark = tr; break; }
        if (leaf != null)
        {
            var lp = leaf.localPosition;
            if (hingeMark != null && leaf.parent != null)
            {
                var hp = leaf.parent.InverseTransformPoint(hingeMark.position);
                _hinge = new Vector3(hp.x, lp.y, hp.z);
                _hasHinge = true;
            }
            else
            {
                _hinge = new Vector3(lp.x - HingeOffset, lp.y, lp.z);
                _hasHinge = true;
            }
            Debug.Log("[DFDoor] " + name + " leaf lp=" + lp + " hinge=" + _hinge + " (hardware: " + (hingeMark != null) + ")");
        }
        _blocker = GetComponent<BoxCollider>();
    }

    public void Toggle() { IsOpen = !IsOpen; }

    void Update()
    {
        _t = Mathf.MoveTowards(_t, IsOpen ? 1f : 0f, Speed * Time.deltaTime);
        if (_hasHinge)
        {
            var r = Quaternion.Euler(0f, OpenAngle * _t, 0f);
            foreach (var p in _parts)
            {
                p.T.localPosition = _hinge + r * (p.P0 - _hinge);
                p.T.localRotation = r * p.Q0;
            }
        }
        if (_blocker != null) _blocker.enabled = _t < 0.5f;
    }
}
