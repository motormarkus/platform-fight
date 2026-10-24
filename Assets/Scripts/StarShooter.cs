using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tähtipyssy: sivulta kuvattu ammuntapeli pelihallin koneessa. Kuvat ovat Resources/TahtiPyssy (Tools/SpaceGame/slice_sprites.py),
/// äänet syntetisoidaan koodilla. Peli rakentaa oman kameransa ja maailmansa kaukana pelialueesta (y 6000), pysäyttää pelaajat ja
/// palauttaa ne lopussa (kuten Candin treeni). Ohjaus: liike (nuolet/WASD/tatti), isku-nappi ampuu (pohjassa), erikoisnappi laukaisee
/// säteen kun mittari on täynnä, Esc/Q poistuu.
/// </summary>
public class StarShooter : MonoBehaviour
{
    public static StarShooter Instance;
    public static bool Active => Instance != null;
    const string HiKey = "tahtipyssy_hi";

    public static void Begin(PlayerController who)
    {
        if (Instance != null || who == null) return;
        var go = new GameObject("Tähtipyssy");
        go.AddComponent<StarShooter>().Init(who);
    }

    // ---------------------------------------------------------------- kuvat
    static class Art
    {
        static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite[]> sets = new Dictionary<string, Sprite[]>();
        static Sprite whiteSprite;

        public static Texture2D Tex(string n)
        {
            if (tex.TryGetValue(n, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("TahtiPyssy/" + n);
            tex[n] = t;
            return t;
        }

        /// Kuvasarja prefix_0..n-1; yhteinen mittakaava niin, että leveimmän kuvan leveys on <widest> yksikköä.
        public static Sprite[] Set(string prefix, int n, float widest, float pivX = 0.5f)
        {
            string key = prefix + "|" + widest + "|" + pivX;
            if (sets.TryGetValue(key, out var s) && s[0] != null) return s;
            var texes = new Texture2D[n];
            float maxW = 1f;
            for (int i = 0; i < n; i++) { texes[i] = Tex(prefix + "_" + i); if (texes[i] != null) maxW = Mathf.Max(maxW, texes[i].width); }
            float ppu = maxW / widest;
            s = new Sprite[n];
            for (int i = 0; i < n; i++)
            {
                var t = texes[i] != null ? texes[i] : Texture2D.whiteTexture;
                s[i] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(pivX, 0.5f), ppu, 0, SpriteMeshType.FullRect);
            }
            sets[key] = s;
            return s;
        }

        public static Sprite One(string name, float widest, float pivX = 0.5f)
        {
            string key = "1:" + name + "|" + widest + "|" + pivX;
            if (sets.TryGetValue(key, out var s) && s[0] != null) return s[0];
            var t = Tex(name) ?? Texture2D.whiteTexture;
            var sp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(pivX, 0.5f), t.width / widest, 0, SpriteMeshType.FullRect);
            sets[key] = new[] { sp };
            return sp;
        }

        public static Sprite White()
        {
            if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }

    // ---------------------------------------------------------------- äänet (syntetisoitu)
    static class Sfx
    {
        public static AudioClip Laser, EShot, Boom, Big, Pick, Ping, Siren, Beam, Power, Disc, Hurt, Start;
        const int SR = 44100;
        static System.Random rnd = new System.Random(7);

        static AudioClip Make(string name, float dur, System.Func<float, float, float> f)
        {
            int n = Mathf.RoundToInt(dur * SR);
            var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)SR; d[i] = Mathf.Clamp(f(t, t / dur), -1f, 1f); }
            var c = AudioClip.Create(name, n, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }
        static float Noise() { return (float)(rnd.NextDouble() * 2.0 - 1.0); }
        static float Sq(float ph) { return Mathf.Sin(ph) >= 0f ? 1f : -1f; }
        static float Saw(float ph) { float x = ph / (2f * Mathf.PI); return 2f * (x - Mathf.Floor(x)) - 1f; }

