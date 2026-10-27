using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Yakuza-pomo (punainen puku, Thaitownin klubin yläkerta). Oma liikesarja-aivo Enemyn päällä (kuten Gustav): Enemy hoitaa osumat, kaatumisen,
/// ylösnousun, suojauksen ja energiapalkin; tämä ohjaa liikkeen ja hyökkäykset. Kuvat tulevat videoista (yakuzapomo_*.png, ppu 200).
/// Liikkeet: kolme lyöntiä (Jab1-3), kaksi potkua (Kick1 korkea etupotku, Kick2 sivupotku), kierrepotku (Spin) ja tönäisy (Push, lennättää heron pitkin ruutua). Kaukana juoksee heron luo ja tönäisee perään. Potkut ovat nopeita (valmistelu ja palautus lyhyitä).
/// Pääkombo: kolme lyöntiä ja perään kaikki potkut kierrepotkuun asti. Lyhyempiä sarjoja vaihtelun vuoksi. Kuvat katsovat oikealle.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class YakuzaBrain : MonoBehaviour, IEnemyBrain, ICornerBurstBrain, ISelfTimedBurst
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] idle, walk;
    public Sprite[] jab1, jab2, jab3, kick1, kick2, spin;
    [Tooltip("Purkaus (hermostuminen): 98 kuvaa videosta (24 fps), ääni samasta videosta.")]
    public Sprite[] burst;
    public AudioClip burstSound;
    public float burstVolume = 1f;
    [Tooltip("1 = videon nopeus.")]
    public float burstSpeed = 1f;
    [Tooltip("Tönäisy: 13 kuvaa videosta (kädet eteen), ääni samasta videosta. Lennättää heron pitkin ruutua; torjunta ei auta.")]
    public Sprite[] push;
    public AudioClip pushSound;
    public float pushVolume = 1f;
    [Tooltip("Juoksu: alku (25 kuvaa vartiosta vauhtiin) ja silmukka (19 kuvaa). Juoksun päätteeksi tönäisy.")]
    public Sprite[] runStart, run;
    [Tooltip("Väistö: 5 kuvaa (vartio, nojaus taakse, syvä taivutus, nousu, vartio). Perään heti lyönti ja kierrepotku.")]
    public Sprite[] dodge;
    [Range(0f, 1f)] public float dodgeProbability = 0.55f;
    public float dodgeDelay = 3f;

    [Header("Liike")]
    public float walkSpeed = 2.6f, shuffleSpeed = 1.6f, depthSpeed = 3.0f;
    public float walkFrameTime = 0.028f, idleFrameTime = 0.267f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet (lisäksi herää osumasta).")]
    public float wakeDistance = 9f;
    [Tooltip("Mieluisa iskuetäisyys (x).")]
    public float rangeMin = 1.7f, rangeMax = 2.7f;

    [Header("Iskut")]
    public float impactHold = 0.05f;
    [Tooltip("Kombon seuraavan iskun valmistelu on näin paljon nopeampi (1 = ei).")]
    public float comboWindupScale = 0.6f;
    public int jab1Damage = 7, jab2Damage = 8, jab3Damage = 10, kick1Damage = 13, kick2Damage = 13, spinDamage = 17;
    [Tooltip("Kombon viimeisen iskun lisävahinko.")]
    public int finisherBonus = 3;
    [Tooltip("Pääkombon todennäköisyys (3 lyöntiä + kaikki potkut + kierrepotku).")]
    [Range(0f, 1f)] public float fullComboChance = 0.55f;
    public Vector2 decisionPause = new Vector2(0.15f, 0.55f);

    [Header("Torjunnan jälkeen")]
    public float counterWindow = 1.0f;
    public float breakerCooldown = 2.6f;
    [Tooltip("Satunnainen purkaus (ei tarvitse nurkkaa): todennäköisyys päätöksellä, kun hero on lähellä; kasvaa kun energia vähenee.")]
    [Range(0f, 1f)] public float randomBurstChance = 0.12f;
    public float burstCooldown = 9f;
    [Header("Juoksu")]
    public float runSpeed = 8.5f, runStartFt = 0.026f, runFt = 0.03f;
    [Tooltip("Juoksu alkaa vasta tämän etäisyyden (x) päästä ja se on kerran tässä ajassa mahdollinen.")]
    public float runMinDistance = 5.5f, runMaxDistance = 18f, runCooldown = 4f;
    [Range(0f, 1f)] public float runChance = 0.6f;
    [Header("Tönäisy")]
    public int pushDamage = 6;
    [Tooltip("Heron lentonopeus (vaaka) ja ponnistus (pysty); suuri vaaka = lentää pitkin ruutua.")]
    public float pushSpeed = 26f, pushUp = 10f;
    [Range(0f, 1f)] public float pushChance = 0.12f;
    [Tooltip("Todennäköisyys, kun hero torjuu (tönäisy murtaa suojan).")]
    [Range(0f, 1f)] public float pushChanceVsBlock = 0.5f;

    enum Mode { Dormant, Fight, Act }
    enum Kind { Jab1, Jab2, Jab3, Kick1, Kick2, Spin, Push }
    enum Hit { Normal, Knock, Shove }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk
    {
        public Sprite[] frames; public int impact;   // osumakuva = frames[impact]
        public float windFt, recFt, reach, lunge, hold = -1f; public int damage; public Hit hit;
        public float[] windDur, recDur;   // valinnaiset kuvakohtaiset kestot (valmistelu ja palautus): kiihtyvä liike
    }

    Enemy e;
    Mode mode = Mode.Dormant;
    float lastTick = -99f; int lastTickFrame = -99;
    float decisionTimer = 0.6f, animClock, t, tWind, tHold, tRec;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    Step cur;
    bool hitDone, comboRunning, fastStep, charging, dodging;
    float chargeT, nextRunTime, dodgeT, nextDodgeTime;
    static readonly float[] DodgeDur = { 0.05f, 0.07f, 0.24f, 0.08f, 0.03f };
    float lastCounterBlockStamp = -99f, nextBreakerTime, nextBurstTime;
    bool counterUsed;
    // purkaus: videon ruutu 12 kestää kaksi ruutua (24 fps), muut yhden. Osumat: kuvaindeksi, vahinko, kantama, laji (0 lyönti, 1 potku, 2 loppupotku = heron lennätys)
    bool burstRunning; float burstT, burstTotal; float[] burstDur; bool[] burstDone;
    static readonly int[] BurstHitIdx = { 17, 20, 23, 26, 29, 32, 39, 52, 76 };
    static readonly int[] BurstHitDmg = { 2, 2, 2, 2, 2, 3, 5, 5, 9 };
    static readonly float[] BurstHitReach = { 2.7f, 2.7f, 2.7f, 2.7f, 2.7f, 2.8f, 3.2f, 3.2f, 3.4f };
    bool BurstReady => Has(burst) && burst.Length == 98;
    float stepLunge, stepTravelTime, windScale = 1f, recTotal;
    readonly List<float> hitTimes = new List<float>();

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && (comboRunning || burstRunning);

    /// Raivo 0-1: kasvaa kun energia vähenee (nopeampi päätöstahti, enemmän väistöjä).
    float Rage => Mathf.Clamp01(1f - e.HealthFraction);
    float DepthFollow => depthSpeed * 0.5f;
    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        // vaikeus (ohittaa kentän tallennetut arvot): torjuu usein ja kestää pitkän ketjun, päätökset nopeammin
        e.blockChance = Mathf.Max(e.blockChance, 0.65f); e.maxBlocksInRow = Mathf.Max(e.maxBlocksInRow, 5); e.guardWhenHurt = true;
        decisionPause = new Vector2(Mathf.Min(decisionPause.x, 0.08f), Mathf.Min(decisionPause.y, 0.3f));
        // (tyyppi, kuvat, osumakuva, valmistelun kuva-aika, palautuksen kuva-aika, kantama, ponnahdus, vahinko, osuman laji)
        // Potkut nopeiksi: lyhyet kuva-ajat (videon potkut olivat hitaita).
        Add(Kind.Jab1, jab1, 4, 0.03f, 0.03f, 2.7f, 0.2f, jab1Damage, Hit.Normal);
        Add(Kind.Jab2, jab2, 3, 0.03f, 0.03f, 2.8f, 0.2f, jab2Damage, Hit.Normal);
        Add(Kind.Jab3, jab3, 4, 0.03f, 0.03f, 2.9f, 0.2f, jab3Damage, Hit.Normal);
        Add(Kind.Kick1, kick1, 7, 0.022f, 0.022f, 3.2f, 0.3f, kick1Damage, Hit.Knock);
        Add(Kind.Kick2, kick2, 9, 0.02f, 0.022f, 3.2f, 0.3f, kick2Damage, Hit.Knock);
        Add(Kind.Spin, spin, 7, 0.024f, 0.022f, 3.2f, 0.3f, spinDamage, Hit.Knock);
        // tönäisy: 13 kuvaa (kädet eteen ojennukseen, 0-12), perään palautus soittamalla aiempia kuvia takaperin; osuma kun kädet ojentuneet (9)
        if (Has(push) && push.Length == 13)
        {
            var pf = new List<Sprite>(push) { push[8], push[6], push[4], push[2] };
            Add(Kind.Push, pf.ToArray(), 9, 0.034f, 0.04f, 2.4f, 0.25f, pushDamage, Hit.Shove);
            if (atks.TryGetValue(Kind.Push, out var pa))
            {   // voima: hidas painonsiirto taakse (0-4), kiihtyvä isku (5-8: kädet ojentuvat) ja pitkä ojennus; palautus rauhallisesti
                pa.hold = 0.22f; pa.lunge = 0.7f;
                pa.windDur = new[] { 0.09f, 0.08f, 0.07f, 0.06f, 0.05f, 0.04f, 0.028f, 0.018f, 0.01f };
                pa.recDur = new[] { 0.05f, 0.05f, 0.05f, 0.06f, 0.07f, 0.08f, 0.09f };
            }
        }
        // kierrepotku: hidas alku (painonsiirto ja käännös) ja kiihtyvä loppu
        if (atks.TryGetValue(Kind.Spin, out var sp))
        {
            sp.windDur = new[] { 0.09f, 0.075f, 0.06f, 0.048f, 0.038f, 0.03f, 0.024f };
            sp.recDur = new[] { 0.034f, 0.028f, 0.023f, 0.019f, 0.015f, 0.012f, 0.01f, 0.008f };
        }
    }

    void Add(Kind k, Sprite[] frames, int impact, float windFt, float recFt, float reach, float lunge, int damage, Hit hit)
    {
        if (!Has(frames) || frames.Length <= impact) return;
        damage = Mathf.RoundToInt(damage * 1.25f);   // iskut satuttavat enemmän
        atks[k] = new Atk { frames = frames, impact = impact, windFt = windFt, recFt = recFt, reach = reach, lunge = lunge, damage = damage, hit = hit };
    }

    // ---------------- Enemyn kutsut ----------------

    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f && Time.frameCount - lastTickFrame > 6) AbortAction();   // väliin tuli osuma, kaatuminen tms.
        lastTick = Time.time; lastTickFrame = Time.frameCount;
        animClock += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        if (mode == Mode.Dormant)
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            CurrentSprite = Loop(idle, idleFrameTime);
            bool near = Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 3.5f;
            if (e.IsAwake || near) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.5f; }
            else return;
        }
        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    /// Nurkkapurkaus (pomo ajettu nurkkaan): etupotku ja kierrepotku, jota ei voi keskeyttää.
    public void CornerBurst()
    {
        if (mode == Mode.Dormant || e.Hero == null) return;
        StartBurst(e.Hero);
    }

    /// Purkaus: oma videosarja (lataus, lyöntisarja, potkut, loppupotku lennättää heron); jos kuvia ei ole, etupotku + kierrepotku.
    void StartBurst(PlayerController hero)
    {
        lastTick = Time.time; lastTickFrame = Time.frameCount;
        nextBurstTime = Time.time + burstCooldown;
        if (BurstReady)
        {
            if (burstDur == null || burstDur.Length != burst.Length)
            {
                burstDur = new float[burst.Length]; burstTotal = 0f;
                for (int i = 0; i < burstDur.Length; i++) { burstDur[i] = (i == 11 ? 2f : 1f) / 24f; burstTotal += burstDur[i]; }
            }
            burstDone = new bool[BurstHitIdx.Length];
            plan = new List<Step>(); planPos = 0; fastStep = false;
            mode = Mode.Act; burstRunning = true; burstT = 0f; comboRunning = false;
            e.FacingRight = hero.transform.position.x > transform.position.x;
            e.BrainPhaseSet(Enemy.BrainPhase.Windup);
            CurrentSprite = burst[0];
            if (burstSound != null && e.Audio != null) e.Audio.PlayOneShotLeveled(burstSound, burstVolume);
            return;
        }
        if (!atks.ContainsKey(Kind.Kick1)) return;
        e.BeginBurstWindow(3.4f);
        plan = new List<Step> { new Step(Kind.Kick1) };
        if (atks.ContainsKey(Kind.Spin)) plan.Add(new Step(Kind.Spin, true));
        planPos = 0; comboRunning = plan.Count > 1; fastStep = false;
        StartStep(hero);
    }

    void BurstUpdate(float dt, PlayerController hero)
    {
        burstT += dt * burstSpeed;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, face = e.FacingRight ? 1f : -1f;
        int pos = IndexAt(burstDur, burstT, burst.Length - 1);
        CurrentSprite = burst[pos];
        e.BrainPhaseSet(pos < 15 ? Enemy.BrainPhase.Windup : Enemy.BrainPhase.Punch);
        // lataus: kaksi punaista välähdystä ennen kuin sarja lähtee
        e.redFlash = (burstT > 0.10f && burstT < 0.22f) || (burstT > 0.33f && burstT < 0.45f);
        if (pos < 15)
        {   // latauksen aikana lähemmäs heroa (iskuetäisyys ~1,9) ja syvyys perässä
            if (Mathf.Abs(dx) > 1.9f) e.BrainMove(new Vector2(face * Mathf.Min(Mathf.Abs(dx) - 1.9f, 5f * dt), 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt)));
        }
        for (int i = 0; i < BurstHitIdx.Length; i++)
        {
            if (burstDone[i] || pos < BurstHitIdx[i]) continue;
            burstDone[i] = true;
            BurstHit(hero, i);
        }
        if (burstT >= burstTotal) { burstRunning = false; e.redFlash = false; EndPlan(); decisionTimer = Random.Range(0.5f, 0.9f); }
    }

    void BurstHit(PlayerController hero, int i)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > BurstHitReach[i] + 0.2f || Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f || hero.AirHeight > 0.9f || hero.IsFloored) return;
        int dmg = BurstHitDmg[i];
        if (i == BurstHitIdx.Length - 1)
        {   // loppupotku: heron lennätys
            e.BeginBurstWindow(0.7f);
            if (!hero.TakeKnockdown(dmg, me.x, 9.5f, 6.5f, e)) hero.TakeHit(dmg, me.x, e, true);
        }
        else hero.TakeHit(dmg, me.x, e, true);
    }

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi.
    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (mode == Mode.Dormant) return false;
        if (dodging) return dodgeT < 0.4f;   // väistön aikana iskut menevät ohi
        if (e.Hero != null)
        {
            float now = Time.time;
            hitTimes.Add(now); hitTimes.RemoveAll(x => now - x > 1.1f);
            // väistö: yksittäinen isku ohitetaan ja perään lyönti + kierrepotku
            if (mode == Mode.Fight && hitTimes.Count < 3 && now >= nextDodgeTime && Dodgeable() && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 2.9f && Random.value < dodgeProbability + 0.25f * Rage)
            {
                hitTimes.Clear(); nextDodgeTime = now + dodgeDelay * (1f - 0.45f * Rage);
                StartDodge(e.Hero);
                return true;
            }
            // osumaputki: hero hakkaa jatkuvasti -> isku ohi ja vastasarja etupotku + kierrepotku
            if (mode == Mode.Fight && hitTimes.Count >= 3 && now >= nextBreakerTime && !knockdown && atks.ContainsKey(Kind.Kick1) && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 3.6f)
            {
                nextBreakerTime = now + breakerCooldown; hitTimes.Clear();
                StartBurst(e.Hero);
                return true;
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

        // torjunnan jälkeen vastaisku: etupotku tai ensimmäinen lyönti
        if (e.LastBlockTime > lastCounterBlockStamp) { lastCounterBlockStamp = e.LastBlockTime; counterUsed = false; }
        if (!counterUsed && Time.time - e.LastBlockTime < counterWindow && adx < 3.2f)
        {
            counterUsed = true;
            Kind k = Random.value < 0.5f && atks.ContainsKey(Kind.Kick1) ? Kind.Kick1 : Kind.Jab1;
            if (atks.ContainsKey(k)) { plan = new List<Step> { new Step(k) }; planPos = 0; comboRunning = false; fastStep = true; StartStep(hero); return; }
        }

        // juoksu: kaukaa vauhdilla heron luo, perään tönäisy
        if (Has(run) && Has(runStart) && atks.ContainsKey(Kind.Push) && adx > runMinDistance && adx < runMaxDistance && Mathf.Abs(dy) < 3f && Time.time >= nextRunTime && !hero.IsFloored)
        {
            nextRunTime = Time.time + runCooldown;
            if (Random.value < runChance) { StartCharge(); return; }
        }

        bool moving = false;
        if (adx > rangeMax + 0.9f)
        {
            MoveToward(dt, dir, dy);
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
        CurrentSprite = moving ? Loop(Has(walk) ? walk : idle, walkFrameTime) : Loop(idle, idleFrameTime);

        if (decisionTimer > 0f || hero.IsFloored) return;
        if (adx > 3.4f) { decisionTimer = 0.2f; return; }
        if (adx <= rangeMax + 0.5f && adx >= rangeMin - 0.5f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f)
        {
            float lowHp = 1f - e.HealthFraction;
            if (Time.time >= nextBurstTime && atks.ContainsKey(Kind.Kick1) && Random.value < randomBurstChance * (1f + 1.5f * lowHp))
            {   // purkaus: etupotku ja kierrepotku lennättävät heron sivuun (kuten nurkassa)
                StartBurst(hero);
                return;
            }
            ChoosePlan(hero);
            if (plan.Count > 0) StartStep(hero);
        }
    }

    bool Dodgeable() => Has(dodge) && dodge.Length == 5 && atks.ContainsKey(Kind.Jab1) && atks.ContainsKey(Kind.Spin);

    void StartDodge(PlayerController hero)
    {
        mode = Mode.Act; dodging = true; dodgeT = 0f; comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        e.FacingRight = hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        CurrentSprite = dodge[0];
    }

    /// Väistö: nojaus taakse ja syvä taivutus (iskut ohi), sitten heti lyönti ja kierrepotku.
    void DodgeUpdate(float dt, PlayerController hero)
    {
        dodgeT += dt;
        float face = e.FacingRight ? 1f : -1f, total = Sum(DodgeDur);
        CurrentSprite = dodge[IndexAt(DodgeDur, dodgeT, dodge.Length - 1)];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (dodgeT < 0.2f) e.BrainMove(new Vector2(-face * 1.6f * dt, 0f));   // hieman taaksepäin
        if (dodgeT < total) return;
        dodging = false;
        plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Spin, true) }; planPos = 0;
        comboRunning = true; fastStep = true;
        StartStep(hero);
    }

    void StartCharge()
    {
        mode = Mode.Act; charging = true; chargeT = 0f; comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        CurrentSprite = runStart[0];
    }

    /// Juoksu: alku kiihtyen, sitten silmukka; kun hero on tönäisyetäisyydellä, tönäisy (nopeutettu lataus).
    void ChargeUpdate(float dt, PlayerController hero)
    {
        chargeT += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y, dir = dx >= 0f ? 1f : -1f;
        e.FacingRight = dx > 0f;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        float startTotal = runStart.Length * runStartFt, speed = runSpeed;
        if (chargeT < startTotal)
        {
            CurrentSprite = runStart[Mathf.Min((int)(chargeT / runStartFt), runStart.Length - 1)];
            float k = chargeT / startTotal; speed = runSpeed * Mathf.Lerp(0.1f, 1f, k * k);
        }
        else CurrentSprite = run[(int)((chargeT - startTotal) / runFt) % run.Length];
        if (hero.IsFloored || chargeT > 4f) { charging = false; EndPlan(); return; }
        if (adx <= 2.4f && chargeT > startTotal * 0.6f)
        {
            charging = false;
            plan = new List<Step> { new Step(Kind.Push, true) }; planPos = 0; comboRunning = false; fastStep = true;
            StartStep(hero);
            return;
        }
        float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.6f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.8f * dt) : 0f;
        e.BrainMove(new Vector2(dir * Mathf.Min(speed * dt, Mathf.Max(0f, adx - 2.0f)), dyMove));
    }

    void MoveToward(float dt, float dir, float dy)
    {
        float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
        e.BrainMove(new Vector2(dir * walkSpeed * dt, dyMove));
    }

    void ChoosePlan(PlayerController hero)
    {
        plan = new List<Step>(); planPos = 0; fastStep = false;
        if (atks.ContainsKey(Kind.Push) && Random.value < (hero.BlocksFrom(transform.position.x) ? pushChanceVsBlock : pushChance))
        {   // tönäisy: lennättää heron pitkin ruutua (murtaa myös suojan)
            plan = new List<Step> { new Step(Kind.Push, true) };
            comboRunning = false; decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (1f - 0.5f * Rage);
            return;
        }
        float r = Random.value;
        if (r < fullComboChance)
        {   // pääkombo: kolme lyöntiä, kaikki potkut, kierrepotku
            plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Jab2), new Step(Kind.Jab3), new Step(Kind.Kick1), new Step(Kind.Kick2), new Step(Kind.Spin, true) };
        }
        else if (r < fullComboChance + 0.15f)
            plan = new List<Step> { new Step(Kind.Jab1), new Step(Kind.Jab2), new Step(Kind.Jab3, true) };
        else if (r < fullComboChance + 0.30f)
            plan = new List<Step> { new Step(Kind.Jab3), new Step(Kind.Kick1), new Step(Kind.Spin, true) };
        else
            plan = new List<Step> { new Step(Random.value < 0.5f ? Kind.Kick1 : Kind.Kick2, true) };
        plan.RemoveAll(s => !atks.ContainsKey(s.kind));
        comboRunning = plan.Count > 1;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (1f - 0.5f * Rage);
    }

    // ---------------- toiminnan suoritus ----------------

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        mode = Mode.Act; t = 0f; hitDone = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        var a = atks[cur.kind];
        float scale = fastStep ? 0.5f : (planPos > 0 ? comboWindupScale : 1f);
        tWind = (HasDur(a.windDur, a.impact) ? Sum(a.windDur) : a.impact * a.windFt) * scale;
        windScale = scale;
        tHold = a.hold >= 0f ? a.hold : impactHold;
        if (cur.kind == Kind.Push && pushSound != null && e.Audio != null) e.Audio.PlayOneShotLeveled(pushSound, pushVolume);
        tRec = a.recFt;
        recTotal = HasDur(a.recDur, a.frames.Length - 1 - a.impact) ? Sum(a.recDur) : (a.frames.Length - 1 - a.impact) * a.recFt;
        stepLunge = a.lunge;
        stepTravelTime = tWind;
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        if (dodging) { DodgeUpdate(dt, hero); return; }
        if (charging) { ChargeUpdate(dt, hero); return; }
        if (burstRunning) { BurstUpdate(dt, hero); return; }
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;

        var a = atks[cur.kind];
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < stepTravelTime + 0.02f && stepLunge > 0f)
        {
            float step = stepTravelTime > 0.01f ? stepLunge / stepTravelTime * dt : 0f;
            if (Mathf.Abs(hp.x - me.x) > 1.1f) e.BrainMove(new Vector2(face * step, 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * 0.8f * dt)));
        }
        int pos;
        if (t < tWind) pos = a.impact > 0 ? (HasDur(a.windDur, a.impact) ? IndexAt(a.windDur, t / windScale, a.impact - 1) : Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * a.impact), a.impact - 1)) : 0;
        else if (t < tWind + tHold) pos = a.impact;
        else if (HasDur(a.recDur, a.frames.Length - 1 - a.impact)) pos = a.impact + 1 + IndexAt(a.recDur, t - tWind - tHold, a.frames.Length - 2 - a.impact);
        else pos = Mathf.Min(a.impact + 1 + (int)((t - tWind - tHold) / Mathf.Max(0.001f, tRec)), a.frames.Length - 1);
        CurrentSprite = a.frames[pos];

        if (!hitDone && t >= tWind) { hitDone = true; TryHit(hero, a); }
        float total = tWind + tHold + recTotal;
        if (t >= total)
        {
            planPos++;
            if (planPos < plan.Count && ComboTargetOk(hero)) { fastStep = false; StartStep(hero); }
            else EndPlan();
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
        mode = Mode.Fight; comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (1f - 0.5f * Rage);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, kaatuminen...).
    void AbortAction()
    {
        if (mode == Mode.Act) mode = Mode.Fight;
        burstRunning = false; charging = false; dodging = false; e.redFlash = false; comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Mathf.Max(decisionTimer, 0.15f);
    }

    void TryHit(PlayerController hero, Atk a)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > a.reach + 0.2f) return;
        if (Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f) return;
        if (hero.AirHeight > 0.9f) return;
        int dmg = a.damage + (cur.finisher ? finisherBonus : 0);
        bool follow = planPos > 0;
        switch (a.hit)
        {
            case Hit.Normal: hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Knock: if (!hero.TakeKnockdown(dmg, me.x, 4.8f, 5f, e)) hero.TakeHit(dmg, me.x, e, follow); break;
            case Hit.Shove: if (!hero.TakeKnockdown(dmg, me.x, pushSpeed, pushUp, e, true)) hero.TakeHit(dmg, me.x, e, follow); break;
        }
    }

    // ---------------- apurit ----------------

    static bool HasDur(float[] d, int n) => d != null && n > 0 && d.Length == n;
    static float Sum(float[] d) { float s = 0f; foreach (var x in d) s += x; return s; }
    /// Kuvan indeksi ajanhetkellä t kuvakohtaisista kestoista (viimeinen kuva pitää, jos aika ylittyy).
    static int IndexAt(float[] d, float t, int max)
    {
        float acc = 0f;
        for (int i = 0; i < d.Length; i++) { acc += d[i]; if (t < acc) return Mathf.Min(i, max); }
        return Mathf.Min(d.Length - 1, max);
    }

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
