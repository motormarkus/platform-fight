using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Pelaajakohtainen syöte. Yksinpelissä (P2 ei ole mukana) toimii kuten ennen: näppäimistö + viimeksi käytetty ohjain.
/// Kaksinpelissä P1 = ohjain 1 + WASD, P2 = ohjain 2 (tai näppäimistön nuolet ja oikean käden näppäimet, jos P2 liittyi F2:lla).
/// </summary>
public static class CoopInput
{
    /// P2 pelaa näppäimistöllä (nuolet ym.), ei ohjaimella.
    public static bool P2OnKeyboard;
    /// Pelaajan ohjain kaksinpelissä (0 = P1, 1 = P2).
    public static Gamepad[] Pads = new Gamepad[2];
    /// Ohjain, jota P1 on käyttänyt (yksinpelissä, ennen P2:n liittymistä).
    public static Gamepad P1Pad;
    /// Kehys, jona P2 liittyi (P2:n Start ei saa avata taukovalikkoa).
    public static int JoinFrame = -10;

    static bool Joined => Players.Count > 1;

    public static Gamepad PadFor(int i)
    {
        if (!Joined) return i == 0 ? Gamepad.current : null;
        i = Mathf.Clamp(i, 0, 1);
        var g = Pads[i];
        if (g != null && g.added) return g;
        // ei ohjainta tai ohjain on irronnut/liitetty uudelleen: etsi uusi
        // P1: viimeksi käytetty ohjain (myös kun P2 pelaa näppäimistöllä); P2: jokin muu kuin P1:n ohjain (ei, jos P2 on näppäimistöllä)
        var all = Gamepad.all;
        if (i == 0)
        {
            g = P1Pad != null && P1Pad.added ? P1Pad : Gamepad.current;
            if (g == null && all.Count > 0) g = all[0];
            if (g != null && !P2OnKeyboard && all.Count >= 2 && Pads[1] == g) { g = null; foreach (var o in all) if (o != Pads[1]) { g = o; break; } }
        }
        else if (!P2OnKeyboard)
        {
            var p1 = Pads[0] != null && Pads[0].added ? Pads[0] : (P1Pad != null && P1Pad.added ? P1Pad : Gamepad.current);
            g = null;
            foreach (var o in all) if (o != p1) { g = o; break; }
        }
        else g = null;
        if (g != null) Pads[i] = g;
        return g;
    }

    static bool KbOwns(int i) => i == 0 || P2OnKeyboard;

    static bool Down(Keyboard k, Key a, Key b = Key.None) => k != null && (k[a].isPressed || (b != Key.None && k[b].isPressed));
    static bool Hit(Keyboard k, Key a, Key b = Key.None) => k != null && (k[a].wasPressedThisFrame || (b != Key.None && k[b].wasPressedThisFrame));

    // rikkinäinen tikku (jää keskelle muttei palaa nollaan): jos arvo pysyy täsmälleen samana yli 3 s, se ohitetaan, kunnes tikku liikkuu uudelleen
    static readonly System.Collections.Generic.Dictionary<int, Vector3> stickMemory = new System.Collections.Generic.Dictionary<int, Vector3>();   // x, y, aika jolloin arvo viimeksi muuttui

    static Vector2 StickNoDrift(Gamepad g)
    {
        Vector2 cur = g.leftStick.ReadValue();
        float now = Time.unscaledTime;
        if (!stickMemory.TryGetValue(g.deviceId, out Vector3 m)) m = new Vector3(cur.x, cur.y, now);
        if ((new Vector2(m.x, m.y) - cur).sqrMagnitude > 0.0001f) m = new Vector3(cur.x, cur.y, now);   // muutos yli 0,01
        stickMemory[g.deviceId] = m;
        if (cur.magnitude > 0.25f && now - m.z > 3f) return Vector2.zero;
        return cur;
    }

    public static Vector2 Move(int i)
    {
        Vector2 v = Vector2.zero;
        var k = Keyboard.current;
        if (k != null)
        {
            if (i == 0)
            {
                bool arrows = !(Joined && P2OnKeyboard);
                if (k.aKey.isPressed || (arrows && k.leftArrowKey.isPressed)) v.x -= 1;
                if (k.dKey.isPressed || (arrows && k.rightArrowKey.isPressed)) v.x += 1;
                if (k.sKey.isPressed || (arrows && k.downArrowKey.isPressed)) v.y -= 1;
                if (k.wKey.isPressed || (arrows && k.upArrowKey.isPressed)) v.y += 1;
            }
            else if (P2OnKeyboard)
            {
                if (k.leftArrowKey.isPressed) v.x -= 1;
                if (k.rightArrowKey.isPressed) v.x += 1;
                if (k.downArrowKey.isPressed) v.y -= 1;
                if (k.upArrowKey.isPressed) v.y += 1;
            }
        }
        var g = PadFor(i);
        if (g != null)
        {
            Vector2 stick = StickNoDrift(g);
            if (stick.magnitude > 0.25f) v += stick;
            v += g.dpad.ReadValue();
        }
        return Vector2.ClampMagnitude(v, 1f);
    }

