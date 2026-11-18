using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Taikuri (kasinon showbaari, baari, hääkappeli): tavalliset iskut hoitaa Enemy (jabi, kepin työntö, suora, korkea potku), tämä aivo lisää taiat:
///  - SAVUPOMMI (Smoke): heittää pommin lattiaan, räjähdys lennättää kaikki lähellä olevat, taikuri katoaa räjähdykseen ja ilmestyy kauas.
///  - JÄÄ (Ice): sauvalla maahan, jäätävä lattia (IcePatch) kaataa kaikki, jotka astuvat siihen.
///  - KORTIT (Cards): sauvan lyönnin ojennuksessa kortteja lentää hirmuvauhdilla (CardBarrage).
///  - PEILIKUVAT (Mirrors): 2-3 läpikuultavaa kopiota hyökkää ja taikuri vaihtaa paikkaa yhden kanssa (MirrorImages).
///  - VÄISTÖ: satunnainen väistö (ei suojausta); taikuri teleporttaa hyökkääjän taakse ja potkaisee heti kaatavasti.
/// Käyttö: moves-lista jää tyhjäksi; spells ja dodgeMove/teleportKick asetetaan MagicianSetupissa.
/// </summary>
public class MagicianBrain : MongolBrain, IEnemyBrain
{
    public enum Kind { Smoke, Ice, Cards, Mirrors, Gallon, Bull }
    [System.Serializable]
    public class Spell
    {
        public string name; public Kind kind; public Move move; public int eventFrame;
        public float minRange = 0f, maxRange = 9f, weight = 1f, cooldown = 8f;
        [HideInInspector] public float nextTime;
    }

    public Spell[] spells;
    [Header("Väistö + teleportti + kaatava potku")]
    public Move dodgeMove, teleportKick; public int dodgeEventFrame = 3;
    [Range(0f, 1f)] public float dodgeChance = 0.3f; public float dodgeCooldown = 3f, behindDistance = 1.7f;
    [Header("Savupommi")]
    public Sprite[] explosionSprites; public float explosionFrameTime = 0.05f, explosionScale = 1f, explosionRadiusX = 6.5f, explosionRadiusY = 2.6f, explosionLaunch = 11f, explosionUp = 6f; public int explosionDamage = 18;
    public float reappearDistance = 8f; public Vector2 explosionPivot = new Vector2(0.5f, 0.05f);
    [Header("Taiat")]
    public float wandTipForward = 1.9f;
    public MirrorImages.Config mirrorConfig; public CardBarrage.Config cardConfig;
    public Sprite[] poofSmall;

    Spell curSpell; bool fired; bool hidden; float nextDodgeAt; int dodgePhase;   // dodgePhase: 0 ei väistöä, 1 väistössä (teleportti tekemättä), 2 teleportattu
    Vector3 smokeCenter;

    bool Hidden => hidden;
    [Tooltip("Savupommin heiton nopeutus (1 = videon nopeus).")] public float smokeThrowSpeed = 1.45f; bool smokeSped;
    // HÄRKÄ: napsautus -> muodonmuutos -> härkä juoksee eteenpäin (Move.moveFrom...) ja lennättää kaikki tieltään; koko ajan suojattu (tavalliset iskut eivät keskeytä, kaatavat eivät osu)
    bool bullActive, bullCharging; Move bullMove; readonly System.Collections.Generic.HashSet<Object> bullHit = new System.Collections.Generic.HashSet<Object>();
    public int bullDamage = 28; public float bullLaunch = 30f, bullLift = 9f, bullEnemyLaunch = 30f, bullEnemyLift = 11f, bullJumpClear = 1.0f;   // yli voi hypätä (AirHeight > bullJumpClear)
    int recentHits; float lastHitT;
    bool Armored => SmokeWinding || bullActive;
    bool SmokeWinding => cur != null && curSpell != null && curSpell.kind == Kind.Smoke && !fired && !hidden;
    /// Savupommin heiton ajan tavalliset iskut tekevät vahinkoa mutta eivät keskeytä (Enemy.TakeHit tarkistaa ArmorNow).
    public new bool ArmorNow => Armored;
    static bool Has(Move m) => m != null && m.sprites != null && m.sprites.Length > 0;

