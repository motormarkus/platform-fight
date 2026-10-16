using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Kaksinpelin ohjaus: P2 liittyy toisen ohjaimen Startilla (tai F2:lla näppäimistölle), kaatunut pelaaja herää
/// kumppanin viereen, kun kumppani on selvinnyt tarpeeksi pitkään. Luodaan itsestään (ei tarvitse scene-muutoksia).
/// </summary>
public class CoopManager : MonoBehaviour
{
    /// Kuinka monta sekuntia kaatunut pelaaja odottaa, ennen kuin herää kumppanin viereen.
    public const float ReviveDelay = 12f;
    public const float ReviveHealthFraction = 0.4f;

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

        // kaksinpeli: kaatunut pelaaja herää, kun toinen on hengissä
        foreach (var p in Players.All)
        {
            if (p == null || !p.GameOver) continue;
            bool partnerAlive = false;
            foreach (var o in Players.All) if (o != null && o != p && !o.GameOver) partnerAlive = true;
            if (partnerAlive && Time.time - p.DeadAt >= ReviveDelay)
            {
                PlayerController partner = null;
                foreach (var o in Players.All) if (o != null && o != p && !o.GameOver) partner = o;
                p.Revive(Mathf.Max(1, Mathf.RoundToInt(p.maxHealth * ReviveHealthFraction)), partner.transform.position + new Vector3(partner.transform.position.x < 0f ? 1.5f : -1.5f, 0f, 0f));
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
