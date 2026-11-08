using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tehosteiden (sfx) voimakkuus: skaalaa kaikkien ei-silmukoivien AudioSourcejen voimakkuutta asetuksen mukaan.
/// Musiikki (MusicPlayer) jää pois, silmukat (moottorit, ambienssi) skaalataan omissa skripteissään GameSettings.SfxVolumella.
/// Pääääni on AudioListenerissa, joten tämä on erillinen kerros sen päällä.
/// </summary>
public class SfxVolume : MonoBehaviour
{
    class Rec { public float baseVol, lastSet; public bool skip; }
    readonly Dictionary<AudioSource, Rec> recs = new Dictionary<AudioSource, Rec>();
    readonly List<AudioSource> dead = new List<AudioSource>();
    float next, nextScan, nextClean, lastK = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<SfxVolume>() != null) return;
        var go = new GameObject("SfxVolume");
        DontDestroyOnLoad(go);
        go.AddComponent<SfxVolume>();
    }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 0.1f;
        float k = GameSettings.SfxVolume;
        // Uudet äänilähteet haetaan koko scenestä vain puolen sekunnin välein (tai kun asetus muuttuu); välissä päivitetään jo tunnetut.
        // Ennen haku tehtiin 10 kertaa sekunnissa, ja isossa kentässä lähteitä on satoja.
        if (Time.unscaledTime >= nextScan || !Mathf.Approximately(k, lastK))
        {
            nextScan = Time.unscaledTime + 0.5f; lastK = k;
            foreach (var s in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (s != null && !recs.ContainsKey(s))
                    recs[s] = new Rec { baseVol = s.volume, lastSet = s.volume, skip = s.GetComponent<MusicPlayer>() != null };
        }
        foreach (var kv in recs)
        {
            var s = kv.Key; var r = kv.Value;
            if (s == null) { dead.Add(s); continue; }
            if (r.skip || s.loop || !s.isActiveAndEnabled) continue;
            if (!Mathf.Approximately(s.volume, r.lastSet)) r.baseVol = s.volume;   // skripti muutti voimakkuutta itse
            float nv = r.baseVol * k;
            if (!Mathf.Approximately(s.volume, nv)) s.volume = nv;
            r.lastSet = s.volume;
        }
        if (dead.Count > 0 && Time.unscaledTime >= nextClean)
        {
            nextClean = Time.unscaledTime + 2f;
            foreach (var d in dead) recs.Remove(d);
        }
        dead.Clear();
    }
}
