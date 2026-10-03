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
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("Häivytys sisään alussa (sekuntia).")]
    public float fadeInTime = 1.5f;
    [Tooltip("Ristihäivytys alueen musiikkiin ja takaisin (sekuntia).")]
    public float crossfadeTime = 1.2f;

    AudioSource source, areaSource;
    float t, areaMix;          // 0 = pääkappale, 1 = alueen kappale
    AudioClip areaClip;

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
        source.volume = master * (1f - areaMix);
        areaSource.volume = master * areaMix;
        if (areaClip == null && areaMix <= 0f && areaSource.isPlaying) areaSource.Stop();
    }
}
