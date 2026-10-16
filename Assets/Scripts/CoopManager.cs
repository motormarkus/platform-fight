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
            if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                CoopInput.P2OnKeyboard = true;
                Join();
                return;
            }
            var joinPad = JoiningPad();
            if (joinPad != null)
            {
                CoopInput.P2OnKeyboard = false;
                CoopInput.Pads[0] = CoopInput.P1Pad;
                CoopInput.Pads[1] = joinPad;
                Join();
            }
            return;
        }

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

    void OnGUI()
    {
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
            if (Players.Count <= 1 && Gamepad.all.Count >= 2 && CoopInput.P1Pad != null && g != CoopInput.P1Pad && !Players.CoopWanted) continue;   // liittyminen
            return true;
        }
        return false;
    }
}
