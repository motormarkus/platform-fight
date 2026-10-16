using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelaajarekisteri (kaksinpeli): P1 = playerIndex 0, P2 = playerIndex 1. Yksinpelissä listassa on vain P1.
/// Viholliset valitsevat kohteen täältä, kamera seuraa kaikkia, ovet siirtävät kaikki.
/// </summary>
public static class Players
{
    public static readonly List<PlayerController> All = new List<PlayerController>();

    /// Päällä, kun mallikopiota (P2:n pohja) luodaan: kopion Awake ei tee mitään.
    public static bool Cloning;
    /// Passiivinen kopio P1:stä ennen hahmon (Ruby) asettamista. P2 luodaan tästä.
    public static PlayerController Template;
    /// P2:n hahmo synnytyshetkellä (0 = Rocco, 1 = Ruby), -1 = GameSettings.Character.
    public static int SpawnCharacter = -1;
    /// Kaksinpeli on päällä (säilyy uuden pelin aloituksessa).
    public static bool CoopWanted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        All.Clear(); Cloning = false; Template = null; SpawnCharacter = -1; CoopWanted = false;
        CoopInput.P2OnKeyboard = false; CoopInput.Pads = new UnityEngine.InputSystem.Gamepad[2]; CoopInput.P1Pad = null; CoopInput.JoinFrame = -10;
    }

    static void Clean() { All.RemoveAll(p => p == null); }

    public static int Count { get { Clean(); return All.Count; } }

    public static void Register(PlayerController p) { if (!All.Contains(p)) All.Add(p); All.Sort((a, b) => a.playerIndex.CompareTo(b.playerIndex)); }
    public static void Unregister(PlayerController p) { All.Remove(p); }

    /// P1 (tai ensimmäinen pelaaja). Vastaa vanhaa FindFirstObjectByType<PlayerController>().
    public static PlayerController Primary
    {
        get
        {
            Clean();
            if (All.Count > 0) return All[0];
            return Object.FindFirstObjectByType<PlayerController>();
        }
    }

    public static bool AllDown
    {
        get
        {
            Clean();
            if (All.Count == 0) return false;
            foreach (var p in All) if (!p.GameOver) return false;
            return true;
        }
    }

    public static bool AnyBoosted
    {
        get
        {
            foreach (var p in All) if (p != null && p.Boosted) return true;
            return false;
        }
    }

    /// Lähin elossa oleva pelaaja. current = nykyinen kohde: vaihdetaan vain, jos toinen on selvästi lähempänä (ei värinää kohteiden välillä).
    public static PlayerController Target(Vector3 from, PlayerController current)
    {
        Clean();
        if (All.Count == 0) return current != null ? current : Object.FindFirstObjectByType<PlayerController>();
        if (All.Count == 1) return All[0];
        PlayerController best = null; float bestD = float.MaxValue;
        foreach (var p in All)
        {
            if (p.GameOver) continue;
            float d = Dist(from, p.transform.position);
            if (d < bestD) { bestD = d; best = p; }
        }
        if (best == null) return current != null && All.Contains(current) ? current : All[0];
        if (current != null && !current.GameOver && All.Contains(current) && Dist(from, current.transform.position) <= bestD + 3f) return current;
        return best;
    }

    /// Lähin elossa oleva pelaaja (esineet, jotka kuka tahansa voi poimia tai joihin kuka tahansa voi osua).
    public static PlayerController Nearest(Vector3 from)
    {
        Clean();
        if (All.Count <= 1) return Primary;
        PlayerController best = null; float bestD = float.MaxValue;
        foreach (var p in All)
        {
            if (p.GameOver) continue;
            float d = Dist(from, p.transform.position);
            if (d < bestD) { bestD = d; best = p; }
        }
        return best != null ? best : All[0];
    }

    static float Dist(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x, dy = (a.y - b.y) * 2f;   // syvyysero painaa vähemmän kuin sivuero
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// Keskipiste ja ulottuma elossa olevien pelaajien x:stä (kamera).
    public static bool LivingBounds(out float minX, out float maxX, out float avgY)
    {
        minX = float.MaxValue; maxX = float.MinValue; avgY = 0f; int n = 0;
        Clean();
        foreach (var p in All)
        {
            if (p.GameOver) continue;
            float x = p.transform.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            avgY += p.transform.position.y; n++;
        }
        if (n == 0) return false;
        avgY /= n;
        return true;
    }

    /// Luo P1:n passiivisen kopion ennen kuin P1:n Awake muuttaa mitään (P2 luodaan tästä).
    public static void MakeTemplate(PlayerController p1)
    {
        if (Template != null) return;
        Cloning = true;
        var t = Object.Instantiate(p1.gameObject);
        Cloning = false;
        t.name = "Hero template (P2)";
        t.SetActive(false);
        Template = t.GetComponent<PlayerController>();
    }

    /// Luo P2:n P1:n viereen. Hahmo on toinen kuin P1:llä.
    public static PlayerController SpawnSecond()
    {
        var p1 = Primary;
        Clean();
        if (p1 == null || Template == null || All.Count > 1) return null;
        SpawnCharacter = p1.AppliedCharacter == 1 ? 0 : 1;
        var go = Object.Instantiate(Template.gameObject, p1.transform.position + new Vector3(1.4f, 0f, 0f), Quaternion.identity);
        go.name = "Hero P2";
        var p2 = go.GetComponent<PlayerController>();
        p2.playerIndex = 1;
        p2.CopyAreaFrom(p1);
        go.SetActive(true);
        SpawnCharacter = -1;
        CoopWanted = true;
        return p2;
    }

    /// Päävalikon 1 / 2 pelaajaa. Päälle: P2 syntyy heti (CoopManager) toisella ohjaimella tai, jos ohjaimia on vain yksi, näppäimistön nuolilla.
    public static void SetCoop(bool on)
    {
        if (on)
        {
            var all = UnityEngine.InputSystem.Gamepad.all;
            CoopInput.P2OnKeyboard = all.Count < 2;
            if (all.Count >= 2)
            {
                var p1 = CoopInput.P1Pad != null ? CoopInput.P1Pad : (UnityEngine.InputSystem.Gamepad.current != null ? UnityEngine.InputSystem.Gamepad.current : all[0]);
                UnityEngine.InputSystem.Gamepad p2 = null;
                foreach (var g in all) if (g != p1) { p2 = g; break; }
                CoopInput.Pads[0] = p1; CoopInput.Pads[1] = p2;
            }
            CoopWanted = true;
        }
        else
        {
            CoopWanted = false;
            CoopInput.P2OnKeyboard = false;
            Clean();
            foreach (var p in All.ToArray()) if (p != null && p.playerIndex == 1) Object.Destroy(p.gameObject);
        }
    }

    /// Siirtää muut pelaajat samaan kohtaan kuin pelaaja who (alueenvaihto, ovet).
    public static void TeleportOthers(PlayerController who, Vector3 pos, bool alsoDead = true)
    {
        Clean();
        int n = 0;
        foreach (var p in All)
        {
            if (p == who || p == null) continue;
            n++;
            p.TeleportTo(pos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f));
        }
    }
}
