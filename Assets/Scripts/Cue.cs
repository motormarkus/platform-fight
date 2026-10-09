using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Biljardikeppi: lepää pöydällä (tai lattialla heiton jälkeen). Pelaaja ottaa sen kiinniottonapilla (kuvat heron omissa
/// keppisarjoissa, oma kuva piilotetaan) ja heittää sen painamalla nappia uudelleen: keppi lentää pyörien, kaataa ensimmäisen
/// osuman ja katkeaa. Lyönneillä kestää 10 osumaa.
/// </summary>
public class Cue : MonoBehaviour
{
    public static readonly List<Cue> All = new List<Cue>();
    public static Cue Held { get; private set; }

    public SpriteRenderer body;
    public int throwDamage = 22;
    [Tooltip("Lyöntejä ennen katkeamista (lyöntikuvien kanssa).")]
    public int durability = 10;
    public AudioClip[] breakSounds;

    enum S { Rest, Held, Thrown, Lying, Gone }
    S state = S.Rest;
    float height, vx, vy, spin, t, groundY;
    int hits;
    Vector3 worldScale;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }
    void Awake() { if (body == null) body = GetComponent<SpriteRenderer>(); }

    /// Kiinniottonappi kepin lähellä (pöydän edessä tai takana, tai lattialla): keppi käteen.
    public static bool TryPickUp(PlayerController p)
    {
        if (Held != null || p == null) return false;
        Vector3 me = p.transform.position;
        Cue best = null; float bd = 99f;
        foreach (var c in All)
        {
            if (c == null || (c.state != S.Rest && c.state != S.Lying)) continue;
            Vector3 q = c.FootPos;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            float rx = c.state == S.Rest ? 2.8f : 1.6f, ry = c.state == S.Rest ? 1.3f : 0.6f;   // pöydällä: koko pöydän leveydeltä
            if (dx > rx || dy > ry) continue;
            if (dx + dy < bd) { bd = dx + dy; best = c; }
        }
        if (best == null) return false;
        best.state = S.Held;
        if (best.body != null) best.body.enabled = false;
        Held = best;
        return true;
    }

    /// Maan kohta (syvyys): pöydällä pöydän juuri, lattialla oma paikka.
    Vector3 FootPos => state == S.Rest && transform.parent != null ? transform.parent.position + new Vector3(transform.localPosition.x * transform.parent.lossyScale.x, 0.37f, 0f) : new Vector3(transform.position.x, groundY, 0f);

    /// Heitto pelaajan kädestä: from = pelaajan jalat, h = käden korkeus.
    public static void ThrowHeld(Vector3 from, float h, float dir)
    {
        if (Held == null) return;
        var c = Held; Held = null;
        c.Detach();
        c.groundY = from.y; c.height = h;
        c.transform.position = new Vector3(from.x + dir * 0.6f, from.y, 0f);
        c.vx = dir * 16f; c.vy = 3.5f; c.spin = -dir * 900f; c.t = 0f;
        c.state = S.Thrown;
        if (c.body != null) { c.body.enabled = true; c.body.color = Color.white; }
    }

    /// Pelaaja sai osuman: keppi putoaa lattialle.
    public static void DropHeld(Vector3 at)
    {
        if (Held == null) return;
        var c = Held; Held = null;
        c.Detach();
        c.groundY = at.y; c.height = 1.2f;
        c.transform.position = new Vector3(at.x, at.y, 0f);
        c.vx = 0f; c.vy = 1f; c.spin = 200f; c.t = 0f;
        c.state = S.Thrown; c.dropped = true;
        if (c.body != null) c.body.enabled = true;
    }
    bool dropped;

    /// Biljardipöytä hajosi: pöydällä oleva keppi valahtaa lattialle.
    public void FallOff()
    {
        if (state != S.Rest) return;
        Vector3 foot = FootPos;
        float h = transform.position.y - foot.y;
        Detach();
        transform.position = new Vector3(transform.position.x, foot.y, 0f);
        groundY = foot.y; height = Mathf.Max(0.5f, h);
        vx = Random.Range(-1f, 1f); vy = 1f; spin = Random.value < 0.5f ? 160f : -160f; t = 0f;
        state = S.Thrown; dropped = true;
    }

    /// Lyönti kepillä osui: kuluu, kolmannesta katkeaa (palauttaa true, jos katkesi).
    public static bool UseHeld(Vector3 at)
    {
        if (Held == null) return false;
        var c = Held;
        c.hits++;
        if (c.hits < c.durability) return false;
        Held = null; c.Detach(); c.transform.position = new Vector3(at.x, at.y, 0f); c.groundY = at.y; c.height = 1.6f;
        c.Snap();
        return true;
    }

    void Detach()
    {
        if (transform.parent == null) return;
        worldScale = transform.lossyScale;
        // pöydän piirtolistalta pois (Obstacle.onTop)
        var ob = transform.parent.GetComponent<Obstacle>();
        if (ob != null && ob.onTop != null) for (int i = 0; i < ob.onTop.Length; i++) if (ob.onTop[i] == body) ob.onTop[i] = null;
        transform.SetParent(null, true);
        transform.localScale = worldScale;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (state != S.Thrown && state != S.Gone) return;
        float dt = Time.deltaTime; t += dt;
        if (state == S.Gone)
        {
            if (body != null) body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t / 0.5f));
            vy -= 25f * dt; height = Mathf.Max(0f, height + vy * dt); transform.position += new Vector3(vx * dt, 0f, 0f);
            transform.Rotate(0f, 0f, spin * dt);
            Place();
            if (t > 0.5f) Destroy(gameObject);
            return;
        }
        transform.position += new Vector3(vx * dt, 0f, 0f);
        vy -= 18f * dt; height += vy * dt;
        transform.Rotate(0f, 0f, spin * dt);
        if (!dropped)
            foreach (var e in Enemy.All.ToArray())
            {
                if (e == null || e.IsDead || !e.isActiveAndEnabled) continue;
                Vector3 q = e.transform.position;
                if (Mathf.Abs(q.x - transform.position.x) > 1.1f || Mathf.Abs(q.y - groundY) > 0.5f || height > 3.2f) continue;
                if (e.TakeHit(throwDamage, transform.position.x - Mathf.Sign(vx), true))
                {
                    HitFx.OnHit(true);
                    HitFx.PlayClip(Resources.Load<AudioClip>("Sfx/keppi_isku"), 1f);
                    HitSpark.Spawn(new Vector3(q.x, q.y + 1.6f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                    Snap();
                    return;
                }
            }
        if (height <= 0f)
        {
            // lattialle kyljelleen, voi nostaa uudelleen
            height = 0f; state = S.Lying; dropped = false;
            transform.rotation = Quaternion.identity;
            HitFx.OnBreak(0.02f);
        }
        Place();
    }

    /// Katkeaa: kaksi palaa lentää ja häipyy.
    void Snap()
    {
        state = S.Gone; t = 0f; vy = 5f; vx *= -0.2f; spin = 600f;
        HitFx.OnBreak(0.05f);
        if (breakSounds != null && breakSounds.Length > 0) HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], 0.4f);
        if (body != null && body.sprite != null)
        {
            // toinen pala: kopio, lentää vastakkaiseen suuntaan
            var g = new GameObject("Kepin pala");
            g.transform.SetPositionAndRotation(transform.position, transform.rotation);
            g.transform.localScale = new Vector3(transform.localScale.x * 0.5f, transform.localScale.y, 1f);
            var r = g.AddComponent<SpriteRenderer>(); r.sprite = body.sprite; r.sortingOrder = body.sortingOrder;
            var c2 = g.AddComponent<Cue>(); c2.body = r; c2.state = S.Gone; c2.groundY = groundY; c2.height = height; c2.vx = -vx + 2f; c2.vy = 6f; c2.spin = -700f;
            transform.localScale = new Vector3(transform.localScale.x * 0.5f, transform.localScale.y, 1f);
        }
    }

    void Place()
    {
        if (body == null) return;
        Vector3 p = transform.position; p.y = groundY + height + 0.08f;
        transform.position = p;
        body.sortingOrder = Mathf.RoundToInt(-groundY * 100f) + 1;
    }
}
