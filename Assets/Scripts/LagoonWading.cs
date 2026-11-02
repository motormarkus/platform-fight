using System.Collections.Generic;
using UnityEngine;

/// <summary>Laguunin kahlaus: heron, kumppanin ja vihujen alaruumis uppoaa veteen (noin rintaan keskellä laguunia). Veden korkeus riippuu x-asemasta:
/// rannalla nolla, keskellä täysi, luolan hiekkarannalla matalampi. Toteutus: hahmojen spritemateriaali vaihdetaan veden pinnan (_WaterY) mukaan leikkaavaan varjostimeen vain laguunin alueella.</summary>
public class LagoonWading : MonoBehaviour
{
    public Material wadeMaterial;
    [Tooltip("Kuvan 1672 px:n leveyden maailmayksikkömitta (W) ja katu_12:n vasemman reunan x.")]
    public float tileWidth = 29f, tile12Left = 0f;
    public float chestHeight = 2.1f;
    public float shoreStartPx = 850f, fullPx = 1500f, caveStartPx = 2450f, caveEndPx = 2840f;
    public string areaName = "Viidakon katu";

    class Saved { public SpriteRenderer sr; public Material mat; }
    readonly Dictionary<SpriteRenderer, Material> original = new Dictionary<SpriteRenderer, Material>();
    readonly List<SpriteRenderer> tmp = new List<SpriteRenderer>();
    MaterialPropertyBlock block;
    static readonly int WaterY = Shader.PropertyToID("_WaterY");

    [Header("Luolan matala vesi (kengät kahlaavat)")]
    public string caveAreaName = "Viidakon luola";
    public float caveX0 = 0f, caveShallow = 0.3f, caveInStartPx = 1650f, caveInFullPx = 1950f, caveOutStartPx = 4520f, caveOutEndPx = 4700f;

