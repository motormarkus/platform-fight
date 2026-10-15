using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rottakeisari, viemärin minibossi. Oma liikesarja-aivo Enemy-komponentin päällä (kuten Deacon):
/// Enemy hoitaa osumat, kaatumisen, ylösnousun, energiapalkin ja kipuäänet; tämä ohjaa liikkeen, iskut ja erikoisliikkeet.
/// - Pysyy aina viemärin keskiosassa (zoneHalfWidth), ei koskaan päädyissä.
/// - Kävelee ja juoksee (juoksu = kävelykuvat nopeammin), lyö jabin, suoran, koukun ja potkun kombona.
/// - Vihellys: tuo heti yhden punkkarin ja yhden skeittarin mukaan tappeluun.
/// - Tynnyrit: potkaisee seisovan tynnyrin vierimään heroa kohti ja potkaisee heron potkaiseman tai heittämän tynnyrin takaisin.
/// Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (157).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class RatBoss : MonoBehaviour, IEnemyBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] calm;        // idle1: rauhallinen odotus (edestakaisin)
    public Sprite[] guard;       // taisteluasento (silmukka)
    public Sprite[] walk;        // kävelysilmukka (juoksussa samat kuvat nopeammin)
    public Sprite[] whistle;     // vihellys (6 kuvaa: 0–1 nosto, 2–4 sormet suussa, 5 paluu)
    public Sprite[] annoyed;     // harmistuminen ylösnousun jälkeen (6 kuvaa)
    public Sprite[] baguetteThrow;   // patongin heitto (6 kuvaa: 0–3 otto ja heilautus, 4 irti, 5 jatko)
    public Sprite[] baguetteSprites; // lentävän patongin pyörimiskuvat (8)
    public Sprite[] jab, straight, hook, kick;

    [Header("Ajoitus")]
    public float calmFrameTime = 0.17f, guardFrameTime = 0.17f;
    [Tooltip("Yhden kävelysilmukan kesto (s) kävellessä ja juostessa.")]
    public float walkCycle = 1.1f, runCycle = 0.6f;
    public float windupFrameTime = 0.07f, impactHold = 0.08f, recoverFrameTime = 0.055f, comboWindupScale = 0.6f;

    [Header("Liike")]
    public float walkSpeed = 2.4f, runSpeed = 5f, depthSpeed = 2.6f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet.")]
    public float wakeDistance = 10f;
    public float rangeMin = 1.7f, rangeMax = 2.4f, runDistance = 6.5f;
    [Tooltip("Pysyy viemärin keskellä: näin kauas lähtöpaikastaan (x) hän liikkuu korkeintaan.")]
    public float zoneHalfWidth = 24f;

    [Header("Iskut")]
    public int jabDamage = 8, straightDamage = 10, hookDamage = 12, kickDamage = 18, finisherBonus = 4;
    [Range(0f, 1f)] public float comboChance = 0.65f;
    public Vector2 decisionPause = new Vector2(0.3f, 0.7f);

    [Header("Patonki (kovettunut): kevyt heittoisku")]
    public int baguetteDamage = 4;
    public float baguetteSpeed = 11f;
    public Vector2 baguetteInterval = new Vector2(6f, 10f);
    [Range(0f, 1f)] public float baguetteChance = 0.5f;

    [Header("Harmistuminen")]
    [Tooltip("Todennäköisyys, että hän harmistuu noustuaan ylös (kuvat 6, ei vahingoita; hero voi iskeä)")]
    [Range(0f, 1f)] public float annoyedChance = 0.5f;

    [Header("Äänet")]
    public AudioClip welcome, frustrated;
    public AudioClip[] speak, punchSounds;
    public float welcomeVolume = 1f, speakVolume = 0.9f, frustratedVolume = 1f, punchVolume = 0.9f;
    public Vector2 speakInterval = new Vector2(9f, 16f);
    float nextSpeak = -1f, lastPunchSound = -9f;
    int lastSpeak = -1;

    [Header("Vihellys")]
    public AudioClip whistleSound;
    public float whistleVolume = 1f;
    [Tooltip("Aikaisintaan näin monen sekunnin kuluttua taistelun alusta ensimmäinen vihellys.")]
    public float firstWhistleDelay = 1.2f;
    public float whistleGap = 8f;

    [Header("Tynnyrit")]
    public int barrelDamage = 16;
    public float barrelKickSpeed = 12f, barrelCooldown = 6f;

    // ---------------- sisäinen tila ----------------
    enum Mode { Dormant, Fight, Act }
    enum Kind { Jab, Straight, Hook, Kick, Whistle, BarrelKick, Annoyed, Baguette }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk { public Sprite[] frames; public int impact; public float reach, lunge; public int damage; }

    static readonly float[] BaguetteTimes = { 0.2f, 0.2f, 0.18f, 0.3f, 0.12f, 0.3f };
    const int BaguetteReleaseFrame = 4;
    float baguetteCd = 4f;
    bool baguetteDone;
    static readonly float[] AnnoyedTimes = { 0.2f, 0.25f, 0.35f, 0.35f, 0.3f, 0.3f };
    float handledGetUp = -99f;
    static readonly float[] WhistleTimes = { 0.18f, 0.18f, 0.12f, 0.45f, 0.4f, 0.22f };
    const int WhistleSoundFrame = 3;

    Enemy e;
    Mode mode = Mode.Dormant;
    float modeTime, lastTick = -99f, decisionTimer = 0.5f, animClock, fightTime;
    float homeX, zoneMin, zoneMax;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    Step cur;
    float t, tWind, tHold, tRecFrame, prevLunge;
    int curImpact, curFrames;
    bool hitDone, comboRunning, whistleDone;
    int whistles;
    float lastWhistleTime = -99f, barrelCd = 2f, reflectCd;
    Crate targetBarrel;
    float barrelGiveUp;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    /// Kombon aikana tavalliset osumat eivät katkaise iskusarjaa.
    public bool ArmorNow => mode == Mode.Act && comboRunning;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) { return false; }

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        homeX = transform.position.x;
        BuildAttacks();
    }

    void BuildAttacks()
    {
        atks.Clear();
        Add(Kind.Jab, jab, 3, 2.5f, 0.4f, jabDamage);
        Add(Kind.Straight, straight, 1, 2.6f, 0.5f, straightDamage);
        Add(Kind.Hook, hook, 1, 2.5f, 0.5f, hookDamage);
        Add(Kind.Kick, kick, 4, 3.3f, 0.5f, kickDamage);
        Add(Kind.BarrelKick, kick, 4, 3.3f, 0f, kickDamage);
    }

    void Add(Kind k, Sprite[] frames, int impact, float reach, float lunge, int damage)
    {
        if (frames == null || frames.Length <= impact) return;
        atks[k] = new Atk { frames = frames, impact = impact, reach = reach, lunge = lunge, damage = damage };
    }

    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    // ---------------- Enemyn kutsut ----------------

    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();   // väliin tuli osuma tms.
        lastTick = Time.time;
        modeTime += dt; animClock += dt;
        if (mode == Mode.Fight || mode == Mode.Act) fightTime += dt;
        barrelCd -= dt; reflectCd -= dt; baguetteCd -= dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        if (mode == Mode.Dormant && e.IsAwake) StartFight();   // hero iski ensin
        switch (mode)
        {
            case Mode.Dormant:
                e.BrainPhaseSet(Enemy.BrainPhase.Idle);
                CurrentSprite = PingPong(calm, calmFrameTime);
                if (Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 4f) StartFight();
                break;
            case Mode.Fight: FightUpdate(dt, hero); SpeakMaybe(); break;
            case Mode.Act: ActUpdate(dt, hero); break;
        }
    }

    void StartFight()
    {
        mode = Mode.Fight; modeTime = 0f; fightTime = 0f;
        zoneMin = homeX - zoneHalfWidth; zoneMax = homeX + zoneHalfWidth;
        e.IsAwake = true;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        decisionTimer = 0.5f;
        if (welcome != null && e.Audio != null) e.Audio.PlayOneShot(welcome, welcomeVolume);
        nextSpeak = Time.time + (welcome != null ? welcome.length : 0f) + Random.Range(speakInterval.x, speakInterval.y);
    }

    // ---------------- taistelu ----------------

    void FightUpdate(float dt, PlayerController hero)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y;
        e.FacingRight = dx > 0f;
        float dir = dx >= 0f ? 1f : -1f;
        decisionTimer -= dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);

        // harmistuu noustuaan ylös (kaatamisen jälkeen)
        if (e.LastGetUpTime > handledGetUp)
        {
            handledGetUp = e.LastGetUpTime;
            if (Has(annoyed) && Random.value < annoyedChance) { StartAct(Kind.Annoyed, false); return; }
        }
        // 1. heron potkaisema tai heittämä tynnyri tulossa: potkaisee takaisin
        if (TryReflect(me)) return;
        // 2. vihellys: punkkari ja skeittari mukaan
        if (WantsWhistle()) { StartAct(Kind.Whistle, false); return; }
        // 3. tynnyrin potkaisu heroa kohti
        if (BarrelPlan(dt, hero, me, hp, dir)) return;
        // 4. kovettuneen patongin heitto kauempaa
        if (Has(baguetteThrow) && Has(baguetteSprites) && baguetteCd <= 0f && adx >= 4.5f && adx <= 12f && Mathf.Abs(dy) < 1.6f && hp.x >= zoneMin - 4f && hp.x <= zoneMax + 4f && !hero.IsFloored)
        {
            baguetteCd = Random.Range(baguetteInterval.x, baguetteInterval.y);
            if (Random.value < baguetteChance) { StartAct(Kind.Baguette, false); return; }
        }

        bool heroInZone = hp.x >= zoneMin - 2f && hp.x <= zoneMax + 2f;
        bool moving = false, running = false;
        if (!heroInZone)
        {
            // hero on viemärin päässä: pysyy keskialueella (reunalla) vartiossa
            float tx = Mathf.Clamp(hp.x, zoneMin, zoneMax);
            if (Mathf.Abs(tx - me.x) > 0.3f) { MoveX(Mathf.Sign(tx - me.x), walkSpeed, dt); moving = true; }
        }
        else if (adx > rangeMax + 0.9f)
        {
            running = adx > runDistance;
            float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.5f * dt) : 0f;
            MoveXY(dir, running ? runSpeed : walkSpeed, dyMove, dt);
            moving = true;
        }
        else
        {
            float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.5f * dt) : 0f;
            float stepX = 0f;
            if (adx > rangeMax) stepX = dir * walkSpeed * 0.8f;
            else if (adx < rangeMin) stepX = -dir * 1.3f;
            if (stepX != 0f || dyMove != 0f) MoveXY(Mathf.Sign(stepX == 0f ? 1f : stepX), Mathf.Abs(stepX), dyMove, dt, stepX != 0f);
        }
        CurrentSprite = moving && Has(walk) ? Walk(running) : Loop(Has(guard) ? guard : walk, guardFrameTime);

        if (decisionTimer <= 0f && heroInZone && adx <= 3.2f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f && !hero.IsFloored)
        {
            ChoosePlan(adx);
            if (plan.Count > 0) StartStep(hero);
        }
    }

    Sprite Walk(bool run)
    {
        float ft = (run ? runCycle : walkCycle) / Mathf.Max(1, walk.Length);
        return walk[(int)(animClock / ft) % walk.Length];
    }

    void MoveX(float dirX, float speed, float dt) { MoveXY(dirX, speed, 0f, dt); }

    /// Liike zonen rajoissa (rotta ei mene viemärin päihin).
    void MoveXY(float dirX, float speed, float dyMove, float dt, bool doX = true)
    {
        float me = transform.position.x;
        float dxm = doX ? dirX * speed * dt : 0f;
        float nx = Mathf.Clamp(me + dxm, zoneMin, zoneMax);
        e.BrainMove(new Vector2(nx - me, dyMove));
    }

    void ChoosePlan(float adx)
    {
        plan = new List<Step>(); planPos = 0; comboRunning = false;
        if (adx > rangeMax + 0.2f && atks.ContainsKey(Kind.Kick)) { plan.Add(new Step(Kind.Kick, true)); decisionTimer = Random.Range(decisionPause.x, decisionPause.y); return; }   // kauempaa potku
        if (Random.value < comboChance)
        {
            switch (Random.Range(0, 5))
            {
                case 0: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Straight), new Step(Kind.Hook, true) }; break;
                case 1: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Straight), new Step(Kind.Hook), new Step(Kind.Kick, true) }; break;
                case 2: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Hook), new Step(Kind.Kick, true) }; break;
                case 3: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Jab), new Step(Kind.Kick, true) }; break;
                default: plan = new List<Step> { new Step(Kind.Straight), new Step(Kind.Kick, true) }; break;
            }
            plan.RemoveAll(s => !atks.ContainsKey(s.kind));
            comboRunning = plan.Count > 1;
            return;
        }
        float r = Random.value;
        Kind k = r < 0.4f ? Kind.Jab : r < 0.65f ? Kind.Straight : r < 0.85f ? Kind.Hook : Kind.Kick;
        if (!atks.ContainsKey(k)) k = Kind.Jab;
        if (atks.ContainsKey(k)) plan.Add(new Step(k, k == Kind.Kick));
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
    }

    // ---------------- vihellys ----------------

    bool FindAllies(out Enemy punk, out Enemy skater)
    {
        punk = skater = null;
        float bp = 9999f, bs = 9999f;
        var area = Area.Current;
        float mx = transform.position.x;
        foreach (var en in Enemy.All.ToArray())
        {
            if (en == null || en == e || en.IsDead || en.IsAwake) continue;
            bool isPunk = en.displayName == "Punkkari", isSkater = en.displayName == "Skettari";
            if (!isPunk && !isSkater) continue;
            float x = en.transform.position.x;
            if (area != null && area.walkMaxX > area.walkMinX && (x < area.walkMinX - 1f || x > area.walkMaxX + 1f)) continue;   // vain tämän viemärin väki
            float d = Mathf.Abs(x - mx);
            if (isPunk && d < bp) { bp = d; punk = en; }
            if (isSkater && d < bs) { bs = d; skater = en; }
        }
        return punk != null || skater != null;
    }

    bool WantsWhistle()
    {
        if (!Has(whistle) || Time.time - lastWhistleTime < whistleGap) return false;
        bool due = (whistles == 0 && fightTime >= firstWhistleDelay)
                || (whistles == 1 && e.HealthFraction < 0.65f)
                || (whistles >= 2 && whistles < 4 && e.HealthFraction < 0.3f);
        return due && FindAllies(out _, out _);
    }

    void Summon()
    {
        if (!FindAllies(out var punk, out var skater)) return;
        if (punk != null) punk.WakeUp();
        if (skater != null) skater.WakeUp();
    }

    // ---------------- tynnyrit ----------------

    /// Heron potkaisema/heittämä tynnyri tulossa kohti: potkaisee takaisin.
    bool TryReflect(Vector3 me)
    {
        if (reflectCd > 0f || !atks.ContainsKey(Kind.BarrelKick)) return false;
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.InFlightByHero) continue;
            Vector3 cp = c.transform.position;
            float vx = c.VelX;
            if (Mathf.Abs(vx) < 3f || (me.x - cp.x) * vx <= 0f) continue;                 // ei tule tätä kohti
            if (Mathf.Abs(cp.y - me.y) > 0.9f || c.Height > 2.5f) continue;
            if (Mathf.Abs(me.x - cp.x) > Mathf.Max(5f, Mathf.Abs(vx) * 0.55f + 2f)) continue;
            e.FacingRight = cp.x > me.x;                                                   // kasvot tynnyriä (ja heroa) kohti
            reflectCd = 0.9f;
            StartAct(Kind.BarrelKick, false);
            return true;
        }
        return false;
    }

    /// Hakee heron ja itsensä väliin seisovan tynnyrin, kävelee sen taakse ja potkaisee sen vierimään heroa kohti.
    bool BarrelPlan(float dt, PlayerController hero, Vector3 me, Vector3 hp, float dir)
    {
        if (!atks.ContainsKey(Kind.BarrelKick)) return false;
        float adx = Mathf.Abs(hp.x - me.x);
        if (targetBarrel == null)
        {
            if (barrelCd > 0f || adx < 4.5f || adx > 18f || hero.IsFloored) return false;
            float best = 999f;
            foreach (var c in Crate.All.ToArray())
            {
                if (c == null || !c.IsKickable) continue;
                Vector3 cp = c.transform.position;
                if (cp.x < zoneMin || cp.x > zoneMax) continue;
                if ((cp.x - me.x) * dir < 0.8f || (hp.x - cp.x) * dir < 2.5f) continue;   // tynnyrin on oltava hänen ja heron välissä
                if (Mathf.Abs(cp.y - hp.y) > 0.9f) continue;                                 // samassa syvyydessä heron kanssa
                float d = Mathf.Abs(cp.x - me.x);
                if (d < best) { best = d; targetBarrel = c; }
            }
            if (targetBarrel == null) { barrelCd = 1.5f; return false; }
            barrelGiveUp = Time.time + 6f;
        }
        var b = targetBarrel;
        if (b == null || !b.IsKickable || Time.time > barrelGiveUp || (hp.x - b.transform.position.x) * dir < 1.5f || Mathf.Abs(b.transform.position.y - hp.y) > 1.4f)
        { targetBarrel = null; barrelCd = 3f; return false; }
        // kävelee tynnyrin taakse (heron vastakkaiselle puolelle)
        Vector3 bp = b.transform.position;
        float kx = bp.x - dir * 1.7f, ky = bp.y;
        float ddx = kx - me.x, ddy = ky - me.y;
        if (Mathf.Abs(ddx) < 0.35f && Mathf.Abs(ddy) < 0.3f)
        {
            targetBarrel = null; barrelCd = barrelCooldown;
            StartAct(Kind.BarrelKick, false);
            return true;
        }
        float dyMove = Mathf.Abs(ddy) > 0.1f ? Mathf.Sign(ddy) * Mathf.Min(Mathf.Abs(ddy), depthSpeed * dt) : 0f;
        bool run = Mathf.Abs(ddx) > 5f;
        MoveXY(Mathf.Sign(ddx), run ? runSpeed : walkSpeed, dyMove, dt, Mathf.Abs(ddx) > 0.15f);
        e.FacingRight = dir > 0f;   // kasvot heroa kohti, vaikka tynnyrin taakse mennään
        CurrentSprite = Has(walk) ? Walk(run) : CurrentSprite;
        return true;
    }

    // ---------------- toiminnan suoritus ----------------

    void StartAct(Kind k, bool finisher)
    {
        plan = new List<Step> { new Step(k, finisher) };
        planPos = 0; comboRunning = false;
        StartStep(e.Hero);
    }

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        mode = Mode.Act; t = 0f; hitDone = false; whistleDone = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (cur.kind == Kind.Whistle || cur.kind == Kind.Annoyed || cur.kind == Kind.Baguette) { baguetteDone = false; return; }
        var a = atks[cur.kind];
        float scale = (planPos > 0 || comboRunning) ? comboWindupScale : 1f;
        tWind = a.impact * windupFrameTime * scale;
        if (cur.kind != Kind.BarrelKick) PlayPunchSound();
        tHold = impactHold;
        tRecFrame = recoverFrameTime * (cur.finisher ? 1.3f : 1f);
        curImpact = a.impact; curFrames = a.frames.Length;
        prevLunge = a.lunge;
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;

        if (cur.kind == Kind.Whistle)
        {
            int wi = ThrowPose.Index(WhistleTimes, t);
            CurrentSprite = whistle[Mathf.Min(wi < 0 ? whistle.Length - 1 : wi, whistle.Length - 1)];
            if (!whistleDone && t >= ThrowPose.Start(WhistleTimes, WhistleSoundFrame))
            {
                whistleDone = true;
                whistles++; lastWhistleTime = Time.time;
                if (whistleSound != null && e.Audio != null) e.Audio.PlayOneShot(whistleSound, whistleVolume);
                Summon();
            }
            if (wi < 0) EndPlan();
            return;
        }

        if (cur.kind == Kind.Baguette)
        {
            e.FacingRight = hp.x > me.x;
            int bi = ThrowPose.Index(BaguetteTimes, t);
            CurrentSprite = baguetteThrow[Mathf.Min(bi < 0 ? baguetteThrow.Length - 1 : bi, baguetteThrow.Length - 1)];
            if (!baguetteDone && t >= ThrowPose.Start(BaguetteTimes, BaguetteReleaseFrame))
            {
                baguetteDone = true;
                float fd = e.FacingRight ? 1f : -1f;
                RatBaguette.Spawn(transform.position + new Vector3(fd * 1.3f, 0f, 0f), 2.1f, hero, e, baguetteDamage, baguetteSpeed, baguetteSprites);
            }
            if (bi < 0) EndPlan();
            return;
        }
        if (cur.kind == Kind.Annoyed)
        {
            e.FacingRight = hp.x > me.x;
            if (t <= dt * 1.5f && frustrated != null && e.Audio != null) e.Audio.PlayOneShot(frustrated, frustratedVolume);
            int ai = ThrowPose.Index(AnnoyedTimes, t);
            CurrentSprite = annoyed[Mathf.Min(ai < 0 ? annoyed.Length - 1 : ai, annoyed.Length - 1)];
            if (ai < 0) EndPlan();
            return;
        }

        var a = atks[cur.kind];
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < tWind + 0.02f && cur.kind != Kind.BarrelKick)
        {
            float lungeSpeed = tWind > 0.01f ? prevLunge / tWind : 0f;
            if (Mathf.Abs(hp.x - me.x) > 1.1f) MoveXY(face, lungeSpeed, 0f, dt);
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.3f * dt)));
        }
        int idx;
        if (t < tWind) idx = curImpact > 0 ? Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * curImpact), curImpact - 1) : 0;
        else if (t < tWind + tHold) idx = curImpact;
        else idx = Mathf.Min(curImpact + 1 + (int)((t - tWind - tHold) / Mathf.Max(0.001f, tRecFrame)), curFrames - 1);
        CurrentSprite = a.frames[idx];

        if (!hitDone && t >= tWind)
        {
            hitDone = true;
            if (cur.kind == Kind.BarrelKick) KickBarrels(a);
            else TryHit(hero, a);
        }
        float total = tWind + tHold + Mathf.Max(0, curFrames - 1 - curImpact) * tRecFrame;
        if (t >= total)
        {
            planPos++;
            if (planPos < plan.Count && ComboTargetOk(hero)) StartStep(hero);
            else EndPlan();
        }
    }

    bool ComboTargetOk(PlayerController hero)
    {
        if (hero == null || hero.IsFloored) return false;
        Vector3 me = transform.position, hp = hero.transform.position;
        return Mathf.Abs(hp.x - me.x) < 3.6f && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.6f;
    }

    void EndPlan()
    {
        mode = Mode.Fight; modeTime = 0f; comboRunning = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    void AbortAction()
    {
        if (mode == Mode.Act) mode = Mode.Fight;
        comboRunning = false; targetBarrel = null;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Mathf.Max(decisionTimer, 0.4f);
    }

    void TryHit(PlayerController hero, Atk a)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > a.reach + 0.2f) return;
        if (Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f) return;
        if (hero.AirHeight > 0.9f) return;
        if (cur.finisher || cur.kind == Kind.Kick)
        {
            bool k = hero.TakeKnockdown(a.damage + (cur.finisher ? finisherBonus : 0), me.x, 4.5f, 5f, e);
            if (k) { HitFx.Freeze(0.1f); CameraFollow.Shake(0.2f, 0.18f); }
            return;
        }
        hero.TakeHit(a.damage, me.x, e, planPos > 0);
    }

    /// Potku osuu tynnyriin: seisova lähtee vierimään heroa kohti, heron tuoma lähtee takaisin. Samalla osuu heroon, jos tämä on ulottuvilla.
    void KickBarrels(Atk a)
    {
        Vector3 me = transform.position;
        float dir = e.FacingRight ? 1f : -1f;
        bool any = false;
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.IsBarrel || !(c.IsKickable || c.InFlightByHero)) continue;
            Vector3 cp = c.transform.position;
            float rx = (cp.x - me.x) * dir;
            if (rx < -0.4f || rx > a.reach + 0.6f || Mathf.Abs(cp.y - me.y) > 0.8f) continue;
            if (c.Kick(dir, e, barrelDamage, barrelKickSpeed)) any = true;
        }
        if (any) { HitFx.OnHit(false); CameraFollow.Shake(0.08f, 0.1f); }
        var hero = e.Hero;
        if (hero != null) TryHit(hero, a);
    }

    // ---------------- äänet ----------------

    void SpeakMaybe()
    {
        if (speak == null || speak.Length == 0 || e.Audio == null || nextSpeak < 0f || Time.time < nextSpeak) return;
        int i = Random.Range(0, speak.Length);
        if (speak.Length > 1 && i == lastSpeak) i = (i + 1) % speak.Length;
        lastSpeak = i;
        if (speak[i] != null)
        {
            e.Audio.PlayOneShot(speak[i], speakVolume);
            nextSpeak = Time.time + speak[i].length + Random.Range(speakInterval.x, speakInterval.y);
        }
        else nextSpeak = Time.time + 5f;
    }

    void PlayPunchSound()
    {
        if (punchSounds == null || punchSounds.Length == 0 || e.Audio == null) return;
        if (Time.time - lastPunchSound < 0.25f || Random.value > 0.8f) return;   // nopeassa sarjassa ei joka lyönnistä
        lastPunchSound = Time.time;
        var c = punchSounds[Random.Range(0, punchSounds.Length)];
        if (c != null) e.Audio.PlayOneShot(c, punchVolume);
    }

    // ---------------- apu ----------------

    Sprite Loop(Sprite[] a, float ft)
    {
        if (!Has(a)) return CurrentSprite;
        return a[(int)(animClock / Mathf.Max(0.001f, ft)) % a.Length];
    }

    Sprite PingPong(Sprite[] a, float ft)
    {
        if (!Has(a)) return CurrentSprite;
        int n = a.Length;
        if (n == 1) return a[0];
        int period = 2 * n - 2, i = (int)(animClock / Mathf.Max(0.001f, ft)) % period;
        return a[i < n ? i : period - i];
    }
}

