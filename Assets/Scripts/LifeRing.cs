using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelastusrengas: roikkuu telineessä (RingStand), pelaajan käsissä (piirretty heron kuviin), lentää heitettynä
/// pyörien (rengas_kuvat 0–7) ja jää lattialle (kuva 8), josta sen voi nostaa uudelleen.
/// </summary>
public class LifeRing : MonoBehaviour
{
    public static readonly List<LifeRing> All = new List<LifeRing>();
    public static LifeRing Held { get; private set; }

    public Sprite[] sprites;          // rengas_kuvat: 0–7 pyörähdys, 8 lattialla
    public int throwDamage = 18;
    public float spinFrameTime = 0.045f;
    [Tooltip("Renkaan koko lattialla ja lennossa.")]
    public float scale = 1.25f;
    // frisbeelento: vaakatasoiset kuvat (8, 10, 12) vuorotellen ja pieni vaappuminen
    static readonly int[] FlatSpin = { 8, 10, 12, 10 };
    SpriteRenderer body, shadow;
    enum S { Held, Thrown, Falling, Settle, Floor }
    S state = S.Held;
    float height, vx, vy, t;
    int bounces;
    bool flat;            // vaakatasossa (frisbee / pomppii maassa) vai pystyssä pyörien (putoaa)
    float settleT, wobblePhase;
    const float SettleTime = 1.1f;
    readonly HashSet<Enemy> hit = new HashSet<Enemy>();

    public bool OnFloor => state == S.Floor;

