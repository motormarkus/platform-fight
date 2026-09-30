using System.Collections;
using UnityEngine;

/// <summary>
/// Iskujen tuntuma: lyhyt pysähdys osuman hetkellä (hitstop), kameran tärähdys ja osumaääni.
/// Luodaan automaattisesti, jos scenessä ei ole valmiina.
/// </summary>
public class HitFx : MonoBehaviour
{
    static HitFx instance;
    public static HitFx Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HitFx>();
                if (instance == null) instance = new GameObject("HitFx").AddComponent<HitFx>();
            }
            return instance;
        }
    }

    [Header("Osumaäänet (Assets/Audio/Osumat)")]
    public AudioClip[] impactSounds;
    [Range(0f, 1f)] public float impactVolume = 0.8f;

    [Header("Tuntuma")]
    public float lightHitstop = 0.05f;
    public float heavyHitstop = 0.10f;
    public float lightShake = 0.06f;
    public float heavyShake = 0.16f;

    AudioSource source;
    bool stopping;
    int lastSound = -1;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    /// Kutsutaan, kun isku osuu. heavy = kaatava isku (pidempi pysähdys, isompi tärähdys).
    public static void OnHit(bool heavy)
    {
        var fx = Instance;
        fx.PlayImpact();
        CameraFollow.Shake(heavy ? fx.heavyShake : fx.lightShake, heavy ? 0.2f : 0.12f);
        fx.Hitstop(heavy ? fx.heavyHitstop : fx.lightHitstop);
    }

    void Hitstop(float duration)
    {
        if (stopping || duration <= 0f) return;
        StartCoroutine(HitstopRoutine(duration));
    }

    IEnumerator HitstopRoutine(float duration)
    {
        stopping = true;
        float old = Time.timeScale;
        Time.timeScale = 0.02f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = old <= 0.05f ? 1f : old;
        stopping = false;
    }

    void PlayImpact()
    {
        if (impactSounds == null || impactSounds.Length == 0) return;
        int i = Random.Range(0, impactSounds.Length);
        if (impactSounds.Length > 1 && i == lastSound) i = (i + 1) % impactSounds.Length;
        lastSound = i;
        if (impactSounds[i] == null) return;
        source.pitch = Random.Range(0.93f, 1.07f);
        source.PlayOneShot(impactSounds[i], impactVolume);
    }
}
