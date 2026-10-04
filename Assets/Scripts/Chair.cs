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
    bool knocked;           // potkaistu tai lyöty: lentää vähän ja jää kyljelleen
    float lieRot;           // kaatuneen tuolin kulma
    /// Lattialla (pystyssä tai kaatuneena): iskut osuvat.
    public bool CanBeHit => state == S.Idle;

    /// Pelaajan isku: lennähtää iskun suuntaan ja kaatuu (potkusta kauemmas).
    public void Knock(float attackerX, bool kick)
    {
        if (state != S.Idle) return;
        float d = transform.position.x >= attackerX ? 1f : -1f;
        vx = d * (kick ? Random.Range(3.5f, 5f) : Random.Range(1.8f, 2.8f));
        vy = kick ? Random.Range(4f, 5.5f) : Random.Range(2.5f, 3.5f);
        spin = -d * Random.Range(300f, 480f);
        knocked = true;
        lieRot = -d * 90f * (Random.value < 0.5f ? 1f : 1f);
        state = S.Falling;
        HitFx.OnBreak(0f);
        if (breakSounds != null && breakSounds.Length > 0) HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], 0.35f);
    }
    S state = S.Idle;
    float height, vx, vy, rot, spin, trailT = -1f, trailNext;
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
        best.state = S.Held; best.rot = 0f;
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
        c.height = h; c.vx = dir * 18f; c.vy = 4.5f; c.spin = -dir * 760f; c.rot = 0f; c.trailT = 0f;
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
            vy -= (state == S.Thrown ? 20f : 30f) * dt; height += vy * dt; rot += spin * dt;   // heitetty kantaa pitkälle
            if (state == S.Thrown && HitInPath()) { Shatter(); return; }
            if (height <= 0f)
            {
                height = 0f;
                if (state == S.Thrown) { Shatter(); return; }
                if (knocked && vy < -4f)
                {
                    vy = -vy * 0.25f; vx *= 0.4f; spin *= 0.4f;   // pomppaa kerran
                    return;
                }
                state = S.Idle; rot = knocked ? lieRot : 0f; knocked = false;
            }
        }
        if (body != null)
        {
            // kiertopiste tuolin keskellä (kuva alareunasta): kaatunut makaa lattialla
            var q = Quaternion.Euler(0f, 0f, rot);
            float half = body.sprite != null ? body.sprite.bounds.extents.y : 0.8f;
            Vector3 c = new Vector3(0f, half, 0f);
            float lift = Mathf.Abs(Mathf.Sin(rot * Mathf.Deg2Rad)) * (body.sprite != null ? body.sprite.bounds.extents.x - half : 0f);
            body.transform.localRotation = q;
            body.transform.localPosition = new Vector3(0f, height + lift, 0f) + c - q * c;
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
        // heiton nopeusjälki: haalistuvat haamukuvat lennon alussa
        if (state == S.Thrown && trailT >= 0f && body != null && body.sprite != null)
        {
            trailT += Time.deltaTime;
            if (trailT < 0.35f && Time.time >= trailNext)
            {
                trailNext = Time.time + 0.025f;
                var g = new GameObject("Tuolin jälki").AddComponent<SpriteRenderer>();
                g.sprite = body.sprite; g.flipX = body.flipX;
                g.transform.SetPositionAndRotation(body.transform.position, body.transform.rotation);
                g.transform.localScale = body.transform.lossyScale;
                g.sortingOrder = body.sortingOrder - 1;
                g.gameObject.AddComponent<FadeOut>().Begin(0.14f, 0.55f);
            }
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

/// Haalistuva haamukuva (esim. heitetyn tuolin nopeusjälki).
public class FadeOut : MonoBehaviour
{
    SpriteRenderer sr; float life, t, a0;
    public void Begin(float duration, float alpha)
    {
        sr = GetComponent<SpriteRenderer>(); life = duration; a0 = alpha;
        if (sr != null) sr.color = new Color(1f, 1f, 1f, alpha);
    }
    void Update()
    {
        t += Time.deltaTime;
        if (sr != null) sr.color = new Color(1f, 1f, 1f, a0 * Mathf.Clamp01(1f - t / life));
        if (t >= life) Destroy(gameObject);
    }
}
