using UnityEngine;

/// <summary>
/// Mongolisoturi: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa kävelyn, juoksun, suorat (J, S), etupotkun (K), torjunnan ja kaatumisen;
/// tämä lisää erikoisliikkeet: vaakapotku, pyöräytyspotku, hyppypotku (loikka heroa kohti) ja kivenheitto (lentävä kivi, väistö hyppäämällä).
/// Jokaisella liikkeellä on etäisyysalue, paino, jäähtyminen ja osumaikkuna (kuvaindeksit).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class MongolBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    [System.Serializable]
    public class Move
    {
        public string name;
        public Sprite[] sprites;
        public float frameTime = 0.05f;
        [Tooltip("Osumaikkuna: kuvaindeksit (mukaan lukien).")] public int hitFrom = 3, hitTo = 4;
        public float reach = 2.4f, minRange = 1.5f, maxRange = 3.5f, weight = 1f, cooldown = 3f;
        public int damage = 20; public bool knockdown;
        [Tooltip("Eteenliike kuvaindeksien välillä (hyppypotku): nopeus yksikköä/s; -1 = ei liikettä.")] public int moveFrom = -1, moveTo = -1; public float moveSpeed = 8f;
        [Tooltip("Kivenheitto: kuvaindeksi, jossa kivi irtoaa kädestä (-1 = ei kiveä).")] public int releaseFrame = -1;
        [Tooltip("Liikkeen alkaessa soitettava ääni (esim. liukutaklauksen raapaisu).")] public AudioClip sound, hitSound; public float soundVolume = 1f;
        [HideInInspector] public float nextTime;
    }

    public Move[] moves;
    public Sprite rockSprite;
    public float globalCooldown = 1.6f, chancePerSec = 0.9f, rockSpeed = 12f; public int rockDamage = 18;
    public float rockHeight = 1.45f;

    Enemy e; Move cur; float t, lastTick, nextAny = 2f; bool hitDone, hitSoundDone, released; float dirSign = 1f;

    public bool Active => cur != null;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => cur != null && CurrentSprite != null;
    public bool ArmorNow => false;
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    void Awake() { e = GetComponent<Enemy>(); nextAny = Random.Range(1.5f, 4f); }

    void OnDisable() { cur = null; CurrentSprite = null; }

    void Update()
    {
        if (cur != null && Time.time - lastTick > 0.25f) { End(); return; }   // väliin tuli kaatuminen tms.
        if (cur != null || e == null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead || moves == null || Time.time < nextAny) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        if (dy > e.depthTolerance + 0.8f) return;
        if (Random.value > chancePerSec * Time.deltaTime) return;
        float total = 0f;
        foreach (var m in moves) if (Eligible(m, dx)) total += m.weight;
        if (total <= 0f) return;
        float r = Random.value * total;
        foreach (var m in moves)
        {
            if (!Eligible(m, dx)) continue;
            r -= m.weight;
            if (r <= 0f) { Begin(m); return; }
        }
    }

    bool Eligible(Move m, float dx) => m != null && m.sprites != null && m.sprites.Length > 0 && Time.time >= m.nextTime && dx >= m.minRange && dx <= m.maxRange && (m.releaseFrame < 0 || rockSprite != null);

    void Begin(Move m)
    {
        cur = m; t = 0f; hitDone = false; hitSoundDone = false; released = false; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        dirSign = e.FacingRight ? 1f : -1f;
        m.nextTime = Time.time + m.cooldown * Random.Range(0.8f, 1.3f);
        nextAny = Time.time + globalCooldown * Random.Range(0.8f, 1.4f) + m.sprites.Length * m.frameTime;
        CurrentSprite = m.sprites[0];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (m.sound != null) e.PlayExtraSound(m.sound, m.soundVolume); else if (m.releaseFrame < 0) HitFx.PlayWhoosh();
    }

    void End()
    {
        cur = null; CurrentSprite = null;
        if (e != null && !e.IsDead) e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    public void Tick(float dt)
    {
        if (cur == null) return;
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        int i = (int)(t / cur.frameTime);
        if (i >= cur.sprites.Length) { End(); return; }
        CurrentSprite = cur.sprites[i];
        var hero = e.Hero;
        if (cur.moveFrom >= 0 && i >= cur.moveFrom && i <= cur.moveTo) e.BrainMove(new Vector2(dirSign * cur.moveSpeed * dt, 0f));
        if (cur.releaseFrame >= 0)
        {
            if (!released && i >= cur.releaseFrame)
            {
                released = true;
                Vector3 p = transform.position + new Vector3(dirSign * 0.9f, 0f, 0f);
                MongolRock.Throw(e, rockSprite, p, rockHeight, dirSign, rockSpeed, rockDamage);
            }
            return;
        }
        if (cur.hitSound != null && !hitSoundDone && i >= cur.hitFrom) { hitSoundDone = true; e.PlayExtraSound(cur.hitSound, cur.soundVolume); }
        if (hero == null || hitDone || i < cur.hitFrom || i > cur.hitTo) return;
        Vector3 hp = hero.transform.position, mp = transform.position;
        float ahead = (hp.x - mp.x) * dirSign;
        bool near = ahead > -0.5f && ahead <= cur.reach && Mathf.Abs(hp.y - mp.y) <= e.depthTolerance + 0.3f;
        if (near && cur.moveFrom >= 0 && hero.AirHeight > 1.4f) near = false;   // hyppypotkun ali
        if (!near) return;
        hitDone = true;
        if (cur.knockdown) { if (!hero.TakeKnockdown(cur.damage, mp.x, 9f, 5f, e, true)) hero.TakeHit(cur.damage, mp.x, e, true); }
        else hero.TakeHit(cur.damage, mp.x, e, false);
    }
}
