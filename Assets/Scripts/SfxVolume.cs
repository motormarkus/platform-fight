using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tehosteiden (sfx) voimakkuus: skaalaa kaikkien ei-silmukoivien AudioSourcejen voimakkuutta asetuksen mukaan.
/// Musiikki (MusicPlayer) jää pois, silmukat (moottorit, ambienssi) skaalataan omissa skripteissään GameSettings.SfxVolumella.
/// Pääääni on AudioListenerissa, joten tämä on erillinen kerros sen päällä.
/// </summary>
public class SfxVolume : MonoBehaviour
{
    class Rec { public float baseVol, lastSet; }
    readonly Dictionary<AudioSource, Rec> recs = new Dictionary<AudioSource, Rec>();
    float next, nextClean;

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
        var all = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in all)
        {
            if (s == null || s.loop || s.GetComponent<MusicPlayer>() != null) continue;
            if (!recs.TryGetValue(s, out var r)) { r = new Rec { baseVol = s.volume, lastSet = s.volume }; recs[s] = r; }
            else if (!Mathf.Approximately(s.volume, r.lastSet)) r.baseVol = s.volume;   // skripti muutti voimakkuutta itse
            float nv = r.baseVol * k;
            if (!Mathf.Approximately(s.volume, nv)) s.volume = nv;
            r.lastSet = s.volume;
        }
        if (Time.unscaledTime >= nextClean)
        {
            nextClean = Time.unscaledTime + 10f;
            var dead = new List<AudioSource>();
            foreach (var kv in recs) if (kv.Key == null) dead.Add(kv.Key);
            foreach (var d in dead) recs.Remove(d);
        }
    }
}