    // P2:n näppäimistöasettelu: numeronäppäimistö tai oikean käden näppäimet (kannettavissa).
    public static bool Jump(int i) => Pressed(i, Key.Space, Key.None, Key.Numpad0, Key.RightCtrl, Act.Jump);
    public static bool JumpHeld(int i) => Held(i, Key.Space, Key.None, Key.Numpad0, Key.RightCtrl, Act.Jump);
    public static bool Punch(int i) => Pressed(i, Key.J, Key.None, Key.Numpad1, Key.Comma, Act.Punch);
    public static bool PunchHeld(int i) => Held(i, Key.J, Key.None, Key.Numpad1, Key.Comma, Act.Punch);
    public static bool Kick(int i) => Pressed(i, Key.K, Key.None, Key.Numpad2, Key.Period, Act.Kick);
    public static bool Special(int i) => Pressed(i, Key.L, Key.None, Key.Numpad3, Key.Slash, Act.Special);
    public static bool Push(int i) => Pressed(i, Key.U, Key.None, Key.Numpad4, Key.Semicolon, Act.Push);
    /// E / toimintanappi: ei toimi, kun pelaaja on kaatuneen kumppanin vieressä (silloin E nostaa kumppanin ja ohittaa kaiken muun: ovet, tiski, kansi, poiminta).
    public static bool Pick(int i) => !Players.NearFallen(i) && PickRaw(i);
    public static bool PickRaw(int i) => Pressed(i, Key.E, Key.None, Key.Numpad5, Key.Quote, Act.Pick);
    public static bool Catch(int i) => Pressed(i, Key.O, Key.None, Key.Numpad6, Key.RightAlt, Act.Catch);

    public static bool PickHeld(int i) => !Players.NearFallen(i) && PickHeldRaw(i);
    public static bool PickHeldRaw(int i) => Held(i, Key.E, Key.None, Key.Numpad5, Key.Quote, Act.Pick);
    public static bool Block(int i) => Held(i, Key.I, Key.None, Key.Numpad7, Key.Backslash, Act.Block);

    public static bool Run(int i)
    {
        var k = Keyboard.current;
        if (i == 0)
        {
            bool right = !(Joined && P2OnKeyboard);
            if (k != null && (k.leftShiftKey.isPressed || (right && k.rightShiftKey.isPressed))) return true;
        }
        else if (P2OnKeyboard && k != null && k.rightShiftKey.isPressed) return true;
        var g = PadFor(i);
        return g != null && Btn(g, i, Act.Run).isPressed;
    }

    static bool Pressed(int i, Key p1a, Key p1b, Key p2a, Key p2b, Act act)
    {
        var k = Keyboard.current;
        if (i == 0 && Hit(k, p1a, p1b)) return true;
        if (i == 1 && P2OnKeyboard && Hit(k, p2a, p2b)) return true;
        var g = PadFor(i);
        return g != null && Btn(g, i, act).wasPressedThisFrame;
    }

    static bool Held(int i, Key p1a, Key p1b, Key p2a, Key p2b, Act act)
    {
        var k = Keyboard.current;
        if (i == 0 && Down(k, p1a, p1b)) return true;
        if (i == 1 && P2OnKeyboard && Down(k, p2a, p2b)) return true;
        var g = PadFor(i);
        return g != null && Btn(g, i, act).isPressed;
    }

    // ---------------- Ohjaimen nappien määritys (pelaajakohtainen, tallentuu PlayerPrefsiin) ----------------

    /// Toiminnot, joille ohjaimen napin voi vaihtaa. Liike (tikku / ristiohjain) ja Start ovat kiinteät.
    public enum Act { Jump, Punch, Kick, Special, Push, Pick, Catch, Block, Run }
    /// Ohjaimen napit, joihin toiminnon voi sitoa (neljä etunappia numeroituna 1–4, kaksi olkanappia, kaksi liipaisinta ja tikkujen painallukset).
    public enum PadBtn { South, East, West, North, L1, R1, L2, R2, L3, R3 }

    public static readonly Act[] AllActs = { Act.Punch, Act.Kick, Act.Jump, Act.Run, Act.Special, Act.Push, Act.Pick, Act.Catch, Act.Block };

    /// Oletusasettelu: nappi 1 lyönti, 2 potku, 3 hyppy, oikea olkanappi juoksu, oikea tikku erikoisliike, oikea liipaisin pusku, nappi 4 poiminta, vasen liipaisin nappaus, vasen olkanappi suojaus.
    public static PadBtn DefaultBinding(Act a)
    {
        switch (a)
        {
            case Act.Jump: return PadBtn.South;
            case Act.Punch: return PadBtn.West;
            case Act.Kick: return PadBtn.North;
            case Act.Special: return PadBtn.R3;
            case Act.Push: return PadBtn.R2;
            case Act.Pick: return PadBtn.East;
            case Act.Catch: return PadBtn.L2;
            case Act.Block: return PadBtn.L1;
            default: return PadBtn.R1;   // Run
        }
    }

