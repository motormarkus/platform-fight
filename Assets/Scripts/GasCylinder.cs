using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kaasupullo (Vegasin rakennustyömaa). Isku: pannun/tynnyrin kilahdus, korkki lentää ja pullo kaatuu (pohja iskun suuntaan, korkki taakse), sihisee ja päästää höyryä korkin päästä,
/// lähtee sitten kiihtyen liikkeelle iskun suuntaan, kaataa kaiken tieltään (kilahdus jokaisesta osumasta) ja räjähtää seinään osuessaan tai matkan lopussa.
/// Räjähdys: Jerrycanin räjähdys (kuva, ääni, vahinko, ketjureaktio). Toisen kaasupullon tai gallonan räjähdys sytyttää viereiset pullot.
/// Vaatii Caten (vastaanottaa iskut: Disturb/LastHitDir; anchored = ei nosteta). Kuva: crate.sprites[0] (korkki ylhäällä, pivot alhaalla).
/// </summary>
[RequireComponent(typeof(Crate))]
[DefaultExecutionOrder(900)]
public class GasCylinder : MonoBehaviour
{
    public static readonly List<GasCylinder> All = new List<GasCylinder>();
    public float hissTime = 0.75f, topple = 0.3f;
    public float startSpeed = 2.5f, accel = 13f, maxSpeed = 17f, maxDistance = 11f;
    public int rocketDamage = 14, blastDamage = 55;
    public float blastRadiusX = 3.8f, blastRadiusY = 1.9f;
    [Tooltip("Pullon korkeus maailmayksikköinä (korkin kohta sivusuunnassa).")]
    public float capHeight = 2.2f;

