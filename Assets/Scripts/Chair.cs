using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tuoli lattialla. Pelaaja ottaa sen käteen (kiinniottonappi), lyö sillä (lyöntinappi: osuessaan tuoli hajoaa kappaleiksi)
/// tai heittää sen (potku): lentää pyörien, kaataa osuessaan ja hajoaa. Kädessä tuoli on piirretty heron kuviin.
/// Kuvat: Rekvisiitta/tuoli.png (sivukuva), Resources/Tuoli/pala_*.png (palat).
/// </summary>
public class Chair : MonoBehaviour
{
    public static readonly List<Chair> All = new List<Chair>();
    public static Chair Held { get; private set; }

    public SpriteRenderer body, shadow;
    public AudioClip[] breakSounds;
    public int throwDamage = 20;

    enum S { Idle, Held, Thrown, Falling }
    S state = S.Idle;
    float height, vx, vy, rot, spin;
    readonly HashSet<Enemy> hit = new HashSet<Enemy>();
    static Sprite[] pieces;

    void OnEnable() { All.Add(this); }
    void Awake() { if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite(); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }

    /// Kiinniottonappi tuolin vieressä (edessä): tuoli käteen.
    public static bool TryPickUp(PlayerController p)
    {
        if (Held != null || p == null) return false;
        Vector3 me = p.transform.position;
        Chair best = null; float bd = 99f;
        foreach (var c in All)
        {
            if (c == null || c.state != S.Idle) continue;
            Vector3 q = c.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 1.5f || dy > 0.55f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = c; }
        }
        if (best == null) return false;
        best.state = S.Held;
        best.Show(false);
        Held = best;
        return true;
    }

    /// Kädessä olevalla tuolilla osuttiin: hajoaa (heron kuvissa sirpaleet), palat jäävät lattialle.
    public static void BreakHeld(Vector3 at, float dir)
    {
        if (Held == null) return;
        var c = Held; Held = null;
        c.PlaySound();
        SpawnPieces(at, 1.6f, dir, 9);
        Destroy(c.gameObject);
    }

    /// Heitto pelaajan kädestä.
    public static void ThrowHeld(Vector3 from, float h, float dir)
    {
        if (Held == null) return;
        var c = Held; Held = null;
        c.transform.position = new Vector3(from.x, from.y, 0f);
        c.height = h; c.vx = dir * 13f; c.vy = 3.5f; c.spin = -dir * 620f; c.rot = 0f;
        c.hit.Clear();
        c.state = S.Thrown;
        c.Show(true);
    }

    /// Pelaaja sai osuman: tuoli putoaa ehjänä.
    public static void DropHeld(Vector3 at)
    {
        if (Held == null) return;
        var c = Held; Held = null;
        c.transform.position = new Vector3(at.x, at.y, 0f);
        c.height = 1.2f; c.vx = 0f; c.vy = 1f; c.spin = 0f; c.rot = 0f;
        c.state = S.Falling;
        c.Show(true);
    }

    void Show(bool on)
    {
        if (body != null) body.enabled = on;
        if (shadow != null) shadow.enabled = on;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (state == S.Thrown || state == S.Falling)
        {
            Vector3 p = transform.position; p.x += vx * dt; transform.position = p;
            vy -= 30f * dt; height += vy * dt; rot += spin * dt;
            if (state == S.Thrown && HitInPath()) { Shatter(); return; }
            if (height <= 0f)
            {
                height = 0f;
                if (state == S.Thrown) { Shatter(); return; }
                state = S.Idle; rot = 0f;
            }
        }
        if (body != null)
        {
            body.transform.localPosition = new Vector3(0f, height, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
        if (shadow != null)
        {
            shadow.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) - 1;
            float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 3f));
            shadow.transform.localScale = new Vector3(1.1f * s, 0.3f * s, 1f);
        }
    }

    bool HitInPath()
    {
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || hit.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1.0f || Mathf.Abs(q.y - me.y) > 0.5f || height > 3.2f) continue;
            hit.Add(e);
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), true))
            {
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.2f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                return true;
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1.0f || Mathf.Abs(q.y - me.y) > 0.5f || height > 1.6f) continue;
            c.TakeHit(10, me.x - Mathf.Sign(vx));
            return true;
        }
        return false;
    }

    void Shatter()
    {
        PlaySound();
        HitFx.OnBreak(0.08f);
        SpawnPieces(transform.position, Mathf.Max(0.3f, height), Mathf.Sign(vx == 0f ? 1f : vx), 10);
        Destroy(gameObject);
    }

    void PlaySound()
    {
        if (breakSounds != null && breakSounds.Length > 0) HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], Random.Range(0.85f, 1f));
    }

    static void SpawnPieces(Vector3 at, float h, float dir, int n)
    {
        if (pieces == null) pieces = Resources.LoadAll<Sprite>("Tuoli");
        if (pieces == null || pieces.Length == 0) return;
        for (int i = 0; i < n; i++)
            FoodDebris.Spawn(new[] { pieces[Random.Range(0, pieces.Length)] }, at + new Vector3(dir * Random.Range(0.4f, 1.2f), 0f, 0f), h, 1f,
                dir * Random.Range(0.5f, 3.5f) + Random.Range(-1f, 1f), Random.Range(2f, 6f), Random.Range(-0.6f, 0.4f), Random.Range(-700f, 700f), false, 0f);
    }
}
