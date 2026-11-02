using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Koe-olento (alien-pomo): osittainen aivo Enemyn päällä. Tavallinen tekoäly (fightsEveryone) juoksee kohti lähintä kohdetta (hero tai toinen vihu) ja lyö sitä;
/// tämä lisää erikoisliikkeet: pusku (paikalta ja juoksusta), loikka (laskeutuminen tallaa maahan), häntähuitaisu, raapiminen ja karjaisu.
/// Kaikki liikkeet osuvat kaikkiin edessä/ympärillä oleviin (heroihin ja muihin vihuihin, ei omaan lajiin). Alle 50 % elämistä liikkuu 25 % nopeammin.
/// Liikkeen kuvat ovat valmiiksi piirretty (loikan korkeus mukana): aivo liikuttaa vain vaakasuunnassa (ja syvyyssuunnassa kohdetta kohti).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class AlienBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    [System.Serializable]
    public class Move
    {
        public string name;
        public Sprite[] sprites;
        public float frameTime = 0.05f;
        [Tooltip("Kuvakohtaiset kestot (s); tyhjä = frameTime. Karjaisu noudattaa äänen ajoitusta.")] public float[] frameTimes;
        [Tooltip("Osumaikkunat: from0,to0,from1,to1,... (kuvaindeksit mukaan lukien). Jokaiseen ikkunaan osuu kukin kohde kerran.")] public int[] windows;
        [Tooltip("Vahinko ikkunoittain; tyhjä = damage.")] public int[] windowDamage;
        public int damage = 15;
        [Tooltip("Tästä ikkunasta (0 = ensimmäinen) alkaen isku kaataa.")] public int knockFromWindow = 999;
        public float reach = 3f, minRange = 0f, maxRange = 3.5f, weight = 1f, cooldown = 3f;
        [Tooltip("Osuu myös taakse (häntähuitaisu, tallaus, karjaisu).")] public bool allAround;
        [Tooltip("Liike ei katkea tavallisiin iskuihin (vain kaataviin).")] public bool armor;
        [Tooltip("Eteenliike kuvaindeksien välillä: nopeus yksikköä/s. homing: nopeus mitoitetaan niin, että alien päätyy kohteen kohdalle liikkeen lopussa.")]
        public int moveFrom = -1, moveTo = -1; public float moveSpeed = 8f; public bool homing;
        [Tooltip("Liike pysähtyy ensimmäiseen osumaan (pusku).")] public bool stopOnHit;
        [Tooltip("Kameran tärähdys tästä kuvaindeksistä (-1 = ei).")] public int shakeFrame = -1;
        [Tooltip("Ääni, joka soitetaan tästä kuvaindeksistä (-1 = ei).")] public AudioClip sound; public int soundFrame = -1;
        [Header("Puraisu (tarttuu hampailla, ravistaa ja heittää): grabFrame = kuva, jossa leuat sulkeutuvat (-1 = ei tartuntaa); ote kestää kuvaan releaseFrame, jolloin heitto")]
        public int grabFrame = -1, releaseFrame = -1;
        public float mouthDx = 1.7f, mouthLift = 1.0f, tickEvery = 0.3f, throwSpeed = 12f, throwUp = 6f;
        public int tickDamage = 4, throwDamage = 22;
        [System.NonSerialized] public float nextTime;
        [System.NonSerialized] public float[] cum;
    }

    public Move[] moves;
    [Tooltip("Karjaisu: sisääntulo, sitten 50 % ja 25 % elämistä.")] public Move roar;
    public float globalCooldown = 0.5f, chancePerSec = 3.5f;
    [Range(0f, 1f)] public float rageFraction = 0.5f;
    public float rageSpeed = 1.25f;

    Enemy e; Move cur; float t, lastTick, nextAny = 0.4f, hv, targetY; float dirSign = 1f;
    bool hitAny, soundDone, shaken, introDone, grabTried; int phase; float nextTick;
    PlayerController heldHero; Enemy heldFoe;
    readonly HashSet<long> hitDone = new HashSet<long>();
    readonly List<Enemy> tmp = new List<Enemy>();

    public bool Active => cur != null;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => cur != null && CurrentSprite != null;
    public bool ArmorNow => cur != null && cur.armor;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;
    float Speed => e != null && e.HealthFraction <= rageFraction ? rageSpeed : 1f;

    void Awake() { e = GetComponent<Enemy>(); nextAny = Random.Range(0.3f, 0.8f); }

    void OnDisable() { ReleaseVictim(false); cur = null; CurrentSprite = null; }

    static void Build(Move m)
    {
        if (m.cum != null && m.cum.Length == m.sprites.Length + 1) return;
        var c = new float[m.sprites.Length + 1];
        for (int k = 0; k < m.sprites.Length; k++)
            c[k + 1] = c[k] + (m.frameTimes != null && k < m.frameTimes.Length && m.frameTimes[k] > 0f ? m.frameTimes[k] : m.frameTime);
        m.cum = c;
    }

    static int IndexAt(Move m, float tt)
    {
        var c = m.cum;
        if (tt >= c[c.Length - 1]) return m.sprites.Length;
        int i = 0;
        while (i + 1 < c.Length - 1 && c[i + 1] <= tt) i++;
        return i;
    }

    /// <summary>Kohde: tavallisen tekoälyn valitsema vihu (fightsEveryone) tai muuten lähin hero.</summary>
    bool PickTarget(out Vector3 pos)
    {
        var foe = e.TargetEnemy;
        if (foe != null && foe.isActiveAndEnabled && !foe.IsDownOrDead) { pos = foe.transform.position; return true; }
        var h = e.Hero;
        if (h != null) { pos = h.transform.position; return true; }
        pos = Vector3.zero; return false;
    }

    void Update()
    {
        if (cur != null && Time.time - lastTick > 0.25f) { End(); return; }   // väliin tuli kaatuminen tms.
        if (cur != null || e == null || !e.IsAwake || e.IsDead || !e.BrainIdleOrChase || moves == null) return;
        if (!PickTarget(out Vector3 tp)) return;
        Vector3 mp = transform.position;
        float dx = Mathf.Abs(tp.x - mp.x), dy = Mathf.Abs(tp.y - mp.y);
        if (roar != null && roar.sprites != null && roar.sprites.Length > 0)
        {
            float hf = e.HealthFraction;
            bool intro = !introDone && dx < 16f, p2 = phase < 1 && hf <= 0.5f, p3 = phase < 2 && hf <= 0.25f;
            if (intro || p2 || p3)
            {
                if (intro) introDone = true;
                if (p2) phase = 1;
                if (p3) phase = 2;
                Begin(roar, tp);
                return;
            }
        }
        if (Time.time < nextAny || dy > e.depthTolerance + 0.8f) return;
        if (Random.value > chancePerSec * Time.deltaTime) return;
        float total = 0f;
        foreach (var m in moves) if (Eligible(m, dx)) total += m.weight;
        if (total <= 0f) return;
        float r = Random.value * total;
        foreach (var m in moves)
        {
            if (!Eligible(m, dx)) continue;
            r -= m.weight;
            if (r <= 0f) { Begin(m, tp); return; }
        }
    }

    bool Eligible(Move m, float dx) => m != null && m.sprites != null && m.sprites.Length > 0 && Time.time >= m.nextTime && dx >= m.minRange && dx <= m.maxRange;

    void Begin(Move m, Vector3 tp)
    {
        Build(m);
        cur = m; t = 0f; lastTick = Time.time; hitAny = false; soundDone = false; shaken = false; grabTried = false; hitDone.Clear();
        Vector3 mp = transform.position;
        e.FacingRight = tp.x > mp.x;
        dirSign = e.FacingRight ? 1f : -1f;
        float sp = Speed;
        m.nextTime = Time.time + m.cooldown * Random.Range(0.7f, 1.1f) / sp;
        nextAny = Time.time + (globalCooldown * Random.Range(0.6f, 1.2f) + m.cum[m.sprites.Length]) / sp;
        hv = m.moveSpeed;
        if (m.homing && m.moveFrom >= 0)
        {
            int to = Mathf.Min(m.moveTo + 1, m.sprites.Length);
            float dur = Mathf.Max(0.1f, m.cum[to] - m.cum[Mathf.Clamp(m.moveFrom, 0, m.sprites.Length)]);
            float dist = Mathf.Max(0f, (tp.x - mp.x) * dirSign - 0.8f);
            hv = Mathf.Clamp(dist * sp / dur, 2f, m.moveSpeed * 1.5f);
        }
        CurrentSprite = m.sprites[0];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (m != roar) HitFx.PlayWhoosh();
    }

    void End()
    {
        ReleaseVictim(false);
        cur = null; CurrentSprite = null;
        if (e != null && !e.IsDead) e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    public void Tick(float dt)
    {
        if (cur == null) return;
        lastTick = Time.time; t += dt * Speed;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        int n = cur.sprites.Length;
        int i = IndexAt(cur, t);
        if (i >= n) { End(); return; }
        CurrentSprite = cur.sprites[i];
        if (!soundDone && cur.sound != null && cur.soundFrame >= 0 && i >= cur.soundFrame)
        {
            soundDone = true;
            if (e.Audio != null) { e.Audio.pitch = 1f; e.Audio.PlayOneShotLeveled(cur.sound, 1f); }
        }
        if (!shaken && cur.shakeFrame >= 0 && i >= cur.shakeFrame)
        {
            shaken = true;
            if (CameraFollow.Instance != null) CameraFollow.Shake(0.25f, 0.25f);
        }
        if (cur.moveFrom >= 0 && i >= cur.moveFrom && i <= cur.moveTo && !(cur.stopOnHit && hitAny))
        {
            float vy = 0f;
            if (PickTarget(out Vector3 tp)) vy = Mathf.Clamp((tp.y - transform.position.y) * 4f, -3.5f, 3.5f);
            e.BrainMove(new Vector2(dirSign * hv * dt, vy * dt));
        }
        if (cur.grabFrame >= 0)
        {
            if (!grabTried && i >= cur.grabFrame)
            {
                grabTried = true;
                if (!TryGrab())
                {
                    // ohi: hypätään ravistuksen yli loppuliikkeeseen
                    t = cur.cum[Mathf.Clamp(cur.releaseFrame + 1, 0, n)];
                    i = IndexAt(cur, t);
                    if (i >= n) { End(); return; }
                    CurrentSprite = cur.sprites[i];
                }
            }
            if (heldHero != null || heldFoe != null)
            {
                if (i >= cur.releaseFrame) ReleaseVictim(true);
                else HoldVictim();
            }
        }
        if (cur.windows == null) return;
        for (int w = 0; w + 1 < cur.windows.Length; w += 2)
            if (i >= cur.windows[w] && i <= cur.windows[w + 1]) Strike(w / 2);
    }

    /// Puraisu: lähin otettavissa oleva (hero tai vihu) suun edestä; ote alkaa, kun leuat sulkeutuvat.
    bool TryGrab()
    {
        Vector3 mp = transform.position;
        PlayerController bestHero = null; Enemy bestFoe = null; float best = float.MaxValue;
        foreach (var h in Players.All)
        {
            if (h == null || !h.isActiveAndEnabled || h.AirHeight > 0.3f) continue;
            Vector3 hp = h.transform.position; float ahead = (hp.x - mp.x) * dirSign;
            if (ahead < -0.5f || ahead > cur.reach || Mathf.Abs(hp.y - mp.y) > e.depthTolerance + 0.3f || ahead >= best) continue;
            best = ahead; bestHero = h; bestFoe = null;
        }
        tmp.Clear(); tmp.AddRange(Enemy.All);
        foreach (var f in tmp)
        {
            if (f == null || f == e || !f.isActiveAndEnabled || f.IsDownOrDead || f.displayName == e.displayName || !f.CanBeGrabbed) continue;
            Vector3 fp = f.transform.position; float ahead = (fp.x - mp.x) * dirSign;
            if (ahead < -0.5f || ahead > cur.reach || Mathf.Abs(fp.y - mp.y) > e.depthTolerance + 0.3f || ahead >= best) continue;
            best = ahead; bestFoe = f; bestHero = null;
        }
        if (bestHero != null && bestHero.BeginGrab(mp.x)) { heldHero = bestHero; nextTick = Time.time + cur.tickEvery; return true; }
        if (bestFoe != null) { bestFoe.BeginHeldByPlayer(mp.x); heldFoe = bestFoe; return true; }
        return false;
    }

    /// Uhri roikkuu suussa: paikka suun kohdalla, ravistus sivuttain ja pieni pomppu; heroon osuu vahinkoa tasaisin välein.
    void HoldVictim()
    {
        Vector3 mp = transform.position;
        float sh = Mathf.Sin(t * 26f);
        var pos = new Vector3(mp.x + dirSign * (cur.mouthDx + sh * 0.45f), mp.y - 0.02f, 0f);
        float lift = cur.mouthLift + Mathf.Abs(Mathf.Sin(t * 13f)) * 0.3f;
        float rot = dirSign * (70f + sh * 25f);
        if (heldHero != null)
        {
            if (!heldHero.IsGrabbed) { heldHero = null; return; }   // irti (kuoli tms.)
            bool art = heldHero.HasThrowSprites;
            heldHero.SetHeld(pos, lift, art ? 0f : rot, art ? 1 : 0);
            if (Time.time >= nextTick)
            {
                nextTick = Time.time + cur.tickEvery;
                heldHero.HitWhileHeld(cur.tickDamage);
                if (!heldHero.IsGrabbed) heldHero = null;
            }
        }
        else if (heldFoe != null) heldFoe.SetHeldByPlayer(pos, lift, rot, -1);
    }

    /// Heitto (throwIt) tai pelkkä irtipäästö (liike keskeytyi): heittona lentävä uhri kaataa muita reitillään.
    void ReleaseVictim(bool throwIt)
    {
        if (heldHero != null)
        {
            if (heldHero.IsGrabbed)
            {
                if (throwIt && cur != null) heldHero.Throw(dirSign * cur.throwSpeed, cur.throwUp, cur.throwDamage, false);
                else heldHero.ReleaseGrab();
            }
            heldHero = null;
        }
        if (heldFoe != null)
        {
            if (throwIt && cur != null) { heldFoe.thrownByEnemy = true; heldFoe.thrownBy = e; heldFoe.ReleaseThrow(dirSign * cur.throwSpeed, cur.throwUp, cur.throwDamage); }
            else heldFoe.ReleaseThrow(0f, 2f, 0);
            heldFoe = null;
        }
        if (throwIt && e != null) HitFx.PlayWhoosh();
    }

    static long Key(int window, int id) => (long)window * 4294967296L + (long)(uint)id;

    /// <summary>Isku kaikkiin ikkunan alueella oleviin: heroihin ja muihin vihuihin (ei omaan lajiin).</summary>
    void Strike(int w)
    {
        Vector3 mp = transform.position;
        int dmg = cur.windowDamage != null && w < cur.windowDamage.Length ? cur.windowDamage[w] : cur.damage;
        bool kd = w >= cur.knockFromWindow;
        bool isRoar = cur == roar;
        foreach (var h in Players.All)
        {
            if (h == null || !h.isActiveAndEnabled) continue;
            if (!isRoar && h.AirHeight > 1.4f) continue;   // hypyn alta ei osu
            Vector3 hp = h.transform.position;
            if (!InZone(mp, hp)) continue;
            long k = Key(w, h.GetInstanceID());
            if (!hitDone.Add(k)) continue;
            bool ok;
            if (kd) ok = h.TakeKnockdown(dmg, mp.x, 9f, 5f, e, true) || h.TakeHit(dmg, mp.x, e, true);
            else ok = h.TakeHit(dmg, mp.x, e, false);
            if (ok) hitAny = true;
        }
        tmp.Clear(); tmp.AddRange(Enemy.All);
        foreach (var f in tmp)
        {
            if (f == null || f == e || !f.isActiveAndEnabled || f.IsDownOrDead || f.displayName == e.displayName) continue;
            if (!InZone(mp, f.transform.position)) continue;
            long k = Key(w, f.GetInstanceID());
            if (!hitDone.Add(k)) continue;
            if (Enemy.EnemyHits(f, dmg, mp.x, kd)) hitAny = true;
        }
    }

    bool InZone(Vector3 mp, Vector3 p)
    {
        float ahead = (p.x - mp.x) * dirSign;
        bool inX = cur.allAround ? Mathf.Abs(p.x - mp.x) <= cur.reach : (ahead > -0.5f && ahead <= cur.reach);
        return inX && Mathf.Abs(p.y - mp.y) <= e.depthTolerance + 0.3f;
    }
}
