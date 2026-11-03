using UnityEngine;

/// <summary>Kuun tukikohdan (laboratorio, käytävät, olion huone) tunnelma: ruutu on tumma ja reunat mustuvat, valot vilkkuvat ja sähkö sirisee.
/// Satunnaiset vilkunnat ja lyhyet täyssammutukset (kipinän ääni), käytävillä ja olion huoneessa hidas punainen hälytysvalo.
/// Kaikki piirretään kameran lapsina proseduraalisilla kuvilla ja äänillä; käynnistyy itse (RuntimeInitializeOnLoadMethod). Säädöt: Preset-rivit alla.</summary>
public class LabAtmosphere : MonoBehaviour
{
    struct Preset { public string area; public float dark, vignette, red, flickerGap, blackoutChance; }

    // dark = yleinen pimeys (0–1), vignette = reunojen lisäpimeys, red = punaisen hälytysvalon voimakkuus, flickerGap = vilkkujen keskimääräinen väli (s)
    static readonly Preset[] presets =
    {
        new Preset { area = "Muurin laboratorio",  dark = 0.30f, vignette = 0.55f, red = 0f,    flickerGap = 6f, blackoutChance = 0.20f },
        new Preset { area = "Muurin käytävä",      dark = 0.38f, vignette = 0.62f, red = 0.10f, flickerGap = 4.5f, blackoutChance = 0.30f },
        new Preset { area = "Muurin käytävä 2",    dark = 0.38f, vignette = 0.62f, red = 0.10f, flickerGap = 4.5f, blackoutChance = 0.30f },
        new Preset { area = "Muurin olion huone",  dark = 0.42f, vignette = 0.68f, red = 0.14f, flickerGap = 3.5f, blackoutChance = 0.35f },
    };

    /// Vilkkuserä alkaa (esim. olion varjo ikkunassa vilahtaa samaan aikaan).
    public static event System.Action Flickered;

    const int SortingOrder = 4500;      // CaveDarkness 5000, AreaFog 4000

