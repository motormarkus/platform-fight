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
    AudioSource[] pool; int poolIdx;
    bool stopping;
    int lastSound = -1;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        pool = new AudioSource[8];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false; pool[i].spatialBlend = 0f;
        }
    }

    static AudioClip[] whooshClips; int lastWhoosh = -1;

    /// Viuhahdus joka kerta, kun pelaaja heilauttaa iskun tai potkun (osui tai ei). Viisi ääntä, ei samaa kahdesti peräkkäin; ei sävelkorkeuden vaihtelua.
    public static void PlayWhoosh()
    {
        var fx = Instance;
        if (fx == null || fx.pool == null) return;
        if (whooshClips == null)
        {
            var l = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 5; i++) { var c = Resources.Load<AudioClip>("Sfx/Hit/viuh_" + i); if (c != null) l.Add(c); }
            whooshClips = l.ToArray();
        }
        if (whooshClips.Length == 0) return;
        int k = Random.Range(0, whooshClips.Length);
        if (whooshClips.Length > 1 && k == fx.lastWhoosh) k = (k + 1) % whooshClips.Length;
        fx.lastWhoosh = k;
        PlayPitched(whooshClips[k], 0.85f, 1f);
    }

    /// Soittaa äänen omalla äänilähteellään omalla sävelkorkeudellaan (jaettu lähde muuttaisi myös jo soivien äänten sävelkorkeutta: sinkoilua ja säröä).
    public static void PlayPitched(AudioClip clip, float volume, float pitch)
    {
        var fx = Instance;
        if (clip == null || fx == null || fx.pool == null) return;
        var src = fx.pool[fx.poolIdx]; fx.poolIdx = (fx.poolIdx + 1) % fx.pool.Length;
        src.Stop();
        src.clip = clip; src.pitch = pitch; src.volume = Mathf.Clamp01(volume * AudioLevels.Gain(clip));
        src.Play();
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

    /// Vihu iskeytyy maahan (lento, heitto, paiskaus): ääni ja lyhyt pysäytys, ei kameran tärähdystä (häiritsi koko ruudulla).
    public static void OnLand()
    {
        var fx = Instance;
        if (fx == null) return;
        fx.PlayImpact();
        fx.Hitstop(fx.lightHitstop);
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
        fx.source.PlayOneShotLeveled(clip, volume);
    }

    public static void PlayPickup(bool rare)
    {
        var fx = Instance;
        if (fx.pickupSound == null) return;
        fx.source.pitch = rare ? 0.9f : Random.Range(0.97f, 1.05f);
        fx.source.PlayOneShotLeveled(fx.pickupSound, rare ? 1f : fx.pickupVolume);
    }

    float stopUntil;

    /// Erikoisliikkeen osuma: pidempi pysäytys (jatkaa käynnissä olevaa).
    public static void Freeze(float duration)
    {
        var fx = Instance;
        if (fx != null) fx.Hitstop(duration);
    }

    void Hitstop(float duration)
    {
        if (duration <= 0f || Players.Count > 1) return;   // kaksinpelissä ei osumapysäytystä: kuva ei saa pätkiä
        stopUntil = Mathf.Max(stopUntil, Time.unscaledTime + duration);
        if (!stopping) StartCoroutine(HitstopRoutine());
    }

    IEnumerator HitstopRoutine()
    {
        stopping = true;
        float old = Time.timeScale;
        Time.timeScale = 0.02f;
        while (Time.unscaledTime < stopUntil) yield return null;
        Time.timeScale = old <= 0.05f ? 1f : old;
        stopping = false;
    }

    // ---------------- Kombon lopetus: välähdys, hidastus ja teksti ----------------
    float flashUntil, textUntil; string finisherText; GUIStyle finisherStyle; Texture2D whiteTex;
    const float FlashLen = 0.28f, TextLen = 1.7f;

    /// Kymmenen iskun kombon viimeinen isku: valkoinen välähdys, teksti ja (osuessa) hidastus. hit = osui johonkin.
    public static void FinisherFx(string text, bool hit)
    {
        var fx = Instance;
        if (fx == null) return;
        fx.finisherText = text;
        fx.flashUntil = Time.unscaledTime + FlashLen;
        fx.textUntil = Time.unscaledTime + TextLen;
        if (!hit || Players.Count > 1) return;   // kaksinpelissä ei hidastusta eikä pysäytystä
        fx.Hitstop(0.2f);
        fx.StartCoroutine(fx.SlowMoRoutine(0.28f, 0.9f));
    }

    IEnumerator SlowMoRoutine(float scale, float dur)
    {
        while (stopping) yield return null;
        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < dur)
        {
            float k = (Time.unscaledTime - t0) / dur;
            if (Time.timeScale > 0.001f && !stopping) Time.timeScale = Mathf.Lerp(scale, 1f, Mathf.Clamp01((k - 0.5f) / 0.5f));   // hidas alku, palaa normaaliksi loppua kohti
            yield return null;
        }
        if (Time.timeScale > 0.001f && !stopping) Time.timeScale = 1f;
    }

    void OnGUI()
    {
        float now = Time.unscaledTime;
        if (now >= textUntil && now >= flashUntil) return;
        if (whiteTex == null) { whiteTex = new Texture2D(1, 1); whiteTex.SetPixel(0, 0, Color.white); whiteTex.Apply(); }
        var old = GUI.color;
        if (now < flashUntil) { GUI.color = new Color(1f, 1f, 1f, 0.75f * (flashUntil - now) / FlashLen); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex); }
        if (now < textUntil && !string.IsNullOrEmpty(finisherText))
        {
            if (finisherStyle == null) finisherStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            float k = (now - (textUntil - TextLen)) / TextLen;
            finisherStyle.fontSize = Mathf.RoundToInt(Screen.height * (0.11f + 0.04f * Mathf.Max(0f, 1f - k * 6f)));   // pamahtaa alussa
            var r = new Rect(0, Screen.height * 0.18f, Screen.width, Screen.height * 0.2f);
            float a = Mathf.Clamp01((1f - k) * 2f);
            GUI.color = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(r.x + 4, r.y + 4, r.width, r.height), finisherText, finisherStyle);
            GUI.color = new Color(1f, 0.85f, 0.2f, a); GUI.Label(r, finisherText, finisherStyle);
        }
        GUI.color = old;
    }

    void PlayImpact()
    {
        if (impactSounds == null || impactSounds.Length == 0) return;
        int i = Random.Range(0, impactSounds.Length);
        if (impactSounds.Length > 1 && i == lastSound) i = (i + 1) % impactSounds.Length;
        lastSound = i;
        if (impactSounds[i] == null) return;
        source.pitch = Random.Range(0.93f, 1.07f);
        source.PlayOneShotLeveled(impactSounds[i], impactVolume);
    }
}
