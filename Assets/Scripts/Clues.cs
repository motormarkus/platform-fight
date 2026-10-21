using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tarinan vihjeet (tapauskansio): kirjeet, viestit ja muut löydöt. Lista säilyy pelikerran ajan (ei tallennusta) ja nollataan, kun peli aloitetaan alusta.
/// </summary>
public static class Clues
{
    public class Clue { public string id, title, text; public string art; }   // art = kuva (Resources-polku), esim. kirjeen kuva

    public static readonly List<Clue> All = new List<Clue>();
    /// Viimeksi lisätty vihje ja sen aika (HUD näyttää ilmoituksen hetken).
    public static Clue Latest { get; private set; }
    public static float LatestTime { get; private set; } = -99f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { Clear(); }

    public static void Clear() { All.Clear(); Latest = null; LatestTime = -99f; }

    public static bool Has(string id) => All.Exists(c => c.id == id);

    public static int IndexOf(string id) => All.FindIndex(c => c.id == id);

    /// Tarinalukot (pyörän avain, laivalippu). Editorissa voi kytkeä pois testausta varten (valikko 229).
    public static bool GatesOn
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool("TotalFist.StoryGates", true);
#else
            return true;
#endif
        }
    }
    /// Lukko auki: lukot pois päältä tai vihje löytynyt.
    public static bool Open(string id) => string.IsNullOrEmpty(id) || !GatesOn || Has(id);

    public const string Belt = "mestaruusvyo";
    /// Mestaruusvyö: vihujen lentomatka (sivusuunta) pitenee 15 %.
    public static float LaunchMul => Has(Belt) ? 1.15f : 1f;
    public static void WinBelt() { Add(Belt, "Mestaruusvyö", "Voitit Marcuksen kehässä. Vyö antaa lyönneillesi lisää voimaa: viholliset lentävät 15 % pidemmälle."); }

    public const string BikeKey = "avain_pyora", ShipTicket = "lippu_laiva";

    /// Pomo kaatui: antaa tarinaesineen. Metsuri -> moottoripyörän avaimet, Horhe -> laivalippu.
    public static void BossDown(string boss)
    {
        if (boss == "Metsuri") Add(BikeKey, "Moottoripyörän avaimet", "Metsurin taskusta löytyi moottoripyörän avaimet. Nyt pääset ajamaan.");
        else if (boss == "Horhe") Add(ShipTicket, "Laivalippu", "Horhen taskusta löytyi lippu laivaan. Satamassa se avaa tien laivalle.");
    }

    /// Lisää vihjeen, jos sitä ei vielä ole. Palauttaa true, jos se oli uusi.
    public static bool Add(string id, string title, string text, string art = null, bool notify = true)
    {
        if (string.IsNullOrEmpty(id) || Has(id)) return false;
        var c = new Clue { id = id, title = title, text = text, art = art };
        All.Add(c);
        if (notify) { Latest = c; LatestTime = Time.unscaledTime; }
        return true;
    }
}
