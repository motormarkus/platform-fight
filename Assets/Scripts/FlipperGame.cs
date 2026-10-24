using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flipper Flapper: flipperi pelihallin koneessa. Oma kamera ja maailma kaukana (y 6000), maailma jäätyy (timeScale 0), kaikki pyörii
/// skaalaamattomalla ajalla. Fysiikka on omaa (pallo + janat + ympyrät, useita alaskelia), joten se toimii jäädytetyssä ajassa.
/// Kuvat ovat valinnaisia: Resources/Flipper/{pohja,pallo,maila,pompp_0..2}.png; puuttuessa kaikki piirretään koodilla.
/// Ohjaus: vasen maila torjunta-nappi (I) / nuoli vasen / A, oikea maila juoksu-nappi (Shift) / nuoli oikea / D, välilyönti (pohjassa, vapautus) laukaisee, Esc/Q/Start poistuu.
/// </summary>
public class FlipperGame : MonoBehaviour
{
    public static FlipperGame Instance;
    public static bool Active => Instance != null;
    const string HiKey = "flipper_hi";

    public static void Begin(PlayerController who)
    {
        if (Instance != null || who == null) return;
        var go = new GameObject("Flipper Flapper");
        go.AddComponent<FlipperGame>().Init(who);
    }

    // ---------------------------------------------------------------- kuvat
    static Sprite whiteSp;
    static Sprite White()
    {
        if (whiteSp == null) whiteSp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSp;
    }

    static readonly Dictionary<string, Sprite> circles = new Dictionary<string, Sprite>();
    /// Pehmeä (antialiasoitu) ympyrä, halkaisija 2r yksikköä; kuva 64 x 64 skaalautuu sulavasti.
    static Sprite Circle(int r, Color c)
    {
        string k = r + "|" + c;
        if (circles.TryGetValue(k, out var s) && s != null) return s;
        const int D = 64;
        var t = new Texture2D(D, D, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        float rr = D * 0.5f - 1f;
        for (int y = 0; y < D; y++)
            for (int x = 0; x < D; x++)
            {
                float d = Mathf.Sqrt((x - D * 0.5f + 0.5f) * (x - D * 0.5f + 0.5f) + (y - D * 0.5f + 0.5f) * (y - D * 0.5f + 0.5f));
                float a = Mathf.Clamp01(rr - d + 0.5f);
                t.SetPixel(x, y, new Color(c.r, c.g, c.b, c.a * a));
            }
        t.Apply();
        s = Sprite.Create(t, new Rect(0, 0, D, D), new Vector2(0.5f, 0.5f), D / (2f * r), 0, SpriteMeshType.FullRect);
        circles[k] = s;
        return s;
    }

    /// Valinnainen kuva Resources/Flipper/<nimi>; ppu valitaan niin, että kuva on width yksikköä leveä.
    static Sprite Img(string name, float width, Vector2 pivot)
    {
        var t = Resources.Load<Texture2D>("Flipper/" + name);
        if (t == null) return null;
        t.filterMode = FilterMode.Bilinear; t.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, t.width / width, 0, SpriteMeshType.FullRect);
    }

