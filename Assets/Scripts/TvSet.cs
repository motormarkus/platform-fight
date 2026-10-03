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
    public float tableTop = 1.24f;
    public AudioClip[] breakSounds;

    enum S { OnTable, Falling, Breaking }
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
            if (e.TakeHit(flyDamage, me.x - Mathf.Sign(vx), false)) { HitFx.OnHit(false); any = true; }
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

    void Smash()
    {
        HitFx.OnHit(true);
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
        else
            sr.sprite = sprites[(int)(Time.time / showFrameTime) % n];
        // kuvaputken värinä: kevyt kirkkauden vaihtelu ruudun pyöriessä
        float g = state == S.Breaking ? 1f : 0.93f + 0.07f * Mathf.PerlinNoise(Time.time * 6f, 0f);
        sr.color = new Color(g, g, g, 1f);
        var q = Quaternion.Euler(0f, 0f, rot);
        Vector3 c = new Vector3(0f, 0.45f, 0f);
        sr.transform.localRotation = q;
        sr.transform.localPosition = new Vector3(0f, height - 0.04f, 0f) + c - q * c;
        int order = state == S.OnTable && table != null ? table.SortOrder + 2 : Mathf.RoundToInt(-transform.position.y * 100f);
        sr.sortingOrder = order;
        shadow.sortingOrder = order - 1;
        shadow.enabled = state != S.OnTable;
    }
}
