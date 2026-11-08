using UnityEngine;

/// <summary>
/// Temppelin kuningas: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa liikkeen ja lyönnit (jabi, takasuora); tämä lisää kaksi erikoisliikettä:
/// 1) tulipallo: kuningas osoittaa, kerää kämmeneensä kultaisen pallon ja ampuu sen heroa kohti (yli voi hypätä);
/// 2) tärisyttävä hyppy: kuningas hyppää ja iskeytyy maahan: ruutu tärähtää, katosta putoaa pölyä ja kaikki maassa olevat kaatuvat. Hypyllä (AirHeight) pääsee yli.
/// Kuvat: kuningas_tulipallo (latausta releaseFrame kuvaa, sitten toipuminen) ja kuningas_hyppy (12 kuvaa 0,1 s, loput 1/15 s; maahanisku kuvassa slamFrame).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class KingBrain : MonoBehaviour, IEnemyBrain, IPartialBrain, ICornerBurstBrain
{
    public Sprite[] cast;
    public int releaseFrame = 24;
    public float frameTime = 0.05f;
    public float cooldown = 3.5f, minRange = 3.5f, maxRange = 16f, chancePerSec = 1.3f;
    public int damage = 45;
    public float ballSpeed = 28f, ballRange = 30f, ballScale = 1.5f, knockSpeed = 13f, knockUp = 7f;
    [Tooltip("Pallon lähtökohta: etäisyys edessä ja korkeus (maailmayksikköä), ennen visualScalea.")]
    public Vector2 handOffset = new Vector2(2.3f, 2.0f);

    [Header("Tärisyttävä hyppy")]
    public Sprite[] slam;
    public int slamFrame = 27, slamWindupFrames = 9;
    public float slamWindupTime = 4f / 30f, slamFrameTime = 0.1f;
    public float slamCooldown = 7f, slamMaxRange = 14f, slamChancePerSec = 0.8f, slamShockRange = 18f;
    public int slamDamage = 32;
    public float slamKnockSpeed = 7f, slamKnockUp = 6f, slamShake = 0.45f, slamAirSafe = 0.6f;

    [Header("Heitto")]
    [Tooltip("Heitto: 45 kuvaa 1/15 s (kurotus 0-13, ote kuvassa 14, nosto 21-30, heitto kuvassa 31, toipuminen). Hero lentää kauas ja liukuu.")]
    public Sprite[] grab;
    public int grabCatchFrame = 14, grabThrowFrame = 31;
    public float grabRange = 3.2f, grabCooldown = 4f, grabChancePerSec = 2f, grabFrameTime = 1f / 15f;
    public int grabDamage = 38;
    public float throwSpeed = 12f, throwUp = 7f, throwSlide = 1.2f, holdLift = 3.4f;
    bool holding; AudioClip throwSfx;

    [Header("Luurankojen kutsu")]
    [Tooltip("Luurankosoturin malli (passiivinen, SkeletonRiser mukana, triggerDistance suuri); kutsutaan kun elämää on alle summonBelow.")]
    public GameObject summonTemplate;
    public float summonBelow = 0.25f; public int summonCount = 2; public float summonSpread = 4f;
    bool summoned;

    void Summon()
    {
        summoned = true;
        if (summonTemplate == null) return;
        var area = Area.Current;
        float face = e.FacingRight ? 1f : -1f;
        for (int i = 0; i < summonCount; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f, x = transform.position.x + side * summonSpread * (1f + 0.4f * (i / 2));
            if (area != null) x = Mathf.Clamp(x, area.walkMinX + 1.5f, area.walkMaxX - 1.5f);
            float y = transform.position.y + (i % 2 == 0 ? -0.6f : 0.6f);
            if (area != null) y = Mathf.Clamp(y, Mathf.Max(area.MinDepthAtX(x, area.minDepthY), area.minDepthY) + 0.1f, Mathf.Min(area.MaxDepthAtX(x, area.maxDepthY), area.maxDepthY) - 0.1f);
            var c = Instantiate(summonTemplate, summonTemplate.transform.parent);
            c.name = "Luurankosoturi (kutsuttu " + (i + 1) + ")";
            c.transform.position = new Vector3(x, y, 0f);
            c.SetActive(true);
        }
    }

    enum Phase { None, Fireball, Slam, Grab }
    Phase phase;
    Enemy e; float t, nextCast = 3f, nextSlam = 6f, nextGrab = 2f, lastTick; bool fired;
    AudioClip slamSfx, castVoice, throwVoice, welcomeVoice; bool welcomed;

    public bool Active => phase != Phase.None;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => Active && CurrentSprite != null;
    public bool ArmorNow => Active;   // erikoisliikkeet eivät katkea yksittäisestä iskusta

    void Awake() { e = GetComponent<Enemy>(); slamSfx = Resources.Load<AudioClip>("Sfx/Kuningas/hyppy"); throwSfx = Resources.Load<AudioClip>("Sfx/Kuningas/heitto");
        castVoice = Resources.Load<AudioClip>("Sfx/Kuningas/tulipallo"); throwVoice = Resources.Load<AudioClip>("Sfx/Kuningas/heittohuuto"); welcomeVoice = Resources.Load<AudioClip>("Sfx/Kuningas/welcome"); }
    void Voice(AudioClip c) { if (c != null && e != null && e.Audio != null) e.Audio.PlayOneShot(c, 1f); }
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    void Abort()
    {
        if (holding && e != null && e.Hero != null) e.Hero.ReleaseGrab();
        holding = false; phase = Phase.None;
    }

    float SlamTimeAt(int idx) => idx <= slamWindupFrames ? idx * slamWindupTime : slamWindupFrames * slamWindupTime + (idx - slamWindupFrames) * slamFrameTime;
    int SlamIndex(float time)
    {
        if (time < slamWindupFrames * slamWindupTime) return AttackEase.Wind(time, slamWindupFrames * slamWindupTime, slamWindupFrames);   // veto kiihtyy
        return slamWindupFrames + (int)((time - slamWindupFrames * slamWindupTime) / slamFrameTime);
    }

    void Update()
    {
        if (phase != Phase.None && Time.time - lastTick > 0.25f) { Abort(); return; }   // väliin tuli kaatuminen tms.
        if (!welcomed && e != null && e.IsAwake && !e.IsDead) { welcomed = true; Voice(welcomeVoice); }   // tervehdys kun taistelu alkaa
        if (phase != Phase.None || e == null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead) return;
        if (!summoned && e.HealthFraction <= summonBelow) { Summon(); return; }
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        // heitto: heron ollessa käden ulottuvilla
        if (grab != null && grab.Length > grabThrowFrame && Time.time >= nextGrab && dx <= grabRange && dy <= e.depthTolerance + 0.1f && !e.Hero.IsFloored && !e.Hero.IsGrabbed && e.Hero.AirHeight <= 0.5f && e.BrainIdleOrChase
            && Random.value < grabChancePerSec * Time.deltaTime)
        {
            nextGrab = Time.time + grabCooldown; nextSlam = Mathf.Max(nextSlam, Time.time + 2f); nextCast = Mathf.Max(nextCast, Time.time + 2f);
            Begin(Phase.Grab); holding = false; CurrentSprite = grab[0]; Voice(throwVoice);
            return;
        }
        // tärisyttävä hyppy: kauempaa tai läheltä, syvyys ei merkitse (isku osuu kaikkiin maassa oleviin)
        if (slam != null && slam.Length > slamFrame && Time.time >= nextSlam && dx <= slamMaxRange && Random.value < slamChancePerSec * Time.deltaTime)
        {
            nextSlam = Time.time + slamCooldown; nextCast = Mathf.Max(nextCast, Time.time + 3f);
            Begin(Phase.Slam); CurrentSprite = slam[0];
            if (slamSfx != null && e.Audio != null) e.Audio.PlayOneShot(slamSfx, 1f);
            return;
        }
        if (cast == null || cast.Length <= releaseFrame || Time.time < nextCast) return;
        if (dx < minRange || dx > maxRange || dy > e.depthTolerance + 0.4f) return;
        if (Random.value > chancePerSec * Time.deltaTime) return;
        nextCast = Time.time + cooldown; nextSlam = Mathf.Max(nextSlam, Time.time + 3f);
        Begin(Phase.Fireball); CurrentSprite = cast[0]; Voice(castVoice);
    }

    /// Nurkkapurkaus: tärisyttävä hyppy (osuu kaikkiin maassa oleviin), muuten heitto.
    public void CornerBurst()
    {
        if (e == null || e.Hero == null || phase != Phase.None) return;
        if (slam != null && slam.Length > slamFrame)
        {
            nextSlam = Time.time + slamCooldown;
            Begin(Phase.Slam); CurrentSprite = slam[0];
            if (slamSfx != null && e.Audio != null) e.Audio.PlayOneShot(slamSfx, 1f);
        }
        else if (grab != null && grab.Length > grabThrowFrame) { nextGrab = Time.time + grabCooldown; Begin(Phase.Grab); holding = false; CurrentSprite = grab[0]; Voice(throwVoice); }
    }

    void Begin(Phase p)
    {
        phase = p; fired = false; t = 0f; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
    }

    public void Tick(float dt)
    {
        if (phase == Phase.None) return;
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (phase == Phase.Slam) { SlamTick(); return; }
        if (phase == Phase.Grab) { GrabTick(); return; }
        int i = Mathf.Min(AttackEase.Frame(t, frameTime, releaseFrame), cast.Length - 1);
        CurrentSprite = cast[i];
        if (!fired && i >= releaseFrame)
        {
            fired = true;
            float face = e.FacingRight ? 1f : -1f, s = Mathf.Max(0.1f, e.visualScale);
            Vector3 me = transform.position;
            KingFireball.Spawn(e, new Vector3(me.x + face * handOffset.x * s, me.y + handOffset.y * s, 0f), e.FacingRight, damage, ballSpeed, ballRange, ballScale, knockSpeed, knockUp);
        }
        if (t >= cast.Length * frameTime) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    void GrabTick()
    {
        var hero = e.Hero;
        if (hero == null) { Abort(); return; }
        int i = Mathf.Min((int)(t / grabFrameTime), grab.Length - 1);
        CurrentSprite = grab[i];
        float face = e.FacingRight ? 1f : -1f; Vector3 me = transform.position;
        if (!holding && i >= grabCatchFrame && i < grabThrowFrame)
        {
            if (hero.IsGrabbed) return;
            if (Mathf.Abs(hero.transform.position.x - me.x) > grabRange + 0.8f || !hero.BeginGrab(me.x)) { Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }   // hero ehti pois
            holding = true;
        }
        if (holding && i < grabThrowFrame)
        {
            if (!hero.IsGrabbed) { Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }
            float u = Mathf.Clamp01((i - grabCatchFrame) / (float)(grabThrowFrame - grabCatchFrame));
            float lift = Mathf.Lerp(0.6f, holdLift, u * u), x = me.x + face * Mathf.Lerp(1.3f, 0.3f, u);   // rinnuksilta pään yli
            hero.SetHeld(new Vector3(x, me.y - 0.02f, 0f), lift, 0f, 0);
        }
        else if (holding)
        {
            holding = false;
            if (throwSfx != null && e.Audio != null) e.Audio.PlayOneShot(throwSfx, 1f);
            hero.Throw(face * throwSpeed, throwUp, grabDamage, true, throwSlide);   // lentää kauas ja liukuu
        }
        if (t >= grab.Length * grabFrameTime) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    void SlamTick()
    {
        int i = Mathf.Min(SlamIndex(t), slam.Length - 1);
        CurrentSprite = slam[i];
        if (!fired && i >= slamFrame)
        {
            fired = true;
            CameraFollow.Shake(slamShake, 0.9f);
            KingDust.Rain();
            float x = transform.position.x;
            foreach (var p in Players.All)
            {
                if (p == null || !p.enabled || p.GameOver || p.IsFloored || p.AirHeight > slamAirSafe || Mathf.Abs(p.transform.position.x - x) > slamShockRange) continue;   // hypyllä pääsee yli
                if (!p.TakeKnockdown(slamDamage, x, slamKnockSpeed, slamKnockUp, e, true)) p.TakeHit(slamDamage, x, e, true);
            }
        }
        if (t >= SlamTimeAt(slam.Length)) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }
}
