using UnityEngine;

/// <summary>
/// Baarin vakioasiakas (ei tappele): seisoo selin tiskillä, kääntyy välillä tupakoimaan ja takaisin,
/// ja käy välillä suutelemassa prätkäjätkää (kumpikin piilotetaan, suudelmakuva tilalle).
/// Kun tappelu alkaa, suudelma katkeaa ja hän kävelee ulos ovesta.
/// Kuvat katsovat oikealle.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BarRegular : MonoBehaviour
{
    public Sprite[] counterIdle, turn, smoking, walk, kiss;
    public float frameTime = 0.1f, walkFrameTime = 0.085f, speed = 1.2f;
    public Vector2 counterSpot;
    [Tooltip("Ulko-ovi, josta poistutaan tappelun alkaessa.")]
    public Vector2 exitSpot;
    [Tooltip("Prätkäjätkä, jota suudellaan (piilotetaan suudelman ajaksi). Tyhjä = ei suudelmaa.")]
    public Enemy partner;
    public SpriteRenderer kissRenderer;
    [Tooltip("Suudelmakuvan paikka suhteessa prätkäjätkään (yksikköä).")]
    public Vector2 kissOffset = new Vector2(-0.55f, 0f);
    public Vector2 counterTime = new Vector2(6f, 12f), smokeTime = new Vector2(6f, 10f), kissTime = new Vector2(7f, 11f);
    [Range(0f, 1f)] public float kissChance = 0.35f;
    [Tooltip("Tappelun tunnistus: kun joku näistä herää tai saa osuman.")]
    public BarBrawl brawl;

    enum S { Counter, TurnOut, Smoke, TurnIn, WalkToKiss, Kiss, WalkBack, Leave, Gone }
    S st = S.Counter;
    float t, left;
    SpriteRenderer sr;
    bool flip;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(counterIdle); PlayerController.SortByFrameNumber(turn);
        PlayerController.SortByFrameNumber(smoking); PlayerController.SortByFrameNumber(walk); PlayerController.SortByFrameNumber(kiss);
    }

    void Start()
    {
        transform.position = counterSpot;
        left = Random.Range(counterTime.x, counterTime.y);
        if (kissRenderer != null) kissRenderer.enabled = false;
    }

    static bool Has(Sprite[] a) => a != null && a.Length > 0;
    Sprite Loop(Sprite[] a, float ft, bool pingPong = true)
    {
        int n = a.Length; if (n == 1) return a[0];
        int i = (int)(t / ft);
        if (!pingPong) return a[i % n];
        int p = 2 * n - 2; i %= p; return a[i < n ? i : p - i];
    }

    bool FightOn()
    {
        if (brawl != null && brawl.Started) return true;
        return Enemy.FightNear(transform.position, 6f);
    }

    void Enter(S s) { st = s; t = 0f; }

    void Update()
    {
        float dt = Time.deltaTime; t += dt;
        if (st != S.Leave && st != S.Gone && FightOn()) { EndKiss(); Enter(S.Leave); }
        switch (st)
        {
            case S.Counter:
                flip = false;
                if (Has(counterIdle)) sr.sprite = Loop(counterIdle, frameTime * 1.4f);
                left -= dt;
                if (left <= 0f)
                {
                    if (partner != null && Has(kiss) && partner.isActiveAndEnabled && !partner.IsAwake && Random.value < kissChance) Enter(S.WalkToKiss);
                    else Enter(S.TurnOut);
                }
                break;
            case S.TurnOut:
                if (!Has(turn)) { Enter(S.Smoke); left = Random.Range(smokeTime.x, smokeTime.y); break; }
                sr.sprite = turn[Mathf.Min((int)(t / frameTime), turn.Length - 1)];
                if (t >= turn.Length * frameTime) { Enter(S.Smoke); left = Random.Range(smokeTime.x, smokeTime.y); }
                break;
            case S.Smoke:
                if (Has(smoking)) sr.sprite = Loop(smoking, frameTime * 1.2f, false);
                left -= dt;
                if (left <= 0f) Enter(S.TurnIn);
                break;
            case S.TurnIn:
                if (!Has(turn)) { Enter(S.Counter); left = Random.Range(counterTime.x, counterTime.y); break; }
                sr.sprite = turn[Mathf.Max(turn.Length - 1 - (int)(t / frameTime), 0)];
                if (t >= turn.Length * frameTime) { Enter(S.Counter); left = Random.Range(counterTime.x, counterTime.y); }
                break;
            case S.WalkToKiss:
            {
                if (partner == null || partner.IsAwake) { Enter(S.WalkBack); break; }
                Vector2 goal = (Vector2)partner.transform.position + new Vector2(-1.0f, -0.05f);
                if (WalkTo(goal, dt)) { StartKiss(); Enter(S.Kiss); left = Random.Range(kissTime.x, kissTime.y); }
                break;
            }
            case S.Kiss:
                if (kissRenderer != null && Has(kiss))
                {
                    // alku (lähestyminen) kerran, sitten suudelma edestakaisin
                    int intro = Mathf.Min(10, kiss.Length - 1);
                    int i = (int)(t / frameTime);
                    if (i < intro) kissRenderer.sprite = kiss[i];
                    else { int n = kiss.Length - intro, p = 2 * n - 2; int j = p > 0 ? (i - intro) % p : 0; kissRenderer.sprite = kiss[intro + (j < n ? j : p - j)]; }
                }
                left -= dt;
                if (left <= 0f) { EndKiss(); Enter(S.WalkBack); }
                break;
            case S.WalkBack:
                if (WalkTo(counterSpot, dt)) { Enter(S.Counter); left = Random.Range(counterTime.x, counterTime.y); }
                break;
            case S.Leave:
                if (WalkTo(exitSpot, dt, 1.4f)) { Enter(S.Gone); sr.enabled = false; }
                break;
        }
        sr.flipX = flip;
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    bool WalkTo(Vector2 goal, float dt, float mul = 1f)
    {
        Vector2 p = transform.position, d = goal - p;
        if (Mathf.Abs(d.x) > 0.05f) flip = d.x < 0f;
        if (Has(walk)) sr.sprite = Loop(walk, walkFrameTime / mul, false);
        float step = speed * mul * dt;
        if (d.magnitude <= step) { transform.position = goal; return true; }
        transform.position = p + d.normalized * step;
        return false;
    }

    void StartKiss()
    {
        if (partner == null || kissRenderer == null) return;
        sr.enabled = false;
        foreach (var r in partner.GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        kissRenderer.transform.position = partner.transform.position + (Vector3)kissOffset;
        kissRenderer.sortingOrder = Mathf.RoundToInt(-partner.transform.position.y * 100f);
        kissRenderer.enabled = true;
    }

    void EndKiss()
    {
        if (kissRenderer != null && kissRenderer.enabled)
        {
            kissRenderer.enabled = false;
            if (partner != null) foreach (var r in partner.GetComponentsInChildren<SpriteRenderer>()) r.enabled = true;
        }
        if (st != S.Gone) sr.enabled = true;
    }
}
