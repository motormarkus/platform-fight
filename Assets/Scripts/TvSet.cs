using UnityEngine;

/// <summary>
/// Telkkari pöydällä: ruudulla pyörii jääkiekko (kuvat 0–9). Kun pöytää lyödään tai se hajoaa, telkkari tippuu
/// lattialle ja hajoaa (kuvat 10–20): kipinät, lasin särkyminen, ruutu sammuu. Romu jää lattialle.
/// </summary>
public class TvSet : MonoBehaviour
{
    public Sprite[] sprites;
    public int showFrames = 10;
    public float showFrameTime = 0.35f;
    public float breakFrameTime = 0.08f;
    public Crate table;
    [Tooltip("Maassa ilman pöytää (kadulla).")]
    public bool onGround;
    [Tooltip("Ruutu pimeänä (kadulla): näytetään hajoamissarjan ensimmäinen, ehjä kuva.")]
    public bool screenOff;
    [Tooltip("Maan korkeus kuvan alla (esim. jalkakäytävä).")]
    public float groundOffset;
    /// Voiko maassa olevaa telkkaria lyödä.
    public bool CanBeHit => onGround && state == S.OnTable;

    /// Pelaajan isku maassa olevaan telkkariin: potku lennättää kaarella, lyönti hajottaa paikalleen.
    public bool TakeHit(float attackerX, bool kick)
    {
        if (!CanBeHit) return false;
        float dir = transform.position.x >= attackerX ? 1f : -1f;
        if (kick)
        {
            vx = dir * Random.Range(5f, 7.5f); vy = Random.Range(4.5f, 6.5f);
            spin = -dir * Random.Range(300f, 500f);
            flyHits.Clear(); state = S.Falling; t = 0f;
        }
        else { state = S.Breaking; t = 0f; Smash(); }
        return true;
    }
    public float tableTop = 1.24f;
    [Tooltip("Kuvan koko (1 = alkuperäinen).")]
    public float scale = 1.3f;
    public AudioClip[] breakSounds;

