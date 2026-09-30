using UnityEngine;

/// <summary>Soittaa kentän taustamusiikkia silmukkana. Pehmeä häivytys sisään alussa.</summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public AudioClip music;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("Häivytys sisään alussa (sekuntia).")]
    public float fadeInTime = 1.5f;

    AudioSource source;
    float t;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = music;
        source.volume = fadeInTime > 0f ? 0f : volume;
    }

    void Start()
    {
        if (music != null) source.Play();
    }

    void Update()
    {
        if (t < fadeInTime)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, volume, t / fadeInTime);
        }
        else
        {
            source.volume = volume;   // säätö Inspectorissa toimii myös pelin aikana
        }
    }
}
