using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Alppikylän kiitotien kone: E koneen ovella avaa määränpäävalikon (Viidakko, Thaitown tai pysy Alppikylässä).
/// Matka tehdään piilotettujen Door-komponenttien kautta (ruutu pimenee, vanha kenttä vapautetaan). Viidakko vaatii viidakkolipun (Joulupukki pudottaa sen).
/// </summary>
public class PlaneDestination : MonoBehaviour
{
    public string prompt = "Nouse koneeseen";
    public Area here;
    [Tooltip("Reitti Thaitowniin (Door, jonka komponentti on pois päältä; GoNow siirtää).")]
    public Door toThaitown;
    [Tooltip("Reitti viidakkoon. Tyhjä = viidakkoa ei ole vielä tehty.")]
    public Door toJungle;
    public float halfWidth = 1.8f, maxDistanceFromWall = 2.5f;

    bool open; int sel; int openedFrame;
    float msgUntil; string msg = "";
    PlayerController pc;
    GUIStyle box, line;

    static readonly string[] Names = { "Viidakko", "Thaitown", "Pysy Alppikylässä" };

    void Update()
    {
        if (PokerService.BlocksCombat) return;
        if (open) { UpdateMenu(); return; }
        pc = Players.Nearest(transform.position);
        if (pc == null) return;
        if (Players.Count > 1)
            foreach (var q in Players.All) if (q != null && !q.GameOver && CoopInput.Pick(q.playerIndex)) { pc = q; break; }
        if (Near(pc) && CoopInput.Pick(pc.playerIndex))
        {
            open = true; sel = 0; openedFrame = Time.frameCount; msgUntil = 0f;
            SetAllEnabled(false);
        }
    }

    bool Near(PlayerController q)
    {
        Vector3 p = q.transform.position;
        float reach = Mathf.Min(maxDistanceFromWall, 1.0f);
        return !q.Riding && !q.HoldingLid && Mathf.Abs(p.x - transform.position.x) <= halfWidth
            && (here == null || p.y >= here.MaxDepthAtX(p.x, here.maxDepthY) - reach) && q.AirHeight <= 0.05f;
    }

    static void SetAllEnabled(bool on)
    {
        foreach (var p in Players.All) if (p != null) p.enabled = on;
    }

    void UpdateMenu()
    {
        if (Time.frameCount == openedFrame) return;
        if (Dir() != 0) sel = (sel + Dir() + Names.Length) % Names.Length;
        if (Cancel()) { Close(); return; }
        if (!(Confirm() || (pc != null && CoopInput.Pick(pc.playerIndex)))) return;
        if (sel == 2) { Close(); return; }
        Door route = sel == 0 ? toJungle : toThaitown;
        if (sel == 0 && !Clues.Open(Clues.JungleTicket)) { Say("Tarvitset viidakkolipun."); return; }
        if (route == null) { Say("Viidakko ei ole vielä kartalla."); return; }
        open = false;
        route.GoNow(pc);   // Go() palauttaa pelaajien ohjauksen perillä
    }

    void Close() { open = false; SetAllEnabled(true); }
    void Say(string s) { msg = s; msgUntil = Time.unscaledTime + 2.5f; }

    static int Dir()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; var g = Gamepad.current;
        if ((k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame)) return -1;
        if ((k != null && (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame)) return 1;
        return 0;
#else
        return Input.GetKeyDown(KeyCode.UpArrow) ? -1 : Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0;
#endif
    }

    static bool Confirm()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    static bool Cancel()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);
#endif
    }

    void OnGUI()
    {
        if (!open && !(pc != null && Near(pc) && !PokerService.BlocksCombat)) return;
        float s = Screen.height / 1080f;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            line = new GUIStyle(box) { alignment = TextAnchor.MiddleLeft };
        }
        if (!open)
        {
            float w0 = Screen.height * 0.45f, h0 = Screen.height * 0.065f;
            GUI.Box(new Rect((Screen.width - w0) * 0.5f, Screen.height * 0.82f, w0, h0), "[E]  " + Loc.T(prompt), box);
            return;
        }
        float w = 560f * s, rowH = 62f * s, h = rowH * (Names.Length + 1) + 30f * s;
        float x = (Screen.width - w) * 0.5f, y = (Screen.height - h) * 0.5f;
        GUI.Box(new Rect(x, y, w, h), "", box);
        GUI.Label(new Rect(x, y + 10f * s, w, rowH), Loc.T("Minne lennetään?"), box);
        for (int i = 0; i < Names.Length; i++)
        {
            string t = (i == sel ? "▶  " : "    ") + Loc.T(Names[i]);
            if (i == 0 && !Clues.Open(Clues.JungleTicket)) t += "  (lukossa)";
            GUI.Label(new Rect(x + 40f * s, y + 10f * s + rowH * (i + 1), w - 60f * s, rowH), t, line);
        }
        if (Time.unscaledTime < msgUntil)
            GUI.Box(new Rect(x, y + h + 10f * s, w, 60f * s), Loc.T(msg), box);
        else
            GUI.Label(new Rect(x, y + h + 6f * s, w, 40f * s), Loc.T("Ylös / alas valitse   E / Enter hyväksy   Esc takaisin"), box);
    }
}
