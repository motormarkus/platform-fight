using UnityEngine;

/// <summary>
/// Luolan luurankosoturi: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa liikkeen ja iskut (jabi, takasuora, potku); tämä lisää luunheiton:
/// soturi nostaa luun pään yläpuolelle ja heittää sen heroa kohti. Luu lentää pyörien (yli voi hypätä) ja osuessaan vahingoittaa.
/// Kuvat: luuranko_luu (releaseFrame = vapautuskuva) ja Resources/Luuranko/luu.png (Tools/SkeletonAssets/make_skeleton_sheets.py).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class SkeletonBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    public Sprite[] throwSprites;
    public int releaseFrame = 12;
    [Tooltip("Heiton kuvien kesto sekunteina: lataus kiihtyy (alku hidas, loppu nopea), vapautuksen jälkeen tasaisesti. Koko heitto alle sekunnin.")]
    public float windupSlow = 0.07f, windupFast = 0.02f, followFrame = 0.035f;
    public float cooldown = 5f, minRange = 4f, maxRange = 15f, chancePerSec = 4f;
    public int damage = 14;
    public float boneSpeed = 18f, boneRange = 24f, boneScale = 1f;
    [Tooltip("Luun lähtökohta: etäisyys edessä ja korkeus (maailmayksikköä), ennen visualScalea.")]
    public Vector2 handOffset = new Vector2(1.2f, 2.5f);

    [Header("Hyppypotku")]
    [Tooltip("23 kuvaa 0,04 s: ponnistus, nousu, ojennettu potku (osumakuvat leapHitFrom-To), alastulo. Liike kuvissa leapMoveFrom-To.")]
    public Sprite[] leapSprites;
    public float leapFrameTime = 0.04f, leapCooldown = 6f, leapMinRange = 3.5f, leapMaxRange = 11f, leapChancePerSec = 0.7f, leapReach = 3.2f, leapStopGap = 1.7f, leapMaxTravel = 10f;
    public int leapHitFrom = 12, leapHitTo = 17, leapMoveFrom = 5, leapMoveTo = 13, leapDamage = 20;
    bool leap, leapHit; float leapDist, nextLeap = 3f;

    bool active, fired;
    Enemy e; float t, nextThrow = 2f, lastTick;

    public bool Active => active;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => active && CurrentSprite != null;
    public bool ArmorNow => false;   // tavallinen vihollinen: heiton voi katkaista iskulla
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    AudioClip throwVoice, leapVoice;
    void Awake()
    {
        e = GetComponent<Enemy>(); nextThrow = Random.Range(0.1f, 0.6f); nextLeap = Random.Range(2f, 6f);
        throwVoice = Resources.Load<AudioClip>("Sfx/Luuranko/luunheitto"); leapVoice = Resources.Load<AudioClip>("Sfx/Luuranko/potku");
    }
    void Voice(AudioClip c) { if (c != null && e != null && e.Audio != null) e.Audio.PlayOneShotLeveled(c, 0.55f, true); }

    void Update()
    {
        if (active && Time.time - lastTick > 0.25f) { active = false; return; }   // väliin tuli osuma, kaatuminen tms.
        if (active || e == null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        if (dy > e.depthTolerance + 0.4f) return;
        if (leapSprites != null && leapSprites.Length > leapHitTo && Time.time >= nextLeap && dx >= leapMinRange && dx <= leapMaxRange && Random.value < leapChancePerSec * Time.deltaTime)
        {
            nextLeap = Time.time + leapCooldown * Random.Range(0.8f, 1.3f); nextThrow = Mathf.Max(nextThrow, Time.time + 1.5f);
            leapDist = Mathf.Clamp(dx - leapStopGap, 0f, leapMaxTravel); Begin(true); return;
        }
        if (throwSprites == null || throwSprites.Length <= releaseFrame || Time.time < nextThrow) return;
        if (dx < minRange || dx > maxRange) return;
        if (Random.value > chancePerSec * Time.deltaTime) return;
        nextThrow = Time.time + cooldown * Random.Range(0.8f, 1.3f); nextLeap = Mathf.Max(nextLeap, Time.time + 1.5f);
        Begin(false);
    }

    void Begin(bool isLeap)
    {
        leap = isLeap; leapHit = false;
        active = true; fired = false; t = 0f; lastTick = Time.time; CurrentSprite = (isLeap ? leapSprites : throwSprites)[0];
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        Voice(isLeap ? leapVoice : throwVoice);
    }

    void LeapTick(float dt)
    {
        int i = Mathf.Min((int)(t / leapFrameTime), leapSprites.Length - 1);
        CurrentSprite = leapSprites[i];
        float face = e.FacingRight ? 1f : -1f;
        if (leapDist > 0f && i >= leapMoveFrom && i <= leapMoveTo)   // ponnistuksesta potkuun: matka heron eteen
            e.BrainMove(new Vector2(face * leapDist / (leapMoveTo - leapMoveFrom + 1) * (dt / leapFrameTime), 0f));
        var hero = e.Hero;
        if (!leapHit && hero != null && i >= leapHitFrom && i <= leapHitTo)
        {
            Vector3 me = transform.position, hp = hero.transform.position;
            float dxl = hp.x - me.x;
            bool front = e.FacingRight ? dxl >= -0.2f : dxl <= 0.2f;
            if (front && Mathf.Abs(dxl) <= leapReach && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.1f && hero.AirHeight <= 0.9f)
            {
                leapHit = true;
                if (!hero.TakeKnockdown(leapDamage, me.x, 8f, 5f, e, true)) hero.TakeHit(leapDamage, me.x, e, true);
            }
        }
        if (t >= leapSprites.Length * leapFrameTime) { active = false; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    public void Tick(float dt)
    {
        if (!active) return;
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (leap) { LeapTick(dt); return; }
        int i = FrameAt(t);
        CurrentSprite = throwSprites[i];
        if (!fired && i >= releaseFrame)
        {
            fired = true;
            float face = e.FacingRight ? 1f : -1f, s = Mathf.Max(0.1f, e.visualScale);
            Vector3 me = transform.position;
            BoneProjectile.Spawn(e, new Vector3(me.x + face * handOffset.x * s, me.y + handOffset.y * s, 0f), e.FacingRight, damage, boneSpeed, boneRange, boneScale);
        }
        if (t >= TotalTime()) { active = false; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    float Dur(int i) => i < releaseFrame ? Mathf.Lerp(windupSlow, windupFast, releaseFrame <= 1 ? 1f : (float)i / (releaseFrame - 1)) : followFrame;
    int FrameAt(float time)
    {
        float acc = 0f;
        for (int i = 0; i < throwSprites.Length; i++) { acc += Dur(i); if (time < acc) return i; }
        return throwSprites.Length - 1;
    }
    float TotalTime() { float acc = 0f; for (int i = 0; i < throwSprites.Length; i++) acc += Dur(i); return acc; }
}

/// <summary>Heitetty luu: lentää suoraan pyörien; osuu heroon, jos tämä ei ole ilmassa (AirHeight yli 0,9) eikä eri syvyydellä. Kuva Resources/Luuranko/luu.png.</summary>
public class BoneProjectile : MonoBehaviour
{
    Enemy owner; float dir, depthY, speed, range, travelled, ownerX, age, spin;
    int damage; SpriteRenderer sr;
    static Sprite sprite;

    public static Sprite BoneSprite()
    {
        if (sprite == null)
        {
            var tx = Resources.Load<Texture2D>("Luuranko/luu");
            if (tx != null) sprite = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 250f, 0, SpriteMeshType.FullRect);
        }
        return sprite;
    }

    public static void Spawn(Enemy owner, Vector3 start, bool right, int damage, float speed, float range, float scale)
    {
        if (BoneSprite() == null) return;
        var go = new GameObject("Luu");
        go.transform.position = start;
        go.transform.localScale = Vector3.one * scale;
        var b = go.AddComponent<BoneProjectile>();
        b.owner = owner; b.dir = right ? 1f : -1f; b.depthY = owner.transform.position.y; b.damage = damage; b.speed = speed; b.range = range; b.ownerX = start.x;
        b.spin = (right ? -1f : 1f) * 900f;
        b.sr = go.AddComponent<SpriteRenderer>(); b.sr.sprite = sprite; b.sr.sortingOrder = Mathf.RoundToInt(-b.depthY * 100f) + 8;
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        Vector3 p = transform.position; float prevX = p.x;
        p.x += dir * speed * dt; travelled += speed * dt;
        // kevyt kaari: nousee hetken ja laskee lentomatkan loppupuolella
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, age * spin);
        foreach (var target in Players.All)
        {
            if (target == null || !target.enabled || target.GameOver) continue;
            Vector3 tp = target.transform.position;
            bool crossed = (tp.x - prevX) * dir >= -0.6f && (tp.x - p.x) * dir <= 0.6f;
            if (crossed && Mathf.Abs(tp.y - depthY) <= 1.0f && target.AirHeight <= 0.9f)
            {
                target.TakeHit(damage, ownerX, owner, true);
                HitSpark.Spawn(new Vector3(tp.x - dir * 0.2f, p.y, 0f), true, sr.sortingOrder + 2);
                Destroy(gameObject);
                return;
            }
        }
        if (travelled >= range || owner == null) Destroy(gameObject);
    }
}

/// <summary>Luurangon räjähdys: luita lentää joka suuntaan (pääosin iskun suuntaan), pomppivat lattiasta ja osa lentää kameraa päin (kasvavat ja häviävät ruudun eteen).</summary>
public static class BoneBurst
{
    public static void Spawn(Vector3 at, float scale, int order, float dir)
    {
        var sp = BoneProjectile.BoneSprite();
        if (sp == null) return;
        int n = 9;
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Luunpala");
            go.transform.position = at + new Vector3(Random.Range(-0.4f, 0.4f) * scale, Random.Range(0.8f, 2.4f) * scale, 0f);
            var p = go.AddComponent<BonePiece>();
            bool cam = Random.value < 0.3f;
            p.Init(sp, at.y, order, dir, scale, cam);
        }
    }
}

public class BonePiece : MonoBehaviour
{
    SpriteRenderer sr; Vector2 vel; float groundY, spin, life, age, baseScale; bool toCam; int bounces;

    public void Init(Sprite sp, float ground, int order, float dir, float scale, bool camera)
    {
        sr = gameObject.AddComponent<SpriteRenderer>(); sr.sprite = sp;
        groundY = ground; toCam = camera;
        float a = Random.Range(0.3f, 1.35f) * Mathf.PI * 0.5f, spd = Random.Range(2.5f, 6.5f) * (camera ? 0.5f : 1f);
        vel = new Vector2(Mathf.Cos(a) * spd * (Random.value < 0.78f ? dir : -dir), Mathf.Sin(a) * spd + 3f);
        spin = Random.Range(-900f, 900f); life = camera ? Random.Range(0.9f, 1.3f) : Random.Range(1.6f, 2.4f);
        baseScale = Random.Range(0.3f, 0.6f) * scale;
        sr.sortingOrder = camera ? 6000 : order;
        transform.localScale = Vector3.one * baseScale;
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt; float u = age / life;
        if (toCam)
        {   // kameraa päin: kasvaa nopeasti ja häipyy
            transform.localScale = Vector3.one * baseScale * Mathf.Lerp(1f, 7f, u * u);
            transform.position += new Vector3(vel.x * 0.25f, Mathf.Max(0.5f, vel.y * 0.3f) * (1f - u), 0f) * dt;
            sr.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((u - 0.55f) / 0.45f));
        }
        else
        {
            vel.y -= 22f * dt;
            transform.position += (Vector3)(vel * dt);
            if (transform.position.y < groundY && vel.y < 0f)
            {
                transform.position = new Vector3(transform.position.x, groundY, 0f);
                if (bounces++ < 1) { vel.y = -vel.y * 0.3f; vel.x *= 0.3f; spin *= 0.3f; } else { vel = Vector2.zero; spin = 0f; }
            }
            sr.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((u - 0.75f) / 0.25f));
        }
        transform.Rotate(0f, 0f, spin * dt);
        if (age >= life) Destroy(gameObject);
    }
}
