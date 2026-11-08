using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Hissi: hissin eteen mentäessä E avaa kerrosvalinnan (1 aula, 3 asunnot, Kellari). Oman kerroksen valinta = pysyy paikallaan.
/// Siirtymät tekevät piilotetut ovet (Door.manualOnly), jotka on luotu valikossa 341: floors[i] vie kerrokseen i (oman kerroksen kohta tyhjä).
/// Kellari on lukossa, kunnes LaundryQuest.Unlocked on päällä.
/// </summary>
public class ElevatorPanel : MonoBehaviour
{
    public Area here;
    public Door[] floors = new Door[3];
    public int current;
    public float halfWidth = 1.6f;
    public float maxDistanceFromWall = 3f;

    static readonly string[] Labels = { "1  Aula", "3  Asunnot", "B  Kellari" };
    PlayerController who;
    bool near, open;
    int sel;
    float msgUntil;
    GUIStyle box, line;

    static bool Down(int which)
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; var g = Gamepad.current;
        switch (which)
        {
            case 0: return (k != null && (k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame);
            case 1: return (k != null && (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame);
            case 2: return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
            default: return (k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)) || (g != null && g.buttonEast.wasPressedThisFrame);
        }
#else
        switch (which)
        {
            case 0: return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            case 1: return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            case 2: return Input.GetKeyDown(KeyCode.Return);
            default: return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);
        }
#endif
    }

    bool NearOk(PlayerController p)
    {
        Vector3 q = p.transform.position;
        return !p.Riding && !p.GameOver && Mathf.Abs(q.x - transform.position.x) <= halfWidth
            && (here == null || q.y >= here.MaxDepthAtX(q.x, here.maxDepthY) - Mathf.Min(maxDistanceFromWall, 1.0f));
    }

    void Update()
    {
        if (open)
        {
            SetScripted(true);
            if (Down(0)) sel = (sel + Labels.Length - 1) % Labels.Length;
            else if (Down(1)) sel = (sel + 1) % Labels.Length;
            else if (Down(3)) Close();
            else if (Down(2) || (who != null && CoopInput.Pick(who.playerIndex))) Choose();
            return;
        }
        near = false; who = null;
        if (here == null || Area.Current != here || ScreenFader.Alpha > 0f) return;
        foreach (var p in Players.All)
        {
            if (p == null || p.Scripted || !NearOk(p)) continue;
            near = true; who = p;
            if (CoopInput.Pick(p.playerIndex)) { open = true; sel = current; return; }
            break;
        }
    }

    void Choose()
    {
        if (sel == current) { Close(); return; }
        if (sel == 2 && !LaundryQuest.Unlocked) { msgUntil = Time.unscaledTime + 2f; return; }
        var d = sel >= 0 && sel < floors.Length ? floors[sel] : null;
        var p = who;
        Close();
        if (d != null && p != null) d.Trigger(p);
    }

    void Close() { open = false; SetScripted(false); }

    static void SetScripted(bool on)
    {
        foreach (var pc in Players.All) if (pc != null) { pc.Scripted = on; if (on) pc.ScriptedMove = Vector2.zero; }
    }

    void OnDisable() { if (open) { open = false; SetScripted(false); } }

    void OnGUI()
    {
        if (!open && !near) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            line = new GUIStyle(box) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(Mathf.RoundToInt(Screen.height * 0.03f), 0, 0, 0) };
        }
        if (!open)
        {
            float pw = Screen.height * 0.45f, ph = Screen.height * 0.065f;
            GUI.Box(new Rect((Screen.width - pw) * 0.5f, Screen.height * 0.82f, pw, ph), "[E]  " + Loc.T("Hissi"), box);
            return;
        }
        float w = Screen.height * 0.5f, lh = Screen.height * 0.065f, h = lh * (Labels.Length + 1) + Screen.height * 0.02f;
        float x = (Screen.width - w) * 0.5f, y = Screen.height * 0.5f - h * 0.5f;
        GUI.Box(new Rect(x, y, w, h), "", box);
        GUI.Label(new Rect(x, y, w, lh), Loc.T("Valitse kerros"), box);
        for (int i = 0; i < Labels.Length; i++)
        {
            bool locked = i == 2 && !LaundryQuest.Unlocked;
            string t = (i == sel ? "\u25B6  " : "     ") + Labels[i] + (i == current ? "  (" + Loc.T("täällä") + ")" : locked ? "  (" + Loc.T("lukossa") + ")" : "");
            line.normal.textColor = i == sel ? new Color(1f, 0.85f, 0.2f) : (locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
            GUI.Label(new Rect(x, y + lh * (i + 1), w, lh), t, line);
        }
        if (Time.unscaledTime < msgUntil)
            GUI.Label(new Rect(x, y + h, w, lh), Loc.T("Kellarin hissipainike ei toimi."), box);
    }
}