    // ---------------------------------------------------------------- äänet (syntetisoitu)
    static class Sfx
    {
        public static AudioClip Bonk, Click, Launch, Ping, Drain, Bonus, Start, Sling;
        const int SR = 44100;
        static System.Random rnd = new System.Random(5);
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
            if (Bonk != null) return;
            float p1 = 0f;
            Bonk = Make("fl_bonk", 0.18f, (t, u) => { p1 += 2f * Mathf.PI * Mathf.Lerp(900f, 380f, u) / SR; return Mathf.Sin(p1) * 0.45f * Mathf.Exp(-t * 18f); });
            float l2 = 0f;
            Click = Make("fl_click", 0.06f, (t, u) => { l2 += (Noise() - l2) * 0.5f; return l2 * 0.5f * Mathf.Exp(-t * 60f); });
            float l3 = 0f;
            Launch = Make("fl_launch", 0.35f, (t, u) => { l3 += (Noise() - l3) * Mathf.Lerp(0.05f, 0.4f, u); return l3 * 0.8f * Mathf.Sin(u * Mathf.PI); });
            float p4 = 0f;
            Ping = Make("fl_ping", 0.2f, (t, u) => { p4 += 2f * Mathf.PI * 1760f / SR; return Mathf.Sin(p4) * 0.3f * Mathf.Exp(-t * 14f); });
            float p5 = 0f;
            Drain = Make("fl_drain", 0.8f, (t, u) => { p5 += 2f * Mathf.PI * Mathf.Lerp(420f, 90f, u) / SR; return (Mathf.Sin(p5) > 0 ? 1f : -1f) * 0.18f * (1f - u); });
            float p6 = 0f;
            Bonus = Make("fl_bonus", 0.7f, (t, u) => { float f = t < 0.1f ? 523f : t < 0.2f ? 659f : t < 0.3f ? 784f : t < 0.4f ? 1046f : 1318f; p6 += 2f * Mathf.PI * f / SR; return (Mathf.Sin(p6) > 0 ? 1f : -1f) * 0.15f * (1f - u * 0.6f); });
            float p7 = 0f;
            Start = Make("fl_start", 0.4f, (t, u) => { float f = t < 0.1f ? 330f : t < 0.2f ? 440f : 660f; p7 += 2f * Mathf.PI * f / SR; return (Mathf.Sin(p7) > 0 ? 1f : -1f) * 0.14f * (1f - u * 0.5f); });
            float p8 = 0f, l8 = 0f;
            Sling = Make("fl_sling", 0.12f, (t, u) => { p8 += 2f * Mathf.PI * Mathf.Lerp(260f, 150f, u) / SR; l8 += (Noise() - l8) * 0.3f; return (Mathf.Sin(p8) * 0.4f + l8 * 0.3f) * Mathf.Exp(-t * 24f); });
        }
    }

    // ---------------------------------------------------------------- pöydän rakenne (koordinaatit pikseleinä, pöytä x -95..95, y -150..150)
    const float R = 5f;                 // pallon säde
    const float HalfH = 150f;
    const float Grav = 300f, MaxSpeed = 720f;
    const int Sub = 12;
    static readonly Vector2 LaneRest = new Vector2(87.5f, -128f);
    static readonly Vector2 PivotL = new Vector2(-41.5f, -112f), PivotR = new Vector2(26.5f, -112f);
    const float FlipLen = 30f, RestAng = -30f, UpAng = 28f, UpSpeed = 17f, DownSpeed = 11f;
    const float DrainY = -150f;

    class Seg
    {
        public Vector2 a, b, n;       // n = reunan ulospäin (pallon puolelle) osoittava normaali yksipuoliselle ja kimpoaville
        public float e = 0.45f, kick;
        public bool oneWay;
        public int target = -1;       // >= 0: maalitaulu
        public bool sling;
        public float flash;
        public SpriteRenderer vis;
    }
    class Bumper { public Vector2 p; public float r; public Color c; public SpriteRenderer ring, glow; public float flash, kick = 140f; public int score = 100; }
    class Pop { public Vector2 p; public string t; public float life; public Color c; }
    class Part { public Transform tr; public SpriteRenderer sr; public Vector2 v; public float life, max; public Color c; }
    class Light { public Vector2 p; public bool lit; public SpriteRenderer sr; }
    class Flip { public Vector2 pivot; public bool right; public float ang = RestAng, prevAng = RestAng, omega; public Transform tr; public bool held, wasHeld; }

    readonly List<Seg> segs = new List<Seg>();
    readonly List<Bumper> bumpers = new List<Bumper>();
    readonly List<Light> roll = new List<Light>();
    readonly List<SpriteRenderer> targetVis = new List<SpriteRenderer>();
    readonly bool[] targetLit = new bool[6];          // 0-2 vasen ryhmä, 3-5 oikea ryhmä
    readonly List<Pop> pops = new List<Pop>();
    readonly List<Part> parts = new List<Part>();
    readonly List<SpriteRenderer> chase = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> chevrons = new List<SpriteRenderer>();
    readonly List<SpriteRenderer> trail = new List<SpriteRenderer>();
    readonly Vector2[] trailPos = new Vector2[8];
    Flip fl, fr;

    // ---------------------------------------------------------------- tila
    enum St { Title, Play, Over }
    St st = St.Title;
    float stT, openedAt;
    PlayerController pc; int pi;
    Transform root; Camera cam; AudioSource sfx;
    bool prevListenerPause; float prevTimeScale = 1f;
    GUIStyle gs, big, mid;

    Vector2 bp, bv; Transform ballTr;
    bool ballInPlay;          // false = odottaa laukaisua
    float charge, plungerY, sinceLaunch, drainT, stuckT, lastSpeed;
    bool saveUsed, draining;
    int balls, score, hiScore, mult = 1, rollLit;
    float bonusFlash; string bonusText; readonly float[] targetResetT = new float[2];
    SpriteRenderer plungerVis;

    void Init(PlayerController who)
    {
        Instance = this; pc = who; pi = who.playerIndex; openedAt = Time.unscaledTime;
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        var rg = new GameObject("Juuri"); rg.transform.SetParent(transform, false); rg.transform.position = new Vector3(0f, 6000f, 0f); root = rg.transform;
        var cg = new GameObject("Kamera"); cg.transform.SetParent(transform, false); cg.transform.position = new Vector3(0f, 6000f, -10f);
        cam = cg.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = HalfH; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
        cam.depth = 99f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
        sfx = gameObject.AddComponent<AudioSource>(); sfx.spatialBlend = 0f; sfx.playOnAwake = false; sfx.ignoreListenerPause = true;
        prevListenerPause = AudioListener.pause; AudioListener.pause = true;
        prevTimeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f; Time.timeScale = 0f;
        Sfx.Build();
        hiScore = PlayerPrefs.GetInt(HiKey, 0);
        BuildTable();
        st = St.Title; stT = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; AudioListener.pause = prevListenerPause; Time.timeScale = prevTimeScale; }
        foreach (var p in Players.All) if (p != null) p.enabled = true;
    }

    void End() { Destroy(gameObject); }

    void Play(AudioClip c, float vol = 1f, float pitch = 1f)
    {
        if (c == null) return;
        sfx.pitch = pitch; sfx.PlayOneShot(c, vol);
    }

    // ---------------------------------------------------------------- rakennus
    SpriteRenderer Quad(Vector2 p, float w, float h, Color c, int order, float rotDeg = 0f, string name = "r", Transform parent = null)
    {
        var go = new GameObject(name); go.transform.SetParent(parent != null ? parent : root, false);
        go.transform.localPosition = new Vector3(p.x, p.y, 0f); go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotDeg);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = White(); sr.color = c; sr.sortingOrder = order;
        return sr;
    }

    SpriteRenderer Line(Vector2 a, Vector2 b, float w, Color c, int order)
    {
        Vector2 d = b - a;
        return Quad((a + b) * 0.5f, d.magnitude, w, c, order, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, "viiva");
    }

    Seg AddSeg(Vector2 a, Vector2 b, Color col, float width = 2.5f, float e = 0.45f, bool draw = true, int order = -40)
    {
        var s = new Seg { a = a, b = b, e = e };
        Vector2 d = (b - a).normalized; s.n = new Vector2(-d.y, d.x);
        if (draw) { if (width >= 2.5f && e < 0.55f) Line(a, b, width * 3.2f, new Color(col.r, col.g, col.b, 0.18f), order - 1); s.vis = Line(a, b, width, col, order); }
        segs.Add(s);
        return s;
    }

    /// Normaali osoittamaan pisteen towards suuntaan.
    static void FaceNormal(Seg s, Vector2 towards)
    {
        if (Vector2.Dot(towards - (s.a + s.b) * 0.5f, s.n) < 0f) s.n = -s.n;
    }

    void BuildTable()
    {
        var bgSp = Img("pohja", 190f, new Vector2(0.5f, 0.5f));
        Color wall = new Color(0.35f, 0.9f, 1f);
        if (bgSp != null)
        {
            var go = new GameObject("pohja"); go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = bgSp; sr.sortingOrder = -100;
            go.transform.localScale = new Vector3(1f, 1f, 1f);
            // kuvan korkeudeksi 300: skaalataan tarvittaessa
            float h = bgSp.rect.height / bgSp.pixelsPerUnit;
            go.transform.localScale = new Vector3(1f, 300f / h, 1f);
        }
        else
        {
            Quad(new Vector2(0, 0), 190f, 300f, new Color(0.06f, 0.03f, 0.16f), -100, 0f, "pohja");
            for (int i = -9; i <= 9; i++) Quad(new Vector2(i * 10f, 0f), 0.6f, 300f, new Color(0.15f, 0.08f, 0.35f, 0.8f), -99, 0f, "ruudukko");
            for (int j = -14; j <= 14; j++) Quad(new Vector2(0f, j * 10f), 190f, 0.6f, new Color(0.15f, 0.08f, 0.35f, 0.8f), -99, 0f, "ruudukko");
            var ring = Circle(46, new Color(0.2f, 0.07f, 0.4f, 0.55f));
            var rg = new GameObject("rengas"); rg.transform.SetParent(root, false); rg.transform.localPosition = new Vector3(-8f, 10f, 0f);
            var rs = rg.AddComponent<SpriteRenderer>(); rs.sprite = ring; rs.sortingOrder = -98;
        }
        // ulkopuoli peitetään
        Quad(new Vector2(-190f, 0f), 190f, 320f, Color.black, 50, 0f, "reuna"); Quad(new Vector2(190f, 0f), 190f, 320f, Color.black, 50, 0f, "reuna");

        // kaari ylhäällä (keskipiste (0,45), säde 95)
        Vector2 c0 = new Vector2(0f, 45f);
        Vector2 prev = new Vector2(95f, 45f);
        const int N = 28;
        for (int i = 1; i <= N; i++)
        {
            float ang = Mathf.PI * i / N;
            Vector2 q = c0 + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 95f;
            var s = AddSeg(prev, q, wall, 3f, 0.5f); FaceNormal(s, c0);
            prev = q;
        }
        var lw = AddSeg(new Vector2(-95f, 45f), new Vector2(-95f, -70f), wall); FaceNormal(lw, new Vector2(0f, 0f));
        var rw = AddSeg(new Vector2(95f, 45f), new Vector2(95f, -142f), wall); FaceNormal(rw, new Vector2(0f, 0f));
        var lane = AddSeg(new Vector2(80f, 70f), new Vector2(80f, -135f), wall); FaceNormal(lane, new Vector2(87.5f, 0f));
        var gate = AddSeg(new Vector2(80f, 70f), new Vector2(80f, 98f), new Color(1f, 0.8f, 0.2f), 2f, 0.4f);
        gate.oneWay = true; gate.n = new Vector2(-1f, 0f);   // päästää vasemmalle, estää oikealle
        AddSeg(new Vector2(80f, -142f), new Vector2(95f, -142f), wall, 3f, 0.3f);
        // alarinteet maileille
        var sl = AddSeg(new Vector2(-95f, -70f), new Vector2(PivotL.x + 2.4f, PivotL.y + 5.2f), wall); FaceNormal(sl, new Vector2(-40f, -40f));
        var sr2 = AddSeg(new Vector2(80f, -70f), new Vector2(PivotR.x - 2.4f, PivotR.y + 5.2f), wall); FaceNormal(sr2, new Vector2(30f, -40f));
        // linkoparit (potkaisevat)
        var sg1 = AddSeg(new Vector2(-72f, -44f), new Vector2(-52f, -84f), new Color(1f, 0.35f, 0.6f), 3.5f, 0.6f); sg1.sling = true; sg1.kick = 150f; FaceNormal(sg1, new Vector2(0f, -30f));
        var sg2 = AddSeg(new Vector2(60f, -44f), new Vector2(40f, -84f), new Color(1f, 0.35f, 0.6f), 3.5f, 0.6f); sg2.sling = true; sg2.kick = 150f; FaceNormal(sg2, new Vector2(0f, -30f));
        FillTri(new Vector2(-72f, -44f), new Vector2(-52f, -84f), new Vector2(-72f, -84f), new Color(1f, 0.35f, 0.6f, 0.22f), -43);
        FillTri(new Vector2(60f, -44f), new Vector2(40f, -84f), new Vector2(60f, -84f), new Color(1f, 0.35f, 0.6f, 0.22f), -43);
        // linkoparin takaosa (kolmio): reunaviivat
        Line(new Vector2(-72f, -44f), new Vector2(-72f, -84f), 2.5f, new Color(1f, 0.35f, 0.6f, 0.6f), -41); Line(new Vector2(-72f, -84f), new Vector2(-52f, -84f), 2.5f, new Color(1f, 0.35f, 0.6f, 0.6f), -41);
        Line(new Vector2(60f, -44f), new Vector2(60f, -84f), 2.5f, new Color(1f, 0.35f, 0.6f, 0.6f), -41); Line(new Vector2(60f, -84f), new Vector2(40f, -84f), 2.5f, new Color(1f, 0.35f, 0.6f, 0.6f), -41);
        // maalitaulut: vasen ryhmä (0-2) ja oikea ryhmä (3-5), symmetriset
        for (int i = 0; i < 6; i++)
        {
            bool rightSide = i >= 3;
            float y0 = -10f + (i % 3) * 26f;
            float x = rightSide ? 77f : -91f;
            var t = AddSeg(new Vector2(x, y0), new Vector2(x, y0 + 16f), new Color(1f, 0.85f, 0.1f), 4f, 0.5f, true, -39);
            t.n = new Vector2(rightSide ? -1f : 1f, 0f); t.target = i; targetVis.Add(t.vis);
        }
        // pomppijat: kolmio keskellä (pelikentän keskus x = -7.5) ja kaksi pientä ylhäällä
        const float cx = -7.5f;
        AddBumper(new Vector2(cx, 64f), 10f, new Color(1f, 0.3f, 0.3f), 2);
        AddBumper(new Vector2(cx - 34f, 36f), 10f, new Color(0.3f, 0.55f, 1f), 0);
        AddBumper(new Vector2(cx + 34f, 36f), 10f, new Color(0.3f, 1f, 0.5f), 1);
        AddBumper(new Vector2(cx - 62f, 72f), 7f, new Color(0.3f, 1f, 0.5f), 1, 120f, 50);
        AddBumper(new Vector2(cx + 62f, 72f), 7f, new Color(0.3f, 0.55f, 1f), 0, 120f, 50);
        // tapit: pienet kimpoavat pylväät pöydän alaosassa (kevyt potku, vähän pisteitä)
        foreach (var pg in new[] { new Vector2(cx - 50f, 4f), new Vector2(cx + 50f, 4f), new Vector2(cx - 24f, -16f), new Vector2(cx + 24f, -16f), new Vector2(cx, 6f) })
            AddBumper(pg, 3.3f, new Color(0.8f, 0.9f, 1f), -1, 60f, 10);
        // ohitusvalot ylhäällä
        float[][] rl = { new[] { cx - 30f, 104f }, new[] { cx, 112f }, new[] { cx + 30f, 104f } };
        foreach (var q in rl)
        {
            var l = new Light { p = new Vector2(q[0], q[1]) };
            var go = new GameObject("valo"); go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(l.p.x, l.p.y, 0f);
            var ro = go.AddComponent<SpriteRenderer>(); ro.sprite = Circle(7, new Color(0.35f, 0.9f, 1f)); ro.sortingOrder = -39;
            var inner = new GameObject("sisä"); inner.transform.SetParent(go.transform, false);
            l.sr = inner.AddComponent<SpriteRenderer>(); l.sr.sprite = Circle(5, Color.white); l.sr.sortingOrder = -38; l.sr.color = new Color(0.12f, 0.1f, 0.25f);
            roll.Add(l);
        }
        // kaaren juoksuvalot ja kaistan nuolet (koriste)
        for (int i = 0; i < 17; i++)
        {
            float ang = Mathf.PI * (0.1f + 0.8f * i / 16f);
            var go = new GameObject("juoksuvalo"); go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(ang) * 86f, 45f + Mathf.Sin(ang) * 86f, 0f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Circle(2, Color.white); sr.sortingOrder = -38; sr.color = new Color(1f, 0.4f, 0.8f, 0.3f);
            chase.Add(sr);
        }
        for (int i = 0; i < 6; i++)
        {
            float y = -95f + i * 25f;
            chevrons.Add(Line(new Vector2(83f, y), new Vector2(87.5f, y + 4f), 1.6f, new Color(0.4f, 0.9f, 1f, 0.3f), -38));
            chevrons.Add(Line(new Vector2(92f, y), new Vector2(87.5f, y + 4f), 1.6f, new Color(0.4f, 0.9f, 1f, 0.3f), -38));
        }
        // maila
        fl = MakeFlipper(PivotL, false); fr = MakeFlipper(PivotR, true);
        // sulkutanko ja pallo
        plungerVis = Quad(new Vector2(87.5f, -139f), 11f, 6f, new Color(0.8f, 0.8f, 0.85f), -30, 0f, "mäntä");
        var bgo = new GameObject("pallo"); bgo.transform.SetParent(root, false); ballTr = bgo.transform;
        var bs = bgo.AddComponent<SpriteRenderer>(); bs.sortingOrder = 20;
        var ballImg = Img("pallo", R * 2f, new Vector2(0.5f, 0.5f));
        bs.sprite = ballImg != null ? ballImg : Circle(5, new Color(0.82f, 0.85f, 0.92f));
        if (ballImg == null)
        {
            var hl = new GameObject("kiilto"); hl.transform.SetParent(bgo.transform, false); hl.transform.localPosition = new Vector3(-1.5f, 1.5f, 0f);
            var hs = hl.AddComponent<SpriteRenderer>(); hs.sprite = Circle(2, Color.white); hs.sortingOrder = 21;
        }
        for (int i = 0; i < trailPos.Length; i++)
        {
            var tg = new GameObject("jälki"); tg.transform.SetParent(root, false);
            var tsr = tg.AddComponent<SpriteRenderer>(); tsr.sprite = Circle(5, new Color(0.7f, 0.85f, 1f)); tsr.sortingOrder = 18;
            trail.Add(tsr);
        }
        ResetBall();
    }

    /// Kolmion täyttö ohuilla viivoilla (ei erillistä varjostinta).
    void FillTri(Vector2 a, Vector2 b, Vector2 c, Color col, int order)
    {
        int n = Mathf.CeilToInt(Mathf.Max((b - a).magnitude, (c - a).magnitude) / 2f);
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            Line(Vector2.Lerp(a, b, t), Vector2.Lerp(a, c, t), 2.4f, col, order);
        }
    }

    void AddBumper(Vector2 p, float r, Color c, int idx, float kick = 140f, int score = 100)
    {
        var b = new Bumper { p = p, r = r, c = c, kick = kick, score = score };
        var img = idx >= 0 ? Img("pompp_" + idx, r * 2f, new Vector2(0.5f, 0.5f)) : null;
        var go = new GameObject("pomppija"); go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(p.x, p.y, 0f);
        b.ring = go.AddComponent<SpriteRenderer>(); b.ring.sortingOrder = -37;
        if (img != null) b.ring.sprite = img;
        else if (idx < 0)
        {
            b.ring.sprite = Circle(Mathf.CeilToInt(r), new Color(0.75f, 0.82f, 0.95f));
            var pin = new GameObject("sisä"); pin.transform.SetParent(go.transform, false);
            var psr = pin.AddComponent<SpriteRenderer>(); psr.sprite = Circle(Mathf.CeilToInt(r) - 1, new Color(0.3f, 0.35f, 0.6f)); psr.sortingOrder = -36;
        }
        else
        {
            b.ring.sprite = Circle((int)r, new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f));
            var inner = new GameObject("sisä"); inner.transform.SetParent(go.transform, false);
            var isr = inner.AddComponent<SpriteRenderer>(); isr.sprite = Circle((int)r - 3, c); isr.sortingOrder = -36;
            var cap = new GameObject("kansi"); cap.transform.SetParent(go.transform, false);
            var csr = cap.AddComponent<SpriteRenderer>(); csr.sprite = Circle((int)r - 6, Color.white); csr.sortingOrder = -35; csr.color = new Color(1f, 1f, 1f, 0.85f);
        }
        var gl = new GameObject("hohto"); gl.transform.SetParent(go.transform, false);
        b.glow = gl.AddComponent<SpriteRenderer>(); b.glow.sprite = Circle(Mathf.CeilToInt(r) + 7, new Color(c.r, c.g, c.b, 0.35f)); b.glow.sortingOrder = -42; b.glow.enabled = false;
        bumpers.Add(b);
    }

    Flip MakeFlipper(Vector2 pivot, bool right)
    {
        var f = new Flip { pivot = pivot, right = right };
        var go = new GameObject(right ? "maila oikea" : "maila vasen"); go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(pivot.x, pivot.y, 0f);
        f.tr = go.transform;
        var img = Img("maila", FlipLen + 8f, new Vector2(0.118f, 0.533f));   // pyörähdyspiste kuvan nupin keskellä
        if (img != null)
        {
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = img; sr.sortingOrder = 5;
            if (right) go.transform.localScale = new Vector3(1f, -1f, 1f);   // kuva on vasenta mailaa varten; pystypeilaus + 180 asteen kierto = vaakapeilaus
        }
        else
        {
            var body = Quad(new Vector2(FlipLen * 0.5f, 0f), FlipLen, 7f, new Color(1f, 0.85f, 0.2f), 5, 0f, "runko", go.transform);
            Quad(new Vector2(FlipLen * 0.5f, 0f), FlipLen - 3f, 3.5f, new Color(1f, 0.97f, 0.6f), 6, 0f, "kiilto", go.transform);
            var tip = new GameObject("kärki"); tip.transform.SetParent(go.transform, false); tip.transform.localPosition = new Vector3(FlipLen, 0f, 0f);
            var ts = tip.AddComponent<SpriteRenderer>(); ts.sprite = Circle(3, new Color(1f, 0.85f, 0.2f)); ts.sortingOrder = 5;
            var pv = new GameObject("napa"); pv.transform.SetParent(go.transform, false);
            var ps = pv.AddComponent<SpriteRenderer>(); ps.sprite = Circle(5, new Color(1f, 0.85f, 0.2f)); ps.sortingOrder = 5;
        }
        ApplyFlipper(f);
        return f;
    }

    void ApplyFlipper(Flip f)
    {
        float z = f.right ? 180f - f.ang : f.ang;
        f.tr.localRotation = Quaternion.Euler(0f, 0f, z);
    }

    // ---------------------------------------------------------------- pallon hallinta
    void ResetBall()
    {
        ballInPlay = false; draining = false; sinceLaunch = 0f; stuckT = 0f; charge = 0f; saveUsed = false;
        bp = LaneRest; bv = Vector2.zero;
        ballTr.position = root.position + new Vector3(bp.x, bp.y, 0f);
        ballTr.gameObject.SetActive(true);
    }

    void StartGame()
    {
        balls = 3; score = 0; mult = 1; rollLit = 0; targetResetT[0] = targetResetT[1] = 0f;
        for (int i = 0; i < 6; i++) targetLit[i] = false;
        foreach (var l in roll) l.lit = false;
        st = St.Play; stT = 0f;
        Play(Sfx.Start, 0.9f);
        ResetBall();
    }

    // ---------------------------------------------------------------- päivitys
    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        stT += dt;
        var k = UnityEngine.InputSystem.Keyboard.current;
        bool esc = (k != null && (k.escapeKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame)) || CoopInput.StartPressed(pi);
        UpdateVisuals(dt);
        if (Time.unscaledTime - openedAt < 0.35f) return;
        if (st == St.Title)
        {
            if (esc || CoopInput.Kick(pi)) { End(); return; }
            if (CoopInput.Punch(pi) || CoopInput.Pick(pi) || CoopInput.JumpHeld(pi)) StartGame();
            return;
        }
        if (st == St.Over)
        {
            if (esc || CoopInput.Kick(pi)) { End(); return; }
            if (stT > 1.2f && (CoopInput.Punch(pi) || CoopInput.Pick(pi))) StartGame();
            return;
        }
        if (esc) { End(); return; }
        UpdateFlippers(dt);
        if (draining)
        {
            drainT -= dt;
            if (drainT <= 0f)
            {
                balls--;
                if (balls <= 0) { st = St.Over; stT = 0f; Hi(); }
                else ResetBall();
            }
            return;
        }
        if (!ballInPlay) UpdatePlunger(dt);
        else UpdateBall(dt);
    }

    void UpdateFlippers(float dt)
    {
        Vector2 m = CoopInput.Move(pi);
        fl.held = m.x < -0.4f || CoopInput.Block(pi);          // vasen maila: torjunta-nappi
        fr.held = m.x > 0.4f || CoopInput.Run(pi);             // oikea maila: juoksu-nappi
        if ((fl.held && !fl.wasHeld) || (fr.held && !fr.wasHeld)) Play(Sfx.Click, 0.7f, Random.Range(0.9f, 1.1f));
        fl.wasHeld = fl.held; fr.wasHeld = fr.held;
        if (!ballInPlay || draining) { StepFlipper(fl, dt); StepFlipper(fr, dt); }   // pelissä mailat liikkuvat alaskelien mukana
    }

    void StepFlipper(Flip f, float h)
    {
        f.prevAng = f.ang;
        float target = f.held ? UpAng : RestAng;
        f.ang = Mathf.MoveTowards(f.ang, target, (f.held ? UpSpeed : DownSpeed) * Mathf.Rad2Deg * h);
        f.omega = h > 0f ? (f.ang - f.prevAng) * Mathf.Deg2Rad / h : 0f;
        ApplyFlipper(f);
    }

    void UpdatePlunger(float dt)
    {
        bool held = CoopInput.JumpHeld(pi);
        if (held) { charge = Mathf.Min(1f, charge + dt * 1.1f); }
        else if (charge > 0.05f)
        {
            bv = new Vector2(0f, 400f + 330f * charge);
            ballInPlay = true; sinceLaunch = 0f; stuckT = 0f;
            Play(Sfx.Launch, 0.9f, 0.8f + 0.5f * charge);
            charge = 0f;
        }
        else charge = 0f;
        plungerY = -139f - 8f * charge;
        plungerVis.transform.localPosition = new Vector3(87.5f, plungerY + 0f, 0f);
        bp = LaneRest + new Vector2(0f, -8f * charge);
        ballTr.position = root.position + new Vector3(bp.x, bp.y, 0f);
    }

    void UpdateBall(float dt)
    {
        sinceLaunch += dt;
        float h = dt / Sub;
        for (int i = 0; i < Sub; i++)
        {
            StepFlipper(fl, h); StepFlipper(fr, h);
            bv.y -= Grav * h;
            float sp = bv.magnitude; if (sp > MaxSpeed) bv *= MaxSpeed / sp;
            bp += bv * h;
            foreach (var s in segs) CollideSeg(s);
            CollideFlipper(fl); CollideFlipper(fr);
            foreach (var b in bumpers) CollideBumper(b);
        }
        // ohitusvalot
        foreach (var l in roll)
            if (!l.lit && (bp - l.p).sqrMagnitude < 100f) LightRoll(l);
        ballTr.position = root.position + new Vector3(bp.x, bp.y, 0f);
        // jumissa: hieman töytäisyä
        if (bv.sqrMagnitude < 400f && bp.x < 78f) { stuckT += dt; if (stuckT > 1.2f) { bv += new Vector2(Random.Range(-70f, 70f), 140f); stuckT = 0f; } } else stuckT = 0f;
        // lähtökaista takaisin alas (ei jäädä kaistalle)
        if (bp.x > 80f && bp.y < -100f && bv.y <= 0f && sinceLaunch > 1f && bv.sqrMagnitude < 4f) { ballInPlay = false; }
        // tyhjennys
        if (bp.y < DrainY)
        {
            if (!saveUsed && sinceLaunch < 6f) { saveUsed = true; ballTr.gameObject.SetActive(false); draining = true; drainT = 0.5f; balls++; bonusText = Loc.T("PALLONSÄÄSTÖ"); bonusFlash = 1.5f; }
            else
            {
                int bonus = 100 * mult * (rollLit + 1);
                score += bonus; bonusText = Loc.T("BONUS") + " " + bonus; bonusFlash = 1.6f;
                ballTr.gameObject.SetActive(false); draining = true; drainT = 1.4f;
                Play(Sfx.Drain, 0.9f);
                mult = 1;
            }
        }
        for (int g = 0; g < 2; g++)
            if (targetResetT[g] > 0f) { targetResetT[g] -= dt; if (targetResetT[g] <= 0f) for (int i = g * 3; i < g * 3 + 3; i++) targetLit[i] = false; }
    }

    // ---------------------------------------------------------------- törmäykset
    void CollideSeg(Seg s)
    {
        Vector2 ab = s.b - s.a;
        float len2 = ab.sqrMagnitude;
        float t = len2 > 0f ? Mathf.Clamp01(Vector2.Dot(bp - s.a, ab) / len2) : 0f;
        Vector2 c = s.a + ab * t;
        Vector2 d = bp - c;
        float dist = d.magnitude;
        if (dist >= R) return;
        Vector2 n = dist > 1e-4f ? d / dist : s.n;
        if (s.oneWay)
        {
            if (Vector2.Dot(bv, s.n) >= 0f) return;      // liikkuu normaalin suuntaan: läpi
            n = s.n;
        }
        bp = c + n * R;
        float vn = Vector2.Dot(bv, n);
        if (vn < 0f)
        {
            bv -= (1f + s.e) * vn * n;
            if (s.sling && Vector2.Dot(n, s.n) > 0.3f)
            {
                bv += s.n * s.kick;
                Add(10, (s.a + s.b) * 0.5f + s.n * 8f); Burst((s.a + s.b) * 0.5f, new Color(1f, 0.5f, 0.7f), 4); Play(Sfx.Sling, 0.7f, Random.Range(0.95f, 1.1f)); s.flash = 0.15f;
            }
            else if (s.target >= 0 && !targetLit[s.target]) HitTarget(s.target);
            else if (-vn > 60f) Play(Sfx.Click, Mathf.Clamp01(-vn / 400f) * 0.35f, Random.Range(0.8f, 1.2f));
        }
    }

    void CollideFlipper(Flip f)
    {
        float angRad = f.ang * Mathf.Deg2Rad;
        Vector2 dir = f.right ? new Vector2(-Mathf.Cos(angRad), Mathf.Sin(angRad)) : new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
        Vector2 dirPrime = f.right ? new Vector2(Mathf.Sin(angRad), Mathf.Cos(angRad)) : new Vector2(-Mathf.Sin(angRad), Mathf.Cos(angRad));
        float sPar = Mathf.Clamp(Vector2.Dot(bp - f.pivot, dir), 0f, FlipLen);
        Vector2 c = f.pivot + dir * sPar;
        Vector2 d = bp - c;
        float dist = d.magnitude;
        float th = Mathf.Lerp(4.8f, 2.4f, sPar / FlipLen);
        if (dist >= R + th) return;
        Vector2 n = dist > 1e-4f ? d / dist : Vector2.up;
        bp = c + n * (R + th);
        Vector2 vs = dirPrime * (f.omega * sPar);   // f.omega on rad/s
        Vector2 vr = bv - vs;
        float vn = Vector2.Dot(vr, n);
        if (vn < 0f)
        {
            vr -= (1f + 0.3f) * vn * n;
            bv = vr + vs;
            if (-vn > 80f) Play(Sfx.Click, Mathf.Clamp01(-vn / 500f) * 0.5f, Random.Range(0.9f, 1.15f));
        }
    }

    void CollideBumper(Bumper b)
    {
        Vector2 d = bp - b.p;
        float dist = d.magnitude;
        if (dist >= R + b.r) return;
        Vector2 n = dist > 1e-4f ? d / dist : Vector2.up;
        bp = b.p + n * (R + b.r);
        float vn = Vector2.Dot(bv, n);
        if (vn < 0f) bv -= 2f * vn * n;
        bv += n * b.kick;
        b.flash = 0.18f;
        Add(b.score, b.p + new Vector2(0f, b.r + 6f));
        Burst(b.p + n * b.r, b.c, b.r > 5f ? 7 : 3);
        shake = Mathf.Max(shake, b.r > 5f ? 1f : 0f);
        Play(Sfx.Bonk, 0.8f, Random.Range(0.9f, 1.2f));
    }

    // ---------------------------------------------------------------- pisteet
    float shake;
    void Add(int pts, Vector2 at)
    {
        int v = pts * mult; score += v;
        pops.Add(new Pop { p = at, t = v.ToString(), life = 0.8f, c = pts >= 250 ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 1f, 1f) });
    }

    void Burst(Vector2 p, Color c, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("kipinä"); go.transform.SetParent(root, false); go.transform.localPosition = new Vector3(p.x, p.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Circle(1, Color.white); sr.sortingOrder = 25; sr.color = c;
            float ang = Random.Range(0f, Mathf.PI * 2f), sp = Random.Range(30f, 110f);
            parts.Add(new Part { tr = go.transform, sr = sr, v = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp, life = 0.35f, max = 0.35f, c = c });
        }
    }

    void HitTarget(int i)
    {
        targetLit[i] = true;
        int g = i / 3;
        Vector2 at = new Vector2(g == 0 ? -76f : 62f, -10f + (i % 3) * 26f + 8f);
        Add(250, at); Burst(at, new Color(1f, 0.9f, 0.3f), 6); Play(Sfx.Ping, 0.8f, 1f + (i % 3) * 0.2f);
        if (targetLit[g * 3] && targetLit[g * 3 + 1] && targetLit[g * 3 + 2])
        {
            mult = Mathf.Min(5, mult + 1);
            score += 1000; bonusText = Loc.T("KERROIN") + " x" + mult; bonusFlash = 1.5f;
            Play(Sfx.Bonus, 0.8f);
            targetResetT[g] = 0.8f;
        }
    }

    void LightRoll(Light l)
    {
        l.lit = true; rollLit++;
        Add(100, l.p + new Vector2(0f, 10f)); Burst(l.p, new Color(1f, 0.95f, 0.3f), 5); Play(Sfx.Ping, 0.6f, 1.4f);
        if (rollLit >= roll.Count)
        {
            score += 500 * mult; bonusText = Loc.T("VALOT") + " +" + 500 * mult; bonusFlash = 1.3f;
            Play(Sfx.Bonus, 0.7f, 1.2f);
            rollLit = 0; foreach (var r in roll) r.lit = false;
        }
    }

    void Hi() { if (score > hiScore) { hiScore = score; PlayerPrefs.SetInt(HiKey, hiScore); PlayerPrefs.Save(); } }

    // ---------------------------------------------------------------- näkymä
    void UpdateVisuals(float dt)
    {
        foreach (var b in bumpers)
        {
            b.flash = Mathf.Max(0f, b.flash - dt);
            b.glow.enabled = b.flash > 0f;
            b.ring.transform.localScale = Vector3.one * (1f + b.flash * 0.6f);
        }
        foreach (var s in segs)
        {
            s.flash = Mathf.Max(0f, s.flash - dt);
            if (s.vis != null && s.sling) s.vis.color = s.flash > 0f ? Color.white : new Color(1f, 0.35f, 0.6f);
        }
        for (int i = 0; i < targetVis.Count; i++)
            if (targetVis[i] != null) targetVis[i].color = targetLit[i] ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.85f, 0.1f);
        foreach (var l in roll) l.sr.color = l.lit ? new Color(1f, 0.95f, 0.3f) : new Color(0.12f, 0.1f, 0.25f);
        bonusFlash = Mathf.Max(0f, bonusFlash - dt);
        float tm = Time.unscaledTime;
        for (int i = 0; i < chase.Count; i++)
        {
            float k = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(tm * 5f - i * 0.6f));
            chase[i].color = new Color(1f, 0.4f + 0.4f * k, 0.8f, 0.15f + 0.75f * k * k);
        }
        for (int i = 0; i < chevrons.Count; i++)
        {
            float k = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(tm * 6f - (i / 2) * 1.0f));
            chevrons[i].color = new Color(0.4f, 0.9f, 1f, 0.12f + 0.6f * k);
        }
        // pallon jälki
        for (int i = trailPos.Length - 1; i > 0; i--) trailPos[i] = trailPos[i - 1];
        trailPos[0] = bp;
        bool show = st == St.Play && ballInPlay && !draining;
        for (int i = 0; i < trail.Count; i++)
        {
            Vector2 tp = trailPos[Mathf.Min(i + 1, trailPos.Length - 1)];
            trail[i].enabled = show && (bp - tp).sqrMagnitude > 2f;
            if (!trail[i].enabled) continue;
            trail[i].transform.localPosition = new Vector3(tp.x, tp.y, 0f);
            trail[i].transform.localScale = Vector3.one * (1f - (i + 1) * 0.1f);
            trail[i].color = new Color(0.7f, 0.85f, 1f, 0.28f * (1f - i / (float)trail.Count));
        }
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var q = parts[i]; q.life -= dt;
            if (q.life <= 0f || q.tr == null) { if (q.tr != null) Destroy(q.tr.gameObject); parts.RemoveAt(i); continue; }
            q.tr.localPosition += (Vector3)(q.v * dt); q.v *= 1f - dt * 3f;
            q.sr.color = new Color(q.c.r, q.c.g, q.c.b, q.life / q.max);
        }
        for (int i = pops.Count - 1; i >= 0; i--) { pops[i].life -= dt; if (pops[i].life <= 0f) pops.RemoveAt(i); }
        cam.transform.position = new Vector3(0f, 6000f, -10f) + (shake > 0.01f ? (Vector3)(Random.insideUnitCircle * shake * 0.6f) : Vector3.zero);
        shake = Mathf.Max(0f, shake - dt * 8f);
    }

    void OnGUI()
    {
        float H = Screen.height, W = Screen.width, s = H / 1080f;
        if (gs == null || Mathf.Abs(gs.fontSize - Mathf.RoundToInt(34 * s)) > 1)
        {
            gs = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * s), fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow, alignment = TextAnchor.UpperLeft };
            gs.normal.textColor = Color.white;
            mid = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(44 * s) };
            big = new GUIStyle(gs) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(100 * s) };
        }
        var white = Texture2D.whiteTexture;
        // pöydän ulkopuolinen alue pimennetään ja tiedot sivuille
        float tableW = H * 190f / 300f, tx = (W - tableW) * 0.5f;
        GUI.color = new Color(0.02f, 0.01f, 0.05f, 1f);
        GUI.DrawTexture(new Rect(0, 0, tx, H), white); GUI.DrawTexture(new Rect(tx + tableW, 0, W - tx - tableW, H), white);
        GUI.color = Color.white;
        gs.fontSize = Mathf.RoundToInt(26 * s); gs.alignment = TextAnchor.UpperLeft;
        float lx = Mathf.Max(12f * s, tx - W * 0.2f), lw = Mathf.Min(W * 0.2f, tx - 12f * s);
        gs.normal.textColor = new Color(0.4f, 0.9f, 1f);
        if (lw > 100f * s)
        {
            GUI.Label(new Rect(lx, H * 0.08f, lw, 40 * s), Loc.T("PISTEET"), gs);
            gs.fontSize = Mathf.RoundToInt(44 * s); gs.normal.textColor = Color.white;
            GUI.Label(new Rect(lx, H * 0.08f + 34 * s, lw, 60 * s), score.ToString("D7"), gs);
            gs.fontSize = Mathf.RoundToInt(26 * s); gs.normal.textColor = new Color(0.4f, 0.9f, 1f);
            GUI.Label(new Rect(lx, H * 0.08f + 110 * s, lw, 40 * s), Loc.T("PARAS") + "  " + Mathf.Max(hiScore, score).ToString("D7"), gs);
            GUI.Label(new Rect(lx, H * 0.08f + 170 * s, lw, 40 * s), Loc.T("PALLOT") + "  " + Mathf.Max(0, balls), gs);
            gs.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(lx, H * 0.08f + 215 * s, lw, 40 * s), Loc.T("KERROIN") + "  x" + mult, gs);
            float rx = tx + tableW + 16 * s, rw = W - rx - 12 * s;
            if (rw > 100f * s)
            {
                gs.fontSize = Mathf.RoundToInt(22 * s); gs.normal.textColor = new Color(0.8f, 0.8f, 0.9f);
                GUI.Label(new Rect(rx, H * 0.08f, rw, H * 0.6f), Loc.T("Vasen maila: torjunta (I) / nuoli vasen / A") + "\n\n" + Loc.T("Oikea maila: juoksu (Shift) / nuoli oikea / D") + "\n\n" + Loc.T("Laukaisu: pidä välilyönti, vapauta") + "\n\n" + Loc.T("Poistu: Esc / Start"), gs);
            }
        }
        gs.normal.textColor = Color.white; gs.alignment = TextAnchor.UpperLeft;
        if (st == St.Title)
        {
            GUI.color = new Color(0, 0, 0, 0.55f); GUI.DrawTexture(new Rect(tx, H * 0.25f, tableW, H * 0.4f), white); GUI.color = Color.white;
            big.fontSize = Mathf.RoundToInt(64 * s); big.normal.textColor = new Color(1f, 0.4f, 0.7f);
            GUI.Label(new Rect(tx, H * 0.27f, tableW, H * 0.13f), "FLIPPER\nFLAPPER", big);
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
            mid.fontSize = Mathf.RoundToInt(36 * s); mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
            GUI.Label(new Rect(tx, H * 0.50f, tableW, H * 0.07f), "[J]  " + Loc.T("ALOITA"), mid);
            mid.normal.textColor = Color.white;
            return;
        }
        if (st == St.Play)
        {
            Vector3 cp = cam.WorldToScreenPoint(root.position + new Vector3(-8f, 0f, 0f));
            big.fontSize = Mathf.RoundToInt(46 * s); big.normal.textColor = new Color(1f, 0.4f, 0.8f, 0.28f);
            GUI.Label(new Rect(cp.x - tableW * 0.5f, H - cp.y - H * 0.08f, tableW, H * 0.16f), "FLIPPER" + (char)10 + "FLAPPER", big);
            big.normal.textColor = Color.white;
        }
        foreach (var pp in pops)
        {
            Vector3 sp = cam.WorldToScreenPoint(root.position + new Vector3(pp.p.x, pp.p.y + (0.8f - pp.life) * 18f, 0f));
            gs.fontSize = Mathf.RoundToInt(24 * s); gs.alignment = TextAnchor.MiddleCenter; gs.normal.textColor = new Color(pp.c.r, pp.c.g, pp.c.b, Mathf.Clamp01(pp.life * 2.5f));
            GUI.Label(new Rect(sp.x - 60 * s, H - sp.y - 15 * s, 120 * s, 30 * s), pp.t, gs);
        }
        gs.normal.textColor = Color.white; gs.alignment = TextAnchor.UpperLeft;
        // laukaisumittari
        if (st == St.Play && !ballInPlay && !draining)
        {
            float bw = tableW * 0.5f, bx = tx + (tableW - bw) * 0.5f, by = H * 0.93f;
            GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(bx - 4, by - 4, bw + 8, 24 * s + 8), white);
            GUI.color = Color.Lerp(new Color(0.3f, 0.9f, 0.3f), new Color(1f, 0.3f, 0.2f), charge);
            GUI.DrawTexture(new Rect(bx, by, bw * charge, 24 * s), white); GUI.color = Color.white;
            gs.fontSize = Mathf.RoundToInt(22 * s); gs.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(tx, by - 34 * s, tableW, 30 * s), Loc.T("Pidä välilyöntiä ja vapauta"), gs);
            gs.alignment = TextAnchor.UpperLeft;
        }
        if (bonusFlash > 0f)
        {
            mid.fontSize = Mathf.RoundToInt(40 * s); mid.normal.textColor = new Color(1f, 0.9f, 0.3f, Mathf.Clamp01(bonusFlash * 2f));
            GUI.Label(new Rect(tx, H * 0.40f, tableW, H * 0.08f), bonusText, mid);
            mid.normal.textColor = Color.white;
        }
        if (st == St.Over)
        {
            GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(new Rect(tx, H * 0.25f, tableW, H * 0.4f), white); GUI.color = Color.white;
            big.fontSize = Mathf.RoundToInt(56 * s); big.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(tx, H * 0.27f, tableW, H * 0.12f), Loc.T("PELI OHI"), big);
            mid.fontSize = Mathf.RoundToInt(32 * s); mid.normal.textColor = Color.white;
            GUI.Label(new Rect(tx, H * 0.40f, tableW, H * 0.07f), Loc.T("PISTEET") + "  " + score.ToString("D7"), mid);
            if (score >= hiScore && score > 0) { mid.normal.textColor = new Color(1f, 0.9f, 0.3f); GUI.Label(new Rect(tx, H * 0.46f, tableW, H * 0.07f), Loc.T("UUSI ENNÄTYS!"), mid); mid.normal.textColor = Color.white; }
            if (stT > 1.2f)
            {
                float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
                mid.fontSize = Mathf.RoundToInt(28 * s); mid.normal.textColor = new Color(1f, 0.9f, 0.3f, blink);
                GUI.Label(new Rect(tx, H * 0.54f, tableW, H * 0.07f), "[J]  " + Loc.T("UUSI PELI") + "   [K]  " + Loc.T("POISTU"), mid);
                mid.normal.textColor = Color.white;
            }
        }
    }
}
