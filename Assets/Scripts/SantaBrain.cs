using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Joulupukki, Alppikylän afterski-baarin yläkerran pomo. Oma liikesarja-aivo Enemyn päällä (kuten Gustav): Enemy hoitaa osumat, kaatumisen,
/// ylösnousun, suojauksen ja energiapalkin; tämä ohjaa liikkeen ja hyökkäykset.
/// Liikkeet: kuusi iskua (kombot), mahatörmäys, hyppytaklaus (pitkä lähestyminen), alakoukku (lennättää kohteen ruudun poikki; lentävä kaataa muut matkalla),
/// paineaalto (molemmille puolille, ei estettävissä, lennättää lyhyemmän matkan; käytetään ahtaalla tai muuten), viskaus (hero, pöytä tai vihu: lentää kovaa vauhtia ja kaataa muut).
/// Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (282).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class SantaBrain : MonoBehaviour, IEnemyBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] idle, walk, run;
    public Sprite[] punch1, punch2, punch3, punch4, punch5, punch6, bump, tackle, upper, shock, grab, drink;

    [Header("Sisääntulo (tappelun alkaessa)")]
    [Tooltip("Repliikki, jonka pukki sanoo tullessaan (näkyy tekstinä pään yllä; lisäksi ääni, jos entranceSound on asetettu).")]
    public string entranceLine = "HO HO HO! Kuka tässä on ollut tuhma?!";
    public AudioClip entranceSound;
    public float entranceTime = 2.6f;
    [Tooltip("Juoksu portaiden luota esiin (yksikköä) ennen repliikkiä.")]
    public float runDistance = 7f;
    [Tooltip("Tappelumusiikki, joka alkaa soida ryypyn jälkeen ja päättyy pukin kaatuessa.")]
    public AudioClip fightMusic;

    [Header("Äänet (editori täyttää; Assets/Audio/pukki)")]
    [Tooltip("Iskun huuto iskun alkaessa (satunnainen, 60 % iskuista).")]
    public AudioClip[] punchSounds;
    [Tooltip("Koukun isku (koukku ja alakoukku), tuplakoukun alku (koukkukombo), mahatörmäyksen jälkeinen, osuman jälkeinen (joka toinen kaatava osuma heroon tai vihuun) ja taistelun aikana silloin tällöin puhuttava repliikki.")]
    public AudioClip hookSound, doubleHookSound, bumpAfterSound, hitSound, speakSound;
    [Tooltip("Heron liukumatka heiton jälkeen (1 = kapteenin liuku; liukuu muuten ruudun seinään asti).")]
    public float slideDistance = 0.67f;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    int lastTickFrame;
    float nextMusicGainAt; int peakAlive;
    const float MusicGain0 = 0.89f;   // musiikin alkutaso -1 dB
    const float VoiceBoost = 1.0f;   // pukin äänet: +1,5 dB oli liikaa (särki), takaisin perustasoon

    [Header("Kestävyys")]
    [Tooltip("Muiden vihujen iskuista tämän osuus (0-1) menee ohi (pukki on kova ja pysyy pystyssä väkijoukossa); berserkinä enemmän.")]
    [Range(0f, 1f)] public float foeHitIgnore = 0.5f, foeHitIgnoreBerserk = 0.7f;

    [Header("Ryyppy (berserk)")]
    [Tooltip("Juotuaan pukki on näin monta sekuntia (20) nopeampi ja aggressiivisempi, ja hehkuu punaisena (kuten Tom).")]
    public float berserkSeconds = 20f, berserkSpeed = 1.3f, drinkFrameTime = 0.055f, drinkCooldown = 22f;
    public Color glowColor = new Color(1f, 0.08f, 0.04f, 1f);
    [Range(0f, 1f)] public float drinkChance = 0.25f;

    [Header("Liike")]
    public float walkSpeed = 3.2f, runSpeed = 6.0f, shuffleSpeed = 2.2f, depthSpeed = 3.4f;
    public float walkFrameTime = 0.05f, runFrameTime = 0.083f, idleFrameTime = 0.07f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet (lisäksi herää osumasta).")]
    public float wakeDistance = 9f;
    public float rangeMin = 1.7f, rangeMax = 2.7f;

    [Header("Iskut")]
    public int damage1 = 8, damage2 = 11, damage3 = 12, damage4 = 10, damage5 = 9, damage6 = 13, bumpDamage = 18, tackleDamage = 16, upperDamage = 20, shockDamage = 12, throwDamage = 22;
    public int finisherBonus = 3;
    [Range(0f, 1f)] public float comboChance = 0.75f;
    public Vector2 decisionPause = new Vector2(0.15f, 0.5f);
    [Tooltip("Alakoukun lennätys: vaakanopeus ja ylös.")]
    public float upperLaunchX = 17f, upperLaunchUp = 6.5f;
    [Tooltip("Paineaalto: säde, nopeus, ylös ja jäähtyminen.")]
    public float shockRadius = 5.5f, shockLaunchX = 6.5f, shockLaunchUp = 4f, shockCooldown = 9f;
    [Tooltip("Heitto: nopeus ja ylös (hero liukuu, pöytä ja vihu lentää).")]
    public float throwSpeed = 21f, throwUp = 5f, propThrowSpeed = 16f;
    public float tackleCooldown = 5f, throwCooldown = 7f, propCooldown = 9f;

    enum Mode { Dormant, Entrance, Fight, Act }
    enum Kind { P1, P2, P3, P4, P5, P6, Bump, Tackle, Upper, Shock, Grab, Drink }
    enum Hit { Normal, Knock, Upper, Shock }
    enum VKind { Hero, Prop, Foe }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk
    {
        public Sprite[] frames; public float[] dur, cum; public int impact, activeEnd, travelFrom, travelTo;
        public float reach, lunge; public int damage; public Hit hit; public float knockSpeed, knockUp;
    }

    Enemy e;
    Mode mode = Mode.Dormant;
    float lastTick = -99f, decisionTimer = 0.6f, animClock, t;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    Step cur;
    bool comboRunning, heroHit;
    readonly HashSet<Enemy> foesHit = new HashSet<Enemy>();
    float nextShockTime = 4f, nextTackleTime = 2f, nextThrowTime = 5f, nextPropTime = 8f, lastCounterBlockStamp = -99f;
    bool counterUsed;
    readonly List<float> hitTimes = new List<float>();

    // kohde: tappelussa ensin kaikki muut, kun muut on pieksetty hero
    int hitVoiceCount;
    float voiceBusyUntil, nextSpeakAt = float.MaxValue, pendingAt; AudioClip pendingClip; bool voiceDone;
    bool rampage, berserk;
    float berserkT, nextDrinkTime = 12f;
    GameObject glowRoot; SpriteRenderer auraR, tintR;
    Enemy foe;
    float nextTargetTime, noFoeSince = -1f, heroPriorityUntil, entranceT;
    GameObject bubble;
    int entPhase; float runDir = 1f, runStopX; bool introDrink, camTaken, musicOn; Transform camSaved;

    // heitto
    VKind vkind; Crate vCrate; Enemy vFoe; bool holding, thrown;
    Crate fetchCrate; Enemy fetchFoe; float fetchUntil;
    // lentävä uhri kaataa muut matkallaan
    PlayerController bowlHero; float bowlUntil; readonly HashSet<Enemy> bowled = new HashSet<Enemy>();

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && cur.kind != Kind.Grab && (comboRunning || cur.kind == Kind.Bump || cur.kind == Kind.Shock || cur.kind == Kind.Tackle);

    /// Jäähtymisaika: berserkinä puolet (entistä aggressiivisempi).
    float Cd(float v) => berserk ? v * 0.5f : v;
    float DepthFollow => depthSpeed * 0.5f;
    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        LoadDefaultSounds();
        Add(Kind.P1, punch1, Dur(12, 0.045f, 4, 0.08f, 0.04f), 4, 4, 0, 4, 2.8f, 0.5f, damage1, Hit.Normal);
        Add(Kind.P2, punch2, Dur(9, 0.04f, 3, 0.07f, 0.04f), 3, 3, 0, 3, 3.0f, 0.8f, damage2, Hit.Knock);
        Add(Kind.P3, punch3, Dur(9, 0.04f, 3, 0.07f, 0.04f), 3, 3, 0, 3, 3.1f, 0.8f, damage3, Hit.Knock);
        Add(Kind.P4, punch4, Dur(12, 0.04f, 5, 0.08f, 0.04f), 5, 5, 0, 5, 3.0f, 0.4f, damage4, Hit.Normal);
        Add(Kind.P5, punch5, Dur(8, 0.04f, 4, 0.07f, 0.04f), 4, 4, 0, 4, 3.0f, 0.5f, damage5, Hit.Normal);
        Add(Kind.P6, punch6, Dur(8, 0.04f, 2, 0.07f, 0.04f), 2, 2, 0, 2, 3.0f, 0.7f, damage6, Hit.Knock);
        // mahatörmäys: nojaa taakse (0-12), hidas työntö (13-34), isku, palautus
        Add(Kind.Bump, bump, DurRange(59, new[] { (0, 12, 0.02f), (13, 33, 0.025f), (34, 36, 0.03f), (37, 58, 0.015f) }), 34, 36, 13, 34, 2.7f, 1.6f, bumpDamage, Hit.Knock, 10f, 5.5f);
        // hyppytaklaus: juoksu-alku (0-21), liito nyrkki suorana (22-37), lasku (38-54), nousu (55-60)
        Add(Kind.Tackle, tackle, DurRange(61, new[] { (0, 21, 0.04f), (22, 37, 0.0225f), (38, 54, 0.035f), (55, 60, 0.05f) }), 22, 37, 8, 37, 2.4f, 9.5f, tackleDamage, Hit.Knock, 11f, 5f);
        // alakoukku: kiihtyvä (hidas alku, nopea loppu)
        Add(Kind.Upper, upper, new[] { 0.14f, 0.12f, 0.10f, 0.08f, 0.06f, 0.05f, 0.04f, 0.04f, 0.05f, 0.14f }, 8, 8, 5, 8, 2.7f, 0.5f, upperDamage, Hit.Upper);
        // paineaalto: kyykky (0-7), ponnahdus ja aalto (8-23), voimailu (24-28), paluu (29-38)
        Add(Kind.Shock, shock, DurRange(39, new[] { (0, 7, 0.06f), (8, 23, 0.04f), (24, 28, 0.05f), (29, 38, 0.04f) }), 9, 14, 0, 0, 0f, 0f, shockDamage, Hit.Shock);
    }

    static float[] Dur(int n, float wind, int impact, float hold, float rec)
    {
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = i < impact ? wind : i == impact ? hold : rec;
        return d;
    }
    static float[] DurRange(int n, (int a, int b, float ft)[] parts)
    {
        var d = new float[n];
        for (int i = 0; i < n; i++) d[i] = 0.04f;
        foreach (var p in parts) for (int i = p.a; i <= p.b && i < n; i++) d[i] = p.ft;
        return d;
    }

    void Add(Kind k, Sprite[] frames, float[] dur, int impact, int activeEnd, int travelFrom, int travelTo, float reach, float lunge, int damage, Hit hit, float knockSpeed = 4.8f, float knockUp = 5f)
    {
        if (!Has(frames) || frames.Length < dur.Length) return;
        var cum = new float[dur.Length + 1];
        for (int i = 0; i < dur.Length; i++) cum[i + 1] = cum[i] + dur[i];
        atks[k] = new Atk { frames = frames, dur = dur, cum = cum, impact = impact, activeEnd = activeEnd, travelFrom = travelFrom, travelTo = travelTo, reach = reach, lunge = lunge, damage = damage, hit = hit, knockSpeed = knockSpeed, knockUp = knockUp };
    }

    // ---------------- Enemyn kutsut ----------------

    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        // väliin tuli osuma, kaatuminen tms. (aikaero JA kuvien määrä: yksittäinen pitkä kuva, esim. musiikin latauksen nykäys ryypyn alussa, ei keskeytä)
        if (Time.time - lastTick > 0.25f && Time.frameCount - lastTickFrame > 6) AbortAction();
        lastTick = Time.time; lastTickFrame = Time.frameCount;
        if (berserk)
        {
            berserkT += dt;
            if (berserkT >= berserkSeconds) { berserk = false; HideGlow(); nextDrinkTime = Time.time + drinkCooldown; }
            else dt *= berserkSpeed;   // 30 % nopeampi: liike, kuvat ja ajastukset
        }
        animClock += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        if (mode == Mode.Entrance) { EntranceUpdate(dt, hero); return; }
        if (mode == Mode.Fight && Time.time >= nextSpeakAt) { nextSpeakAt = Time.time + Random.Range(14f, 24f); Say(speakSound); }
        if (mode == Mode.Dormant)
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            CurrentSprite = Ping(idle, idleFrameTime);
            bool near = Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 3.5f;
            if (e.IsAwake || near) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.5f; nextSpeakAt = Time.time + Random.Range(8f, 14f); }
            else return;
        }
        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    void Update()
    {
        if (pendingClip != null && Time.time >= pendingAt) { var pc = pendingClip; pendingClip = null; voiceBusyUntil = 0f; Say(pc); }
        if (musicOn && e != null && e.IsDead) { MusicPlayer.StopOverride(3f); musicOn = false; }
        // alakoukun lennättämä hero kaataa muut vihut lentoreitillään
        if (bowlHero != null && Time.time < bowlUntil && bowlHero.IsAirborneThrown)
        {
            Vector3 hp = bowlHero.transform.position;
            foreach (var f in Enemy.All.ToArray())
            {
                if (f == null || f == e || f.IsDead || bowled.Contains(f)) continue;
                Vector3 q = f.transform.position;
                if (Mathf.Abs(q.x - hp.x) > 1.5f || Mathf.Abs(q.y - hp.y) > 0.8f) continue;
                bowled.Add(f);
                f.TakeHit(10, hp.x, true);
            }
        }
        else if (bowlHero != null && Time.time >= bowlUntil) { bowlHero = null; bowled.Clear(); }
    }

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi.
    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (mode == Mode.Entrance || introDrink || (mode == Mode.Act && cur.kind == Kind.Drink)) return true;   // ryypyn aikana (myös myöhemmin) ei osu mikään   // sisääntulon ja ryypyn aikana ei keskeydy mistään osumasta (ei ohita ryyppyä)
        if (mode == Mode.Dormant) return false;
        if (Enemy.EnemyStrike && Random.value < (berserk ? foeHitIgnoreBerserk : foeHitIgnore)) return true;   // muiden vihujen iskuista osa menee ohi: kovempi
        if (!Enemy.EnemyStrike) heroPriorityUntil = Time.time + 3f;   // hero löi: hän on taas kohde
        if (e.Hero != null)
        {
            float now = Time.time;
            hitTimes.Add(now); hitTimes.RemoveAll(x => now - x > 1.2f);
            // osumaputki: hero (tai kaksi) hakkaa jatkuvasti -> paineaalto ahtaalla (isku ohi)
            if (mode == Mode.Fight && hitTimes.Count >= 4 && now >= nextShockTime && atks.ContainsKey(Kind.Shock) && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 4f)
            {
                hitTimes.Clear();
                nextShockTime = now + Cd(shockCooldown);
                plan = new List<Step> { new Step(Kind.Shock) }; planPos = 0; comboRunning = false;
                lastTick = now;
                StartStep(e.Hero);
                return true;
            }
        }
        return false;
    }

    // ---------------- taistelu ----------------

    void FightUpdate(float dt, PlayerController hero)
    {
        Vector3 me = transform.position, hp = TargetPos(hero);
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y;
        e.FacingRight = dx > 0f;
        float dir = dx >= 0f ? 1f : -1f;
        decisionTimer -= dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);

        // heitettävän noutaminen (pöytä tai vihu)
        if (fetchCrate != null || fetchFoe != null)
        {
            if (!FetchUpdate(dt, hero)) { fetchCrate = null; fetchFoe = null; }
            else return;
        }

        // torjunnan jälkeen vastaisku: alakoukku
        if (e.LastBlockTime > lastCounterBlockStamp) { lastCounterBlockStamp = e.LastBlockTime; counterUsed = false; }
        if (!counterUsed && Time.time - e.LastBlockTime < 1.1f && adx < 3.2f && atks.ContainsKey(Kind.Upper))
        {
            counterUsed = true;
            plan = new List<Step> { new Step(Kind.Upper) }; planPos = 0; comboRunning = false;
            StartStep(hero); return;
        }

        bool moving = false, running = false;
        if (adx > rangeMax + 0.9f)
        {
            running = adx > 6f;
            MoveToward(dt, dir, dy, running);
            moving = true;
        }
        else
        {
            float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
            float dxMove = 0f;
            if (adx > rangeMax) dxMove = dir * walkSpeed * 0.8f * dt;
            else if (adx < rangeMin) { dxMove = -dir * shuffleSpeed * dt; moving = true; }
            if (dxMove != 0f || dyMove != 0f) e.BrainMove(new Vector2(dxMove, dyMove));
        }
        CurrentSprite = moving ? Loop(running && Has(run) ? run : (Has(walk) ? walk : idle), running ? runFrameTime : walkFrameTime) : Ping(idle, idleFrameTime);

        if (decisionTimer > 0f || TargetFloored(hero)) return;
        float now = Time.time;

        // ahtaalla: useampi kohde lähellä -> paineaalto
        if (now >= nextShockTime && atks.ContainsKey(Kind.Shock) && TargetsNear(4.2f) >= 2 && Random.value < 0.7f)
        {
            nextShockTime = now + Cd(shockCooldown);
            plan = new List<Step> { new Step(Kind.Shock) }; planPos = 0; comboRunning = false;
            StartStep(hero); return;
        }
        // kaukaa: hyppytaklaus tai heitettävä
        if (adx > 3.6f)
        {
            if (adx > 4.5f && now >= nextTackleTime && atks.ContainsKey(Kind.Tackle) && Mathf.Abs(dy) < e.depthTolerance + 0.4f && Random.value < 0.7f)
            {
                nextTackleTime = now + Cd(tackleCooldown);
                plan = new List<Step> { new Step(Kind.Tackle) }; planPos = 0; comboRunning = false;
                StartStep(hero); return;
            }
            if (foe == null && now >= nextPropTime && Has(grab) && adx < 10f && Random.value < 0.5f && FindThrowable(out fetchCrate, out fetchFoe))
            {
                nextPropTime = now + Cd(propCooldown); fetchUntil = now + 4f;
                return;
            }
            decisionTimer = 0.2f;
            return;
        }
        if (adx <= rangeMax + 0.5f && adx >= rangeMin - 0.5f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f)
        {
            ChoosePlan(hero);
            if (plan.Count > 0) StartStep(hero);
        }
    }

    int TargetsNear(float radius)
    {
        int n = 0; Vector3 me = transform.position;
        var hero = e.Hero;
        if (hero != null && Mathf.Abs(hero.transform.position.x - me.x) < radius && Mathf.Abs(hero.transform.position.y - me.y) < 1.4f) n++;
        foreach (var f in Enemy.All)
            if (f != null && f != e && !f.IsDead && f.isActiveAndEnabled && Mathf.Abs(f.transform.position.x - me.x) < radius && Mathf.Abs(f.transform.position.y - me.y) < 1.4f) n++;
        return n;
    }

    bool FindThrowable(out Crate crate, out Enemy foe)
    {
        crate = null; foe = null; Vector3 me = transform.position; float best = 8f;
        foreach (var c in Crate.All)
        {
            if (c == null || !c.CanPickUp || !(c.IsTable || c.breakable)) continue;
            float d = Mathf.Abs(c.transform.position.x - me.x) + Mathf.Abs(c.transform.position.y - me.y);
            if (d < best) { best = d; crate = c; foe = null; }
        }
        foreach (var f in Enemy.All)
        {
            if (f == null || f == e || f.IsDead || f.isBoss || !f.isActiveAndEnabled || !f.CanBeGrabbed) continue;
            float d = Mathf.Abs(f.transform.position.x - me.x) + Mathf.Abs(f.transform.position.y - me.y);
            if (d < best) { best = d; foe = f; crate = null; }
        }
        return crate != null || foe != null;
    }

    bool FetchUpdate(float dt, PlayerController hero)
    {
        if (Time.time > fetchUntil || !Has(grab)) return false;
        Vector3 me = transform.position, q;
        if (fetchCrate != null) { if (!fetchCrate.CanPickUp) return false; q = fetchCrate.transform.position; }
        else { if (fetchFoe == null || fetchFoe.IsDead || !fetchFoe.CanBeGrabbed) return false; q = fetchFoe.transform.position; }
        float dx = q.x - me.x, dy = q.y - me.y;
        e.FacingRight = dx > 0f;
        if (Mathf.Abs(dx) <= 1.6f && Mathf.Abs(dy) <= e.depthTolerance + 0.3f)
        {
            // heitetään kohti heroa
            e.FacingRight = hero.transform.position.x > me.x;
            vkind = fetchCrate != null ? VKind.Prop : VKind.Foe; vCrate = fetchCrate; vFoe = fetchFoe;
            fetchCrate = null; fetchFoe = null;
            plan = new List<Step> { new Step(Kind.Grab) }; planPos = 0; comboRunning = false;
            StartStep(hero);
            return true;
        }
        MoveToward(dt, dx >= 0f ? 1f : -1f, dy, true);
        CurrentSprite = Loop(Has(run) ? run : walk, runFrameTime);
        return true;
    }

    void MoveToward(float dt, float dir, float dy, bool fast)
    {
        float spd = fast ? runSpeed : walkSpeed;
        float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
        e.BrainMove(new Vector2(dir * spd * dt, dyMove));
    }

    void ChoosePlan(PlayerController hero)
    {
        plan = new List<Step>(); planPos = 0;
        float now = Time.time;
        bool guarding = foe == null && hero.BlocksFrom(transform.position.x);
        // suojaus rikotaan heitolla
        if (guarding && now >= nextThrowTime && Has(grab) && Random.value < 0.6f)
        {
            nextThrowTime = now + Cd(throwCooldown); vkind = VKind.Hero; vFoe = null;
            plan = new List<Step> { new Step(Kind.Grab) }; comboRunning = false; decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
            return;
        }
        // ryyppy: ei berserkinä, jäähtymisen jälkeen satunnaisesti
        if (!berserk && Has(drink) && now >= nextDrinkTime && Random.value < drinkChance)
        {
            nextDrinkTime = now + drinkCooldown;
            plan = new List<Step> { new Step(Kind.Drink) }; comboRunning = false; decisionTimer = 0.1f;
            return;
        }
        float r = Random.value, kb = berserk ? 1.35f : 1f;   // berserkinä enemmän heittoja, mahatörmäyksiä ja alakoukkuja
        if (r < 0.12f * kb && now >= nextThrowTime && Has(grab) && (foe == null || foe.CanBeGrabbed)) { nextThrowTime = now + Cd(throwCooldown); vkind = foe != null ? VKind.Foe : VKind.Hero; vFoe = foe; plan.Add(new Step(Kind.Grab)); }
        else if (r < 0.21f * kb) plan.Add(new Step(Kind.Bump));
        else if (r < 0.42f * kb) plan.Add(new Step(Kind.Upper));   // koukkua viljellen
        else if (Random.value < (berserk ? 0.95f : comboChance))
        {
            switch (Random.Range(0, 6))
            {
                case 0: plan = new List<Step> { new Step(Kind.P1), new Step(Kind.P5), new Step(Kind.P2, true) }; break;
                case 1: plan = new List<Step> { new Step(Kind.P4), new Step(Kind.P3), new Step(Kind.P6, true) }; break;
                case 2: plan = new List<Step> { new Step(Kind.P1), new Step(Kind.P1), new Step(Kind.P5), new Step(Kind.P6, true) }; break;
                case 3: plan = new List<Step> { new Step(Kind.P5), new Step(Kind.P2), new Step(Kind.P4, true) }; break;
                case 4: plan = new List<Step> { new Step(Kind.P4), new Step(Kind.P4), new Step(Kind.P4, true) }; break;
                default: plan = new List<Step> { new Step(Kind.P1), new Step(Kind.P4), new Step(Kind.Upper, true) }; break;
            }
        }
        else
        {
            Kind[] singles = { Kind.P1, Kind.P4, Kind.P4, Kind.P4, Kind.P5, Kind.P2 };
            plan.Add(new Step(singles[Random.Range(0, singles.Length)]));
        }
        if (berserk && plan.Count >= 2 && plan.Count < 6 && Random.value < 0.6f) plan.Insert(plan.Count - 1, new Step(Random.value < 0.5f ? Kind.P5 : Kind.P1));   // berserkinä pidemmät kombot
        plan.RemoveAll(s => s.kind != Kind.Grab && s.kind != Kind.Drink && !atks.ContainsKey(s.kind));
        comboRunning = plan.Count > 1;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (berserk ? 0.25f : 1f);
    }

    // ---------------- toiminnan suoritus ----------------

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        voiceDone = false;
        if (cur.kind <= Kind.P6 && cur.kind != Kind.P4 && Random.value < 0.6f && punchSounds != null && punchSounds.Length > 0) Say(punchSounds[Random.Range(0, punchSounds.Length)]);
        if (planPos == 0 && cur.kind == Kind.P4 && plan.Count >= 3) Say(doubleHookSound);   // koukkukombo alkaa
        mode = Mode.Act; t = 0f; heroHit = false; foesHit.Clear(); grabPhase = 0; grabT = 0f; holding = false; thrown = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        if (cur.kind == Kind.Grab) { GrabUpdate(dt, hero); return; }
        if (cur.kind == Kind.Drink) { DrinkUpdate(); return; }
        Vector3 me = transform.position, hp = foe != null ? foe.transform.position : hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        var a = atks[cur.kind];

        // nykyinen kuva ajasta
        int pos = a.dur.Length - 1;
        for (int i = 0; i < a.dur.Length; i++) if (t < a.cum[i + 1]) { pos = i; break; }
        CurrentSprite = a.frames[pos];
        bool active = pos >= a.impact && pos <= a.activeEnd;
        e.BrainPhaseSet(active ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);

        // eteneminen
        if (a.lunge > 0f && pos >= a.travelFrom && pos <= a.travelTo)
        {
            float span = Mathf.Max(0.02f, a.cum[a.travelTo + 1] - a.cum[a.travelFrom]);
            float step = a.lunge / span * dt;
            if (Mathf.Abs(hp.x - me.x) > 1.2f || cur.kind == Kind.Tackle) e.BrainMove(new Vector2(face * step, 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * 0.8f * dt)));
        }

        if (active) TryHit(hero, a);
        if (active && (cur.kind == Kind.P4 || cur.kind == Kind.Upper)) HookSweep(a);
        if (pos == a.impact && !voiceDone) { voiceDone = true; if (cur.kind == Kind.P4 || cur.kind == Kind.Upper) Say(hookSound); }
        if (pos == a.impact && cur.kind == Kind.Shock && !shockFx) { shockFx = true; if (CameraFollow.Instance != null) CameraFollow.Shake(0.4f, 0.35f); }
        if (t >= a.cum[a.dur.Length])
        {
            shockFx = false;
            planPos++;
            if (planPos < plan.Count && ComboTargetOk(hero)) StartStep(hero);
            else EndPlan();
        }
    }
    bool shockFx;

    // --- heitto: kurotus, tarttuminen, pito, heilautus, heitto ---
    int grabPhase; float grabT;
    static readonly float[] WindDur = { 0.07f, 0.07f, 0.06f, 0.055f, 0.05f, 0.045f, 0.04f, 0.035f, 0.03f, 0.03f };
    void GrabUpdate(float dt, PlayerController hero)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        grabT += dt;
        Vector3 me = transform.position;
        float face = e.FacingRight ? 1f : -1f;
        int n = grab.Length;
        const float reachFt = 0.06f; const float holdTime = 0.35f;
        if (grabPhase == 0)            // kurotus (kuvat 0-8): ote kuvassa 6
        {
            int idx = Mathf.Min((int)(grabT / reachFt), 8);
            CurrentSprite = grab[Mathf.Min(idx, n - 1)];
            if (grabT >= reachFt * 6f)
            {
                bool ok = false;
                if (vkind == VKind.Hero) ok = Mathf.Abs(hero.transform.position.x - me.x) < 2.6f && hero.BeginGrab(me.x);
                else if (vkind == VKind.Prop) { ok = vCrate != null && vCrate.CanPickUp; if (ok) vCrate.PickUp(); }
                else { ok = vFoe != null && !vFoe.IsDead && vFoe.CanBeGrabbed; if (ok) vFoe.BeginHeldByPlayer(me.x, false); }
                if (!ok) { EndPlan(); return; }
                holding = true; grabPhase = 1; grabT = 0f;
            }
        }
        else if (grabPhase == 1)       // pito (kuvat 9-11)
        {
            if (!StillHeld(hero)) { holding = false; EndPlan(); return; }
            CurrentSprite = grab[Mathf.Min(9 + (int)(grabT / 0.1f) % 3, n - 1)];
            float u = Mathf.Clamp01(grabT / holdTime);
            PlaceHeld(me, face, 0.5f + 0.3f * u, 1.0f);
            if (grabT >= holdTime) { grabPhase = 2; grabT = 0f; }
        }
        else if (grabPhase == 2)       // heilautus taakse ja ylös (kuvat 12-21, kiihtyy)
        {
            if (!StillHeld(hero)) { holding = false; EndPlan(); return; }
            int idx = 0; float acc = 0f;
            for (; idx < WindDur.Length; idx++) { acc += WindDur[idx]; if (grabT < acc) break; }
            CurrentSprite = grab[Mathf.Min(12 + Mathf.Min(idx, WindDur.Length - 1), n - 1)];
            float total = 0f; foreach (var w in WindDur) total += w;
            float u = Mathf.Clamp01(grabT / total);
            PlaceHeld(me, face, 0.8f + 1.8f * u, 1.0f - 1.9f * u);
            if (grabT >= total) { grabPhase = 3; grabT = 0f; ThrowHeld(face); }
        }
        else                           // heitto ja seuranta (kuvat 22-33)
        {
            const float throwFt = 0.04f;
            int idx = Mathf.Min(22 + (int)(grabT / throwFt), n - 1);
            CurrentSprite = grab[idx];
            if (22 + (int)(grabT / throwFt) >= n - 1 && grabT > (n - 22) * throwFt + 0.1f) { planPos++; EndPlan(); }
        }
    }

    bool StillHeld(PlayerController hero)
    {
        if (vkind == VKind.Hero) return hero.IsGrabbed;
        if (vkind == VKind.Prop) return vCrate != null;
        return vFoe != null && !vFoe.IsDead;
    }

    void PlaceHeld(Vector3 me, float face, float lift, float forward)
    {
        float x = me.x + face * forward;
        if (vkind == VKind.Hero && e.Hero != null) e.Hero.SetHeld(new Vector3(x, me.y - 0.02f, 0f), lift, 0f, 0);
        else if (vkind == VKind.Prop && vCrate != null) vCrate.SetCarried(new Vector3(x, me.y - 0.01f, 0f), lift, Mathf.RoundToInt(-me.y * 100f) + 6);
        else if (vkind == VKind.Foe && vFoe != null) vFoe.SetHeldByPlayer(new Vector3(x, me.y - 0.05f, 0f), lift, 0f, -1);
    }

    void ThrowHeld(float face)
    {
        if (vkind == VKind.Hero && e.Hero != null)
        {
            e.Hero.Throw(face * throwSpeed, throwUp, throwDamage, true, slideDistance);
            bowlHero = e.Hero; bowlUntil = Time.time + 1.6f; bowled.Clear();
        }
        else if (vkind == VKind.Prop && vCrate != null) { vCrate.Throw(face * propThrowSpeed, 3.5f, e, throwDamage); vCrate = null; }
        else if (vkind == VKind.Foe && vFoe != null)
        {
            vFoe.thrownBy = e; vFoe.thrownByEnemy = true;
            vFoe.ReleaseThrow(face * propThrowSpeed * 1.1f, 4f, 18, true, slideDistance); vFoe = null;
        }
        holding = false; thrown = true;
    }

    bool ComboTargetOk(PlayerController hero)
    {
        if (hero == null || TargetFloored(hero)) return false;
        Vector3 me = transform.position, hp = foe != null ? foe.transform.position : hero.transform.position;
        return Mathf.Abs(hp.x - me.x) < 4.0f && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.6f;
    }

    void EndPlan()
    {
        mode = Mode.Fight; comboRunning = false; holding = false; shockFx = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (berserk ? 0.25f : 1f);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, kaatuminen...). Otteessa oleva päästetään irti.
    void AbortAction()
    {
        if (holding)
        {
            if (vkind == VKind.Hero && e.Hero != null) e.Hero.ReleaseGrab();
            else if (vkind == VKind.Prop && vCrate != null) vCrate.Drop();
            else if (vkind == VKind.Foe && vFoe != null) vFoe.ReleaseThrow(0f, 2f, 0);
        }
        holding = false; shockFx = false;
        if (introDrink) { Debug.Log("[Pukki] ryyppy keskeytyi (AbortAction)"); EndIntro(); }
        if (mode == Mode.Act) mode = Mode.Fight;
        comboRunning = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Mathf.Max(decisionTimer, 0.15f);
    }

    void TryHit(PlayerController hero, Atk a)
    {
        if (foe != null && a.hit != Hit.Shock) { TryHitFoe(a); return; }
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        int dmg = a.damage + (cur.finisher ? finisherBonus : 0);
        if (a.hit == Hit.Shock)
        {
            // paineaalto: molemmille puolille, ei estettävissä; heroon ja muihin (lähellä) vihuihin
            if (!heroHit && Mathf.Abs(dx) <= shockRadius && Mathf.Abs(hp.y - me.y) <= 1.5f && hero.AirHeight < 1.2f)
            {
                heroHit = true;
                hero.TakeKnockdown(dmg, me.x, shockLaunchX, shockLaunchUp, e, true);
            }
            foreach (var f in Enemy.All.ToArray())
            {
                if (f == null || f == e || f.IsDead || foesHit.Contains(f) || !f.isActiveAndEnabled) continue;
                Vector3 q = f.transform.position;
                if (Mathf.Abs(q.x - me.x) > shockRadius || Mathf.Abs(q.y - me.y) > 1.5f) continue;
                foesHit.Add(f);
                f.TakeHit(dmg, me.x, true);
            }
            return;
        }
        if (heroHit) return;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > a.reach + 0.2f) return;
        if (Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f) return;
        bool airHero = hero.AirHeight > 0.9f && a.hit != Hit.Upper;
        if (airHero && cur.kind != Kind.Tackle) return;
        heroHit = true;
        if (cur.kind == Kind.Bump) Later(bumpAfterSound, 0.45f);
        else if (a.hit != Hit.Normal && (++hitVoiceCount % 2) == 0) Say(hitSound);   // joka toinen kaatava osuma
        bool follow = planPos > 0;
        switch (a.hit)
        {
            case Hit.Normal: hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Knock: if (!hero.TakeKnockdown(dmg, me.x, a.knockSpeed, a.knockUp, e, cur.kind == Kind.Bump)) hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Upper:
                // lennättää ruudun poikki; lentävä hero kaataa muut matkallaan (Update)
                if (hero.TakeKnockdown(dmg, me.x, upperLaunchX, upperLaunchUp, e)) { bowlHero = hero; bowlUntil = Time.time + 1.6f; bowled.Clear(); }
                break;
        }
    }

    /// Koukku (ja alakoukku) osuu kaikkiin edessä oleviin vihuihin, myös pelaajan kantamaan.
    void HookSweep(Atk a)
    {
        Vector3 me = transform.position;
        int dmg = a.damage + (cur.finisher ? finisherBonus : 0);
        foreach (var f in Enemy.All.ToArray())
        {
            if (f == null || f == e || f == foe || f.IsDead || foesHit.Contains(f) || !f.isActiveAndEnabled) continue;
            Vector3 q = f.transform.position; float dx = q.x - me.x;
            bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
            if (!front || Mathf.Abs(dx) > a.reach + 0.2f || Mathf.Abs(q.y - me.y) > e.depthTolerance + 0.3f) continue;
            if (f.IsHeldNow) { foesHit.Add(f); f.ReleaseKnockdown(dmg, me.x); continue; }
            if (f.IsDownOrDead) continue;
            foesHit.Add(f);
            Enemy.EnemyHits(f, dmg, me.x, true);
        }
    }

    void TryHitFoe(Atk a)
    {
        if (heroHit || foe == null || foe.IsDownOrDead) return;
        Vector3 me = transform.position, fp = foe.transform.position;
        float dx = fp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > a.reach + 0.2f || Mathf.Abs(fp.y - me.y) > e.depthTolerance + 0.2f) return;
        heroHit = true;
        int dmg = a.damage + (cur.finisher ? finisherBonus : 0);
        Enemy.EnemyHits(foe, dmg, me.x, a.hit != Hit.Normal);
        if (a.hit != Hit.Normal && (++hitVoiceCount % 2) == 0) Say(hitSound);   // joka toinen kaatava osuma (vihuun)
    }

    // ---------------- kohteen valinta ja sisääntulo ----------------

    /// Tappelun alkaessa (7 s jälkeen): kaikki pysähtyvät idleen, pukki juoksee portaiden luota esiin (kamera seuraa häntä), sanoo repliikin ja ottaa ryypyn;
    /// sen jälkeen alkaa mäiskintä: ensin kaikkien muiden päälle, kun muut on pieksetty heron päälle. Tappelumusiikki (fightMusic) alkaa mäiskinnän alkaessa.
    public void Enter(Vector3 pos)
    {
        gameObject.SetActive(true);
        transform.position = pos;
        rampage = true; foe = null; noFoeSince = -1f; heroPriorityUntil = 0f;
        mode = Mode.Entrance; entPhase = 0; entranceT = 0f; plan.Clear(); comboRunning = false; holding = false; introDrink = false;
        e.IsAwake = true; e.ignoreFreeze = true;
        Freeze(true);
        var hero = e.Hero;
        float dir = hero != null && hero.transform.position.x < pos.x ? -1f : 1f;
        runDir = dir; runStopX = pos.x + dir * runDistance;
        // kamera siirtyy pukkiin
        if (CameraFollow.Instance != null) { camSaved = CameraFollow.Instance.target; CameraFollow.Instance.target = transform; camTaken = true; }
    }

    void StartSpeech()
    {
        entPhase = 1; entranceT = 0f;
        if (entranceSound != null)
        {
            entranceTime = Mathf.Max(entranceTime, entranceSound.length + 0.4f);   // kaikki pysyvät paikallaan koko repliikin ajan
            if (e.Audio != null) { e.Audio.mute = false; e.Audio.spatialBlend = 0f; e.Audio.PlayOneShot(entranceSound, voiceVolume * VoiceBoost); } else AudioSource.PlayClipAtPoint(entranceSound, Camera.main != null ? Camera.main.transform.position : transform.position, voiceVolume * VoiceBoost);
            Debug.Log("[Pukki] sisääntulo: repliikki " + entranceSound.name + " (" + entranceSound.length.ToString("0.0") + " s), musiikki " + (fightMusic != null ? fightMusic.name : "puuttuu"));
        }
        else if (!string.IsNullOrEmpty(entranceLine))
        {
            bubble = new GameObject("Pukin repliikki");
            bubble.transform.SetParent(transform, false); bubble.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            var tm = bubble.AddComponent<TextMesh>();
            tm.text = entranceLine; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.fontSize = 64; tm.characterSize = 0.1f; tm.color = new Color(1f, 0.95f, 0.6f);
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var mr = bubble.GetComponent<MeshRenderer>(); mr.sharedMaterial = tm.font.material; mr.sortingOrder = 30000;
        }
    }

    /// Pysäyttää muut viholliset idleen ja herot paikoilleen repliikin ajaksi.
    static void Freeze(bool on)
    {
        Enemy.FreezeToIdle = on;
        foreach (var p in Players.All)
            if (p != null) { p.Scripted = on; p.ScriptedMove = Vector2.zero; }
    }

    /// Sisääntulo ohi (ryyppy juotu tai keskeytyi): kaikki liikkeelle, kamera takaisin heroon, tappelumusiikki alkaa.
    void EndIntro()
    {
        Freeze(false);
        if (camTaken && CameraFollow.Instance != null) CameraFollow.Instance.target = camSaved;
        camTaken = false; introDrink = false;
        if (bubble != null) { Destroy(bubble); bubble = null; }
        nextSpeakAt = Time.time + Random.Range(10f, 16f);
    }

    void StartFightMusic()
    {
        if (fightMusic == null || musicOn) return;
        MusicPlayer.PlayOverride(fightMusic, 0.6f, true, MusicGain0); musicOn = true;
        Debug.Log("[Pukki] tappelumusiikki alkaa: " + fightMusic.name + (MusicPlayer.Instance == null ? " (VAROITUS: MusicPlayer puuttuu scenestä)" : ""));
    }

    void OnDisable()
    {
        if (mode == Mode.Entrance || introDrink) { EndIntro(); mode = Mode.Fight; }
        HideGlow();
        if (musicOn) { MusicPlayer.StopOverride(2f); musicOn = false; }
    }

    void EntranceUpdate(float dt, PlayerController hero)
    {
        if (entPhase == 0)   // juoksee portaiden luota esiin
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Chase);
            e.FacingRight = runDir > 0f;
            float before = transform.position.x;
            e.BrainMove(new Vector2(runDir * runSpeed * dt, 0f));
            CurrentSprite = Loop(Has(run) ? run : walk, runFrameTime);
            entranceT += dt;
            bool arrived = (runDir > 0f ? transform.position.x >= runStopX : transform.position.x <= runStopX) || Mathf.Abs(transform.position.x - before) < 0.01f * runSpeed * dt || entranceT > 4f;
            if (arrived) StartSpeech();
            return;
        }
        entranceT += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Idle);
        e.FacingRight = hero.transform.position.x > transform.position.x;
        // voimailuasento (paineaallon kuvat 24-28) pyörii repliikin ajan
        if (Has(shock) && shock.Length > 28) CurrentSprite = shock[24 + (int)(entranceT / 0.08f) % 5];
        else CurrentSprite = Ping(idle, idleFrameTime);
        if (entranceT >= entranceTime)
        {
            if (bubble != null) { Destroy(bubble); bubble = null; }
            mode = Mode.Fight; decisionTimer = 0.3f;
            Debug.Log("[Pukki] repliikki loppu, ryyppy: " + (Has(drink) ? drink.Length + " kuvaa" : "EI KUVIA (aja valikko 282)"));
            if (Has(drink)) { introDrink = true; plan = new List<Step> { new Step(Kind.Drink) }; planPos = 0; comboRunning = false; StartStep(hero); }   // repliikin perään ryyppy
            else EndIntro();
        }
    }

    bool TargetFloored(PlayerController hero) => foe != null ? foe.IsDownOrDead : hero.IsFloored;

    /// Kohteen paikka: tappelussa lähin muu vihollinen (hero on kohde vasta, kun muut on pieksetty tai hero lyö pukkia).
    Vector3 TargetPos(PlayerController hero)
    {
        if (!rampage || Time.time < heroPriorityUntil) { foe = null; return hero.transform.position; }
        if (foe != null && (!foe.isActiveAndEnabled || foe.IsDownOrDead)) foe = null;
        if (mode != Mode.Act && Time.time >= nextTargetTime) { nextTargetTime = Time.time + 0.35f; foe = PickFoe(); }
        if (foe == null)
        {
            if (noFoeSince < 0f) noFoeSince = Time.time;
            if (Time.time - noFoeSince > 1.5f) rampage = false;   // muut on pieksetty: nyt herot
            return hero.transform.position;
        }
        noFoeSince = -1f;
        return foe.transform.position;
    }

    Enemy PickFoe()
    {
        Vector3 me = transform.position; Enemy best = null; float bd = 1e9f;
        foreach (var o in Enemy.All)
        {
            if (o == null || o == e || !o.isActiveAndEnabled || o.IsDownOrDead) continue;
            float dx = Mathf.Abs(o.transform.position.x - me.x), dy = Mathf.Abs(o.transform.position.y - me.y);
            if (dx > 40f || dy > 6f) continue;
            float d = dx + dy * 2f;
            if (d < bd) { bd = d; best = o; }
        }
        return best;
    }

    // --- ryyppy: nostaa tuopin, juo (berserk alkaa kuvasta 18), laskee ja voimailee ---
    void DrinkUpdate()
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        int n = drink.Length;
        int idx = Mathf.Min((int)(t / drinkFrameTime), n - 1);
        CurrentSprite = drink[idx];
        StartFightMusic();   // musiikki alkaa heti ryypyn noston alussa
        if (idx >= 18 && !berserk) { berserk = true; berserkT = 0f; ShowGlow(); }
        if (t >= n * drinkFrameTime + 0.1f)
        {
            planPos++;
            EndPlan();
            decisionTimer = 0.1f;
            if (introDrink) EndIntro();   // ryyppy juotu: mäiskintä alkaa
        }
    }

    // ---------------- punainen hehku (kuten Tomilla) ----------------

    void ShowGlow()
    {
        if (glowRoot != null || e.body == null) { if (glowRoot != null) glowRoot.SetActive(true); return; }
        glowRoot = new GameObject("Pukin hehku");
        glowRoot.transform.SetParent(e.body.transform.parent, false);
        var a = new GameObject("Aura"); a.transform.SetParent(glowRoot.transform, false); auraR = a.AddComponent<SpriteRenderer>();
        var tnt = new GameObject("Sävy"); tnt.transform.SetParent(glowRoot.transform, false); tintR = tnt.AddComponent<SpriteRenderer>();
    }

    void HideGlow() { if (glowRoot != null) glowRoot.SetActive(false); }

    void LateUpdate()
    {
        if (glowRoot == null || !glowRoot.activeSelf || e == null || e.body == null) return;
        var b = e.body;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
        float rise = Mathf.Clamp01(berserkT / 0.8f) * Mathf.Clamp01((berserkSeconds - berserkT) / 0.8f);   // hehku nousee ja laskee pehmeästi
        foreach (var r in new[] { auraR, tintR })
        {
            r.sprite = b.sprite; r.flipX = b.flipX;
            r.transform.position = b.transform.position; r.transform.rotation = b.transform.rotation;
        }
        auraR.transform.localScale = b.transform.localScale * (1.08f + 0.04f * pulse);
        tintR.transform.localScale = b.transform.localScale;
        auraR.sortingLayerID = b.sortingLayerID; tintR.sortingLayerID = b.sortingLayerID;
        auraR.sortingOrder = b.sortingOrder - 1; tintR.sortingOrder = b.sortingOrder + 1;
        var c = glowColor;
        auraR.color = new Color(c.r, c.g, c.b, (0.30f + 0.20f * pulse) * rise);
        tintR.color = new Color(c.r, c.g * 0.6f, c.b * 0.6f, (0.16f + 0.14f * pulse) * rise);
        auraR.enabled = tintR.enabled = b.enabled && b.sprite != null;
    }

    // ---------------- äänet ----------------

    static AudioClip R(string n) => Resources.Load<AudioClip>("Pukki/" + n);

    /// Puuttuvat äänet haetaan Resources/Pukki-kansiosta (toimii, vaikka editorivalikkoa ei olisi ajettu uudelleen).
    void LoadDefaultSounds()
    {
        if (entranceSound == null) entranceSound = R("pukin_repliikki");
        if (fightMusic == null) fightMusic = R("pukki_musiikki");
        if (punchSounds == null || punchSounds.Length == 0) punchSounds = new[] { R("pukki_lyo1"), R("pukki_lyo2") };
        if (hookSound == null) hookSound = R("pukki_koukku_lyo");
        if (doubleHookSound == null) doubleHookSound = R("pukki_tuplakoukku");
        if (bumpAfterSound == null) bumpAfterSound = R("pukki_bellypump_jalkeen");
        if (hitSound == null) hitSound = R("pukki_osuu");
        if (speakSound == null) speakSound = R("pukki_speak1");
        if (e != null && (e.hurtSounds == null || e.hurtSounds.Length == 0)) { e.hurtSounds = new[] { R("pukki_pain1"), R("pukki_pain2") }; e.hurtVolume = 1f; }
    }

    /// Pukin ääni (yksi kerrallaan: uusi ei katkaise vanhaa, vaan jää väliin).
    void Say(AudioClip c)
    {
        if (c == null || Time.time < voiceBusyUntil) return;
        voiceBusyUntil = Time.time + Mathf.Min(c.length, 1.2f);
        if (e != null && e.Audio != null) { e.Audio.mute = false; e.Audio.spatialBlend = 0f; e.Audio.PlayOneShot(c, voiceVolume * VoiceBoost); } else AudioSource.PlayClipAtPoint(c, Camera.main != null ? Camera.main.transform.position : transform.position, voiceVolume * VoiceBoost);
    }

    void Later(AudioClip c, float delay) { if (c == null) return; pendingClip = c; pendingAt = Time.time + delay; }

    // ---------------- apurit ----------------

    Sprite Ping(Sprite[] a, float ft)
    {
        if (!Has(a)) return CurrentSprite;
        if (a.Length == 1) return a[0];
        int period = 2 * a.Length - 2, i = (int)(animClock / Mathf.Max(0.001f, ft)) % period;
        return a[i < a.Length ? i : period - i];
    }

    Sprite Loop(Sprite[] a, float ft)
    {
        if (!Has(a)) return CurrentSprite;
        return a[(int)(animClock / Mathf.Max(0.001f, ft)) % a.Length];
    }
}
