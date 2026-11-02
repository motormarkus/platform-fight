using System.Collections.Generic;
using UnityEngine;

/// <summary>Sumu ja pöly kaivoksessa ja luolassa: hidas yleisusva, matala maasumu ja leijuvat pölyhiukkaset. Kaikki piirretään kameran lapsina proseduraalisilla kuvilla,
/// joten kuvia tai scenen uudelleenrakennusta ei tarvita. Käynnistyy itse (RuntimeInitializeOnLoadMethod) ja sumenee sisään/ulos alueen mukaan. Säädöt: Preset-rivit alla.</summary>
public class AreaFog : MonoBehaviour
{
    struct Preset { public string area; public Color tint; public float haze, ground, dust; }

    // haze = yleisusvan peittävyys, ground = maasumun peittävyys, dust = pölyn näkyvyys (0–1)
    static readonly Preset[] presets =
    {
        new Preset { area = "Kaivos",          tint = new Color(0.62f, 0.68f, 0.74f), haze = 0.16f, ground = 0.30f, dust = 0.9f },
        new Preset { area = "Viidakon luola",  tint = new Color(0.55f, 0.68f, 0.70f), haze = 0.13f, ground = 0.26f, dust = 0.7f },
    };

    const int SortingOrder = 4000;      // CaveDarkness piirtää 5000:ssa, joten pimeys peittää sumunkin
    const int DustCount = 36;

    class Layer { public SpriteRenderer[] tiles = new SpriteRenderer[3]; public float speed, camParallax, offset; public bool ground; }

