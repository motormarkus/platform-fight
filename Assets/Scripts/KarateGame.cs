using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turpaan Vaan: pikselikarate pelihallin koneessa. 1v1-turnaus: valitaan Rocco tai Gustav, vastustajia kuusi (kolmen kierroksen otteluita,
/// kaksi voittoa riittää). Kuvat ovat pelin omia liikekuvia pikseloituna (Tools/KarateAssets/make_karate.py -> Resources/Karate).
/// Rakenne kuten Tähtipyssyssä: oma kamera ja maailma kaukana (y 6000), maailma jäätyy (timeScale 0) ja kaikki pyörii skaalaamattomalla ajalla.
/// Ohjaus: liike nuolet/WASD/tatti, isku J, potku K, erikoispotku L (latautuu), torjunta I (pohjassa), Esc/Q poistuu.
/// </summary>
public class KarateGame : MonoBehaviour
{
    public static KarateGame Instance;
    public static bool Active => Instance != null;
    const string HiKey = "karate_hi";

    public static void Begin(PlayerController who)
    {
        if (Instance != null || who == null) return;
        var go = new GameObject("Turpaan Vaan");
        go.AddComponent<KarateGame>().Init(who);
    }

    // ---------------------------------------------------------------- kuvat
    static readonly Dictionary<string, Sprite[]> sprCache = new Dictionary<string, Sprite[]>();
    static Sprite whiteSp;
    static Sprite White()
    {
        if (whiteSp == null) whiteSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSp;
    }

    /// Hahmon liike: Resources/Karate/<hahmo>_<liike>_NN, jalkojen kohta (pivot) on kuvan alareunassa.
    static Sprite[] Load(string key, string anim)
    {
        string ck = key + "_" + anim;
        if (sprCache.TryGetValue(ck, out var cached) && (cached == null || cached.Length == 0 || cached[0] != null)) return cached;
        var l = new List<Sprite>();
        for (int i = 0; i < 80; i++)
        {
            var t = Resources.Load<Texture2D>("Karate/" + ck + "_" + i.ToString("D2"));
            if (t == null) break;
            t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            l.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 2f / t.height), 1f, 0, SpriteMeshType.FullRect));
        }
        var arr = l.Count > 0 ? l.ToArray() : null;
        sprCache[ck] = arr;
        return arr;
    }

    static Sprite CircleSprite(int r, Color c)
    {
        int d = r * 2 + 1;
        var t = new Texture2D(d, d, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < d; y++)
            for (int x = 0; x < d; x++)
                t.SetPixel(x, y, (x - r) * (x - r) + (y - r) * (y - r) <= r * r ? c : Color.clear);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, d, d), new Vector2(0.5f, 0.5f), 1f);
    }

    // ---------------------------------------------------------------- äänet (syntetisoitu)
    static class Sfx
    {
        public static AudioClip Punch, Kick, Block, Whoosh, Ko, Bell, Win, Blip, Lose;
        const int SR = 44100;
        static System.Random rnd = new System.Random(11);
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

        public static void Build()
        {
            if (Punch != null) return;
            float lp = 0f, ph = 0f;
            Punch = Make("kg_punch", 0.16f, (t, u) => { lp += (Noise() - lp) * 0.35f; ph += 2f * Mathf.PI * Mathf.Lerp(190f, 80f, u) / SR; return (lp * 0.9f + Mathf.Sin(ph) * 0.7f) * Mathf.Exp(-t * 26f) * 0.9f; });
            float lp2 = 0f, ph2 = 0f;
            Kick = Make("kg_kick", 0.24f, (t, u) => { lp2 += (Noise() - lp2) * 0.22f; ph2 += 2f * Mathf.PI * Mathf.Lerp(150f, 55f, u) / SR; return (lp2 * 1.1f + Mathf.Sin(ph2) * 0.9f) * Mathf.Exp(-t * 16f); });
            float ph3 = 0f;
            Block = Make("kg_block", 0.09f, (t, u) => { ph3 += 2f * Mathf.PI * 520f / SR; return (Mathf.Sin(ph3) > 0 ? 1f : -1f) * 0.2f * (1f - u) + Noise() * 0.12f * (1f - u); });
            float lp4 = 0f;
            Whoosh = Make("kg_whoosh", 0.22f, (t, u) => { lp4 += (Noise() - lp4) * Mathf.Lerp(0.05f, 0.5f, Mathf.Sin(u * Mathf.PI)); return lp4 * 0.9f * Mathf.Sin(u * Mathf.PI); });
            float lp5 = 0f, ph5 = 0f;
            Ko = Make("kg_ko", 0.9f, (t, u) => { lp5 += (Noise() - lp5) * 0.1f; ph5 += 2f * Mathf.PI * Mathf.Lerp(120f, 35f, u) / SR; return (lp5 * 1.6f + Mathf.Sin(ph5) * 0.8f) * Mathf.Exp(-t * 3.5f); });
            float ph6 = 0f;
            Bell = Make("kg_bell", 0.9f, (t, u) => { ph6 += 2f * Mathf.PI * 880f / SR; return (Mathf.Sin(ph6) + 0.5f * Mathf.Sin(ph6 * 2.7f)) * 0.22f * Mathf.Exp(-t * 4.5f); });
            float ph7 = 0f;
            Win = Make("kg_win", 1.1f, (t, u) => { float f = t < 0.18f ? 523f : t < 0.36f ? 659f : t < 0.54f ? 784f : 1046f; ph7 += 2f * Mathf.PI * f / SR; return (Mathf.Sin(ph7) > 0 ? 1f : -1f) * 0.16f * (1f - u * 0.7f); });
            float ph8 = 0f;
            Blip = Make("kg_blip", 0.06f, (t, u) => { ph8 += 2f * Mathf.PI * 990f / SR; return Mathf.Sin(ph8) * 0.2f * (1f - u); });
            float ph9 = 0f;
            Lose = Make("kg_lose", 1.0f, (t, u) => { float f = Mathf.Lerp(330f, 110f, u); ph9 += 2f * Mathf.PI * f / SR; return (Mathf.Sin(ph9) > 0 ? 1f : -1f) * 0.15f * (1f - u); });
        }
    }

    // ---------------------------------------------------------------- hahmot
    enum S { Idle, Walk, Punch, Kick, Special, Hurt, Fall, GetUp, Block }

    class Atk { public string anim; public float dur, hs, he, reach, dmg, stun, push; public bool kd; public AudioClip hitSfx; }
    static readonly Atk APunch = new Atk { anim = "punch", dur = 0.38f, hs = 0.35f, he = 0.62f, reach = 48f, dmg = 7f, stun = 0.30f, push = 55f };
    static readonly Atk AKick = new Atk { anim = "kick", dur = 0.58f, hs = 0.36f, he = 0.64f, reach = 60f, dmg = 11f, stun = 0.42f, push = 85f };
    static readonly Atk ASpecial = new Atk { anim = "special", dur = 0.85f, hs = 0.38f, he = 0.72f, reach = 68f, dmg = 18f, stun = 0.5f, push = 170f, kd = true };
    const float FallDur = 0.85f, GetUpDur = 0.7f;

    class Fg
    {
        public string key, name; public bool player;
        public Transform tr; public SpriteRenderer sr, shadow;
        public float x, vx, hp = 100f, shownHp = 100f;
        public int face = 1;
        public S s; public float t, stunT;
        public Atk atk; public bool hit; public int atkId;
        public bool stayDown; public int comboN; public float comboAt;
        public float specialCd, spMax = 3f;
        public float dmgMul = 1f, speed = 95f;
        // tekoäly
        public float thinkT, moveT, blockT, thinkMin = 0.8f, thinkMax = 1.3f, blockChance, specialChance;
        public int moveDir, seenAtk; public bool moveAway;
        public bool IsAtk => s == S.Punch || s == S.Kick || s == S.Special;
        public bool Grounded => s != S.Fall && s != S.GetUp;
    }

    struct Opp { public string key, name; public float speed, dmg, tMin, tMax, block, special; }
    static Opp O(string key, string name, float speed, float dmg, float tMin, float tMax, float block, float special) { return new Opp { key = key, name = name, speed = speed, dmg = dmg, tMin = tMin, tMax = tMax, block = block, special = special }; }
    static readonly Opp[] Table =
    {
        O("lippis",   "LIPPIS",   70f, 0.75f, 0.95f, 1.5f, 0.05f, 0.05f),
        O("lenkkari", "LENKKARI", 80f, 0.85f, 0.85f, 1.35f, 0.12f, 0.08f),
        O("horhe",    "HORHE",    85f, 0.95f, 0.75f, 1.2f, 0.18f, 0.12f),
        O("marcus",   "MARCUS",   95f, 1.0f, 0.6f, 1.0f, 0.26f, 0.12f),
        O("",         "",        100f, 1.1f, 0.5f, 0.85f, 0.32f, 0.2f),   // toinen sankareista (täytetään valinnan mukaan)
        O("deacon",   "DEACON",  105f, 1.2f, 0.4f, 0.75f, 0.42f, 0.22f),
    };

    class Pt { public Transform tr; public SpriteRenderer sr; public Vector2 v; public float life, max; public Color c; }

    // ---------------------------------------------------------------- tila
    enum St { Select, Intro, Fight, KO, Over, Win }
    St st = St.Select;
    float stT, openedAt;
    PlayerController pc; int pi;
    Transform root; Camera cam; AudioSource sfx;
    bool prevListenerPause; float prevTimeScale = 1f;
    float halfW;
    const float HalfH = 90f;
    const float GroundY = -62f, ArenaX = 168f;
    Fg P, E;
    readonly List<Pt> pts = new List<Pt>();
    readonly List<GameObject> bgObjs = new List<GameObject>();
    int sel;                   // 0 = Rocco, 1 = Gustav
    int bout, round, winsP, winsE;
    float roundT; string banner, bannerSub;
    int score, hiScore, atkCounter;
    float hitStop, shake, flashT, lastMx, koFlash;
    bool playerWonRound, timeOut;
    Fg[] selFg = new Fg[2];
    GUIStyle gs, big, mid;
    float lastSelDir;

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
        sfx = gameObject.AddComponent<AudioSource>(); sfx.spatialBlend = 0f; sfx.playOnAwake = false; sfx.ignoreListenerPause = true;
        // muu maailma vaikenee ja jäätyy peliajaksi (kuten Tähtipyssyssä)
        prevListenerPause = AudioListener.pause; AudioListener.pause = true;
        prevTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f; Time.timeScale = 0f;
        Sfx.Build();
        hiScore = PlayerPrefs.GetInt(HiKey, 0);
        BuildDojo();
        BuildSelect();
        st = St.Select; stT = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; AudioListener.pause = prevListenerPause; Time.timeScale = prevTimeScale; }
        foreach (var p in Players.All) if (p != null) p.enabled = true;
    }

    void End() { Destroy(gameObject); }

    // ---------------------------------------------------------------- apuja
    SpriteRenderer Quad(float x, float y, float w, float h, Color c, int order, string name = "r")
    {
        var go = new GameObject(name); go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, y, 0f); go.transform.localScale = new Vector3(w, h, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White(); sr.color = c; sr.sortingOrder = order;
        bgObjs.Add(go);
        return sr;
    }

    void Play(AudioClip c, float vol = 1f, float pitch = 1f)
    {
        if (c == null) return;
        sfx.pitch = pitch; sfx.PlayOneShot(c, vol);
    }

    /// Dojo: pimeä puuseinä, paperiovet, kuu ikkunassa, lyhdyt ja lankkulattia.
    void BuildDojo()
    {
        float W = 520f;
        Quad(0, 0, W * 2f, HalfH * 2f, new Color(0.13f, 0.10f, 0.19f), -100, "seinä");
        // pystylankut
        for (float x = -W; x < W; x += 24f)
            Quad(x, 14f, 2f, 150f, new Color(0.09f, 0.07f, 0.14f), -99, "lankku");
        // iso ikkuna ja kuu
        Quad(0, 28f, 170f, 78f, new Color(0.07f, 0.06f, 0.11f), -98, "ikkunakehys");
        Quad(0, 28f, 162f, 70f, new Color(0.17f, 0.22f, 0.42f), -97, "yötaivas");
        var moon = new GameObject("kuu"); moon.transform.SetParent(root, false); moon.transform.localPosition = new Vector3(34f, 38f, 0f);
        var ms = moon.AddComponent<SpriteRenderer>(); ms.sprite = CircleSprite(12, new Color(1f, 0.93f, 0.62f)); ms.sortingOrder = -96; bgObjs.Add(moon);
        for (int i = 0; i < 14; i++) Quad(-75f + (i * 37 % 150), 8f + (i * 53 % 54), 1.5f, 1.5f, new Color(0.9f, 0.9f, 1f, 0.8f), -96, "tähti");
        // vuoren siluetti
        Quad(-30f, 4f, 56f, 8f, new Color(0.09f, 0.1f, 0.2f), -95); Quad(-30f, 10f, 34f, 6f, new Color(0.09f, 0.1f, 0.2f), -95); Quad(-30f, 15f, 14f, 6f, new Color(0.09f, 0.1f, 0.2f), -95);
        // ikkunan ristikko
        Quad(0, 28f, 3f, 70f, new Color(0.07f, 0.06f, 0.11f), -94, "ristikko"); Quad(0, 28f, 162f, 3f, new Color(0.07f, 0.06f, 0.11f), -94, "ristikko");
        // sivujen paperiovet
        for (int side = -1; side <= 1; side += 2)
            for (int k = 0; k < 2; k++)
            {
                float cx = side * (150f + k * 54f);
                Quad(cx, 22f, 50f, 100f, new Color(0.78f, 0.72f, 0.58f), -98, "ovi");
                Quad(cx, 22f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi"); Quad(cx, -10f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi");
                Quad(cx, 22f, 2f, 100f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi"); Quad(cx, 58f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi");
            }
        // lyhdyt
        for (int side = -1; side <= 1; side += 2)
        {
            float lx = side * 100f;
            Quad(lx, 74f, 1f, 22f, new Color(0.05f, 0.05f, 0.07f), -93, "naru");
            Quad(lx, 58f, 15f, 20f, new Color(0.78f, 0.15f, 0.12f), -92, "lyhty");
            Quad(lx, 58f, 9f, 14f, new Color(1f, 0.78f, 0.3f, 0.9f), -91, "lyhty");
            Quad(lx, 68f, 17f, 2f, new Color(0.15f, 0.1f, 0.08f), -91, "lyhty"); Quad(lx, 48f, 17f, 2f, new Color(0.15f, 0.1f, 0.08f), -91, "lyhty");
        }
        // lattia
        float floorTop = -34f;
        Quad(0, (floorTop - HalfH) * 0.5f, W * 2f, floorTop + HalfH, new Color(0.50f, 0.32f, 0.18f), -90, "lattia");
        Quad(0, floorTop, W * 2f, 3f, new Color(0.28f, 0.17f, 0.10f), -89, "lattiaraja");
        for (float y = floorTop - 9f; y > -HalfH; y -= 10f)
            Quad(0, y, W * 2f, 1.5f, new Color(0.38f, 0.23f, 0.13f), -89, "sauma");
        int row = 0;
        for (float y = floorTop - 4f; y > -HalfH; y -= 10f, row++)
            for (float x = -W + (row % 2) * 40f; x < W; x += 80f)
                Quad(x, y, 1.5f, 8.5f, new Color(0.38f, 0.23f, 0.13f), -89, "sauma");
        Quad(0, GroundY - 4f, 2f * ArenaX + 20f, 1f, new Color(0.8f, 0.6f, 0.25f, 0.35f), -88, "matto");
    }

    // ---------------------------------------------------------------- hahmot ja kuvat
    Fg MakeFighter(string key, string name, bool player, float x, int face)
    {
        var f = new Fg { key = key, name = name, player = player, x = x, face = face };
        var go = new GameObject(name); go.transform.SetParent(root, false); f.tr = go.transform;
        f.sr = go.AddComponent<SpriteRenderer>(); f.sr.sortingOrder = player ? 12 : 11;
        var sh = new GameObject("varjo"); sh.transform.SetParent(go.transform, false);
        sh.transform.localPosition = new Vector3(0f, 1f, 0f); sh.transform.localScale = new Vector3(34f, 7f, 1f);
        f.shadow = sh.AddComponent<SpriteRenderer>(); f.shadow.sprite = CircleSprite(8, Color.white); f.shadow.color = new Color(0f, 0f, 0f, 0.4f); f.shadow.sortingOrder = 5;
        sh.transform.localScale = new Vector3(2.2f, 0.45f, 1f);
        Show(f);
        return f;
    }

    void DestroyFighter(ref Fg f) { if (f != null && f.tr != null) Destroy(f.tr.gameObject); f = null; }

    Sprite Frame(Fg f, string anim, float t, float cycle, bool loop, bool reverse = false)
    {
        var arr = Load(f.key, anim) ?? Load(f.key, "idle");
        if (arr == null) return null;
        int n = arr.Length, i;
        float u = cycle <= 0f ? 0f : t / cycle;
        if (loop) { u -= Mathf.Floor(u); i = Mathf.Min(n - 1, (int)(u * n)); }
        else i = Mathf.Min(n - 1, (int)(Mathf.Clamp01(u) * n));
        if (reverse) i = n - 1 - i;
        return arr[i];
    }

    void Show(Fg f)
    {
        Sprite sp;
        f.sr.color = Color.white;
        switch (f.s)
        {
            case S.Punch: case S.Kick: case S.Special: sp = Frame(f, f.atk.anim, f.t, f.atk.dur, false); break;
            case S.Hurt: sp = Frame(f, "hurt", f.t, Mathf.Max(0.2f, f.stunT), false); break;
            case S.Fall: sp = Frame(f, "fall", f.t, FallDur, false); break;
            case S.GetUp: sp = Frame(f, "getup", f.t, GetUpDur, false); break;
            case S.Block:
                if (Load(f.key, "block") != null) sp = Frame(f, "block", Mathf.Min(f.t, 0.15f), 0.15f, false);
                else { sp = Frame(f, "idle", 0f, 1f, false); f.sr.color = new Color(0.75f, 0.85f, 1f); }
                break;
            case S.Walk: { var a = Load(f.key, "walk"); sp = Frame(f, "walk", Time.unscaledTime, Mathf.Clamp((a != null ? a.Length : 8) * 0.07f, 0.5f, 0.9f), true, f.moveAway); break; }
            default: { var a = Load(f.key, "idle"); sp = Frame(f, "idle", Time.unscaledTime, Mathf.Clamp((a != null ? a.Length : 6) * 0.1f, 0.6f, 1.6f), true); break; }
        }
        f.sr.sprite = sp;
        f.sr.flipX = f.face < 0;
        float lift = 0f;
        f.tr.localPosition = new Vector3(f.x, GroundY + lift, 0f);
        f.shadow.enabled = f.s != S.Fall || f.t < FallDur;
    }

    // ---------------------------------------------------------------- valintaruutu
    void BuildSelect()
    {
        string[] keys = { "rocco", "gustav" };
        for (int i = 0; i < 2; i++)
        {
            selFg[i] = MakeFighter(keys[i], keys[i] == "rocco" ? "ROCCO" : "GUSTAV", i == 0, i == 0 ? -62f : 62f, 1);
        }
        sel = 0;
    }

    void ClearSelect()
    {
        for (int i = 0; i < 2; i++) if (selFg[i] != null && selFg[i].tr != null) Destroy(selFg[i].tr.gameObject);
        selFg[0] = selFg[1] = null;
    }

    void UpdateSelect(float dt)
    {
        float mx = CoopInput.Move(pi).x;
        int dir = mx > 0.5f ? 1 : mx < -0.5f ? -1 : 0;
        if (dir != 0 && lastSelDir == 0) { sel = dir > 0 ? 1 : 0; Play(Sfx.Blip, 0.8f); }
        lastSelDir = dir;
        for (int i = 0; i < 2; i++)
        {
            var f = selFg[i]; if (f == null) continue;
            bool on = i == sel;
            f.s = S.Idle; f.face = i == 0 ? 1 : -1;
            Show(f);
            f.sr.color = on ? Color.white : new Color(0.45f, 0.45f, 0.5f);
            f.tr.localScale = Vector3.one * (on ? 1.15f : 0.95f);
            f.sr.sortingOrder = on ? 13 : 11;
        }
        if (stT > 0.3f && (CoopInput.Punch(pi) || CoopInput.Pick(pi) || CoopInput.Special(pi)))
        {
            Play(Sfx.Bell, 0.7f);
            string k = sel == 0 ? "rocco" : "gustav";
            ClearSelect();
            StartTournament(k);
        }
    }

    // ---------------------------------------------------------------- turnaus
    string heroKey;

    void StartTournament(string key)
    {
        heroKey = key;
        bout = 0; score = 0;
        SetupBout();
    }

    Opp BoutOpp()
    {
        var o = Table[bout];
        if (string.IsNullOrEmpty(o.key)) { o.key = heroKey == "rocco" ? "gustav" : "rocco"; o.name = heroKey == "rocco" ? "GUSTAV" : "ROCCO"; }
        return o;
    }

    void SetupBout()
    {
        DestroyFighter(ref P); DestroyFighter(ref E);
        var o = BoutOpp();
        P = MakeFighter(heroKey, heroKey == "rocco" ? "ROCCO" : "GUSTAV", true, -70f, 1);
        E = MakeFighter(o.key, o.name, false, 70f, -1);
        E.speed = o.speed; E.dmgMul = o.dmg; E.thinkMin = o.tMin; E.thinkMax = o.tMax; E.blockChance = o.block; E.specialChance = o.special;
        winsP = winsE = 0; round = 1;
        StartRound();
    }

    void StartRound()
    {
        foreach (var f in new[] { P, E })
        {
            f.hp = f.shownHp = 100f; f.s = S.Idle; f.t = 0f; f.vx = 0f; f.stayDown = false; f.comboN = 0; f.specialCd = 0f;
            f.thinkT = Random.Range(0.6f, 1.2f); f.moveT = 0f; f.blockT = 0f;
        }
        P.x = -70f; E.x = 70f; P.face = 1; E.face = -1;
        P.specialCd = 1.5f;
        roundT = 60f;
        st = St.Intro; stT = 0f;
        banner = Loc.T("KIERROS") + " " + round;
        bannerSub = "VS  " + E.name + "   ( " + (bout + 1) + " / " + Table.Length + " )";
        Play(Sfx.Bell, 0.6f);
        Show(P); Show(E);
    }

    // ---------------------------------------------------------------- päivitys
    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        stT += dt;
        cam.transform.position = new Vector3(0f, 6000f, -10f) + (shake > 0.001f ? (Vector3)(Random.insideUnitCircle * shake) : Vector3.zero);
        shake = Mathf.Max(0f, shake - dt * 30f);
        flashT = Mathf.Max(0f, flashT - dt * 3f);
        UpdateParticles(dt);

        var k = UnityEngine.InputSystem.Keyboard.current;
        bool esc = k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame);
        if (Time.unscaledTime - openedAt < 0.35f) return;
        switch (st)
        {
            case St.Select:
                if (esc || CoopInput.Kick(pi)) { End(); return; }
                UpdateSelect(dt);
                return;
            case St.Over:
            case St.Win:
                P.s = st == St.Win ? S.Idle : P.s; Show(P); Show(E);
                if (esc || CoopInput.Kick(pi)) { End(); return; }
                if (stT > 1.6f && (CoopInput.Punch(pi) || CoopInput.Pick(pi))) { StartTournament(heroKey); }
                return;
        }
        if (esc) { End(); return; }

        if (st == St.Intro)
        {
            Show(P); Show(E);
            if (stT > 1.9f) { st = St.Fight; stT = 0f; Play(Sfx.Bell, 0.9f, 1.4f); }
            return;
        }
        if (hitStop > 0f) { hitStop -= dt; dt = 0f; }
        if (st == St.Fight)
        {
            roundT -= dt;
            UpdatePlayer(dt);
            UpdateAI(dt);
        }
        UpdateFighter(P, E, dt); UpdateFighter(E, P, dt);
        Separate();
        Show(P); Show(E);
        P.shownHp = Mathf.MoveTowards(P.shownHp, P.hp, 40f * Time.unscaledDeltaTime);
        E.shownHp = Mathf.MoveTowards(E.shownHp, E.hp, 40f * Time.unscaledDeltaTime);

        if (st == St.Fight)
        {
            bool timeUp = roundT <= 0f;
            if (P.hp <= 0f || E.hp <= 0f || timeUp)
            {
                timeOut = timeUp && P.hp > 0f && E.hp > 0f;
                playerWonRound = P.hp > E.hp;
                if (timeOut && Mathf.Approximately(P.hp, E.hp)) playerWonRound = false;
                st = St.KO; stT = 0f;
                Play(Sfx.Ko, 0.9f); shake = 3f; flashT = 1f;
                if (playerWonRound)
                {
                    winsP++;
                    score += 500 + Mathf.RoundToInt(P.hp) * 5 + Mathf.CeilToInt(Mathf.Max(0f, roundT)) * 10;
                }
                else winsE++;
                if (timeOut) { var loser = playerWonRound ? E : P; loser.hp = Mathf.Max(loser.hp, 0.01f); }
            }
        }
        else if (st == St.KO && stT > 2.6f)
        {
            if (winsP >= 2)
            {
                score += 1000 * (bout + 1);
                bout++;
                if (bout >= Table.Length) { st = St.Win; stT = 0f; Play(Sfx.Win, 0.9f); Hi(); }
                else SetupBout();
            }
            else if (winsE >= 2) { st = St.Over; stT = 0f; Play(Sfx.Lose, 0.9f); Hi(); }
            else { round++; StartRound(); }
        }
    }

    void Hi()
    {
        if (score > hiScore) { hiScore = score; PlayerPrefs.SetInt(HiKey, hiScore); PlayerPrefs.Save(); }
    }

    // ---------------------------------------------------------------- pelaaja
    bool Free(Fg f) => f.s == S.Idle || f.s == S.Walk || f.s == S.Block;

    void StartAtk(Fg f, Atk a)
    {
        f.atk = a; f.s = a == APunch ? S.Punch : a == AKick ? S.Kick : S.Special;
        f.t = 0f; f.hit = false; f.atkId = ++atkCounter;
        Play(Sfx.Whoosh, 0.5f, a == ASpecial ? 0.75f : a == AKick ? 0.9f : 1.15f);
    }

    void UpdatePlayer(float dt)
    {
        P.specialCd = Mathf.Max(0f, P.specialCd - dt);
        if (!Free(P)) return;
        Vector2 m = CoopInput.Move(pi);
        float mx = Mathf.Abs(m.x) > 0.3f ? Mathf.Sign(m.x) : 0f;
        P.face = E.x >= P.x ? 1 : -1;
        if (CoopInput.Block(pi))
        {
            if (P.s != S.Block) { P.s = S.Block; P.t = 0f; }
            return;
        }
        if (P.s == S.Block) { P.s = S.Idle; P.t = 0f; }
        if (CoopInput.Special(pi) && P.specialCd <= 0f) { StartAtk(P, ASpecial); P.specialCd = P.spMax; return; }
        if (CoopInput.Kick(pi)) { StartAtk(P, AKick); return; }
        if (CoopInput.Punch(pi)) { StartAtk(P, APunch); return; }
        P.moveAway = mx != 0f && Mathf.Sign(mx) != P.face;
        if (mx != 0f) { P.x += mx * P.speed * dt; P.s = S.Walk; } else P.s = S.Idle;
    }

    // ---------------------------------------------------------------- tekoäly
    void UpdateAI(float dt)
    {
        var e = E;
        e.specialCd = Mathf.Max(0f, e.specialCd - dt);
        if (!Free(e)) return;
        float d = Mathf.Abs(P.x - e.x);
        e.face = P.x >= e.x ? 1 : -1;
        if (P.IsAtk && P.atkId != e.seenAtk)
        {
            e.seenAtk = P.atkId;
            if (d < P.atk.reach + 22f && Random.value < e.blockChance) { e.s = S.Block; e.t = 0f; e.blockT = Random.Range(0.35f, 0.7f); }
        }
        if (e.s == S.Block)
        {
            e.blockT -= dt;
            if (e.blockT <= 0f) { e.s = S.Idle; e.t = 0f; }
            return;
        }
        e.thinkT -= dt;
        if (e.moveT > 0f)
        {
            e.moveT -= dt;
            if (e.moveDir == e.face && d < 42f) e.moveT = 0f;
            e.x += e.moveDir * e.speed * dt;
            e.moveAway = e.moveDir != e.face;
            e.s = e.moveT > 0f ? S.Walk : S.Idle;
            return;
        }
        e.s = S.Idle;
        if (e.thinkT > 0f) return;
        // päätös
        float r = Random.value;
        Atk pick = (e.specialCd <= 0f && r < e.specialChance) ? ASpecial : (r < 0.5f ? AKick : APunch);
        if (d > pick.reach - 4f) { e.moveDir = e.face; e.moveT = Random.Range(0.25f, 0.6f); e.thinkT = 0.05f; return; }
        if (Random.value < 0.14f) { e.moveDir = -e.face; e.moveT = Random.Range(0.3f, 0.6f); e.thinkT = Random.Range(0.1f, 0.3f); return; }
        StartAtk(e, pick);
        if (pick == ASpecial) e.specialCd = 5f;
        e.thinkT = Random.Range(e.thinkMin, e.thinkMax);
    }

    // ---------------------------------------------------------------- taistelu
    void UpdateFighter(Fg f, Fg o, float dt)
    {
        f.t += dt;
        f.x += f.vx * dt; f.vx = Mathf.MoveTowards(f.vx, 0f, 420f * dt);
        f.x = Mathf.Clamp(f.x, -ArenaX, ArenaX);
        switch (f.s)
        {
            case S.Punch: case S.Kick: case S.Special:
                float u = f.t / f.atk.dur;
                if (!f.hit && u >= f.atk.hs && u <= f.atk.he && st == St.Fight) TryHit(f, o);
                if (u >= 1f) { f.s = S.Idle; f.t = 0f; }
                break;
            case S.Hurt:
                if (f.t >= f.stunT) { f.s = S.Idle; f.t = 0f; }
                break;
            case S.Fall:
                if (f.t >= FallDur) { if (f.stayDown) f.t = FallDur; else { f.s = S.GetUp; f.t = 0f; } }
                break;
            case S.GetUp:
                if (f.t >= GetUpDur) { f.s = S.Idle; f.t = 0f; }
                break;
        }
    }

    void TryHit(Fg a, Fg o)
    {
        float dx = (o.x - a.x) * a.face;
        if (dx < -6f || dx > a.atk.reach) return;
        if (!o.Grounded) return;
        a.hit = true;
        float dmg = a.atk.dmg * (a.player ? 1f : a.dmgMul);
        Vector2 at = new Vector2((a.x + o.x) * 0.5f + a.face * 6f, GroundY + 34f);
        bool blocked = o.s == S.Block && o.face == -a.face;
        if (blocked)
        {
            o.hp = Mathf.Max(0.01f, o.hp - dmg * 0.15f);
            o.vx = a.face * a.atk.push * 0.45f;
            Play(Sfx.Block, 0.8f); Spark(at, new Color(0.6f, 0.85f, 1f), 5);
            hitStop = 0.04f;
            return;
        }
        o.hp = Mathf.Max(0f, o.hp - dmg);
        Play(a.atk == APunch ? Sfx.Punch : Sfx.Kick, 1f, Random.Range(0.92f, 1.08f));
        Spark(at, a.atk == ASpecial ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.95f, 0.5f), a.atk == ASpecial ? 12 : 7);
        o.comboN = Time.unscaledTime - o.comboAt < 1.6f ? o.comboN + 1 : 1; o.comboAt = Time.unscaledTime;
        bool kd = a.atk.kd || o.hp <= 0f || o.comboN >= 3;
        o.vx = a.face * a.atk.push * (kd ? 1.5f : 1f);
        hitStop = o.hp <= 0f ? 0.28f : a.atk == ASpecial ? 0.12f : 0.06f;
        shake = o.hp <= 0f ? 3f : a.atk == ASpecial ? 2f : 0.8f;
        if (kd) { o.s = S.Fall; o.t = 0f; o.stayDown = o.hp <= 0f; o.comboN = 0; }
        else { o.s = S.Hurt; o.t = 0f; o.stunT = a.atk.stun; }
    }

    void Separate()
    {
        if (!P.Grounded || !E.Grounded) return;
        float d = E.x - P.x, min = 24f;
        if (Mathf.Abs(d) < min)
        {
            float push = (min - Mathf.Abs(d)) * 0.5f, sgn = d >= 0f ? 1f : -1f;
            P.x = Mathf.Clamp(P.x - sgn * push, -ArenaX, ArenaX); E.x = Mathf.Clamp(E.x + sgn * push, -ArenaX, ArenaX);
        }
    }

    // ---------------------------------------------------------------- tehosteet
    void Spark(Vector2 p, Color c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("kipinä"); go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(p.x, p.y, 0f);
            float sz = Random.Range(2f, 4f); go.transform.localScale = new Vector3(sz, sz, 1f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White(); sr.color = c; sr.sortingOrder = 30;
            float ang = Random.Range(0f, Mathf.PI * 2f), sp = Random.Range(60f, 170f);
            pts.Add(new Pt { tr = go.transform, sr = sr, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp, life = 0.28f, max = 0.28f, c = c });
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = pts.Count - 1; i >= 0; i--)
        {
            var p = pts[i];
            p.life -= dt;
            if (p.life <= 0f || p.tr == null) { if (p.tr != null) Destroy(p.tr.gameObject); pts.RemoveAt(i); continue; }
            p.tr.localPosition += (Vector3)(p.v * dt); p.v *= 1f - dt * 4f;
            p.sr.color = new Color(p.c.r, p.c.g, p.c.b, p.life / p.max);
        }
    }

    // ---------------------------------------------------------------- käyttöliittymä
    void OnGUI()
    {
        float H = Screen.height, W = Screen.width, s = H / 1080f;
        if (gs == null || Mathf.Abs(gs.fontSize - Mathf.RoundToInt(34 * s)) > 1)
        {
            gs = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * s), fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow, alignment = TextAnchor.UpperLeft };
            gs.normal.textColor = Color.white;
            mid = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(44 * s) };
            big = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(120 * s) };
        }
        var white = Texture2D.whiteTexture;
        if (st == St.Select)
        {
            big.fontSize = Mathf.RoundToInt(110 * s); big.normal.textColor = new Color(1f, 0.85f, 0.25f);
            GUI.Label(new Rect(0, H * 0.04f, W, H * 0.16f), "TURPAAN VAAN", big);
            mid.normal.textColor = Color.white;
            GUI.Label(new Rect(0, H * 0.20f, W, H * 0.07f), Loc.T("VALITSE TAISTELIJA"), mid);
            for (int i = 0; i < 2; i++)
            {
                if (selFg[i] == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(selFg[i].tr.position);
                mid.normal.textColor = i == sel ? new Color(1f, 0.9f, 0.3f) : new Color(0.7f, 0.7f, 0.75f);
                GUI.Label(new Rect(sp.x - W * 0.2f, H - sp.y + H * 0.05f, W * 0.4f, H * 0.07f), selFg[i].name, mid);
            }
            mid.normal.textColor = Color.white;
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
            GUI.Label(new Rect(0, H * 0.80f, W, H * 0.07f), "[J]  " + Loc.T("ALOITA"), mid);
            mid.normal.textColor = Color.white;
            gs.alignment = TextAnchor.MiddleCenter; gs.fontSize = Mathf.RoundToInt(26 * s);
            GUI.Label(new Rect(0, H * 0.88f, W, H * 0.05f), Loc.T("Valitse: vasen / oikea     Isku: J     Potku: K     Erikoispotku: L     Torjunta: I     Poistu: Esc"), gs);
            GUI.Label(new Rect(0, H * 0.93f, W, H * 0.05f), Loc.T("Paras tulos") + ": " + hiScore.ToString("D6"), gs);
            gs.alignment = TextAnchor.UpperLeft; gs.fontSize = Mathf.RoundToInt(34 * s);
            return;
        }
        if (flashT > 0f && st == St.KO) { GUI.color = new Color(1, 1, 1, Mathf.Clamp01(flashT) * 0.6f); GUI.DrawTexture(new Rect(0, 0, W, H), white); GUI.color = Color.white; }
        if (P == null || E == null) return;
        // elämäpalkit
        float m = 36 * s, bw = W * 0.38f, bh = 34 * s, by = m * 0.7f;
        DrawBar(m, by, bw, bh, P.hp / 100f, P.shownHp / 100f, false, white);
        DrawBar(W - m - bw, by, bw, bh, E.hp / 100f, E.shownHp / 100f, true, white);
        gs.fontSize = Mathf.RoundToInt(30 * s);
        gs.alignment = TextAnchor.UpperLeft; GUI.Label(new Rect(m, by + bh + 6 * s, bw, 40 * s), P.name, gs);
        gs.alignment = TextAnchor.UpperRight; GUI.Label(new Rect(W - m - bw, by + bh + 6 * s, bw, 40 * s), E.name, gs);
        // kierrospisteet
        for (int i = 0; i < 2; i++)
        {
            GUI.color = i < winsP ? new Color(1f, 0.85f, 0.2f) : new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(m + bw - 60 * s + i * 30 * s, by + bh + 12 * s, 22 * s, 22 * s), white);
            GUI.color = i < winsE ? new Color(1f, 0.85f, 0.2f) : new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(W - m - bw + 38 * s - i * 30 * s, by + bh + 12 * s, 22 * s, 22 * s), white);
        }
        GUI.color = Color.white;
        gs.alignment = TextAnchor.UpperCenter; gs.fontSize = Mathf.RoundToInt(64 * s);
        GUI.Label(new Rect(W * 0.43f, m * 0.4f, W * 0.14f, 80 * s), Mathf.CeilToInt(Mathf.Max(0f, roundT)).ToString(), gs);
        gs.fontSize = Mathf.RoundToInt(28 * s);
        GUI.Label(new Rect(W * 0.35f, m * 0.4f + 76 * s, W * 0.3f, 40 * s), Loc.T("PISTEET") + "  " + score.ToString("D6"), gs);
        gs.alignment = TextAnchor.UpperLeft; gs.fontSize = Mathf.RoundToInt(34 * s);
        // erikoismittari
        float sw = 300 * s, sh2 = 20 * s, sx = m, sy = H - m - sh2;
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(sx - 4, sy - 4, sw + 8, sh2 + 8), white);
        bool ready = P.specialCd <= 0f;
        GUI.color = ready ? Color.Lerp(new Color(1f, 0.6f, 0.2f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)) : new Color(0.9f, 0.45f, 0.1f);
        GUI.DrawTexture(new Rect(sx, sy, sw * Mathf.Clamp01(1f - P.specialCd / P.spMax), sh2), white);
        GUI.color = Color.white;
        gs.fontSize = Mathf.RoundToInt(22 * s);
        GUI.Label(new Rect(sx, sy - 30 * s, sw, 28 * s), ready ? "[L]  " + Loc.T("ERIKOISPOTKU VALMIS") : Loc.T("ERIKOISPOTKU"), gs);
        gs.fontSize = Mathf.RoundToInt(34 * s);
        // tekstit
        if (st == St.Intro)
        {
            big.fontSize = Mathf.RoundToInt(100 * s); big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, H * 0.27f, W, H * 0.16f), banner, big);
            mid.normal.textColor = new Color(1f, 0.9f, 0.3f);
            GUI.Label(new Rect(0, H * 0.43f, W, H * 0.08f), bannerSub, mid); mid.normal.textColor = Color.white;
        }
        else if (st == St.Fight && stT < 0.9f)
        {
            big.fontSize = Mathf.RoundToInt(120 * s); big.normal.textColor = new Color(1f, 0.8f, 0.2f);
            GUI.Label(new Rect(0, H * 0.27f, W, H * 0.2f), Loc.T("TAISTELU!"), big);
        }
        else if (st == St.KO)
        {
            big.fontSize = Mathf.RoundToInt(150 * s); big.normal.textColor = new Color(1f, 0.25f, 0.2f);
            GUI.Label(new Rect(0, H * 0.22f, W, H * 0.25f), timeOut ? Loc.T("AIKA LOPPUI") : "K.O.", big);
            mid.normal.textColor = Color.white;
            string who = playerWonRound ? P.name : E.name;
            GUI.Label(new Rect(0, H * 0.46f, W, H * 0.08f), who + " " + Loc.T("VOITTAA KIERROKSEN"), mid);
        }
        else if (st == St.Over)
        {
            big.fontSize = Mathf.RoundToInt(120 * s); big.normal.textColor = new Color(1f, 0.25f, 0.2f);
            GUI.Label(new Rect(0, H * 0.22f, W, H * 0.2f), Loc.T("HÄVISIT"), big);
            EndText(W, H, s);
        }
        else if (st == St.Win)
        {
            big.fontSize = Mathf.RoundToInt(120 * s); big.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, H * 0.2f, W, H * 0.2f), Loc.T("MESTARI!"), big);
            EndText(W, H, s);
        }
        big.normal.textColor = Color.white; mid.normal.textColor = Color.white;
    }

    void EndText(float W, float H, float s)
    {
        mid.normal.textColor = Color.white;
        GUI.Label(new Rect(0, H * 0.43f, W, H * 0.08f), Loc.T("PISTEET") + "  " + score.ToString("D6") + "     " + Loc.T("PARAS") + "  " + Mathf.Max(hiScore, score).ToString("D6"), mid);
        if (stT > 1.6f)
        {
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
            GUI.Label(new Rect(0, H * 0.54f, W, H * 0.07f), "[J]  " + Loc.T("UUSI TURNAUS") + "     [K]  " + Loc.T("POISTU"), mid);
            mid.normal.textColor = Color.white;
        }
    }

    void DrawBar(float x, float y, float w, float h, float frac, float shown, bool rightToLeft, Texture2D white)
    {
        GUI.color = new Color(0, 0, 0, 0.7f); GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, h + 8), white);
        float fw = w * Mathf.Clamp01(frac), sw = w * Mathf.Clamp01(shown);
        GUI.color = new Color(0.8f, 0.15f, 0.1f);
        GUI.DrawTexture(new Rect(rightToLeft ? x + w - sw : x, y, sw, h), white);
        GUI.color = frac > 0.3f ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.45f, 0.15f);
        GUI.DrawTexture(new Rect(rightToLeft ? x + w - fw : x, y, fw, h), white);
        GUI.color = Color.white;
    }
}
