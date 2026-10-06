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
    SpriteRenderer body, shadow;
    enum S { Held, Thrown, Falling, Floor }
    S state = S.Held;
    float height, vx, vy, t;
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
    public void TakeBy() { Held = this; state = S.Held; Show(false); }

    /// Lattialla oleva rengas pelaajan edessä (kiinniottonappi).
    public static LifeRing NearbyOnFloor(Vector3 me)
    {
        LifeRing best = null; float bd = 99f;
        foreach (var r in All)
        {
            if (r == null || r.state != S.Floor) continue;
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
        r.height = h; r.vx = dir * 15f; r.vy = 3.2f; r.t = 0f;
        r.hit.Clear();
        r.state = S.Thrown;
        r.Show(true);
    }

    public static void DropHeld(Vector3 at)
    {
        if (Held == null) return;
        var r = Held; Held = null;
        r.transform.position = new Vector3(at.x, at.y, 0f);
        r.height = 1.0f; r.vx = 0f; r.vy = 1f;
        r.state = S.Falling;
        r.Show(true);
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
            vy -= (state == S.Thrown ? 14f : 30f) * dt; height += vy * dt;
            if (state == S.Thrown && HitInPath()) { vx *= -0.25f; vy = 4f; state = S.Falling; }
            if (height <= 0f)
            {
                height = 0f;
                if (Mathf.Abs(vy) > 4f && state == S.Thrown) { vy = -vy * 0.3f; vx *= 0.5f; state = S.Falling; }
                else { state = S.Floor; vx = 0f; }
            }
        }
        if (body == null || sprites == null || sprites.Length == 0) return;
        int f = state == S.Floor ? Mathf.Min(8, sprites.Length - 1) : (int)(t / spinFrameTime) % Mathf.Min(8, sprites.Length);
        body.sprite = sprites[f];
        body.transform.localPosition = new Vector3(0f, height + (state == S.Floor ? 0.25f : 0.7f), 0f);
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + (state == S.Floor ? -20 : 0);
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