    static LabAtmosphere instance;
    Camera cam; Transform root;
    SpriteRenderer shade, vig, redGlow;
    Sprite white, vignetteSprite;
    AudioSource aud; AudioClip[] zaps; AudioClip buzz;
    float amount; Preset cur;
    float nextEvent, eventEnd, nextStep, level = 1f, blackoutEnd, blackoutFrom; bool inEvent;
    float lastW, lastH;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("Tukikohdan tunnelma"); DontDestroyOnLoad(go);
        instance = go.AddComponent<LabAtmosphere>();
    }

    void Start()
    {
        var tx = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) tx.SetPixel(x, y, Color.white);
        tx.Apply(); white = Sprite.Create(tx, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        const int N = 128;
        var vt = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.8f + dy * dy) / 1.15f);
                vt.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0.05f, 1f, d * d)));
            }
        vt.Apply(); vignetteSprite = Sprite.Create(vt, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
        aud = gameObject.AddComponent<AudioSource>(); aud.playOnAwake = false; aud.spatialBlend = 0f;
        zaps = new[] { MakeZap(0.22f, 11), MakeZap(0.35f, 12), MakeZap(0.16f, 13) };
        buzz = MakeBuzz(0.5f);
        nextEvent = Time.time + 2f;
    }

    void LateUpdate()
    {
        string name = Area.Current != null ? Area.Current.areaName : null;
        int idx = -1;
        for (int i = 0; i < presets.Length; i++) if (presets[i].area == name) idx = i;
        if (idx >= 0) cur = presets[idx];
        amount = Mathf.MoveTowards(amount, idx >= 0 ? 1f : 0f, Time.unscaledDeltaTime * 0.9f);
        cam = Camera.main;
        if (cam == null || amount <= 0.001f || white == null) { if (root != null) root.gameObject.SetActive(false); return; }
        if (root == null || root.parent != cam.transform) Build();
        root.gameObject.SetActive(true);
        float h = cam.orthographicSize * 2f, w = h * cam.aspect;
        if (Mathf.Abs(w - lastW) > 0.01f || Mathf.Abs(h - lastH) > 0.01f)
        {
            shade.transform.localScale = new Vector3(w * 1.2f, h * 1.2f, 1f);
            redGlow.transform.localScale = new Vector3(w * 1.2f, h * 1.2f, 1f);
            vig.transform.localScale = new Vector3(w * 1.25f, h * 1.25f, 1f);
            lastW = w; lastH = h;
        }
        Tick(Time.time);
        float lvl = level;   // 1 = valot normaalisti, 0 = sammuksissa
        float a = cur.dark + (1f - lvl) * (0.95f - cur.dark);
        float vg = cur.vignette + (1f - lvl) * (1f - cur.vignette);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f);
        shade.color = new Color(0.02f, 0.03f, 0.05f, a * amount);
        vig.color = new Color(0f, 0f, 0f, Mathf.Clamp01(vg) * amount);
        redGlow.color = new Color(0.85f, 0.04f, 0.02f, cur.red * (0.35f + 0.65f * pulse) * Mathf.Lerp(0.5f, 1f, lvl) * amount);
    }

    void Build()
    {
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("Tunnelma").transform; root.SetParent(cam.transform, false); root.localPosition = new Vector3(0f, 0f, 2f);
        redGlow = MakeLayer("Hälytysvalo", white, SortingOrder - 2);
        shade = MakeLayer("Pimeys", white, SortingOrder - 1);
        vig = MakeLayer("Reunat", vignetteSprite, SortingOrder);
        lastW = lastH = 0f;
    }

    SpriteRenderer MakeLayer(string n, Sprite sp, int order)
    {
        var sr = new GameObject(n).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(root, false); sr.sprite = sp; sr.sortingOrder = order;
        return sr;
    }

    /// Valojen taso (level): pieni jatkuva huojunta, satunnaiset vilkkuserät ja täyssammutukset.
    void Tick(float t)
    {
        if (Time.timeScale <= 0f) return;
        float steady = 1f - 0.04f * (0.5f + 0.5f * Mathf.Sin(t * 17f)) - 0.03f * (0.5f + 0.5f * Mathf.Sin(t * 41f + 1f));
        if (!inEvent)
        {
            level = Mathf.MoveTowards(level, steady, Time.deltaTime * 6f);
            if (t >= nextEvent)
            {
                inEvent = true; eventEnd = t + Random.Range(0.35f, 1.3f); nextStep = t;
                if (Random.value < cur.blackoutChance) { blackoutFrom = t + 0.3f; blackoutEnd = blackoutFrom + Random.Range(0.5f, 1.1f); eventEnd = blackoutEnd; }
                else blackoutEnd = 0f;
                Zap(blackoutEnd > 0f ? 0.7f : 0.45f);
                Flickered?.Invoke();
            }
        }
        else
        {
            if (blackoutEnd > 0f && t >= blackoutFrom && t < blackoutEnd - 0.2f)
            {
                // täyssammutus: pitkä pimeys, vain satunnaisia kipinöitä
                level = Random.value < 0.08f ? 0.55f : 0.02f;
                if (Random.value < 0.03f) Zap(0.4f);
            }
            else if (t >= nextStep)
            {
                level = Random.value < 0.5f ? Random.Range(0f, 0.25f) : Random.Range(0.6f, 1f);
                nextStep = t + Random.Range(0.025f, 0.11f);
            }
            if (t >= eventEnd)
            {
                inEvent = false; level = 1f;
                nextEvent = t + cur.flickerGap * Random.Range(0.4f, 1.6f);
                if (blackoutEnd > 0f) Zap(0.35f);   // valot palaavat napsahtaen
            }
        }
    }

    void Zap(float vol)
    {
        if (aud == null || zaps == null) return;
        aud.pitch = Random.Range(0.85f, 1.15f);
        aud.PlayOneShot(zaps[Random.Range(0, zaps.Length)], vol * 0.5f);
        if (vol > 0.5f && buzz != null) aud.PlayOneShot(buzz, 0.18f);
    }

    // proseduraalinen siritys: kohinapurske, jonka taajuus vaihtelee, nopea vaimeneminen
    static AudioClip MakeZap(float len, int seed)
    {
        const int sr = 44100; int n = (int)(sr * len); var d = new float[n]; var rnd = new System.Random(seed);
        float hold = 0f, ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n, env = Mathf.Pow(1f - k, 2.2f);
            if (i % 55 == 0) hold = (float)(rnd.NextDouble() * 2 - 1);
            ph += (180f + 160f * Mathf.Sin(k * 40f)) / sr;
            float saw = (ph % 1f) * 2f - 1f;
            d[i] = (hold * 0.6f + saw * 0.35f + (float)(rnd.NextDouble() * 2 - 1) * 0.35f) * env * 0.8f;
        }
        var c = AudioClip.Create("lab_zap", n, 1, sr, false); c.SetData(d, 0); return c;
    }

    // matala 100 Hz:n sähköhurina
    static AudioClip MakeBuzz(float len)
    {
        const int sr = 44100; int n = (int)(sr * len); var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n, env = Mathf.Sin(Mathf.PI * k);
            float t = (float)i / sr;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.25f + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.12f) * env;
        }
        var c = AudioClip.Create("lab_buzz", n, 1, sr, false); c.SetData(d, 0); return c;
    }
}
