using UnityEngine;

/// <summary>
/// Kaukalopomon apuri (jääkiekkoilija). Oma aivo Enemyn päällä (kuten Yakuza-pomo): Enemy hoitaa osumat, kaatumisen, ylösnousun ja energiapalkin; tämä ohjaa liikkeen ja hyökkäykset.
/// Vaihe 1 (mailalla, energiaa yli 50 %): luistelee heron kimppuun ja taklaa mailalla täydestä vauhdista (ei jarrutusta ennen taklausta); taklauksen jälkeen liukuu ojennettuna ja palautuu.
/// Jos ryntäys ohittaa tai hero kaatuu, jarruttaa jarrutuskuvilla. Kun energia putoaa alle 50 %, hän pudottaa mailan (ei nosta sitä enää), vaihtaa reaktiokuvat ja
/// Vaihe 2 (nyrkit): liukuu luistimilla heron kimppuun ja lyö suoria (vasen ja oikea); ei suojaa. Kuvat katsovat oikealle (ppu 200, ruudun keskellä jalkojen keskikohta).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class ApuriBrain : MonoBehaviour, IEnemyBrain
{
    [Header("Kuvat (editori täyttää)")]
    public Sprite[] skate, brake, tackle, drop, fistIdle, jab1, jab2;
    [Tooltip("Reaktiot vaiheittain; aivo vaihtaa ne Enemyn kenttiin mailan pudotuksen yhteydessä.")]
    public Sprite[] stickHurt, stickKnockdown, stickLand, stickGetUp, fistHurt, fistKnockdown, fistLand, fistGetUp;
    public float stickGetUpTime = 0.6f, fistGetUpTime = 0.6f;
    [Tooltip("Ilmakuvien kuva-aika (s) vaiheittain; nyrkkivaiheessa taulukko on laajennettu kestoilla (horjunta + kaatuminen), joten kuva-aika on pieni.")]
    public float stickAirFrameTime = 0.05f, fistAirFrameTime = 0.01f;

    [Header("Äänet")]
    [Tooltip("Haymaker (Koviksen big thug) vain taklauksessa: lataus ryntäyksen alussa ja isku osuessa.")]
    public AudioClip[] windupSounds, swingSounds;
    [Tooltip("Videoista: luistelu (silmukka kertoina), jarrutus, mailan pudotus ja suorat (jab1, jab2).")]
    public AudioClip[] skateSounds, brakeSounds, dropSounds, jab1Sounds, jab2Sounds;
    public float soundVolume = 1f, skateSoundVolume = 0.7f;

    [Header("Liike")]
    public float skateSpeed = 4.2f, depthSpeed = 2.8f, glideSpeed = 3.4f;
    public float skateFrameTime = 0.07f, idleFrameTime = 0.13f;
    public float wakeDistance = 10f;

    [Header("Taklaus (vaihe 1)")]
    public float chargeSpeed = 11f, chargeMinDistance = 3.4f, chargeMaxDistance = 15f, chargeCooldown = 1.1f;
    [Tooltip("Etäisyys (x), jolla taklaus alkaa ryntäyksen aikana.")]
    public float tackleStartDistance = 3.4f;
    public float tackleReach = 4.0f, tackleFrameTime = 0.032f, tackleHoldFrameTime = 0.07f;
    public int tackleImpact = 6, tackleDamage = 15;
    public float tackleLaunchX = 11f, tackleLaunchUp = 6f;
    [Tooltip("Liuku taklauksen jälkeen: alkunopeus ja kesto.")]
    public float slideSpeed = 9f, slideTime = 0.9f;

    [Header("Nyrkit (vaihe 2)")]
    public float fistRushSpeed = 8.5f, fistRushMinDistance = 4.5f, fistRushCooldown = 2.2f;
    public int jab1Impact = 6, jab2Impact = 3, jab1Damage = 8, jab2Damage = 9, finisherBonus = 4;
    public float jabFrameTime = 1f / 30f, jabReach = 3.0f, jabLunge = 0.9f;
    public Vector2 decisionPause = new Vector2(0.15f, 0.55f);

    enum Mode { Dormant, Fight, Act }
    enum Act2 { None, Charge, Tackle, Brake, Drop, Jab }

    Enemy e;
    Mode mode = Mode.Dormant;
    Act2 act = Act2.None;
    bool fists;
    float lastTick = -99f; int lastTickFrame = -99;
    float decisionTimer = 0.5f, animClock, t, nextChargeTime, nextRushTime;
    float chargeT, brakeSpeed, slideT, glideV;
    bool hitDone, hitLanded, tackleHit;
    int jabStep, jabCount;
    float jabT; bool jabHitDone;
    bool combo;

    bool lastMoving; float nextTurnBrake;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => CurrentSprite != null;
    public bool ArmorNow => mode == Mode.Act && (act == Act2.Charge || act == Act2.Tackle || act == Act2.Drop || act == Act2.Jab);

    float Rage => Mathf.Clamp01(1f - e.HealthFraction);
    float DepthFollow => depthSpeed * 0.5f;
    static bool Has(Sprite[] a) => a != null && a.Length > 0;

    void Awake()
    {
        e = GetComponent<Enemy>();
        e.brain = this;
        e.blockChance = 0f; e.guardWhenHurt = false;   // ei suojaa
        ApplyReactions(false);
    }

    void Play(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || e.Audio == null) return;
        var c = clips[Random.Range(0, clips.Length)];
        if (c != null) e.Audio.PlayOneShotLeveled(c, soundVolume);
    }

    void ApplyReactions(bool f)
    {
        if (f ? Has(fistHurt) : Has(stickHurt)) e.hurtSprites = f ? fistHurt : stickHurt;
        if (f ? Has(fistKnockdown) : Has(stickKnockdown)) { e.knockdownSprites = f ? fistKnockdown : stickKnockdown; e.airFrameTime = f ? fistAirFrameTime : stickAirFrameTime; }
        if (f ? Has(fistLand) : Has(stickLand)) e.landSprites = f ? fistLand : stickLand;
        if (f ? Has(fistGetUp) : Has(stickGetUp)) { e.getUpSprites = f ? fistGetUp : stickGetUp; e.getUpTime = f ? fistGetUpTime : stickGetUpTime; }
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
            CurrentSprite = IdleSprite();
            bool near = Mathf.Abs(dx) <= wakeDistance && Mathf.Abs(hp.y - me.y) < 4f;
            if (e.IsAwake || near) { mode = Mode.Fight; e.IsAwake = true; decisionTimer = 0.4f; }
            else return;
        }
        if (mode == Mode.Fight) FightUpdate(dt, hero);
        else ActUpdate(dt, hero);
    }

    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    // ---------------- taistelu ----------------

    Sprite IdleSprite()
    {
        if (fists) return Has(fistIdle) ? fistIdle[(int)(animClock / idleFrameTime) % fistIdle.Length] : CurrentSprite;
        return Has(skate) ? skate[0] : CurrentSprite;   // paikallaan ei luistella: liuku-asento (jää pitää, hän hallitsee luistimiaan)
    }

    void FightUpdate(float dt, PlayerController hero)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y, dir = dx >= 0f ? 1f : -1f;
        if (!fists && Has(brake) && lastMoving && (dx > 0f) != e.FacingRight && Time.time >= nextTurnBrake)
        {   // suunnanmuutos luistimilla: jarruttaa (ei käänny paikallaan)
            nextTurnBrake = Time.time + 1.3f; lastMoving = false;
            StartBrake(skateSpeed * 0.9f); return;
        }
        e.FacingRight = dx > 0f;
        decisionTimer -= dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);

        // energia alle 50 %: pudottaa mailan (ei nosta sitä enää)
        if (!fists && e.HealthFraction < 0.5f && Has(drop) && !hero.IsFloored) { StartDrop(); return; }
        if (!fists && e.HealthFraction < 0.5f && !Has(drop)) { fists = true; ApplyReactions(true); }

        bool moving = false;
        if (!fists)
        {
            if (Has(tackle) && Time.time >= nextChargeTime && adx > chargeMinDistance && adx < chargeMaxDistance && Mathf.Abs(dy) < e.depthTolerance + 1.2f && !hero.IsFloored && decisionTimer <= 0f)
            {
                nextChargeTime = Time.time + chargeCooldown * (1f - 0.4f * Rage);
                StartCharge(); return;
            }
            // muuten luistelee rauhallisemmin lähemmäs ja pitää etäisyyttä
            if (adx > 5f) { MoveToward(dt, dir, dy, skateSpeed); moving = true; }
            else { float dyM = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f; if (dyM != 0f) e.BrainMove(new Vector2(0f, dyM)); }
            CurrentSprite = moving && Has(skate) ? skate[(int)(animClock / skateFrameTime) % skate.Length] : IdleSprite();
            if (moving) SkateSound(0.65f);
            lastMoving = moving;
            if (decisionTimer <= 0f && !hero.IsFloored && adx <= 5f && adx >= 1.2f && Mathf.Abs(dy) <= e.depthTolerance + 0.4f && Has(tackle) && Time.time >= nextChargeTime - 0.2f)
            {   // lähellä: suora taklaus ilman ryntäystä
                nextChargeTime = Time.time + chargeCooldown;
                StartTackle(hero); return;
            }
            if (decisionTimer <= 0f) decisionTimer = Random.Range(0.2f, 0.5f);
            return;
        }

        // vaihe 2: nyrkit, liukuu luistimilla kimppuun
        if (Has(jab1) && adx > fistRushMinDistance && adx < chargeMaxDistance && Time.time >= nextRushTime && Mathf.Abs(dy) < e.depthTolerance + 1.2f && !hero.IsFloored)
        {
            nextRushTime = Time.time + fistRushCooldown * (1f - 0.4f * Rage);
            StartCharge(); return;
        }
        float stop = 2.5f;
        if (adx > stop) { MoveToward(dt, dir, dy, glideSpeed * (1f + 0.4f * Rage)); moving = true; }
        else { float dyM = Mathf.Abs(dy) > e.depthTolerance * 0.8f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f; if (dyM != 0f) e.BrainMove(new Vector2(0f, dyM)); }
        CurrentSprite = IdleSprite();
        if (decisionTimer > 0f || hero.IsFloored) return;
        if (adx <= stop + 0.7f && Mathf.Abs(dy) <= e.depthTolerance + 0.15f) { StartJabs(hero); return; }
        decisionTimer = 0.15f;
    }

    void MoveToward(float dt, float dir, float dy, float speed)
    {
        float dyMove = Mathf.Abs(dy) > 0.4f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * dt) : 0f;
        e.BrainMove(new Vector2(dir * speed * dt, dyMove));
    }

    // ---------------- toiminnat ----------------

    readonly System.Collections.Generic.HashSet<Enemy> swept = new System.Collections.Generic.HashSet<Enemy>();
    public int sweepDamage = 14;
    /// Ryntäys ja taklaus kaataavat tiellä olevat toiset viholliset (toinen apuri, pomo), jos hero väistää.
    void SweepFoes()
    {
        Vector3 me = transform.position; float face = e.FacingRight ? 1f : -1f;
        foreach (var foe in Enemy.All)
        {
            if (foe == null || foe == e || foe.IsDownOrDead || swept.Contains(foe)) continue;
            Vector3 fp = foe.transform.position; float dx = (fp.x - me.x) * face;
            if (dx < -0.3f || dx > 1.7f || Mathf.Abs(fp.y - me.y) > e.depthTolerance + 0.2f) continue;
            swept.Add(foe);
            if (Enemy.EnemyHits(foe, sweepDamage, me.x, true)) HitFx.OnHitQuiet();
        }
    }

    void StartCharge()
    {
        swept.Clear();
        mode = Mode.Act; act = Act2.Charge; chargeT = 0f;
        if (!fists) Play(windupSounds);   // haymaker vain taklaukseen
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    void StartTackle(PlayerController hero)
    {
        if (act != Act2.Charge) swept.Clear();
        mode = Mode.Act; act = Act2.Tackle; t = 0f; hitDone = false; tackleHit = false; slideT = 0f; glideV = Mathf.Max(chargeSpeed * 0.6f, 5f);
        e.FacingRight = hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
    }

    void StartDrop()
    {
        mode = Mode.Act; act = Act2.Drop; t = 0f;
        Play(dropSounds);
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        CurrentSprite = drop[0];
    }

    void StartJabs(PlayerController hero)
    {
        mode = Mode.Act; act = Act2.Jab; jabStep = 0; jabT = 0f; jabHitDone = false;
        float r = Random.value;
        jabCount = r < 0.3f ? 1 : r < 0.7f ? 2 : 4;   // yksi, kaksi tai neljä suoraa
        combo = jabCount > 1;
        if (!Has(jab2)) jabCount = Mathf.Min(jabCount, 1);
        e.FacingRight = hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        decisionTimer = 0f;
    }

    void ActUpdate(float dt, PlayerController hero)
    {
        switch (act)
        {
            case Act2.Charge: ChargeUpdate(dt, hero); break;
            case Act2.Tackle: TackleUpdate(dt, hero); break;
            case Act2.Brake: BrakeUpdate(dt, hero); break;
            case Act2.Drop: DropUpdate(dt, hero); break;
            case Act2.Jab: JabUpdate(dt, hero); break;
            default: EndAct(); break;
        }
    }

    void ChargeUpdate(float dt, PlayerController hero)
    {
        chargeT += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x, adx = Mathf.Abs(dx), dy = hp.y - me.y, dir = dx >= 0f ? 1f : -1f;
        e.FacingRight = dx > 0f;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
        float speed = (fists ? fistRushSpeed : chargeSpeed) * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(chargeT / 0.6f));
        if (!fists && Has(skate)) { CurrentSprite = skate[(int)(animClock / (skateFrameTime * 0.7f)) % skate.Length]; SkateSound(0.42f); }
        else CurrentSprite = IdleSprite();
        if (hero.IsFloored || chargeT > 3.5f || Mathf.Abs(dy) > e.depthTolerance + 2.5f)
        {
            if (!fists && Has(brake)) { StartBrake(speed); return; }
            EndAct(); return;
        }
        if (!fists && adx <= tackleStartDistance) { StartTackle(hero); glideV = Mathf.Max(speed, 5f); return; }
        if (fists && adx <= 2.6f) { StartJabs(hero); return; }
        float dyMove = Mathf.Abs(dy) > e.depthTolerance * 0.6f ? Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.8f * dt) : 0f;
        if (speed > 3f) SweepFoes();
        e.BrainMove(new Vector2(dir * Mathf.Min(speed * dt, Mathf.Max(0f, adx - 1.4f)), dyMove));
    }

    void TackleUpdate(float dt, PlayerController hero)
    {
        t += dt;
        SweepFoes();
        float face = e.FacingRight ? 1f : -1f;
        int n = tackle.Length, imp = Mathf.Min(tackleImpact, n - 2);
        float wind = imp * tackleFrameTime;
        e.BrainPhaseSet(t >= wind - 0.05f && t < wind + 0.12f ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (t < wind)
        {   // lataus lähellä: kuvat 0..impact-1 vauhdissa eteenpäin
            CurrentSprite = tackle[Mathf.Min((int)(t / tackleFrameTime), imp - 1)];
            e.BrainMove(new Vector2(face * glideV * dt, 0f));
            float dy = hero.transform.position.y - transform.position.y;
            if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), depthSpeed * 0.8f * dt)));
            return;
        }
        if (!hitDone)
        {
            hitDone = true; TryTackleHit(hero);
            slideT = 0f;
        }
        // osuman jälkeen: ojennettu kuva (impact, impact+1) liukuu hidastuen, sitten palautuminen (viimeinen kuva)
        slideT += dt;
        float k = Mathf.Clamp01(slideT / slideTime);
        float v = slideSpeed * (1f - k) * (1f - k);
        e.BrainMove(new Vector2(face * v * dt, 0f));
        int last = n - 1;
        if (slideT < slideTime * 0.35f) CurrentSprite = tackle[imp];
        else if (slideT < slideTime * 0.85f) CurrentSprite = tackle[Mathf.Min(imp + 1, last - 1 >= imp ? last - 1 : imp)];
        else CurrentSprite = tackle[last];
        if (slideT >= slideTime) { decisionTimer = Random.Range(0.3f, 0.8f) * (1f - 0.4f * Rage); EndAct(); }
    }

    void TryTackleHit(PlayerController hero)
    {
        Vector3 me = transform.position, hp = hero.transform.position;
        float dx = hp.x - me.x;
        bool front = e.FacingRight ? dx >= -0.3f : dx <= 0.3f;
        if (!front || Mathf.Abs(dx) > tackleReach || Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.15f || hero.AirHeight > 0.9f) return;
        tackleHit = true;
        Play(swingSounds);
        if (!hero.TakeKnockdown(tackleDamage, me.x, tackleLaunchX, tackleLaunchUp, e)) hero.TakeHit(tackleDamage, me.x, e, true);
    }

    float nextSkateSound;
    void SkateSound(float interval)
    {
        if (Time.time < nextSkateSound) return;
        nextSkateSound = Time.time + interval;
        if (skateSounds == null || skateSounds.Length == 0 || e.Audio == null) return;
        var c = skateSounds[Random.Range(0, skateSounds.Length)];
        if (c != null) e.Audio.PlayOneShotLeveled(c, skateSoundVolume);
    }

    void StartBrake(float speed)
    {
        mode = Mode.Act; act = Act2.Brake; t = 0f; brakeSpeed = speed;
        Play(brakeSounds);
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    void BrakeUpdate(float dt, PlayerController hero)
    {
        t += dt;
        float face = e.FacingRight ? 1f : -1f, total = brake.Length * 0.034f;
        CurrentSprite = brake[Mathf.Min((int)(t / 0.034f), brake.Length - 1)];
        float k = Mathf.Clamp01(t / total);
        e.BrainMove(new Vector2(face * brakeSpeed * (1f - k) * dt, 0f));
        if (t >= total) { decisionTimer = Random.Range(0.2f, 0.5f); EndAct(); }
    }

    void DropUpdate(float dt, PlayerController hero)
    {
        t += dt;
        const float ft = 0.045f;
        CurrentSprite = drop[Mathf.Min((int)(t / ft), drop.Length - 1)];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (t >= drop.Length * ft + 0.1f)
        {
            fists = true; ApplyReactions(true);
            decisionTimer = 0.3f; EndAct();
        }
    }

    void JabUpdate(float dt, PlayerController hero)
    {
        jabT += dt;
        Vector3 me = transform.position, hp = hero.transform.position;
        float face = e.FacingRight ? 1f : -1f;
        var seq = (jabStep % 2 == 0) ? jab1 : (Has(jab2) ? jab2 : jab1);
        int imp = (jabStep % 2 == 0) ? jab1Impact : jab2Impact; imp = Mathf.Min(imp, seq.Length - 2);
        float wind = imp * jabFrameTime * (jabStep > 0 ? 0.7f : 1f);
        int pos = jabT < wind ? Mathf.Min((int)(jabT / Mathf.Max(0.001f, wind) * imp), imp - 1 < 0 ? 0 : imp - 1)
                              : Mathf.Min(imp + (int)((jabT - wind) / jabFrameTime), seq.Length - 1);
        CurrentSprite = seq[Mathf.Max(0, pos)];
        e.BrainPhaseSet(jabT >= wind - 0.04f && jabT < wind + 0.1f ? Enemy.BrainPhase.Punch : Enemy.BrainPhase.Windup);
        if (jabT < wind && jabLunge > 0f && Mathf.Abs(hp.x - me.x) > 1.1f) e.BrainMove(new Vector2(face * jabLunge / Mathf.Max(0.05f, wind) * dt, 0f));
        float dy = hp.y - me.y;
        if (Mathf.Abs(dy) > e.depthTolerance * 0.6f) e.BrainMove(new Vector2(0f, Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), DepthFollow * 0.8f * dt)));
        if (!jabHitDone && jabT >= wind)
        {
            jabHitDone = true;
            Play(jabStep % 2 == 0 ? jab1Sounds : jab2Sounds);
            float dx = hp.x - me.x; bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
            if (front && Mathf.Abs(dx) <= jabReach + 0.2f && Mathf.Abs(dy) <= e.depthTolerance + 0.1f && hero.AirHeight <= 0.9f)
            {
                bool last = jabStep == jabCount - 1;
                int dmg = ((jabStep % 2 == 0) ? jab1Damage : jab2Damage) + (last && combo ? finisherBonus : 0);
                if (last && combo) { if (!hero.TakeKnockdown(dmg, me.x, 6f, 4.5f, e)) hero.TakeHit(dmg, me.x, e, jabStep > 0); }
                else hero.TakeHit(dmg, me.x, e, jabStep > 0);
            }
        }
        float total = wind + (seq.Length - imp) * jabFrameTime;
        if (jabT >= total)
        {
            jabStep++;
            if (jabStep < jabCount && !hero.IsFloored && Mathf.Abs(hp.x - me.x) < 3.8f && Mathf.Abs(dy) <= e.depthTolerance + 0.6f) { jabT = 0f; jabHitDone = false; }
            else { decisionTimer = Random.Range(decisionPause.x, decisionPause.y) * (1f - 0.5f * Rage); EndAct(); }
        }
    }

    void EndAct()
    {
        mode = Mode.Fight; act = Act2.None; combo = false;
        e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    /// Toiminta keskeytyi ulkopuolelta (osuma, kaatuminen...).
    void AbortAction()
    {
        if (mode == Mode.Act && act != Act2.None) { mode = Mode.Fight; act = Act2.None; }
        combo = false;
        decisionTimer = Mathf.Max(decisionTimer, 0.3f);
    }
}
