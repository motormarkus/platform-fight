using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Irrotettu viemärin kansi: hero nostaa sen kaivon päältä (tai lattialta), kantaa (kansi on heron kuvissa) ja heittää frisbeenä.
/// Heitetty kansi lentää matalalla, osuu vihuihin (läpäisee muutaman), putoaa ja jää lattialle, josta sen voi nostaa uudelleen.
/// </summary>
public class Lid : MonoBehaviour
{
    public static readonly List<Lid> All = new List<Lid>();
    public static Lid Held { get; private set; }

    public Sprite sprite;
    public Area area;
    public float sizeScale = 1f;
    public int throwDamage = 30;
    public float throwSpeed = 17f;
    public float range = 12f;
    public int maxHits = 3;
    public AudioClip[] sounds;
    static int lastSound = -1;

    enum S { Held, Thrown, Falling, Floor }
    S state = S.Held;
    SpriteRenderer body, shadow;
    float height, vx, vy, t, travelled;
    int bounces, hits;
    readonly HashSet<Enemy> hit = new HashSet<Enemy>();

    public static Lid Create(Sprite sprite, Area area, Vector3 pos, AudioClip[] sounds)
    {
        var go = new GameObject("Viemärin kansi (irti)");
        go.transform.position = pos;
        var l = go.AddComponent<Lid>();
        l.sprite = sprite; l.area = area; l.sounds = sounds;
        l.body = new GameObject("Visual").AddComponent<SpriteRenderer>(); l.body.transform.SetParent(go.transform, false);
        l.shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); l.shadow.transform.SetParent(go.transform, false);
        l.shadow.sprite = PlayerController.CreateShadowSprite(); l.shadow.color = new Color(0f, 0f, 0f, 0.35f);
        l.body.sprite = sprite;
        l.Show(false);
        return l;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }

    /// Pelaaja ottaa kannen käteen (kansi on heron kuvissa, tämä olio piiloon).
    public void TakeBy() { Held = this; state = S.Held; vx = 0f; Show(false); }

    /// Lattialla oleva kansi pelaajan edessä.
    public static Lid NearbyOnFloor(Vector3 me)
    {
        Lid best = null; float bd = 99f;
        foreach (var l in All)
        {
            if (l == null || l.state != S.Floor || (l.area != null && l.area != Area.Current)) continue;
            Vector3 q = l.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 2.2f || dy > 0.8f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = l; }
        }
        return best;
    }

    public static void ThrowHeld(Vector3 from, float h, float dir)
    {
        if (Held == null) return;
        var l = Held; Held = null;
        l.transform.position = new Vector3(from.x, from.y, 0f);
        l.height = h; l.vx = dir * l.throwSpeed; l.vy = 0.5f; l.t = 0f; l.travelled = 0f;
        l.bounces = 0; l.hits = 0; l.hit.Clear();
        l.state = S.Thrown;
        l.Show(true);
    }

    public static void DropHeld(Vector3 at)
    {
        if (Held == null) return;
        var l = Held; Held = null;
        l.transform.position = new Vector3(at.x, at.y, 0f);
        l.height = 1.2f; l.vx = 0f; l.vy = 1f; l.bounces = 0;
        l.state = S.Falling;
        l.Show(true);
    }

    void Show(bool on)
    {
        if (body != null) body.enabled = on;
        if (shadow != null) shadow.enabled = on;
    }

    void PlaySound()
    {
        if (sounds == null || sounds.Length == 0) return;
        int i = Random.Range(0, sounds.Length);
        if (sounds.Length > 1 && i == lastSound) i = (i + 1) % sounds.Length;
        lastSound = i;
        if (sounds[i] != null && Area.IsHere(transform.position)) HitFx.PlayClip(sounds[i], 0.8f);
    }

    void Update()
    {
        if (state == S.Held) return;
        bool visible = area == null || Area.Current == area;
        Show(visible);
        float dt = Time.deltaTime;
        t += dt;
        if (state == S.Thrown)
        {
            Vector3 p = transform.position;
            float dx = vx * dt; p.x += dx; travelled += Mathf.Abs(dx);
            if (area != null) p.x = Mathf.Clamp(p.x, area.walkMinX + 0.3f, area.walkMaxX - 0.3f);
            transform.position = p;
            vy -= 1.2f * dt; height = Mathf.Max(0.4f, height + vy * dt);   // liitää lähes vaakaan
            HitInPath();
            if (travelled >= range || hits >= maxHits || (area != null && (p.x <= area.walkMinX + 0.31f || p.x >= area.walkMaxX - 0.31f)))
            { state = S.Falling; vy = 1.5f; vx *= 0.3f; }
        }
        else if (state == S.Falling)
        {
            Vector3 p = transform.position; p.x += vx * dt; transform.position = p;
            vx = Mathf.MoveTowards(vx, 0f, 5f * dt);
            vy -= 30f * dt; height += vy * dt;
            if (height <= 0f)
            {
                height = 0f;
                if (bounces < 1 && Mathf.Abs(vy) > 3f) { vy = Mathf.Abs(vy) * 0.35f; bounces++; PlaySound(); }
                else { state = S.Floor; vx = 0f; vy = 0f; PlaySound(); }
            }
        }
        if (body == null || sprite == null) return;
        bool flying = state == S.Thrown;
        float rot = flying ? Mathf.Sin(t * 22f) * 8f : 0f;
        float sc = (flying ? 0.85f : 1f) * sizeScale;
        body.transform.localScale = new Vector3(sc, sc, 1f);
        body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        body.transform.localPosition = new Vector3(0f, height + 0.55f * sc, 0f);
        bool low = state == S.Floor;
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + (low ? -20 : 0);
        if (shadow != null)
        {
            shadow.sortingOrder = body.sortingOrder - 1;
            float s = Mathf.Lerp(1f, 0.6f, Mathf.Clamp01(height / 3f));
            shadow.transform.localScale = new Vector3(2.0f * s, 0.5f * s, 1f);
        }
    }

    void HitInPath()
    {
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || e.ally || hit.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1.2f || Mathf.Abs(q.y - me.y) > 0.65f || height > 3.4f) continue;
            hit.Add(e);
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), true))
            {
                hits++;
                vx *= 0.8f;
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.0f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                PlaySound();
            }
        }
    }
}
