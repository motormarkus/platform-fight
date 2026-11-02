using UnityEngine;

/// <summary>
/// Timppa (mainareiden pomo): osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa liikkeen, iskut, vasaran, torjunnan ja heiton; tämä lisää pääpuskun:
/// kaksi kurotuskuvaa ja kuuden kuvan sarja, jonka lopussa Timppa puskee päällään ja lennättää osuman saaneen heron ruudun toiseen laitaan.
/// Pääpusku tulee satunnaisesti, kun Timppa saa paljon osumia lyhyessä ajassa tai kun hän joutuu alueen reunaan (nurkkaan), ja silloin se on yleisempi.
/// Kuvat: timppa_puskupaa (8 kuvaa: 0-1 kurotus, 2-7 sarja; osumakuva = impactFrame).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class TimppaBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    public Sprite[] buttSprites;
    public int impactFrame = 6;
    public float reachFrameTime = 0.13f, seriesFrameTime = 0.07f;
    public float cooldown = 6f, reach = 2.7f, lungeDistance = 1.8f;
    [Tooltip("Satunnaisesti lähietäisyydellä: todennäköisyys sekunnissa.")]
    public float randomChancePerSec = 0.18f;
    [Tooltip("Paljon osumia: näin monta osumaa tällä ajalla (sekuntia) laukaisee pääpuskun lähietäisyydellä.")]
    public int battered = 4; public float batteredWindow = 4f;
    [Tooltip("Nurkassa: etäisyys alueen reunasta (maailmayksikköä), jolla Timppa katsotaan nurkkaan ajetuksi.")]
    public float cornerDistance = 3.5f;
    public int damage = 30; public float knockSpeed = 17f, knockUp = 7f;

    float t, nextButt = 3f, lastTick, lastFrac = 1f; bool active, hitDone;
    readonly System.Collections.Generic.List<float> hitTimes = new System.Collections.Generic.List<float>();
    Enemy e; float dir = 1f;

    public bool Active => active;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => active && CurrentSprite != null;
    public bool ArmorNow => active;   // pääpusku ei katkea iskuun (Enemy.TakeHit tarkistaa myös kaatavat iskut)
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    void Awake() { e = GetComponent<Enemy>(); nextButt = Random.Range(2f, 5f); }
    void OnDisable() { active = false; }

    bool Cornered()
    {
        var a = Area.Current; if (a == null) return false;
        float x = transform.position.x;
        return x - a.walkMinX < cornerDistance || a.walkMaxX - x < cornerDistance;
    }

    void Update()
    {
        if (e == null) return;
        // osumien seuranta: elämäpalkin pieneneminen = osuma
        float frac = e.HealthFraction;
        if (frac < lastFrac - 0.0005f) hitTimes.Add(Time.time);
        lastFrac = frac;
        for (int i = hitTimes.Count - 1; i >= 0; i--) if (Time.time - hitTimes[i] > batteredWindow) hitTimes.RemoveAt(i);

        if (active && Time.time - lastTick > 0.25f) { active = false; return; }   // väliin tuli osuma, kaatuminen tms.
        if (active || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead) return;
        if (buttSprites == null || buttSprites.Length <= impactFrame || Time.time < nextButt) return;
        var hp = e.Hero.transform.position; var me = transform.position;
        float dx = Mathf.Abs(hp.x - me.x), dy = Mathf.Abs(hp.y - me.y);
        if (dx > reach + 0.6f || dy > e.depthTolerance + 0.3f) return;
        bool trigger = false;
        if (hitTimes.Count >= battered || Cornered()) trigger = Random.value < 5f * Time.deltaTime;   // paniikki: lähes heti, kun hero on lähellä
        else trigger = Random.value < randomChancePerSec * Time.deltaTime;
        if (!trigger) return;
        nextButt = Time.time + cooldown * Random.Range(0.9f, 1.3f);
        hitTimes.Clear();
        Begin();
    }

    void Begin()
    {
        active = true; hitDone = false; t = 0f; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x; dir = e.FacingRight ? 1f : -1f;
        CurrentSprite = buttSprites[0];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
    }

    int FrameAt(float time)
    {
        float reachT = 2f * reachFrameTime;
        if (time < reachT) return Mathf.Min(1, (int)(time / reachFrameTime));
        return Mathf.Min(buttSprites.Length - 1, 2 + (int)((time - reachT) / seriesFrameTime));
    }

    public void Tick(float dt)
    {
        if (!active) return;
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        var hero = e.Hero;
        if (hero != null && t < 2f * reachFrameTime) { e.FacingRight = hero.transform.position.x > transform.position.x; dir = e.FacingRight ? 1f : -1f; }
        int i = FrameAt(t);
        CurrentSprite = buttSprites[i];
        // puskun syöksy: sarjan kolme viimeistä kuvaa viedään eteenpäin
        if (i >= impactFrame - 2) e.BrainMove(new Vector2(dir * lungeDistance / (3f * seriesFrameTime) * dt, 0f));
        if (!hitDone && hero != null && i >= impactFrame)
        {
            Vector3 me = transform.position, hp = hero.transform.position;
            float dxl = hp.x - me.x;
            bool front = e.FacingRight ? dxl >= -0.3f : dxl <= 0.3f;
            if (front && Mathf.Abs(dxl) <= reach && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.2f && hero.AirHeight <= 1.2f)
            {
                hitDone = true;
                if (!hero.TakeKnockdown(damage, me.x, knockSpeed, knockUp, e, false)) hero.TakeHit(damage, me.x, e, true);
                CameraFollow.Shake(0.15f, 0.3f);
            }
        }
        float total = 2f * reachFrameTime + (buttSprites.Length - 2) * seriesFrameTime;
        if (t >= total) { active = false; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }
}
