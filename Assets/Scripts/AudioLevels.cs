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
        if (clip.name.StartsWith("latkafani_")) return clip.name.StartsWith("latkafani_puhe") || clip.name.StartsWith("latkafani_voitto") ? 1.2f : 0.5f;   // vain puheet täydellä; kaikki muu (isku, kipu, heitto, voitonhuuto) puoleen: olivat liian kovia
        if (table == null) Load();
        if (!table.TryGetValue(clip.name, out var l) || l.Count == 0) return 1f;
        float best = 1f, bestD = 0.2f;   // samannimisistä valitaan lähin kesto (enintään 0,2 s ero)
        foreach (var v in l) { float dd = Mathf.Abs(v.x - clip.length); if (dd < bestD) { bestD = dd; best = v.y; } }
        return Mathf.Lerp(1f, best, Mathf.Clamp01(Strength));
    }

    // Väkijoukon äänten rajoitus: kun moni vihu soittaa saman äänen yhtä aikaa, äänet summautuvat samassa vaiheessa ja voimistuvat (+6 dB per kaksinkertaistus).
    // Siksi vihujen ääniä vaimennetaan sen mukaan, montako samaa (tai mitä tahansa) ääntä on juuri soinut, ja samaa ääntä soittavien sävelkorkeus vaihtelee hieman.
    struct Played { public float time; public AudioClip clip; }
    static readonly List<Played> recent = new List<Played>();
    /// Vihujen äänet vaimenevat tämän verran (0-1), kun taustamusiikki (esim. pukin biisi) soi erillisenä kappaleena.
    public static float MusicDuck = 0.55f;
    public static bool CrowdLimiter = true;
    /// Vihujen puheiden (ja puheena merkittyjen huutojen) lisävahvistus; PlayOneShotin voimakkuus saa ylittää 1:n.
    public static float SpeechBoost = 1.6f;

    /// AudioSource.PlayOneShot tasatulla voimakkuudella.
    public static void PlayOneShotLeveled(this AudioSource source, AudioClip clip, float volume = 1f, bool speech = false)
    {
        if (source == null || clip == null) return;
        float v = volume * Gain(clip);
        if (speech && !clip.name.StartsWith("latkafani_") && source.GetComponent<Enemy>() != null && source.GetComponent<SantaBrain>() == null) v *= SpeechBoost;   // vihujen puheet kuuluvat paremmin
        if (CrowdLimiter && !speech &&   // puheet (speech) eivät vaimene väkijoukon rajoittimessa eikä pukin musiikin alla
 source.GetComponent<Enemy>() != null && source.GetComponent<SantaBrain>() == null)   // heron ja pukin äänet eivät rajoitu
        {
            float now = Time.unscaledTime;
            recent.RemoveAll(p => now - p.time > 0.3f);
            int nSame = 0, nAll = 0;
            foreach (var p in recent) { if (now - p.time <= 0.25f) nAll++; if (p.clip == clip && now - p.time <= 0.12f) nSame++; }
            if (nSame >= 3) return;                                   // sama ääni kolmannen kerran heti: jätetään soittamatta
            v *= 1f / (1f + 0.7f * nSame) / Mathf.Sqrt(1f + 0.2f * nAll);
            if (nSame > 0) source.pitch = Random.Range(0.94f, 1.06f); // eri sävelkorkeus: ei summaudu samassa vaiheessa
            if (MusicPlayer.OverridePlaying) v *= MusicDuck;           // erillinen kappale soi (pukin biisi): väkijoukko hiljemmalle
            recent.Add(new Played { time = now, clip = clip });
        }
        source.PlayOneShot(clip, v);
    }

    /// Lataa taulukon uudelleen (esim. tiedoston päivityksen jälkeen editorissa).
    public static void Reload() { table = null; }
}
