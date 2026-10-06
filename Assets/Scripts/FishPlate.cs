using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// El Loipparin kala-annos pöydällä. Lyönti pöytään: annos valuu reunalta lattialle ja hajoaa (kala liukuu, ranskalaiset leviävät).
/// Raju häiriö (potku, heitetty pöytä, päälle lentävä vihu): lautanen räjähtää, kala ja ranskalaiset lentävät kauas ja pyörivät,
/// kala välillä jopa kameraa kohti (suurenee). Kaikki jää lattialle. Kuvat: Resources/Kala.
/// </summary>
public class FishPlate : MonoBehaviour
{
    public Crate table;
    public float tableX, tableTop = 1.6f;
    public float scale = 1f;
    public AudioClip[] breakSounds;
    [Tooltip("Kuvat kansiosta Resources/<spriteSet> (Kala = kala-annos, Hummeri = hummeriannos).")]
    public string spriteSet = "Kala";

    class Set { public Sprite plate; public Sprite[] burst, fish, fries, shards, lemons, veg1, veg2; }
    static readonly Dictionary<string, Set> sets = new Dictionary<string, Set>();
    Sprite plate; Sprite[] burst, fish, fries, shards, lemons, veg1, veg2;
    enum S { OnTable, Tipping, Burst, Done }
    S state = S.OnTable;
    SpriteRenderer sr;
    int seenDisturb;
    float t, height, vx, vy, rot, spin, dir;

    void Load()
    {
        string key = string.IsNullOrEmpty(spriteSet) ? "Kala" : spriteSet;
        if (!sets.TryGetValue(key, out var set)) { set = LoadSet(key); sets[key] = set; }
        plate = set.plate; burst = set.burst; fish = set.fish; fries = set.fries; shards = set.shards; lemons = set.lemons; veg1 = set.veg1; veg2 = set.veg2;
    }

    static Set LoadSet(string folder)
    {
        var set = new Set();
        var all = Resources.LoadAll<Sprite>(folder);
        Sprite[] Pick(string p)
        {
            var l = new List<Sprite>();
            foreach (var s in all) if (s.name.StartsWith(p)) l.Add(s);
            l.Sort((a, b) => FrameNo(a).CompareTo(FrameNo(b)));
            return l.ToArray();
        }
        foreach (var s in all) if (s.name == "annos") set.plate = s;
        set.burst = Pick("hajoaa_"); set.fish = Pick("kala_"); set.fries = Pick("ranska_"); set.shards = Pick("sirpale_"); set.lemons = Pick("sitruuna_");
        set.veg1 = Pick("parsa_"); set.veg2 = Pick("peruna_");   // lentokuvat (pyörivät kuvasarjana)
        return set;
    }
    static int FrameNo(Sprite s) { int i = s.name.LastIndexOf('_'); return i >= 0 && int.TryParse(s.name.Substring(i + 1), out int n) ? n : 0; }

    void Awake()
    {
        Load();
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = plate;
    }

    void Start()
    {
        if (table != null) seenDisturb = table.Disturb;
        height = tableTop;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        switch (state)
        {
            case S.OnTable:
                if (table == null) { Tip(Random.value < 0.5f ? -1f : 1f); break; }
                // annos pysyy pöydällä myös kannossa ja lennossa (heitetty pöytä)
                transform.position = table.transform.position + new Vector3(tableX, -0.01f, 0f);
                height = tableTop + table.Height;
                if (table.Disturb != seenDisturb)
                {
                    seenDisturb = table.Disturb;
                    if (table.Airborne && !table.IsBroken) break;          // nostettiin: pysyy pöydällä
                    float d = table.LastHitDir != 0f ? table.LastHitDir : (Random.value < 0.5f ? -1f : 1f);
                    if (table.LastViolent) Explode(d);
                    else Tip(d);
                }
                break;
            case S.Tipping:
            {
                // valuu reunalta: liukuu sivulle, kallistuu ja putoaa
                Vector3 p = transform.position; p.x += vx * dt; transform.position = p;
                vy -= 25f * dt; height += vy * dt; rot += spin * dt;
                if (height <= 0f) { height = 0f; Spill(false); }
                break;
            }
            case S.Burst:
                // lautanen räjähtää pöydän pinnalla (hajoamiskuvat), sitten palat lentävät
                if (burst.Length > 0) sr.sprite = burst[Mathf.Min((int)(t / 0.07f), burst.Length - 1)];
                if (t >= 0.07f * Mathf.Max(1, burst.Length)) Spill(true);
                break;
        }
        if (state == S.Done) return;
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        sr.transform.localPosition = new Vector3(0f, height, 0f);
        sr.sortingOrder = state == S.OnTable && table != null ? table.SortOrder + 2 : Mathf.RoundToInt(-transform.position.y * 100f) + 1;
    }

