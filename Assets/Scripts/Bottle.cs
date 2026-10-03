using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pullo baaripöydällä. Kun pöytää lyödään, osa pulloista lentää, osa kaatuu ja tippuu lattialle, osa jää heilumaan.
/// Lattialle osuessa osa hajoaa (kuvat 1–6), osa jää ehjänä kyljelleen. Ehjän pullon voi poimia käteen
/// (kiinniottonappi) ja heittää lyöntinapilla: osuu vihuun, kaataa sen ja hajoaa.
/// Kuvat: pullo_*.png 128x96, 0 pystyssä, 1–6 hajoaa.
/// </summary>
public class Bottle : MonoBehaviour
{
    public static readonly List<Bottle> All = new List<Bottle>();
    /// Pelaajan kädessä oleva pullo (null = ei pulloa).
    public static Bottle Held { get; private set; }

    public Sprite[] sprites;
    [Tooltip("Pöytä, jolla pullo seisoo (null = lattialla).")]
    public Crate table;
    [Tooltip("Paikka pöydällä: x pöydän keskeltä, korkeus pöydän pinnasta (yksikköä).")]
    public float tableX, tableTop = 1.24f;
    public float frameTime = 0.06f;
    public int throwDamage = 18;
    public float throwSpeed = 15f;

    enum S { OnTable, Wobble, Falling, Lying, Held, Thrown, Breaking }
    S state = S.OnTable;
    SpriteRenderer sr, shadow;
    PlayerController pc;
    int seenDisturb;
    float t, height, vy, vx, rot, spin, wobbleT;
    bool fastFall;
    readonly HashSet<Object> hitList = new HashSet<Object>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }

    void Awake()
    {
        PlayerController.SortByFrameNumber(sprites);
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        sr = v.AddComponent<SpriteRenderer>();
        var s = new GameObject("Shadow"); s.transform.SetParent(transform, false);
        shadow = s.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(0.35f, 0.12f, 1f);
    }

    void Start()
    {
        pc = FindFirstObjectByType<PlayerController>();
        if (table != null) { seenDisturb = table.Disturb; height = tableTop; }
        else { state = S.Lying; rot = 90f; }
    }

    // ---------------- pelaajan käsi ----------------

    /// Kiinniottonappi: poimi lähin ehjä lattialla oleva pullo.
    public static bool TryPickUp(PlayerController p)
    {
        if (Held != null || p == null) return false;
        Vector3 me = p.transform.position;
        Bottle best = null; float bd = 99f;
        foreach (var b in All)
        {
            if (b == null || b.state != S.Lying) continue;
            Vector3 q = b.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 1.1f || dy > 0.5f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = b; }
        }
        if (best == null) return false;
        best.state = S.Held; best.t = 0f;
        Held = best;
        HitFx.PlayPickup(false);
        return true;
    }

    /// Lyöntinappi pullo kädessä: heitto eteen.
    public static bool ThrowHeld(PlayerController p)
    {
        if (Held == null || p == null) return false;
        var b = Held; Held = null;
        float dir = p.FacingRight ? 1f : -1f;
        b.transform.position = p.transform.position + new Vector3(dir * 0.6f, 0f, 0f);
        b.vx = dir * b.throwSpeed; b.vy = 1.5f; b.height = 1.9f;
        b.spin = -dir * 900f; b.hitList.Clear();
        b.state = S.Thrown; b.t = 0f;
        return true;
    }

    /// Pelaaja saa osuman: pullo putoaa kädestä.
    public static void DropHeld()
    {
        if (Held == null) return;
        var b = Held; Held = null;
        b.vx = Random.Range(-1.5f, 1.5f); b.vy = 2f; b.spin = Random.Range(-500f, 500f);
        b.fastFall = false; b.state = S.Falling; b.t = 0f;
    }

    // ---------------- päivitys ----------------

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        Vector3 p = transform.position;
        switch (state)
        {
            case S.OnTable:
            case S.Wobble:
                if (table == null || table.Disturb != seenDisturb)
                {
                    bool gone = table == null || !table.Intact;   // pöytä hajosi tai nostettiin
                    if (table != null) seenDisturb = table.Disturb;
                    React(gone);
                    break;
                }
                p = table.transform.position + new Vector3(tableX, 0f, 0f);
                p.y -= 0.01f;
                transform.position = p;
                height = tableTop;
                if (state == S.Wobble)
                {
                    rot = Mathf.Sin(t * 28f) * 14f * Mathf.Max(0f, 1f - t / 0.8f);
                    if (t > 0.8f) { state = S.OnTable; rot = 0f; }
                }
                break;

            case S.Falling:
                p.x += vx * dt; vx = Mathf.MoveTowards(vx, 0f, 0.5f * dt);
                transform.position = p;
                vy -= 30f * dt; height += vy * dt; rot += spin * dt;
                if (height <= 0f)
                {
                    height = 0f;
                    float breakChance = fastFall ? 0.7f : 0.4f;
                    if (Random.value < breakChance) Shatter();
                    else { state = S.Lying; rot = Random.value < 0.5f ? 90f : -90f; t = 0f; }   // jää ehjänä kyljelleen
                }
                break;

            case S.Lying:
                break;

            case S.Held:
                if (pc == null) { DropHeld(); break; }
                float dir = pc.FacingRight ? 1f : -1f;
                transform.position = pc.transform.position + new Vector3(dir * 0.5f, -0.01f, 0f);
                height = 1.75f + pc.AirHeight;
                rot = -dir * 25f;
                break;

            case S.Thrown:
                p.x += vx * dt;
                transform.position = p;
                vy -= 6f * dt; height = Mathf.Max(0f, height + vy * dt);
                rot += spin * dt;
                if (HitInPath() || height <= 0.05f || t > 1.0f) Shatter();
                break;

            case S.Breaking:
                if (t > 1.6f) Destroy(gameObject);
                break;
        }
        ApplyVisual();
    }

    /// Pöytää lyötiin: lentää, kaatuu ja tippuu tai jää heilumaan.
    void React(bool tableGone)
    {
        float r = Random.value;
        float side = Random.value < 0.5f ? -1f : 1f;
        t = 0f;
        if (!tableGone && r < 0.3f) { state = S.Wobble; return; }
        if (r < 0.65f)
        {
            // lentää
            vx = side * Random.Range(2f, 4.5f); vy = Random.Range(3.5f, 6f);
            spin = -side * Random.Range(400f, 900f); fastFall = true;
        }
        else
        {
            // kaatuu ja tippuu reunalta
            vx = side * Random.Range(0.8f, 1.6f); vy = Random.Range(0.5f, 1.5f);
            spin = -side * Random.Range(150f, 300f); fastFall = false;
        }
        table = null;
        state = S.Falling;
    }

    bool HitInPath()
    {
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || hitList.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.7f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            hitList.Add(e);
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), true))
            {
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.2f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                return true;
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit || hitList.Contains(c)) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.9f || Mathf.Abs(q.y - me.y) > 0.5f || height > 1.6f) continue;
            hitList.Add(c);
            c.TakeHit(10, me.x - Mathf.Sign(vx));
            return true;
        }
        return false;
    }

    void Shatter()
    {
        state = S.Breaking; t = 0f; height = 0f; rot = 0f;
        HitFx.OnHit(false);
        BarStain.SpawnAt(transform.position);
    }

    void ApplyVisual()
    {
        if (sprites == null || sprites.Length == 0) return;
        if (state == S.Breaking)
        {
            sr.sprite = sprites[Mathf.Min(1 + (int)(t / frameTime), sprites.Length - 1)];
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01((1.6f - t) / 0.5f));
        }
        else sr.sprite = sprites[0];
        // kierto pullon keskikohdan ympäri (kuva alareunan keskellä, pullo n. 0.58 yks korkea)
        var q = Quaternion.Euler(0f, 0f, state == S.Breaking ? 0f : rot);
        Vector3 c = new Vector3(0f, 0.3f, 0f);
        sr.transform.localRotation = q;
        sr.transform.localPosition = new Vector3(0f, height - 0.04f, 0f) + c - q * c;
        int order;
        if ((state == S.OnTable || state == S.Wobble) && table != null) order = table.SortOrder + 1;
        else if (state == S.Held && pc != null) order = Mathf.RoundToInt(-pc.transform.position.y * 100f) + 2;
        else order = Mathf.RoundToInt(-transform.position.y * 100f);
        sr.sortingOrder = order;
        shadow.sortingOrder = order - 1;
        shadow.enabled = state != S.OnTable && state != S.Wobble && state != S.Breaking && state != S.Held;
    }
}
