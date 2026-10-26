using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gustav, uusiutuva pomo. Oma liikesarja-aivo Enemy-komponentin päällä (kuten Marcus ja Deacon): Enemy hoitaa osumat, kaatumisen,
/// ylösnousun, suojauksen ja energiapalkin; tämä ohjaa liikkeen ja hyökkäykset. Nopea ja kova vastus (Deaconin taso).
/// Liikkeet: jab, takasuora, etupotku, takajalan sivupotku (Spin), takajalan kiertopotku, kirvespotku (rikkoo suojan),
/// uppercut (hidas, tehokas), hyppypotku (lähestyminen), hiekka (huumaa 2 s ellei suojaa) ja heitto (rinnuksista toiselle puolelle).
/// Hiekan osuma -> Gustav hakee heiton heti. Energian loppuessa (undying) hän kaatuu, nousee ja juoksee pois (sitten piiloon).
/// Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (212).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class GustavBrain : MonoBehaviour, IEnemyBrain, ICornerBurstBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] idle, walk, run;
    public Sprite[] jab, cross, front, round, spin, axe, upper, jumpKick, sandThrow, grab;
    [Tooltip("Hiekan puuskat (5) ja pilvi.")]
    public Sprite[] sandPuffs;
    public Sprite sandCloud;

    [Header("Liike")]
    public float walkSpeed = 3.6f, runSpeed = 6.4f, shuffleSpeed = 2.4f, depthSpeed = 3.8f;
    public float walkFrameTime = 0.06f, runFrameTime = 0.045f, idleFrameTime = 0.12f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet (lisäksi herää osumasta).")]
    public float wakeDistance = 9f;
    [Tooltip("Mieluisa iskuetäisyys (x).")]
    public float rangeMin = 1.6f, rangeMax = 2.6f;

    [Header("Iskut")]
    public float impactHold = 0.05f;
    [Tooltip("Kombon seuraavan iskun valmistelu on näin paljon nopeampi (1 = ei).")]
    public float comboWindupScale = 0.55f;
    public int jabDamage = 6, crossDamage = 9, frontDamage = 10, roundDamage = 12, spinDamage = 14, axeDamage = 15, upperDamage = 17, jumpDamage = 13, throwDamage = 20;
    [Tooltip("Kombon viimeisen iskun lisävahinko.")]
    public int finisherBonus = 3;
    [Tooltip("Uppercutin valmistelu on hidas, jotta sen ehtii väistää (s per kuva).")]
    public float upperWindFrameTime = 0.07f;
    [Range(0f, 1f)] public float comboChance = 0.8f;
    public Vector2 decisionPause = new Vector2(0.12f, 0.45f);

    [Header("Hiekka ja heitto")]
    public float sandCooldown = 7f, sandSpeed = 7f, sandLife = 1.0f, stunSeconds = 2f;
    [Range(0f, 1f)] public float sandChance = 0.4f;

    [Header("Torjunnan jälkeen")]
    public float counterWindow = 1.1f;
    [Tooltip("Torjunnan jälkeen uppercut (muuten etupotku tai jab).")]
    [Range(0f, 1f)] public float upperAfterBlock = 0.4f;
    public float breakerCooldown = 2.4f;

    [Header("Pako (energia loppu)")]
    public float fleeSpeed = 7f, fleeMaxTime = 3.5f;

    enum Mode { Dormant, Fight, Act, Flee }
    enum Kind { Jab, Cross, Front, Round, Spin, Axe, Upper, JumpKick, Sand, Grab }
    enum Hit { Normal, Knock, Upper, Break }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk
    {
        public Sprite[] frames; public int[] order; public int impact;   // order[impact] = osumahetki
        public float windFt, recFt, reach, lunge; public int damage; public Hit hit;
    }

    Enemy e;
    Mode mode = Mode.Dormant;
    float lastTick = -99f, decisionTimer = 0.6f, animClock, t, tWind, tHold, tRec;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    Step cur;
    bool hitDone, comboRunning, fastStep;
    float nextSandTime = 3f, nextJumpTime = 2f, lastCounterBlockStamp = -99f, nextBreakerTime, fleeT, fleeDir;
    bool counterUsed;
    float stepLunge, stepTravelTime;
    readonly List<float> hitTimes = new List<float>();

    // heitto
    int grabPhase; float grabT; bool holding;
    // hiekka
    bool sandReleased, sandWasHit; float sandHitStamp = -99f;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && comboRunning && cur.kind != Kind.Grab;

    float DepthFollow => depthSpeed * 0.5f;
    static bool Has(Sprite[] a) => a != null && a.Length > 0;
    static int[] Seq(int from, int to) { var l = new List<int>(); if (from <= to) for (int i = from; i <= to; i++) l.Add(i); else for (int i = from; i >= to; i--) l.Add(i); return l.ToArray(); }
    static int[] Cat(params int[][] parts) { var l = new List<int>(); foreach (var p in parts) l.AddRange(p); return l.ToArray(); }

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        // (kuvat, järjestys, osumakohta järjestyksessä, valmistelun kuva-aika, palautuksen kuva-aika, kantama, ponnahdus, vahinko, tyyppi)
        Add(Kind.Jab, jab, Seq(0, 8), 4, 0.03f, 0.035f, 2.4f, 0.45f, jabDamage, Hit.Normal);
        Add(Kind.Cross, cross, Seq(0, 8), 4, 0.04f, 0.04f, 2.6f, 0.6f, crossDamage, Hit.Normal);
        Add(Kind.Front, front, Seq(0, 10), 4, 0.04f, 0.04f, 2.7f, 0.5f, frontDamage, Hit.Normal);
        Add(Kind.Round, round, Seq(0, 11), 4, 0.045f, 0.04f, 3.0f, 0.7f, roundDamage, Hit.Knock);
        Add(Kind.Spin, spin, Cat(Seq(0, 5), Seq(4, 0)), 4, 0.05f, 0.045f, 2.9f, 0.6f, spinDamage, Hit.Knock);
        Add(Kind.Axe, axe, Cat(Seq(0, 5), Seq(4, 0)), 5, 0.06f, 0.05f, 2.5f, 0.4f, axeDamage, Hit.Break);
        Add(Kind.Upper, upper, Seq(0, 13), 7, upperWindFrameTime, 0.045f, 1.9f, 0.45f, upperDamage, Hit.Upper);
        Add(Kind.JumpKick, jumpKick, Seq(0, 9), 3, 0.05f, 0.05f, 2.4f, 0f, jumpDamage, Hit.Knock);
    }

    void Add(Kind k, Sprite[] frames, int[] order, int impact, float windFt, float recFt, float reach, float lunge, int damage, Hit hit)
    {
        if (!Has(frames) || frames.Length <= Mathf.Max(order)) return;
        atks[k] = new Atk { frames = frames, order = order, impact = impact, windFt = windFt, recFt = recFt, reach = reach, lunge = lunge, damage = damage, hit = hit };
    }

    // ---------------- Enemyn kutsut ----------------

    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();   // väliin tuli osuma, kaatuminen tms.
        lastTick = Time.time;
        animClock += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        if (mode == Mode.Flee) { FleeUpdate(dt, hero); return; }
        if (e.UndyingFallen) { StartFlee(hero); FleeUpdate(dt, hero); return; }

        if (mode == Mode.Dormant)
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            CurrentSprite = Ping(idle, idleFrameTime);
            bool near = Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 3.5f;
            if (e.IsAwake || near) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.5f; }
            else return;
        }
        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    /// Nurkkapurkaus (pomo ajettu nurkkaan): kirves ja takapotku (selkä kameraan), jota ei voi keskeyttää.
    public void CornerBurst()
    {
        if (mode == Mode.Dormant || mode == Mode.Flee || e.Hero == null || !atks.ContainsKey(Kind.Axe)) return;
        plan = new List<Step> { new Step(Kind.Axe) };
        if (atks.ContainsKey(Kind.Spin)) plan.Add(new Step(Kind.Spin, true));
        planPos = 0; comboRunning = plan.Count > 1; fastStep = false;
        lastTick = Time.time;
        StartStep(e.Hero);
    }

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi.
    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (mode == Mode.Dormant) return false;
        if (scriptedRun) return true;   // kopteriin juoksevaan ei osu
        if (e.Hero != null && mode != Mode.Flee)
        {
            float now = Time.time;
            hitTimes.Add(now); hitTimes.RemoveAll(x => now - x > 1.1f);
            // osumaputki: hero hakkaa jatkuvasti -> isku ohi ja vastasarja etupotku + kiertopotku (mode == Fight)
            if (mode == Mode.Fight && hitTimes.Count >= 3 && now >= nextBreakerTime && !knockdown && atks.ContainsKey(Kind.Front) && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 3.6f)
            {
                nextBreakerTime = now + breakerCooldown; hitTimes.Clear();
                plan = new List<Step> { new Step(Kind.Front) };
                if (atks.ContainsKey(Kind.Spin)) plan.Add(new Step(Kind.Spin, true));
                planPos = 0; comboRunning = plan.Count > 1; fastStep = false;
                lastTick = now;
                StartStep(e.Hero);
                return true;    // tämä isku ohi (Gustav pujahtaa ja vastaa heti sarjalla)
            }
        }
        return false;
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

        // hiekka osui (tai hero on huumattu): heitto heti
        bool dazed = hero.IsDazed || Time.time - sandHitStamp < 2.5f;
        if (dazed && !hero.IsFloored && atks != null && Has(grab))
        {
            if (adx > 1.9f || Mathf.Abs(dy) > e.depthTolerance + 0.2f)
            {
                MoveToward(dt, dir, dy, adx, true);
                CurrentSprite = Loop(Has(run) ? run : walk, runFrameTime);
                return;
            }
            sandHitStamp = -99f;
            plan = new List<Step> { new Step(Kind.Grab) }; planPos = 0; comboRunning = false; fastStep = true;
            StartStep(hero);
            return;
        }

        // torjunnan jälkeen vastaisku: uppercut (hidas) tai etupotku/jab
        if (e.LastBlockTime > lastCounterBlockStamp) { lastCounterBlockStamp = e.LastBlockTime; counterUsed = false; }
        if (!counterUsed && Time.time - e.LastBlockTime < counterWindow && adx < 3.2f)
        {
            counterUsed = true;
            Kind k = Random.value < upperAfterBlock && atks.ContainsKey(Kind.Upper) ? Kind.Upper : (atks.ContainsKey(Kind.Front) && Random.value < 0.6f ? Kind.Front : Kind.Jab);
            if (atks.ContainsKey(k)) { plan = new List<Step> { new Step(k) }; planPos = 0; comboRunning = false; fastStep = k != Kind.Upper; StartStep(hero); return; }
        }

        bool moving = false, running = false;
        if (adx > rangeMax + 0.9f)
        {
            running = adx > 6f;
            MoveToward(dt, dir, dy, adx, running);
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

        if (decisionTimer > 0f || hero.IsFloored) return;
        // pitkän matkan liike: hiekka tai hyppypotku
        if (adx > 3.4f)
        {
            if (Time.time >= nextSandTime && Has(sandThrow) && adx < 8.5f && Mathf.Abs(dy) < e.depthTolerance + 0.3f && Random.value < SandChance())
            {
                nextSandTime = Time.time + sandCooldown * Mathf.Lerp(0.6f, 1f, e.HealthFraction);
                plan = new List<Step> { new Step(Kind.Sand) }; planPos = 0; comboRunning = false; fastStep = false;
                StartStep(hero); return;
            }
            if (adx > 4f && Time.time >= nextJumpTime && atks.ContainsKey(Kind.JumpKick) && Random.value < 0.75f)
            {
                nextJumpTime = Time.time + 3.5f;
                plan = new List<Step> { new Step(Kind.JumpKick) };
                if (atks.ContainsKey(Kind.Front)) plan.Add(new Step(Kind.Front, true));
                planPos = 0; comboRunning = true; fastStep = false;
                StartStep(hero); return;
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

    float SandChance() => sandChance * Mathf.Lerp(1.5f, 1f, e.HealthFraction);

    void MoveToward(float dt, float dir, float dy, float adx, bool fast)
    {
        float spd = fast ? runSpeed : walkSpeed;
        float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
        e.BrainMove(new Vector2(dir * spd * dt, dyMove));
    }

    void ChoosePlan(PlayerController hero)
    {
        plan = new List<Step>(); planPos = 0; fastStep = false;
        bool guarding = hero.BlocksFrom(transform.position.x);
        if (guarding && atks.ContainsKey(Kind.Axe) && Random.value < 0.6f)
        {
            plan = new List<Step> { new Step(Kind.Axe) };
            if (Random.value < 0.5f && atks.ContainsKey(Kind.Upper)) plan.Add(new Step(Kind.Upper, true));
            comboRunning = plan.Count > 1; decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
            return;
        }
        if (Random.value < comboChance)
        {
            switch (Random.Range(0, 7))
            {
                case 0: case 1:   // kombo 1: jab, takasuora, etupotku, takajalan sivupotku (= selkä kameraan -kuvasarja, Kind.Spin)
                    plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Cross), new Step(Kind.Front), new Step(Kind.Spin, true) }; break;
                case 2: case 3:   // kombo 2: etupotku, kiertopotku, kirvespotku, kierrepotku
                    plan = new List<Step> { new Step(Kind.Front), new Step(Kind.Spin), new Step(Kind.Axe), new Step(Kind.Round, true) }; break;
                case 4:           // kombo 3: jab, kirvespotku, takasuora, kiertopotku, etupotku, kierrepotku
                    plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Axe), new Step(Kind.Cross), new Step(Kind.Spin), new Step(Kind.Front), new Step(Kind.Round, true) }; break;
                case 5:           // uppercut yksin (hidas, väistettävä)
                    plan = new List<Step> { new Step(Kind.Upper) }; break;
                default:          // pikku sarja
                    plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Jab), new Step(Kind.Cross, true) }; break;
            }
            plan.RemoveAll(s => !atks.ContainsKey(s.kind));
            comboRunning = plan.Count > 1;
            decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
            return;
        }
        float r = Random.value;
        Kind k = r < 0.3f ? Kind.Jab : r < 0.5f ? Kind.Front : r < 0.7f ? Kind.Cross : r < 0.85f ? Kind.Round : Kind.Spin;
        if (!atks.ContainsKey(k)) k = Kind.Jab;
        if (atks.ContainsKey(k)) plan.Add(new Step(k));
        comboRunning = false;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
    }

    // ---------------- toiminnan suoritus ----------------

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        mode = Mode.Act; t = 0f; hitDone = false; sandReleased = false; grabPhase = 0; grabT = 0f; holding = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        float face = e.FacingRight ? 1f : -1f;
        if (cur.kind == Kind.Sand || cur.kind == Kind.Grab) return;
        var a = atks[cur.kind];
        float scale = fastStep ? 0.5f : (planPos > 0 ? comboWindupScale : 1f);
        if (cur.kind == Kind.Upper) scale = 1f;   // uppercutin valmistelu on aina hidas (ehtii väistää)
        tWind = a.impact * a.windFt * scale;
        tHold = impactHold;
        tRec = a.recFt;
        stepLunge = a.lunge;
        stepTravelTime = tWind;
        if (cur.kind == Kind.JumpKick)
        {
            float adx = Mathf.Abs(hero.transform.position.x - transform.position.x);
            stepLunge = Mathf.Clamp(adx - 1.7f, 0.5f, 6.5f);
            stepTravelTime = tWind + tHold * 3f + 0.12f;
        }
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        if (cur.kind == Kind.Sand) { SandUpdate(dt, hero, face); return; }
        if (cur.kind == Kind.Grab) { GrabUpdate(dt, hero, face); return; }

        var a = atks[cur.kind];
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < stepTravelTime + 0.02f && stepLunge > 0f)
        {
            float step = stepTravelTime > 0.01f ? stepLunge / stepTravelTime * dt : 0f;
            if (Mathf.Abs(hp.x - me.x) > 1.1f || cur.kind == Kind.JumpKick) e.BrainMove(new Vector2(face * step, 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * 0.8f * dt)));
        }
        int pos;
        if (t < tWind) pos = a.impact > 0 ? Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * a.impact), a.impact - 1) : 0;
        else if (t < tWind + tHold) pos = a.impact;
        else pos = Mathf.Min(a.impact + 1 + (int)((t - tWind - tHold) / Mathf.Max(0.001f, tRec)), a.order.Length - 1);
        CurrentSprite = a.frames[a.order[pos]];

        if (!hitDone && t >= tWind) { hitDone = true; TryHit(hero, a); }
        float total = tWind + tHold + Mathf.Max(0, a.order.Length - 1 - a.impact) * tRec;
        if (t >= total)
        {
            planPos++;
            if (planPos < plan.Count && ComboTargetOk(hero)) { fastStep = false; StartStep(hero); }
            else EndPlan();
        }
    }

    // --- hiekka: kaivaa taskusta, heittää kuvassa 6 ---
    void SandUpdate(float dt, PlayerController hero, float face)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        const float ft = 0.06f;
        int idx = Mathf.Min((int)(t / ft), sandThrow.Length - 1);
        CurrentSprite = sandThrow[idx];
        if (!sandReleased && idx >= 6 && sandThrow.Length > 6)
        {
            sandReleased = true;
            Vector3 start = transform.position + new Vector3(face * 1.3f, 0f, 0f);
            GustavSand.Spawn(start, face, hero, e, sandPuffs, sandCloud, sandSpeed, sandLife, stunSeconds, SandResult);
        }
        if (t >= sandThrow.Length * ft + 0.15f) { planPos++; if (planPos < plan.Count) StartStep(hero); else EndPlan(); }
    }

    void SandResult(bool hit)
    {
        if (hit) sandHitStamp = Time.time;
        else decisionTimer = Mathf.Min(decisionTimer, 0.2f);
    }

    // --- heitto: kurotus, tarttuminen, nosto, käännös, heitto toiselle puolelle ---
    void GrabUpdate(float dt, PlayerController hero, float face)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        grabT += dt;
        Vector3 me = transform.position;
        // vaiheet: 0 kurotus (kuvat 0-1), 1 ote (2), 2 käännös (3), 3 veto (4), 4 heitto (5), 5 toipuminen
        float[] dur = { 0.16f, 0.16f, 0.2f, 0.2f, 0.12f, 0.3f };
        if (grabPhase == 0)
        {
            CurrentSprite = grab[grabT < dur[0] * 0.5f ? 0 : 1];
            if (grabT >= dur[0])
            {
                if (!hero.BeginGrab(me.x)) { EndPlan(); return; }   // ei tartu (hero ehti pois): takaisin
                holding = true; grabPhase = 1; grabT = 0f;
            }
        }
        else if (grabPhase >= 1 && grabPhase <= 3)
        {
            if (!hero.IsGrabbed) { holding = false; EndPlan(); return; }
            float u = Mathf.Clamp01(grabT / dur[grabPhase]);
            int spriteIdx = Mathf.Min(grabPhase + 1, grab.Length - 1);
            CurrentSprite = grab[spriteIdx];
            // pelaaja kulkee rinnuksista: edessä -> ylös -> pään yli toiselle puolelle
            float lift, x;
            if (grabPhase == 1) { lift = 0.2f + 0.3f * u; x = me.x + face * 1.0f; }
            else if (grabPhase == 2) { lift = 0.5f + 0.9f * u; x = me.x + face * (1.0f - 0.3f * u); }
            else if (grabPhase == 3) { lift = 1.4f + 0.7f * u; x = me.x + face * (0.7f - 1.0f * u); }
            else { lift = 2.1f; x = me.x - face * 0.5f; }   // (vaihe 4 = heitto, hoidetaan alla)
            hero.SetHeld(new Vector3(x, me.y - 0.02f, 0f), lift, 0f, 0);
            if (grabT >= dur[grabPhase])
            {
                grabT = 0f; grabPhase++;
                if (grabPhase == 4) { hero.Throw(-face * 14f, 8f, throwDamage, true); holding = false; }
            }
        }
        else if (grabPhase == 4)
        {
            CurrentSprite = grab[grab.Length - 1];   // heitto on jo lähtenyt
            if (grabT >= dur[4]) { grabT = 0f; grabPhase = 5; }
        }
        else
        {
            CurrentSprite = grab[grab.Length - 1];
            if (grabT >= dur[5]) { planPos++; EndPlan(); }
        }
    }

    bool ComboTargetOk(PlayerController hero)
    {
        if (hero == null || hero.IsFloored) return false;
        Vector3 me = transform.position, hp = hero.transform.position;
        return Mathf.Abs(hp.x - me.x) < 3.8f && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.6f;
    }

    void EndPlan()
    {
        mode = Mode.Fight; comboRunning = false; fastStep = false; holding = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, kaatuminen...). Otteessa oleva pelaaja päästetään irti.
    void AbortAction()
    {
        if (holding && e.Hero != null) e.Hero.ReleaseGrab();
        holding = false;
        if (mode == Mode.Act) mode = Mode.Fight;
        comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Mathf.Max(decisionTimer, 0.15f);
    }

    void TryHit(PlayerController hero, Atk a)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        float reach = a.reach + (cur.kind == Kind.JumpKick ? 0.4f : 0f);
        if (!front || Mathf.Abs(dx) > reach + 0.2f) return;
        if (Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f) return;
        bool airHero = hero.AirHeight > 0.9f && a.hit != Hit.Upper;
        if (airHero && cur.kind != Kind.JumpKick) return;
        int dmg = a.damage + (cur.finisher ? finisherBonus : 0);
        bool follow = planPos > 0;
        switch (a.hit)
        {
            case Hit.Normal: hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Knock: if (!hero.TakeKnockdown(dmg, me.x, 4.8f, 5f, e)) hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Break: hero.TakeKnockdown(dmg, me.x, 4.2f, 5.5f, e, true); break;   // kirvespotku rikkoo suojan
            case Hit.Upper: hero.TakeUppercut(dmg, me.x, 7.5f, e); break;
        }
    }

    // ---------------- pako ----------------

    void StartFlee(PlayerController hero)
    {
        mode = Mode.Flee; fleeT = 0f;
        e.ClearUndyingFallen();
        plan.Clear(); comboRunning = false;
        fleeDir = hero.transform.position.x < transform.position.x ? 1f : -1f;   // pois heroista
        e.IsAwake = false;
    }

    // Katon kohtaus (ThaiFinale): Gustav juoksee kopteriin eikä vahingoitu matkalla
    bool scriptedRun; float runTargetX;

    public void RunTo(Vector3 from, float targetX)
    {
        gameObject.SetActive(true);
        transform.position = from;
        e.ClearUndyingFallen();
        plan.Clear(); comboRunning = false; holding = false;
        mode = Mode.Flee; fleeT = 0f; scriptedRun = true; runTargetX = targetX;
        e.IsAwake = false;
    }

    void FleeUpdate(float dt, PlayerController hero)
    {
        fleeT += dt;
        if (scriptedRun) fleeDir = Mathf.Sign(runTargetX - transform.position.x);
        e.FacingRight = fleeDir > 0f;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        Vector3 before = transform.position;
        e.BrainMove(new Vector2(fleeDir * fleeSpeed * dt, 0f));
        CurrentSprite = Loop(Has(run) ? run : walk, runFrameTime);
        if (scriptedRun)
        {
            if (Mathf.Abs(transform.position.x - runTargetX) < 0.5f || fleeT > 15f) { scriptedRun = false; Vanish(); ThaiFinale.OnGustavBoarded(); }
            return;
        }
        bool stuck = Mathf.Abs(transform.position.x - before.x) < 0.2f * fleeSpeed * dt && fleeT > 0.6f;
        if (fleeT >= fleeMaxTime || stuck || Mathf.Abs(transform.position.x - hero.transform.position.x) > 18f) Vanish();
    }

    /// Gustav poistuu näkyvistä (seuraava kohtaaminen laukaistaan muualla: Appear).
    void Vanish() { gameObject.SetActive(false); ThaiFinale.OnGustavVanished(this); }

    /// Uusi kohtaaminen: energia täyteen (hp) ja takaisin taistelutilaan.
    public void Appear(Vector3 pos, int hp)
    {
        gameObject.SetActive(true);
        transform.position = pos;
        e.Renew(hp);
        mode = Mode.Dormant; plan.Clear(); comboRunning = false; holding = false;
        nextSandTime = Time.time + 3f; nextJumpTime = Time.time + 2f;
    }

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