    void Tip(float d)
    {
        state = S.Tipping; t = 0f; dir = d;
        vx = d * Random.Range(1.0f, 1.8f); vy = Random.Range(0.5f, 1.5f); spin = -d * Random.Range(120f, 220f);
        table = null;
    }

    void Explode(float d)
    {
        state = S.Burst; t = 0f; dir = d;
        if (table != null) height = tableTop + table.Height;
        table = null;
        HitFx.OnBreak(0.08f);
        PlayBreak(1f);
    }

    void PlayBreak(float vol)
    {
        if (breakSounds != null && breakSounds.Length > 0) HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], vol * Random.Range(0.7f, 1f));
    }

    /// Ruoka ja lautasen palat lattialle: violent = kauas lentäen, muuten valuu läheltä.
    void Spill(bool violent)
    {
        state = S.Done;
        if (!violent) { HitFx.OnBreak(0f); PlayBreak(0.7f); }
        Vector3 p = transform.position;
        float h = violent ? height : 0.1f;
        float s = scale;
        // kala
        if (fish.Length > 0)
        {
            bool towardCam = violent && Random.value < 0.4f;
            FoodDebris.Spawn(fish, p, h, s,
                violent ? dir * Random.Range(3f, 7f) : dir * Random.Range(1.2f, 2.4f),
                violent ? Random.Range(6f, 10f) : Random.Range(1f, 2f),
                violent ? Random.Range(-1.2f, 0.6f) : 0f,
                violent ? Random.Range(-500f, 500f) : -dir * Random.Range(60f, 140f),
                true, towardCam ? Random.Range(0.9f, 1.4f) : 0f);
        }
        int nf = violent ? Random.Range(10, 17) : Random.Range(5, 9);
        for (int i = 0; i < nf && fries.Length > 0; i++)
        {
            float a = violent ? Random.Range(-1f, 1f) : dir * Random.Range(0.2f, 1f);
            FoodDebris.Spawn(new[] { fries[Random.Range(0, fries.Length)] }, p + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f), h, s * 0.6f,
                violent ? a * Random.Range(2f, 6.5f) + dir * 1.5f : a * Random.Range(0.6f, 1.8f),
                violent ? Random.Range(4f, 10f) : Random.Range(0.5f, 2.5f),
                Random.Range(-0.8f, 0.5f) * (violent ? 1f : 0.4f),
                Random.Range(-900f, 900f) * (violent ? 1f : 0.3f), false, 0f);
        }
        // lisukkeet omilla lentokuvillaan (hummeriannos: parsat ja lohkoperunat)
        foreach (var veg in new[] { veg1, veg2 })
        {
            if (veg == null || veg.Length == 0) continue;
            int nv = violent ? Random.Range(4, 7) : Random.Range(2, 4);
            for (int i = 0; i < nv; i++)
            {
                float a = violent ? Random.Range(-1f, 1f) : dir * Random.Range(0.2f, 1f);
                FoodDebris.Spawn(veg, p + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f), h, s * 0.6f,
                    violent ? a * Random.Range(2f, 6.5f) + dir * 1.5f : a * Random.Range(0.6f, 1.8f),
                    violent ? Random.Range(4f, 10f) : Random.Range(0.5f, 2.5f),
                    Random.Range(-0.8f, 0.5f) * (violent ? 1f : 0.4f),
                    Random.Range(-900f, 900f) * (violent ? 1f : 0.3f), false, 0f);
            }
        }
        int ns = violent ? Random.Range(5, 8) : Random.Range(3, 6);
        for (int i = 0; i < ns && shards.Length > 0; i++)
            FoodDebris.Spawn(new[] { shards[Random.Range(0, shards.Length)] }, p, h, s,
                Random.Range(-1f, 1f) * (violent ? 4f : 1.5f), violent ? Random.Range(3f, 7f) : Random.Range(0.5f, 1.5f),
                Random.Range(-0.6f, 0.4f), Random.Range(-600f, 600f), false, 0f);
        int nl = violent ? Random.Range(1, 3) : 1;
        for (int i = 0; i < nl && lemons.Length > 0; i++)
            FoodDebris.Spawn(new[] { lemons[Random.Range(0, lemons.Length)] }, p, h, s,
                Random.Range(-1f, 1f) * (violent ? 4f : 1f) + dir, violent ? Random.Range(4f, 8f) : Random.Range(0.5f, 1.5f),
                Random.Range(-0.5f, 0.3f), Random.Range(-700f, 700f), false, 0f);
        Destroy(gameObject);
    }
}