    // taikurin äänet (Resources/Sfx/Taikuri): jäätaian huuto ja voitto, kun taikuri kaataa heron
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> voiceCache = new System.Collections.Generic.Dictionary<string, AudioClip>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetVoices() { voiceCache.Clear(); }
    void Say(string n)
    {
        if (e == null || e.Audio == null) return;
        if (!voiceCache.TryGetValue(n, out var c)) { c = Resources.Load<AudioClip>("Sfx/Taikuri/" + n); voiceCache[n] = c; }
        if (c != null) e.Audio.PlayOneShot(c, 1f);
    }
    float lastActiveT = -9f, lastWinT = -99f; readonly System.Collections.Generic.Dictionary<PlayerController, bool> downPrev = new System.Collections.Generic.Dictionary<PlayerController, bool>();

    protected override void Update()
    {
        base.Update();
        // voitto-ääni: kun taikuri kaataa heron (hero kaatuu taian/iskun aikana tai heti sen jälkeen)
        if (e != null && !e.IsDead && e.IsAwake)
        {
            if (cur != null || hidden) lastActiveT = Time.time;
            foreach (var pl in Players.All)
            {
                if (pl == null) continue;
                bool dn = pl.IsFloored; downPrev.TryGetValue(pl, out bool was); downPrev[pl] = dn;
                if (dn && !was && Time.time - lastActiveT < 3f && Time.time - lastWinT > 5f) { lastWinT = Time.time; Say("taikuri_voitto"); }
            }
        }
        if (!hidden && curSpell != null && cur == null) curSpell = null;
        TryTrick();
        // taidon valinta (samat ehdot kuin MongolBrainilla)
        if (cur != null || spells == null || e == null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead || Time.time < nextAny) return;
        var tt = TargetT(); if (tt == null) return;
        float dx = Mathf.Abs(tt.position.x - transform.position.x), dy = Mathf.Abs(tt.position.y - transform.position.y);
        if (Random.value > chancePerSec * Time.deltaTime) return;
        float total = 0f;
        foreach (var s in spells) if (Ok(s, dx)) total += s.weight;
        if (total <= 0f) return;
        float r = Random.value * total;
        foreach (var s in spells)
        {
            if (!Ok(s, dx)) continue;
            r -= s.weight;
            if (r <= 0f) { Cast(s); return; }
        }
    }

