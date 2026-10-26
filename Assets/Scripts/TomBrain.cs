using UnityEngine;

/// <summary>
/// Tom, salin puntti. Ei tee mitään, ellei hero aloita (seisoo ja ottaa välillä lihasposeerauksen). Osumasta herää: voimakas, hidas,
/// jokainen lyönti kaataa jos ei suojaa, heittää heron kauas (otteen suojaus ei auta) ja väsyy nopeasti (heikko kunto: vähän energiaa ja
/// kestävyyttä; lyöntien välissä läähättää ja on avoin). Kun energiaa on enää 25 %, hän juo proteiinishakerin ja menee BERSERK-tilaan:
/// nopeutuu, ei väsy, tulee punaiseen hehkuun ja heittelee kaiken käsiinsä saamansa (painot, pullot ym. Bottle-oliot sekä heron).
/// Oma aivo Enemyn päällä (kuten Marcus ja Gustav); Enemy hoitaa osumat ja kaatumisen. Kuvat katsovat oikealle (valikko 215).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class TomBrain : MonoBehaviour, IEnemyBrain, IBottleHolder
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] idle, pose, walk, jab, cross, power, grab;
    [Tooltip("Proteiinishakerin juonti (10 kuvaa): berserk-tilaan siirtyminen.")]
    public Sprite[] drink;

    [Header("Liike")]
    public float walkSpeed = 2.4f, depthSpeed = 2.0f, walkFrameTime = 0.0417f, idleFrameTime = 0.14f, poseFrameTime = 0.1f;
    [Tooltip("Etäisyys (x), jolta hän lyö; heitto ja potkut tästä sisään.")]
    public float rangeMin = 1.5f, rangeMax = 2.3f;
    [Tooltip("Rauhassa: lihasposeeraus näin usein (s, satunnaisesti tältä väliltä).")]
    public Vector2 poseEvery = new Vector2(6f, 14f);

    [Header("Iskut (kaatavat jos ei suojaa)")]
    public int jabDamage = 14, crossDamage = 18, powerDamage = 24, throwDamage = 26, foeThrowDamage = 30;
    public float windFrameTime = 0.05f, impactHold = 0.07f, recoverFrameTime = 0.05f;
    [Tooltip("Iso lyönti on hitaampi (valmistelu kertoimella).")]
    public float powerWindScale = 1.5f;
    [Range(0f, 1f)] public float throwChance = 0.35f;
    [Tooltip("Heitto: vaakanopeus ja ylösnopeus (hero lentää kauas).")]
    public float throwSpeed = 16f, throwUp = 9f;

    [Header("Kestävyys")]
    public float staminaMax = 100f, staminaRegen = 14f, tiredTime = 2.4f;
    public float jabCost = 22f, crossCost = 28f, powerCost = 38f, throwCost = 45f;

    [Header("Berserk (energiaa jäljellä enintään tämän verran)")]
    [Range(0f, 1f)] public float berserkAt = 0.25f;
    public float drinkFrameTime = 0.13f;
    [Tooltip("Nopeus ja iskujen valmistelun nopeus berserkinä.")]
    public float berserkSpeed = 1.9f, berserkWindScale = 0.55f;
    [Range(0f, 1f)] public float berserkThrowChance = 0.6f;
    [Tooltip("Etäisyys (x), jolta hän hakee heitettävän esineen.")]
    public float fetchRange = 12f;
    public Color glowColor = new Color(1f, 0.08f, 0.04f, 1f);

    [Header("Taistelutapa")]
    [Tooltip("Käy kaikkien kimppuun (hero ja muut viholliset), myös ilman osumaa: hotellin Tom. Muuten vain heroa, vasta osumasta (salin Tom).")]
    public bool attacksEveryone;
    [Tooltip("Etäisyys (x), jolta hän huomaa kohteen (kaikkien kimppuun käydessään).")]
    public float foeRange = 10f;
    [Tooltip("Deacon's Den: Tom pysyy rauhassa, kunnes hero on ollut huoneessa näin kauan (s) tai joku aloittaa tappelun (wakeOnFight). 0 = herää heti, kun joku on lähellä (hotelli).")]
    public float wakeAfterSeconds = 0f;
    public bool wakeOnFight;
    [Tooltip("Ei rauhoitu, vaikka kohde olisi kaukana (Salsafestin tappelu: Tom tulee ruudun ulkopuolelta heroa kohti).")]
    public bool noRest;
    [Tooltip("Rauhassa tallustelee tällä x-välillä (0 = pysyy paikallaan). Salsafest.")]
    public float wanderMinX, wanderMaxX;
    public float wanderSpeed = 1.1f;
    float strollTarget, strollPause;
    float calmTimer;
    public int foeDamageBonus = 6;   // iskut muihin vihuihin (kaataa aina)

    enum Mode { Dormant, Fight, Act, Tired, Fetch }
    enum Kind { Jab, Cross, Power, Grab, Drink, Lift }
    class Atk { public Sprite[] frames; public int impact; public float reach, lunge; public int damage; public float cost, windScale; }

    Enemy e;
    Mode mode = Mode.Dormant;
    Kind cur;
    float lastTick = -99f, decisionTimer = 0.6f, animClock, t, tWind, tHold, tRec, stamina, nextPoseTime, poseStart = -1f, tiredT, nextFetchTime;
    readonly System.Collections.Generic.Dictionary<Kind, Atk> atks = new System.Collections.Generic.Dictionary<Kind, Atk>();
    bool hitDone, holding, holdingFoe;
    int grabPhase; float grabT;
    bool berserk;
    float berserkT;
    Bottle fetchTarget, heldBottle;
    Crate fetchCrate, heldCrate;   // berserk: koju, pöytä tai laatikko nostetaan pään yli ja heitetään
    Enemy grabFoe;                 // otteessa oleva (heitettävä) vihollinen
    [Header("Äänet (editori täyttää: Assets/Audio/tom)")]
    public AudioClip[] speakLines, berserkLines, throwSounds;
    public AudioClip fightStartLine, berserkStartSound, drinkSound, berserkHitSound;
    [Tooltip("Puhe taistelun aikana näin harvoin (s); berserkinä 0,7 ×.")]
    public Vector2 speakEvery = new Vector2(7f, 14f);
    float nextSpeakAt = 1.5f, voiceBusyUntil;
    AudioClip[] normalHurtSounds;

    [Header("Heitettävät esineet (berserk)")]
    public int crateThrowDamage = 26;
    public float crateThrowSpeed = 12f, crateThrowUp = 5f;
    [Tooltip("Berserk: nostaa ja heittää myös Bottle-pallot (painot). Pullot, ruoka ym. jäävät maahan.")]
    public bool liftsWeights = true;
    GameObject glowRoot;
    SpriteRenderer auraR, tintR;

    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && cur != Kind.Grab && cur != Kind.Lift;   // iskun ja juonnin aikana ei keskeydy kevyestä osumasta
    float Speed => walkSpeed * (berserk ? berserkSpeed : 1f);
    float WindFrame => windFrameTime * (berserk ? berserkWindScale : 1f);

    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    /// Tomin ääni (puhe, kiljahdus, juonti): ei päällekkäin muiden puheäänten kanssa, ellei force.
    /// Ruudun ulkopuolella huutaessa ääni vaimenee (muuten kovaääninen huuto säröytyy muiden äänten päällä).
    float OffscreenGain()
    {
        var cam = Camera.main;
        if (cam == null) return 1f;
        float halfW = cam.orthographicSize * cam.aspect;
        float d = Mathf.Abs(transform.position.x - cam.transform.position.x) - halfW;
        return d <= 0f ? 1f : Mathf.Lerp(1f, 0.3f, Mathf.Clamp01(d / 18f));
    }

    void Voice(AudioClip c, float volume = 1f, bool force = true)
    {
        if (c == null || e == null || e.Audio == null) return;
        if (!force && Time.time < voiceBusyUntil) return;
        e.Audio.PlayOneShotLeveled(c, volume * OffscreenGain());
        voiceBusyUntil = Time.time + c.length;
        if (speakLines != null && System.Array.IndexOf(speakLines, c) >= 0) Enemy.NoteSpeech(e, c.length);   // Puliukko voi huutaa perään
    }

    void VoiceRandom(AudioClip[] cs, float volume = 1f, bool force = true)
    {
        if (cs == null || cs.Length == 0) return;
        Voice(cs[Random.Range(0, cs.Length)], volume, force);
    }

    /// Satunnainen ääni annetuista (Enemyn Kovis-äänet): hyökkäys-, veto- ja heilautusäänet.
    void Say(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || e == null || e.Audio == null) return;
        var c = clips[Random.Range(0, clips.Length)];
        if (c != null) e.Audio.PlayOneShotLeveled(c, volume * OffscreenGain());
    }

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        stamina = staminaMax;
        nextPoseTime = Time.time + Random.Range(poseEvery.x, poseEvery.y);
        if (Has(jab)) atks[Kind.Jab] = new Atk { frames = jab, impact = 4, reach = 2.3f, lunge = 0.35f, damage = jabDamage, cost = jabCost, windScale = 1f };
        if (Has(cross)) atks[Kind.Cross] = new Atk { frames = cross, impact = 3, reach = 2.5f, lunge = 0.5f, damage = crossDamage, cost = crossCost, windScale = 1.1f };
        if (Has(power)) atks[Kind.Power] = new Atk { frames = power, impact = 4, reach = 2.6f, lunge = 0.6f, damage = powerDamage, cost = powerCost, windScale = powerWindScale };
    }

    public void Tick(float dt)
    {
        e.damageTakenMul = berserk ? 0.5f : 1f;   // berserkissä kestää osumia 50 % paremmin (ei ehdi olla siinä kauaa)
        var hero = e.Hero;
        if (hero == null) return;
        if (Time.time - lastTick > 0.25f) AbortAction();
        lastTick = Time.time;
        animClock += dt;
        Vector3 me = transform.position, hp = TargetPos(hero);
        float dx = hp.x - me.x;

        if (mode == Mode.Dormant)
        {
            e.BrainPhaseSet(Enemy.BrainPhase.Idle);
            if (!(wanderMaxX > wanderMinX && !e.IsAwake && Stroll(dt))) DormantSprite();
            if (Time.time >= nextSpeakAt) { if (Mathf.Abs(dx) < 22f) VoiceRandom(speakLines, 0.9f, false); nextSpeakAt = Time.time + Random.Range(5f, 10f); }   // jorisee rauhassakin silloin tällöin
            if (Mathf.Abs(dx) < 30f) calmTimer += dt;   // aika, jonka hero on ollut huoneessa
            bool sees = attacksEveryone && Mathf.Abs(dx) <= foeRange && Mathf.Abs(hp.y - me.y) < 4f;   // hotellin Tom huomaa kenet tahansa
            if (wakeAfterSeconds > 0f)
            {
                // Deacon's Den: ei heti; vasta kun aikaa on kulunut tai joku (hero) aloittaa tappelun huoneessa
                bool fightStarted = wakeOnFight && Time.time - Enemy.LastHitTime < 0.6f && Enemy.LastHit != null && Enemy.LastHit != e && Mathf.Abs(Enemy.LastHit.transform.position.x - me.x) < 30f;
                sees = attacksEveryone && (calmTimer >= wakeAfterSeconds || fightStarted);
            }
            if (e.IsAwake || sees) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.5f; poseStart = -1f; Voice(fightStartLine); nextSpeakAt = Time.time + Random.Range(speakEvery.x, speakEvery.y); }   // "lähtee tappeluun"
            return;
        }
        // kohde lähti kauas: takaisin rauhaan, energia täyteen
        if (!berserk && !noRest && (Mathf.Abs(dx) > 20f || Mathf.Abs(hp.y - me.y) > 7f)) { Rest(); return; }   // berserkinä ei rauhoitu: jahtaa loppuun asti
        if (berserk) berserkT += dt;
        stamina = Mathf.Min(staminaMax, stamina + staminaRegen * dt * (mode == Mode.Tired ? 1.6f : 1f) * (berserk ? 3f : 1f));

        // energia 25 %: juo shakerin ja menee berserkiin (kerran, vain perustilassa)
        if (!berserk && (mode == Mode.Fight || mode == Mode.Tired || mode == Mode.Fetch) && e.HealthFraction <= berserkAt)
        {
            if (heldBottle != null) { heldBottle.Drop(); heldBottle = null; }
            if (heldCrate != null) { heldCrate.Drop(); heldCrate = null; }
            fetchTarget = null; fetchCrate = null;
            StartAct(Kind.Drink); return;
        }
        if (mode == Mode.Tired) { TiredUpdate(dt, hero); return; }
        if (mode == Mode.Fetch) { FetchUpdate(dt, hero); return; }
        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    void Rest()
    {
        // kohde karkasi: rauhoittuu, mutta energia ja berserk SÄILYVÄT (ei paranemista karkaamalla; ei uutta juomista)
        mode = Mode.Dormant; e.IsAwake = false; stamina = staminaMax; AbortAction();
        fetchTarget = null; fetchCrate = null;
        e.BrainPhaseSet(Enemy.BrainPhase.Idle);
    }

    /// Haavoittumaton shakerin juonnin ajan (isku ei osu eikä häntä voi napata); muuten iskut osuvat normaalisti.
    public bool Invulnerable => mode == Mode.Act && cur == Kind.Drink;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => Invulnerable;

    // ---------------- rauha: seisoo, välillä lihasposeeraus ----------------

    /// Rauhassa tallustelu (wanderMinX..wanderMaxX): kävelee, pysähtyy välillä. Palauttaa true, kun kävelee tässä ruudussa.
    bool Stroll(float dt)
    {
        if (strollPause > 0f) { strollPause -= dt; return false; }
        Vector3 me = transform.position;
        if (strollTarget < wanderMinX || strollTarget > wanderMaxX) strollTarget = Random.Range(wanderMinX, wanderMaxX);
        float dx = strollTarget - me.x;
        if (Mathf.Abs(dx) < 0.15f) { strollPause = Random.Range(2f, 5f); strollTarget = Random.Range(wanderMinX, wanderMaxX); return false; }
        e.FacingRight = dx > 0f;
        e.BrainMove(new Vector2(Mathf.Sign(dx) * wanderSpeed * dt, 0f));
        CurrentSprite = Has(walk) ? Loop(walk, walkFrameTime * walkSpeed / Mathf.Max(0.1f, wanderSpeed)) : Ping(idle, idleFrameTime);
        return true;
    }

    void DormantSprite()
    {
        if (poseStart >= 0f && Has(pose))
        {
            int i = (int)((Time.time - poseStart) / poseFrameTime);
            if (i < pose.Length) { CurrentSprite = pose[i]; return; }
            poseStart = -1f; nextPoseTime = Time.time + Random.Range(poseEvery.x, poseEvery.y);
        }
        else if (Has(pose) && Time.time >= nextPoseTime) { poseStart = Time.time; CurrentSprite = pose[0]; return; }
        CurrentSprite = Ping(idle, idleFrameTime);
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

        // berserk: hakee ja heittää esineitä
        // (kojun/pöydän nosto ja heitto onnistuu myös ilman berserkiä, harvemmin; painopallot vain berserkinä)
        if (Time.time >= nextFetchTime && decisionTimer <= 0.2f && !TargetFloored(hero) && adx > rangeMax + 1f && (berserk || stamina >= staminaMax * 0.5f))
        {
            nextFetchTime = Time.time + (berserk ? 0.3f : 2.5f);
            if (Random.value < (berserk ? 0.6f : 0.3f) && FindLiftable()) { mode = Mode.Fetch; return; }
        }

        // puhe silloin tällöin (berserkinä raivoisempia ja tiheämmin)
        if (Time.time >= nextSpeakAt)
        {
            VoiceRandom(berserk && berserkLines != null && berserkLines.Length > 0 ? berserkLines : speakLines, 1f, false);
            nextSpeakAt = Time.time + Random.Range(speakEvery.x, speakEvery.y) * (berserk ? 0.7f : 1f);
        }

        bool moving = false;
        float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * (berserk ? berserkSpeed : 1f) * dt) : 0f;
        if (adx > rangeMax) { e.BrainMove(new Vector2(dir * Speed * dt, dyMove)); moving = true; }
        else if (dyMove != 0f) e.BrainMove(new Vector2(0f, dyMove));
        CurrentSprite = moving && Has(walk) ? Loop(walk, walkFrameTime / (berserk ? berserkSpeed : 1f)) : Ping(idle, idleFrameTime / (berserk ? 1.6f : 1f));

        if (decisionTimer > 0f || TargetFloored(hero) || adx > rangeMax + 0.4f || Mathf.Abs(dy) > e.depthTolerance + 0.2f) return;
        // heitto: heroon vartioon (suojaan) ei auta; vihuja hän heittää usein (nostaa ja viskaa kauas), muuten lyönti
        bool guarding = foe == null && hero.BlocksFrom(me.x);
        bool canGrab = Has(grab) && (foe == null || foe.CanBeGrabbed);
        float grabChance = foe != null ? (berserk ? 0.8f : 0.6f) : (berserk ? berserkThrowChance : throwChance);
        Kind k;
        if (canGrab && (guarding || Random.value < grabChance)) k = Kind.Grab;
        else { float r = Random.value; k = r < 0.35f ? Kind.Jab : r < 0.7f ? Kind.Cross : Kind.Power; }
        float cost = k == Kind.Grab ? throwCost : (atks.ContainsKey(k) ? atks[k].cost : jabCost);
        if (k != Kind.Grab && !atks.ContainsKey(k)) { decisionTimer = 0.4f; return; }
        if (!berserk && stamina < cost) { mode = Mode.Tired; tiredT = 0f; return; }   // ei jaksa: läähättää (berserkinä jaksaa aina)
        stamina -= berserk ? 0f : cost;
        StartAct(k);
    }

    void TiredUpdate(float dt, PlayerController hero)
    {
        tiredT += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Idle);
        e.FacingRight = TargetPos(hero).x > transform.position.x;
        CurrentSprite = Ping(idle, idleFrameTime * 0.5f);   // läähättää (nopeampi hengitys)
        if (tiredT >= tiredTime && stamina >= staminaMax * 0.6f) { mode = Mode.Fight; decisionTimer = 0.3f; }
    }

    void StartAct(Kind k)
    {
        cur = k; mode = Mode.Act; t = 0f; hitDone = false; holding = false; grabPhase = 0; grabT = 0f;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (k == Kind.Grab) grabFoe = foe;   // null = hero
        if (k == Kind.Grab || k == Kind.Lift) { if (throwSounds != null && throwSounds.Length > 0) VoiceRandom(throwSounds); else Say(e.attackSounds, 0.9f); return; }   // "heitän!"
        if (k == Kind.Drink) { Voice(drinkSound); return; }
        Say(k == Kind.Power ? e.punch3WindupSounds : e.attackSounds, 0.9f);   // iso lyönti: veto (kuten Kovis), muut: hyökkäysääni
        var a = atks[k];
        tWind = a.impact * WindFrame * a.windScale; tHold = impactHold; tRec = recoverFrameTime * (berserk ? 0.8f : 1f);
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        t += dt;
        Vector3 me = transform.position, hp = TargetPos(hero);
        float face = e.FacingRight ? 1f : -1f;
        if (cur == Kind.Drink) { DrinkUpdate(); return; }
        if (cur == Kind.Grab) { GrabUpdate(dt, hero, face); return; }
        if (cur == Kind.Lift) { LiftUpdate(dt, hero, face); return; }
        var a = atks[cur];
        bool active = t >= tWind - 0.05f && t < tWind + tHold + 0.05f;
        e.BrainPhaseSet(active ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < tWind && tWind > 0.01f && Mathf.Abs(hp.x - me.x) > 1.2f) e.BrainMove(new Vector2(face * a.lunge / tWind * dt, 0f));
        int idx;
        if (t < tWind) idx = Mathf.Min((int)(t / Mathf.Max(0.001f, tWind) * a.impact), a.impact - 1);
        else if (t < tWind + tHold) idx = a.impact;
        else idx = Mathf.Min(a.impact + 1 + (int)((t - tWind - tHold) / tRec), a.frames.Length - 1);
        CurrentSprite = a.frames[idx];
        if (!hitDone && t >= tWind)
        {
            hitDone = true;
            if (cur == Kind.Power) Say(e.punch3SwingSounds, 1f);   // iso lyönnin heilautus (osumaääni tulee HitFx:stä)
            TryHit(hero, a);
        }
        float total = tWind + tHold + Mathf.Max(0, a.frames.Length - 1 - a.impact) * tRec;
        if (t >= total) EndAct();
    }

    void TryHit(PlayerController hero, Atk a)
    {
        Vector3 me = transform.position, hp = TargetPos(hero);
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > a.reach + 0.2f) return;
        if (Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.1f) return;
        if (foe != null) { if (Enemy.EnemyHits(foe, a.damage + foeDamageBonus, me.x, true)) HitFx.OnHitQuiet(); return; }   // muut viholliset kaatuvat iskuun (osumaääni ilman tärähdystä)
        if (hero.AirHeight > 0.9f) return;
        if (!hero.TakeKnockdown(a.damage, me.x, 5.5f, 5.5f, e)) hero.TakeHit(a.damage, me.x, e, false);   // jokainen lyönti kaataa (suojaus torjuu)
    }

    // --- shakerin juonti: berserk alkaa ---
    void DrinkUpdate()
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        // juomakuvat pitää olla (valikko 215/217); jos puuttuvat, berserk alkaa silti seisten hetken hehkuen
        if (!Has(drink) && t < 0.05f) Debug.LogWarning("TomBrain: juomakuvat (tom_juoma) puuttuvat. Aja valikko 215 (sali) tai 217 (hotelli).");
        int n = Has(drink) ? drink.Length : 8;
        int idx = Mathf.Min((int)(t / drinkFrameTime), n - 1);
        CurrentSprite = Has(drink) ? drink[idx] : Ping(idle, idleFrameTime * 0.5f);
        if (idx >= 5 && !berserk)
        {
            berserk = true; berserkT = 0f; ShowGlow();   // hehku syttyy kun hän juo
            Voice(berserkStartSound);
            if (berserkHitSound != null && e != null) { normalHurtSounds = e.hurtSounds; e.hurtSounds = new[] { berserkHitSound }; }   // berserkinä osuma kuulostaa raivolta
            nextSpeakAt = Time.time + 2.5f;
        }
        if (t >= n * drinkFrameTime + 0.1f)
        {
            stamina = staminaMax; mode = Mode.Fight; decisionTimer = 0.15f;
            e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        }
    }

    // --- heitto: kurotus, ote, nosto, heilautus, heitto kauas ---
    void GrabUpdate(float dt, PlayerController hero, float face)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        grabT += dt;
        Vector3 me = transform.position;
        float k = berserk ? 0.7f : 1f;
        // vaiheet: 0 kurotus (kuvat 0–1), 1 ote ja nosto (2–3), 2 heilautus (4), 3 heitto (5–6), 4 toipuminen
        float[] dur = { 0.22f * k, 0.45f * k, 0.18f * k, 0.26f * k, 0.4f * k };
        if (grabPhase == 0)
        {
            CurrentSprite = grab[grabT < dur[0] * 0.5f ? 0 : Mathf.Min(1, grab.Length - 1)];
            if (grabT >= dur[0])
            {
                if (grabFoe != null)
                {
                    if (!grabFoe.isActiveAndEnabled || !grabFoe.CanBeGrabbed) { grabFoe = null; EndAct(); return; }   // vihu ehti pois: huti
                    grabFoe.BeginHeldByPlayer(me.x, grabFoe.HasFlipArt);   // omat heittokuvat (kuperkeikka), jos on; muuten niskalenkki tai pyörähdys
                    holdingFoe = true; grabPhase = 1; grabT = 0f;
                }
                else
                {
                    if (!hero.BeginGrab(me.x)) { EndAct(); return; }   // ei tartu: huti
                    holding = true; grabPhase = 1; grabT = 0f;
                }
            }
        }
        else if (grabPhase == 1 || grabPhase == 2)
        {
            if (grabFoe == null && !hero.IsGrabbed) { holding = false; EndAct(); return; }
            float u = Mathf.Clamp01(grabT / dur[grabPhase]);
            int si = grabPhase == 1 ? (u < 0.5f ? 2 : 3) : 4;
            CurrentSprite = grab[Mathf.Min(si, grab.Length - 1)];
            float lift = grabPhase == 1 ? Mathf.Lerp(0.3f, 2.6f, u) : 2.6f;
            float x = me.x + face * (grabPhase == 1 ? Mathf.Lerp(1.0f, 0.2f, u) : Mathf.Lerp(0.2f, -0.5f, u));
            if (grabFoe != null)
            {
                // heitettävän vihun kuvat: omat otekuvat (pose 1 = napattu, 2 = kierähdys), tai jos kuvia ei ole, kierto kuten pelaajan heitossa (ei jää seisoma-asentoon)
                bool foeArt = grabFoe.HasFlipArt || grabFoe.HasHeadlockArt;
                float spin = foeArt ? 0f : face * Mathf.Lerp(grabPhase == 1 ? 0f : 40f, grabPhase == 1 ? 40f : 130f, u);
                grabFoe.SetHeldByPlayer(new Vector3(x, me.y - 0.02f, 0f), lift, spin, foeArt ? (grabPhase == 1 ? 1 : 2) : -1);
            }
            else hero.SetHeld(new Vector3(x, me.y - 0.02f, 0f), lift, 0f, 0);
            if (grabT >= dur[grabPhase]) { grabT = 0f; grabPhase++; }
        }
        else if (grabPhase == 3)
        {
            if (grabT <= dt + 0.0001f && grabFoe != null && holdingFoe)
            {
                grabFoe.thrownByEnemy = true;   // ei kameran tärähdystä (vihun heitto)
                grabFoe.thrownBy = e;           // heitetty vihu ei kaada heittäjää itseään
                grabFoe.ReleaseThrow(face * throwSpeed * (berserk ? 1.15f : 1f), throwUp, foeThrowDamage);   // lentää kauas ja kaataa muut tieltään
                holdingFoe = false; grabFoe = null;
            }
            else if (grabT <= dt + 0.0001f && grabFoe == null && hero.IsGrabbed) { hero.Throw(face * throwSpeed * (berserk ? 1.15f : 1f), throwUp, throwDamage); holding = false; }
            CurrentSprite = grab[Mathf.Min(grabT < dur[3] * 0.5f ? 5 : 6, grab.Length - 1)];
            if (grabT >= dur[3]) { grabT = 0f; grabPhase = 4; }
        }
        else
        {
            CurrentSprite = grab[grab.Length - 1];
            if (grabT >= dur[4]) EndAct();
        }
    }

    // ---------------- kohteen valinta (kaikkien kimppuun) ----------------

    Enemy foe;           // null = kohteena hero
    float nextTargetTime;

    /// Kohteen paikka: hero tai (kaikkien kimppuun käydessä) lähin muu vihollinen.
    Vector3 TargetPos(PlayerController hero)
    {
        if (!attacksEveryone && !berserk) { foe = null; return hero.transform.position; }   // berserkinä jokainen on kohde
        if (foe != null && (!foe.isActiveAndEnabled || foe.IsDownOrDead)) foe = null;
        if (mode != Mode.Act && Time.time >= nextTargetTime) { nextTargetTime = Time.time + 0.4f; foe = PickFoe(hero); }
        return foe != null ? foe.transform.position : hero.transform.position;
    }

    bool TargetFloored(PlayerController hero) => foe != null ? foe.IsDownOrDead : hero.IsFloored;

    Enemy PickFoe(PlayerController hero)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float heroD = hero.IsFloored || hero.GameOver ? 1e9f : Mathf.Abs(hp.x - me.x) + Mathf.Abs(hp.y - me.y) * 2f;
        Enemy best = null; float bd = 1e9f;
        foreach (var o in Enemy.All)
        {
            if (o == null || o == e || !o.isActiveAndEnabled || o.IsDownOrDead) continue;
            float dx = Mathf.Abs(o.transform.position.x - me.x), dy = Mathf.Abs(o.transform.position.y - me.y);
            if (dx > (berserk ? 80f : foeRange + 4f) || dy > (berserk ? 9f : 5f)) continue;   // berserkinä kohde löytyy mistä tahansa ruudulta
            float d = dx + dy * 2f;
            if (d < bd) { bd = d; best = o; }
        }
        return best != null && bd < heroD ? best : null;   // lähin kohde: hero tai vihu
    }

    // ---------------- berserk: esineiden haku ja heitto ----------------

    /// Koju (ankkuroitu Thaikioski) tai muu laatikko/pöytä/tynnyri voidaan nostaa; kioski irrotetaan nostettaessa.
    static bool LiftableCrate(Crate c) => c != null && c.isActiveAndEnabled && c.stackedOn == null
        && (c.CanPickUp || (c.anchored && c.Intact && c.gameObject.name.StartsWith("Thaikioski")));

    /// Onko piste ruudulla (esineitä haetaan vain näkyvistä: raivossa ei lähdetä ruudun ulkopuolelle).
    static bool OnScreen(Vector3 p)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        return Mathf.Abs(p.x - cam.transform.position.x) <= cam.orthographicSize * cam.aspect - 0.3f;
    }

    /// Hakee lähimmän nostettavan: koju/pöytä/laatikko tai (liftsWeights) painopallo. Pullot, ruoka ym. eivät kelpaa.
    bool FindLiftable()
    {
        fetchTarget = null; fetchCrate = null;
        float bd = 1e9f; Vector3 me = transform.position;
        foreach (var c in Crate.All)
        {
            if (!LiftableCrate(c)) continue;
            float dx = Mathf.Abs(c.transform.position.x - me.x), dy = Mathf.Abs(c.transform.position.y - me.y);
            if (dx > fetchRange || dy > 5f || !OnScreen(c.transform.position)) continue;
            float d = dx + dy * 2f;
            if (d < bd) { bd = d; fetchCrate = c; fetchTarget = null; }
        }
        if (liftsWeights && berserk)
            foreach (var b in Bottle.All)
            {
                if (b == null || !b.isActiveAndEnabled || !b.CanPickUp || !b.ball) continue;
                float dx = Mathf.Abs(b.transform.position.x - me.x), dy = Mathf.Abs(b.transform.position.y - me.y);
                if (dx > fetchRange || dy > 5f || !OnScreen(b.transform.position)) continue;
                float d = dx + dy * 2f;
                if (d < bd) { bd = d; fetchTarget = b; fetchCrate = null; }
            }
        return fetchTarget != null || fetchCrate != null;
    }

    void FetchUpdate(float dt, PlayerController hero)
    {
        bool gone = fetchCrate != null ? !LiftableCrate(fetchCrate) : (fetchTarget == null || !fetchTarget.CanPickUp);
        if (gone) { mode = Mode.Fight; nextFetchTime = Time.time + 0.5f; return; }
        Vector3 me = transform.position, tp = fetchCrate != null ? fetchCrate.transform.position : fetchTarget.transform.position;
        if (!OnScreen(tp) || !OnScreen(me)) { fetchTarget = null; fetchCrate = null; mode = Mode.Fight; nextFetchTime = Time.time + 1f; return; }   // esine tai Tom ruudun ulkopuolella: käy vihujen kimppuun
        float dx = tp.x - me.x, dy = tp.y - me.y, adx = Mathf.Abs(dx);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        e.FacingRight = dx > 0f;
        if (adx > 0.9f || Mathf.Abs(dy) > 0.45f)
        {
            float dyMove = Mathf.Abs(dy) > 0.2f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * berserkSpeed * 1.5f * dt) : 0f;
            e.BrainMove(new Vector2(Mathf.Sign(dx) * Mathf.Min(adx, Speed * 1.3f * dt), dyMove));
            CurrentSprite = Loop(walk, walkFrameTime / berserkSpeed);
            return;
        }
        // perillä: nostaa esineen (kuvat 0–3) ja heittää heroa kohti
        e.FacingRight = TargetPos(hero).x > me.x;
        StartAct(Kind.Lift);
    }

    void LiftUpdate(float dt, PlayerController hero, float face)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        grabT += dt;
        // vaiheet: 0 kurotus (kuvat 0–1), 1 nosto yläpuolelle (2–3), 2 heilautus (4), 3 heitto (5–6), 4 toipuminen
        float[] dur = { 0.16f, 0.28f, 0.12f, 0.2f, 0.25f };
        if (grabPhase == 0)
        {
            CurrentSprite = grab[grabT < dur[0] * 0.5f ? 0 : Mathf.Min(1, grab.Length - 1)];
            if (grabT >= dur[0])
            {
                if (fetchCrate != null)
                {
                    if (!LiftableCrate(fetchCrate)) { EndAct(); nextFetchTime = Time.time + 0.4f; return; }
                    fetchCrate.anchored = false;   // koju irtoaa paikaltaan
                    fetchCrate.PickUp();
                    heldCrate = fetchCrate; fetchCrate = null; grabPhase = 1; grabT = 0f;
                }
                else
                {
                    if (fetchTarget == null || !fetchTarget.TakeBy(this)) { EndAct(); nextFetchTime = Time.time + 0.4f; return; }
                    heldBottle = fetchTarget; fetchTarget = null; grabPhase = 1; grabT = 0f;
                }
            }
        }
        else if (grabPhase == 1 || grabPhase == 2)
        {
            float u = Mathf.Clamp01(grabT / dur[grabPhase]);
            CurrentSprite = grab[Mathf.Min(grabPhase == 1 ? (u < 0.5f ? 2 : 3) : 4, grab.Length - 1)];
            if (heldCrate != null)
            {
                // laatikko/koju kannetaan pään yllä (kuten pöytä): paikka edessä -> pään päälle -> heilautus taakse
                float cx = grabPhase == 1 ? Mathf.Lerp(0.9f, 0.1f, u) : Mathf.Lerp(0.1f, -0.5f, u);
                float cl = grabPhase == 1 ? Mathf.Lerp(1.0f, 3.4f, u) : 3.4f;
                heldCrate.SetCarried(new Vector3(transform.position.x + face * cx, transform.position.y - 0.02f, 0f), cl, (e.body != null ? e.body.sortingOrder : 0) + 2);
            }
            if (grabT >= dur[grabPhase])
            {
                grabT = 0f; grabPhase++;
                if (grabPhase == 3)
                {
                    float gy = TargetPos(hero).y;
                    if (heldBottle != null) { heldBottle.Throw(gy, face, foe == null); heldBottle = null; }   // lentää kohteen suuntaan ja syvyydelle (true = osuu heroon, false = muihin vihuihin)
                    if (heldCrate != null) { heldCrate.Throw(face * crateThrowSpeed, crateThrowUp, e, crateThrowDamage); heldCrate = null; }
                }
            }
        }
        else if (grabPhase == 3)
        {
            CurrentSprite = grab[Mathf.Min(grabT < dur[3] * 0.5f ? 5 : 6, grab.Length - 1)];
            if (grabT >= dur[3]) { grabT = 0f; grabPhase = 4; }
        }
        else
        {
            CurrentSprite = grab[grab.Length - 1];
            if (grabT >= dur[4]) { EndAct(); nextFetchTime = Time.time + 0.35f; }
        }
    }

    /// Esineen pitelijä (Bottle): käsi nostossa, päälaella ja heilautuksessa. Kurotusvaiheessa esine on vielä lattialla.
    public bool BottleGrip(out Vector3 hand, out float rot, out int order)
    {
        rot = 0f; hand = Vector3.zero;
        order = e != null && e.body != null ? e.body.sortingOrder + 1 : 0;
        if (heldBottle == null || mode != Mode.Act || cur != Kind.Lift || grabPhase < 1) return false;
        float dir = e.FacingRight ? 1f : -1f;
        float u = Mathf.Clamp01(grabT / 0.28f);
        float x, y;
        if (grabPhase == 1) { x = Mathf.Lerp(0.8f, 0.25f, u); y = Mathf.Lerp(1.4f, 4.0f, u); }
        else if (grabPhase == 2) { x = Mathf.Lerp(0.25f, -0.4f, Mathf.Clamp01(grabT / 0.12f)); y = 4.1f; }
        else { x = 0.9f; y = 3.2f; }
        hand = transform.position + new Vector3(dir * x, y, 0f);
        return true;
    }

    // ---------------- punainen hehku ----------------

    void ShowGlow()
    {
        if (glowRoot != null || e.body == null) { if (glowRoot != null) glowRoot.SetActive(true); return; }
        glowRoot = new GameObject("Tomin hehku");
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
        float rise = Mathf.Clamp01(berserkT / 0.8f);   // hehku nousee pehmeästi
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

    void OnDisable() { HideGlow(); }

    // ---------------- yleiset ----------------

    void EndAct()
    {
        mode = Mode.Fight; holding = false;
        decisionTimer = berserk ? Random.Range(0.1f, 0.4f) : Random.Range(0.35f, 0.9f);   // hidas: ei ketjuta iskuja; berserkinä tiheämmin
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Enemy kutsuu, kun Tom joutuu osuman, kaatumisen tai kuoleman takia pois toiminnasta: ote vapautetaan heti (ei odoteta aivojen seuraavaa tikkiä, jolloin heitettävä jäisi ilmaan).
    public void Interrupt() { AbortAction(); }

    void AbortAction()
    {
        if (holding && e.Hero != null) e.Hero.ReleaseGrab();
        holding = false;
        if (heldBottle != null) { heldBottle.Drop(); heldBottle = null; }   // osuma: esine putoaa käsistä
        if (heldCrate != null) { heldCrate.Drop(); heldCrate = null; }
        if (holdingFoe && grabFoe != null) { grabFoe.ReleaseKnockdown(0, transform.position.x); }   // vihu pudotetaan otteesta kaatumaan
        holdingFoe = false; grabFoe = null;
        if (mode == Mode.Act || mode == Mode.Fetch) mode = Mode.Fight;
        fetchTarget = null; fetchCrate = null;
        decisionTimer = Mathf.Max(decisionTimer, 0.3f);
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
