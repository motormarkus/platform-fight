using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pierre, viemärin toinen minibossi (ensimmäisen kadun viemäri). Oma liikesarja-aivo Enemy-komponentin päällä (kuten Deacon ja Rottakeisari):
/// Enemy hoitaa osumat, torjunnan, kaatumisen, ylösnousun, energiapalkin ja kipuäänet; tämä ohjaa liikkeen, iskut ja erikoisliikkeet.
/// - Rauhallisena kävelee edestakaisin lähtöpaikkansa ympärillä; hero lähellä: taisteluasento ja -kävely.
/// - Pysyy viemärin keskiosassa (zoneHalfWidth), ei koskaan päädyissä.
/// - Juoksee kaukaa ja päättää juoksun juoksuiskuun. Jabit, suora, jabisarja ja potku kombona.
/// - Erikoisliikkeet: tornadopotku (osuu molempiin suuntiin) ja ylhäältä isku (maahan, iskuaalto).
/// Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (159).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class PierreBoss : MonoBehaviour, IEnemyBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] walk;          // normaali kävely (rauhallinen)
    public Sprite[] idle;          // taisteluidle (edestakaisin)
    public Sprite[] fightWalk;     // taistelukävely
    public Sprite[] run;           // juoksu
    public Sprite[] jab1, jab2, jab3, kick, runPunch, tornado, overhead;

    [Header("Äänet")]
    public AudioClip specialSound, welcome;
    public float welcomeVolume = 1f;
    public AudioClip[] speak, punchSounds;
    public float speakVolume = 0.9f, punchVolume = 0.9f, specialVolume = 1f;
    public Vector2 speakInterval = new Vector2(9f, 16f);
    float nextSpeak = -1f, lastPunchSound = -9f;
    int lastSpeak = -1;

    [Header("Ajoitus")]
    public float idleFrameTime = 0.2f;
    [Tooltip("Yhden kävelysilmukan kesto (s).")]
    public float walkCycle = 1.4f, fightWalkCycle = 0.9f, runCycle = 0.55f;
    public float recoverFrameTime = 0.055f, impactHold = 0.07f, comboWindupScale = 0.6f;

    [Header("Liike")]
    public float patrolSpeed = 1.1f, patrolRange = 5f;
    public float walkSpeed = 2.5f, runSpeed = 5.4f, depthSpeed = 2.6f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet.")]
    public float wakeDistance = 10f;
    public float rangeMin = 1.2f, rangeMax = 1.9f, runDistance = 6.5f;
    [Tooltip("Näin kauas lähtöpaikastaan (x) hän liikkuu korkeintaan taistelussa.")]
    public float zoneHalfWidth = 24f;

    [Header("Iskut")]
    public int jab1Damage = 9, jab2Damage = 11, jab3Damage = 12, kickDamage = 20, runPunchDamage = 18, tornadoDamage = 12, overheadDamage = 20, finisherBonus = 3;
    [Range(0f, 1f)] public float comboChance = 0.8f;
    public Vector2 decisionPause = new Vector2(0.15f, 0.45f);
    [Tooltip("Erikoisliike (tornadopotku tai ylhäältä isku) aikaisintaan näin monen sekunnin välein, kun hero on lähellä.")]
    public Vector2 specialInterval = new Vector2(5f, 9f);
    [Range(0f, 1f)] public float specialChance = 0.5f;

    // ---------------- sisäinen tila ----------------
    enum Mode { Dormant, Fight, Act }
    enum Kind { Jab1, Jab2, Jab3, Kick, RunPunch, Tornado, Overhead }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk { public Sprite[] frames; public int impact, impact2 = -1; public float ft, reach, lunge; public int damage; public bool hitsBack, wave; }

    Enemy e;
    Mode mode = Mode.Dormant;
    float modeTime, lastTick = -99f, decisionTimer = 0.5f, animClock, specialCd = 4f;
    float homeX, zoneMin, zoneMax, patrolTarget, patrolPause;
    float runningFor;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    Step cur;
    float t, tWind, tHold, tRecFrame, prevLunge;
    int curImpact, curImpact2, curFrames;
    bool hitDone, hit2Done, comboRunning;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && comboRunning;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) { return false; }

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        homeX = transform.position.x;
        patrolTarget = homeX;
        BuildAttacks();
    }

    void BuildAttacks()
    {
        atks.Clear();
        Add(Kind.Jab1, jab1, 2, -1, 0.06f, 2.4f, 0.4f, jab1Damage);
        Add(Kind.Jab2, jab2, 2, -1, 0.06f, 2.5f, 0.4f, jab2Damage);
        Add(Kind.Jab3, jab3, 2, 5, 0.055f, 2.6f, 0.5f, jab3Damage);
        Add(Kind.Kick, kick, 10, -1, 0.05f, 3.4f, 0.4f, kickDamage);
        Add(Kind.RunPunch, runPunch, 6, -1, 0.05f, 3.0f, 2.2f, runPunchDamage);
        Add(Kind.Tornado, tornado, 10, 15, 0.045f, 2.7f, 0f, tornadoDamage);
        Add(Kind.Overhead, overhead, 8, -1, 0.06f, 2.9f, 0f, overheadDamage);
        if (atks.ContainsKey(Kind.Tornado)) atks[Kind.Tornado].hitsBack = true;
        if (atks.ContainsKey(Kind.Overhead)) atks[Kind.Overhead].wave = true;
    }

    void Add(Kind k, Sprite[] frames, int impact, int impact2, float ft, float reach, float lunge, int damage)
    {
        if (frames == null || frames.Length <= impact) return;
        atks[k] = new Atk { frames = frames, impact = impact, impact2 = impact2 < frames.Length ? impact2 : -1, ft = ft, reach = reach, lunge = lunge, damage = damage };
    }

    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    // ---------------- Enemyn kutsut ----------------

    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();
        lastTick = Time.time;
        modeTime += dt; animClock += dt; specialCd -= dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        if (mode == Mode.Dormant && e.IsAwake) StartFight();
        switch (mode)
        {
            case Mode.Dormant:
                e.BrainPhaseSet(Enemy.BrainPhase.Idle);
                Patrol(dt);
                if (Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 4f) StartFight();
                break;
            case Mode.Fight: FightUpdate(dt, hero); SpeakMaybe(); break;
            case Mode.Act: ActUpdate(dt, hero); break;
        }
    }

    void StartFight()
    {
        mode = Mode.Fight; modeTime = 0f;
        zoneMin = homeX - zoneHalfWidth; zoneMax = homeX + zoneHalfWidth;
        e.IsAwake = true;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        decisionTimer = 0.6f; specialCd = Random.Range(3f, 6f); runningFor = 0f;
        if (welcome != null && e.Audio != null) e.Audio.PlayOneShot(welcome, welcomeVolume);   // tervetuloa, kun hänet nähdään
        nextSpeak = Time.time + (welcome != null ? welcome.length : 0f) + Random.Range(speakInterval.x * 0.5f, speakInterval.y * 0.5f);
    }

    /// Rauhallinen kävely edestakaisin kotipaikan ympärillä (normaali kävely).
    void Patrol(float dt)
    {
        float me = transform.position.x;
        if (patrolPause > 0f)
        {
            patrolPause -= dt;
            CurrentSprite = Loop(Has(idle) ? idle : walk, idleFrameTime);
            return;
        }
        float d = patrolTarget - me;
        if (Mathf.Abs(d) < 0.15f)
        {
            patrolTarget = homeX + Random.Range(-patrolRange, patrolRange);
            patrolPause = Random.Range(0.8f, 2.2f);
            return;
        }
        e.FacingRight = d > 0f;
        e.BrainMove(new Vector2(Mathf.Sign(d) * Mathf.Min(Mathf.Abs(d), patrolSpeed * dt), 0f));
        CurrentSprite = Has(walk) ? Cycle(walk, walkCycle) : CurrentSprite;
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

        bool heroInZone = hp.x >= zoneMin - 2f && hp.x <= zoneMax + 2f;
        bool moving = false, running = false;
        if (!heroInZone)
        {
            float tx = Mathf.Clamp(hp.x, zoneMin, zoneMax);
            if (Mathf.Abs(tx - me.x) > 0.3f) { MoveXY(Mathf.Sign(tx - me.x), walkSpeed, 0f, dt); moving = true; }
            runningFor = 0f;
        }
        else if (adx > rangeMax + 0.9f)
        {
            running = adx > runDistance && Has(run);
            float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.5f * dt) : 0f;
            MoveXY(dir, running ? runSpeed : walkSpeed, dyMove, dt);
            moving = true;
            runningFor = running ? runningFor + dt : 0f;
        }
        else
        {
            float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.5f * dt) : 0f;
            float stepX = 0f;
            if (adx > rangeMax) stepX = dir * walkSpeed * 0.8f;
            else if (adx < rangeMin) stepX = -dir * 1.3f;
            if (stepX != 0f || dyMove != 0f) MoveXY(Mathf.Sign(stepX == 0f ? 1f : stepX), Mathf.Abs(stepX), dyMove, dt, stepX != 0f);
            runningFor = 0f;
        }
        if (moving)
            CurrentSprite = running ? Cycle(run, runCycle) : Has(fightWalk) ? Cycle(fightWalk, fightWalkCycle) : Loop(idle, idleFrameTime);
        else
            CurrentSprite = PingPong(idle, idleFrameTime);

        // juoksusta suoraan juoksuiskuun (juoksu on kestänyt tovin ja hero on lähellä)
        if (runningFor > 0.6f && adx <= 4.4f && Mathf.Abs(dy) <= e.depthTolerance + 0.3f && atks.ContainsKey(Kind.RunPunch) && !hero.IsFloored)
        {
            runningFor = 0f;
            StartAct(Kind.RunPunch, true);
            return;
        }

        if (decisionTimer <= 0f && heroInZone && adx <= 3.4f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f && !hero.IsFloored)
        {
            ChoosePlan(adx);
            if (plan.Count > 0) StartStep(hero);
        }
    }

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
        // erikoisliike lähietäisyydeltä
        if (specialCd <= 0f && adx <= 2.9f && Random.value < specialChance)
        {
            specialCd = Random.Range(specialInterval.x, specialInterval.y);
            Kind sp = Random.value < 0.5f ? Kind.Tornado : Kind.Overhead;
            if (!atks.ContainsKey(sp)) sp = sp == Kind.Tornado ? Kind.Overhead : Kind.Tornado;
            if (atks.ContainsKey(sp)) { plan.Add(new Step(sp, true)); decisionTimer = Random.Range(decisionPause.x, decisionPause.y); return; }
        }
        if (adx > 2.9f && atks.ContainsKey(Kind.Kick)) { plan.Add(new Step(Kind.Kick, true)); decisionTimer = Random.Range(decisionPause.x, decisionPause.y); return; }   // kauempaa potku
        if (Random.value < comboChance)
        {
            switch (Random.Range(0, 5))
            {
                case 0: plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Jab2, true) }; break;
                case 1: plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Jab2), new Step(Kind.Jab3, true) }; break;
                case 2: plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Jab2), new Step(Kind.Jab3), new Step(Kind.Kick, true) }; break;
                case 3: plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Kick, true) }; break;
                default: plan = new List<Step> { new Step(Kind.Jab3), new Step(Kind.Kick, true) }; break;
            }
            plan.RemoveAll(s => !atks.ContainsKey(s.kind));
            comboRunning = plan.Count > 1;
            return;
        }
        float r = Random.value;
        Kind k = r < 0.35f ? Kind.Jab1 : r < 0.65f ? Kind.Jab2 : r < 0.9f ? Kind.Jab3 : Kind.Kick;
        if (!atks.ContainsKey(k)) k = Kind.Jab1;
        if (atks.ContainsKey(k)) plan.Add(new Step(k, k == Kind.Kick));
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
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
        mode = Mode.Act; t = 0f; hitDone = false; hit2Done = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        var a = atks[cur.kind];
        float scale = (planPos > 0 || comboRunning) ? comboWindupScale : 1f;
        tWind = a.impact * a.ft * scale;
        if (cur.kind == Kind.Tornado || cur.kind == Kind.Overhead) { if (specialSound != null && e.Audio != null) e.Audio.PlayOneShot(specialSound, specialVolume); }
        else PlayPunchSound();
        tHold = impactHold;
        tRecFrame = recoverFrameTime * (cur.finisher ? 1.2f : 1f);
        curImpact = a.impact; curImpact2 = a.impact2; curFrames = a.frames.Length;
        prevLunge = a.lunge;
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        var a = atks[cur.kind];
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < tWind + 0.02f && prevLunge > 0f)
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

        if (!hitDone && t >= tWind) { hitDone = true; TryHit(hero, a, false); }
        if (curImpact2 >= 0 && !hit2Done && t >= tWind + tHold + Mathf.Max(0, curImpact2 - curImpact - 1) * tRecFrame) { hit2Done = true; TryHit(hero, a, true); }
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
        comboRunning = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Mathf.Max(decisionTimer, 0.4f);
    }

    void TryHit(PlayerController hero, Atk a, bool second)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front && !a.hitsBack && !a.wave) return;                       // tornadopotku ja iskuaalto osuvat myös selän taakse
        if (Mathf.Abs(dx) > a.reach + 0.2f) return;
        float depthTol = a.wave ? 1.2f : e.depthTolerance + 0.1f;
        if (Mathf.Abs(hp.y - me.y) > depthTol) return;
        if (hero.AirHeight > (a.wave ? 0.6f : 0.9f)) return;                // iskuaallon yli voi hypätä
        bool last = !second && a.impact2 < 0 || second;
        if (cur.finisher && last || cur.kind == Kind.Kick || cur.kind == Kind.RunPunch || a.wave)
        {
            bool k = hero.TakeKnockdown(a.damage + (cur.finisher ? finisherBonus : 0), me.x, 4.5f, 5f, e);
            if (k) { HitFx.Freeze(0.1f); CameraFollow.Shake(a.wave ? 0.3f : 0.2f, 0.18f); }
            return;
        }
        hero.TakeHit(a.damage, me.x, e, planPos > 0 || second);
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

    Sprite Cycle(Sprite[] a, float cycle)
    {
        if (!Has(a)) return CurrentSprite;
        float ft = cycle / a.Length;
        return a[(int)(animClock / ft) % a.Length];
    }

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
