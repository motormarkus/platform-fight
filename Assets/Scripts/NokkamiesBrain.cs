using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nokkamies (työmaakonttorin pomo): MongolBrainin erikoisliikkeet + kolme omaa mekaniikkaa:
/// 1) KAASUPULLOT: kävelee huoneen kaasupullon taakse ja potkaisee sen raketiksi heroa kohti (hero väistää syvyyssuunnassa; pullo räjähtää seinään).
///    Hero voi itse lyödä pulloja Nokkamiestä kohti (GasCylinder.bossBlastDamage).
/// 2) RAKSAJÄTKÄT: elämien pudotessa (waveAt-rajat) viheltää pilliin, ja apurit (mallit, ei aktiivisia) ryntäävät portaista; mukana tulee uusia kaasupulloja.
/// 3) RYNNÄKKÖ: vauhdinotto (syöksyn alku), sitten juoksu seinään asti. Rynnäkköä ei voi pysäyttää (kevyet iskut eivät katkaise, kaatavat menevät ohi);
///    osuu heroon kerran (kaataa). Seinään törmätessä Nokkamies on pökerryksissä (stunTime): ei hyökkää eikä torju, ottaa enemmän vahinkoa.
/// </summary>
public class NokkamiesBrain : MongolBrain, IEnemyBrain
{
    [Header("Kaasupullot")]
    public Move kickMove;                       // etupotku: osumakuvassa (kickMove.hitFrom) pullo lähtee
    public Sprite[] walkSprites;
    public float walkFrameTime = 1f / 12f, fetchSpeed = 4.2f, fetchMaxTime = 3f, fetchMaxDistance = 9f;
    public float cylinderCooldown = 5f, cylinderMinHeroDx = 3.2f;
    [Header("Raksajätkät")]
    public Move whistleMove;
    [Tooltip("Pillin kuvajonon indeksi, jossa pilli on suulla: ääni + apurit.")] public int whistleFrame = 10;
    public AudioClip whistleClip;
    public GameObject[] crewTemplates;          // ei-aktiiviset vihumallit
    public GameObject cylinderTemplate;          // ei-aktiivinen kaasupullomalli
    [Range(0f, 1f)] public float[] waveAt = { 0.75f, 0.5f, 0.25f };
    public int crewPerWave = 2, cylindersPerWave = 2, maxCrewAlive = 3;
    public float crewSpawnX, crewSpawnSpread = 1.2f;
    [Header("Rynnäkkö")]
    public Move chargeMove;                      // sprites = juoksu (silmukka)
    public Sprite[] chargeWindup;                // vauhdinotto
    public Move stunMove;                        // sprites = osuma (silmukka)
    public float chargeWindupFrameTime = 0.05f, chargeFrameTime = 0.05f, chargeSpeed = 12.5f, chargeMaxTime = 3.2f, chargeCooldown = 7f, chargeMinDx = 3f;
    public int chargeDamage = 26;
    public float stunTime = 2.6f, stunDamageMul = 1.6f;

    enum Mode { None, Fetch, Kick, Whistle, Charge, Stun }
    Mode mode;
    float modeT, nextCylinderAt = 4f, nextChargeAt = 6f, stunUntil, keepBlock, keepDmgMul = 1f; bool keepGuard;
    Move fetchMove;
    bool chargeHit, chargeRunning, stunApplied, whistled;
    GasCylinder target;
    float targetDir;
    int wavesDone;
    readonly List<Enemy> crew = new List<Enemy>();

    public new bool ArmorNow => mode == Mode.Charge;
    public new bool InterceptHit(int damage, float attackerX, bool knockdown) => mode == Mode.Charge && knockdown;   // rynnäkössä kaatavat iskut ohi

