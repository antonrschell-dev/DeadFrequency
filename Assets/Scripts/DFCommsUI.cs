using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Dead Frequency greybox - comms panel (build step 3c).
// Press T to open the terminal: typing is captured straight from the Input
// System (no IMGUI text fields), Enter sends. Explorers send to the Operator
// and read their inbox; the Operator works the queue: number key picks a
// message, A or B relays it to that team. While the panel is open the player
// controller stands still (DFPlayerController checks CommsOpen).
public class DFCommsUI : MonoBehaviour
{
    public static bool CommsOpen;

    static DFCommsUI _inst;

    readonly List<string> _inbox = new List<string>();
    readonly List<string> _queue = new List<string>();
    string _draft = "";
    int _unread;
    DFCommsUser _me;
    DFCommsRelay _relay;
    float _findTimer;
    int _selected = -1;

    public static void OnDelivered(string body)
    {
        if (_inst == null) return;
        _inst._inbox.Insert(0, body);
        if (!CommsOpen) _inst._unread++;
    }
    public static void OnQueued(string body) { if (_inst != null) _inst._queue.Add(body); }

    void Awake() { _inst = this; }

    void Start()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDestroy()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
    }

    void OnTextInput(char c)
    {
        if (!CommsOpen || _me == null || _me.IsOperator) return;
        if (c == '\b') { if (_draft.Length > 0) _draft = _draft.Substring(0, _draft.Length - 1); }
        else if (c >= ' ' && _draft.Length < 240) _draft += c;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (_me == null)
        {
            _findTimer -= Time.deltaTime;
            if (_findTimer <= 0f)
            {
                _findTimer = 0.5f;
                foreach (var u in FindObjectsByType<DFCommsUser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (u.IsOwner) { _me = u; break; }
            }
            return;
        }
        if (_relay == null) _relay = FindAnyObjectByType<DFCommsRelay>();

        if (kb.tKey.wasPressedThisFrame)
        {
            CommsOpen = !CommsOpen;
            if (CommsOpen) _unread = 0;
            Cursor.lockState = CommsOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = CommsOpen;
        }
        if (!CommsOpen) return;
        if (kb.escapeKey.wasPressedThisFrame)
        {
            CommsOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        if (!_me.IsOperator)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_relay != null && _draft.Trim().Length > 0)
                {
                    _relay.CmdSendToOperator(_draft);
                    _draft = "";
                }
            }
        }
        else
        {
            var digits = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key,
                                 kb.digit6Key, kb.digit7Key, kb.digit8Key, kb.digit9Key };
            for (int i = 0; i < digits.Length; i++)
                if (digits[i].wasPressedThisFrame && i < _queue.Count) _selected = i;
            if (_selected >= 0 && _selected < _queue.Count)
            {
                if (kb.aKey.wasPressedThisFrame) RelaySelected(0);
                else if (kb.bKey.wasPressedThisFrame) RelaySelected(1);
            }
        }
    }

    void RelaySelected(int team)
    {
        if (_relay == null) return;
        _relay.CmdRelay(_selected, team);
        _queue.RemoveAt(_selected);
        _selected = -1;
    }

    Texture2D _bgTex;
    GUIStyle _labelStyle;

    void EnsureStyles()
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0.02f, 0.03f, 0.05f, 0.78f));
            _bgTex.Apply();
        }
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);
            _labelStyle.fontSize = 14;
            _labelStyle.wordWrap = true;
        }
    }

    void OnGUI()
    {
        if (_me == null) return;
        EnsureStyles();

        if (!CommsOpen)
        {
            var bar = new Rect(12, Screen.height - 46, 500, 34);
            GUI.DrawTexture(bar, _bgTex);
            string hint = _me.IsOperator ? "[T] COMMS - OPERATOR" : "[T] COMMS - TEAM " + (_me.Team.Value == 0 ? "A" : "B");
            if (_unread > 0) hint += "   ** " + _unread + " NEW **";
            GUI.Label(new Rect(24, Screen.height - 40, 480, 24), hint, _labelStyle);
            return;
        }

        var panel = new Rect(12, Screen.height - 330, 500, 318);
        GUI.DrawTexture(panel, _bgTex);
        GUILayout.BeginArea(new Rect(26, Screen.height - 316, 472, 292));
        if (!_me.IsOperator)
        {
            GUILayout.Label("EXPLORER - TEAM " + (_me.Team.Value == 0 ? "A" : "B") + "   (T / Esc to close)", _labelStyle);
            GUILayout.Label("> " + _draft + "_", _labelStyle);
            GUILayout.Label("Enter = send to Operator", _labelStyle);
            GUILayout.Space(6);
            GUILayout.Label("INBOX (source hidden by design):", _labelStyle);
            if (_inbox.Count == 0) GUILayout.Label("(nothing received yet)", _labelStyle);
            for (int i = 0; i < _inbox.Count && i < 6; i++) GUILayout.Label("- " + _inbox[i], _labelStyle);
        }
        else
        {
            GUILayout.Label("OPERATOR - RELAY QUEUE   (T / Esc to close)", _labelStyle);
            if (_queue.Count == 0) GUILayout.Label("(queue empty)", _labelStyle);
            for (int i = 0; i < _queue.Count && i < 9; i++)
                GUILayout.Label((i == _selected ? "> " : "  ") + (i + 1) + ". " + _queue[i], _labelStyle);
            GUILayout.Label("Number = select, then A = relay to Team A, B = Team B", _labelStyle);
        }
        GUILayout.EndArea();
    }
}
