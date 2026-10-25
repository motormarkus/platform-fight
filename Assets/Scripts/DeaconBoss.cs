using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deacon, entinen nyrkkeilijä (Aavikkobaarin yläkerran pomo). Oma liikesarja-aivo Enemy-komponentin päällä:
/// Enemy hoitaa osumat, torjunnan (suojaus), kaatumisen, ylösnousun, energiapalkin ja kipuäänet;
/// tämä komponentti ohjaa kaiken muun (Enemy.Update antaa vuoron, kun Enemy on perustilassa).
/// Kulku: seisoo sikari suussa pöydän vieressä -> hero tulee: tervetulopuhe -> sikarin heitto -> käy päälle.
/// Taistelu: pitää nyrkkeilyetäisyyden, lyö yksittäisiä lyöntejä ja kombojaan, väistää satunnaisesti ja torjuttuaan vastaa heti jabilla.
/// Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (146).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class DeaconBoss : MonoBehaviour, IEnemyBrain, ICornerBurstBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] cigarIdle;      // sikari suussa, seisoo
    public Sprite[] intro;          // savuttelu, sikarin heitto, tappeluasento
    public Sprite[] idle;           // tappeluasento (silmukka)
    public Sprite[] walk, run;
    [Tooltip("Nyrkkeilijän jalkatyö tappeluasennossa (silmukka): käytetään liikkuessa ja odottaessa; puuttuessa kävely ja idle.")]
    public Sprite[] shuffle;
    public Sprite[] jab, cross;     // suorat: jab 0–5, takasuora jab-sarjan kuvasta 5 alkaen
    public Sprite[] hookFront, hookRear, bodyRear, upperFront, upperRear;   // 6 kuvaa kukin: lataus 0–3, isku 4, palautus 5
    public Sprite[] dodgeLight, dodgeDeep;   // väistöt (kevyt painuminen, syvä painuminen)

    [Header("Intron ajoitus")]
    [Tooltip("Intro-kuva, jossa sikari irtoaa kädestä.")]
    public int introReleaseFrame = 15;
    [Tooltip("Intro-kuva, johon asti sarja toistetaan (tappeluasento).")]
    public int introEndFrame = 38;
    public float introFrameTime = 0.067f, idleFrameTime = 0.04f, walkFrameTime = 0.067f, runFrameTime = 0.033f, cigarIdleFrameTime = 0.067f, shuffleFrameTime = 0.035f;

    [Header("Liike")]
    public float walkSpeed = 2.2f, runSpeed = 4.2f, shuffleSpeed = 1.4f, depthSpeed = 2.6f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet.")]
    public float wakeDistance = 9f;
    [Tooltip("Mieluisa lyöntietäisyys (x).")]
    public float rangeMin = 1.7f, rangeMax = 2.4f;
    [Tooltip("Tämän yli juoksee.")]
    public float runDistance = 6.5f;

    [Header("Iskut")]
    public float windupFrameTime = 0.055f, impactHold = 0.07f, recoverFrameTime = 0.06f;
    public float comboWindupScale = 0.6f, flurryWindupScale = 0.45f;
    public int jabDamage = 6, crossDamage = 9, hookDamage = 10, bodyDamage = 9, upperFrontDamage = 11, upperRearDamage = 14;
    [Tooltip("Kombon viimeisen iskun kaatavan iskun lisävahinko.")]
    public int finisherBonus = 4;
    [Range(0f, 1f)] public float comboChance = 0.7f;
    [Tooltip("Tauko toimintojen välillä (s).")]
    public Vector2 decisionPause = new Vector2(0.25f, 0.6f);

    [Header("Väistö ja torjunnan vastaisku")]
    [Range(0f, 1f)] public float reactiveDodgeChance = 0.3f;
    public float dodgeCooldown = 1.6f;
    [Tooltip("Väistöliikkeen syvyysliike (yksikköä kohti kameraa tai poispäin).")]
    public float dodgeDepth = 1.0f;
    [Tooltip("Torjunnan jälkeen vastaisku jabilla näin pienellä viiveellä (s).")]
    public float counterWindow = 0.9f;

    [Header("Sikarin heitto")]
    public int cigarDamage = 7;
    public float cigarSpeed = 9f;

    [Header("Äänet")]
    public AudioClip welcome, speak, cigarThrowSound;
    public AudioClip[] punchSounds;
    public float[] punchGains;
    public float welcomeVolume = 1.25f, speakVolume = 0.5f, cigarVolume = 0.6f;
    public Vector2 speakInterval = new Vector2(10f, 18f);

    // ---------------- sisäinen tila ----------------
    enum Mode { Dormant, Welcome, IntroThrow, Fight, Act }
    enum Kind { Jab, Cross, HookF, HookR, BodyR, UpperF, UpperR, DodgeCam, DodgeAway, Duck }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }

    class Atk
    {
        public Sprite[] frames; public int[] order; public int impact;
        public float reach, lunge; public int damage;
    }

    Enemy e;
    Mode mode = Mode.Dormant;
    float modeTime, lastTick = -99f, decisionTimer = 0.6f, nextDodgeTime, nextSpeak = -1f, lastPunchSound = -9f;
    bool released, welcomeStarted, counterUsed;
    float lastCounterBlockStamp = -99f;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos;
    bool reactivePlan;
    Step cur;
    float t, tWind, tHold, tRecFrame, dodgeLen, dodgeDirY;
    int curImpactPos, curFrames;
    bool hitDone, dodging, prevWasDodge, comboRunning;
    float animClock;
    bool fastStep;

    /// Syvyysseuranta on tarkoituksella hidas ja laiska (ei jatkuvaa pystysuuntaista värinää): osa depthSpeedistä.
    float DepthFollow => depthSpeed * 0.45f;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    /// Kombon aikana tavalliset osumat eivät katkaise iskusarjaa.
    public bool ArmorNow => mode == Mode.Act && comboRunning && !dodging;

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        BuildAttacks();
    }

    void BuildAttacks()
    {
        atks.Clear();
        Add(Kind.Jab, jab, new[] { 0, 1, 2, 3, 4, 5 }, 2, 2.5f, 0.35f, jabDamage);
        Add(Kind.Cross, jab != null && cross != null ? CrossFrames() : null, new[] { 0, 1, 2, 3, 4 }, 1, 2.6f, 0.5f, crossDamage);
        Add(Kind.HookF, hookFront, new[] { 0, 1, 2, 3, 4, 5 }, 4, 2.1f, 0.5f, hookDamage);
        Add(Kind.HookR, hookRear, new[] { 0, 1, 2, 3, 4, 5 }, 4, 2.1f, 0.5f, hookDamage);
        Add(Kind.BodyR, bodyRear, new[] { 0, 1, 2, 3, 4, 5 }, 4, 2.0f, 0.45f, bodyDamage);
        Add(Kind.UpperF, upperFront, new[] { 0, 1, 2, 3, 4, 5 }, 4, 1.8f, 0.4f, upperFrontDamage);
        Add(Kind.UpperR, upperRear, new[] { 0, 1, 2, 3, 4, 5 }, 4, 1.8f, 0.4f, upperRearDamage);
    }

    // takasuora: guard-kuva (jabin viimeinen) + cross-kuvat; cross-taulukko sisältää kuvat järjestyksessä 0–6 (kuvat 6–12 sheetistä)
    Sprite[] CrossFrames()
    {
        if (jab.Length < 6 || cross.Length < 4) return null;
        return new[] { jab[5], cross[0], cross[1], cross[2], cross[3] };
    }

    void Add(Kind k, Sprite[] frames, int[] order, int impact, float reach, float lunge, int damage)
    {
        if (frames == null) return;
        int max = 0; foreach (int i in order) max = Mathf.Max(max, i);
        if (frames.Length <= max) return;
        atks[k] = new Atk { frames = frames, order = order, impact = impact, reach = reach, lunge = lunge, damage = damage };
    }

    static Sprite At(Sprite[] a, int i) => a != null && a.Length > 0 ? a[Mathf.Clamp(i, 0, a.Length - 1)] : null;
    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    // ---------------- Enemyn kutsut ----------------

    /// Enemy kutsuu tätä jokaisella ruudulla, kun se on perustilassa (ei osumaa, kaatumista tms.).
    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();   // väliin tuli osuma tms.: aiempi toiminta keskeytyi
        lastTick = Time.time;
        modeTime += dt;
        animClock += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        // hero iski ennen introa: suoraan taisteluun
        if (mode < Mode.Fight && e.IsAwake) { StartFight(); }

        switch (mode)
        {
            case Mode.Dormant:
                e.BrainPhaseSet(Enemy.BrainPhase.Idle);
                CurrentSprite = Loop(Has(cigarIdle) ? cigarIdle : idle, cigarIdleFrameTime);
                if (Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 4f) { mode = Mode.Welcome; modeTime = 0f; welcomeStarted = false; }
                break;

            case Mode.Welcome:
                e.FacingRight = dx > 0f;
                CurrentSprite = Loop(Has(cigarIdle) ? cigarIdle : idle, cigarIdleFrameTime);
                if (!welcomeStarted)
                {
                    welcomeStarted = true;
                    if (welcome != null && e.Audio != null) e.Audio.PlayOneShotLeveled(welcome, welcomeVolume);
                }
                if (modeTime >= (welcome != null ? welcome.length + 0.3f : 0.8f)) { mode = Mode.IntroThrow; modeTime = 0f; released = false; }
                break;

            case Mode.IntroThrow:
                e.FacingRight = dx > 0f;
                IntroUpdate(hero);
                break;

            case Mode.Fight:
                FightUpdate(dt, hero);
                break;

            case Mode.Act:
                ActUpdate(dt, hero);
                break;
        }

        if (mode >= Mode.Fight) SpeakMaybe();
    }

    /// Nurkkapurkaus (pomo ajettu nurkkaan): vimmattu sarja, jota ei voi keskeyttää.
    public void CornerBurst()
    {
        if ((mode != Mode.Fight && mode != Mode.Act) || e.Hero == null) return;
        plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Jab), new Step(Kind.Cross), new Step(Kind.Jab), new Step(Kind.HookF), new Step(Kind.HookR), new Step(Kind.UpperR, true) };
        plan.RemoveAll(s => !CanDo(s.kind));
        if (plan.Count == 0) return;
        planPos = 0; reactivePlan = false; comboRunning = plan.Count > 1; fastStep = true; dodging = false;
        StartStep(e.Hero);
    }

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi (väistö).
    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (mode == Mode.Act && dodging && t < dodgeLen * 0.85f) return true;   // väistön aikana ei osu
        if (mode != Mode.Fight || !e.BrainNeutral) return false;
        if (Time.time < nextDodgeTime || !Has(dodgeLight)) return false;
        if (Random.value > reactiveDodgeChance) return false;
        nextDodgeTime = Time.time + dodgeCooldown;
        Kind[] ks = { Kind.DodgeCam, Kind.DodgeAway, Kind.Duck };
        plan = new List<Step> { new Step(ks[Random.Range(0, ks.Length)]) };
        planPos = 0; reactivePlan = true; comboRunning = false; fastStep = false;
        StartStep(e.Hero);
        return true;
    }

    // ---------------- intro ----------------

    void IntroUpdate(PlayerController hero)
    {
        int i = Mathf.Min((int)(modeTime / introFrameTime), Mathf.Max(0, (intro != null ? intro.Length : 1) - 1));
        if (!Has(intro)) { StartFight(); return; }
        CurrentSprite = intro[Mathf.Min(i, intro.Length - 1)];
        if (!released && i >= introReleaseFrame)
        {
            released = true;
            DeaconCigar.Spawn(transform.position + new Vector3(e.FacingRight ? 1.1f : -1.1f, 0f, 0f), 2.4f, hero, e, cigarDamage, cigarSpeed);
            if (cigarThrowSound != null && e.Audio != null) e.Audio.PlayOneShotLeveled(cigarThrowSound, cigarVolume);
        }
        if (i >= introEndFrame) StartFight();
    }

    void StartFight()
    {
        mode = Mode.Fight; modeTime = 0f;
        e.IsAwake = true;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        decisionTimer = 0.5f;
        if (nextSpeak < 0f) nextSpeak = Time.time + Random.Range(speakInterval.x, speakInterval.y);
    }

    // ---------------- taistelu ----------------

    void FightUpdate(float dt, PlayerController hero)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y;
        e.FacingRight = dx > 0f;
        float dir = dx >= 0f ? 1f : -1f;
        decisionTimer -= dt;

        // torjunnan jälkeen vastaisku jabilla heti
        if (e.LastBlockTime > lastCounterBlockStamp)
        {
            lastCounterBlockStamp = e.LastBlockTime;
            counterUsed = false;
        }
        if (!counterUsed && Time.time - e.LastBlockTime < counterWindow && adx < 3.2f && atks.ContainsKey(Kind.Jab))
        {
            counterUsed = true;
            plan = new List<Step> { new Step(Kind.Jab) };
            planPos = 0; reactivePlan = false; comboRunning = false; fastStep = true;
            StartStep(hero);
            return;
        }

        bool movingNow = false;
        bool running = false;
        if (adx > rangeMax + 0.9f)
        {
            // lähestyy: juoksee kaukaa
            running = adx > runDistance && Has(run);
            float sp = running ? runSpeed : walkSpeed;
            float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;   // lähestyessä hakee syvyyden hitaasti
            e.BrainPhaseSet(Enemy.BrainPhase.Chase);
            e.BrainMove(new Vector2(dir * sp * dt, dyMove));
            movingNow = true;
        }
        else
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Chase);
            float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;   // seuraa heron syvyyttä vain kun lyönti ei enää yltäisi
            float dxMove = 0f;
            if (adx > rangeMax) dxMove = dir * walkSpeed * 0.8f * dt;               // tarkka etäisyys: hiukan lähemmäs
            else if (adx < rangeMin) dxMove = -dir * shuffleSpeed * dt;             // liian lähellä: askel taakse
            if (dxMove != 0f || dyMove != 0f) e.BrainMove(new Vector2(dxMove, dyMove));
            movingNow = false;
        }

        bool box = Has(shuffle);
        if (movingNow) CurrentSprite = running ? Loop(run, runFrameTime) : box ? Loop(shuffle, shuffleFrameTime) : Loop(Has(walk) ? walk : idle, walkFrameTime);
        else CurrentSprite = box ? Loop(shuffle, shuffleFrameTime) : Loop(idle, idleFrameTime);

        // päätös toiminnasta, kun sopivalla etäisyydellä ja samassa syvyydessä
        if (decisionTimer <= 0f && adx <= rangeMax + 0.5f && adx >= rangeMin - 0.4f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f && !hero.IsFloored)
        {
            ChoosePlan();
            if (plan.Count > 0) StartStep(hero);
        }
    }

    void ChoosePlan()
    {
        plan = new List<Step>();
        planPos = 0; reactivePlan = false; fastStep = false;
        if (Random.value < comboChance)
        {
            switch (Random.Range(0, 5))
            {
                case 0: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Cross), new Step(Kind.HookF), new Step(Kind.UpperR, true) }; break;
                case 1: plan = new List<Step> { new Step(Kind.DodgeCam), new Step(Kind.BodyR), new Step(Kind.HookF, true) }; break;
                case 2: plan = new List<Step> { new Step(Kind.Duck), new Step(Kind.UpperF), new Step(Kind.UpperR, true) }; break;
                case 3: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Jab), new Step(Kind.Jab), new Step(Kind.Cross, true) }; break;
                default: plan = new List<Step> { new Step(Kind.DodgeAway), new Step(Kind.HookF), new Step(Kind.Cross, true) }; break;
            }
            // puuttuvat kuvasarjat: pudotetaan askeleet, joita ei voi tehdä
            plan.RemoveAll(s => !CanDo(s.kind));
            comboRunning = plan.Count > 1;
            return;
        }
        // yksittäinen lyönti etäisyyden mukaan
        float r = Random.value;
        Kind k = r < 0.4f ? Kind.Jab : r < 0.65f ? Kind.Cross : r < 0.77f ? Kind.HookF : r < 0.85f ? Kind.UpperF : r < 0.93f ? Kind.BodyR : Kind.HookR;
        if (!CanDo(k)) k = Kind.Jab;
        if (CanDo(k)) plan.Add(new Step(k));
        comboRunning = false;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
    }

    bool CanDo(Kind k)
    {
        if (k == Kind.DodgeCam || k == Kind.DodgeAway) return Has(dodgeLight);
        if (k == Kind.Duck) return Has(dodgeDeep) || Has(dodgeLight);
        return atks.ContainsKey(k);
    }

    // ---------------- toiminnan suoritus ----------------

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        mode = Mode.Act; t = 0f; hitDone = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (cur.kind == Kind.DodgeCam || cur.kind == Kind.DodgeAway || cur.kind == Kind.Duck)
        {
            dodging = true;
            var seq = cur.kind == Kind.Duck && Has(dodgeDeep) ? dodgeDeep : dodgeLight;
            float ft = cur.kind == Kind.Duck ? 0.04f : 0.045f;
            curFrames = seq.Length; dodgeLen = curFrames * ft;
            dodgeDirY = cur.kind == Kind.DodgeCam ? -1f : cur.kind == Kind.DodgeAway ? 1f : 0f;
            return;
        }
        dodging = false;
        var a = atks[cur.kind];
        float scale = fastStep ? flurryWindupScale : (planPos > 0 || comboRunning ? comboWindupScale : 1f);
        if (cur.kind == Kind.Jab && planPos > 0 && plan[planPos - 1].kind == Kind.Jab) scale = flurryWindupScale;
        tWind = a.impact * windupFrameTime * scale;
        tHold = impactHold;
        tRecFrame = recoverFrameTime * (cur.finisher ? 1.4f : 1f);
        curImpactPos = a.impact;
        curFrames = a.order.Length;
        prevLunge = a.lunge * (prevWasDodge ? 1.5f : 1f);
    }
    float prevLunge;

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        if (dodging)
        {
            var seq = cur.kind == Kind.Duck && Has(dodgeDeep) ? dodgeDeep : dodgeLight;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, dodgeLen));
            CurrentSprite = seq[Mathf.Min((int)(u * seq.Length), seq.Length - 1)];
            if (dodgeDirY != 0f)
            {
                // syvyysliike ulos ja takaisin (y = D sin(pi u)): ei jää eri syvyyteen eikä vaelleta ylös-alas
                float amp = dodgeDepth * 0.55f;
                e.BrainMove(new Vector2(0f, dodgeDirY * amp * Mathf.PI / Mathf.Max(0.1f, dodgeLen) * Mathf.Cos(Mathf.PI * u) * dt));
            }
            if (t >= dodgeLen)
            {
                prevWasDodge = true;
                planPos++;
                if (planPos < plan.Count && !reactivePlan) StartStep(hero); else EndPlan();
            }
            return;
        }

        var a = atks[cur.kind];
        // iskun aktiivinen vaihe (osuma ja pito): Punch-tila, jolloin hero voi napata lyövästä kädestä (ajoitettu heitto); muuten Windup
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        // lataus: etenee heroa kohti ja hakee syvyyden
        if (t < tWind + 0.02f)
        {
            float step = tWind > 0.01f ? prevLunge / tWind * dt : 0f;
            float dxToHero = hp.x - me.x;
            // ei kävellä heron läpi
            if (Mathf.Abs(dxToHero) > 1.1f) e.BrainMove(new Vector2(face * step, 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * (prevWasDodge ? 1.6f : 0.8f) * dt)));
        }
        int idx;
        if (t < tWind) idx = curImpactPos > 0 ? Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * curImpactPos), curImpactPos - 1) : 0;
        else if (t < tWind + tHold) idx = curImpactPos;
        else idx = Mathf.Min(curImpactPos + 1 + (int)((t - tWind - tHold) / Mathf.Max(0.001f, tRecFrame)), curFrames - 1);
        CurrentSprite = a.frames[a.order[idx]];

        if (!hitDone && t >= tWind)
        {
            hitDone = true;
            TryHit(hero, a);
            PlayPunchSound();
        }
        float total = tWind + tHold + Mathf.Max(0, curFrames - 1 - curImpactPos) * tRecFrame;
        if (t >= total)
        {
            prevWasDodge = false;
            planPos++;
            if (planPos < plan.Count && ComboTargetOk(hero)) { fastStep = false; StartStep(hero); }
            else EndPlan();
        }
    }

    bool ComboTargetOk(PlayerController hero)
    {
        if (hero == null || hero.IsFloored) return false;
        Vector3 me = transform.position, hp = hero.transform.position;
        return Mathf.Abs(hp.x - me.x) < 3.4f && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.6f;
    }

    void EndPlan()
    {
        mode = Mode.Fight; modeTime = 0f; dodging = false; comboRunning = false; reactivePlan = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, heitto, kaatuminen...).
    void AbortAction()
    {
        if (mode == Mode.Act) { mode = Mode.Fight; }
        dodging = false; comboRunning = false; reactivePlan = false; fastStep = false;
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
        bool follow = planPos > 0;
        if (cur.finisher)
        {
            bool k = hero.TakeKnockdown(a.damage + finisherBonus, me.x, 4.5f, 5f, e);
            if (k) { HitFx.Freeze(0.1f); CameraFollow.Shake(0.2f, 0.18f); }
            return;
        }
        hero.TakeHit(a.damage, me.x, e, follow);
    }

    void PlayPunchSound()
    {
        if (punchSounds == null || punchSounds.Length == 0 || e.Audio == null) return;
        if (Time.time - lastPunchSound < 0.22f) return;   // nopeassa sarjassa ei joka lyönnistä
        if (Random.value > 0.7f) return;
        lastPunchSound = Time.time;
        int i = Random.Range(0, punchSounds.Length);
        float gain = punchGains != null && i < punchGains.Length ? punchGains[i] : 1f;
        e.Audio.PlayOneShotLeveled(punchSounds[i], gain);
    }

    void SpeakMaybe()
    {
        if (speak == null || e.Audio == null || nextSpeak < 0f || Time.time < nextSpeak) return;
        if (mode == Mode.Act) return;
        e.Audio.PlayOneShotLeveled(speak, speakVolume);
        nextSpeak = Time.time + speak.length + Random.Range(speakInterval.x, speakInterval.y);
    }

    Sprite Loop(Sprite[] a, float ft)
    {
        if (!Has(a)) return CurrentSprite;
        return a[(int)(animClock / Mathf.Max(0.001f, ft)) % a.Length];
    }
}

