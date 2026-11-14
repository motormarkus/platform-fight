using UnityEngine;

/// <summary>
/// Vegasin hääkappelin morsian (valikko 389): seisoo sulhasen vieressä kimppu sylissä, kunnes rähinä (SalsaBrawl) alkaa. Silloin hän heittää kimpun olkansa yli taakse (vasemmalle),
/// kimppu osuu Puliukkoa päähän ja hajoaa kappaleiksi, ja morsian pakenee ovelle ja katoaa. Ei taistelija eikä lyötävissä.
/// Kuvat: Assets/Sprites/Morsian (Tools/BrideAssets). Hahmo katsoo kuvissa oikealle; pako vasemmalle peilataan.
/// </summary>
public class ChapelBride : MonoBehaviour
{
    public SalsaBrawl brawl;
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] idleSprites, throwSprites, runSprites, burstSprites;
    public Sprite bouquetSprite;
    public float idleFrameTime = 1f / 12f, throwFrameTime = 1f / 12f, runFrameTime = 1f / 24f, burstFrameTime = 0.09f;
    [Tooltip("Heittokuvan indeksi, jossa kimppu irtoaa kädestä.")]
    public int releaseFrame = 9;
    public float runSpeed = 6.5f;
    [Tooltip("Ovi, johon morsian pakenee (x); siellä hän katoaa.")]
    public float exitX;
    [Tooltip("Kimpun korkeus maailmassa.")]
    public float bouquetWorldHeight = 0.7f;
    [Tooltip("Kädet: kimppu lähtee morsiamen paikasta tämän verran (x vasemmalle, y ylös).")]
    public Vector2 handOffset = new Vector2(-0.5f, 2.9f);
    public float flightTime = 0.65f, flightArc = 1.6f;
    [Tooltip("Heitettävä Puliukko: jos tyhjä, lähin Puliukko kappelissa.")]
    public Enemy target;

    enum Mode { Idle, Throw, Run, Gone }
    Mode mode = Mode.Idle;
    float t, px;
    bool released;
    double x;

    // lentävä kimppu ja hajoamisanimaatio
    SpriteRenderer bouq, burst;
    float flyT = -1f, burstT = -1f;
    Vector3 flyFrom, flyTo; Enemy flyTarget;

    void Awake()
    {
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        x = transform.position.x;
    }

    void Update()
    {
        if (body == null || idleSprites == null || idleSprites.Length == 0) return;
        if (area != null && Area.Current != area) return;
        float dt = Time.deltaTime;
        t += dt;
        Vector3 p = transform.position;
        switch (mode)
        {
            case Mode.Idle:
                body.flipX = false;   // katsoo oikealle (vihkijää kohti)
                body.sprite = idleSprites[(int)(t / idleFrameTime) % idleSprites.Length];
                if (brawl != null && brawl.Started) { mode = Mode.Throw; t = 0f; released = false; PickTarget(); }
                break;
            case Mode.Throw:
            {
                int f = (int)(t / throwFrameTime);
                if (f >= throwSprites.Length) { mode = Mode.Run; t = 0f; break; }
                body.sprite = throwSprites[f];
                if (!released && f >= releaseFrame) { released = true; Release(); }
                break;
            }
            case Mode.Run:
            {
                body.flipX = exitX < p.x;   // kuvat juoksevat oikealle: vasemmalle peilataan
                body.sprite = runSprites[(int)(t / runFrameTime) % runSprites.Length];
                double step = runSpeed * (double)dt, diff = exitX - x;
                if (System.Math.Abs(diff) <= step) { x = exitX; mode = Mode.Gone; body.enabled = false; if (shadow != null) shadow.enabled = false; }
                else x += System.Math.Sign(diff) * step;
                p.x = (float)x; transform.position = p;
                break;
            }
        }
        int order = Mathf.RoundToInt(-p.y * 100f);
        body.sortingOrder = order;
        if (shadow != null) shadow.sortingOrder = order - 1;
        UpdateBouquet();
    }

    void PickTarget()
    {
        if (target != null && !target.IsDead) return;
        target = null; float best = float.MaxValue;
        foreach (var e in Enemy.All)
        {
            if (e == null || e.IsDead || e.displayName != "Puliukko" || !e.isActiveAndEnabled) continue;
            if (area != null && (e.transform.position.x < area.walkMinX - 2f || e.transform.position.x > area.walkMaxX + 2f)) continue;
            float d = Mathf.Abs(e.transform.position.x - transform.position.x);
            if (d < best) { best = d; target = e; }
        }
    }

    void Release()
    {
        if (bouquetSprite == null) return;
        if (bouq == null)
        {
            var go = new GameObject("Kimppu (lentää)"); bouq = go.AddComponent<SpriteRenderer>(); bouq.sprite = bouquetSprite;
            float k = bouquetWorldHeight / Mathf.Max(0.01f, bouquetSprite.bounds.size.y); go.transform.localScale = Vector3.one * k;
        }
        PickTarget();
        flyTarget = target;
        flyFrom = transform.position + new Vector3(handOffset.x, handOffset.y, 0f);
        Vector3 hit = flyTarget != null ? HeadOf(flyTarget) : transform.position + new Vector3(-4f, 1.8f, 0f);
        flyTo = hit;
        flyT = 0f; bouq.enabled = true;
    }

    Vector3 HeadOf(Enemy e)
    {
        var r = e.body != null ? e.body.bounds : new Bounds(e.transform.position + Vector3.up * 2f, Vector3.one);
        return new Vector3(r.center.x, r.max.y - 0.4f, 0f);
    }

    void UpdateBouquet()
    {
        float dt = Time.deltaTime;
        if (flyT >= 0f && bouq != null)
        {
            flyT += dt / Mathf.Max(0.1f, flightTime);
            Vector3 to = flyTarget != null && !flyTarget.IsDead ? HeadOf(flyTarget) : flyTo;
            float u = Mathf.Clamp01(flyT);
            Vector3 pos = Vector3.Lerp(flyFrom, to, u); pos.y += Mathf.Sin(u * Mathf.PI) * flightArc;
            bouq.transform.position = pos; bouq.transform.rotation = Quaternion.Euler(0f, 0f, u * 540f);
            float footY = flyTarget != null ? flyTarget.transform.position.y : transform.position.y;
            bouq.sortingOrder = Mathf.RoundToInt(-footY * 100f) + 400;
            if (flyT >= 1f)
            {
                flyT = -1f; bouq.enabled = false;
                if (flyTarget != null && !flyTarget.IsDead) flyTarget.TakeHit(6, to.x + 1f, false);   // kimppu pään päälle: pieni vahinko ja säikähdys
                StartBurst(to, footY);
            }
        }
        if (burstT >= 0f && burst != null && burstSprites != null && burstSprites.Length > 0)
        {
            burstT += dt;
            int f = 1 + (int)(burstT / burstFrameTime);   // kuva 0 = ehjä kimppu: hajoaminen alkaa kuvasta 1
            if (f >= burstSprites.Length) { burstT = -1f; burst.enabled = false; }
            else burst.sprite = burstSprites[f];
        }
    }

    void StartBurst(Vector3 at, float footY)
    {
        if (burstSprites == null || burstSprites.Length < 2) return;
        if (burst == null)
        {
            var go = new GameObject("Kimppu (hajoaa)"); burst = go.AddComponent<SpriteRenderer>();
            float cell = burstSprites[0].bounds.size.y;
            go.transform.localScale = Vector3.one * (bouquetWorldHeight * 2.3f / Mathf.Max(0.01f, cell));   // hajonnut kimppu kattaa laajemman alueen kuin ehjä
        }
        burst.transform.position = at; burst.sortingOrder = Mathf.RoundToInt(-footY * 100f) + 401;
        burst.sprite = burstSprites[1]; burst.enabled = true; burstT = 0f;
    }
}
