using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Kaksinpelin ohjaus: P2 liittyy toisen ohjaimen Startilla (tai F2:lla näppäimistölle), kaatunut pelaaja herää
/// kumppanin viereen, kun kumppani on selvinnyt tarpeeksi pitkään. Luodaan itsestään (ei tarvitse scene-muutoksia).
/// </summary>
public class CoopManager : MonoBehaviour
{
    /// Kuinka kauan E:tä pidetään pohjassa, ja kuinka suuri osa energiapalkista kaatuneelle palautuu.
    public const float ReviveTime = 1.6f;
    public const float ReviveHealthFraction = 0.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<CoopManager>() != null) return;
        var go = new GameObject("CoopManager");
        DontDestroyOnLoad(go);
        go.AddComponent<CoopManager>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f4Key.wasPressedThisFrame) padDebug = !padDebug;
        var p1 = Players.Primary;
        if (p1 == null) return;

        if (Players.Count <= 1)
        {
            TrackP1Pad();
            if (Players.CoopWanted)
            {
                // uusi peli tai uusi scene: P2 takaisin heti, kun P1 on valmis
                if (Players.Template != null && !p1.GameOver) Players.SpawnSecond();
                return;
            }
            if (GameMenu.IsOpen || p1.GameOver) return;
            // kaksinpeliin liitytään vain päävalikon Pelaajia-valinnalla (ei kesken pelin Startilla tai F2:lla)
            return;
        }

        UpdatePadAssignment();

        // kaksinpeli: kaatunut pelaaja nostetaan pitämällä E / toimintanappia pohjassa hänen vieressään
        foreach (var d in Players.All)
        {
            if (d == null || !d.GameOver) continue;
            bool reviving = false;
            foreach (var a in Players.All)
                if (a != null && !a.GameOver && Players.FallenNear(a) == d && CoopInput.PickHeldRaw(a.playerIndex)) { reviving = true; break; }
            float prev = reviveProgress.TryGetValue(d, out float v) ? v : 0f;
            if (reviving) prev += Time.deltaTime;
            else prev = Mathf.Max(0f, prev - Time.deltaTime * 2f);
            if (prev >= ReviveTime)
            {
                reviveProgress.Remove(d);
                d.Revive(Mathf.Max(1, Mathf.RoundToInt(d.maxHealth * ReviveHealthFraction)), d.transform.position);
                HitFx.PlayPickup(true);
            }
            else reviveProgress[d] = prev;
        }
    }

    readonly System.Collections.Generic.Dictionary<PlayerController, float> reviveProgress = new System.Collections.Generic.Dictionary<PlayerController, float>();

    GUIStyle reviveStyle;

    readonly System.Collections.Generic.Dictionary<int, float> padActive = new System.Collections.Generic.Dictionary<int, float>();

    static bool PadInput(Gamepad g)
    {
        return g.startButton.wasPressedThisFrame || g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame
            || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame || g.leftTrigger.wasPressedThisFrame || g.rightTrigger.wasPressedThisFrame
            || g.leftStick.ReadValue().magnitude > 0.6f || g.dpad.ReadValue().magnitude > 0.5f;
    }

    /// Kaksinpelissä: ohjain, jota ei ole annettu kummallekaan pelaajalle, mutta jota painetaan, korvaa sen pelaajan ohjaimen,
    /// jonka ohjaimella ei ole kahteen sekuntiin tapahtunut mitään (esim. kaksoislaite tai väärä ohjain). P2 siirtyy näppäimistöltä ohjaimelle.
    void UpdatePadAssignment()
    {
        var all = Gamepad.all;
        Gamepad p1 = CoopInput.PadFor(0), p2 = CoopInput.P2OnKeyboard ? null : CoopInput.PadFor(1);
        if (!CoopInput.P2OnKeyboard && p2 == null) CoopInput.P2OnKeyboard = true;   // P2:n ohjain on poissa: P2 pelaa näppäimistöllä
        if (all.Count == 0) return;
        foreach (var g in all)
        {
            if (!PadInput(g)) continue;
            padActive[g.deviceId] = Time.unscaledTime;
            if (g == p1 || g == p2) continue;
            float a1 = p1 != null && padActive.TryGetValue(p1.deviceId, out float t1) ? t1 : -99f;
            float a2 = p2 != null && padActive.TryGetValue(p2.deviceId, out float t2) ? t2 : -99f;
            if (CoopInput.P2OnKeyboard || p2 == null || Time.unscaledTime - a2 > 2f)
            {
                CoopInput.Pads[0] = p1; CoopInput.Pads[1] = g; CoopInput.P2OnKeyboard = false; break;
            }
            if (p1 == null || Time.unscaledTime - a1 > 2f)
            {
                CoopInput.Pads[0] = g; CoopInput.Pads[1] = p2; break;
            }
        }
    }

    bool padDebug;
    GUIStyle padStyle;

    /// F4: listaa kytketyt laitteet ja kertoo, mikä ohjain on P1 ja mikä P2 (vianetsintään).
    void DrawPadDebug()
    {
        if (padStyle == null) padStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = false };
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("OHJAINTEN TILA (F4 sulkee)   pelaajia: " + Players.Count + "   P2 näppäimistöllä: " + CoopInput.P2OnKeyboard);
        var p1 = CoopInput.PadFor(0); var p2 = CoopInput.PadFor(1);
        int gi = 0;
        foreach (var g in Gamepad.all)
        {
            string role = g == p1 ? "P1" : g == p2 ? "P2" : "-";
            sb.AppendLine("Ohjain " + gi + ": [" + role + "] " + g.displayName + "  (" + g.GetType().Name + ", id " + g.deviceId + ", " + g.description.interfaceName + ", sarja " + g.description.serial + ")  tikku " + g.leftStick.ReadValue().ToString("F1") + (g.buttonSouth.isPressed || g.buttonEast.isPressed || g.buttonWest.isPressed || g.buttonNorth.isPressed ? "  NAPPI" : ""));
            gi++;
        }
        if (gi == 0) sb.AppendLine("Ei yhtään Gamepad-laitetta.");
        sb.AppendLine("Muut syötelaitteet (ei Gamepad):");
        foreach (var d in InputSystem.devices)
        {
            if (d is Gamepad || d is Keyboard || d is Mouse) continue;
            if (d is UnityEngine.InputSystem.Pointer || d is UnityEngine.InputSystem.Sensor) continue;
            sb.AppendLine("  " + d.displayName + "  (" + d.layout + ")");
        }
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(10, 10, 1100, 24 * (sb.ToString().Split('\n').Length + 1)), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 14, 1080, 900), sb.ToString(), padStyle);
    }

    void OnGUI()
    {
        if (padDebug) DrawPadDebug();
        if (Players.Count < 2) return;
        var cam = Camera.main;
        if (cam == null) return;
        float s = Mathf.Min(Screen.height / 1080f, Screen.width / 1920f);
        if (reviveStyle == null) reviveStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
        reviveStyle.fontSize = Mathf.RoundToInt(28 * s);
        foreach (var d in Players.All)
        {
            if (d == null || !d.GameOver) continue;
            bool partnerNear = false;
            foreach (var a in Players.All) if (a != null && !a.GameOver && Players.FallenNear(a) == d) partnerNear = true;
            Vector3 sp = cam.WorldToScreenPoint(d.transform.position + Vector3.up * 2.6f);
            if (sp.z < 0f) continue;
            reviveStyle.normal.textColor = Color.white;
            var r = new Rect(sp.x - 200 * s, Screen.height - sp.y - 20 * s, 400 * s, 40 * s);
            GUI.Label(r, partnerNear ? Loc.T("Pidä E: nosta") : "...", reviveStyle);
            float k = reviveProgress.TryGetValue(d, out float v) ? Mathf.Clamp01(v / ReviveTime) : 0f;
            if (k > 0f)
            {
                var bar = new Rect(sp.x - 70 * s, Screen.height - sp.y + 22 * s, 140 * s, 14 * s);
                GUI.color = Color.black; GUI.DrawTexture(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), Texture2D.whiteTexture);
                GUI.color = new Color(0.3f, 1f, 0.4f); GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * k, bar.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }

    static void Join()
    {
        CoopInput.JoinFrame = Time.frameCount;
        Players.SpawnSecond();
    }

    /// Ohjain, jota P1 käyttää: se, jolla viimeksi on liikuttu tai painettu nappeja (Start ei lasketa).
    static void TrackP1Pad()
    {
        var all = Gamepad.all;
        if (all.Count == 0) { CoopInput.P1Pad = null; return; }
        if (all.Count == 1) { CoopInput.P1Pad = all[0]; return; }
        if (CoopInput.P1Pad == null || !CoopInput.P1Pad.added) CoopInput.P1Pad = all[0];
        foreach (var g in all)
        {
            if (g == CoopInput.P1Pad) continue;
            bool active = g.leftStick.ReadValue().magnitude > 0.5f || g.dpad.ReadValue().magnitude > 0.5f
                || g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame;
            if (active) { CoopInput.P1Pad = g; break; }
        }
    }

    /// Toinen ohjain, jonka Start painettiin (P1:n ohjaimen Start on tauko).
    static Gamepad JoiningPad()
    {
        var all = Gamepad.all;
        if (all.Count < 2 || CoopInput.P1Pad == null) return null;
        foreach (var g in all)
            if (g != CoopInput.P1Pad && g.startButton.wasPressedThisFrame) return g;
        return null;
    }

    /// Taukonappi (Start): ei kun toinen ohjain liittyy P2:ksi.
    public static bool PauseStartPressed()
    {
        foreach (var g in Gamepad.all)
        {
            if (!g.startButton.wasPressedThisFrame) continue;
            return true;
        }
        return false;
    }
}
