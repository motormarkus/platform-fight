using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marcus, nyrkkeilykehän sparrauskumppani. Oma liikesarja-aivo Enemy-komponentin päällä (kuten DeaconBoss):
/// Enemy hoitaa osumat, kaatumisen (kaksi eri kaatumista), ylösnousun ja energiapalkin; tämä ohjaa liikkeen ja iskut.
/// Sparraus: iskut ovat kevyitä, hero ei kuole eikä Marcus kuole (Enemy.sparring), tappelu alkaa kun hero tulee lähelle tai lyö.
/// Taistelu: pitää nyrkkeilyetäisyyden, lyö jabeja, suoria ja koukkuja yksittäin ja sarjoina, väistää osumia (painuu alas)
/// ja vastaa iskuun nopeasti, kun hero on juuri lyönyt ohi. Kuvat katsovat oikealle. Kuvajärjestykset tulevat editorivalikosta (205).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class MarcusBoxer : MonoBehaviour, IEnemyBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] idle, walk;
    public Sprite[] jab, jab2, cross, hook;
    [Tooltip("Väistö: painuu alas ja nousee takaisin.")]
    public Sprite[] dodge;

    [Tooltip("Kehän alue: Marcus tappelee vain kun hero on tässä alueessa (muuten seisoo rauhassa, ei varjonyrkkeile).")]
    public Area ringArea;

    [Header("Liike")]
    public float walkSpeed = 3.2f, shuffleSpeed = 2.0f, depthSpeed = 3.4f;
    public float walkFrameTime = 0.066f, idleFrameTime = 0.12f;
    [Tooltip("Etäisyys (x), jolta hero herättää hänet (lisäksi herää osumasta).")]
    public float wakeDistance = 3.6f;
    [Tooltip("Mieluisa lyöntietäisyys (x).")]
    public float rangeMin = 1.7f, rangeMax = 2.5f;

    [Header("Iskut (sparraus: kevyet)")]
    public float windupFrameTime = 0.032f, impactHold = 0.035f, recoverFrameTime = 0.036f;
    public float comboWindupScale = 0.5f, flurryWindupScale = 0.35f;
    public int jabDamage = 7, jab2Damage = 7, crossDamage = 11, hookDamage = 14;
    [Tooltip("Kombon viimeisen iskun lisävahinko.")]
    public int finisherBonus = 4;
    [Range(0f, 1f)] public float comboChance = 0.92f;
    [Tooltip("Tauko toimintojen välillä (s).")]
    public Vector2 decisionPause = new Vector2(0.05f, 0.2f);

    [Header("Torjunnan vastaisku")]
    [Tooltip("Torjuttuaan iskun vastaa jabilla näin pienellä viiveellä (s).")]
    public float counterWindow = 1.1f;

    [Header("Väistö")]
    [Range(0f, 1f)] public float reactiveDodgeChance = 0.55f;
    public float dodgeCooldown = 0.8f;
    public float dodgeFrameTime = 0.032f;
    [Tooltip("Väistön jälkeen vastaisku heti (ehtii ennen kuin hero toipuu), todennäköisyys.")]
    [Range(0f, 1f)] public float counterAfterDodge = 0.9f;

    enum Mode { Dormant, Fight, Act }
    enum Kind { Jab, Jab2, Cross, Hook, Dodge }
    struct Step { public Kind kind; public bool finisher; public Step(Kind k, bool f = false) { kind = k; finisher = f; } }
    class Atk { public Sprite[] frames; public int impact; public float reach, lunge; public int damage; }

    Enemy e;
    Mode mode = Mode.Dormant;
    float lastTick = -99f, decisionTimer = 0.6f, nextDodgeTime, animClock, t, tWind, tHold, tRecFrame, dodgeLen;
    readonly Dictionary<Kind, Atk> atks = new Dictionary<Kind, Atk>();
    List<Step> plan = new List<Step>();
    int planPos, curImpactPos, curFrames;
    Step cur;
    bool hitDone, dodging, comboRunning, fastStep, prevWasDodge;
    float prevLunge, lastCounterBlockStamp = -99f;
    bool counterUsed;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && comboRunning && !dodging;

    float DepthFollow => depthSpeed * 0.45f;

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        e.sparring = true;
        Add(Kind.Jab, jab, 3, 2.5f, 0.3f, jabDamage);
        Add(Kind.Jab2, jab2, 3, 2.5f, 0.3f, jab2Damage);
        Add(Kind.Cross, cross, 2, 2.6f, 0.5f, crossDamage);
        Add(Kind.Hook, hook, 5, 2.2f, 0.45f, hookDamage);
    }

    void Update()
    {
        bool heroInRing = ringArea == null || Area.Current == ringArea;
        // kanveesiin lyöty Marcus nousee, kun hero lähtee kehästä (Enemy ei anna aivojen ajaa maassa ollessa, joten tämä tehdään täällä)
        if (e.SparKO && !heroInRing) e.SparReset();
        // sparraus-suoja (hero ei kuole) vain kun tappelu on käynnissä
        PlayerController.Sparring = heroInRing && mode != Mode.Dormant && !e.SparKO;
    }

    void OnEnable() { PlayerController.SparDefeated += OnHeroDefeated; }
    void OnDisable() { PlayerController.SparDefeated -= OnHeroDefeated; PlayerController.Sparring = false; }

    // ---------------- häviö: hero kaatuu, YOU LOSE, jatkuu kehän ulkopuolelta ----------------
    bool defeating;
    static float loseUntil = -1f, loseStart;

    void OnHeroDefeated(PlayerController hero)
    {
        if (defeating || mode == Mode.Dormant || hero == null || !isActiveAndEnabled) return;
        defeating = true;
        ScreenFader.Host.StartCoroutine(DefeatRoutine(hero));
    }

    System.Collections.IEnumerator DefeatRoutine(PlayerController hero)
    {
        // Marcus rauhoittuu, hero kaatuu
        mode = Mode.Dormant; plan.Clear(); dodging = false; comboRunning = false; e.IsAwake = false;
        PlayerController.Sparring = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Idle);
        hero.SparKnockOut(transform.position.x);
        loseStart = Time.unscaledTime; loseUntil = Time.unscaledTime + 4.2f;
        yield return new WaitForSecondsRealtime(1.2f);
        hero.enabled = false;   // makaa kanveesissa, ei ohjausta
        yield return new WaitForSecondsRealtime(1.6f);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = t / 0.5f; yield return null; }
        ScreenFader.Alpha = 1f;
        // kehän ulkopuolelle: kehän poistumisoven kohde (salin portaat)
        Door exit = null;
        foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None)) if (d != null && d.here == ringArea && (d.target != null || !string.IsNullOrEmpty(d.targetName))) { exit = d; break; }
        Area dest = exit != null ? (exit.target != null ? exit.target : Area.Find(exit.targetName)) : Area.Find("Nyrkkeilysali");
        Vector3 pos = exit != null ? new Vector3(exit.spawnPoint.x, exit.spawnPoint.y, 0f) : hero.transform.position;
        if (dest != null) dest.Apply(hero);
        hero.TeleportTo(pos);
        hero.enabled = true;
        hero.health = Mathf.Max(hero.health, hero.maxHealth / 2);
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);
        e.WarHeal(); e.SparReset();
        yield return new WaitForSecondsRealtime(0.15f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / 0.4f; yield return null; }
        ScreenFader.Alpha = 0f;
        loseUntil = -1f;
        defeating = false;
    }

    GUIStyle loseStyle;
    void OnGUI()
    {
        if (loseUntil < 0f || Time.unscaledTime > loseUntil) return;
        if (loseStyle == null) loseStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        loseStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.16f);
        float a = Mathf.Clamp01((Time.unscaledTime - loseStart) / 0.4f);
        GUI.depth = -300;
        var r = new Rect(0f, Screen.height * 0.28f, Screen.width, Screen.height * 0.24f);
        loseStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
        GUI.Label(new Rect(r.x + 4f, r.y + 5f, r.width, r.height), "YOU LOSE", loseStyle);
        loseStyle.normal.textColor = new Color(0.95f, 0.15f, 0.12f, a);
        GUI.Label(r, "YOU LOSE", loseStyle);
    }
    void OnDestroy() { PlayerController.Sparring = false; }

    void Add(Kind k, Sprite[] frames, int impact, float reach, float lunge, int damage)
    {
        if (frames == null || frames.Length <= impact) return;
        atks[k] = new Atk { frames = frames, impact = impact, reach = reach, lunge = lunge, damage = damage };
    }

    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    // ---------------- Enemyn kutsut ----------------

    /// Enemy kutsuu tätä jokaisella ruudulla, kun se on perustilassa.
    public void Tick(float dt)
    {
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();   // väliin tuli osuma tms.
        lastTick = Time.time;
        animClock += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;

        // hero ei ole kehässä (esim. salissa): lepo, ei toimintaa
        if (ringArea != null && Area.Current != ringArea)
        {
            if (mode != Mode.Dormant) Rest();
            e.IsAwake = false;
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            CurrentSprite = Ping(idle, idleFrameTime);
            PlayerController.Sparring = false;
            return;
        }
        if (mode == Mode.Dormant)
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            CurrentSprite = Ping(idle, idleFrameTime);
            PlayerController.Sparring = false;
            bool near = Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 3f;
            if (e.IsAwake || near) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.6f; }
            else return;
        }
        // hero lähti kehästä (toinen alue tai kaukana): takaisin lepoon ja energia täyteen
        if (Mathf.Abs(dx) > 18f || Mathf.Abs(hp.y - me.y) > 6f) { Rest(); return; }
        PlayerController.Sparring = true;

        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    void Rest()
    {
        mode = Mode.Dormant; e.IsAwake = false; e.WarHeal(); plan.Clear(); dodging = false; comboRunning = false;
        PlayerController.Sparring = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Idle);
    }

    [Header("Putken katkaisu")]
    [Tooltip("Kolmas osuma 1,1 sekunnin sisällä: Marcus väistää ja vastaa heti sarjalla (ei jää pelkäksi säkiksi). Väli (s):")]
    public float breakerCooldown = 1.6f;
    readonly List<float> hitTimes = new List<float>();
    float nextBreakerTime;

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi (väistö).
    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (mode == Mode.Act && dodging && t < dodgeLen * 0.85f) return true;   // väistön aikana ei osu
        // osumaputki: hero hakkaa jatkuvasti -> väistö ja vastasarja (toimii myös osumatilasta)
        if (mode != Mode.Dormant && e.Hero != null)
        {
            float now = Time.time;
            hitTimes.Add(now); hitTimes.RemoveAll(x => now - x > 1.1f);
            if (hitTimes.Count >= 3 && now >= nextBreakerTime && Has(dodge) && !knockdown && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 4f)
            {
                nextBreakerTime = now + breakerCooldown; hitTimes.Clear();
                plan = new List<Step> { new Step(Kind.Dodge) };
                if (atks.ContainsKey(Kind.Cross)) plan.Add(new Step(Kind.Cross));
                if (atks.ContainsKey(Kind.Hook)) plan.Add(new Step(Kind.Hook, true));
                planPos = 0; comboRunning = plan.Count > 1; fastStep = false;
                lastTick = now;   // muuten seuraava Tick luulee toiminnan keskeytyneen
                StartStep(e.Hero);
                return true;
            }
        }
        if (mode != Mode.Fight || !e.BrainNeutral) return false;
        if (Time.time < nextDodgeTime || !Has(dodge) || knockdown) return false;
        if (Random.value > reactiveDodgeChance) return false;
        nextDodgeTime = Time.time + dodgeCooldown;
        plan = new List<Step> { new Step(Kind.Dodge) };
        if (Random.value < counterAfterDodge) plan.Add(new Step(Random.value < 0.5f ? Kind.Cross : Kind.Jab));
        planPos = 0; comboRunning = false; fastStep = false;
        StartStep(e.Hero);
        return true;
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
        if (e.LastBlockTime > lastCounterBlockStamp) { lastCounterBlockStamp = e.LastBlockTime; counterUsed = false; }
        if (!counterUsed && Time.time - e.LastBlockTime < counterWindow && adx < 3.2f && atks.ContainsKey(Kind.Jab))
        {
            counterUsed = true;
            plan = new List<Step> { new Step(Kind.Jab) };
            planPos = 0; comboRunning = false; fastStep = true;
            StartStep(hero);
            return;
        }
        bool moving = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        if (adx > rangeMax + 0.9f)
        {
            float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
            e.BrainMove(new Vector2(dir * walkSpeed * dt, dyMove));
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
        CurrentSprite = moving ? Loop(Has(walk) ? walk : idle, walkFrameTime) : Ping(idle, idleFrameTime);

        if (decisionTimer <= 0f && adx <= rangeMax + 0.5f && adx >= rangeMin - 0.4f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f && !hero.IsFloored)
        {
            ChoosePlan();
            if (plan.Count > 0) StartStep(hero);
        }
    }

    void ChoosePlan()
    {
        plan = new List<Step>(); planPos = 0; fastStep = false;
        if (Random.value < comboChance)
        {
            switch (Random.Range(0, 5))
            {
                case 0: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Cross), new Step(Kind.Hook, true) }; break;
                case 1: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Jab2), new Step(Kind.Cross, true) }; break;
                case 2: plan = new List<Step> { new Step(Kind.Jab2), new Step(Kind.Jab), new Step(Kind.Cross), new Step(Kind.Hook, true) }; break;
                case 3: plan = new List<Step> { new Step(Kind.Dodge), new Step(Kind.Cross), new Step(Kind.Hook, true) }; break;
                default: plan = new List<Step> { new Step(Kind.Jab), new Step(Kind.Hook), new Step(Kind.Cross, true) }; break;
            }
            plan.RemoveAll(s => !CanDo(s.kind));
            comboRunning = plan.Count > 1;
            return;
        }
        float r = Random.value;
        Kind k = r < 0.35f ? Kind.Jab : r < 0.55f ? Kind.Jab2 : r < 0.8f ? Kind.Cross : Kind.Hook;
        if (!CanDo(k)) k = Kind.Jab;
        if (CanDo(k)) plan.Add(new Step(k));
        comboRunning = false;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
    }

    bool CanDo(Kind k) => k == Kind.Dodge ? Has(dodge) : atks.ContainsKey(k);

    // ---------------- toiminnan suoritus ----------------

    void StartStep(PlayerController hero)
    {
        if (planPos >= plan.Count) { EndPlan(); return; }
        cur = plan[planPos];
        mode = Mode.Act; t = 0f; hitDone = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (cur.kind == Kind.Dodge)
        {
            dodging = true; dodgeLen = dodge.Length * dodgeFrameTime;
            return;
        }
        dodging = false;
        var a = atks[cur.kind];
        float scale = fastStep ? flurryWindupScale : (planPos > 0 || comboRunning ? comboWindupScale : 1f);
        if ((cur.kind == Kind.Jab || cur.kind == Kind.Jab2) && planPos > 0 && (plan[planPos - 1].kind == Kind.Jab || plan[planPos - 1].kind == Kind.Jab2)) scale = flurryWindupScale;
        tWind = a.impact * windupFrameTime * scale;
        tHold = impactHold;
        tRecFrame = recoverFrameTime * (cur.finisher ? 1.4f : 1f);
        curImpactPos = a.impact;
        curFrames = a.frames.Length;
        prevLunge = a.lunge * (prevWasDodge ? 1.5f : 1f);
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        if (dodging)
        {
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, dodgeLen));
            CurrentSprite = dodge[Mathf.Min((int)(u * dodge.Length), dodge.Length - 1)];
            if (t >= dodgeLen)
            {
                prevWasDodge = true; planPos++;
                if (planPos < plan.Count) StartStep(hero); else EndPlan();
            }
            return;
        }
        var a = atks[cur.kind];
        bool activePhase = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(activePhase ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < tWind + 0.02f)
        {
            float step = tWind > 0.01f ? prevLunge / tWind * dt : 0f;
            if (Mathf.Abs(hp.x - me.x) > 1.1f) e.BrainMove(new Vector2(face * step, 0f));
            float dy = hp.y - me.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * (prevWasDodge ? 1.6f : 0.8f) * dt)));
        }
        int idx;
        if (t < tWind) idx = curImpactPos > 0 ? Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * curImpactPos), curImpactPos - 1) : 0;
        else if (t < tWind + tHold) idx = curImpactPos;
        else idx = Mathf.Min(curImpactPos + 1 + (int)((t - tWind - tHold) / Mathf.Max(0.001f, tRecFrame)), curFrames - 1);
        CurrentSprite = a.frames[idx];

        if (!hitDone && t >= tWind) { hitDone = true; TryHit(hero, a); }
        float total = tWind + tHold + Mathf.Max(0, curFrames - 1 - curImpactPos) * tRecFrame;
        if (t >= total)
        {
            prevWasDodge = false; planPos++;
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
        mode = Mode.Fight; dodging = false; comboRunning = false; fastStep = false;
        plan = new List<Step>(); planPos = 0;
        decisionTimer = Random.Range(decisionPause.x, decisionPause.y);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, kaatuminen...).
    void AbortAction()
    {
        if (mode == Mode.Act) mode = Mode.Fight;
        dodging = false; comboRunning = false; fastStep = false;
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
        hero.TakeHit(a.damage + (cur.finisher ? finisherBonus : 0), me.x, e, planPos > 0);
    }

    /// Edestakaisin (ensimmäinen ja viimeinen kuva eivät toistu): hengittävä vartioasento ilman hyppäystä silmukan saumassa.
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
