using UnityEngine;

/// <summary>
/// Lippis: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa liikkeen ja iskut (jab, takasuora, potku, kombo JSP);
/// tämä ottaa vain väistön: kun heroa osuu, Lippis väistää satunnaisesti (isku menee ohi) ja vastaa heti takasuoralla (counter).
/// Kuvat: Resources/Lippis/lippis_takasuora (4 kuvaa) ja lippis_vaisto (4 kuvaa), 512 x 384, ppu 100. Lisätään Enemylle ajon aikana (Enemy.Awake).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class LippisBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    [Range(0f, 1f)] public float dodgeChance = 0.35f;
    public float dodgeCooldown = 2f, dodgeRange = 3f;
    public float dodgeFrameTime = 0.05f, counterFrameTime = 0.045f;
    public int counterImpactFrame = 2, counterDamage = 14;
    public float counterReach = 2.3f, counterKnockSpeed = 6f, counterKnockUp = 5f;

    static Sprite[] dodgeShared, crossShared, counterShared; static bool tried;

    /// Takasuora: vartio, vartio, veto, ojennettu; jatko (pito) ja paluu. Osuma indeksissä 6.
    public static Sprite[] Cross { get { Load(); return crossShared; } }
    static Sprite[] DodgeSeq { get { Load(); return dodgeShared; } }
    static Sprite[] CounterSeq { get { Load(); return counterShared; } }

    static Sprite[] Slice(string name)
    {
        var tx = Resources.Load<Texture2D>("Lippis/" + name);
        if (tx == null || tx.width < 2048 || tx.height < 384) return null;
        var f = new Sprite[4];
        for (int i = 0; i < 4; i++) { f[i] = Sprite.Create(tx, new Rect(i * 512, 0, 512, 384), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect); f[i].name = name + "_" + i; }
        return f;
    }

    static void Load()
    {
        if (tried) return;
        tried = true;
        var c = Slice("lippis_takasuora");
        if (c != null)
        {
            // veto kiihtyy (0 pitkään, 1 lyhyemmin), osuma ojennetussa (3), pito, paluu
            crossShared = new[] { c[0], c[0], c[0], c[1], c[1], c[2], c[3], c[3], c[3], c[2], c[1] };
            counterShared = new[] { c[1], c[2], c[3], c[3], c[3], c[2], c[1] };   // vastaisku: heti veto, osuma kuvassa 2
        }
        var d = Slice("lippis_vaisto");
        if (d != null) dodgeShared = new[] { d[0], d[1], d[2], d[3], d[3], d[2], d[1] };   // kyykistyy ja nousee
    }

    Enemy e;
    enum Phase { None, Dodge, Counter }
    Phase phase;
    float t, nextDodge, lastTick;
    bool counterHit;

    public bool Active => phase != Phase.None;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => Active && CurrentSprite != null;
    public bool ArmorNow => false;

    void Awake() { e = GetComponent<Enemy>(); }

    void Update()
    {
        if (phase != Phase.None && Time.time - lastTick > 0.25f) { phase = Phase.None; }   // väliin tuli osuma, kaatuminen tms.
    }

    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        var dodge = DodgeSeq; var counter = CounterSeq;
        if (phase == Phase.Dodge) return t < dodge.Length * dodgeFrameTime * 0.75f;   // väistön aikana iskut menevät ohi
        if (phase != Phase.None || e == null || e.ally || e.Hero == null || dodge == null || counter == null) return false;
        if (!e.BrainNeutral || Time.time < nextDodge) return false;
        if (Mathf.Abs(e.Hero.transform.position.x - transform.position.x) > dodgeRange) return false;
        if (Random.value >= dodgeChance) return false;
        nextDodge = Time.time + dodgeCooldown;
        phase = Phase.Dodge; t = 0f; counterHit = false; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        CurrentSprite = dodge[0];
        return true;
    }

    public void Tick(float dt)
    {
        if (phase == Phase.None) return;
        var hero = e.Hero;
        if (hero == null) { phase = Phase.None; return; }
        lastTick = Time.time;
        t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        var dodge = DodgeSeq; var counter = CounterSeq;
        float face = e.FacingRight ? 1f : -1f;
        if (phase == Phase.Dodge)
        {
            int i = Mathf.Min((int)(t / dodgeFrameTime), dodge.Length - 1);
            CurrentSprite = dodge[i];
            if (t < 0.15f) e.BrainMove(new Vector2(-face * 1.2f * dt, 0f));   // hieman taaksepäin
            if (t >= dodge.Length * dodgeFrameTime * 0.8f)   // nousun loppuvaihe jää pois: vastaisku alkaa heti
            {
                phase = Phase.Counter; t = 0f;
                e.FacingRight = hero.transform.position.x > transform.position.x;
            }
            return;
        }
        int k = Mathf.Min(AttackEase.Frame(t, counterFrameTime, counterImpactFrame), counter.Length - 1);   // vastaiskun veto kiihtyy
        CurrentSprite = counter[k];
        if (!counterHit && k >= counterImpactFrame)
        {
            counterHit = true;
            Vector3 me = transform.position, hp = hero.transform.position;
            float dx = hp.x - me.x;
            bool front = e.FacingRight ? dx >= -0.2f : dx <= 0.2f;
            if (front && Mathf.Abs(dx) <= counterReach && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.1f && hero.AirHeight <= 0.9f)
                if (!hero.TakeKnockdown(counterDamage, me.x, counterKnockSpeed, counterKnockUp, e)) hero.TakeHit(counterDamage, me.x, e, true);
        }
        if (t >= counter.Length * counterFrameTime) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }
}
