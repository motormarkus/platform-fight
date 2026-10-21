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

    /// Lisää vihjeen, jos sitä ei vielä ole. Palauttaa true, jos se oli uusi.
    public static bool Add(string id, string title, string text, string art = null)
    {
        if (string.IsNullOrEmpty(id) || Has(id)) return false;
        var c = new Clue { id = id, title = title, text = text, art = art };
        All.Add(c);
        Latest = c; LatestTime = Time.unscaledTime;
        return true;
    }
}
