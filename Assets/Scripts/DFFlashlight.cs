using UnityEngine;
using UnityEngine.InputSystem;

// Dead Frequency greybox - flashlight toggle (build step 2). F key.
public class DFFlashlight : MonoBehaviour
{
    Light _spot;

    void Awake() { _spot = GetComponentInChildren<Light>(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || _spot == null) return;
        if (kb.fKey.wasPressedThisFrame) _spot.enabled = !_spot.enabled;
    }
}
