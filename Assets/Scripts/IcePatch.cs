using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Taikurin jäätävä lattia: kaistale jäätä, joka leviää nopeasti sauvan kärjestä eteenpäin (IcePatch.Spawn). Jäälle astuva hero tai vihu liukastuu lähes varmasti
/// (Enemy.CreamSlip / PlayerController.CreamSlip: sätkii, liukuu ja kaatuu selälleen) ja liukuu jäällä kuin muuallakin (OnIce). Omistaja (taikuri) ja muut `exempt`-vihut eivät liukastu.
/// Jää häipyy ajan kuluttua. Kuvat: Resources/Jaa/jaa_1..3 (Tools/IceAssets/make_ice.py tai käyttäjän omat kuvat), koko 1000 px = 3,4 yksikköä.
/// </summary>
public sealed class IcePatch : MonoBehaviour
{
    public static readonly List<IcePatch> All = new List<IcePatch>();
    public const float WorldWidth = 3.4f, StretchY = 1.45f;
    public const int FloorOrder = -9000;

    [Tooltip("Liukastumisia sekunnissa jäällä liikkuessa (suuri = melkein varma).")] public float walkSlipPerSec = 14f, runSlipPerSec = 24f, enemySlipPerSec = 12f;
    public float slipTime = 0.55f, slipSpeed = 9f, enemySlipTime = 0.65f, enemySlipSpeed = 10.5f, immunity = 1.2f;   // rajumpi: pitkä, nopea liuku
    public Enemy owner;                       // ei liukastu
    public readonly HashSet<Enemy> exempt = new HashSet<Enemy>();

    class Seg { public Transform t; public SpriteRenderer sr; public float mul = 1f, sy = 1f, halfW, halfH, delay, grow; public Vector3 center; }
    readonly List<Seg> segs = new List<Seg>();
    float age, life = 9f;
    static Sprite[] sprites; static Sprite shardSprite;
    static readonly Dictionary<Object, float> immuneUntil = new Dictionary<Object, float>();
    static readonly Dictionary<Object, Vector3> lastPos = new Dictionary<Object, Vector3>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { All.Clear(); immuneUntil.Clear(); lastPos.Clear(); lastDir.Clear(); iceSeen.Clear(); sprites = null; shardSprite = null; }