    // ---- taikatemput kesken tappelun: kun hero on kaukana, taikuri esittelee kyyhky- tai silinteritempun (rauhallisen idlen kuvat); keskeytyy, kun hero tulee lähelle tai osuu ----
    Move[] trickMoves; bool isTrick; float nextTrickAt = -1f; Move trickCur;
    [Tooltip("Kyyhky- ja silinteritemput kesken tappelun (pois: kuvissa kyyhky ja hattu häviävät, kun taikuri päästää niistä irti).")] public bool fightTricks = false;
    [Tooltip("Herätysetäisyys (-1 = ei läheisyydestä): taikuri esiintyy rauhassa (baari, kappeli, lava), kunnes häneen tai lähellä olevaan osuu.")] public float calmWakeDistance = -1f; bool wakeSet;
    /// Taiat olivat liian harvinaisia: taikuri seisoo heron vieressä (minRange 2-4 esti kaiken) ja lähitaistelu vie vuoron. Nyt taikoja voi käyttää myös läheltä, ne toistuvat tiheämmin ja valinta tapahtuu useammin.
    public float spellCooldownScale = 0.6f, spellChancePerSec = 2.5f, spellMinRangeCap = 0.8f;
    void TuneSpells()
    {
        dodgeChance = Mathf.Max(dodgeChance, 0.5f); dodgeCooldown = Mathf.Min(dodgeCooldown, 1.5f);   // väistö + teleportti + kaatava potku tiheämmin
        chancePerSec = Mathf.Max(chancePerSec, spellChancePerSec); globalCooldown = Mathf.Min(globalCooldown, 1.3f);
        if (spells == null) return;
        foreach (var sp in spells) { if (sp == null) continue; sp.minRange = 0f; sp.maxRange = 9999f;   // taikoja käytetään välimatkasta riippumatta
             sp.cooldown *= spellCooldownScale; sp.nextTime = Time.time + Random.Range(0f, 3f); }
    }
    void AddGallonSpell()
    {
        if (spells == null) return;
        Spell ice = null; foreach (var sp in spells) { if (sp == null) continue; if (sp.kind == Kind.Gallon) return; if (sp.kind == Kind.Ice) ice = sp; }
        if (ice == null || !Has(ice.move)) return;
        var mv = new Move { name = "Gallona", sprites = ice.move.sprites, frameTime = ice.move.frameTime, hitFrom = ice.eventFrame, hitTo = -1, damage = 0, impactHold = 0.05f };
        var l = new System.Collections.Generic.List<Spell>(spells);
        l.Add(new Spell { name = "Savuava jerrygallona", kind = Kind.Gallon, move = mv, eventFrame = ice.eventFrame, minRange = 3f, maxRange = 11f, weight = 0.7f, cooldown = 13f });
        spells = l.ToArray();
    }
    void TryTrick()
    {
        if (!wakeSet && e != null) { wakeSet = true; e.calmIdleLoop = true; AddGallonSpell(); TuneSpells(); if (GetComponent<MagicianTricks>() == null) gameObject.AddComponent<MagicianTricks>(); e.wakeDistance = calmWakeDistance; if (e.wakeOnFightRadius <= 0f) e.wakeOnFightRadius = 12f; }   // herää vasta osumasta tai kun lähellä tapellaan (ei pelkästä läheisyydestä)
        if (!fightTricks) return;
        if (e == null || e.Hero == null || cur != null || hidden || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead) return;
        if (nextTrickAt < 0f) nextTrickAt = Time.time + Random.Range(4f, 9f);
        if (Time.time < nextTrickAt || Time.time < nextAny) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x);
        if (dx < 5f) return;
        if (trickMoves == null)
        {
            var cs = e.calmIdleSprites; var l = new List<Move>();
            if (cs != null && cs.Length >= 240) { l.Add(TrickMove("Temppu 1", cs, 0, 120)); l.Add(TrickMove("Temppu 2", cs, 120, 120)); }
            else if (cs != null && cs.Length > 10) l.Add(TrickMove("Temppu", cs, 0, cs.Length));
            trickMoves = l.ToArray();
        }
        nextTrickAt = Time.time + Random.Range(9f, 16f);
        if (trickMoves.Length == 0) return;
        trickCur = trickMoves[Random.Range(0, trickMoves.Length)];
        Begin(trickCur); isTrick = true; curSpell = null;
    }
    static Move TrickMove(string name, Sprite[] all, int from, int count)
    {
        var s = new Sprite[count]; System.Array.Copy(all, from, s, 0, count);
        return new Move { name = name, sprites = s, frameTime = 0.045f, hitFrom = 0, hitTo = -1, damage = 0, impactHold = 0f };
    }

    /// Kohde: lähin hero tai se vihu, jota taikuri jahtaa (kappelin rähinä: kaikki kaikkia vastaan) -> taikoja käytetään myös, kun lähellä on muita vihuja.
    Transform TargetT()
    {
        Transform best = e.Hero != null ? e.Hero.transform : null; var te = e.TargetEnemy;
        if (te != null && te.isActiveAndEnabled && !te.IsDead)
        {
            float dh = best != null ? Mathf.Abs(best.position.x - transform.position.x) : 999f, dt2 = Mathf.Abs(te.transform.position.x - transform.position.x);
            if (best == null || dt2 < dh) best = te.transform;
        }
        return best;
    }
    void FaceTarget()
    {
        var tt = TargetT(); if (tt == null || e == null) return;
        e.FacingRight = tt.position.x > transform.position.x; dirSign = e.FacingRight ? 1f : -1f;
    }

    bool Ok(Spell s, float dx) => s != null && Has(s.move) && Time.time >= s.nextTime && dx >= s.minRange && dx <= s.maxRange;

    void Cast(Spell s)
    {
        if (s.kind == Kind.Smoke && !smokeSped && s.move != null) { smokeSped = true; s.move.frameTime /= Mathf.Max(1f, smokeThrowSpeed); }   // savupommin heitto nopeammaksi (kerran)
        curSpell = s; fired = false; s.nextTime = Time.time + s.cooldown * Random.Range(0.85f, 1.25f);
        Begin(s.move); FaceTarget();
    }

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi (väistö tai piilossa räjähdyksessä).
    public new bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (hidden || dodgePhase == 1) return true;
        if (Armored && knockdown) return true;   // savupommin heitto ei katkea edes kaatavaan iskuun (porukka ei estä sitä)
        if (cur != null || e == null || e.Hero == null || !Has(dodgeMove) || Time.time < nextDodgeAt || !e.BrainIdleOrChase) return false;
        // mitä enemmän iskuja lyhyen ajan sisällä, sitä todennäköisemmin hän väistää ja teleporttaa
        if (Time.time - lastHitT > 2.5f) recentHits = 0;
        recentHits++; lastHitT = Time.time;
        if (Random.value > Mathf.Min(0.92f, dodgeChance + 0.14f * (recentHits - 1))) return false;
        recentHits = 0;
        nextDodgeAt = Time.time + dodgeCooldown;
        curSpell = null; dodgePhase = 1; fired = false;
        Begin(dodgeMove);
        return true;
    }

    public new void Tick(float dt)
    {
        base.Tick(dt);
        if (cur == null)
        {
            if (hidden) Reappear(true);
            if (bullActive) EndBull();
            dodgePhase = 0; isTrick = false; return;
        }
        if (isTrick)
        {
            if (cur != trickCur) isTrick = false;
            else if (e.Hero != null && Mathf.Abs(e.Hero.transform.position.x - transform.position.x) < 3.5f) { End(); isTrick = false; return; }
            else return;
        }
        if (bullActive) TickBull();
        if (cur == dodgeMove && dodgePhase == 1)
        {
            int di = System.Array.IndexOf(cur.sprites, CurrentSprite);
            if (di >= dodgeEventFrame) { TeleportBehind(); dodgePhase = 2; }
            return;
        }
        if (curSpell != null && cur == curSpell.move && !fired)
        {
            int i = System.Array.IndexOf(cur.sprites, CurrentSprite);
            if (i >= curSpell.eventFrame) { fired = true; Fire(curSpell); }
        }
        if (curSpell != null && curSpell.kind == Kind.Smoke && fired && hidden && cur == curSpell.move)
        {
            // piilossa: ilmestyy kauas muutaman kuvan päästä räjähdyksestä
            int i = System.Array.IndexOf(cur.sprites, CurrentSprite);
            if (i >= curSpell.eventFrame + 14) Reappear(false);
        }
    }

    void LateUpdate()
    {
        if (blastPending && Time.time >= blastAt) Blast();
        if (!hidden || e == null) return;
        if (e.body != null) e.body.enabled = false;
        if (e.shadow != null) e.shadow.enabled = false;
    }

    void OnDisable() { blastPending = false; if (hidden) Reappear(true); }

    // ---------------- taiat ----------------
    Vector3 Tip() { return transform.position + new Vector3(dirSign * wandTipForward, 0f, 0f); }

    void Fire(Spell s)
    {
        switch (s.kind)
        {
            case Kind.Ice: IcePatch.Spawn(Tip(), dirSign, 13f, e, 9f); Say("taikuri_jaatikko"); break;
            case Kind.Cards: CardBarrage.Cast(e, Tip(), dirSign, new CardBarrage.Config()); break;   // oletusasetukset koodissa (ei tarvitse ajaa valikkoa uudelleen)
            case Kind.Mirrors:
                if (mirrorConfig == null || e.Hero == null) { Debug.LogWarning("[Taikuri] peilikuvat: mirrorConfig puuttuu tai ei heroa (aja valikko 409)"); break; }
                mirrorConfig.alpha = Mathf.Max(mirrorConfig.alpha, 0.8f); mirrorConfig.life = Mathf.Max(mirrorConfig.life, 6f);
                if (MirrorImages.Cast(e, e.Hero, mirrorConfig) == null) Debug.LogWarning("[Taikuri] peilikuvia ei luotu (kuvia puuttuu tai edelliset vielä käynnissä)");
                break;
            case Kind.Smoke: Explode(); break;
            case Kind.Gallon: ShootGallon(); break;
            case Kind.Bull: StartBull(s); break;
        }
    }

    // Räjähdyskuvasarjan 0. kuva on vain taikuri kyyköttämässä pommin vieressä ja salama syttyy vasta kuvissa 1-2: lennätys, ääni ja tärinä viivästetään salaman kohdalle (blastDelay)
    bool blastPending; float blastAt; public float blastDelay = 0.09f;
    // ---- savuava jerrygallona sauvasta: sytytetty gallona lentää kohti heroa ja räjähtää hetken päästä (ei räjäytä taikuria) ----
    static AudioClip bullChangeClip, bullRunClip;
    void StartBull(Spell s)
    {
        bullActive = true; bullCharging = false; bullMove = s.move; bullHit.Clear();
        if (bullChangeClip == null) bullChangeClip = Resources.Load<AudioClip>("Sfx/Taikuri/harka_muutos");
        if (bullChangeClip != null) HitFx.PlayClip(bullChangeClip, 1.0f);
    }
    void TickBull()
    {
        if (cur == null || cur != bullMove) { EndBull(); return; }
        int idx = System.Array.IndexOf(cur.sprites, CurrentSprite);
        if (!bullCharging && idx >= cur.moveFrom)
        {
            bullCharging = true; bullHit.Clear();
            if (bullRunClip == null) bullRunClip = Resources.Load<AudioClip>("Sfx/Taikuri/harka_juoksu");
            if (bullRunClip != null) HitFx.PlayClip(bullRunClip, 1.1f);
            CameraFollow.Shake(0.2f, 0.4f);
        }
        if (!bullCharging || idx < cur.moveFrom + 10) return;   // vahinko vasta, kun härkä on muotoutunut savusta
        Vector3 c = transform.position + new Vector3(dirSign * 1.3f, 0f, 0f);
        foreach (var pl in Players.All)
        {
            if (pl == null || pl.GameOver || bullHit.Contains(pl)) continue;
            Vector3 p = pl.transform.position;
            if (Mathf.Abs(p.x - c.x) > 3.3f || Mathf.Abs(p.y - c.y) > 1.5f || pl.AirHeight > bullJumpClear) continue;
            bullHit.Add(pl);
            float srcX = p.x - dirSign;   // lento eteenpäin härän menosuuntaan
            if (!pl.TakeKnockdown(bullDamage, srcX, bullLaunch, bullLift, e, true)) pl.TakeHit(bullDamage, srcX, e, true);
            CameraFollow.Shake(0.25f, 0.3f);
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || en == e || !en.isActiveAndEnabled || en.IsDead || bullHit.Contains(en)) continue;
            Vector3 p = en.transform.position;
            if (Mathf.Abs(p.x - c.x) > 3.3f || Mathf.Abs(p.y - c.y) > 1.5f || en.IsAirborneNow) continue;
            bullHit.Add(en); Enemy.EnemyHits(en, bullDamage, p.x - dirSign, true);
            en.Launch(dirSign * bullEnemyLaunch, bullEnemyLift);   // kauas härän menosuuntaan
            CameraFollow.Shake(0.2f, 0.25f);
        }
    }
    void EndBull()
    {
        bool was = bullActive; bullActive = false; bullCharging = false;
        if (was) PlayFx(poofSmall, transform.position, 0.045f, 1.3f, new Vector2(0.5f, 0.05f));   // härkä muuttuu takaisin taikuriksi savupuuskassa
    }

    void ShootGallon()
    {
        if (e == null || e.Hero == null) return;
        var go = new GameObject("Taikurin jerrygallona"); Vector3 tip = Tip();
        go.transform.position = new Vector3(tip.x, transform.position.y, 0f);
        var can = go.AddComponent<Jerrycan>(); can.spawnedByFoe = true; can.blastExempt = e; can.blastRadiusX = 5.4f; can.blastRadiusY = 2.4f; can.launchMul = 1.7f;   // isompi ala ja kauemmas lentävät
        var crate = go.GetComponent<Crate>(); if (crate == null) { Destroy(go); return; }
        can.Ignite(1.7f);
        crate.PickUp(); crate.SetCarried(go.transform.position, 1.7f, Mathf.RoundToInt(-transform.position.y * 100f) + 2);
        var gt = TargetT(); float dx = gt != null ? Mathf.Abs(gt.position.x - transform.position.x) : 8f;
        crate.Throw(dirSign * Mathf.Clamp(dx * 1.7f, 9f, 17f), 3.4f, e, 12);
        CameraFollow.Shake(0.05f, 0.1f);
    }

    void Explode()
    {
        smokeCenter = transform.position + new Vector3(dirSign * 0.6f, 0f, 0f);
        PlayFx(explosionSprites, transform.position, explosionFrameTime, explosionScale, explosionPivot);
        blastPending = true; blastAt = Time.time + blastDelay;
        hidden = true;
        if (e.body != null) e.body.enabled = false;
        if (e.shadow != null) e.shadow.enabled = false;
    }

    void Blast()
    {
        blastPending = false;
        CameraFollow.Shake(0.25f, 0.5f);
        var boom = Resources.Load<AudioClip>("Sfx/Rajahdys/rajahdys");
        if (boom != null) HitFx.PlayClip(boom, 1.3f); else HitFx.OnBreak(0.12f);
        // lennätä kaikki lähellä olevat (herot ja muut vihut, ei taikuria); arvot koodissa vähintään näin isoja (scenessä vanhat oletukset)
        float rx = Mathf.Max(explosionRadiusX, 9f), ry = Mathf.Max(explosionRadiusY, 3.6f), launch = Mathf.Max(explosionLaunch, 15f), up = Mathf.Max(explosionUp, 8f);
        foreach (var pl in Players.All)
        {
            if (pl == null || pl.GameOver) continue;
            Vector3 p = pl.transform.position;
            if (Mathf.Abs(p.x - smokeCenter.x) > rx || Mathf.Abs(p.y - smokeCenter.y) > ry) continue;
            float srcX = Mathf.Abs(p.x - smokeCenter.x) > 0.3f ? smokeCenter.x : (p.x >= smokeCenter.x ? p.x - 1f : p.x + 1f);   // lento aina ulospäin räjähdyksestä
            if (!pl.TakeKnockdown(explosionDamage, srcX, launch, up, e, true)) pl.TakeHit(explosionDamage, srcX, e, true);
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || en == e || !en.isActiveAndEnabled || en.IsDead) continue;
            Vector3 p = en.transform.position;
            if (Mathf.Abs(p.x - smokeCenter.x) > rx || Mathf.Abs(p.y - smokeCenter.y) > ry) continue;
            Enemy.EnemyHits(en, explosionDamage, smokeCenter.x, true);
        }
    }

    void Reappear(bool immediate)
    {
        if (!hidden) return;
        hidden = false;
        if (e != null && e.body != null) e.body.enabled = true;
        if (e != null && e.shadow != null) e.shadow.enabled = true;
        if (e == null) return;
        if (!immediate) PlaceFar();
    }

    void PlaceFar()
    {
        var hero = e.Hero; var a = Area.Current;
        float hx = hero != null ? hero.transform.position.x : smokeCenter.x;
        float side = Random.value < 0.5f ? -1f : 1f;
        float x = smokeCenter.x + side * reappearDistance, y = transform.position.y;
        if (a != null)
        {
            if (x < a.walkMinX + 1f || x > a.walkMaxX - 1f) x = smokeCenter.x - side * reappearDistance;
            x = Mathf.Clamp(x, a.walkMinX + 0.8f, a.walkMaxX - 0.8f);
            y = Mathf.Clamp(y + Random.Range(-1f, 1f), a.minDepthY + 0.3f, a.maxDepthY - 0.3f);
        }
        var old = transform.position;
        transform.position = new Vector3(x, y, old.z);
        e.FacingRight = hx > x; dirSign = e.FacingRight ? 1f : -1f;
        PlayFx(poofSmall, transform.position, 0.045f, 1f, new Vector2(0.5f, 0.05f));
    }

    void TeleportBehind()
    {
        var hero = e.Hero; if (hero == null) return;
        var a = Area.Current; Vector3 hp = hero.transform.position, old = transform.position;
        float behind = old.x <= hp.x ? 1f : -1f;                 // hyökkääjän toiselle puolelle
        float x = hp.x + behind * behindDistance;
        if (a != null && (x < a.walkMinX + 0.5f || x > a.walkMaxX - 0.5f)) { behind = -behind; x = hp.x + behind * behindDistance; }
        if (a != null) x = Mathf.Clamp(x, a.walkMinX + 0.5f, a.walkMaxX - 0.5f);
        PlayFx(poofSmall, old, 0.045f, 1f, new Vector2(0.5f, 0.05f));
        transform.position = new Vector3(x, hp.y, old.z);
        PlayFx(poofSmall, transform.position, 0.045f, 1f, new Vector2(0.5f, 0.05f));
        e.FacingRight = hp.x > x; dirSign = e.FacingRight ? 1f : -1f;
        if (Has(teleportKick)) { cur = null; CurrentSprite = null; Begin(teleportKick); }
    }

    // ---------------- yksinkertainen kuva-animaatio (räjähdys, savu) ----------------
    void PlayFx(Sprite[] frames, Vector3 pos, float frameTime, float scale, Vector2 pivotHint)
    {
        if (frames == null || frames.Length == 0 || e == null || e.body == null) return;
        var go = new GameObject("Taikurin efekti"); go.transform.position = new Vector3(pos.x, pos.y, 0f);
        float s = Mathf.Max(0.01f, e.body.transform.lossyScale.x) * scale; go.transform.localScale = new Vector3(s * (dirSign < 0f && frames == explosionSprites ? -1f : 1f), s, 1f);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = frames[0]; sr.sortingOrder = e.body.sortingOrder + 4;
        var a = go.AddComponent<FxAnim>(); a.frames = frames; a.frameTime = frameTime;
    }
    sealed class FxAnim : MonoBehaviour
    {
        public Sprite[] frames; public float frameTime; float t; SpriteRenderer sr;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime; int i = (int)(t / frameTime);
            if (i >= frames.Length) { Destroy(gameObject); return; }
            sr.sprite = frames[i];
        }
    }
}
