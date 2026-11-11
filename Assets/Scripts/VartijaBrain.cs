using UnityEngine;

/// <summary>
/// Vankilan vartija: aloittaa taistelun pampulla (pamppu-idle, kaksi pamppuiskua, pidempi ulottuvuus ja kovempi isku). Kun vartija kaatuu (tai heitetään),
/// pamppu lentää hänen kädestään (FlyingBaton), ja nousun jälkeen hän taistelee nyrkein (suorat J/S ja takajalan etupotku K, kaataa).
/// Aivo on osittainen (MongolBrain-tyyli): tavallinen tekoäly hoitaa kävelyn, juoksun, iskut ja kombot; tämä vaihtaa Enemyn iskukuvat ja -arvot pampun ja nyrkkien välillä
/// sekä lisää pamppuvaiheen erikoisiskun (yläkautta lyönti, kaataa).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class VartijaBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    [System.Serializable]
    public class Loadout
    {
        public Sprite[] idle, j, s, k;
        public int jImpact = 3, sImpact = 3, kImpact = 4;
        public int jDamage = 10, sDamage = 12, kDamage = 16; public bool sKnockdown;
        public float frameTime = 0.05f, attackRange = 2.2f, kReach = 3.0f;
        public string[] combos = { "JS", "JSK", "J" };
    }

    public Loadout baton, fists;
    public Sprite batonSprite;
    public bool hasBaton = true;
    [Tooltip("Pamppuvaiheen erikoisisku (yläkautta lyönti, kaataa): kuvat, osumaikkuna.")] public Sprite[] slamSprites;
    public int slamHitFrom = 5, slamHitTo = 8, slamDamage = 20; public float slamFrameTime = 0.05f, slamReach = 3.1f, slamMinRange = 1.4f, slamMaxRange = 3.3f, slamCooldown = 4.5f, slamWeight = 1f;

    [Header("Hälytys: pillin soitto, kun vartija äkkää heron; kutsuu lisää vartijoita")]
    public Sprite[] whistleSprites; public AudioClip whistleClip; public bool canCall = true; public int callCount = 3;
    public float whistleFrameTime = 0.045f, whistleHold = 0.8f, callCooldownAll = 45f;
    static float nextCallAnywhere;   // yksi hälytys kerrallaan koko pelissä (ei kaikki vartijat yhtä aikaa)
    bool whistling, called; float whistleT; bool spawned;
    const int WhDraw = 9, WhBlow = 3;   // kuvat 0-8 veto ja nosto, 9-11 puhallus, 12- lasku

    Enemy e; float lastTick, nextSlam, nextAny = 2f; bool slamActive, slamHit; float slamT; float dirSign = 1f;

    public bool Active => slamActive || whistling;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => (slamActive || whistling) && CurrentSprite != null;
    public bool ArmorNow => false;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    void Awake() { e = GetComponent<Enemy>(); nextAny = Random.Range(1.5f, 4f); }
    void Start() { Apply(hasBaton ? baton : fists); }
    void OnDisable() { slamActive = false; whistling = false; CurrentSprite = null; }

    void Apply(Loadout l)
    {
        if (e == null || l == null) return;
        e.idleSprites = l.idle; e.attackRange = l.attackRange;
        float F = l.frameTime;
        e.punchSprites = l.j; e.punchImpactFrame = l.jImpact; e.windupTime = l.jImpact * F; e.punchRecoverTime = Mathf.Max(0.05f, (l.j.Length - l.jImpact) * F); e.punchDamage = l.jDamage;
        e.punch2Sprites = l.s; e.punch2ImpactFrame = l.sImpact; e.punch2Damage = l.sDamage; e.punch2Knockdown = l.sKnockdown; e.punch2Chance = 0.4f;
        if (l.k != null && l.k.Length > 0)
        {
            e.punch3Sprites = l.k; e.punch3ImpactFrame = l.kImpact; e.punch3Damage = l.kDamage; e.punch3Knockdown = true; e.punch3Chance = 0.3f;
            e.punch3WindupTime = l.kImpact * F; e.punch3RecoverTime = 0.25f; e.punch3Reach = l.kReach;
        }
        else { e.punch3Sprites = null; e.punch3Chance = 0f; }
        e.combos = l.combos;
    }

    /// Kutsutaan, kun vartija kaatuu: pamppu irtoaa ja lentää; sen jälkeen nyrkit.
    public void DropBaton()
    {
        if (!hasBaton) return;
        hasBaton = false; slamActive = false; whistling = false; CurrentSprite = null;
        Apply(fists);
        if (batonSprite != null && e != null)
        {
            bool fr = e.FacingRight;
            FlyingBaton.Spawn(e, batonSprite, transform.position + new Vector3(fr ? 0.5f : -0.5f, 0f, 0f), fr ? -1f : 1f);
        }
    }

    void Update()
    {
        if (e == null) return;
        if (hasBaton && (e.IsAirborneNow || e.IsDead)) { DropBaton(); return; }
        if (whistling && (e.IsDead || Time.time - lastTick > 0.25f)) { EndWhistle(); return; }
        if (canCall && !called && !whistling && !slamActive && whistleSprites != null && whistleSprites.Length > WhDraw + WhBlow && e.Hero != null && e.IsAwake && e.BrainIdleOrChase && !e.IsDead
            && Time.time >= nextCallAnywhere && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) <= 11f) { BeginWhistle(); return; }
        if (slamActive && Time.time - lastTick > 0.25f) { EndSlam(); return; }
        if (!hasBaton || slamActive || whistling || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead || slamSprites == null || slamSprites.Length == 0) return;
        if (Time.time < nextAny || Time.time < nextSlam) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        if (dy > e.depthTolerance + 0.8f || dx < slamMinRange || dx > slamMaxRange) return;
        if (Random.value > 0.8f * slamWeight * Time.deltaTime) return;
        BeginSlam();
    }

    void BeginSlam()
    {
        slamActive = true; slamHit = false; slamT = 0f; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x; dirSign = e.FacingRight ? 1f : -1f;
        nextSlam = Time.time + slamCooldown * Random.Range(0.8f, 1.3f); nextAny = Time.time + 1.2f + slamSprites.Length * slamFrameTime;
        CurrentSprite = slamSprites[0];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        HitFx.PlayWhoosh();
    }

    void EndSlam()
    {
        slamActive = false; CurrentSprite = null;
        if (e != null && !e.IsDead) e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    void BeginWhistle()
    {
        whistling = true; called = true; spawned = false; whistleT = 0f; lastTick = Time.time;
        nextCallAnywhere = Time.time + callCooldownAll;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        CurrentSprite = whistleSprites[0];
    }

    void EndWhistle()
    {
        whistling = false; CurrentSprite = null;
        if (e != null && !e.IsDead) e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    void TickWhistle(float dt)
    {
        lastTick = Time.time; whistleT += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        float draw = WhDraw * whistleFrameTime;
        int n = whistleSprites.Length, i;
        if (whistleT < draw) i = Mathf.Min(WhDraw - 1, (int)(whistleT / whistleFrameTime));
        else if (whistleT < draw + whistleHold)
        {
            i = WhDraw + (int)((whistleT - draw) / 0.12f) % WhBlow;   // puhallus: kolme kuvaa silmukkana
            if (!spawned && whistleT >= draw + 0.25f) { spawned = true; if (whistleClip != null) e.PlayExtraSound(whistleClip, 1f); CallGuards(); }
        }
        else
        {
            i = WhDraw + WhBlow + (int)((whistleT - draw - whistleHold) / whistleFrameTime);
            if (i >= n) { EndWhistle(); return; }
        }
        CurrentSprite = whistleSprites[i];
    }

    /// Kolme uutta vartijaa (kopioita tästä) ruudun reunoilta; ne ovat heti hereillä, pampulla ja eivät kutsu lisää.
    void CallGuards()
    {
        var hero = e.Hero; var area = Area.Current;
        float half = 9f;
        if (Camera.main != null) half = Camera.main.orthographicSize * Camera.main.aspect + 1.5f;
        for (int k = 0; k < callCount; k++)
        {
            float side = k % 2 == 0 ? 1f : -1f;
            float x = hero.transform.position.x + side * (half + (k / 2) * 1.8f);
            if (area != null) x = Mathf.Clamp(x, area.walkMinX + 0.5f, area.walkMaxX - 0.5f);
            float y = transform.position.y + (k - 1) * 0.5f;
            if (area != null) { float yMax = area.MaxDepthAtX(x, area.maxDepthY), yMin = area.MinDepthAtX(x, area.minDepthY); y = Mathf.Clamp(y, yMin + 0.05f, yMax - 0.05f); }
            var go = Instantiate(gameObject, new Vector3(x, y, 0f), Quaternion.identity, transform.parent);
            go.name = "Vartija (hälytys " + (k + 1) + ")";
            var vb = go.GetComponent<VartijaBrain>(); vb.canCall = false; vb.called = true; vb.whistling = false; vb.slamActive = false; vb.hasBaton = true;
            var ne = go.GetComponent<Enemy>(); ne.IsAwake = true;
        }
    }

    public void Tick(float dt)
    {
        if (whistling) { TickWhistle(dt); return; }
        if (!slamActive) return;
        lastTick = Time.time; slamT += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        int i = AttackEase.Frame(slamT, slamFrameTime, slamHitFrom);
        if (i >= slamSprites.Length) { EndSlam(); return; }
        CurrentSprite = slamSprites[i];
        var hero = e.Hero;
        if (hero == null || slamHit || i < slamHitFrom || i > slamHitTo) return;
        Vector3 hp = hero.transform.position, mp = transform.position;
        float ahead = (hp.x - mp.x) * dirSign;
        if (!(ahead > -0.5f && ahead <= slamReach && Mathf.Abs(hp.y - mp.y) <= e.depthTolerance + 0.3f)) return;
        slamHit = true;
        if (!hero.TakeKnockdown(slamDamage, mp.x, 9f, 5f, e, true)) hero.TakeHit(slamDamage, mp.x, e, true);
    }
}

/// <summary>Vartijan kädestä lentävä pamppu: heittyy sivulle ylös pyöriessään, pomppaa lattiasta kerran, jää hetkeksi lattialle ja häviää.</summary>
public class FlyingBaton : MonoBehaviour
{
    float vx, vy, groundY, h, spin, life, rest; bool landed; SpriteRenderer sr;

    public static void Spawn(Enemy owner, Sprite sprite, Vector3 start, float dir)
    {
        var go = new GameObject("Pamppu");
        var b = go.AddComponent<FlyingBaton>();
        b.vx = dir * Random.Range(4.5f, 6.5f); b.vy = Random.Range(7f, 9f); b.groundY = start.y; b.h = 1.7f; b.spin = -dir * Random.Range(540f, 800f);
        b.sr = go.AddComponent<SpriteRenderer>(); b.sr.sprite = sprite;
        if (owner != null && owner.body != null) { b.sr.sortingLayerID = owner.body.sortingLayerID; b.sr.sortingOrder = owner.body.sortingOrder + 2; }
        go.transform.position = new Vector3(start.x, start.y + b.h, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime; life += dt;
        if (!landed)
        {
            vy -= 28f * dt; h += vy * dt;
            transform.position += new Vector3(vx * dt, 0f, 0f);
            transform.Rotate(0f, 0f, spin * dt);
            if (h <= 0.15f)
            {
                h = 0.15f;
                if (Mathf.Abs(vy) > 3f) { vy = -vy * 0.35f; vx *= 0.5f; spin *= 0.5f; }
                else { landed = true; transform.rotation = Quaternion.Euler(0f, 0f, 80f); }
            }
            transform.position = new Vector3(transform.position.x, groundY + h, 0f);
        }
        else
        {
            rest += dt;
            if (rest > 2.5f) { var c = sr.color; c.a = Mathf.Clamp01(1f - (rest - 2.5f) / 0.8f); sr.color = c; }
            if (rest > 3.3f) Destroy(gameObject);
        }
        if (life > 8f) Destroy(gameObject);
    }
}