    static readonly PadBtn[][] bindings = new PadBtn[2][];

    static string Key2(int player, Act a) => "nappi" + player + "_" + (int)a;

    static PadBtn[] Table(int player)
    {
        player = Mathf.Clamp(player, 0, 1);
        if (bindings[player] == null)
        {
            var t = new PadBtn[System.Enum.GetValues(typeof(Act)).Length];
            foreach (Act a in System.Enum.GetValues(typeof(Act)))
                t[(int)a] = (PadBtn)PlayerPrefs.GetInt(Key2(player, a), (int)DefaultBinding(a));
            bindings[player] = t;
        }
        return bindings[player];
    }

    public static PadBtn GetBinding(int player, Act a) => Table(player)[(int)a];

    /// Sitoo toiminnon nappiin. Jos nappi on jo toisella toiminnolla, toiminnot vaihtavat paikkaa (ei kahta toimintoa samalle napille).
    public static void SetBinding(int player, Act a, PadBtn b)
    {
        var t = Table(player);
        PadBtn old = t[(int)a];
        foreach (Act o in System.Enum.GetValues(typeof(Act)))
            if (o != a && t[(int)o] == b) { t[(int)o] = old; PlayerPrefs.SetInt(Key2(player, o), (int)old); }
        t[(int)a] = b;
        PlayerPrefs.SetInt(Key2(player, a), (int)b);
        PlayerPrefs.Save();
    }

    public static void ResetBindings(int player)
    {
        player = Mathf.Clamp(player, 0, 1);
        foreach (Act a in System.Enum.GetValues(typeof(Act))) PlayerPrefs.DeleteKey(Key2(player, a));
        bindings[player] = null;
        PlayerPrefs.Save();
    }

    /// Napin nimi pelaajalle: etunapit numeroituina (1 vasen, 2 ylä, 3 ala, 4 oikea), muut asennon mukaan. Ei merkkien tai valmistajien nimiä.
    public static string PadBtnName(PadBtn b)
    {
        switch (b)
        {
            case PadBtn.South: return "Nappi 3";
            case PadBtn.East: return "Nappi 4";
            case PadBtn.West: return "Nappi 1";
            case PadBtn.North: return "Nappi 2";
            case PadBtn.L1: return "Vasen olkanappi";
            case PadBtn.R1: return "Oikea olkanappi";
            case PadBtn.L2: return "Vasen liipaisin";
            case PadBtn.R2: return "Oikea liipaisin";
            case PadBtn.L3: return "Vasen tikku (paina)";
            default: return "Oikea tikku (paina)";
        }
    }

    /// Ohjaimen nappi (ohjain, pelaaja, toiminto) -> ohjauselementti.
    public static UnityEngine.InputSystem.Controls.ButtonControl Btn(Gamepad g, int player, Act a)
    {
        switch (GetBinding(player, a))
        {
            case PadBtn.South: return g.buttonSouth;
            case PadBtn.East: return g.buttonEast;
            case PadBtn.West: return g.buttonWest;
            case PadBtn.North: return g.buttonNorth;
            case PadBtn.L1: return g.leftShoulder;
            case PadBtn.R1: return g.rightShoulder;
            case PadBtn.L2: return g.leftTrigger;
            case PadBtn.R2: return g.rightTrigger;
            case PadBtn.L3: return g.leftStickButton;
            default: return g.rightStickButton;
        }
    }

    /// Mitä ohjaimen nappia pelaaja painoi tällä hetkellä (uuden napin odotus määrityssivulla). Null, jos mitään ei painettu.
    public static PadBtn? ReadPressedPadBtn(int player)
    {
        var g = PadFor(player);
        if (g == null) return null;
        if (g.buttonSouth.wasPressedThisFrame) return PadBtn.South;
        if (g.buttonEast.wasPressedThisFrame) return PadBtn.East;
        if (g.buttonWest.wasPressedThisFrame) return PadBtn.West;
        if (g.buttonNorth.wasPressedThisFrame) return PadBtn.North;
        if (g.leftShoulder.wasPressedThisFrame) return PadBtn.L1;
        if (g.rightShoulder.wasPressedThisFrame) return PadBtn.R1;
        if (g.leftTrigger.wasPressedThisFrame) return PadBtn.L2;
        if (g.rightTrigger.wasPressedThisFrame) return PadBtn.R2;
        if (g.leftStickButton.wasPressedThisFrame) return PadBtn.L3;
        if (g.rightStickButton.wasPressedThisFrame) return PadBtn.R3;
        return null;
    }
}