/// Lentävä ja lattialle jäävä ruoan tai lautasen pala. Juuri on maan kohdassa (piirtojärjestys), kuva korkeuden verran ylempänä.
public class FoodDebris : MonoBehaviour
{
    static readonly List<FoodDebris> live = new List<FoodDebris>();
    static PlayerController pc;
    const int MaxPieces = 300;
    Sprite[] frames; SpriteRenderer sr;
    float height, vx, vy, vDepth, spin, rot, baseScale, camBoost, frameT, peak;
    bool isFish, landed; int bounces;

    public static void Spawn(Sprite[] frames, Vector3 pos, float height, float scale, float vx, float vy, float vDepth, float spin, bool isFish, float camBoost)
    {
        if (frames == null || frames.Length == 0) return;
        var go = new GameObject(isFish ? "Kala" : "Pala");
        go.transform.position = new Vector3(pos.x, pos.y - Random.Range(0f, 0.15f), 0f);
        var d = go.AddComponent<FoodDebris>();
        var v = new GameObject("Kuva"); v.transform.SetParent(go.transform, false);
        d.sr = v.AddComponent<SpriteRenderer>();
        d.frames = frames; d.height = height; d.vx = vx; d.vy = vy; d.vDepth = vDepth; d.spin = spin;
        d.baseScale = scale; d.isFish = isFish; d.camBoost = camBoost; d.peak = Mathf.Max(0.5f, height + vy * vy / 60f);
        d.frameT = Random.Range(0f, 8f);
        d.rot = isFish || frames.Length > 1 ? 0f : Random.Range(0f, 360f);
        d.sr.sprite = frames[0];
        d.sr.flipX = vx < 0f;
        live.Add(d);
        while (live.Count > MaxPieces) { var o = live[0]; live.RemoveAt(0); if (o != null) Destroy(o.gameObject); }
        d.Apply();
    }

    void OnDestroy() { live.Remove(this); }

    void Update()
    {
        if (landed) return;
        float dt = Time.deltaTime;
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        Vector3 p = transform.position;
        p.x += vx * dt;
        float minY = pc != null ? pc.minDepthY : -10f, maxY = pc != null ? pc.maxDepthY : 10f;
        p.y = Mathf.Clamp(p.y + vDepth * dt, minY, maxY);
        transform.position = p;
        vy -= 30f * dt;
        height += vy * dt;
        rot += spin * dt;
        if (!isFish && frames.Length > 1)
        {
            // piirretyt lentokuvat: kuvasarja pyörii (ei kuvan kääntöä)
            frameT += dt * Mathf.Abs(spin) / 45f;
            sr.sprite = frames[(int)frameT % frames.Length];
            rot = 0f;
        }
        if (isFish && frames.Length > 1)
        {
            // kala vääntelehtii lennossa: asennot vaihtuvat
            frameT += dt * (7f + Mathf.Abs(spin) / 70f);
            sr.sprite = frames[(int)frameT % frames.Length];
        }
        if (height <= 0f)
        {
            height = 0f;
            if (isFish && bounces < 2 && vy < -3f)
            {
                // kala sätkii lattialla ennen kuin jää
                bounces++;
                vy = Random.Range(2.5f, 4f) / bounces; vx *= 0.35f; vDepth = 0f; spin *= 0.3f;
            }
            else if (!isFish && vy < -6f && bounces < 1)
            {
                bounces++;
                vy = -vy * 0.25f; vx *= 0.5f; spin *= 0.5f;
            }
            else
            {
                landed = true;
                camBoost = 0f;
                if (isFish)
                {
                    // lattialle jäänyt kala: poimittava ja heitettävä kuten pullo (nyrkin takana), osuma 1
                    var side = frames[Random.value < 0.5f ? 0 : frames.Length - 1];
                    var go = new GameObject("Kala (maassa)");
                    go.transform.position = transform.position;
                    var b = go.AddComponent<Bottle>();
                    b.sprites = new[] { side };
                    b.food = true; b.throwDamage = 1; b.stainKind = "-"; b.scale = baseScale; b.pivotY = 0f; b.throwSpeed = 13f;
                    Destroy(gameObject);
                    return;
                }
            }
        }
        Apply();
    }

    void Apply()
    {
        // kameraa kohti lentävä kala suurenee lennon huipulla
        float k = camBoost > 0f ? Mathf.Clamp01(height / peak) : 0f;
        float s = baseScale * (1f + camBoost * k);
        sr.transform.localScale = new Vector3(s, s, 1f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        sr.transform.localPosition = new Vector3(0f, height, 0f);
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        sr.sortingOrder = camBoost > 0f && k > 0.2f ? 32000 : (landed ? order - 60 : order + 5);
    }
}
