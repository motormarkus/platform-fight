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
    float overMix, overTarget, overFade = 1f, overGain = 1f;

    /// Soita kappale muun musiikin tilalle (häivytys fade sekunnissa).
    public static void PlayOverride(AudioClip clip, float fade = 1.5f, bool loop = true, float gain = 1f)
    {
        if (Instance == null || clip == null) return;
        var m = Instance;
        m.overSource.loop = loop; m.overGain = gain;
        m.overSource.clip = clip; m.overSource.time = 0f; m.overSource.Play();
        m.overTarget = 1f; m.overFade = Mathf.Max(0.01f, fade);
    }

    /// Muuttaa erillisen kappaleen vahvistusta kesken soiton (esim. musiikki hiljenee tappelun loppua kohden).
    public static void SetOverrideGain(float g) { if (Instance != null) Instance.overGain = g; }

    /// Soiko erillinen kappale (PlayOverride) vielä? Silmukatta soitettu kappale päättyy itsestään.
    public static bool OverridePlaying => Instance != null && Instance.overSource.isPlaying;

    /// Häivytä erillinen kappale pois (fade sekunnissa), kenttämusiikki palaa.
    public static void StopOverride(float fade = 3f)
    {
        if (Instance == null) return;
        Instance.overTarget = 0f; Instance.overFade = Mathf.Max(0.01f, fade);
    }

    /// Alueen oma musiikki (null = takaisin pääkappaleeseen).
    public static void SetAreaMusic(AudioClip clip, float gain = 1f)
    {
        if (Instance != null) { Instance.areaGain = gain; Instance.SetArea(clip); }
    }
    // Taustamusiikin tasaus: kappaleiden RMS mitattu (dBFS), kaikki vaimennetaan samalle tasolle (hiljaisimmat jäävät ennalleen). Mittaus: PyAV, mono-keskiarvo.
    const float MusicTargetDb = -16.0f;   // yhteinen taso (dBFS RMS); -14 vastasi Uccopulcoa ennen, mutta säröytti hiljaisemmat kappaleet nostettaessa, joten 2 dB alemmas
    static readonly System.Collections.Generic.Dictionary<string, float> MusicRms = new System.Collections.Generic.Dictionary<string, float>
    {
        { "Aavikko", -16.3f }, { "Aavikkobaari", -17.4f }, { "Blade Anthem", -16.4f }, { "S-Club", -15.8f }, { "Shadows in the Neon", -18.1f }, { "Sini ja Dinan asunto", -18.2f },
        { "Thaitown", -16.7f }, { "Turpaan vaan", -16.4f }, { "Uccopulco", -14.0f }, { "Viemari", -19.7f }, { "tanssi_tausta", -17.8f }, { "Alppikyla", -19.3f },
    };
    static float ClipGain(AudioClip c) => c != null && MusicRms.TryGetValue(c.name, out float db) ? Mathf.Pow(10f, (MusicTargetDb - db) / 20f) : 1f;
    MusicBoost mainBoost, areaBoost;
    float areaGain = 1f;   // alueen musiikin vahvistus (1 = kappaleen oma taso; 1,334 = +2,5 dB)

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = music;
        mainBoost = gameObject.AddComponent<MusicBoost>();   // vain tämän olion äänilähteet (alue- ja erillinen kappale ovat omissa lapsioliossaan)
        source.volume = fadeInTime > 0f ? 0f : volume;
        var areaGo = new GameObject("Alueen musiikki"); areaGo.transform.SetParent(transform, false);
        areaSource = areaGo.AddComponent<AudioSource>(); areaBoost = areaGo.AddComponent<MusicBoost>();
        areaSource.playOnAwake = false;
        areaSource.loop = true;
        areaSource.spatialBlend = 0f;
        areaSource.volume = 0f;
        var overGo = new GameObject("Erillinen kappale"); overGo.transform.SetParent(transform, false);
        overSource = overGo.AddComponent<AudioSource>();
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
        float master = (fadeInTime > 0f ? Mathf.Clamp01(t / fadeInTime) * volume : volume) * GameSettings.MusicVolume;   // asetusten musiikin voimakkuus
        float target = areaClip != null ? 1f : 0f;
        areaMix = Mathf.MoveTowards(areaMix, target, dt / Mathf.Max(0.01f, crossfadeTime));
        overMix = Mathf.MoveTowards(overMix, overTarget, dt / overFade);
        float rest = 1f - overMix;
        source.volume = master * (1f - areaMix) * rest; mainBoost.gain = ClipGain(source.clip);
        areaSource.volume = Mathf.Clamp01(master * areaMix * rest * areaGain); areaBoost.gain = ClipGain(areaSource.clip);
        overSource.volume = Mathf.Clamp01(master * overMix * overGain);
        if (overTarget <= 0f && overMix <= 0f && overSource.isPlaying) overSource.Stop();
        if (areaClip == null && areaMix <= 0f && areaSource.isPlaying) areaSource.Stop();
    }
}

/// Musiikin vahvistus yli 1:n (AudioSource.volume ei ylitä 1:tä): kertoo ja rajoittaa pehmeästi.
public class MusicBoost : MonoBehaviour
{
    public volatile float gain = 1f;
    void OnAudioFilterRead(float[] data, int channels)
    {
        float g = gain;
        if (Mathf.Abs(g - 1f) < 0.001f) return;
        for (int i = 0; i < data.Length; i++)
        {
            float x = data[i] * g, a = Mathf.Abs(x);
            data[i] = a <= 0.7f ? x : Mathf.Sign(x) * (0.7f + 0.3f * (float)System.Math.Tanh((a - 0.7f) / 0.3f));
        }
    }
}
