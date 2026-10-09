using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Alueen taustahäly (lokit ja aallot kannella, puheensorina baarissa…) silmukkana musiikin alla.
/// Ääni haetaan alueen nimellä kansiosta Resources/Ambienssi: "Laivan kansi" -> laivan_kansi.wav/.mp3/.ogg
/// (pienet kirjaimet, ä -> a, ö -> o, välilyönti -> _). Alueen oma ambience-kenttä menee edelle.
/// Ristihäivytys alueelta toiselle; syntyy itsestään, sceneen ei tarvitse lisätä mitään.
/// </summary>
public class AmbiencePlayer : MonoBehaviour
{
    [Range(0f, 1f)] public float volume = 0.55f;
    public float crossfadeTime = 1.5f;

    AudioSource a, b;          // a = nykyinen, b = häipyvä
    Area shownArea;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindFirstObjectByType<AmbiencePlayer>() != null) return;
        new GameObject("Ambienssi").AddComponent<AmbiencePlayer>();
    }

    void Awake()
    {
        a = NewSource(); b = NewSource();
    }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f;
        return s;
    }

    public static string FileName(string areaName)
    {
        var s = areaName.ToLowerInvariant().Replace('ä', 'a').Replace('ö', 'o').Replace('å', 'a').Replace(' ', '_').Replace('-', '_');
        return s;
    }

    AudioClip ClipFor(Area area)
    {
        if (area == null) return null;
        if (area.ambience != null) return area.ambience;
        string key = FileName(area.areaName);
        if (!cache.TryGetValue(key, out var c))
        {
            c = Resources.Load<AudioClip>("Ambienssi/" + key);
            cache[key] = c;
        }
        return c;
    }

    void Update()
    {
        var area = Area.Current;
        if (area != shownArea)
        {
            shownArea = area;
            var clip = ClipFor(area);
            if (clip != a.clip)
            {
                // vanha häipyy, uusi alkaa satunnaisesta kohdasta (ei kuulosta samalta joka kerta)
                (a, b) = (b, a);
                a.clip = clip;
                if (clip != null) { a.time = Random.Range(0f, clip.length * 0.9f); a.Play(); }
                else a.Stop();
            }
        }
        float dt = Time.unscaledDeltaTime / Mathf.Max(0.01f, crossfadeTime);
        float target = volume * (shownArea != null ? shownArea.ambienceVolume : 1f) * GameSettings.MusicVolume;
        a.volume = Mathf.MoveTowards(a.volume, a.clip != null ? target : 0f, dt);
        b.volume = Mathf.MoveTowards(b.volume, 0f, dt);
        if (b.volume <= 0f && b.isPlaying) b.Stop();
    }
}