/// Deaconin heittämä sikari: lentää heroa kohti kaarella ja pyörien, osuu heroon pieneen vahinkoon.
public class DeaconCigar : MonoBehaviour
{
    static Sprite sprite;
    Vector2 vel;
    float height, vHeight, life, spin;
    PlayerController hero;
    Enemy owner;
    int damage;
    Transform vis;

    static Sprite MakeSprite()
    {
        if (sprite != null) return sprite;
        const int w = 20, h = 4;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                Color c = new Color(0.43f, 0.27f, 0.15f, 1f);
                if (x < 3) c = new Color(1f, 0.55f, 0.15f, 1f);          // hehkuva pää
                else if (x < 5) c = new Color(0.6f, 0.6f, 0.58f, 1f);    // tuhka
                else if (x > w - 5 && x < w - 3) c = new Color(0.8f, 0.65f, 0.2f, 1f);   // rengas
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 40f);
        return sprite;
    }

    public static void Spawn(Vector3 start, float startHeight, PlayerController hero, Enemy owner, int damage, float speed)
    {
        var go = new GameObject("Deaconin sikari");
        go.transform.position = new Vector3(start.x, start.y, 0f);
        var c = go.AddComponent<DeaconCigar>();
        c.hero = hero; c.owner = owner; c.damage = damage;
        var visGo = new GameObject("Kuva");
        visGo.transform.SetParent(go.transform, false);
        var sr = visGo.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSprite();
        c.vis = visGo.transform;
        c.height = startHeight;
        Vector3 target = hero != null ? hero.transform.position : start + Vector3.right * 6f;
        Vector2 d = new Vector2(target.x - start.x, target.y - start.y);
        float dist = Mathf.Max(0.5f, d.magnitude);
        float time = dist / Mathf.Max(1f, speed);
        c.vel = d / time;
        c.vHeight = (1.3f - startHeight) / time;   // putoaa rinnan korkeudelle perille tultaessa
        c.life = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt;
        transform.position += new Vector3(vel.x, vel.y, 0f) * dt;
        height += vHeight * dt;
        spin += 900f * dt;
        if (vis != null)
        {
            vis.localPosition = new Vector3(0f, Mathf.Max(0.05f, height), 0f);
            vis.localRotation = Quaternion.Euler(0f, 0f, spin);
        }
        var sr = vis != null ? vis.GetComponent<SpriteRenderer>() : null;
        if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 8;
        if (hero != null)
        {
            Vector3 hp = hero.transform.position, p = transform.position;
            if (Mathf.Abs(hp.x - p.x) < 0.6f && Mathf.Abs(hp.y - p.y) < 0.5f && hero.AirHeight < 1.8f && height > 0.2f)
            {
                hero.TakeHit(damage, p.x - Mathf.Sign(vel.x) * 0.5f, owner, false);
                Destroy(gameObject);
                return;
            }
        }
        if (life > 3f || height < -0.5f) Destroy(gameObject);
    }
}
