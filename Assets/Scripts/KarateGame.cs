using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turpaan Vaan: pikselikarate pelihallin koneessa. 1v1-turnaus: valitaan seitsemästä taistelijasta yksi, vastustajia kuusi (kolmen kierroksen otteluita,
/// kaksi voittoa riittää). Liikkeet: isku- ja potkuketjut (J, K), hyppy (väli) ja ilmaisku, hahmon oma erikoisliike (L). Kuvat ovat pelin omia liikekuvia pikseloituna (Tools/KarateAssets/make_karate.py -> Resources/Karate).
/// Rakenne kuten Tähtipyssyssä: oma kamera ja maailma kaukana (y 6000), maailma jäätyy (timeScale 0) ja kaikki pyörii skaalaamattomalla ajalla.
/// Ohjaus: liike nuolet/WASD/tatti, isku J (ketju J-J-J), potku K (ketju K-K), hyppy välilyönti, erikoisliike L (latautuu), torjunta I (pohjassa), Esc/Q poistuu.
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
    enum S { Idle, Walk, Punch, Kick, Special, Air, Jump, Hurt, Fall, GetUp, Block }

    /// kind: 'p' isku, 'k' potku, 's' erikoisliike, 'a' ilmaisku. hits = osumia (erikoisliikkeissä monta), dodge = osuma-ajan murto-osa, jonka ajan väistetään
    class Atk
    {
        public string anim; public char kind; public int idx;
        public float dur, hs, he, reach, dmg, stun, push, lunge, lungeEnd, dodge, cd; public bool kd, sand, feint; public int hits = 1;
    }
    static Atk At(string anim, char kind, int idx, float dur, float hs, float he, float reach, float dmg, float stun, float push, bool kd = false)
        => new Atk { anim = anim, kind = kind, idx = idx, dur = dur, hs = hs, he = he, reach = reach, dmg = dmg, stun = stun, push = push, kd = kd };
    static readonly Atk[] Punches =
    {
        At("punch",  'p', 0, 0.32f, 0.30f, 0.60f, 46f, 5f, 0.26f, 45f),
        At("punch2", 'p', 1, 0.38f, 0.30f, 0.60f, 50f, 7f, 0.30f, 60f),
        At("punch3", 'p', 2, 0.50f, 0.35f, 0.65f, 52f, 10f, 0.45f, 120f),
    };
    static readonly Atk[] Kicks =
    {
        At("kick",  'k', 0, 0.50f, 0.36f, 0.64f, 58f, 9f, 0.40f, 80f),
        At("kick2", 'k', 1, 0.62f, 0.36f, 0.66f, 64f, 12f, 0.45f, 110f),
        At("kick3", 'k', 2, 0.60f, 0.36f, 0.66f, 62f, 14f, 0.50f, 150f, true),
    };
    static readonly Atk AAir = At("air", 'a', 0, 0.45f, 0.08f, 0.95f, 52f, 9f, 0.4f, 90f, true);
    const float FallDur = 0.85f, GetUpDur = 0.7f;
    const float JumpV = 235f, Grav = 640f;

    /// Hahmon oma erikoisliike (L).
    static Atk Sp(float dur, float hs, float he, float reach, float dmg, float stun, float push, bool kd, int hits = 1, float cd = 4f)
        => new Atk { anim = "special", kind = 's', dur = dur, hs = hs, he = he, reach = reach, dmg = dmg, stun = stun, push = push, kd = kd, hits = hits, cd = cd };
    static Atk SpecialOf(string key)
    {
        switch (key)
        {
            case "rocco":    return Sp(0.78f, 0.18f, 0.82f, 64f, 21f, 0.25f, 150f, true, 3, 4f);                       // tullimylly: pyörivä potkusarja
            case "gustav":   { var a = Sp(0.80f, 0.42f, 0.50f, 135f, 0f, 1.8f, 0f, false, 1, 6f); a.sand = true; return a; }   // hiekanheitto: lamauttaa
            case "kapteeni": { var a = Sp(0.95f, 0.40f, 0.75f, 74f, 16f, 0.5f, 160f, true, 1, 4f); a.lunge = 150f; a.lungeEnd = 0.8f; return a; }                       // liuku: syöksyy matalana eteen
            case "lenkkari": { var a = Sp(0.85f, 0.52f, 0.78f, 62f, 15f, 0.5f, 120f, true, 1, 4f); a.feint = true; return a; }  // lippishämäys: ei torjuttavissa
            case "horhe":    return Sp(1.0f, 0.22f, 0.92f, 54f, 24f, 0.2f, 70f, true, 8, 5f);                        // läpsykombo
            case "marcus":   { var a = Sp(0.95f, 0.55f, 0.82f, 72f, 17f, 0.5f, 150f, true, 1, 4f); a.dodge = 0.5f; a.lunge = 105f; return a; }
            default:         { var a = Sp(1.0f, 0.60f, 0.86f, 74f, 20f, 0.5f, 160f, true, 1, 4f); a.dodge = 0.45f; a.lunge = 100f; return a; }   // deacon
        }
    }
    static readonly Dictionary<string, Atk> specCache = new Dictionary<string, Atk>();
    static Atk SpecialFor(string key) { if (!specCache.TryGetValue(key, out var a)) specCache[key] = a = SpecialOf(key); return a; }

    static readonly string[] Roster = { "rocco", "gustav", "kapteeni", "lenkkari", "horhe", "marcus", "deacon" };
    static readonly string[] RosterNames = { "ROCCO", "GUSTAV", "KAPTEENI", "LENKKARI", "HORHE", "MARCUS", "DEACON" };
    static readonly string[] SpecialNames = { "TULLIMYLLY", "HIEKANHEITTO", "LIUKU", "LIPPISHÄMÄYS", "LÄPSYKOMBO", "VÄISTÖISKU", "VÄISTÖKOUKKU" };
    // vastustajien järjestys (heikoimmasta vahvimpaan); pelaajan oma hahmo jätetään pois
    static readonly string[] Rank = { "lenkkari", "horhe", "kapteeni", "marcus", "gustav", "rocco", "deacon" };

    class Fg
    {
        public string key, name; public bool player;
        public Transform tr; public SpriteRenderer sr, shadow;
        public float x, vx, y, vy, hp = 100f, shownHp = 100f;
        public int face = 1;
        public S s; public float t, stunT;
        public Atk atk; public int hitIdx, atkId, queue; public bool aiRolled;
        public bool stayDown; public int comboN; public float comboAt;
        public float specialCd, spMax = 4f;
        public float dmgMul = 1f, speed = 95f;
        // tekoäly
        public float thinkT, moveT, blockT, thinkMin = 0.8f, thinkMax = 1.3f, blockChance, specialChance, chainChance;
        public int moveDir, seenAtk; public bool moveAway;
        public bool IsAtk => s == S.Punch || s == S.Kick || s == S.Special || s == S.Air;
        public bool Grounded => s != S.Fall && s != S.GetUp;
        public bool Airborne => y > 0.5f;
    }

    struct Opp { public float speed, dmg, tMin, tMax, block, special, chain; }
    static Opp O(float speed, float dmg, float tMin, float tMax, float block, float special, float chain) { return new Opp { speed = speed, dmg = dmg, tMin = tMin, tMax = tMax, block = block, special = special, chain = chain }; }
    static Opp StatsOf(string key)
    {
        switch (key)
        {
            case "lenkkari": return O(70f, 0.75f, 0.95f, 1.5f, 0.05f, 0.06f, 0.10f);
            case "horhe":    return O(82f, 0.85f, 0.85f, 1.35f, 0.12f, 0.10f, 0.20f);
            case "kapteeni": return O(88f, 0.95f, 0.75f, 1.2f, 0.18f, 0.14f, 0.25f);
            case "marcus":   return O(95f, 1.0f, 0.6f, 1.0f, 0.26f, 0.12f, 0.35f);
            case "gustav":   return O(100f, 1.1f, 0.5f, 0.85f, 0.32f, 0.2f, 0.4f);
            case "rocco":    return O(100f, 1.1f, 0.5f, 0.85f, 0.32f, 0.22f, 0.4f);
            default:         return O(105f, 1.2f, 0.4f, 0.75f, 0.42f, 0.22f, 0.5f);   // deacon
        }
    }

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
    int sel;                   // valittu taistelija (Roster)
    string[] opps;             // vastustajat järjestyksessä
    int bout, round, winsP, winsE;
    float roundT; string banner, bannerSub;
    int score, hiScore, atkCounter;
    float hitStop, shake, flashT;
    bool playerWonRound, timeOut;
    readonly Fg[] selFg = new Fg[7];
    GUIStyle gs, big, mid;
    float lastSelDir;
    string heroKey;

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
        for (float x = -W; x < W; x += 24f)
            Quad(x, 14f, 2f, 150f, new Color(0.09f, 0.07f, 0.14f), -99, "lankku");
        Quad(0, 28f, 170f, 78f, new Color(0.07f, 0.06f, 0.11f), -98, "ikkunakehys");
        Quad(0, 28f, 162f, 70f, new Color(0.17f, 0.22f, 0.42f), -97, "yötaivas");
        var moon = new GameObject("kuu"); moon.transform.SetParent(root, false); moon.transform.localPosition = new Vector3(34f, 38f, 0f);
        var ms = moon.AddComponent<SpriteRenderer>(); ms.sprite = CircleSprite(12, new Color(1f, 0.93f, 0.62f)); ms.sortingOrder = -96; bgObjs.Add(moon);
        for (int i = 0; i < 14; i++) Quad(-75f + (i * 37 % 150), 8f + (i * 53 % 54), 1.5f, 1.5f, new Color(0.9f, 0.9f, 1f, 0.8f), -96, "tähti");
        Quad(-30f, 4f, 56f, 8f, new Color(0.09f, 0.1f, 0.2f), -95); Quad(-30f, 10f, 34f, 6f, new Color(0.09f, 0.1f, 0.2f), -95); Quad(-30f, 15f, 14f, 6f, new Color(0.09f, 0.1f, 0.2f), -95);
        Quad(0, 28f, 3f, 70f, new Color(0.07f, 0.06f, 0.11f), -94, "ristikko"); Quad(0, 28f, 162f, 3f, new Color(0.07f, 0.06f, 0.11f), -94, "ristikko");
        for (int side = -1; side <= 1; side += 2)
            for (int k = 0; k < 2; k++)
            {
                float cx = side * (150f + k * 54f);
                Quad(cx, 22f, 50f, 100f, new Color(0.78f, 0.72f, 0.58f), -98, "ovi");
                Quad(cx, 22f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi"); Quad(cx, -10f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi");
                Quad(cx, 22f, 2f, 100f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi"); Quad(cx, 58f, 50f, 2f, new Color(0.3f, 0.2f, 0.12f), -97, "ovi");
            }
        for (int side = -1; side <= 1; side += 2)
        {
            float lx = side * 100f;
            Quad(lx, 74f, 1f, 22f, new Color(0.05f, 0.05f, 0.07f), -93, "naru");
            Quad(lx, 58f, 15f, 20f, new Color(0.78f, 0.15f, 0.12f), -92, "lyhty");
            Quad(lx, 58f, 9f, 14f, new Color(1f, 0.78f, 0.3f, 0.9f), -91, "lyhty");
            Quad(lx, 68f, 17f, 2f, new Color(0.15f, 0.1f, 0.08f), -91, "lyhty"); Quad(lx, 48f, 17f, 2f, new Color(0.15f, 0.1f, 0.08f), -91, "lyhty");
        }
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
    static string NameOf(string key) { int i = System.Array.IndexOf(Roster, key); return i >= 0 ? RosterNames[i] : key.ToUpperInvariant(); }

    Fg MakeFighter(string key, bool player, float x, int face)
    {
        var f = new Fg { key = key, name = NameOf(key), player = player, x = x, face = face };
        var go = new GameObject(f.name); go.transform.SetParent(root, false); f.tr = go.transform;
        f.sr = go.AddComponent<SpriteRenderer>(); f.sr.sortingOrder = player ? 12 : 11;
        var sh = new GameObject("varjo"); sh.transform.SetParent(go.transform, false);
        sh.transform.localScale = new Vector3(2.2f, 0.45f, 1f);
        f.shadow = sh.AddComponent<SpriteRenderer>(); f.shadow.sprite = CircleSprite(8, Color.white); f.shadow.color = new Color(0f, 0f, 0f, 0.4f); f.shadow.sortingOrder = 5;
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
        Sprite sp; Color col = Color.white;
        switch (f.s)
        {
            case S.Punch: case S.Kick: sp = Frame(f, f.atk.anim, f.t, f.atk.dur, false); break;
            case S.Special:
                sp = Frame(f, "special", f.t, f.atk.dur, false);
                if (f.atk.dodge > 0f && f.t / f.atk.dur < f.atk.dodge) col = new Color(1f, 1f, 1f, 0.55f);   // väistö
                break;
            case S.Air: sp = Frame(f, Load(f.key, "air") != null ? "air" : "kick", f.t, f.atk.dur, false); break;
            case S.Jump:
                {
                    var j = Load(f.key, "jump");
                    if (j != null) sp = j[Mathf.Min(j.Length - 1, f.vy > 70f ? 0 : f.vy > -70f ? 1 : 2)];
                    else sp = Frame(f, "idle", 0f, 1f, false);
                    break;
                }
            case S.Hurt:
                sp = Frame(f, "hurt", f.t, Mathf.Max(0.2f, Mathf.Min(f.stunT, 0.5f)), false);
                if (f.stunT > 1f) col = new Color(1f, 0.9f, 0.55f);   // hiekka silmissä
                break;
            case S.Fall: sp = Frame(f, "fall", f.t, FallDur, false); break;
            case S.GetUp: sp = Frame(f, "getup", f.t, GetUpDur, false); break;
            case S.Block:
                if (Load(f.key, "block") != null) sp = Frame(f, "block", Mathf.Min(f.t, 0.15f), 0.15f, false);
                else { sp = Frame(f, "idle", 0f, 1f, false); col = new Color(0.75f, 0.85f, 1f); }
                break;
            case S.Walk: { var a = Load(f.key, "walk"); sp = Frame(f, "walk", Time.unscaledTime, Mathf.Clamp((a != null ? a.Length : 8) * 0.07f, 0.5f, 0.9f), true, f.moveAway); break; }
            default: { var a = Load(f.key, "idle"); sp = Frame(f, "idle", Time.unscaledTime, Mathf.Clamp((a != null ? a.Length : 6) * 0.1f, 0.6f, 1.6f), true); break; }
        }
        f.sr.sprite = sp; f.sr.color = col;
        f.sr.flipX = f.face < 0;
        f.tr.localPosition = new Vector3(f.x, GroundY + f.y, 0f);
        f.shadow.transform.localPosition = new Vector3(0f, 1f - f.y, 0f);   // varjo pysyy maassa
        f.shadow.color = new Color(0f, 0f, 0f, 0.4f * Mathf.Clamp01(1f - f.y / 90f));
    }

    // ---------------------------------------------------------------- valintaruutu
    void BuildSelect()
    {
        for (int i = 0; i < Roster.Length; i++) selFg[i] = MakeFighter(Roster[i], i == 0, 0f, 1);
        sel = 0;
    }

    void ClearSelect()
    {
        for (int i = 0; i < selFg.Length; i++) { if (selFg[i] != null && selFg[i].tr != null) Destroy(selFg[i].tr.gameObject); selFg[i] = null; }
    }

    void UpdateSelect(float dt)
    {
        float mx = CoopInput.Move(pi).x;
        int dir = mx > 0.5f ? 1 : mx < -0.5f ? -1 : 0;
        if (dir != 0 && lastSelDir == 0) { sel = (sel + dir + Roster.Length) % Roster.Length; Play(Sfx.Blip, 0.8f); }
        lastSelDir = dir;
        float sp = Mathf.Min(46f, (halfW - 18f) / 3f);
        for (int i = 0; i < Roster.Length; i++)
        {
            var f = selFg[i]; if (f == null) continue;
            bool on = i == sel;
            f.s = S.Idle; f.face = 1; f.x = (i - 3) * sp;
            Show(f);
            f.sr.color = on ? Color.white : new Color(0.42f, 0.42f, 0.48f);
            f.tr.localScale = Vector3.one * (on ? 1.1f : 0.78f);
            f.sr.sortingOrder = on ? 13 : 11;
        }
        if (stT > 0.3f && (CoopInput.Punch(pi) || CoopInput.Pick(pi) || CoopInput.Special(pi)))
        {
            Play(Sfx.Bell, 0.7f);
            string k = Roster[sel];
            ClearSelect();
            StartTournament(k);
        }
    }

    // ---------------------------------------------------------------- turnaus
    void StartTournament(string key)
    {
        heroKey = key;
        var l = new List<string>();
        foreach (var k in Rank) if (k != key) l.Add(k);
        opps = l.ToArray();
        bout = 0; score = 0;
        SetupBout();
    }

    void SetupBout()
    {
        DestroyFighter(ref P); DestroyFighter(ref E);
        string ok = opps[bout];
        var o = StatsOf(ok);
        P = MakeFighter(heroKey, true, -70f, 1);
        P.spMax = SpecialFor(heroKey)?.cd ?? 4f;
        E = MakeFighter(ok, false, 70f, -1);
        E.speed = o.speed; E.dmgMul = o.dmg; E.thinkMin = o.tMin; E.thinkMax = o.tMax; E.blockChance = o.block; E.specialChance = o.special; E.chainChance = o.chain;
        winsP = winsE = 0; round = 1;
        StartRound();
    }

    void StartRound()
    {
        foreach (var f in new[] { P, E })
        {
            f.hp = f.shownHp = 100f; f.s = S.Idle; f.t = 0f; f.vx = 0f; f.y = 0f; f.vy = 0f; f.stayDown = false; f.comboN = 0; f.specialCd = 0f; f.queue = 0;
            f.thinkT = Random.Range(0.6f, 1.2f); f.moveT = 0f; f.blockT = 0f;
        }
        P.x = -70f; E.x = 70f; P.face = 1; E.face = -1;
        P.specialCd = 1.5f;
        roundT = 60f;
        st = St.Intro; stT = 0f;
        banner = Loc.T("KIERROS") + " " + round;
        bannerSub = "VS  " + E.name + "   ( " + (bout + 1) + " / " + opps.Length + " )";
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
        bool esc = (k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)) || CoopInput.StartPressed(pi);
        if (Time.unscaledTime - openedAt < 0.35f) return;
        switch (st)
        {
            case St.Select:
                if (esc || CoopInput.Kick(pi)) { End(); return; }
                UpdateSelect(dt);
                return;
            case St.Over:
            case St.Win:
                if (st == St.Win) P.s = S.Idle;
                Show(P); Show(E);
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
                if (bout >= opps.Length) { st = St.Win; stT = 0f; Play(Sfx.Win, 0.9f); Hi(); }
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

    // ---------------------------------------------------------------- hyökkäysten aloitus
    bool Free(Fg f) => f.s == S.Idle || f.s == S.Walk || f.s == S.Block;

    static int ChainLen(string key, char kind)
    {
        string b = kind == 'p' ? "punch" : "kick";
        return Load(key, b + "3") != null ? 3 : Load(key, b + "2") != null ? 2 : 1;
    }

    void StartAtk(Fg f, Atk a)
    {
        f.atk = a; f.t = 0f; f.hitIdx = 0; f.atkId = ++atkCounter; f.queue = 0; f.aiRolled = false;
        f.s = a.kind == 'p' ? S.Punch : a.kind == 'k' ? S.Kick : a.kind == 's' ? S.Special : S.Air;
        Play(Sfx.Whoosh, 0.5f, a.kind == 's' ? 0.75f : a.kind == 'k' ? 0.9f : 1.15f);
    }

    void ChainNext(Fg f)
    {
        int q = f.queue; f.queue = 0;
        Atk n;
        if (q == 1) n = f.atk.kind == 'p' && f.atk.idx + 1 < ChainLen(f.key, 'p') ? Punches[f.atk.idx + 1] : Punches[0];
        else n = f.atk.kind == 'k' && f.atk.idx + 1 < ChainLen(f.key, 'k') ? Kicks[f.atk.idx + 1] : Kicks[0];
        StartAtk(f, n);
    }

    void Jump(Fg f)
    {
        f.s = S.Jump; f.t = 0f; f.vy = JumpV;
    }

    // ---------------------------------------------------------------- pelaaja
    void UpdatePlayer(float dt)
    {
        P.specialCd = Mathf.Max(0f, P.specialCd - dt);
        Vector2 m = CoopInput.Move(pi);
        float mx = Mathf.Abs(m.x) > 0.3f ? Mathf.Sign(m.x) : 0f;
        // ketjuisku: seuraava nappi vastauksena iskun aikana
        if ((P.s == S.Punch || P.s == S.Kick) && P.t / P.atk.dur > 0.2f)
        {
            if (CoopInput.Punch(pi)) P.queue = 1;
            else if (CoopInput.Kick(pi)) P.queue = 2;
            return;
        }
        if (P.s == S.Jump)
        {
            P.face = E.x >= P.x ? 1 : -1;
            P.x += mx * P.speed * 0.85f * dt;
            if (CoopInput.Punch(pi) || CoopInput.Kick(pi)) StartAtk(P, AAir);
            return;
        }
        if (P.s == S.Air) { P.x += mx * P.speed * 0.6f * dt; return; }
        if (!Free(P)) return;
        P.face = E.x >= P.x ? 1 : -1;
        if (CoopInput.Block(pi))
        {
            if (P.s != S.Block) { P.s = S.Block; P.t = 0f; }
            return;
        }
        if (P.s == S.Block) { P.s = S.Idle; P.t = 0f; }
        if (CoopInput.Jump(pi)) { Jump(P); return; }
        if (CoopInput.Special(pi) && P.specialCd <= 0f && SpecialFor(heroKey) != null) { var sp = SpecialFor(heroKey); StartAtk(P, sp); P.specialCd = sp.cd; return; }
        if (CoopInput.Kick(pi)) { StartAtk(P, Kicks[0]); return; }
        if (CoopInput.Punch(pi)) { StartAtk(P, Punches[0]); return; }
        P.moveAway = mx != 0f && Mathf.Sign(mx) != P.face;
        if (mx != 0f) { P.x += mx * P.speed * dt; P.s = S.Walk; } else P.s = S.Idle;
    }

    // ---------------------------------------------------------------- tekoäly
    void UpdateAI(float dt)
    {
        var e = E;
        e.specialCd = Mathf.Max(0f, e.specialCd - dt);
        float d = Mathf.Abs(P.x - e.x);
        if (e.s == S.Jump)
        {
            e.face = P.x >= e.x ? 1 : -1;
            e.x += e.face * e.speed * 0.8f * dt;
            if (d < 54f && Random.value < 4f * dt) StartAtk(e, AAir);
            return;
        }
        if (e.s == S.Air) { e.x += e.face * e.speed * 0.5f * dt; return; }
        if (!Free(e)) return;
        e.face = P.x >= e.x ? 1 : -1;
        if (P.IsAtk && P.atkId != e.seenAtk)
        {
            e.seenAtk = P.atkId;
            if (d < P.atk.reach + 22f && !P.Airborne && Random.value < e.blockChance) { e.s = S.Block; e.t = 0f; e.blockT = Random.Range(0.35f, 0.7f); }
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
        if (d > 80f && d < 150f && Random.value < 0.08f) { Jump(e); e.thinkT = 0.8f; return; }
        Atk pick;
        if (e.specialCd <= 0f && r < e.specialChance && SpecialFor(e.key) != null) pick = SpecialFor(e.key);
        else pick = Random.value < 0.55f ? Punches[0] : Kicks[0];
        if (d > pick.reach - 4f) { e.moveDir = e.face; e.moveT = Random.Range(0.25f, 0.6f); e.thinkT = 0.05f; return; }
        if (Random.value < 0.14f) { e.moveDir = -e.face; e.moveT = Random.Range(0.3f, 0.6f); e.thinkT = Random.Range(0.1f, 0.3f); return; }
        StartAtk(e, pick);
        if (pick.kind == 's') e.specialCd = pick.cd + 2f;
        e.thinkT = Random.Range(e.thinkMin, e.thinkMax);
    }

    // ---------------------------------------------------------------- taistelu
    void UpdateFighter(Fg f, Fg o, float dt)
    {
        f.t += dt;
        f.x += f.vx * dt; f.vx = Mathf.MoveTowards(f.vx, 0f, 420f * dt);
        if (f.s == S.Special && f.atk.lunge > 0f)
        {
            float lu = f.t / f.atk.dur;
            if (lu >= f.atk.hs - 0.25f && lu < (f.atk.lungeEnd > 0f ? f.atk.lungeEnd : f.atk.hs + 0.08f) && Mathf.Abs(o.x - f.x) > 30f) f.x += f.face * f.atk.lunge * dt;
        }
        if (f.y > 0f || f.vy > 0f)
        {
            f.vy -= Grav * dt; f.y += f.vy * dt;
            if (f.y <= 0f)
            {
                f.y = 0f; f.vy = 0f;
                if (f.s == S.Jump || f.s == S.Air) { f.s = S.Idle; f.t = 0f; }
            }
        }
        f.x = Mathf.Clamp(f.x, -ArenaX, ArenaX);
        switch (f.s)
        {
            case S.Punch: case S.Kick: case S.Special: case S.Air:
                {
                    var a = f.atk;
                    float u = f.t / a.dur;
                    if (st == St.Fight)
                    {
                        int due = u < a.hs ? 0 : u >= a.he ? a.hits : Mathf.Min(a.hits, (int)((u - a.hs) / (a.he - a.hs) * a.hits) + 1);
                        while (f.hitIdx < due) { Strike(f, o, f.hitIdx); f.hitIdx++; }
                    }
                    if (!f.player && !f.aiRolled && u >= a.he && (a.kind == 'p' || a.kind == 'k'))
                    {
                        f.aiRolled = true;
                        if (Random.value < f.chainChance) f.queue = a.kind == 'p' ? 1 : 2;
                    }
                    if (u >= a.he && f.queue != 0 && (a.kind == 'p' || a.kind == 'k')) { ChainNext(f); break; }
                    if (u >= 1f)
                    {
                        if (f.s == S.Air) f.t = a.dur;           // ilmassa kunnes laskeutuu
                        else { f.s = S.Idle; f.t = 0f; f.queue = 0; }
                    }
                    break;
                }
            case S.Hurt:
                if (f.t >= f.stunT) { f.s = S.Idle; f.t = 0f; }
                break;
            case S.Fall:
                if (f.t >= FallDur && f.y <= 0.5f) { if (f.stayDown) f.t = FallDur; else { f.s = S.GetUp; f.t = 0f; } }
                break;
            case S.GetUp:
                if (f.t >= GetUpDur) { f.s = S.Idle; f.t = 0f; }
                break;
        }
    }

    void Strike(Fg a, Fg o, int idx)
    {
        var at = a.atk;
        bool last = idx >= at.hits - 1;
        float dx = (o.x - a.x) * a.face;
        if (dx < -6f || dx > at.reach) return;
        if (Mathf.Abs(a.y - o.y) > (at.kind == 'a' ? 46f : 24f)) return;
        if (!o.Grounded) return;
        if (o.s == S.Special && o.atk.dodge > 0f && o.t / o.atk.dur < o.atk.dodge) return;   // väistää
        bool blocked = o.s == S.Block && o.face == -a.face && !at.feint;
        if (at.sand) { SandThrow(a, o, blocked); return; }
        float dmg = at.dmg / at.hits * (a.player ? 1f : a.dmgMul);
        Vector2 sp = new Vector2((a.x + o.x) * 0.5f + a.face * 6f, GroundY + 34f + (a.y + o.y) * 0.5f);
        bool mid = at.hits > 1 && !last;
        if (blocked)
        {
            o.hp = Mathf.Max(0.01f, o.hp - dmg * 0.15f);
            o.vx = a.face * at.push * 0.45f;
            Play(Sfx.Block, 0.8f); Spark(sp, new Color(0.6f, 0.85f, 1f), 5);
            hitStop = Mathf.Max(hitStop, 0.04f);
            return;
        }
        o.hp = Mathf.Max(0f, o.hp - dmg);
        Play(at.kind == 'p' || mid ? Sfx.Punch : Sfx.Kick, mid ? 0.7f : 1f, mid ? Random.Range(1.2f, 1.45f) : Random.Range(0.92f, 1.08f));
        Spark(sp, at.kind == 's' ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.95f, 0.5f), mid ? 4 : at.kind == 's' ? 12 : 7);
        if (!mid) { o.comboN = Time.unscaledTime - o.comboAt < 1.6f ? o.comboN + 1 : 1; o.comboAt = Time.unscaledTime; }
        bool kd = (at.kd && last) || o.hp <= 0f || (!mid && o.comboN >= 3) || o.Airborne;
        o.vx = a.face * at.push * (kd ? 1.5f : mid ? 0.15f : 1f);
        o.queue = 0;
        hitStop = o.hp <= 0f ? 0.28f : mid ? 0.025f : at.kind == 's' ? 0.12f : 0.06f;
        shake = Mathf.Max(shake, o.hp <= 0f ? 3f : mid ? 0.4f : at.kind == 's' ? 2f : 0.8f);
        if (kd) { o.s = S.Fall; o.t = 0f; o.stayDown = o.hp <= 0f; o.comboN = 0; }
        else { o.s = S.Hurt; o.t = 0f; o.stunT = at.stun; }
    }

    /// Gustavin hiekanheitto: ei vahinkoa, mutta lamauttaa (ellei vastustaja torju).
    void SandThrow(Fg a, Fg o, bool blocked)
    {
        for (int i = 0; i < 22; i++)
            Puff(new Vector2(a.x + a.face * 18f, GroundY + 40f + a.y), new Color(0.86f, 0.74f, 0.46f), new Vector2(a.face * Random.Range(130f, 280f), Random.Range(-25f, 35f)), 0.55f);
        Play(Sfx.Whoosh, 0.6f, 1.6f);
        if (blocked) { Play(Sfx.Block, 0.6f); return; }
        o.s = S.Hurt; o.t = 0f; o.stunT = a.atk.stun; o.vx = a.face * 15f; o.queue = 0;
    }

    void Separate()
    {
        if (!P.Grounded || !E.Grounded || P.Airborne || E.Airborne) return;
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
            float ang = Random.Range(0f, Mathf.PI * 2f), sp = Random.Range(60f, 170f);
            Puff(p, c, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp, 0.28f);
        }
    }

    void Puff(Vector2 p, Color c, Vector2 v, float life)
    {
        var go = new GameObject("kipinä"); go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(p.x, p.y, 0f);
        float sz = Random.Range(2f, 4f); go.transform.localScale = new Vector3(sz, sz, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White(); sr.color = c; sr.sortingOrder = 30;
        pts.Add(new Pt { tr = go.transform, sr = sr, v = v, life = life, max = life, c = c });
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
            // ylhäältä alas: otsikko, valitun nimi ja erikoisliike; hahmot keskellä; pikkunimet, aloitus ja ohjeet alhaalla
            big.fontSize = Mathf.RoundToInt(84 * s); big.normal.textColor = new Color(1f, 0.85f, 0.25f);
            GUI.Label(new Rect(0, H * 0.015f, W, H * 0.12f), "TURPAAN VAAN", big);
            mid.fontSize = Mathf.RoundToInt(30 * s); mid.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(0, H * 0.135f, W, H * 0.05f), Loc.T("VALITSE TAISTELIJA"), mid);
            big.fontSize = Mathf.RoundToInt(70 * s); big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, H * 0.19f, W, H * 0.11f), RosterNames[sel], big);
            mid.fontSize = Mathf.RoundToInt(34 * s); mid.normal.textColor = new Color(1f, 0.6f, 0.2f);
            GUI.Label(new Rect(0, H * 0.30f, W, H * 0.06f), SpecialNames[sel].Length > 0 ? Loc.T("ERIKOISLIIKE") + ":  " + Loc.T(SpecialNames[sel]) : Loc.T("Ei erikoisliikettä"), mid);
            gs.alignment = TextAnchor.MiddleCenter; gs.fontSize = Mathf.RoundToInt(20 * s);
            for (int i = 0; i < selFg.Length; i++)
            {
                if (selFg[i] == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(selFg[i].tr.position);
                gs.normal.textColor = i == sel ? new Color(1f, 0.9f, 0.3f) : new Color(0.7f, 0.7f, 0.75f);
                GUI.Label(new Rect(sp.x - W * 0.06f, H * 0.855f, W * 0.12f, H * 0.04f), selFg[i].name, gs);
            }
            gs.normal.textColor = Color.white;
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            mid.fontSize = Mathf.RoundToInt(38 * s); mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
            GUI.Label(new Rect(0, H * 0.895f, W, H * 0.055f), "[J]  " + Loc.T("ALOITA"), mid);
            mid.normal.textColor = Color.white; mid.fontSize = Mathf.RoundToInt(44 * s);
            gs.fontSize = Mathf.RoundToInt(20 * s);
            GUI.Label(new Rect(0, H * 0.95f, W, H * 0.04f), Loc.T("Valitse: vasen / oikea   Isku J   Potku K   Hyppy välilyönti   Erikoisliike L   Torjunta I   Poistu: Esc / Start   Paras: ") + hiScore.ToString("D6"), gs);
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
        if (SpecialFor(heroKey) != null)
        {
        float sw = 300 * s, sh2 = 20 * s, sx = m, sy = H - m - sh2;
        GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(sx - 4, sy - 4, sw + 8, sh2 + 8), white);
        bool ready = P.specialCd <= 0f;
        GUI.color = ready ? Color.Lerp(new Color(1f, 0.6f, 0.2f), Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f)) : new Color(0.9f, 0.45f, 0.1f);
        GUI.DrawTexture(new Rect(sx, sy, sw * Mathf.Clamp01(1f - P.specialCd / P.spMax), sh2), white);
        GUI.color = Color.white;
        gs.fontSize = Mathf.RoundToInt(22 * s);
        string spn = Loc.T(SpecialNames[System.Array.IndexOf(Roster, heroKey)]);
        GUI.Label(new Rect(sx, sy - 30 * s, sw * 1.6f, 28 * s), ready ? "[L]  " + spn : spn, gs);
        gs.fontSize = Mathf.RoundToInt(34 * s);
        }
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
