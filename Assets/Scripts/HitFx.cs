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

    [Header("Rahan keräys (Assets/Audio/sfx/setelin nosto)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    [Header("Tuntuma")]
    public float lightHitstop = 0.08f;
    public float heavyHitstop = 0.16f;
    public float lightShake = 0.08f;
    public float heavyShake = 0.22f;

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
    /// Esineen (pullo, lasi, telkkari) hajoaminen: tärähdys ilman osumapysäytystä, ettei peli nyi kun moni hajoaa peräkkäin.
    public static void OnBreak(float shake = 0.05f)
    {
        var fx = Instance;
        if (fx == null) return;
        if (shake > 0f) CameraFollow.Shake(shake, 0.1f);
    }

    /// Vihut tappelevat keskenään: vain osumaääni, ei pysäytystä eikä tärähdystä (peli ei nyi baaritappelussa).
    public static void OnHitQuiet()
    {
        var fx = Instance;
        if (fx != null) fx.PlayImpact();
    }

    public static void OnHit(bool heavy)
    {
        var fx = Instance;
        fx.PlayImpact();
        CameraFollow.Shake(heavy ? fx.heavyShake : fx.lightShake, heavy ? 0.2f : 0.12f);
        fx.Hitstop(heavy ? fx.heavyHitstop : fx.lightHitstop);
    }

    /// Osuman saaneen tärinän loppuhetki (reaaliaikaa): koko pysäytyksen ajan ja vähän sen jälkeen.
    public static float ShakeUntil(bool heavy)
    {
        var fx = Instance;
        return Time.unscaledTime + (heavy ? fx.heavyHitstop : fx.lightHitstop) + 0.1f;
    }

    /// Vaakasuuntainen tärinä (yksikköä) reaaliajassa, jotta se näkyy myös pysäytyksen aikana.
    public static float ShakeOffset(float until, float amplitude = 0.07f)
    {
        float left = until - Time.unscaledTime;
        if (left <= 0f) return 0f;
        return Mathf.Sin(Time.unscaledTime * 95f) * amplitude * Mathf.Clamp01(left / 0.1f);
    }

    /// Rahan keräysääni. Tukku soi vähän matalammalta ja kovempaa.
    /// Yksittäinen ääni (esim. energiajuoma).
    public static void PlayClip(AudioClip clip, float volume)
    {
        var fx = Instance;
        if (clip == null || fx == null || fx.source == null) return;
        fx.source.pitch = 1f;
        fx.source.PlayOneShot(clip, volume);
    }

    public static void PlayPickup(bool rare)
    {
        var fx = Instance;
        if (fx.pickupSound == null) return;
        fx.source.pitch = rare ? 0.9f : Random.Range(0.97f, 1.05f);
        fx.source.PlayOneShot(fx.pickupSound, rare ? 1f : fx.pickupVolume);
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