    public static LifeRing Create(Sprite[] sprites, Vector3 pos)
    {
        var go = new GameObject("Pelastusrengas");
        go.transform.position = pos;
        var r = go.AddComponent<LifeRing>();
        r.sprites = sprites;
        r.body = new GameObject("Visual").AddComponent<SpriteRenderer>(); r.body.transform.SetParent(go.transform, false);
        r.shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); r.shadow.transform.SetParent(go.transform, false);
        r.shadow.sprite = PlayerController.CreateShadowSprite(); r.shadow.color = new Color(0f, 0f, 0f, 0.35f);
        r.Show(false);
        return r;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }

    /// Pelaaja ottaa renkaan käteen (telineeltä tai lattialta); käsissä rengas on heron kuvissa.
    public void TakeBy() { Held = this; state = S.Held; vx = 0f; Show(false); }

    /// Lattialla oleva rengas pelaajan edessä (kiinniottonappi).
    public static LifeRing NearbyOnFloor(Vector3 me)
    {
        LifeRing best = null; float bd = 99f;
        foreach (var r in All)
        {
            if (r == null || (r.state != S.Floor && r.state != S.Settle)) continue;
            Vector3 q = r.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 1.8f || dy > 0.55f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = r; }
        }
        return best;
    }

    public static void ThrowHeld(Vector3 from, float h, float dir)
    {
        if (Held == null) return;
        var r = Held; Held = null;
        r.transform.position = new Vector3(from.x, from.y, 0f);
        r.height = h; r.vx = dir * 22f; r.vy = 0.6f; r.t = 0f;   // frisbee: lujaa ja matalalla, liitää
        r.bounces = 0; r.flat = true;
        r.hit.Clear();
        r.state = S.Thrown;
        r.Show(true);
    }

    public static void DropHeld(Vector3 at)
    {
        if (Held == null) return;
        var r = Held; Held = null;
        r.transform.position = new Vector3(at.x, at.y, 0f);
        r.height = 1.0f; r.vx = 0f; r.vy = 1f; r.bounces = 0; r.flat = false;
        r.state = S.Falling;
        r.Show(true);
    }

    /// Rengas putoaa annetusta kohdasta ja korkeudesta lattialle (vihun päästä, telineestä).
    public void DropFrom(Vector3 at, float h)
    {
        if (Held == this) Held = null;
        transform.position = new Vector3(at.x, at.y, 0f);
        height = h; vx = 0f; vy = 1.5f; bounces = 0; flat = false;
        state = S.Falling;
        Show(true);
    }

    /// Käsissä oleva rengas lyödään vihun päähän: rengas siirtyy vihulle (sen kuviin), tämä poistuu.
    public static Sprite[] ConsumeHeld()
    {
        if (Held == null) return null;
        var r = Held; Held = null;
        var sp = r.sprites;
        Destroy(r.gameObject);
        return sp;
    }

    void Show(bool on)
    {
        if (body != null) body.enabled = on;
        if (shadow != null) shadow.enabled = on;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        if (state == S.Thrown || state == S.Falling)
        {
            Vector3 p = transform.position; p.x += vx * dt; transform.position = p;
            vy -= (state == S.Thrown ? 6f : 30f) * dt; height += vy * dt;
            if (state == S.Thrown) vx *= 1f - 0.35f * dt;   // ilmanvastus: hidastuu hieman
            if (state == S.Thrown && HitInPath()) { vx *= -0.25f; vy = 4f; state = S.Falling; flat = false; }
            if (state == S.Thrown && height < 3f) RingStand.SmashNear(transform.position, 0.8f, 0.5f);   // lentävä rengas hajottaa telineen
            if (height <= 0f)
            {
                // maahan: pari pomppua (vaakatasossa), sitten liukuu ja vaappuu asettuessaan
                height = 0f; flat = true;
                float kick = 3.6f - bounces * 1.3f;
                if (bounces < 2 && (Mathf.Abs(vy) > 1.5f || state == S.Thrown) && kick > 0.8f)
                {
                    vy = Mathf.Max(Mathf.Abs(vy) * 0.4f, kick); vx *= 0.6f; bounces++; state = S.Falling;
                }
                else { state = S.Settle; settleT = 0f; wobblePhase = 0f; vy = 0f; }
            }
        }
        else if (state == S.Settle)
        {
            Vector3 p = transform.position; p.x += vx * dt; transform.position = p;
            vx = Mathf.MoveTowards(vx, 0f, 9f * dt);
            settleT += dt;
            float k = Mathf.Clamp01(settleT / SettleTime);
            wobblePhase += dt * Mathf.Lerp(9f, 34f, k);              // kuin kolikko: vaappu nopeutuu ja pienenee
            if (settleT >= SettleTime) { state = S.Floor; vx = 0f; }
        }
        if (body == null || sprites == null || sprites.Length == 0) return;
        int f;
        float rot = 0f, lift = 0f;
        bool hasFlat = sprites.Length > 12;
        if (state == S.Floor || !hasFlat && state == S.Settle) f = Mathf.Min(8, sprites.Length - 1);
        else if (state == S.Settle)
        {
            float a = 1f - Mathf.Clamp01(settleT / SettleTime);
            float w = Mathf.Sin(wobblePhase) * a;
            f = Mathf.Abs(w) > 0.55f ? (w > 0f ? 12 : 10) : 8;      // kallistuu puolelta toiselle
            rot = w * 9f;
            lift = Mathf.Abs(w) * 0.06f;
        }
        else if (flat && hasFlat)
        {
            f = FlatSpin[(int)(t / 0.06f) % FlatSpin.Length];
            rot = Mathf.Sin(t * 13f) * 7f;                       // vaappuu
        }
        else f = (int)(t / spinFrameTime) % Mathf.Min(8, sprites.Length);   // pudotessa pyörii
        body.sprite = sprites[f];
        body.transform.localScale = new Vector3(scale, scale, 1f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        // kuvan keskikohta (0.965 yks. kuvan alareunasta) maan yläpuolelle: vaakana lähes maan tasolla, pystyssä säteen verran
        float centre = height + lift + (state == S.Floor || state == S.Settle || flat ? 0.05f : 0.81f);
        body.transform.localPosition = new Vector3(0f, centre - 0.965f * scale, 0f);
        bool low = state == S.Floor || state == S.Settle;
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + (low ? -20 : 0);
        if (shadow != null)
        {
            shadow.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) - 25;
            float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 3f));
            shadow.transform.localScale = new Vector3(1.1f * s, 0.3f * s, 1f);
        }
    }

    bool HitInPath()
    {
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || e.ally || hit.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.9f || Mathf.Abs(q.y - me.y) > 0.5f || height > 3.2f) continue;
            hit.Add(e);
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), true))
            {
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.0f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                return true;
            }
        }
        return false;
    }
}
