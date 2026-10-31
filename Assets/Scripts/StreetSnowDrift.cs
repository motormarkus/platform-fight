using UnityEngine;

/// <summary>
/// Vaakasuoraan puhaltava lumipöly Alppikylän kadulle: samat saumattomat pölykuvat kuin lumirinteellä (SlopeLoop), mutta liike on tuulen suuntainen (oikealta vasemmalle)
/// ja hahmon kävely siirtää pölyä parallaksina. Luo itsensä ajossa (ei valikkoa); näkyy vain Alppikylän kadulla. Kuvat: Resources/Snow.
/// </summary>
public class StreetSnowDrift : MonoBehaviour
{
    const string AreaName = "Alppikylän katu";
    static readonly string[] DesertAreas = { "Aavikko", "Aavikkotie", "Aavikkokatu", "Junapiha", "Juna-asema" };
    static readonly Color Sand = new Color(0.86f, 0.70f, 0.46f, 1f);
    const float StormCycle = 75f;        // hiekkamyrskyn väli (s); myrsky on voimakkaimmillaan noin kolmanneksen kierrosta
    float stormOffset;
    class Layer { public float baseAlpha; public Transform root; public SpriteRenderer[] srs; public float speed, parallax, fall, phase; public int cols, rows; }
    Layer[] layers;
    bool shown = true;
    readonly System.Collections.Generic.Dictionary<Transform, float> scroll = new System.Collections.Generic.Dictionary<Transform, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var back = Resources.Load<Texture2D>("Snow/lumi_poly_2");
        var front = Resources.Load<Texture2D>("Snow/lumi_poly_1");
        if (back == null && front == null) return;
        var go = new GameObject("Katulumipöly");
        DontDestroyOnLoad(go);
        var d = go.AddComponent<StreetSnowDrift>();
        d.stormOffset = Random.Range(0f, StormCycle);
        d.Build(back, front);
    }

    static Sprite MakeSprite(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

    void Build(Texture2D back, Texture2D front)
    {
        var list = new System.Collections.Generic.List<Layer>();
        if (back != null) list.Add(MakeLayer("Taka", MakeSprite(back), 5f, 0.25f, 0.8f, -9985, 0.30f, 0.6f));
        if (front != null) list.Add(MakeLayer("Etu", MakeSprite(front), 9f, 0.55f, 1.2f, 5000, 0.45f, 0.3f));
        layers = list.ToArray();
    }

    Layer MakeLayer(string name, Sprite sp, float speed, float parallax, float fall, int order, float alpha, float phase)
    {
        var l = new Layer { baseAlpha = alpha, speed = speed, parallax = parallax, fall = fall, phase = phase, cols = 3, rows = 3 };
        var r = new GameObject(name); r.transform.SetParent(transform, false); l.root = r.transform;
        l.srs = new SpriteRenderer[l.cols * l.rows];
        for (int i = 0; i < l.srs.Length; i++)
        {
            var c = new GameObject("Laatta " + i); c.transform.SetParent(r.transform, false);
            var sr = c.AddComponent<SpriteRenderer>();
            sr.sprite = sp; sr.sortingOrder = order; sr.color = new Color(1f, 1f, 1f, alpha);
            l.srs[i] = sr;
        }
        return l;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (layers == null || cam == null) return;
        var a = Area.Effective;
        bool snow = a != null && a.areaName == AreaName;
        bool desert = a != null && System.Array.IndexOf(DesertAreas, a.areaName) >= 0;
        float storm = 0f;
        if (desert)
        {
            float p = Mathf.Repeat(Time.time + stormOffset, StormCycle) / StormCycle;
            storm = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Mathf.Sin(p * Mathf.PI * 2f) - 0.15f) / 0.6f));
        }
        bool here = snow || (desert && storm > 0.01f);
        if (here != shown) { shown = here; foreach (var l in layers) foreach (var s in l.srs) s.enabled = here; }
        if (!here) return;
        float size = cam.orthographicSize;
        float worldH = 1134f / (0.9f * 941f) * 2f * size;      // sama koko kuin rinteellä (suhteessa ruudun korkeuteen)
        float cx = cam.transform.position.x, cy = cam.transform.position.y;
        float spdMul = desert ? 1.5f + 2.5f * storm : 1f;
        Color tint = desert ? Sand : Color.white;
        foreach (var l in layers)
        {
            float tile = worldH * (l.speed > 6f ? 1f : 0.83f);
            float sc = tile / l.srs[0].sprite.bounds.size.x;
            float t = Time.time + l.phase * 3f;
            scroll[l.root] = (scroll.TryGetValue(l.root, out float sv) ? sv : 0f) + l.speed * spdMul * Time.deltaTime;
            float offX = Mathf.Repeat(-scroll[l.root] - cx * l.parallax, tile) - tile * 0.5f;   // keskitetty: kolme laattaa peittävät aina vähintään ±1 laatan, ei aukkoa ruudun reunassa
            float offY = Mathf.Repeat(-t * l.fall, tile) - tile * 0.5f;
            float al = desert ? l.baseAlpha * 1.6f * storm : l.baseAlpha;
            var col = new Color(tint.r, tint.g, tint.b, Mathf.Clamp01(al));
            for (int k = 0; k < l.srs.Length; k++)
            {
                l.srs[k].color = col;
                int i = k % l.cols, j = k / l.cols;
                var tr = l.srs[k].transform;
                tr.localScale = new Vector3(sc, sc, 1f);
                tr.position = new Vector3(cx + offX + (i - 1) * tile, cy + offY + (j - 1) * tile, 0f);
            }
        }
    }
}