    /// Veden korkeus (maailmayksikköä jalkojen tasosta) x:ssä nykyisellä alueella; 0 = ei vettä.
    float Depth(float x, float scale)
    {
        var a = Area.Current;
        if (a == null) return 0f;
        if (a.areaName == areaName) return Level(x) * chestHeight * scale;
        if (a.areaName == caveAreaName)
        {
            float u = (x - caveX0) / tileWidth * 1672f;
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(caveInStartPx, caveInFullPx, u)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(caveOutStartPx, caveOutEndPx, u))) * caveShallow * scale;
        }
        return 0f;
    }

    float Level(float x)
    {
        float u = (x - tile12Left) / tileWidth * 1672f;
        float ramp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(shoreStartPx, fullPx, u));
        return ramp * (1f - 0.55f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(caveStartPx, caveEndPx, u)));
    }

    void LateUpdate()
    {
        if (wadeMaterial == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        bool here = Area.Current != null && Area.Current.areaName == areaName;
        foreach (var p in Players.All) if (p != null) Apply(p.transform, 1f, here);
        foreach (var e in Enemy.All) if (e != null) Apply(e.transform, Mathf.Max(0.1f, e.visualScale), here);
    }

    public float slowAtFull = 0.62f;   // liikkeen kerroin täydessä syvyydessä
    readonly Dictionary<Transform, Vector3> lastPos = new Dictionary<Transform, Vector3>();
    readonly Dictionary<Transform, float> nextSplash = new Dictionary<Transform, float>();

    void Splash(Transform t, float lvl, float scale, bool burst)
    {
        Vector3 p = t.position; int order = Mathf.RoundToInt(-p.y * 100f) + 7;
        var soft = KingDust.Soft();
        int n = burst ? 14 : 3;
        for (int i = 0; i < n; i++)
            WaterDrop.Make(soft, new Vector3(p.x + Random.Range(-0.6f, 0.6f) * scale, p.y + lvl, 0f), new Vector2(Random.Range(-1.8f, 1.8f), Random.Range(burst ? 3f : 1.6f, burst ? 6f : 3.4f)), Random.Range(0.35f, 0.6f), Random.Range(0.07f, 0.15f), order, false);
        WaterDrop.Make(soft, new Vector3(p.x, p.y + lvl * 0.9f, 0f), Vector2.zero, burst ? 0.9f : 0.6f, burst ? 3.4f : 2.4f, order - 1, true);   // laajeneva värerengas pinnalla
    }

    void Apply(Transform t, float scale, bool here)
    {
        float lvl = Depth(t.position.x, scale);
        float mul = Mathf.Lerp(1f, slowAtFull, Mathf.Clamp01(lvl / (chestHeight * scale)));
        var pc = t.GetComponent<PlayerController>(); if (pc != null) pc.wadeMul = mul;
        var en = t.GetComponent<Enemy>(); if (en != null) en.wadeMul = mul;
        if (lvl > 0.08f)
        {
            bool wasIn = lastPos.TryGetValue(t, out var prev);
            lastPos[t] = t.position;
            float spd = wasIn && Time.deltaTime > 0f ? (t.position - prev).magnitude / Time.deltaTime : 0f;
            nextSplash.TryGetValue(t, out var ns);
            if (!wasIn) { Splash(t, lvl, scale, true); nextSplash[t] = Time.time + 0.2f; }
            else if (spd > 1.2f && Time.time >= ns) { Splash(t, lvl, scale, false); nextSplash[t] = Time.time + Mathf.Lerp(0.28f, 0.14f, Mathf.Clamp01(spd / 6f)); }
        }
        else lastPos.Remove(t);
        tmp.Clear(); t.GetComponentsInChildren(true, tmp);
        foreach (var sr in tmp)
        {
            if (sr == null || sr.name.Contains("Shadow") || sr.name.Contains("Varjo")) continue;
            if (lvl > 0.05f)
            {
                if (!original.ContainsKey(sr)) original[sr] = sr.sharedMaterial;
                if (sr.sharedMaterial != wadeMaterial) sr.sharedMaterial = wadeMaterial;
                sr.GetPropertyBlock(block); block.SetFloat(WaterY, t.position.y + lvl); sr.SetPropertyBlock(block);
            }
            else if (original.TryGetValue(sr, out var m))
            {
                sr.sharedMaterial = m; original.Remove(sr);
                sr.GetPropertyBlock(block); block.SetFloat(WaterY, -9999f); sr.SetPropertyBlock(block);
            }
        }
    }
}

/// <summary>Vesipisara tai laajeneva värerengas (ring): pehmeä pyöreä kuva, vaalea sinertävä, himmenee.</summary>
public class WaterDrop : MonoBehaviour
{
    Vector2 vel; float life, age, size; bool ring; SpriteRenderer sr;

    public static void Make(Sprite sp, Vector3 pos, Vector2 vel, float life, float size, int order, bool ring)
    {
        var go = new GameObject(ring ? "Väre" : "Pisara"); go.transform.position = pos;
        var d = go.AddComponent<WaterDrop>();
        d.sr = go.AddComponent<SpriteRenderer>(); d.sr.sprite = sp; d.sr.sortingOrder = order;
        d.vel = vel; d.life = life; d.size = size; d.ring = ring;
        go.transform.localScale = ring ? new Vector3(size * 0.4f, size * 0.12f, 1f) : Vector3.one * size;
        d.sr.color = ring ? new Color(0.85f, 0.97f, 1f, 0.45f) : new Color(0.8f, 0.95f, 1f, 0.85f);
    }

    void Update()
    {
        age += Time.deltaTime; float u = Mathf.Clamp01(age / life);
        var c = sr.color;
        if (ring) { transform.localScale = new Vector3(size * Mathf.Lerp(0.4f, 1f, u), size * Mathf.Lerp(0.12f, 0.3f, u), 1f); c.a = 0.45f * (1f - u); }
        else { vel.y -= 14f * Time.deltaTime; transform.position += (Vector3)(vel * Time.deltaTime); c.a = 0.85f * (1f - u * u); }
        sr.color = c;
        if (age >= life) Destroy(gameObject);
    }
}
