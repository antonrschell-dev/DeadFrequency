using UnityEngine;
using UnityEngine.InputSystem;

// Dead Frequency greybox - interaction raycast (build step 2).
// Press E while looking at a door (within range) to toggle it.
public class DFInteract : MonoBehaviour
{
    public float Range = 2.6f;

    Camera _cam;

    void Start() { _cam = GetComponentInChildren<Camera>(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || _cam == null) return;
        if (!kb.eKey.wasPressedThisFrame) return;

        RaycastHit hit;
        var ray = new Ray(_cam.transform.position, _cam.transform.forward);
        if (Physics.Raycast(ray, out hit, Range))
        {
            var door = hit.collider.GetComponentInParent<DFDoor>();
            if (door != null) door.Toggle();
        }
    }
}