/// Rottakeisarin heittämä kovettunut patonki: lentää heroa kohti pyörien, osuu heroon pieneen vahinkoon (hypyllä yli).
public class RatBaguette : MonoBehaviour
{
    Sprite[] frames;
    SpriteRenderer sr;
    Vector2 vel;
    float height, vHeight, life, t;
    PlayerController hero;
    Enemy owner;
    int damage;

    public static void Spawn(Vector3 start, float startHeight, PlayerController hero, Enemy owner, int damage, float speed, Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return;
        var go = new GameObject("Rotan patonki");
        go.transform.position = new Vector3(start.x, start.y, 0f);
        var b = go.AddComponent<RatBaguette>();
        b.hero = hero; b.owner = owner; b.damage = damage; b.frames = frames;
        b.sr = new GameObject("Kuva").AddComponent<SpriteRenderer>();
        b.sr.transform.SetParent(go.transform, false);
        b.sr.sprite = frames[0];
        b.height = startHeight;
        Vector3 target = hero != null ? hero.transform.position : start + Vector3.right * 6f;
        Vector2 d = new Vector2(target.x - start.x, target.y - start.y);
        float dist = Mathf.Max(0.5f, Mathf.Abs(d.x));
        float time = dist / Mathf.Max(1f, speed);
        b.vel = new Vector2(d.x, d.y) / time;
        b.vHeight = (1.2f - startHeight) / time;   // laskee rinnan korkeudelle perille tultaessa
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt; t += dt;
        transform.position += new Vector3(vel.x, vel.y, 0f) * dt;
        height += vHeight * dt;
        int f = (int)(t / 0.045f) % frames.Length;
        sr.sprite = frames[f];
        // kuvan keskikohta (pivot alareunassa) maan yläpuolelle
        float half = sr.sprite != null ? sr.sprite.bounds.extents.y : 0f;
        sr.transform.localPosition = new Vector3(0f, Mathf.Max(0.05f, height) - half, 0f);
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 8;
        if (hero != null)
        {
            Vector3 hp = hero.transform.position, p = transform.position;
            if (Mathf.Abs(hp.x - p.x) < 0.7f && Mathf.Abs(hp.y - p.y) < 0.55f && hero.AirHeight < 1.2f && height > 0.2f)
            {
                hero.TakeHit(damage, p.x - Mathf.Sign(vel.x) * 0.5f, owner, false);
                Destroy(gameObject);
                return;
            }
        }
        if (life > 3f || height < -0.3f) Destroy(gameObject);
    }
}