    enum S { Idle, Hiss, Rocket, Done }
    S state = S.Idle;
    Crate crate;
    int seenDisturb;
    float t, dir, travelled, boomAt = -1f, puffT;
    SpriteRenderer lying, lyingShadowDummy;
    AudioSource hiss;
    static Sprite[] boomSprs; static AudioClip[] clangs; static AudioClip hissClip; static Sprite softSpr;
    readonly HashSet<Enemy> bowled = new HashSet<Enemy>();
    readonly HashSet<PlayerController> bowledHeroes = new HashSet<PlayerController>();
    // raketti kuljettaa osuman saaneet mukanaan nokallaan seinään asti, jolloin räjähdys osuu niihin eikä mene ohi
    readonly List<Transform> carried = new List<Transform>();
    void CarryAlong()
    {
        var a = Area.Current; int n = 0;
        for (int i = carried.Count - 1; i >= 0; i--)
        {
            var tr = carried[i]; if (tr == null) { carried.RemoveAt(i); continue; }
            var en = tr.GetComponent<Enemy>(); var pl = tr.GetComponent<PlayerController>();
            bool keep = en != null ? (!en.IsDead && (en.IsAirborneNow || en.IsDownNow)) : pl != null && !pl.GameOver && (pl.IsAirborneThrown || pl.IsFloored);
            if (!keep) { carried.RemoveAt(i); continue; }
            float x = transform.position.x + dir * (1.0f - 0.5f * n++);
            if (a != null && a.walkMaxX > a.walkMinX) x = Mathf.Clamp(x, a.walkMinX + 0.15f, a.walkMaxX - 0.15f);
            tr.position = new Vector3(x, tr.position.y, tr.position.z);
        }
    }
    class Puff { public GameObject go; public SpriteRenderer sr; public Vector3 v; public float age, life, s0, s1; }
    readonly List<Puff> puffs = new List<Puff>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        crate = GetComponent<Crate>();
        crate.breakable = false; crate.anchored = true; crate.rollSprites = null; crate.standUpChance = 0f; crate.kickLaunch = false;
        if (clangs == null)
        {
            var pan = Resources.LoadAll<AudioClip>("Sfx/Pan"); var barrel = Resources.LoadAll<AudioClip>("Sfx/Barrel");
            var l = new List<AudioClip>(); l.AddRange(pan); l.AddRange(barrel); clangs = l.ToArray();
        }
        if (hissClip == null) hissClip = MakeHiss();
        if (softSpr == null) softSpr = MakeSoft();
    }

    void Start() { seenDisturb = crate.Disturb; }

    static void Clang(float vol = 0.9f)
    {
        if (clangs == null || clangs.Length == 0) { HitFx.OnBreak(0.05f); return; }
        HitFx.PlayClip(clangs[Random.Range(0, clangs.Length)], vol * Random.Range(0.85f, 1f));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdatePuffs(dt);
        if (state == S.Done) return;
        if (boomAt >= 0f && Time.time >= boomAt) { Explode(); return; }
        switch (state)
        {
            case S.Idle:
                if (crate.Disturb != seenDisturb)
                {
                    seenDisturb = crate.Disturb;
                    float d = crate.LastHitDir; if (Mathf.Abs(d) < 0.5f) d = Random.value < 0.5f ? -1f : 1f;
                    BeginHiss(d);
                }
                break;
            case S.Hiss:
                t += dt;
                float k = Mathf.Clamp01(t / topple);
                Pose(k, Mathf.Sin(Time.time * 70f) * 0.04f * (1f - k));
                EmitSteam(dt, 110f, 3.6f);   // paineella ulos korkin päästä
                if (t >= hissTime) { state = S.Rocket; t = 0f; travelled = 0f; }
                break;
            case S.Rocket:
                t += dt;
                float sp = Mathf.Min(maxSpeed, startSpeed + accel * t);
                float dx = dir * sp * dt;
                Vector3 p = transform.position; p.x += dx; transform.position = p; travelled += Mathf.Abs(dx);
                Pose(1f, 0f);
                EmitSteam(dt, 240f, 7f);
                Bowl(); CarryAlong();
                var a = Area.Current;
                bool wall = a != null && a.walkMaxX != 0f && (p.x <= a.walkMinX + 0.3f || p.x >= a.walkMaxX - 0.3f);
                if (wall || travelled >= maxDistance) Explode();
                break;
        }
    }

    void BeginHiss(float d)
    {
        dir = d; state = S.Hiss; t = 0f;
        Clang(1f);
        if (hiss == null) { hiss = gameObject.AddComponent<AudioSource>(); hiss.clip = hissClip; hiss.loop = true; hiss.spatialBlend = 0f; hiss.volume = 0.5f * GameSettings.SfxVolume; }
        hiss.Play();
        if (crate.body != null) crate.body.enabled = false;
        if (lying == null)
        {
            var g = new GameObject("Makuu"); g.transform.SetParent(transform, false);
            lying = g.AddComponent<SpriteRenderer>();
            lying.sprite = crate.sprites != null && crate.sprites.Length > 0 ? crate.sprites[0] : null;
            g.transform.localScale = crate.body != null ? crate.body.transform.localScale : Vector3.one * crate.visualScale;
        }
        lying.enabled = true;
    }

    /// Kaatuminen: k 0 = pystyssä, 1 = kyljellään (korkki taakse, pohja liikkumissuuntaan); wob = tärinä.
    void Pose(float k, float wob)
    {
        if (lying == null || lying.sprite == null) return;
        float ease = 1f - (1f - k) * (1f - k);
        float width = lying.sprite.bounds.size.x * lying.transform.localScale.x;
        lying.transform.localRotation = Quaternion.Euler(0f, 0f, dir * 90f * ease + wob * 57f);
        lying.transform.localPosition = new Vector3(dir * -0.0f, Mathf.Lerp(0f, width * 0.5f, ease), 0f);
        lying.sortingOrder = crate.SortOrder + 1;
    }

    Vector3 CapWorld()
    {
        float h = lying != null && lying.sprite != null ? lying.sprite.bounds.size.y * lying.transform.localScale.y : capHeight;
        Vector3 up = lying != null ? lying.transform.up : Vector3.up;
        return lying != null ? lying.transform.position + up * h : transform.position + Vector3.up * h;
    }

    void EmitSteam(float dt, float rate, float speed)
    {
        puffT += dt * rate / 10f;
        while (puffT >= 1f)
        {
            puffT -= 1f;
            var go = new GameObject("Höyry"); var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = softSpr;
            sr.sortingOrder = crate.SortOrder + 3;
            Vector3 at = CapWorld();
            go.transform.position = at;
            Vector3 back = new Vector3(-dir, 0.15f, 0f);
            var pf = new Puff { go = go, sr = sr, v = (back * speed * Random.Range(0.6f, 1.4f)) + new Vector3(0f, Random.Range(0.1f, 0.9f), 0f), life = Random.Range(0.35f, 0.7f), s0 = 0.35f, s1 = Random.Range(1.1f, 1.8f) };
            sr.color = new Color(0.95f, 0.97f, 1f, 0.75f);
            puffs.Add(pf);
        }
    }

    void UpdatePuffs(float dt)
    {
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var pf = puffs[i]; pf.age += dt;
            if (pf.age >= pf.life) { Destroy(pf.go); puffs.RemoveAt(i); continue; }
            float u = pf.age / pf.life;
            pf.go.transform.position += pf.v * dt; pf.v *= 1f - 2.2f * dt;
            pf.go.transform.localScale = Vector3.one * Mathf.Lerp(pf.s0, pf.s1, u);
            var c = pf.sr.color; c.a = 0.75f * (1f - u); pf.sr.color = c;
        }
    }

    void Bowl()
    {
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || bowled.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1.4f || Mathf.Abs(q.y - me.y) > 0.8f) continue;
            bowled.Add(e);
            e.TakeHit(rocketDamage, me.x - dir, true);
            e.Launch(dir * 3.5f, e.maxHealth >= 300 ? 2f : 3.5f); carried.Add(e.transform);   // lento pieneksi: pullo kuljettaa mukanaan
            Clang();
            if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.12f);
        }
        foreach (var p in Players.All.ToArray())
        {
            if (p == null || p.GameOver || bowledHeroes.Contains(p)) continue;
            Vector3 q = p.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1.2f || Mathf.Abs(q.y - me.y) > 0.7f) continue;
            bowledHeroes.Add(p);
            if (p.TakeKnockdown(12, me.x - dir, 3.5f, 3.5f)) carried.Add(p.transform);
            Clang();
        }
        for (int ci = Crate.All.Count - 1; ci >= 0; ci--)
        {
            var c = Crate.All[ci];
            if (c == null || c == crate || !c.breakable || !c.CanBeHit) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) < c.impactRadiusX && Mathf.Abs(q.y - me.y) < 0.8f) { c.Smash(); Clang(0.6f); }
        }
        foreach (var ch in Chair.All.ToArray())
        {
            if (ch == null || !ch.CanBeHit) continue;
            Vector3 q = ch.transform.position;
            if (Mathf.Abs(q.x - me.x) < 1.2f && Mathf.Abs(q.y - me.y) < 0.8f) ch.Knock(me.x - dir, false);
        }
        foreach (var g in All.ToArray())
        {
            if (g == null || g == this || g.state == S.Done) continue;
            Vector3 q = g.transform.position;
            if (Mathf.Abs(q.x - me.x) < 1.3f && Mathf.Abs(q.y - me.y) < 0.9f) { g.Ignite(0.05f); Explode(); return; }
        }
    }

    /// Pystyssä ja koskematon (Nokkamies voi potkaista).
    public bool IsIdle => state == S.Idle;
    /// Vihollinen (Nokkamies) potkaisee pullon: kaatuu, sihisee ja lähtee raketiksi suuntaan dir.
    public void KickBy(float dir)
    {
        if (state != S.Idle) return;
        seenDisturb = crate.Disturb;
        BeginHiss(Mathf.Sign(dir));
    }
    [Tooltip("Räjähdyksen vahinko pomoille (Jerrycan.bossDamage): hero voi käyttää pulloja pomoa vastaan.")]
    public int bossBlastDamage = 60;

    /// Räjähdys tai toisen pullon osuma sytyttää: räjähtää hetken kuluttua.
    public void Ignite(float delay)
    {
        if (state == S.Done) return;
        if (state == S.Idle) { dir = Random.value < 0.5f ? -1f : 1f; BeginHiss(dir); }
        boomAt = Mathf.Min(boomAt < 0f ? float.MaxValue : boomAt, Time.time + delay);
    }

    void Explode()
    {
        if (state == S.Done) return;
        state = S.Done;
        Vector3 me = transform.position;
        // ketjureaktio viereisiin pulloihin
        foreach (var g in All.ToArray())
        {
            if (g == null || g == this || g.state == S.Done) continue;
            Vector3 q = g.transform.position;
            if (Mathf.Abs(q.x - me.x) <= blastRadiusX * 1.4f && Mathf.Abs(q.y - me.y) <= blastRadiusY * 1.4f) g.Ignite(Random.Range(0.15f, 0.4f));
        }
        var go = new GameObject("Kaasupullon räjähdys");
        go.transform.position = me;
        var jc = go.AddComponent<Jerrycan>();
        jc.blastRadiusX = blastRadiusX; jc.blastRadiusY = blastRadiusY; jc.blastDamage = blastDamage; jc.heroDamage = 30; jc.bossDamage = bossBlastDamage;
        if (boomSprs == null) { var tl = Resources.LoadAll<Texture2D>("Kaasupullo"); System.Array.Sort(tl, (x, y) => string.CompareOrdinal(x.name, y.name)); var l = new List<Sprite>(); foreach (var tx in tl) if (tx.name.StartsWith("boom_")) l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 100f)); boomSprs = l.ToArray(); }
        jc.customBoom = boomSprs; jc.customBoomScale = 3.0f;
        jc.DetonateNow();
        if (hiss != null) hiss.Stop();
        foreach (var pf in puffs) if (pf.go != null) Destroy(pf.go);
        puffs.Clear();
        Destroy(gameObject);
    }

    void OnDestroy() { foreach (var pf in puffs) if (pf != null && pf.go != null) Destroy(pf.go); }

    // ---------------- koodilla tehdyt äänet ja kuvat ----------------

    static AudioClip MakeHiss()
    {
        int sr = 22050, n = sr;   // 1 s saumaton silmukka: kohina, josta matalat pois
        var d = new float[n]; var rnd = new System.Random(7); float prev = 0f;
        for (int i = 0; i < n; i++) { float w = (float)(rnd.NextDouble() * 2.0 - 1.0); float hp = w - prev * 0.92f; prev = w; d[i] = hp * 0.5f; }
        int fade = 800; for (int i = 0; i < fade; i++) { float u = i / (float)fade; d[i] *= u; d[n - 1 - i] *= u; }
        var c = AudioClip.Create("kaasusihina", n, 1, sr, false); c.SetData(d, 0); return c;
    }

    static Sprite MakeSoft()
    {
        int s = 64; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float dx = (x - s * 0.5f + 0.5f) / (s * 0.5f), dy = (y - s * 0.5f + 0.5f) / (s * 0.5f);
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)); a = a * a * (3f - 2f * a);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 64f);
    }
}