    static void EnsureSprites()
    {
        if (sprites != null) return;
        var l = new List<Sprite>();
        for (int i = 1; i <= 3; i++)
        {
            var tx = Resources.Load<Texture2D>("Jaa/jaa_" + i);
            if (tx == null) continue;
            tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
            l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), tx.width / WorldWidth));
        }
        sprites = l.ToArray();
        // pieni kidelasi: tehdään koodilla (valkoinen sinertävä sirpale)
        var st = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float dx = (x - 7.5f) / 7.5f, dy = (y - 7.5f) / 7.5f; float d = Mathf.Abs(dx) * .8f + Mathf.Abs(dy) * 1.2f;   // timantti
                float a = Mathf.Clamp01((1f - d) * 3f); float v = Mathf.Lerp(.75f, 1f, 1f - Mathf.Abs(dx + dy * .3f));
                st.SetPixel(x, y, new Color(.8f * v, .93f * v, 1f, a));
            }
        st.Apply(); shardSprite = Sprite.Create(st, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 120f);
    }

    /// <summary>Jääkaistale: alkaa pisteestä origin ja leviää suuntaan dir (+1 oikealle, -1 vasemmalle) length yksikköä (noin 0,5 s); kestää lifetime sekuntia.
    /// Kaistale peittää alueen koko lattiasyvyyden (useita limittäisiä laikkuja eri syvyyksillä), jolloin kukaan ei pääse jään ohi sivusta; kuvia suurennetaan sizeMul-kertaisiksi.</summary>
    public static IcePatch Spawn(Vector3 origin, float dir, float length = 7f, Enemy owner = null, float lifetime = 9f, float sizeMul = 1.25f)
    {
        EnsureSprites();
        if (sprites.Length == 0) return null;
        var go = new GameObject("Jäälaikka"); go.transform.position = new Vector3(origin.x, origin.y, 0f);
        var p = go.AddComponent<IcePatch>(); p.owner = owner; p.life = lifetime;
        float sign = dir >= 0f ? 1f : -1f; float step = 2.6f; int n = Mathf.Max(1, Mathf.CeilToInt(length / step));
        // syvyysrivit: koko lattian syvyys (Area), muuten pelkkä oma kohta
        float lo = origin.y, hi = origin.y;
        var area = Area.Current;
        // jää pysyy lattialla: laikun näkyvä korkeus on VisH, ja keskipisteet rajataan niin, ettei reuna ylitä lattian (syvyysalueen) rajoja
        const float VisH = 1.8f;
        if (area != null && area.maxDepthY > area.minDepthY) { lo = area.minDepthY + VisH * 0.3f; hi = area.maxDepthY - VisH * 0.32f; if (hi < lo) lo = hi = (area.minDepthY + area.maxDepthY) * 0.5f; }
        int rows = hi - lo < 0.5f ? 1 : Mathf.Max(1, Mathf.CeilToInt((hi - lo) / 1.0f) + 1);
        for (int r = 0; r < rows; r++)
        {
            float ry = rows == 1 ? (area != null && area.maxDepthY > area.minDepthY ? lo : origin.y) : Mathf.Lerp(lo, hi, r / (float)(rows - 1));
            for (int i = 0; i < n; i++)
            {
                float rx = origin.x + sign * (1.3f + i * step + (r % 2) * 1.0f), halfVis = WorldWidth * 0.5f * sizeMul;
                if (area != null && area.walkMaxX > area.walkMinX)
                {
                    if (rx < area.walkMinX + 0.3f || rx > area.walkMaxX - 0.3f) continue;   // lattian ulkopuolelle ei jäätä
                    rx = Mathf.Clamp(rx, area.walkMinX + halfVis * 0.55f, area.walkMaxX - halfVis * 0.55f);
                }
                var sp = sprites[Random.Range(0, sprites.Length)];
                var seg = new GameObject("Jää " + r + "." + i); seg.transform.SetParent(go.transform, false);
                Vector3 c = new Vector3(rx, ry + Random.Range(-0.05f, 0.05f), 0f); seg.transform.position = c;
                float sy = VisH / Mathf.Max(0.1f, sp.bounds.size.y);
                var sr = seg.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.flipX = Random.value < .5f; sr.sortingOrder = FloorOrder + Mathf.RoundToInt(-c.y * 2f) + i;
                sr.color = new Color(1f, 1f, 1f, 0f); seg.transform.localScale = new Vector3(0.3f * sizeMul, 0.3f * sy, 1f);
                p.segs.Add(new Seg { t = seg.transform, sr = sr, mul = sizeMul, sy = sy, halfW = WorldWidth * 0.46f * sizeMul, halfH = VisH * 0.5f, delay = i * (0.5f / n) + r * 0.04f, grow = 0f, center = c });
            }
        }
        return p;
    }

    public static bool At(Vector3 p)
    {
        for (int i = 0; i < All.Count; i++) { var c = All[i]; if (c != null && c.Contains(p)) return true; }
        return false;
    }
    public bool Contains(Vector3 p)
    {
        foreach (var s in segs)
        {
            if (s.grow < 0.35f) continue;
            float dx = (p.x - s.center.x) / Mathf.Max(0.01f, s.halfW * s.grow), dy = (p.y - s.center.y) / Mathf.Max(0.01f, s.halfH);
            if (dx * dx + dy * dy <= 1f) return true;
        }
        return false;
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        float fade = age < life - 1.6f ? 1f : Mathf.Clamp01((life - age) / 1.6f);
        foreach (var s in segs)
        {
            if (age < s.delay) continue;
            if (s.grow < 1f)
            {
                s.grow = Mathf.Min(1f, s.grow + dt * 5f); float k = 1f - (1f - s.grow) * (1f - s.grow); float sc = Mathf.Lerp(0.3f, 1f, k);
                s.t.localScale = new Vector3(sc * s.mul, sc * s.sy, 1f);
                if (s.grow < 0.2f) Shards(s.center, 4, 3.5f);
            }
            s.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(s.grow * 2f) * fade);
        }
        if (age >= life) { Destroy(gameObject); return; }
        // liukastuminen (jokainen jäälaikka hoitaa omat; yhteinen suoja estää kaksoisliukastumisen)
        foreach (var pl in Players.All)
        {
            if (pl == null) continue;
            Vector3 q = pl.transform.position; float dxm = MoveX(pl, q); float sp = Speed(pl, q, dt);
            bool inside = Contains(q); bool entered = inside && Time.time - Seen(pl) > 0.3f; if (inside) iceSeen[pl] = Time.time;   // jäälle astuessa liukastuu heti
            if (!inside || pl.AirHeight > 0.05f || sp < 0.25f || Time.time < Immune(pl)) continue;
            float rate = sp > pl.moveSpeedX * 1.3f ? runSlipPerSec : walkSlipPerSec;
            if ((entered || Random.value < rate * dt) && pl.CreamSlip(dxm, slipTime, slipSpeed)) OnSlip(pl, pl.transform, slipTime);
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || !en.isActiveAndEnabled || en.IsDead || en == owner || exempt.Contains(en)) continue;
            Vector3 q = en.transform.position; float dxm = MoveX(en, q); float sp = Speed(en, q, dt);
            bool inside = Contains(q); bool entered = inside && Time.time - Seen(en) > 0.3f; if (inside) iceSeen[en] = Time.time;
            if (!inside || en.IsAirborneNow || sp < 0.25f || !en.BrainIdleOrChase || Time.time < Immune(en)) continue;
            if ((entered || Random.value < enemySlipPerSec * dt) && en.CreamSlip(dxm, enemySlipTime, enemySlipSpeed)) OnSlip(en, en.transform, enemySlipTime);
        }
    }

    static readonly Dictionary<Object, float> iceSeen = new Dictionary<Object, float>();
    static float Seen(Object o) => iceSeen.TryGetValue(o, out float u) ? u : -99f;
    static float Immune(Object o) => immuneUntil.TryGetValue(o, out float u) ? u : 0f;
    static readonly Dictionary<Object, float> lastDir = new Dictionary<Object, float>();
    /// Kulkusuunta (±1) ENNEN kuin Speed päivittää edellisen sijainnin (aiemmin suunta laskettiin jälkeenpäin: aina nolla -> satunnainen, liuku meni usein poispäin jäältä). Liuku jatkaa kulkusuuntaan.
    static float MoveX(Object o, Vector3 p)
    {
        float d = lastPos.TryGetValue(o, out var q) ? p.x - q.x : 0f;
        if (Mathf.Abs(d) > 0.0005f) lastDir[o] = Mathf.Sign(d);
        return lastDir.TryGetValue(o, out float s) ? s : 1f;
    }
    static float Speed(Object o, Vector3 p, float dt)
    {
        float sp = 0f;
        if (lastPos.TryGetValue(o, out var q) && dt > 0f) sp = new Vector2(p.x - q.x, p.y - q.y).magnitude / dt;
        lastPos[o] = p; return sp > 40f ? 0f : sp;
    }
    void OnSlip(Object key, Transform t, float time)
    {
        immuneUntil[key] = Time.time + time + 2.2f + immunity;   // liuku + makuu/ylösnousu + suoja
        Shards(t.position + Vector3.up * 0.3f, 12, 4.2f); HitFx.PlayWhoosh();
    }

    // ---------------- kidelasit ----------------
    class Shard { public Transform t; public SpriteRenderer sr; public Vector3 v; public float vh, age, life; }
    static readonly List<Shard> shards = new List<Shard>(); static ShardRunner runner;
    public static void Shards(Vector3 at, int n, float power)
    {
        EnsureSprites();
        if (runner == null) runner = new GameObject("Jääsirpaleet").AddComponent<ShardRunner>();
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject("Sirpale"); g.transform.SetParent(runner.transform, false); g.transform.position = new Vector3(at.x, at.y, 0f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = shardSprite; sr.sortingOrder = Mathf.RoundToInt(-at.y * 100f) + 6;
            float s = Random.Range(0.5f, 1.2f); g.transform.localScale = new Vector3(s, s, 1f);
            shards.Add(new Shard { t = g.transform, sr = sr, v = new Vector3(Random.Range(-1f, 1f) * power * 0.7f, Random.Range(-0.25f, 0.25f), 0f), vh = Random.Range(0.5f, 1f) * power, life = Random.Range(0.4f, 0.8f) });
        }
    }
    sealed class ShardRunner : MonoBehaviour
    {
        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                var d = shards[i]; if (d.t == null) { shards.RemoveAt(i); continue; }
                d.age += dt; d.vh -= 14f * dt; Vector3 p = d.t.position; p.x += d.v.x * dt; p.y += (d.v.y + d.vh) * dt; d.t.position = p; d.t.Rotate(0f, 0f, d.v.x * -180f * dt);
                float k = d.age / d.life; d.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - k * 1.4f));
                if (k >= 1f) { Destroy(d.t.gameObject); shards.RemoveAt(i); }
            }
        }
    }
}