    enum S { OnTable, Falling, Held, Thrown, Breaking }
    /// Pelaajan kantama telkkari.
    public static TvSet Held { get; private set; }
    public static readonly System.Collections.Generic.List<TvSet> All = new System.Collections.Generic.List<TvSet>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }
    [Tooltip("Heitetyn telkkarin osuma (kaataa).")]
    public int throwDamage = 22;
    int carriedOrder;

    /// Kiinniottonappi telkkaripöydän vieressä: telkkari pään yli.
    public static bool TryPickUp(PlayerController p)
    {
        if (Held != null || p == null) return false;
        Vector3 me = p.transform.position;
        float dir = p.FacingRight ? 1f : -1f;
        foreach (var tv in All)
        {
            if (tv == null || tv.state != S.OnTable) continue;
            Vector3 q = tv.transform.position;
            float dx = (q.x - me.x) * dir;
            if (dx < -0.4f || dx > 1.6f || Mathf.Abs(q.y - me.y) > 0.6f) continue;
            tv.table = null; tv.onGround = false; tv.state = S.Held; tv.t = 0f; tv.rot = 0f;
            Held = tv;
            return true;
        }
        return false;
    }

    public void SetCarried(Vector3 pos, float h, int order)
    {
        if (state != S.Held) return;
        transform.position = pos; height = h; carriedOrder = order;
    }

    public static void ThrowHeld(float vx, float up)
    {
        if (Held == null) return;
        var tv = Held; Held = null;
        tv.vx = vx; tv.vy = up; tv.spin = -Mathf.Sign(vx) * 260f;
        tv.flyHits.Clear(); tv.state = S.Thrown; tv.t = 0f;
    }

    public static void DropHeld()
    {
        if (Held == null) return;
        var tv = Held; Held = null;
        tv.vx = 0f; tv.vy = 0f; tv.spin = 90f; tv.flyHits.Clear(); tv.state = S.Falling; tv.t = 0f;
    }
    S state = S.OnTable;
    SpriteRenderer sr, shadow;
    int seenDisturb;
    float t, height, vy, vx, rot, spin;

    void Awake()
    {
        PlayerController.SortByFrameNumber(sprites);
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        sr = v.AddComponent<SpriteRenderer>();
        var s = new GameObject("Shadow"); s.transform.SetParent(transform, false);
        shadow = s.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.35f);
        shadow.transform.localScale = new Vector3(1.1f, 0.25f, 1f);
    }

    void Start()
    {
        if (table != null) seenDisturb = table.Disturb;
        height = table != null ? tableTop : 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        Vector3 p = transform.position;
        switch (state)
        {
            case S.OnTable:
                if (table == null && onGround) { height = 0f; break; }
                if (table == null || table.Disturb != seenDisturb)
                {
                    // isku pöytään: telkkari lentää iskun suuntaan kaarella ja pyörii; muuten tippuu kallistuen
                    float hitDir = table != null ? table.LastHitDir : 0f;
                    bool kicked = table != null && table.LastHitKick;
                    if (hitDir != 0f && kicked)
                    {
                        vx = hitDir * Random.Range(5f, 7.5f); vy = Random.Range(4.5f, 6.5f);
                        spin = -hitDir * Random.Range(300f, 500f);
                    }
                    else
                    {
                        // lyönti tai muu: tippuu sivulle alas (iskun suuntaan, jos tiedossa)
                        float side = hitDir != 0f ? hitDir : (Random.value < 0.5f ? -1f : 1f);
                        vx = side * Random.Range(0.8f, 1.8f); vy = Random.Range(1.5f, 3f);
                        spin = -side * Random.Range(120f, 220f);
                    }
                    table = null; state = S.Falling; t = 0f;
                    break;
                }
                transform.position = table.transform.position + new Vector3(0f, -0.005f, 0f);
                height = tableTop;
                break;
            case S.Falling:
                p.x += vx * dt; transform.position = p;
                vy -= 30f * dt; height += vy * dt; rot += spin * dt;
                if (HitEnemyInFlight()) { vx *= 0.5f; }
                if (height <= 0f) { height = 0f; rot = 0f; state = S.Breaking; t = 0f; Smash(); }
                break;
            case S.Held:
                break;
            case S.Thrown:
                p.x += vx * dt; transform.position = p;
                vy -= 30f * dt; height += vy * dt; rot += spin * dt;
                if (ThrownHit() || height <= 0f) { height = 0f; rot = 0f; state = S.Breaking; t = 0f; Smash(); }
                break;
            case S.Breaking:
                break;
        }
        Apply();
    }

    [Tooltip("Lentävän telkkarin osuma (pieni vahinko, ei kaada).")]
    public int flyDamage = 10;
    readonly System.Collections.Generic.HashSet<Object> flyHits = new System.Collections.Generic.HashSet<Object>();
    /// Lentävä telkkari osuu matkalla vihuihin (pieni vahinko) ja pöytiin (pöytä nitkahtaa, pullot lentävät).
    bool HitEnemyInFlight()
    {
        if (Mathf.Abs(vx) < 3f || height > 2.6f) return false;
        Vector3 me = transform.position;
        bool any = false;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || flyHits.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.8f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            flyHits.Add(e);
            if (e.TakeHit(flyDamage, me.x - Mathf.Sign(vx), false)) any = true;
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || c == table || !c.CanBeHit || flyHits.Contains(c) || height > 1.6f) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            flyHits.Add(c);
            c.TakeHit(flyDamage, me.x - Mathf.Sign(vx));
            any = true;
        }
        return any;
    }

    /// Heitetty telkkari: osuu vihuun (kaataa) tai pöytään ja hajoaa saman tien.
    bool ThrownHit()
    {
        if (height > 2.8f) return false;
        Vector3 me = transform.position;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.9f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), true)) return true;
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit || height > 1.6f) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > 1f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            c.TakeHit(throwDamage, me.x - Mathf.Sign(vx));
            return true;
        }
        return false;
    }

    void Smash()
    {
        HitFx.OnBreak(0.08f);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.12f, 0.2f);
        int order = Mathf.RoundToInt(-transform.position.y * 100f) + 5;
        for (int i = 0; i < 3; i++)
            HitSpark.Spawn(transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.3f, 0.9f), 0f), i == 0, order);
        if (breakSounds != null && breakSounds.Length > 0)
            HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], 1f);
    }

    void Apply()
    {
        if (sprites == null || sprites.Length == 0) return;
        int n = Mathf.Min(showFrames, sprites.Length);
        if (state == S.Breaking)
            sr.sprite = sprites[Mathf.Min(n + (int)(t / breakFrameTime), sprites.Length - 1)];
        else if (screenOff)
            sr.sprite = sprites[Mathf.Min(n, sprites.Length - 1)];
        else
            sr.sprite = sprites[(int)(Time.time / showFrameTime) % n];
        // kuvaputken värinä: kevyt kirkkauden vaihtelu ruudun pyöriessä
        float g = state == S.Breaking || screenOff ? 1f : 0.93f + 0.07f * Mathf.PerlinNoise(Time.time * 6f, 0f);
        sr.color = new Color(g, g, g, 1f);
        var q = Quaternion.Euler(0f, 0f, rot);
        Vector3 c = new Vector3(0f, 0.45f * scale, 0f);
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.localRotation = q;
        sr.transform.localPosition = new Vector3(0f, groundOffset + height - 0.04f * scale, 0f) + c - q * c;
        shadow.transform.localPosition = new Vector3(0f, groundOffset, 0f);
        shadow.transform.localScale = new Vector3(1.1f * scale, 0.25f * scale, 1f);
        int order = state == S.OnTable && table != null ? table.SortOrder + 2
                  : state == S.Held ? carriedOrder : Mathf.RoundToInt(-transform.position.y * 100f);
        sr.sortingOrder = order;
        shadow.sortingOrder = order - 1;
        shadow.enabled = (state != S.OnTable || onGround) && state != S.Held;
    }
}