    protected override void Update()
    {
        if (e == null || e.IsDead) { base.Update(); return; }
        crew.RemoveAll(c => c == null || c.IsDead);
        if (cur != null && Time.time - lastTick > 0.25f) { End(); mode = Mode.None; }   // osuma/kaatuminen katkaisi liikkeen
        // nurkkapurkaus (pomoa hakataan seinää vasten) katkaisee pökerryksen: muuten pökerrys piti hänet loputtomiin lukittuna ja purkaus ei päässyt alkamaan
        if (e.CornerBurstOn && (Time.time < stunUntil || mode == Mode.Stun)) { stunUntil = 0f; if (cur != null) End(); mode = Mode.None; }
        // pökerrys jatkuu osumien välissä (osuma katkaisee liikkeen -> aloitetaan uudelleen)
        if (Time.time < stunUntil)
        {
            if (cur == null && e.BrainIdleOrChase && e.Hero != null && HasSprites(stunMove)) { StartMode(Mode.Stun, stunMove); }
            return;
        }
        if (stunApplied) EndStun();
        if (cur != null || !e.IsAwake || !e.BrainIdleOrChase || e.Hero == null) { base.Update(); return; }
        // pillin vihellys elämärajoilla
        if (waveAt != null && wavesDone < waveAt.Length && e.Health <= e.maxHealth * waveAt[wavesDone] && HasSprites(whistleMove))
        {
            wavesDone++;
            if (crew.Count < maxCrewAlive) { StartMode(Mode.Whistle, whistleMove); return; }
        }
        float hx = e.Hero.transform.position.x, mx = transform.position.x, dx = Mathf.Abs(hx - mx);
        float dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        // rynnäkkö
        if (Time.time >= nextChargeAt && dx >= chargeMinDx && dy < 1.2f && HasSprites(chargeMove) && Random.value < 0.9f * Time.deltaTime * (Raged ? 2f : 1f))
        {
            nextChargeAt = Time.time + chargeCooldown * Random.Range(0.8f, 1.3f) * (Raged ? 0.6f : 1f);
            StartMode(Mode.Charge, chargeMove); return;
        }
        // kaasupullo
        if (Time.time >= nextCylinderAt && dx >= cylinderMinHeroDx && HasSprites(kickMove) && walkSprites != null && walkSprites.Length > 0)
        {
            var c = PickCylinder();
            if (c != null)
            {
                nextCylinderAt = Time.time + cylinderCooldown * Random.Range(0.8f, 1.3f);
                target = c; targetDir = Mathf.Sign(hx - c.transform.position.x);
                StartMode(Mode.Fetch, kickMove); return;
            }
            nextCylinderAt = Time.time + 1f;
        }
        base.Update();
    }

    static bool HasSprites(Move m) => m != null && m.sprites != null && m.sprites.Length > 0;

    GasCylinder PickCylinder()
    {
        var a = Area.Current; GasCylinder best = null; float bd = fetchMaxDistance;
        float hx = e.Hero.transform.position.x;
        foreach (var g in GasCylinder.All)
        {
            if (g == null || !g.IsIdle) continue;
            Vector3 q = g.transform.position;
            if (a != null && a.walkMaxX > a.walkMinX && (q.x < a.walkMinX || q.x > a.walkMaxX)) continue;
            if (Mathf.Abs(q.x - hx) < 2.5f) continue;   // pullo heron vieressä: ei järkeä
            float d = Mathf.Abs(q.x - transform.position.x) + Mathf.Abs(q.y - transform.position.y);
            if (d < bd) { bd = d; best = g; }
        }
        return best;
    }

    void StartMode(Mode m, Move mv)
    {
        mode = m; modeT = 0f; chargeHit = false; chargeRunning = false; whistled = false;
        if (m == Mode.Fetch)
        {
            if (fetchMove == null) fetchMove = new Move { name = "haku", sprites = walkSprites, frameTime = walkFrameTime, releaseFrame = 0, cooldown = 0f };   // releaseFrame: ei huitaisuääntä (kävely)
            mv = fetchMove;
        }
        Begin(mv);
        if (m == Mode.Fetch) CurrentSprite = walkSprites[0];
        if (m == Mode.Charge && chargeWindup != null && chargeWindup.Length > 0) CurrentSprite = chargeWindup[0];
        if (m == Mode.Stun)
        {
            if (!stunApplied) { keepBlock = e.blockChance; keepDmgMul = e.heroDamageMul; keepGuard = e.guardWhenHurt; stunApplied = true; }
            e.blockChance = 0f; e.guardWhenHurt = false; e.heroDamageMul = keepDmgMul * stunDamageMul;
        }
    }

    void EndStun()
    {
        stunApplied = false;
        e.blockChance = keepBlock; e.heroDamageMul = keepDmgMul; e.guardWhenHurt = keepGuard;
        nextAny = Time.time + 0.4f;
    }

