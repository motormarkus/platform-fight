using UnityEngine;

/// <summary>
/// Soittaa kentän taustamusiikkia silmukkana. Pehmeä häivytys sisään alussa.
/// Alueella voi olla oma musiikkinsa (esim. S-Club): ristihäivytys alueen kappaleeseen ja takaisin,
/// pääkappale jatkuu taustalla äänettömänä, joten se palaa samasta kohdasta.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    public AudioClip music;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Häivytys sisään alussa (sekuntia).")]
    public float fadeInTime = 1.5f;
    [Tooltip("Ristihäivytys alueen musiikkiin ja takaisin (sekuntia).")]
    public float crossfadeTime = 1.2f;

    AudioSource source, areaSource;
    float t, areaMix;          // 0 = pääkappale, 1 = alueen kappale
    AudioClip areaClip;
    // erillinen kappale muun päälle (esim. tanssi): oma häivytys sisään ja ulos, kenttämusiikki jatkuu taustalla
    AudioSource overSource;
    float overMix, overTarget, overFade = 1f;

    /// Soita kappale muun musiikin tilalle (häivytys fade sekunnissa).
    public static void PlayOverride(AudioClip clip, float fade = 1.5f)
    {
        if (Instance == null || clip == null) return;
        var m = Instance;
        m.overSource.clip = clip; m.overSource.time = 0f; m.overSource.Play();
        m.overTarget = 1f; m.overFade = Mathf.Max(0.01f, fade);
    }

    /// Häivytä erillinen kappale pois (fade sekunnissa), kenttämusiikki palaa.
    public static void StopOverride(float fade = 3f)
    {
        if (Instance == null) return;
        Instance.overTarget = 0f; Instance.overFade = Mathf.Max(0.01f, fade);
    }

    /// Alueen oma musiikki (null = takaisin pääkappaleeseen).
    public static void SetAreaMusic(AudioClip clip)
    {
        if (Instance != null) Instance.SetArea(clip);
    }

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = music;
        source.volume = fadeInTime > 0f ? 0f : volume;
        areaSource = gameObject.AddComponent<AudioSource>();
        areaSource.playOnAwake = false;
        areaSource.loop = true;
        areaSource.spatialBlend = 0f;
        areaSource.volume = 0f;
        overSource = gameObject.AddComponent<AudioSource>();
        overSource.playOnAwake = false;
        overSource.loop = true;
        overSource.spatialBlend = 0f;
        overSource.volume = 0f;
    }

    void Start()
    {
        if (music != null) source.Play();
    }

    void SetArea(AudioClip clip)
    {
        if (clip == areaClip) return;
        areaClip = clip;
        if (clip != null)
        {
            areaSource.clip = clip;
            areaSource.Play();
        }
    }

    void Update()
    {
        // ovien pimennys käyttää pysäytettyä aikaa, joten häivytys reaaliajassa
        float dt = Time.unscaledDeltaTime;
        if (t < fadeInTime) t += dt;
        float master = fadeInTime > 0f ? Mathf.Clamp01(t / fadeInTime) * volume : volume;
        float target = areaClip != null ? 1f : 0f;
        areaMix = Mathf.MoveTowards(areaMix, target, dt / Mathf.Max(0.01f, crossfadeTime));
        overMix = Mathf.MoveTowards(overMix, overTarget, dt / overFade);
        float rest = 1f - overMix;
        source.volume = master * (1f - areaMix) * rest;
        areaSource.volume = master * areaMix * rest;
        overSource.volume = master * overMix;
        if (overTarget <= 0f && overMix <= 0f && overSource.isPlaying) overSource.Stop();
        if (areaClip == null && areaMix <= 0f && areaSource.isPlaying) areaSource.Stop();
    }
}
