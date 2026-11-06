using UnityEngine;
using UnityEngine.InputSystem;

// Dead Frequency greybox - first-person explorer controller (build step 2; comms gate added step 3c).
// WASD + mouse look (click in Play to pointer-lock, Esc releases), gravity via CharacterController.
// Uses the Input System direct device API (no input assets needed).
[RequireComponent(typeof(CharacterController))]
public class DFPlayerController : MonoBehaviour
{
    public float WalkSpeed = 3.2f;
    public float LookSensitivity = 0.12f;

    CharacterController _cc;
    Transform _head;
    float _pitch;
    float _vy;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _head = transform.Find("Head");
    }

    void Update()
    {
        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null || ms == null) return;
        if (DFCommsUI.CommsOpen) return; // typing at the comms terminal: stand still

        if (ms.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            var d = ms.delta.ReadValue();
            transform.Rotate(0f, d.x * LookSensitivity, 0f);
            if (_head != null)
            {
                _pitch = Mathf.Clamp(_pitch - d.y * LookSensitivity, -85f, 85f);
                _head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        float f = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        float r = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        var move = transform.forward * f + transform.right * r;
        if (move.sqrMagnitude > 1f) move.Normalize();

        _vy = _cc.isGrounded ? -1f : _vy - 18f * Time.deltaTime;
        var vel = move * WalkSpeed;
        vel.y = _vy;
        _cc.Move(vel * Time.deltaTime);
    }
}
