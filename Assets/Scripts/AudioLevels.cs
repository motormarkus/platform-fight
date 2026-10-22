using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puheiden ja tehosteiden voimakkuuden tasaus. Tools/AudioLevels/measure_audio.py mittaa kaikki äänet ja kirjoittaa Resources/AudioLevels.json:iin
/// jokaiselle vahvistuskertoimen (kovat vaimennetaan yhteiselle tasolle). Kaikki äänet soitetaan PlayOneShotLeveled-kutsulla, joka kertoo voimakkuuden kertoimella.
/// Avain on "tiedostonimi|kesto (1/100 s)", koska eri hahmoilla on samannimisiä tiedostoja (puhe1, isku1…).
/// </summary>
public static class AudioLevels
{
    [System.Serializable] class Data { public float targetDb; public string[] names; public float[] gains; }

    /// Tasaus päälle/pois (esim. vertailuun).
    public static bool Enabled = true;
    /// 0 = ei tasausta, 1 = täysi tasaus.
    public static float Strength = 1f;

    static Dictionary<string, List<Vector2>> table;   // nimi -> (kesto, kerroin)

    static void Load()
    {
        table = new Dictionary<string, List<Vector2>>();
        var ta = Resources.Load<TextAsset>("AudioLevels");
        if (ta == null) return;
        Data d = null;
        try { d = JsonUtility.FromJson<Data>(ta.text); } catch { }
        if (d == null || d.names == null || d.gains == null) return;
        for (int i = 0; i < d.names.Length && i < d.gains.Length; i++)
        {
            string key = d.names[i]; int bar = key.LastIndexOf('|');
            if (bar < 0) continue;
            string name = key.Substring(0, bar);
            if (!int.TryParse(key.Substring(bar + 1), out int dur)) continue;
            if (!table.TryGetValue(name, out var l)) table[name] = l = new List<Vector2>();
            l.Add(new Vector2(dur * 0.01f, d.gains[i]));
        }
    }

    /// Äänen tasauskerroin (1 = ei muutosta, jos ääntä ei ole mitattu).
    public static float Gain(AudioClip clip)
    {
        if (!Enabled || clip == null) return 1f;
        if (table == null) Load();
        if (!table.TryGetValue(clip.name, out var l) || l.Count == 0) return 1f;
        float best = 1f, bestD = 0.2f;   // samannimisistä valitaan lähin kesto (enintään 0,2 s ero)
        foreach (var v in l) { float dd = Mathf.Abs(v.x - clip.length); if (dd < bestD) { bestD = dd; best = v.y; } }
        return Mathf.Lerp(1f, best, Mathf.Clamp01(Strength));
    }

    /// AudioSource.PlayOneShot tasatulla voimakkuudella.
    public static void PlayOneShotLeveled(this AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip, volume * Gain(clip));
    }

    /// Lataa taulukon uudelleen (esim. tiedoston päivityksen jälkeen editorissa).
    public static void Reload() { table = null; }
}