        public static void Build()
        {
            if (Laser != null) return;
            float ph = 0f;
            Laser = Make("ss_laser", 0.09f, (t, u) => { ph += 2f * Mathf.PI * Mathf.Lerp(1500f, 520f, u) / SR; return Sq(ph) * 0.22f * Mathf.Pow(1f - u, 1.5f); });
            float ph2 = 0f;
            EShot = Make("ss_eshot", 0.16f, (t, u) => { ph2 += 2f * Mathf.PI * Mathf.Lerp(650f, 240f, u) / SR; return Saw(ph2) * 0.2f * (1f - u); });
            float lp = 0f, ph3 = 0f;
            Boom = Make("ss_boom", 0.5f, (t, u) => { lp += (Noise() - lp) * 0.12f; ph3 += 2f * Mathf.PI * Mathf.Lerp(110f, 35f, u) / SR; return (lp * 2.2f + Mathf.Sin(ph3) * 0.6f) * Mathf.Exp(-t * 7.5f) * 0.9f; });
            float lp2 = 0f, ph4 = 0f;
            Big = Make("ss_big", 1.3f, (t, u) => { lp2 += (Noise() - lp2) * 0.07f; ph4 += 2f * Mathf.PI * Mathf.Lerp(80f, 25f, u) / SR; return (lp2 * 2.6f + Mathf.Sin(ph4) * 0.7f) * Mathf.Exp(-t * 2.6f); });
            Pick = Make("ss_pick", 0.28f, (t, u) => { float fr = t < 0.07f ? 660f : (t < 0.14f ? 880f : 1320f); return Sq(2f * Mathf.PI * fr * t) * 0.18f * (1f - u); });
            Ping = Make("ss_ping", 0.05f, (t, u) => Mathf.Sin(2f * Mathf.PI * 1900f * t) * 0.18f * (1f - u));
            Siren = Make("ss_siren", 3.0f, (t, u) => { float fr = Mathf.Lerp(480f, 880f, 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 1.0f * t)); float p = 2f * Mathf.PI * fr * t; return Sq(p) * 0.16f * Mathf.Min(1f, (1f - u) * 8f); });
            float ph5 = 0f, ln = 0f;
            Beam = Make("ss_beam", 0.5f, (t, u) => { ph5 += 2f * Mathf.PI * (120f + 14f * Mathf.Sin(t * 60f)) / SR; ln += (Noise() - ln) * 0.3f; return (Saw(ph5) * 0.22f + ln * 0.12f); });
            float ph6 = 0f;
            Power = Make("ss_power", 0.4f, (t, u) => { ph6 += 2f * Mathf.PI * Mathf.Lerp(300f, 1300f, u) / SR; return Sq(ph6) * 0.18f * (1f - u * 0.5f); });
            float ph7 = 0f;
            Disc = Make("ss_disc", 0.32f, (t, u) => { ph7 += 2f * Mathf.PI * Mathf.Lerp(260f, 90f, u) / SR; return Mathf.Sin(ph7) * (0.55f + 0.45f * Mathf.Sin(t * 70f)) * 0.3f * (1f - u); });
            float ln2 = 0f;
            Hurt = Make("ss_hurt", 0.35f, (t, u) => { ln2 += (Noise() - ln2) * 0.25f; return (ln2 * 0.7f + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.4f) * Mathf.Exp(-t * 9f); });
            float ph8 = 0f;
            Start = Make("ss_start", 0.5f, (t, u) => { float fr = t < 0.12f ? 392f : (t < 0.24f ? 523f : (t < 0.36f ? 659f : 784f)); ph8 += 2f * Mathf.PI * fr / SR; return Sq(ph8) * 0.16f * (1f - u * 0.6f); });
        }
    }

    // ---------------------------------------------------------------- oliot
    class Ent
    {
        public Transform tr; public SpriteRenderer sr;
        public Vector2 p, v;
        public float hx, hy, hp, maxHp, life, flash, phase, a, b, spin, fps, timer, timer2, rot;
        public int kind, state, frame, score;
        public bool dead, shoots, pierce;
        public Color tint = Color.white;
        public int fade;   // tähtitausta: 1 = häivytetään sisään, -1 = ulos (kentän 2 tausta)
        public Sprite[] anim;
        public HashSet<Ent> seen;
    }

    enum St { Title, Play, Over, Win }
    St st = St.Title;
    float stT, openedAt;

    PlayerController pc; int pi;
    Transform root; Camera cam; AudioSource sfx, loopSrc, fastSrc, musicSrc, musicSrc2;
    float musicFadeT;
    bool prevListenerPause; float prevTimeScale = 1f;
    float halfW;
    const float HalfH = 4.5f;
    const float Sz = 0.8f;        // pelaajan, vihollisten ja ammusten koko suhteessa alkuperäiseen (pomot ja taustat eivät pienene)

    // pelaaja
    Ent hero, flame, shieldFx;
    Sprite[] heroSp, flameSp, shieldSp;
    int lives, weapon, shieldHits;
    float invuln, nextFire, nextBolt, charge, beamT, beamTick, deadT, hitFlashUntil;
    Ent beamEnt; Sprite[] beamSp; Ent hudDummy;
    int score, combo; float comboT;
    int hiScore;

    // listat
    readonly List<Ent> pShots = new List<Ent>(), eShots = new List<Ent>(), enemies = new List<Ent>(), pickups = new List<Ent>(), fxs = new List<Ent>(), bg = new List<Ent>(), debris = new List<Ent>();
    Ent boss; bool bossDying; float bossDeadT;
    readonly List<KeyValuePair<float, System.Action>> queue = new List<KeyValuePair<float, System.Action>>();
    float clock; int evIdx; float warnT; bool warned, bossSpawned;
    int level = 1; Ev[] cur; float curEnd; float bannerT;
    Sprite[] bossSp2, eshotS2, eshotB2, arcSp2, ballSp2, ebeamSp2;   // Tuhoajan omat (vihreät) ammukset: tulipallo, salamanuoli, kasvava kaari, plasmapallo
    static readonly Color Stage2Tint = new Color(0.62f, 0.9f, 1f, 1f);
    float shake; float whiteFlash, redPulse;
    float pingAt;
    float debrisT, planetT;
    Sprite[] explSp, shotSp, boltSp, enemySp, bossSp, pickSp, discSp, eshotS, eshotB, orbSp, ebeamSp, astSp, debrisSp;
    Sprite planetSp, warnSp, starsSp;
    Ent warnEnt, telegraph, ebeam; float bossBeamT, bossBeamWarn; float bossBeamY;
    float bossVolleyT, bossDiscT, bossOrbT, bossBeamCd;
    float bossStreamT, bossDroneT, bossArcT;
    float bossMuz = 3.3f;   // etäisyys pomon keskeltä aseiden/säteen alkuun
    GUIStyle gs, big, mid;

    // ---------------------------------------------------------------- alku ja loppu
    void Init(PlayerController who)
    {
        Instance = this; pc = who; pi = who.playerIndex; openedAt = Time.unscaledTime;
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        var rg = new GameObject("Juuri"); rg.transform.SetParent(transform, false); rg.transform.position = new Vector3(0f, 6000f, 0f); root = rg.transform;
        var cg = new GameObject("Kamera"); cg.transform.SetParent(transform, false); cg.transform.position = new Vector3(0f, 6000f, -10f);
        cam = cg.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = HalfH; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.depth = 99f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
        halfW = HalfH * Mathf.Clamp(cam.aspect, 1.3f, 2.4f);
        sfx = gameObject.AddComponent<AudioSource>(); sfx.spatialBlend = 0f; sfx.playOnAwake = false;
        fastSrc = gameObject.AddComponent<AudioSource>(); fastSrc.spatialBlend = 0f; fastSrc.playOnAwake = false;
        loopSrc = gameObject.AddComponent<AudioSource>(); loopSrc.spatialBlend = 0f; loopSrc.playOnAwake = false; loopSrc.loop = true; loopSrc.volume = 0.6f;
        musicSrc = gameObject.AddComponent<AudioSource>(); musicSrc.spatialBlend = 0f; musicSrc.playOnAwake = false; musicSrc.loop = true; musicSrc.volume = 0.5f;
        // muu maailma (hallin ääni, tappelu, musiikki) vaikenee peliajaksi; vain Tähtipyssyn omat äänet soivat
        prevListenerPause = AudioListener.pause; AudioListener.pause = true;
        // maailma jäätyy peliajaksi (tappelu hallissa ei jatka ääniä ja vie kaikkia äänikanavia); Tähtipyssy käyttää vain skaalaamatonta aikaa
        prevTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f; Time.timeScale = 0f;
        musicSrc2 = gameObject.AddComponent<AudioSource>(); musicSrc2.spatialBlend = 0f; musicSrc2.playOnAwake = false; musicSrc2.loop = true; musicSrc2.volume = 0f;
        sfx.ignoreListenerPause = fastSrc.ignoreListenerPause = loopSrc.ignoreListenerPause = musicSrc.ignoreListenerPause = musicSrc2.ignoreListenerPause = true;
        var mus = Resources.Load<AudioClip>("TahtiPyssy/musiikki");
        if (mus != null) { musicSrc.clip = mus; musicSrc.Play(); }
        Sfx.Build();
        LoadArt();
        hiScore = PlayerPrefs.GetInt(HiKey, 0);
        BuildBackground();
        st = St.Title; stT = 0f;
    }

    void LoadArt()
    {
        heroSp = Art.Set("hero", 3, 1.9f * Sz);
        flameSp = Art.Set("flame", 4, 0.74f, 1f);   // liekki 20 % pienempi kuin Sz-skaalattu (oli liian iso)
        shieldSp = Art.Set("shield", 3, 3.0f * Sz);
        shotSp = Art.Set("shot", 3, 0.6f * Sz);
        boltSp = Art.Set("bolt", 3, 1.9f * Sz);
        explSp = Art.Set("expl", 8, 2.4f);
        enemySp = new[] { Art.One("enemy_0", 1.15f * Sz), Art.One("enemy_1", 1.7f * Sz), Art.One("enemy_2", 3.0f * Sz) };
        bossSp = Art.Set("boss", 3, 7.2f);
        pickSp = Art.Set("pickup", 3, 0.85f * Sz);
        discSp = Art.Set("disc", 6, 1.0f * Sz);
        eshotS = Art.Set("eshot_s", 3, 0.5f * Sz);
        eshotB = Art.Set("eshot_b", 3, 0.9f * Sz);
        orbSp = Art.Set("orb", 3, 1.0f * Sz);
        ebeamSp = Art.Set("ebeam", 3, 3.4f, 1f);
        bool g = Art.Tex("e2s_0") != null;
        eshotS2 = g ? Art.Set("e2s", 5, 0.95f) : eshotS; eshotB2 = g ? Art.Set("e2b", 5, 1.35f) : eshotB;
        arcSp2 = g ? Art.Set("e2arc", 5, 1.1f) : discSp; ballSp2 = g ? Art.Set("e2ball", 5, 1.7f) : orbSp;
        ebeamSp2 = g ? Art.Set("e2beam", 3, 3.4f, 1f) : ebeamSp;
        beamSp = Art.Set("beam", 3, 10.6f, 0f);
        astSp = Art.Set("ast", 6, 1.4f * Sz);
        debrisSp = Art.Set("debris", 4, 7.5f);
        planetSp = Art.One("planet", 4.2f);
        warnSp = Art.One("warn", 7.0f);
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; AudioListener.pause = prevListenerPause; Time.timeScale = prevTimeScale; }
        foreach (var p in Players.All) if (p != null) p.enabled = true;
    }

    void End()
    {
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- apuja
    Ent Make(Sprite s, int order, Vector2 p, string name = "e")
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var e = new Ent { tr = go.transform, p = p };
        e.sr = go.AddComponent<SpriteRenderer>(); e.sr.sprite = s; e.sr.sortingOrder = order;
        e.tr.localPosition = new Vector3(p.x, p.y, 0f);
        return e;
    }

    void Kill(Ent e) { if (e == null || e.dead) return; e.dead = true; if (e.tr != null) Destroy(e.tr.gameObject); }

    void Sweep(List<Ent> l) { l.RemoveAll(e => e.dead || e.tr == null); }

    void Play(AudioClip c, float vol = 1f, float pitch = 1f)
    {
        if (c == null) return;
        var src = (c == Sfx.Laser || c == Sfx.EShot || c == Sfx.Ping) ? fastSrc : sfx;
        src.pitch = pitch; src.PlayOneShot(c, vol);
    }

    static float AngleDeg(Vector2 d) { return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; }

    void Anim(Ent e)
    {
        if (e.anim == null || e.anim.Length == 0) return;
        e.sr.sprite = e.anim[Mathf.FloorToInt(e.life * e.fps) % e.anim.Length];
    }

    void Sync(Ent e)
    {
        e.tr.localPosition = new Vector3(e.p.x, e.p.y, 0f);
        if (e.flash > 0f) e.sr.color = new Color(1f, 0.5f, 0.5f, 1f); else e.sr.color = e.tint;
    }

    // ---------------------------------------------------------------- tausta
    void BuildBackground()
    {
        var tex = Art.Tex("stars");
        if (tex != null) BuildStars(tex, false);
        // pienet kaukaiset asteroidit
        for (int i = 0; i < 6; i++) SpawnFloater(Random.Range(-halfW, halfW));
        SpawnPlanet(halfW * 0.55f);
    }

    bool starsAlt; float starFadeT;
    const float StarFadeTime = 3.5f;

    /// Tähtitausta kahtena kerroksena (hidas ja nopea, peilatut ruudut): sama kuva liikkuu eri nopeuksilla = parallaksi.
    void BuildStars(Texture2D tex, bool fadeIn)
    {
        float h = HalfH * 2f * 1.0f, w = h * tex.width / tex.height;
        var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.height / h, 0, SpriteMeshType.FullRect);
        if (!fadeIn) starsSp = sp;
        int n = Mathf.CeilToInt(halfW * 2f / w) + 2;
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < n; i++)
            {
                var e = Make(sp, -100 + layer, new Vector2(-halfW + i * w, 0f), "tähdet");
                e.sr.flipX = i % 2 == 1; e.sr.flipY = layer == 1;
                e.a = w; e.b = layer == 0 ? 0.7f : 1.8f; e.kind = n;
                if (layer == 1) { e.sr.color = new Color(1f, 1f, 1f, 0.55f); e.tr.localScale = new Vector3(1.4f, 1.4f, 1f); e.a = w * 1.4f; e.p.x = -halfW + i * e.a; }
                e.tint = e.sr.color; e.state = 7;   // state 7 = tähtitausta
                if (fadeIn) { e.fade = 1; e.sr.color = new Color(e.tint.r, e.tint.g, e.tint.b, 0f); }
                bg.Add(e);
            }
    }

    /// Kentän 2 tausta: vanha tausta häivytetään pois ja punainen häivytetään tilalle (ristiinhäivytys).
    void StartStarCrossfade()
    {
        var t2 = Art.Tex("stars2");
        if (t2 == null) { ApplyStageTint(true); return; }
        foreach (var e in bg) if (e != null && !e.dead && e.state == 7) e.fade = -1;
        BuildStars(t2, true);
        starsAlt = true; starFadeT = StarFadeTime;
    }

    void ResetStars()
    {
        ApplyStageTint(false);
        if (!starsAlt) return;
        foreach (var e in bg) if (e != null && !e.dead && e.state == 7) Kill(e);
        starsAlt = false; starFadeT = 0f;
        var tex = Art.Tex("stars");
        if (tex != null) BuildStars(tex, false);
    }

    void UpdateStarFade(float dt)
    {
        if (starFadeT <= 0f) return;
        starFadeT -= dt;
        float k = Mathf.Clamp01(1f - starFadeT / StarFadeTime);
        foreach (var e in bg)
        {
            if (e == null || e.dead || e.state != 7 || e.fade == 0) continue;
            float f = e.fade > 0 ? k : 1f - k;
            e.sr.color = new Color(e.tint.r, e.tint.g, e.tint.b, e.tint.a * f);
            if (starFadeT <= 0f) { if (e.fade < 0) Kill(e); else { e.fade = 0; e.sr.color = e.tint; } }
        }
    }

    void SpawnFloater(float x)
    {
        int k = 3 + Random.Range(0, 3);
        var e = Make(astSp[k], -70, new Vector2(x, Random.Range(-3.8f, 3.8f)), "kaukainen");
        e.v = new Vector2(-Random.Range(0.8f, 2.2f), 0f); e.spin = Random.Range(-30f, 30f); e.kind = 99;
        float sc = Random.Range(0.5f, 0.9f); e.tr.localScale = Vector3.one * sc;
        e.sr.color = new Color(0.6f, 0.65f, 0.8f, 1f);
        bg.Add(e);
    }

    void SpawnPlanet(float x)
    {
        var e = Make(planetSp, -80, new Vector2(x + 3f, Random.Range(-1.6f, 1.8f)), "planeetta");
        float sc = Random.Range(0.8f, 1.3f); e.tr.localScale = Vector3.one * sc;
        e.v = new Vector2(-0.35f, 0f); e.kind = 98; e.sr.color = new Color(0.85f, 0.9f, 1f, 1f);
        bg.Add(e);
    }

    void UpdateBackground(float dt)
    {
        UpdateStarFade(dt);
        foreach (var e in bg)
        {
            if (e.dead) continue;
            if (e.kind == 99 || e.kind == 98)
            {
                float mul = st == St.Play ? 1f : 0.5f;
                e.p += e.v * dt * mul; e.tr.localEulerAngles = new Vector3(0, 0, e.tr.localEulerAngles.z + e.spin * dt);
                if (e.p.x < -halfW - 5f) { if (e.kind == 99) { e.p.x = halfW + Random.Range(1f, 5f); e.p.y = Random.Range(-3.8f, 3.8f); } else Kill(e); }
            }
            else
            {
                e.p.x -= e.b * dt * (st == St.Play ? 1f : 0.5f);
                if (e.p.x < -halfW - e.a * 0.6f) e.p.x += e.a * e.kind;
            }
            e.tr.localPosition = new Vector3(e.p.x, e.p.y, 0f);
        }
        Sweep(bg);
        planetT -= dt;
        if (planetT <= 0f) { planetT = 55f; if (!bg.Exists(x => x.kind == 98)) SpawnPlanet(halfW + 1f); }
    }

    void UpdateDebris(float dt)
    {
        debrisT -= dt;
        if (st == St.Play && debrisT <= 0f && debrisSp != null)
        {
            debrisT = Random.Range(7f, 12f);
            bool top = Random.value < 0.5f;
            var e = Make(debrisSp[Random.Range(0, debrisSp.Length)], 60, new Vector2(halfW + 5f, top ? 4.3f : -4.3f), "romu");
            e.v = new Vector2(-Random.Range(7f, 10f), 0f); e.spin = 0f;
            e.sr.color = new Color(0.3f, 0.34f, 0.46f, 0.9f);
            e.tr.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
            e.sr.flipY = !top; e.sr.flipX = Random.value < 0.5f;
            debris.Add(e);
        }
        foreach (var e in debris) { e.p += e.v * dt; e.tr.localPosition = new Vector3(e.p.x, e.p.y, 0f); if (e.p.x < -halfW - 8f) Kill(e); }
        Sweep(debris);
    }

    // ---------------------------------------------------------------- päivitys
    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        stT += dt;
        UpdateMusicFade(dt);
        UpdateBackground(dt);
        UpdateDebris(dt);
        UpdateFx(dt);
        cam.transform.position = new Vector3(0f, 6000f, -10f) + (shake > 0.001f ? (Vector3)(Random.insideUnitCircle * shake) : Vector3.zero);
        shake = Mathf.Max(0f, shake - dt * 2.2f);
        whiteFlash = Mathf.Max(0f, whiteFlash - dt * 2.5f);

        bool esc = Keyboard_EscOrQ() || CoopInput.StartPressed(pi);
        if (Time.unscaledTime - openedAt < 0.35f) return;
        if (st == St.Title)
        {
            if (esc || CoopInput.Kick(pi)) { End(); return; }
            if (CoopInput.Punch(pi) || CoopInput.Pick(pi)) StartGame();
            return;
        }
        if (st == St.Over || st == St.Win)
        {
            if (esc || CoopInput.Kick(pi)) { End(); return; }
            if (stT > 1.2f && (CoopInput.Punch(pi) || CoopInput.Pick(pi))) { ClearGame(); StartGame(); }
            if (st == St.Win) { UpdateWinEffects(dt); }
            return;
        }
        if (esc) { End(); return; }
        UpdateGame(dt);
    }

    static bool Keyboard_EscOrQ()
    {
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame);
    }

    void StartGame()
    {
        ClearGame();
        st = St.Play; stT = 0f; clock = 0f; evIdx = 0; warned = false; bossSpawned = false; warnT = 0f;
        level = 1; cur = Script; curEnd = ScriptEnd; bannerT = 0f; ResetStars();
        ResetMusic();
        lives = 3; weapon = 1; shieldHits = 0; invuln = 1.5f; charge = 0f; score = 0; combo = 0; comboT = 0f; beamT = 0f; deadT = 0f; bossDying = false;
        nextFire = 0f; nextBolt = 0f;
        hero = Make(heroSp[0], 20, new Vector2(-halfW + 2.2f, 0f), "alus");
        flame = Make(flameSp[0], 19, hero.p, "liekki"); flame.anim = flameSp; flame.fps = 16f;
        shieldFx = Make(shieldSp[0], 22, hero.p, "kilpi"); shieldFx.anim = shieldSp; shieldFx.fps = 10f; shieldFx.sr.enabled = false;
        beamEnt = Make(beamSp[0], 16, hero.p, "säde"); beamEnt.anim = beamSp; beamEnt.fps = 24f; beamEnt.sr.enabled = false;
        Play(Sfx.Start, 0.9f);
    }

    void ClearGame()
    {
        foreach (var l in new[] { pShots, eShots, enemies, pickups, fxs })
        {
            foreach (var e in l) Kill(e);
            l.Clear();
        }
        Kill(hero); Kill(flame); Kill(shieldFx); Kill(beamEnt); Kill(boss); Kill(warnEnt); Kill(telegraph); Kill(ebeam);
        hero = flame = shieldFx = beamEnt = boss = warnEnt = telegraph = ebeam = null;
        queue.Clear(); loopSrc.Stop(); sfx.Stop();
        bossDying = false; whiteFlash = 0f; redPulse = 0f;
    }

    // ---------------------------------------------------------------- pelin kulku
    struct Ev { public float t; public char k; public int n; public float y, a; public int pat; }
    static Ev E(float t, char k, int n, float y = 0f, float a = 1f, int pat = 0) { return new Ev { t = t, k = k, n = n, y = y, a = a, pat = pat }; }
    static readonly Ev[] Script =
    {
        E(3f,'S',6, 2.3f,1.1f,0), E(9f,'S',6,-2.0f,1.1f,0), E(15f,'G',2), E(21f,'A',6), E(23f,'S',7,0f,1f,2),
        E(31f,'G',3), E(35f,'S',5,1.4f,1.2f,3), E(41f,'H',1), E(46f,'S',8,0f,1f,1), E(55f,'A',8), E(57f,'G',2),
        E(66f,'H',2), E(79f,'S',10,-1.5f,1.5f,3), E(83f,'G',3), E(93f,'A',10), E(95f,'S',6,0f,1f,1), E(106f,'H',1), E(108f,'G',4),
    };
    const float ScriptEnd = 118f;
    // kenttä 2: tiheämpi, nopeampia hyökkääjiä (K = kamikaze-drone, M = miina) ja enemmän raskaita
    static readonly Ev[] Script2 =
    {
        E(3f,'S',7, 2.0f,1.2f,0), E(7f,'K',4), E(11f,'S',7,-2.0f,1.2f,3), E(17f,'G',3), E(21f,'M',3),
        E(28f,'S',8,0f,1f,2), E(31f,'K',6), E(36f,'H',1), E(40f,'G',4), E(46f,'A',8), E(49f,'M',4),
        E(55f,'S',6,1.5f,1.3f,1), E(59f,'K',7), E(64f,'H',2), E(70f,'G',3), E(74f,'S',10,-1.5f,1.5f,3),
        E(80f,'M',5), E(85f,'K',8), E(90f,'H',2), E(94f,'G',4), E(100f,'A',10), E(103f,'S',8,0f,1f,1), E(109f,'H',1), E(111f,'K',6), E(114f,'M',4),
    };
    const float ScriptEnd2 = 124f;

    void UpdateGame(float dt)
    {
        clock += dt; if (bannerT > 0f) bannerT -= dt;
        if (st != St.Play) return;
        // tapahtumakäsikirjoitus
        while (evIdx < cur.Length && clock >= cur[evIdx].t) { RunEvent(cur[evIdx]); evIdx++; }
        for (int i = queue.Count - 1; i >= 0; i--)
            if (clock >= queue[i].Key) { var a = queue[i].Value; queue.RemoveAt(i); a(); }
        if (evIdx >= cur.Length && !warned && ((enemies.Count == 0 && queue.Count == 0) || clock > curEnd + 14f) && clock > curEnd) { warned = true; warnT = 2.8f; StartWarning(); }
        if (warned && !bossSpawned) { warnT -= dt; UpdateWarning(dt); if (warnT <= 0f) { bossSpawned = true; SpawnBoss(); } }

        UpdateHero(dt);
        UpdateEnemies(dt);
        if (boss != null) UpdateBoss(dt);
        UpdateShots(dt);
        UpdatePickups(dt);
        Collide();

        comboT -= dt; if (comboT <= 0f) combo = 0;
        if (invuln > 0f) invuln -= dt;
        if (st == St.Play && lives <= 0 && deadT <= 0f) { deadT = 1.6f; }
        if (deadT > 0f) { deadT -= dt; if (deadT <= 0f) GameOver(); }
    }

    void RunEvent(Ev e)
    {
        switch (e.k)
        {
            case 'S': SpawnScouts(e); break;
            case 'G':
                for (int i = 0; i < e.n; i++) { float y = Mathf.Lerp(-2.7f, 2.7f, (i + 0.5f) / e.n); float at = clock + i * 0.9f; queue.Add(new KeyValuePair<float, System.Action>(at, () => SpawnGunner(y))); }
                break;
            case 'H':
                for (int i = 0; i < e.n; i++) { float y = e.n == 1 ? 0f : Mathf.Lerp(-2.2f, 2.2f, (float)i / (e.n - 1)); float at = clock + i * 2.0f; queue.Add(new KeyValuePair<float, System.Action>(at, () => SpawnHeavy(y))); }
                break;
            case 'A':
                for (int i = 0; i < e.n; i++) queue.Add(new KeyValuePair<float, System.Action>(clock + i * 0.75f, () => SpawnAsteroid()));
                break;
            case 'K':
                for (int i = 0; i < e.n; i++) queue.Add(new KeyValuePair<float, System.Action>(clock + i * 0.45f, () => SpawnDrone()));
                break;
            case 'M':
                for (int i = 0; i < e.n; i++) { float y = Mathf.Lerp(-3.0f, 3.0f, (i + 0.5f) / e.n); queue.Add(new KeyValuePair<float, System.Action>(clock + i * 1.1f, () => SpawnMine(y))); }
                break;
        }
    }

    // ---------------------------------------------------------------- pelaaja
    void UpdateHero(float dt)
    {
        if (hero == null) return;
        if (lives <= 0) { hero.sr.enabled = false; flame.sr.enabled = false; shieldFx.sr.enabled = false; beamEnt.sr.enabled = false; loopSrc.Stop(); return; }
        Vector2 m = CoopInput.Move(pi);
        hero.p += m * 7.2f * dt;
        hero.p.x = Mathf.Clamp(hero.p.x, -halfW + 0.9f, halfW - 0.9f);
        hero.p.y = Mathf.Clamp(hero.p.y, -HalfH + 0.55f, HalfH - 0.55f);
        hero.sr.sprite = m.y > 0.3f ? heroSp[1] : (m.y < -0.3f ? heroSp[2] : heroSp[0]);
        hero.life += dt;
        // vilkkuu osuman jälkeen
        hero.sr.enabled = invuln <= 0f || Mathf.FloorToInt(Time.unscaledTime * 18f) % 2 == 0;
        hero.tr.localPosition = new Vector3(hero.p.x, hero.p.y, 0f);
        // liekki: pitenee kun liikkuu eteenpäin
        flame.life += dt; Anim(flame);
        // liekki moottorin suulle: suun korkeus riippuu aluksen kuvasta (suoraan / ylös / alas)
        float fy = m.y > 0.3f ? -0.005f : (m.y < -0.3f ? -0.03f : -0.04f);
        flame.p = hero.p + new Vector2(-0.68f, fy);
        float fs = 0.8f + 0.5f * Mathf.Clamp01(m.x * 0.5f + 0.5f);
        flame.tr.localScale = new Vector3(fs, 1f, 1f);
        flame.tr.localPosition = new Vector3(flame.p.x, flame.p.y, 0f);
        flame.sr.enabled = hero.sr.enabled;
        // kilpi
        shieldFx.sr.enabled = shieldHits > 0;
        if (shieldHits > 0) { shieldFx.life += dt; Anim(shieldFx); shieldFx.p = hero.p + new Vector2(0.1f, 0f); shieldFx.tr.localPosition = new Vector3(shieldFx.p.x, shieldFx.p.y, 0f); }

        // ampuminen
        nextFire -= dt; nextBolt -= dt;
        if (CoopInput.PunchHeld(pi) && nextFire <= 0f)
        {
            nextFire = weapon >= 3 ? 0.095f : 0.11f;
            Vector2 nose = hero.p + new Vector2(0.8f, 0f);
            if (weapon == 1) FireShot(nose, 0f);
            else if (weapon == 2) { FireShot(nose + new Vector2(0f, 0.2f), 0f); FireShot(nose + new Vector2(0f, -0.2f), 0f); }
            else { FireShot(nose, 0f); FireShot(nose + new Vector2(0f, 0.18f), 7f); FireShot(nose + new Vector2(0f, -0.18f), -7f); }
            Play(Sfx.Laser, 0.35f, Random.Range(0.95f, 1.08f));
        }
        if (weapon >= 4 && CoopInput.PunchHeld(pi) && nextBolt <= 0f)
        {
            nextBolt = 0.5f;
            var b = Make(boltSp[0], 13, hero.p + new Vector2(1.1f, 0f), "salama");
            b.anim = boltSp; b.fps = 18f; b.v = new Vector2(13f, 0f); b.hx = 0.9f; b.hy = 0.25f; b.pierce = true; b.seen = new HashSet<Ent>(); b.kind = 1;
            pShots.Add(b);
        }
        // erikoissäde
        if (beamT <= 0f && charge >= 100f && CoopInput.Special(pi)) { beamT = 2.3f; charge = 0f; if (!loopSrc.isPlaying) { loopSrc.clip = Sfx.Beam; loopSrc.Play(); } Play(Sfx.Power, 0.7f, 0.7f); }
        beamEnt.sr.enabled = beamT > 0f;
        if (beamT > 0f)
        {
            beamT -= dt; beamTick -= dt;
            beamEnt.life += dt; Anim(beamEnt);
            float x0 = hero.p.x + 1.1f, len = halfW + 1f - x0;
            beamEnt.p = new Vector2(x0, hero.p.y);
            beamEnt.tr.localPosition = new Vector3(x0, hero.p.y, 0f);
            float spW = beamEnt.sr.sprite.bounds.size.x;
            beamEnt.tr.localScale = new Vector3(len / spW, 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 40f), 1f);
            if (beamTick <= 0f)
            {
                beamTick = 0.08f;
                foreach (var e in enemies) if (!e.dead && e.p.x > x0 - 0.2f && Mathf.Abs(e.p.y - hero.p.y) < 0.55f + e.hy * Sz) Damage(e, 2.5f, e.p);
                if (boss != null && !bossDying && Mathf.Abs(boss.p.y - hero.p.y) < 0.55f + boss.hy) Damage(boss, 2.5f, new Vector2(boss.p.x - 3f, hero.p.y));
                foreach (var s in eShots) if (!s.dead && s.p.x > x0 && Mathf.Abs(s.p.y - hero.p.y) < 0.55f) { Kill(s); Pop(s.p, 0.5f); }
            }
            shake = Mathf.Max(shake, 0.04f);
            if (beamT <= 0f) loopSrc.Stop();
        }
    }

    void FireShot(Vector2 pos, float angleDeg)
    {
        var s = Make(shotSp[0], 12, pos, "laukaus");
        s.anim = shotSp; s.fps = 20f;
        float r = angleDeg * Mathf.Deg2Rad;
        s.v = new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * 17f; s.hx = 0.3f; s.hy = 0.1f;
        s.tr.localEulerAngles = new Vector3(0, 0, angleDeg);
        pShots.Add(s);
    }

    void HurtHero()
    {
        if (invuln > 0f || lives <= 0 || beamT > 1.9f) return;
        if (shieldHits > 0) { shieldHits--; invuln = 0.8f; Play(Sfx.Ping, 0.8f, 0.6f); whiteFlash = 0.25f; return; }
        lives--; weapon = Mathf.Max(1, weapon - 1); invuln = 2.2f; shake = 0.45f; whiteFlash = 0.5f;
        Play(Sfx.Hurt, 1f); Pop(hero.p, 1.6f);
        if (lives <= 0) { Pop(hero.p, 3.2f); Play(Sfx.Big, 1f); }
    }

    // ---------------------------------------------------------------- viholliset
    void SpawnScouts(Ev e)
    {
        for (int i = 0; i < e.n; i++)
        {
            int idx = i; Ev ev = e;
            queue.Add(new KeyValuePair<float, System.Action>(clock + i * (e.pat == 1 ? 0.5f : 0.38f), () => SpawnScout(idx, ev)));
        }
    }

    void SpawnScout(int i, Ev e)
    {
        float y = e.y;
        if (e.pat == 2) y = e.y + (i - (e.n - 1) * 0.5f) * 0.0f;
        var s = Make(enemySp[0], 10, new Vector2(halfW + 1.2f, y), "tiedustelija");
        if (level == 2) s.tint = new Color(0.7f, 1f, 0.85f);
        s.hx = 0.5f; s.hy = 0.22f; s.hp = s.maxHp = 2f; s.kind = 0; s.score = 100;
        s.b = y; s.a = e.a; s.state = e.pat; s.v = new Vector2(-5.6f, 0f); s.phase = 0f;
        if (e.pat == 2) { s.b = y + (i % 2 == 0 ? 1 : -1) * ((i + 1) / 2) * 0.7f; s.state = 2; }
        if (e.pat == 3) { s.shoots = true; s.timer = Random.Range(1.2f, 2.6f); s.state = 0; }
        s.p.y = s.b;
        enemies.Add(s);
    }

    void SpawnGunner(float y)
    {
        var s = Make(enemySp[1], 10, new Vector2(halfW + 1.5f, y), "tykkivene");
        if (level == 2) s.tint = new Color(0.9f, 0.75f, 1f);
        s.hx = 0.75f; s.hy = 0.32f; s.hp = s.maxHp = 7f; s.kind = 1; s.score = 250;
        s.a = halfW - 2.0f - Random.value * 3.0f; s.v = new Vector2(-3.6f, 0f); s.timer = Random.Range(1.0f, 1.8f);
        s.phase = Random.value * 6f;
        enemies.Add(s);
    }

    void SpawnHeavy(float y)
    {
        var s = Make(enemySp[2], 11, new Vector2(halfW + 2.8f, y), "raskas");
        if (level == 2) s.tint = new Color(1f, 0.82f, 0.6f);
        s.hx = 1.35f; s.hy = 0.5f; s.hp = s.maxHp = 28f; s.kind = 2; s.score = 700;
        s.a = halfW - 2.6f; s.v = new Vector2(-2.0f, 0f); s.timer = 2.0f; s.timer2 = 1.0f;
        enemies.Add(s);
    }

    void SpawnDrone()
    {
        var s = Make(enemySp[0], 10, new Vector2(halfW + 1.2f, Random.Range(-3.6f, 3.6f)), "drone");
        s.tint = new Color(1f, 0.55f, 0.5f);
        s.hx = 0.45f; s.hy = 0.2f; s.hp = s.maxHp = 3f; s.kind = 4; s.score = 150;
        s.v = new Vector2(-4.2f, 0f); s.phase = Random.value * 3f;
        enemies.Add(s);
    }

    void SpawnMine(float y)
    {
        var s = Make(astSp[0], 9, new Vector2(halfW + 1f, y), "miina");
        s.tint = new Color(1f, 0.45f, 0.4f);
        s.hx = s.hy = 0.55f; s.hp = s.maxHp = 6f; s.kind = 5; s.score = 200;
        s.v = new Vector2(-1.9f, 0f); s.spin = 40f;
        enemies.Add(s);
    }

    void SpawnAsteroid()
    {
        int k = Random.Range(0, 3);
        var s = Make(astSp[k], 9, new Vector2(halfW + 1f, Random.Range(-3.8f, 3.8f)), "asteroidi");
        float r = k == 0 ? 0.55f : (k == 1 ? 0.42f : 0.32f);
        s.hx = s.hy = r; s.hp = s.maxHp = k == 0 ? 7f : (k == 1 ? 4f : 2f); s.kind = 3; s.score = 50;
        s.v = new Vector2(-Random.Range(3f, 5.2f), Random.Range(-0.6f, 0.6f)); s.spin = Random.Range(-90f, 90f);
        enemies.Add(s);
    }

    void UpdateEnemies(float dt)
    {
        foreach (var e in enemies)
        {
            if (e.dead) continue;
            e.life += dt; if (e.flash > 0f) e.flash -= dt;
            Vector2 hp = hero != null && lives > 0 ? hero.p : new Vector2(-halfW, 0f);
            switch (e.kind)
            {
                case 0:
                    {
                        e.p.x += e.v.x * dt;
                        if (e.state == 1)
                        {   // syöksyy kohti pelaajaa kun lähestyy
                            if (e.p.x < halfW - 1f) e.b = Mathf.MoveTowards(e.b, hp.y, 2.2f * dt);
                            e.p.y = e.b;
                        }
                        else if (e.state == 2) e.p.y = e.b;
                        else e.p.y = e.b + e.a * Mathf.Sin(e.p.x * 0.85f + e.phase);
                        if (e.shoots) { e.timer -= dt; if (e.timer <= 0f && e.p.x < halfW - 1f && e.p.x > -halfW + 3f) { e.timer = Random.Range(1.8f, 3.2f); EnemyShot(e.p + new Vector2(-0.6f, 0f), AimAt(e.p, hp), 0); } }
                        float vy = (e.state == 1 || e.state == 2) ? 0f : e.a * 0.85f * Mathf.Cos(e.p.x * 0.85f + e.phase) * e.v.x;
                        e.tr.localEulerAngles = new Vector3(0, 0, AngleDeg(new Vector2(e.v.x, vy)) - 180f);
                        if (e.p.x < -halfW - 2f) Kill(e);
                        break;
                    }
                case 1:
                    {
                        if (e.state == 0) { e.p.x += e.v.x * dt; if (e.p.x <= e.a) e.state = 1; }
                        else if (e.state == 1)
                        {
                            float ty = hp.y + Mathf.Sin(e.life * 1.3f + e.phase) * 1.2f;
                            e.p.y = Mathf.MoveTowards(e.p.y, Mathf.Clamp(ty, -3.7f, 3.7f), 1.6f * dt);
                            e.timer -= dt;
                            if (e.timer <= 0f)
                            {
                                e.timer = Random.Range(1.5f, 2.3f) * (level == 2 ? 0.75f : 1f);
                                Vector2 d = AimAt(e.p, hp);
                                EnemyShot(e.p + new Vector2(-0.9f, 0f), d, 0);
                                if (clock > 60f || level == 2) { EnemyShot(e.p + new Vector2(-0.9f, 0f), Rot(d, 10f), 0); EnemyShot(e.p + new Vector2(-0.9f, 0f), Rot(d, -10f), 0); }
                            }
                            if (e.life > 15f) e.state = 2;
                        }
                        else e.p.x -= 4.5f * dt;
                        if (e.p.x < -halfW - 2f) Kill(e);
                        break;
                    }
                case 2:
                    {
                        if (e.p.x > e.a) e.p.x += e.v.x * dt;
                        else
                        {
                            float ty = hp.y * 0.8f + Mathf.Sin(e.life * 0.9f) * 1.4f;
                            e.p.y = Mathf.MoveTowards(e.p.y, Mathf.Clamp(ty, -3.5f, 3.5f), 1.0f * dt);
                            e.timer -= dt; e.timer2 -= dt;
                            if (e.timer <= 0f)
                            {
                                e.timer = 2.6f;
                                Vector2 d = AimAt(e.p, hp);
                                for (int i = -1; i <= 1; i++) EnemyShot(e.p + new Vector2(-1.4f, 0f), Rot(d, i * 17f), 1);
                            }
                            if (e.timer2 <= 0f)
                            {
                                e.timer2 = 1.1f;
                                EnemyShot(e.p + new Vector2(-1.2f, 0.45f), AimAt(e.p, hp), 0);
                                EnemyShot(e.p + new Vector2(-1.2f, -0.45f), AimAt(e.p, hp), 0);
                            }
                            if (e.life > 22f) e.p.x += -3f * dt;
                        }
                        if (e.p.x < -halfW - 3f) Kill(e);
                        break;
                    }
                case 4:
                    {   // drone: kääntyy hitaasti kohti pelaajaa
                        float dy = Mathf.Clamp(hp.y - e.p.y, -1f, 1f);
                        e.p.y += dy * (e.p.x > hp.x + 1.5f ? 3.2f : 0.4f) * dt;
                        e.p.x += e.v.x * dt;
                        e.tr.localEulerAngles = new Vector3(0, 0, AngleDeg(new Vector2(e.v.x, dy * 3f)) - 180f);
                        if (e.p.x < -halfW - 2f) Kill(e);
                        break;
                    }
                case 5:
                    e.p += e.v * dt;
                    e.tr.localEulerAngles = new Vector3(0, 0, e.tr.localEulerAngles.z + e.spin * dt);
                    if (e.p.x < -halfW - 2f) Kill(e);
                    break;
                case 3:
                    e.p += e.v * dt;
                    e.tr.localEulerAngles = new Vector3(0, 0, e.tr.localEulerAngles.z + e.spin * dt);
                    if (e.p.x < -halfW - 2f) Kill(e);
                    break;
            }
            if (!e.dead) Sync(e);
        }
        Sweep(enemies);
    }

    static Vector2 Rot(Vector2 v, float deg) { float r = deg * Mathf.Deg2Rad; float c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }
    static Vector2 AimAt(Vector2 from, Vector2 to) { Vector2 d = to - from; if (d.x > -0.5f) d.x = -0.5f; return d.normalized; }

    void EnemyShot(Vector2 pos, Vector2 dir, int type, bool green = false)
    {
        Sprite[] set = green ? (type == 0 ? eshotS2 : (type == 1 ? eshotB2 : (type == 4 ? arcSp2 : ballSp2))) : (type == 0 ? eshotS : (type == 1 ? eshotB : (type == 2 ? discSp : orbSp)));
        var s = Make(set[0], 15, pos, "vihollisen laukaus");
        s.anim = set; s.fps = type >= 2 ? 12f : 14f; s.kind = type;
        float speed = type == 0 ? 6.8f : (type == 1 ? 5.4f : (type == 2 ? 3.6f : (type == 4 ? 5.6f : (type == 5 ? 2.3f : 4.4f))));
        s.v = dir.normalized * speed;
        float r = type == 0 ? 0.14f : (type == 1 ? 0.24f : (type == 2 ? 0.38f : (type == 5 ? 0.7f : 0.45f)));
        s.hx = s.hy = r;
        if (type == 4) { s.hx = 0.3f; s.hy = 0.75f; s.tr.localScale = Vector3.one * 0.5f; }   // kaari kasvaa matkalla (UpdateShots)
        if (type < 2 || type == 4) s.tr.localEulerAngles = new Vector3(0, 0, AngleDeg(dir) - 180f);
        eShots.Add(s);
        if (type == 2) Play(Sfx.Disc, 0.4f, Random.Range(0.9f, 1.1f));
        else if (type == 3) Play(Sfx.Disc, 0.5f, 0.7f);
        else if (type == 4) Play(Sfx.Disc, 0.55f, 1.25f);
        else if (type == 5) Play(Sfx.Big, 0.45f, 1.6f);
        else Play(Sfx.EShot, type == 1 ? 0.35f : 0.22f, Random.Range(0.9f, 1.1f));
    }

    // ---------------------------------------------------------------- ammukset ja osumat
    void UpdateShots(float dt)
    {
        foreach (var s in pShots)
        {
            if (s.dead) continue;
            s.life += dt; s.p += s.v * dt; Anim(s); s.tr.localPosition = new Vector3(s.p.x, s.p.y, 0f);
            if (s.p.x > halfW + 2f) Kill(s);
        }
        Sweep(pShots);
        foreach (var s in eShots)
        {
            if (s.dead) continue;
            s.life += dt; s.p += s.v * dt; Anim(s); s.tr.localPosition = new Vector3(s.p.x, s.p.y, 0f);
            if (s.kind == 4) { float sc = Mathf.Lerp(0.5f, 1.9f, Mathf.Clamp01(s.life / 2.2f)); s.tr.localScale = Vector3.one * sc; s.hx = 0.3f * sc; s.hy = 0.75f * sc; }
            else if (s.kind == 5 && s.sr != null) s.tr.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(s.life * 7f));
            if (s.p.x < -halfW - 2f || s.p.x > halfW + 3f || Mathf.Abs(s.p.y) > HalfH + 2f) Kill(s);
        }
        Sweep(eShots);
    }

    void Collide()
    {
        // pelaajan ammukset -> viholliset ja pomo
        foreach (var s in pShots)
        {
            if (s.dead) continue;
            foreach (var e in enemies)
            {
                if (e.dead) continue;
                if (Mathf.Abs(s.p.x - e.p.x) < (s.hx + e.hx) * Sz && Mathf.Abs(s.p.y - e.p.y) < (s.hy + e.hy) * Sz)
                {
                    if (s.pierce) { if (s.seen.Contains(e)) continue; s.seen.Add(e); Damage(e, 4f, s.p); }
                    else { Damage(e, 1f, s.p); Kill(s); Spark(s.p); break; }
                }
            }
            if (s.dead) continue;
            if (boss != null && !bossDying && Mathf.Abs(s.p.x - boss.p.x) < s.hx + boss.hx && Mathf.Abs(s.p.y - boss.p.y) < s.hy + boss.hy + 0.05f)
            {
                if (s.pierce) { if (!s.seen.Contains(boss)) { s.seen.Add(boss); Damage(boss, 4f, s.p); } }
                else { Damage(boss, 1f, s.p); Kill(s); Spark(s.p); }
            }
        }
        if (hero == null || lives <= 0) return;
        // vihollisen ammukset -> pelaaja
        foreach (var s in eShots)
        {
            if (s.dead) continue;
            if (Mathf.Abs(s.p.x - (hero.p.x + 0.1f)) < (s.hx + 0.5f) * Sz && Mathf.Abs(s.p.y - hero.p.y) < (s.hy + 0.17f) * Sz)
            {
                if (invuln <= 0f) { Kill(s); Pop(s.p, 0.7f); }
                HurtHero();
            }
        }
        // vihollisen runko -> pelaaja
        foreach (var e in enemies)
        {
            if (e.dead) continue;
            if (Mathf.Abs(e.p.x - hero.p.x) < (e.hx + 0.5f) * Sz && Mathf.Abs(e.p.y - hero.p.y) < (e.hy + 0.17f) * Sz)
            {
                if (invuln <= 0f) { Damage(e, e.kind == 3 ? 4f : 6f, e.p); }
                HurtHero();
            }
        }
        if (boss != null && !bossDying && Mathf.Abs(boss.p.x - hero.p.x) < boss.hx + 0.2f && Mathf.Abs(boss.p.y - hero.p.y) < boss.hy + 0.15f) HurtHero();
        // pomon säde
        if (ebeam != null && boss != null && bossBeamT > 0f && Mathf.Abs(hero.p.y - bossBeamY) < 0.42f + 0.17f && hero.p.x < boss.p.x - bossMuz + 0.3f) HurtHero();
    }

    void Damage(Ent e, float d, Vector2 at)
    {
        if (e.dead) return;
        e.hp -= d; e.flash = 0.06f;
        if (e == boss) { if (Time.unscaledTime - pingAt > 0.07f) { pingAt = Time.unscaledTime; Play(Sfx.Ping, 0.4f, Random.Range(0.8f, 1.1f)); } }
        else if (e.kind >= 1 && Time.unscaledTime - pingAt > 0.08f) { pingAt = Time.unscaledTime; Play(Sfx.Ping, 0.25f, Random.Range(0.9f, 1.3f)); }
        if (e.hp > 0f) return;
        if (e == boss) { StartBossDeath(); return; }
        KillEnemy(e);
    }

    void KillEnemy(Ent e)
    {
        float size = e.kind == 2 ? 3.2f : (e.kind == 1 ? 2.0f : (e.kind == 3 ? 1.3f : (e.kind == 5 ? 2.2f : 1.5f)));
        Pop(e.p, size);
        Play(e.kind == 2 ? Sfx.Big : Sfx.Boom, e.kind == 2 ? 0.8f : 0.55f, Random.Range(0.9f, 1.1f));
        if (e.kind == 2) shake = Mathf.Max(shake, 0.25f);
        if (e.kind == 5) { for (int i = 0; i < 8; i++) EnemyShot(e.p, Rot(new Vector2(1f, 0f), i * 45f + 22f), 0); }   // miina räjähtää luotirenkaaksi
        combo++; comboT = 2.0f;
        int mul = Mathf.Min(4, 1 + combo / 6);
        score += e.score * mul;
        charge = Mathf.Min(100f, charge + (e.kind == 0 ? 3f : (e.kind == 1 ? 8f : (e.kind == 2 ? 20f : (e.kind == 4 ? 3f : (e.kind == 5 ? 5f : 2f))))));
        // pudotukset
        if (e.kind == 2) DropPickup(e.p, weapon < 4 ? 0 : 1);
        else if (e.kind == 1 && Random.value < 0.22f) DropPickup(e.p, Random.value < 0.5f ? 1 : 2);
        else if (e.kind == 0 && Random.value < 0.09f) DropPickup(e.p, 2);
        else if ((e.kind == 3 || e.kind == 4) && Random.value < 0.12f) DropPickup(e.p, 2);
        else if (e.kind == 5 && Random.value < 0.2f) DropPickup(e.p, Random.value < 0.5f ? 1 : 2);
        Kill(e);
    }

    void Pop(Vector2 pos, float size)
    {
        var x = Make(explSp[0], 30, pos, "räjähdys");
        x.anim = explSp; x.fps = 15f; x.timer = explSp.Length / 15f;
        x.tr.localScale = Vector3.one * (size / 2.4f);
        x.tr.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
        fxs.Add(x);
    }

    void Spark(Vector2 pos)
    {
        var x = Make(explSp[0], 29, pos, "kipinä");
        x.anim = new[] { explSp[0], explSp[1], explSp[1] }; x.fps = 22f; x.timer = 3f / 22f;
        x.tr.localScale = Vector3.one * 0.18f;
        fxs.Add(x);
    }

    void UpdateFx(float dt)
    {
        foreach (var x in fxs)
        {
            if (x.dead) continue;
            x.life += dt; x.timer -= dt;
            if (x.timer <= 0f) { Kill(x); continue; }
            Anim(x); x.p += x.v * dt; x.tr.localPosition = new Vector3(x.p.x, x.p.y, 0f);
        }
        Sweep(fxs);
    }

    // ---------------------------------------------------------------- kerättävät
    void DropPickup(Vector2 pos, int type)
    {
        var p = Make(pickSp[type], 18, pos, "kerättävä");
        p.kind = type; p.hx = p.hy = 0.42f; p.v = new Vector2(-1.7f, 0f); p.phase = Random.value * 6f;
        pickups.Add(p);
    }

    void UpdatePickups(float dt)
    {
        foreach (var p in pickups)
        {
            if (p.dead) continue;
            p.life += dt;
            if (hero != null && lives > 0 && (hero.p - p.p).sqrMagnitude < 5f) p.p = Vector2.MoveTowards(p.p, hero.p, 5f * dt);
            else p.p += p.v * dt;
            p.tr.localPosition = new Vector3(p.p.x, p.p.y + Mathf.Sin(p.life * 4f + p.phase) * 0.08f, 0f);
            p.tr.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(p.life * 7f));
            if (p.p.x < -halfW - 1f) { Kill(p); continue; }
            if (hero != null && lives > 0 && Mathf.Abs(p.p.x - hero.p.x) < (0.6f + p.hx) * Sz && Mathf.Abs(p.p.y - hero.p.y) < (0.35f + p.hy) * Sz)
            {
                Collect(p.kind); Kill(p);
            }
        }
        Sweep(pickups);
    }

    void Collect(int kind)
    {
        if (kind == 0) { if (weapon < 4) { weapon++; Play(Sfx.Power, 0.8f); } else { score += 500; Play(Sfx.Pick, 0.8f); } }
        else if (kind == 1) { shieldHits = Mathf.Min(2, shieldHits + 1); Play(Sfx.Power, 0.8f, 1.2f); }
        else { score += 200; charge = Mathf.Min(100f, charge + 10f); Play(Sfx.Pick, 0.8f); }
    }

    // ---------------------------------------------------------------- pomo
    void StartWarning()
    {
        warnEnt = Make(warnSp, 60, new Vector2(0f, 1.5f), "varoitus");
        redPulse = 1f;
        loopSrc.clip = Sfx.Siren; loopSrc.loop = false; loopSrc.volume = 0.6f; loopSrc.Play();
    }

    void UpdateWarning(float dt)
    {
        if (warnEnt == null) return;
        float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
        warnEnt.sr.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * a);
        if (warnT <= 0.4f) { Kill(warnEnt); warnEnt = null; loopSrc.Stop(); loopSrc.loop = true; }   // sireeni loppuu varoituksen mukana (ei jää silmukkaan)
    }

    void SpawnBoss()
    {
        boss = Make(BossSprites()[0], 11, new Vector2(halfW + 5f, 0f), "pomo");
        boss.hx = 3.2f; boss.hy = 0.75f; boss.hp = boss.maxHp = level == 2 ? 950f : 650f; boss.kind = 10; boss.score = level == 2 ? 15000 : 10000;
        if (level == 2)
        {
            if (Art.Tex("boss2_0") == null) boss.tint = new Color(0.55f, 1f, 0.85f);   // ei omaa kuvaa vielä: Kurittajan kuvat sävytettyinä
            boss.state = 0; bossStreamT = 3f; bossDroneT = 4f; bossVolleyT = 2.5f; bossDiscT = 4f; bossArcT = 7f; boss.timer2 = 7f;
        }
        boss.a = halfW - 4.6f; bossMuz = 3.3f;
        if (level == 2) { boss.hx = 2.5f; boss.hy = 1.2f; boss.a = halfW - 3.6f; bossMuz = 2.2f; }
        bossVolleyT = 2.5f; bossDiscT = 4.5f; bossOrbT = 7f; bossBeamCd = 9f; bossBeamT = 0f; bossBeamWarn = 0f;
        telegraph = Make(Art.White(), 8, new Vector2(0f, 0f), "ennakointi"); telegraph.sr.enabled = false; telegraph.sr.color = new Color(1f, 0.1f, 0.05f, 0.4f);
        ebeam = Make((level == 2 ? ebeamSp2 : ebeamSp)[0], 14, new Vector2(0f, 0f), "pomon säde"); ebeam.anim = level == 2 ? ebeamSp2 : ebeamSp; ebeam.fps = 22f; ebeam.sr.enabled = false;
    }

    void UpdateBoss(float dt)
    {
        var b = boss;
        b.life += dt; if (b.flash > 0f) b.flash -= dt;
        if (bossDying) { UpdateBossDeath(dt); return; }
        if (level == 2) { UpdateBoss2(dt); return; }
        float frac = b.hp / b.maxHp;
        int ph = frac > 0.66f ? 0 : (frac > 0.33f ? 1 : 2);
        b.sr.sprite = bossSp[ph];
        if (b.p.x > b.a) { b.p.x -= 2.6f * dt; }
        else
        {
            b.p.y = Mathf.Sin(b.life * (0.6f + ph * 0.15f)) * 2.3f;
            b.p.x = b.a + Mathf.Sin(b.life * 0.5f) * 0.5f;
        }
        Sync(b);
        if (b.p.x > b.a + 0.8f) return;
        Vector2 hp = hero != null && lives > 0 ? hero.p : new Vector2(-halfW, 0f);
        // tykit
        bossVolleyT -= dt;
        if (bossVolleyT <= 0f)
        {
            bossVolleyT = ph == 0 ? 1.25f : (ph == 1 ? 0.95f : 0.7f);
            Vector2[] mz = { new Vector2(-2.1f, 0.85f), new Vector2(-2.1f, -0.85f), new Vector2(0.7f, 0.85f), new Vector2(0.7f, -0.85f) };
            int a = Random.Range(0, 4), c = (a + 1 + Random.Range(0, 3)) % 4;
            EnemyShot(b.p + mz[a], AimAt(b.p + mz[a], hp), 0);
            EnemyShot(b.p + mz[c], AimAt(b.p + mz[c], hp), ph >= 1 ? 1 : 0);
        }
        // pulssikiekot
        bossDiscT -= dt;
        if (bossDiscT <= 0f)
        {
            bossDiscT = ph == 0 ? 4.2f : (ph == 1 ? 3.4f : 2.7f);
            Vector2 d = AimAt(b.p, hp);
            int n = ph == 0 ? 3 : (ph == 1 ? 5 : 7);
            for (int i = 0; i < n; i++) EnemyShot(b.p + new Vector2(-3.0f, 0f), Rot(d, (i - (n - 1) * 0.5f) * 14f), 2);
        }
        // tulipallot (vaihe 2+)
        if (ph >= 1)
        {
            bossOrbT -= dt;
            if (bossOrbT <= 0f) { bossOrbT = ph == 1 ? 5.5f : 4f; EnemyShot(b.p + new Vector2(-3.2f, 0f), AimAt(b.p, hp), 3); }
        }
        // liekkisäde (vaihe 3)
        if (ph >= 2)
        {
            if (bossBeamWarn > 0f)
            {
                bossBeamWarn -= dt;
                telegraph.sr.enabled = true;
                float len = b.p.x - 3.3f + halfW + 1f;
                telegraph.p = new Vector2(-halfW - 1f + len * 0.5f, bossBeamY); telegraph.tr.localPosition = new Vector3(telegraph.p.x, telegraph.p.y, 0f);
                telegraph.tr.localScale = new Vector3(len, 0.08f + 0.5f * (1f - bossBeamWarn / 1.2f), 1f);
                telegraph.sr.color = new Color(1f, 0.15f, 0.05f, 0.25f + 0.3f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f)));
                if (bossBeamWarn <= 0f) { telegraph.sr.enabled = false; bossBeamT = 1.3f; Play(Sfx.Big, 0.5f, 1.5f); loopSrc.clip = Sfx.Beam; loopSrc.loop = true; loopSrc.Play(); }
            }
            else if (bossBeamT > 0f)
            {
                bossBeamT -= dt; ebeam.sr.enabled = true; ebeam.life += dt; Anim(ebeam);
                float x0 = b.p.x - 3.3f, len = x0 + halfW + 1f;
                ebeam.p = new Vector2(x0, bossBeamY); ebeam.tr.localPosition = new Vector3(x0, bossBeamY, 0f);
                ebeam.tr.localScale = new Vector3(len / ebeam.sr.sprite.bounds.size.x, 1f, 1f);
                shake = Mathf.Max(shake, 0.12f);
                if (bossBeamT <= 0f) { ebeam.sr.enabled = false; loopSrc.Stop(); bossBeamCd = 6.5f; }
            }
            else
            {
                bossBeamCd -= dt;
                if (bossBeamCd <= 0f) { bossBeamWarn = 1.2f; bossBeamY = Mathf.Clamp(hp.y, -3.6f, 3.6f); Play(Sfx.Ping, 0.6f, 0.4f); }
            }
        }
    }

    void StartBossDeath()
    {
        bossDying = true; bossDeadT = 0f;
        foreach (var s in eShots) { Pop(s.p, 0.6f); Kill(s); }
        telegraph.sr.enabled = false; ebeam.sr.enabled = false; loopSrc.Stop(); bossBeamT = 0f; bossBeamWarn = 0f;
        score += 10000;
        Play(Sfx.Big, 1f, 0.8f);
    }

    float bossPopT;
    void UpdateBossDeath(float dt)
    {
        bossDeadT += dt; bossPopT -= dt;
        boss.sr.color = Color.Lerp(Color.white, new Color(0.35f, 0.3f, 0.3f), Mathf.Clamp01(bossDeadT / 3f));
        boss.p.y -= 0.25f * dt; boss.tr.localPosition = new Vector3(boss.p.x, boss.p.y, 0f);
        shake = Mathf.Max(shake, 0.18f);
        if (bossPopT <= 0f && bossDeadT < 3.4f)
        {
            bossPopT = 0.11f;
            Pop(boss.p + new Vector2(Random.Range(-3.2f, 3.2f), Random.Range(-0.7f, 0.7f)), Random.Range(1.2f, 2.8f));
            Play(Sfx.Boom, 0.7f, Random.Range(0.7f, 1.1f));
        }
        if (bossDeadT >= 3.4f)
        {
            Pop(boss.p, 8f); Play(Sfx.Big, 1f, 0.6f); whiteFlash = 1f; shake = 0.8f;
            Kill(boss); boss = null;
            if (level == 1) NextStage(); else Win();
        }
    }

    const float MusicVol = 0.5f;

    /// Kenttä 2: ykkösmusiikki häivytetään pois ja kakkosmusiikki tilalle samaan aikaan kuin tausta.
    void StartMusicCrossfade()
    {
        var m2 = Resources.Load<AudioClip>("TahtiPyssy/musiikki2");
        if (m2 == null) return;
        musicSrc2.clip = m2; musicSrc2.volume = 0f; musicSrc2.Play();
        musicFadeT = StarFadeTime;
    }

    void ResetMusic()
    {
        musicFadeT = 0f;
        musicSrc2.Stop(); musicSrc2.volume = 0f;
        musicSrc.volume = MusicVol;
        if (!musicSrc.isPlaying && musicSrc.clip != null) musicSrc.Play();
    }

    void UpdateMusicFade(float dt)
    {
        if (musicFadeT <= 0f) return;
        musicFadeT -= dt;
        float k = Mathf.Clamp01(1f - musicFadeT / StarFadeTime);
        musicSrc.volume = MusicVol * (1f - k); musicSrc2.volume = MusicVol * k;
        if (musicFadeT <= 0f) musicSrc.Stop();
    }

    void ApplyStageTint(bool on)
    {
        foreach (var e in bg) if (e != null && !e.dead && e.state == 7 && e.sr != null) e.sr.color = on ? e.tint * Stage2Tint : e.tint;
    }

    /// Pomon kaaduttua alkaa kenttä 2: ammukset ja viholliset pois, pieni palkkio, uusi käsikirjoitus ja uusi pomo.
    void NextStage()
    {
        level = 2; cur = Script2; curEnd = ScriptEnd2;
        foreach (var l in new[] { eShots, pShots, enemies, pickups }) { foreach (var e in l) Kill(e); l.Clear(); }
        queue.Clear();
        clock = 0f; evIdx = 0; warned = false; bossSpawned = false; warnT = 0f;
        boss = null; bossDying = false; bossBeamT = 0f; bossBeamWarn = 0f;
        score += 2000 + lives * 1500;
        lives = Mathf.Min(5, lives + 1); shieldHits = Mathf.Max(shieldHits, 1); invuln = 3f;
        bannerT = 4.2f; whiteFlash = 0.6f;
        Play(Sfx.Start, 1f, 0.8f);
        StartStarCrossfade();
        StartMusicCrossfade();
        if (hero != null) hero.p = new Vector2(-halfW + 2.2f, 0f);
        loopSrc.Stop();
    }

    // ---------------------------------------------------------------- pomo 2: TUHOAJA
    Sprite[] BossSprites()
    {
        if (level != 2) return bossSp;
        if (bossSp2 == null) bossSp2 = Art.Tex("boss2_0") != null ? Art.Set("boss2", 3, 5.6f) : bossSp;
        return bossSp2;
    }

    void UpdateBoss2(float dt)
    {
        var b = boss;
        float frac = b.hp / b.maxHp;
        int ph = frac > 0.66f ? 0 : (frac > 0.33f ? 1 : 2);
        b.sr.sprite = BossSprites()[ph];
        Vector2 hp = hero != null && lives > 0 ? hero.p : new Vector2(-halfW, 0f);
        if (b.state == 0 && b.p.x > b.a + 0.8f) { b.p.x -= 2.6f * dt; Sync(b); return; }
        bool beamBusy = bossBeamWarn > 0f || bossBeamT > 0f;

        // rynnäkkö (vaihe 3): varoitusviiva, syöksy vasemmalle ruudun yli ja paluu oikealta
        if (b.state == 1)
        {
            b.timer -= dt;
            telegraph.sr.enabled = true;
            float len = b.p.x + halfW + 1f;
            telegraph.p = new Vector2(-halfW - 1f + len * 0.5f, b.p.y); telegraph.tr.localPosition = new Vector3(telegraph.p.x, telegraph.p.y, 0f);
            telegraph.tr.localScale = new Vector3(len, 0.9f, 1f);
            telegraph.sr.color = new Color(0.2f, 1f, 0.45f, 0.2f + 0.3f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 16f)));
            if (b.timer <= 0f) { b.state = 2; telegraph.sr.enabled = false; Play(Sfx.Big, 0.7f, 1.3f); shake = Mathf.Max(shake, 0.3f); }
            Sync(b); return;
        }
        if (b.state == 2)
        {
            b.p.x -= 19f * dt; shake = Mathf.Max(shake, 0.1f);
            if (b.p.x < -halfW - 4.5f) { b.state = 3; b.p.y = Mathf.Clamp(hp.y, -2.8f, 2.8f); b.p.x = halfW + 6f; }
            Sync(b); return;
        }
        if (b.state == 3)
        {
            b.p.x -= 8f * dt;
            if (b.p.x <= b.a) { b.state = 0; b.timer2 = 7.5f; }
            Sync(b); return;
        }

        b.p.y = Mathf.Sin(b.life * (0.7f + ph * 0.18f)) * 2.7f;
        b.p.x = b.a + Mathf.Sin(b.life * 0.55f) * 0.8f;
        Sync(b);

        // viuhka isoja ammuksia
        bossVolleyT -= dt;
        if (bossVolleyT <= 0f)
        {
            bossVolleyT = ph == 0 ? 1.8f : (ph == 1 ? 1.5f : 1.25f);
            Vector2 d = AimAt(b.p, hp);
            int n = 5 + ph * 2;
            for (int i = 0; i < n; i++) EnemyShot(b.p + new Vector2(-bossMuz, 0f), Rot(d, (i - (n - 1) * 0.5f) * 11f), 1, true);
        }
        // iso hidas plasmapallo (vaihe 3: kaksi)
        bossDiscT -= dt;
        if (bossDiscT <= 0f)
        {
            bossDiscT = ph == 0 ? 7f : (ph == 1 ? 5.5f : 4.5f);
            Vector2 d = AimAt(b.p, hp);
            if (ph < 2) EnemyShot(b.p + new Vector2(-bossMuz, 0f), d, 5, true);
            else { EnemyShot(b.p + new Vector2(-bossMuz, 0.5f), Rot(d, 10f), 5, true); EnemyShot(b.p + new Vector2(-bossMuz, -0.5f), Rot(d, -10f), 5, true); }
        }
        // kasvava kaari (vaihe 2+; vaiheessa 3 kolmen viuhka)
        if (ph >= 1)
        {
            bossArcT -= dt;
            if (bossArcT <= 0f)
            {
                bossArcT = ph == 1 ? 5.5f : 4f;
                Vector2 d = AimAt(b.p, hp);
                if (ph == 1) EnemyShot(b.p + new Vector2(-bossMuz, 0f), d, 4, true);
                else for (int i = -1; i <= 1; i++) EnemyShot(b.p + new Vector2(-bossMuz, 0f), Rot(d, i * 15f), 4, true);
            }
        }
        // pyyhkivä luotisuihku (vaihe 2+): vaiheessa 3 tiheämpi ja leveämpi
        if (ph >= 1)
        {
            bossStreamT -= dt;
            if (bossStreamT <= 0f)
            {
                bossStreamT = ph == 1 ? 0.32f : 0.14f;
                float sweep = ph == 1 ? 35f : 70f;
                float ang = 180f + Mathf.Sin(b.life * (ph == 1 ? 2.2f : 2.8f)) * sweep;
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                EnemyShot(b.p + new Vector2(-bossMuz, 0f), dir, 0, true);
            }
        }
        // droonit
        bossDroneT -= dt;
        if (bossDroneT <= 0f)
        {
            bossDroneT = ph == 0 ? 8f : 6.5f;
            if (enemies.Count < 7) for (int i = 0; i < 2; i++)
            {
                SpawnDrone();
                var dr = enemies[enemies.Count - 1]; dr.p = new Vector2(b.p.x - bossMuz + 0.4f, b.p.y + (i == 0 ? 0.9f : -0.9f)); Sync(dr);
            }
        }
        // liekkisäde (vaihe 2+), sama kuin Kurittajalla mutta useammin
        if (ph >= 1)
        {
            if (bossBeamWarn > 0f)
            {
                bossBeamWarn -= dt;
                telegraph.sr.enabled = true;
                float len = b.p.x - bossMuz + halfW + 1f;
                telegraph.p = new Vector2(-halfW - 1f + len * 0.5f, bossBeamY); telegraph.tr.localPosition = new Vector3(telegraph.p.x, telegraph.p.y, 0f);
                telegraph.tr.localScale = new Vector3(len, 0.08f + 0.5f * (1f - bossBeamWarn / 1.0f), 1f);
                telegraph.sr.color = new Color(0.2f, 1f, 0.45f, 0.25f + 0.3f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 14f)));
                if (bossBeamWarn <= 0f) { telegraph.sr.enabled = false; bossBeamT = 1.2f; Play(Sfx.Big, 0.5f, 1.5f); loopSrc.clip = Sfx.Beam; loopSrc.loop = true; loopSrc.Play(); }
            }
            else if (bossBeamT > 0f)
            {
                bossBeamT -= dt; ebeam.sr.enabled = true; ebeam.life += dt; Anim(ebeam);
                float x0 = b.p.x - bossMuz, len = x0 + halfW + 1f;
                ebeam.p = new Vector2(x0, bossBeamY); ebeam.tr.localPosition = new Vector3(x0, bossBeamY, 0f);
                ebeam.tr.localScale = new Vector3(len / ebeam.sr.sprite.bounds.size.x, 1f, 1f);
                shake = Mathf.Max(shake, 0.12f);
                if (bossBeamT <= 0f) { ebeam.sr.enabled = false; loopSrc.Stop(); bossBeamCd = ph == 1 ? 6f : 4.5f; }
            }
            else
            {
                bossBeamCd -= dt;
                if (bossBeamCd <= 0f) { bossBeamWarn = 1.0f; bossBeamY = Mathf.Clamp(hp.y, -3.6f, 3.6f); Play(Sfx.Ping, 0.6f, 0.4f); }
            }
        }
        // rynnäkön ajastus (vaihe 3)
        if (ph >= 2 && !beamBusy)
        {
            b.timer2 -= dt;
            if (b.timer2 <= 0f) { b.state = 1; b.timer = 1.1f; Play(Sfx.Ping, 0.7f, 0.35f); }
        }
    }

    void UpdateWinEffects(float dt) { /* räjähdykset jatkuvat UpdateFx:ssä */ }

    // ---------------------------------------------------------------- loppuruudut
    void GameOver()
    {
        st = St.Over; stT = 0f; Hi();
        loopSrc.Stop();
    }

    void Win()
    {
        score += lives * 2000 + (shieldHits > 0 ? 1000 : 0);
        st = St.Win; stT = 0f; Hi();
    }

    void Hi()
    {
        if (score > hiScore) { hiScore = score; PlayerPrefs.SetInt(HiKey, hiScore); PlayerPrefs.Save(); }
    }

    // ---------------------------------------------------------------- käyttöliittymä
    void OnGUI()
    {
        float H = Screen.height, W = Screen.width, s = H / 1080f;
        if (gs == null || Mathf.Abs(gs.fontSize - Mathf.RoundToInt(34 * s)) > 1)
        {
            gs = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * s), fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            gs.normal.textColor = Color.white;
            mid = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(44 * s) };
            big = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(110 * s) };
        }
        var white = Texture2D.whiteTexture;
        if (st == St.Title)
        {
            var t = Art.Tex("title");
            if (t != null)
            {
                float ia = (float)t.width / t.height, sw = W, sh = W / ia;
                if (sh < H) { sh = H; sw = H * ia; }
                GUI.DrawTexture(new Rect((W - sw) * 0.5f, (H - sh) * 0.5f, sw, sh), t, ScaleMode.StretchToFill);
            }
            GUI.color = new Color(0, 0, 0, 0.45f); GUI.DrawTexture(new Rect(0, H * 0.78f, W, H * 0.22f), white); GUI.color = Color.white;
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
            GUI.Label(new Rect(0, H * 0.79f, W, H * 0.08f), "[J]  " + Loc.T("ALOITA PELI"), mid);
            mid.normal.textColor = Color.white;
            string hint = Loc.T("Liiku: nuolet / WASD     Ammu: J (pohjassa)     Erikoisase: L     Poistu: Esc / Start");
            gs.alignment = TextAnchor.MiddleCenter; GUI.Label(new Rect(0, H * 0.88f, W, H * 0.05f), hint, gs);
            GUI.Label(new Rect(0, H * 0.93f, W, H * 0.05f), Loc.T("Paras tulos") + ": " + hiScore.ToString("D7"), gs);
            gs.alignment = TextAnchor.UpperLeft;
            return;
        }
        // välähdykset
        if (whiteFlash > 0f) { GUI.color = new Color(1, 1, 1, Mathf.Clamp01(whiteFlash)); GUI.DrawTexture(new Rect(0, 0, W, H), white); GUI.color = Color.white; }
        if (warned && !bossSpawned)
        {
            float a = 0.12f + 0.12f * Mathf.Sin(Time.unscaledTime * 9f);
            GUI.color = new Color(1f, 0f, 0f, a); GUI.DrawTexture(new Rect(0, 0, W, H), white); GUI.color = Color.white;
            if (warnEnt != null)
            {
                big.normal.textColor = new Color(1f, 0.9f, 0.3f, 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 9f));
                Vector3 sp = cam.WorldToScreenPoint(root.position + new Vector3(0f, 1.5f, 0f));
                big.fontSize = Mathf.RoundToInt(64 * s);
                GUI.Label(new Rect(0, H - sp.y - H * 0.06f, W, H * 0.12f), Loc.T("VAROITUS"), big);
                big.fontSize = Mathf.RoundToInt(110 * s); big.normal.textColor = Color.white;
            }
        }
        float m = 36 * s;
        // pisteet ja kombo
        gs.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect(m, m * 0.6f, W * 0.4f, 50 * s), Loc.T("PISTEET") + "  " + score.ToString("D7"), gs);
        gs.fontSize = Mathf.RoundToInt(26 * s);
        GUI.Label(new Rect(m, m * 0.6f + 44 * s, W * 0.4f, 40 * s), Loc.T("PARAS") + "  " + Mathf.Max(hiScore, score).ToString("D7"), gs);
        gs.fontSize = Mathf.RoundToInt(34 * s);
        if (combo >= 3)
        {
            int mul = Mathf.Min(4, 1 + combo / 6);
            gs.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(m, m * 0.6f + 84 * s, W * 0.4f, 40 * s), "KOMBO " + combo + (mul > 1 ? "   x" + mul : ""), gs);
            gs.normal.textColor = Color.white;
        }
        // elämät ja kilpi
        var ht = Art.Tex("hero_0");
        float iw = 84 * s, ih = ht != null ? iw * ht.height / ht.width : iw * 0.4f;
        for (int i = 0; i < lives; i++) if (ht != null) GUI.DrawTexture(new Rect(W - m - (i + 1) * (iw + 10 * s), m * 0.6f, iw, ih), ht);
        string wp = weapon + "/4";
        gs.alignment = TextAnchor.UpperRight;
        GUI.Label(new Rect(W - m - 400 * s, m * 0.6f + ih + 6 * s, 400 * s, 40 * s), Loc.T("ASE") + " " + wp + (shieldHits > 0 ? "   " + Loc.T("KILPI") + " " + shieldHits : ""), gs);
        gs.alignment = TextAnchor.UpperLeft;
        // erikoismittari
        float bw = 420 * s, bh = 26 * s, bx = m, by = H - m - bh;
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(bx - 4, by - 4, bw + 8, bh + 8), white);
        bool full = charge >= 100f;
        GUI.color = full ? Color.Lerp(new Color(0.3f, 0.8f, 1f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)) : new Color(0.2f, 0.55f, 0.95f);
        GUI.DrawTexture(new Rect(bx, by, bw * Mathf.Clamp01(charge / 100f), bh), white);
        GUI.color = Color.white;
        gs.fontSize = Mathf.RoundToInt(24 * s);
        GUI.Label(new Rect(bx, by - 34 * s, bw, 30 * s), full ? "[L]  " + Loc.T("ERIKOISASE VALMIS") : Loc.T("ERIKOISASE"), gs);
        gs.fontSize = Mathf.RoundToInt(34 * s);
        // pomon elämäpalkki
        if (boss != null)
        {
            float pw = W * 0.5f, ph = 24 * s, px = (W - pw) * 0.5f, py = H - m - ph;
            GUI.color = new Color(0, 0, 0, 0.65f); GUI.DrawTexture(new Rect(px - 5, py - 5, pw + 10, ph + 10), white);
            GUI.color = new Color(0.85f, 0.15f, 0.1f); GUI.DrawTexture(new Rect(px, py, pw * Mathf.Clamp01(boss.hp / boss.maxHp), ph), white);
            GUI.color = Color.white;
            mid.fontSize = Mathf.RoundToInt(28 * s);
            GUI.Label(new Rect(0, py - 40 * s, W, 36 * s), Loc.T(level == 2 ? "TUHOAJA" : "KURITTAJA"), mid);
            mid.fontSize = Mathf.RoundToInt(44 * s);
        }
        // kentän vaihtuminen
        if (st == St.Play && bannerT > 0f)
        {
            float al = Mathf.Clamp01(Mathf.Min(bannerT, 4.2f - bannerT) * 2f);
            big.normal.textColor = new Color(0.6f, 0.95f, 1f, al);
            GUI.Label(new Rect(0, H * 0.3f, W, H * 0.16f), Loc.T("KENTTÄ") + " 2", big);
            big.normal.textColor = Color.white;
            mid.normal.textColor = new Color(1f, 1f, 1f, al);
            GUI.Label(new Rect(0, H * 0.47f, W, H * 0.08f), Loc.T("Vihollinen iskee uusin asein"), mid);
            mid.normal.textColor = Color.white;
        }
        // ohje alussa
        if (st == St.Play && level == 1 && clock < 5f)
        {
            gs.alignment = TextAnchor.MiddleCenter; gs.normal.textColor = new Color(1, 1, 1, Mathf.Clamp01(5f - clock));
            GUI.Label(new Rect(0, H * 0.4f, W, 60 * s), Loc.T("VALMIINA!"), mid);
            gs.normal.textColor = Color.white; gs.alignment = TextAnchor.UpperLeft;
        }
        if (st == St.Over || st == St.Win)
        {
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(new Rect(0, H * 0.28f, W, H * 0.42f), white); GUI.color = Color.white;
            big.normal.textColor = st == St.Win ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.4f, 0.35f);
            GUI.Label(new Rect(0, H * 0.3f, W, H * 0.16f), st == St.Win ? Loc.T("VOITTO!") : Loc.T("PELI OHI"), big);
            big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, H * 0.47f, W, H * 0.08f), Loc.T("Pisteet") + ": " + score.ToString("D7") + (score >= hiScore && score > 0 ? "   " + Loc.T("UUSI ENNÄTYS!") : ""), mid);
            if (stT > 1.2f) GUI.Label(new Rect(0, H * 0.58f, W, H * 0.07f), "[J] " + Loc.T("Uudestaan") + "     [Esc/Start/K] " + Loc.T("Poistu"), mid);
        }
    }
}