    public new void Tick(float dt)
    {
        if (cur == null) { mode = Mode.None; return; }
        modeT += dt;
        switch (mode)
        {
            case Mode.Fetch: TickFetch(dt); return;
            case Mode.Charge: TickCharge(dt); return;
            case Mode.Stun: TickLoop(stunMove, dt); if (Time.time >= stunUntil) { End(); mode = Mode.None; } return;
            case Mode.Whistle:
            {
                // tasainen tahti (ei iskun kiihdytystä): koko liike, vihellys kun pilli on suulla
                lastTick = Time.time; t += dt; e.BrainPhaseSet(Enemy.BrainPhase.Windup);
                int i = Mathf.FloorToInt(t / Mathf.Max(0.02f, whistleMove.frameTime));
                if (i >= whistleMove.sprites.Length) { End(); mode = Mode.None; return; }
                CurrentSprite = whistleMove.sprites[i];
                if (!whistled && i >= Mathf.Min(whistleFrame, whistleMove.sprites.Length - 1)) { whistled = true; Whistle(); }
                return;
            }
            case Mode.Kick:
            {
                base.Tick(dt);
                if (target != null && cur == kickMove && System.Array.IndexOf(kickMove.sprites, CurrentSprite) >= kickMove.hitFrom)
                {
                    if (target.IsIdle && Mathf.Abs(target.transform.position.x - transform.position.x) < 2.2f) { target.KickBy(dirSign); CameraFollow.Shake(0.08f, 0.12f); }
                    target = null;
                }
                if (cur == null) mode = Mode.None;
                return;
            }
            default: base.Tick(dt); return;
        }
    }

    void TickLoop(Move m, float dt)
    {
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        // pökerrys: hidas edestakainen heilunta (nopeasti vaihtuvat osumakuvat näyttivät siltä, että hän lyö itseään naamaan)
        float ft = m == stunMove ? Mathf.Max(0.02f, m.frameTime, 0.34f) : Mathf.Max(0.02f, m.frameTime);
        int n = m.sprites.Length, k = Mathf.FloorToInt(t / ft) % Mathf.Max(1, 2 * n - 2);
        CurrentSprite = m.sprites[k < n ? k : 2 * n - 2 - k];   // edestakaisin
    }

    /// Kävely pullon taakse (heron vastakkaiselle puolelle), sitten potku.
    void TickFetch(float dt)
    {
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (target == null || !target.IsIdle || e.Hero == null || modeT > fetchMaxTime) { End(); mode = Mode.None; target = null; return; }
        targetDir = Mathf.Sign(e.Hero.transform.position.x - target.transform.position.x);
        Vector3 q = target.transform.position, p = transform.position;
        Vector2 goal = new Vector2(q.x - targetDir * 1.15f, q.y);
        Vector2 d = goal - (Vector2)p;
        float step = fetchSpeed * (Raged ? 1.25f : 1f) * dt;
        if (d.magnitude <= step + 0.05f)
        {
            e.BrainMove(d);
            e.FacingRight = targetDir > 0f;
            // potku: suunta heroa kohti (Begin kääntää heroa kohti; pullo on välissä)
            mode = Mode.Kick; modeT = 0f;
            Begin(kickMove);
            return;
        }
        e.BrainMove(d.normalized * step);
        e.FacingRight = d.x > 0f; dirSign = e.FacingRight ? 1f : -1f;
        CurrentSprite = walkSprites[Mathf.FloorToInt(t / walkFrameTime) % walkSprites.Length];
    }