    static AreaFog instance;
    Camera cam;
    Transform root;
    Sprite hazeSprite, groundSprite, dotSprite;
    readonly List<Layer> layers = new List<Layer>();
    SpriteRenderer[] dust; Vector2[] dustPos, dustVel; float[] dustPhase;
    float amount; Preset cur;
    float lastW, lastH;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("Sumu"); DontDestroyOnLoad(go);
        instance = go.AddComponent<AreaFog>();
    }

    void Start()
    {
        hazeSprite = MakeNoise(128, 128, false);
        groundSprite = MakeNoise(256, 64, true);
        dotSprite = MakeDot();
    }

    void LateUpdate()
    {
        string name = Area.Current != null ? Area.Current.areaName : null;
        int idx = -1;
        for (int i = 0; i < presets.Length; i++) if (presets[i].area == name) idx = i;
        if (idx >= 0) cur = presets[idx];
        amount = Mathf.MoveTowards(amount, idx >= 0 ? 1f : 0f, Time.unscaledDeltaTime * 0.8f);

        cam = Camera.main;
        if (cam == null || amount <= 0.001f || hazeSprite == null) { if (root != null) root.gameObject.SetActive(false); return; }
        if (root == null || root.parent != cam.transform) Build();
        root.gameObject.SetActive(true);

        float h = cam.orthographicSize * 2f, w = h * cam.aspect;
        if (Mathf.Abs(w - lastW) > 0.01f || Mathf.Abs(h - lastH) > 0.01f) { Resize(w, h); lastW = w; lastH = h; }

        float t = Time.time, camX = cam.transform.position.x;
        foreach (var l in layers)
        {
            float tileW = l.tiles[0].sprite.bounds.size.x * l.tiles[0].transform.localScale.x;
            l.offset = Mathf.Repeat(t * l.speed - camX * l.camParallax, tileW);
            float a = (l.ground ? cur.ground : cur.haze) * amount * (l.ground ? 1f : 0.55f + 0.45f * Mathf.Sin(t * 0.21f + l.speed * 9f));
            var c = cur.tint; c.a = Mathf.Clamp01(a);
            for (int k = 0; k < 3; k++)
            {
                var tr = l.tiles[k].transform;
                tr.localPosition = new Vector3(-l.offset + (k - 1) * tileW + (l.ground ? 0f : 0f), tr.localPosition.y, tr.localPosition.z);
                l.tiles[k].color = c;
            }
        }

        for (int i = 0; i < DustCount; i++)
        {
            var p = dustPos[i] + dustVel[i] * t;
            p.x = Mathf.Repeat(p.x - camX * 0.05f + w * 0.5f, w) - w * 0.5f;
            p.y = Mathf.Repeat(p.y + h * 0.5f + Mathf.Sin(t * 0.7f + dustPhase[i]) * 0.15f, h) - h * 0.5f;
            dust[i].transform.localPosition = new Vector3(p.x, p.y, 0f);
            float tw = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * (0.5f + dustPhase[i] * 0.1f) + dustPhase[i]));
            var c = Color.Lerp(cur.tint, Color.white, 0.55f); c.a = 0.55f * tw * cur.dust * amount;
            dust[i].color = c;
        }
    }

    void Build()
    {
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("Sumu").transform; root.SetParent(cam.transform, false); root.localPosition = new Vector3(0f, 0f, 2f);
        layers.Clear();
        AddLayer(false, 0.25f, 0.30f, 0f);      // etäinen usva, hidas
        AddLayer(false, -0.45f, 0.55f, 0.37f);  // lähempi usva, vastakkaiseen suuntaan
        AddLayer(true, 0.30f, 0.80f, 0f);       // maasumu lattian tasolla

        dust = new SpriteRenderer[DustCount]; dustPos = new Vector2[DustCount]; dustVel = new Vector2[DustCount]; dustPhase = new float[DustCount];
        var rnd = new System.Random(7);
        for (int i = 0; i < DustCount; i++)
        {
            var go = new GameObject("Pöly"); go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = dotSprite; sr.sortingOrder = SortingOrder + 3;
            float s = 0.05f + (float)rnd.NextDouble() * 0.09f; go.transform.localScale = new Vector3(s, s, 1f);
            dust[i] = sr;
            dustPos[i] = new Vector2((float)rnd.NextDouble() * 40f, (float)rnd.NextDouble() * 40f);
            dustVel[i] = new Vector2(0.08f + (float)rnd.NextDouble() * 0.15f, 0.02f + (float)rnd.NextDouble() * 0.05f);
            dustPhase[i] = (float)rnd.NextDouble() * 6.28f;
        }
        lastW = lastH = 0f;
    }

    void AddLayer(bool ground, float speed, float camParallax, float yShift)
    {
        var l = new Layer { ground = ground, speed = speed, camParallax = camParallax };
        for (int k = 0; k < 3; k++)
        {
            var go = new GameObject(ground ? "Maasumu" : "Usva"); go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = ground ? groundSprite : hazeSprite;
            sr.sortingOrder = SortingOrder + (ground ? 1 : 0) + layers.Count;
            l.tiles[k] = sr;
        }
        l.offset = yShift; // vain käytetään erottamaan kerrokset vaiheeltaan
        layers.Add(l);
    }

    void Resize(float w, float h)
    {
        foreach (var l in layers)
        {
            // yksi laatta on vähintään ruudun levyinen, jotta kolme laattaa peittää ruudun aina
            var sp = l.tiles[0].sprite;
            float tileW = w * 1.15f;
            float tileH = l.ground ? h * 0.45f : h * 1.25f;
            var scale = new Vector3(tileW / sp.bounds.size.x, tileH / sp.bounds.size.y, 1f);
            float y = l.ground ? -h * 0.5f + tileH * 0.5f - h * 0.03f : 0f;
            for (int k = 0; k < 3; k++) { l.tiles[k].transform.localScale = scale; var p = l.tiles[k].transform.localPosition; l.tiles[k].transform.localPosition = new Vector3(p.x, y, p.z); }
        }
    }

    // ---- proseduraaliset kuvat ----

    /// <summary>Vaakasuunnassa (ja tasokuvassa myös pystyssä) saumaton pehmeä kohina. ground = pystysuunnassa tihein alhaalla ja haalenee ylöspäin.</summary>
    static Sprite MakeNoise(int w, int h, bool ground)
    {
        var tx = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        int[] cells = { 4, 8, 16 }; float[] amp = { 0.55f, 0.3f, 0.15f };
        var grids = new float[cells.Length][];
        var rnd = new System.Random(ground ? 11 : 5);
        for (int o = 0; o < cells.Length; o++)
        {
            int cx = cells[o], cy = ground ? Mathf.Max(2, cells[o] / 4) : cells[o];
            grids[o] = new float[cx * cy];
            for (int i = 0; i < grids[o].Length; i++) grids[o][i] = (float)rnd.NextDouble();
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = 0f;
                for (int o = 0; o < cells.Length; o++)
                {
                    int cx = cells[o], cy = ground ? Mathf.Max(2, cells[o] / 4) : cells[o];
                    float fx = (float)x / w * cx, fy = (float)y / h * cy;
                    int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
                    float tx_ = Smooth(fx - x0), ty = Smooth(fy - y0);
                    float a = grids[o][(y0 % cy) * cx + (x0 % cx)], b = grids[o][(y0 % cy) * cx + ((x0 + 1) % cx)];
                    float c = grids[o][((y0 + 1) % cy) * cx + (x0 % cx)], d = grids[o][((y0 + 1) % cy) * cx + ((x0 + 1) % cx)];
                    v += Mathf.Lerp(Mathf.Lerp(a, b, tx_), Mathf.Lerp(c, d, tx_), ty) * amp[o];
                }
                float alpha = Mathf.Clamp01((v - 0.25f) * 1.9f);          // paljon aukkoja, jotta se näyttää usvalta eikä harmaalta kalvolta
                if (ground) { float fall = 1f - (float)y / (h - 1); alpha *= fall * fall; }   // y=0 on alhaalla
                px[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        tx.SetPixels(px); tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    static float Smooth(float t) { return t * t * (3f - 2f * t); }

    static Sprite MakeDot()
    {
        const int n = 16;
        var tx = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tx.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 16f);
    }
}
