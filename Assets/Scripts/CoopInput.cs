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
        return Pads[Mathf.Clamp(i, 0, 1)];
    }

    static bool KbOwns(int i) => i == 0 || P2OnKeyboard;

    static bool Down(Keyboard k, Key a, Key b = Key.None) => k != null && (k[a].isPressed || (b != Key.None && k[b].isPressed));
    static bool Hit(Keyboard k, Key a, Key b = Key.None) => k != null && (k[a].wasPressedThisFrame || (b != Key.None && k[b].wasPressedThisFrame));

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
            Vector2 stick = g.leftStick.ReadValue();
            if (stick.magnitude > 0.25f) v += stick;
            v += g.dpad.ReadValue();
        }
        return Vector2.ClampMagnitude(v, 1f);
    }

    // P2:n näppäimistöasettelu: numeronäppäimistö tai oikean käden näppäimet (kannettavissa).
    public static bool Jump(int i) => Pressed(i, Key.Space, Key.None, Key.Numpad0, Key.RightCtrl, g => g.buttonSouth);
    public static bool Punch(int i) => Pressed(i, Key.J, Key.None, Key.Numpad1, Key.Comma, g => g.buttonWest);
    public static bool Kick(int i) => Pressed(i, Key.K, Key.None, Key.Numpad2, Key.Period, g => g.buttonNorth);
    public static bool Special(int i) => Pressed(i, Key.L, Key.None, Key.Numpad3, Key.Slash, g => g.rightStickButton);
    public static bool Push(int i) => Pressed(i, Key.U, Key.None, Key.Numpad4, Key.Semicolon, g => g.rightTrigger);
    /// E / ympyrä: ei toimi, kun pelaaja on kaatuneen kumppanin vieressä (silloin E nostaa kumppanin ja ohittaa kaiken muun: ovet, tiski, kansi, poiminta).
    public static bool Pick(int i) => !Players.NearFallen(i) && PickRaw(i);
    public static bool PickRaw(int i) => Pressed(i, Key.E, Key.None, Key.Numpad5, Key.Quote, g => g.buttonEast);
    public static bool Catch(int i) => Pressed(i, Key.O, Key.None, Key.Numpad6, Key.RightAlt, g => g.leftTrigger);

    public static bool PickHeld(int i) => !Players.NearFallen(i) && PickHeldRaw(i);
    public static bool PickHeldRaw(int i) => Held(i, Key.E, Key.None, Key.Numpad5, Key.Quote, g => g.buttonEast);
    public static bool Block(int i) => Held(i, Key.I, Key.None, Key.Numpad7, Key.Backslash, g => g.leftShoulder);

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
        return g != null && g.rightShoulder.isPressed;
    }

    static bool Pressed(int i, Key p1a, Key p1b, Key p2a, Key p2b, System.Func<Gamepad, UnityEngine.InputSystem.Controls.ButtonControl> btn)
    {
        var k = Keyboard.current;
        if (i == 0 && Hit(k, p1a, p1b)) return true;
        if (i == 1 && P2OnKeyboard && Hit(k, p2a, p2b)) return true;
        var g = PadFor(i);
        return g != null && btn(g).wasPressedThisFrame;
    }

    static bool Held(int i, Key p1a, Key p1b, Key p2a, Key p2b, System.Func<Gamepad, UnityEngine.InputSystem.Controls.ButtonControl> btn)
    {
        var k = Keyboard.current;
        if (i == 0 && Down(k, p1a, p1b)) return true;
        if (i == 1 && P2OnKeyboard && Down(k, p2a, p2b)) return true;
        var g = PadFor(i);
        return g != null && btn(g).isPressed;
    }
}