    /// Vauhdinotto, juoksu seinään asti (osuu heroon kerran), törmäys -> pökerrys.
    void TickCharge(float dt)
    {
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        int wn = chargeWindup != null ? chargeWindup.Length : 0;
        float windLen = wn * chargeWindupFrameTime;
        if (t < windLen) { CurrentSprite = chargeWindup[Mathf.Min(wn - 1, Mathf.FloorToInt(t / chargeWindupFrameTime))]; return; }
        if (!chargeRunning) { chargeRunning = true; if (e.Hero != null) { dirSign = Mathf.Sign(e.Hero.transform.position.x - transform.position.x); e.FacingRight = dirSign > 0f; } HitFx.PlayWhoosh(); }
        float rt = t - windLen;
        CurrentSprite = chargeMove.sprites[Mathf.FloorToInt(rt / chargeFrameTime) % chargeMove.sprites.Length];
        float vy = 0f;
        if (e.Hero != null && !chargeHit && (e.Hero.transform.position.x - transform.position.x) * dirSign > -0.5f)   // hakeutuu heron syvyydelle, kunnes ohittaa hänet (väistettävissä hypyllä tai syvyyssuunnassa liikkumalla ajoissa)
            vy = Mathf.Clamp((e.Hero.transform.position.y - transform.position.y) * 3.2f, -3.6f, 3.6f);
        e.BrainMove(new Vector2(dirSign * chargeSpeed * dt, vy * dt));
        var hero = e.Hero;
        if (hero != null && !chargeHit)
        {
            Vector3 hp = hero.transform.position, mp = transform.position;
            if (Mathf.Abs(hp.x - mp.x) < 1.5f && Mathf.Abs(hp.y - mp.y) <= e.depthTolerance + 0.6f && hero.AirHeight < 1.4f)
            {
                chargeHit = true;
                if (!hero.TakeKnockdown(chargeDamage, mp.x, 13f, 6f, e, true)) hero.TakeHit(chargeDamage, mp.x, e, true);
                CameraFollow.Shake(0.15f, 0.25f);
            }
        }
        var a = Area.Current; float x = transform.position.x;
        bool wall = a != null && a.walkMaxX > a.walkMinX && ((dirSign < 0f && x <= a.walkMinX + 0.7f) || (dirSign > 0f && x >= a.walkMaxX - 0.7f));
        if (wall)
        {
            CameraFollow.Shake(0.3f, 0.4f); HitFx.OnBreak(0.15f);
            stunUntil = Time.time + stunTime;
            End(); mode = Mode.None;
            if (HasSprites(stunMove)) StartMode(Mode.Stun, stunMove);
            return;
        }
        if (rt > chargeMaxTime) { End(); mode = Mode.None; }
    }

    void Whistle()
    {
        if (whistleClip != null) e.PlayExtraSound(whistleClip, 1f);
        var a = Area.Current;
        float baseY = a != null ? Mathf.Lerp(a.minDepthY, a.maxDepthY, 0.5f) : transform.position.y;
        int n = 0;
        if (crewTemplates != null && crewTemplates.Length > 0)
            for (int i = 0; i < crewPerWave && crew.Count < maxCrewAlive; i++)
            {
                var tpl = crewTemplates[Random.Range(0, crewTemplates.Length)];
                if (tpl == null) continue;
                var g = Instantiate(tpl, tpl.transform.parent);
                g.name = tpl.name + " (pilli)";
                g.transform.position = new Vector3(crewSpawnX + Random.Range(-crewSpawnSpread, crewSpawnSpread), baseY + Random.Range(-0.4f, 0.4f), 0f);
                g.SetActive(true);
                var en = g.GetComponent<Enemy>();
                if (en != null) { en.IsAwake = true; en.fightsEveryone = false; en.brawlGroup = "Nokkamiehen porukka"; if (e != null) e.brawlGroup = "Nokkamiehen porukka"; crew.Add(en); n++; }   // apurit eivät käy pomon kimppuun (sama ryhmä)
            }
        if (cylinderTemplate != null && a != null)
            for (int i = 0; i < cylindersPerWave; i++)
            {
                var g = Instantiate(cylinderTemplate, cylinderTemplate.transform.parent);
                g.name = "Kaasupullo (apurit)";
                float fx = Mathf.Lerp(a.walkMinX + 1.5f, a.walkMaxX - 1.5f, (i + 0.5f) / cylindersPerWave + Random.Range(-0.1f, 0.1f));
                if (e.Hero != null && Mathf.Abs(fx - e.Hero.transform.position.x) < 2f) fx += 3f * Mathf.Sign(fx - e.Hero.transform.position.x + 0.01f);
                fx = Mathf.Clamp(fx, a.walkMinX + 1f, a.walkMaxX - 1f);
                g.transform.position = new Vector3(fx, Mathf.Lerp(a.minDepthY, a.maxDepthY, Random.Range(0.35f, 0.8f)), 0f);
                g.SetActive(true);
            }
        nextCylinderAt = Mathf.Min(nextCylinderAt, Time.time + 2.5f);
    }
}
