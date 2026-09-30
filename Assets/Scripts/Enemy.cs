using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beat 'em up -vihollinen: lähestyy pelaajaa, asettuu lyöntietäisyydelle ja lyö.
/// Ottaa osumia, kaatuu, nousee ylös ja kuolee. Puuttuvat animaatiot korvataan
/// väliaikaisesti idle-kuvalla (osuma = väläys ja tärinä, kaatuminen = kuvan kääntö).
/// </summary>
public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();
    /// Viimeksi osuman saanut vihollinen (energiapalkkia varten).
    public static Enemy LastHit;
    public static float LastHitTime;

    public string displayName = "Kovis";

    [Header("Spritet (jos tyhjä, käytetään idleä)")]
    public Sprite[] idleSprites;
    public Sprite[] walkSprites;
    public Sprite[] punchSprites;
    public Sprite[] hurtSprites;
    public Sprite[] knockdownSprites;
    public Sprite[] getUpSprites;
    public float idleFrameTime = 0.26f;
    public float walkFrameTime = 0.13f;

    [Header("Viittaukset")]
    public SpriteRenderer body;
    public SpriteRenderer shadow;

    [Header("Herääminen")]
    [Tooltip("Vihollinen seisoo paikallaan, kunnes pelaaja tulee näin lähelle (yksikköä vaakasuunnassa).")]
    public float wakeDistance = 9f;

    [Header("Liike")]
    public float moveSpeedX = 2.2f;
    public float moveSpeedY = 1.4f;

    [Header("Hyökkäys")]
    [Tooltip("Etäisyys, jolta lyönti osuu (yksikköä).")]
    public float attackRange = 1.9f;
    [Tooltip("Kuinka lähellä syvyyssuunnassa pitää olla, jotta lyönti osuu.")]
    public float depthTolerance = 0.35f;
    public float attackCooldown = 1.6f;
    [Tooltip("Lyönnin veto taakse – pelaaja ehtii väistää tämän aikana.")]
    public float windupTime = 0.4f;
    public float punchActiveTime = 0.12f;
    public float punchRecoverTime = 0.4f;
    public int punchDamage = 8;
    [Tooltip("Lyöntianimaatiossa kuva, jossa käsi on täysin ojennettu (0 = ensimmäinen).")]
    public int punchImpactFrame = 3;

    [Header("Kestävyys")]
    public int maxHealth = 60;
    public float hurtTime = 0.35f;
    public float downTime = 1.0f;
    public float getUpTime = 0.5f;

    [Header("Äänet")]
    public AudioClip[] hurtSounds;
    [Range(0f, 1f)] public float hurtVolume = 0.99f;

    [Header("Spriten sijoitus")]
    public float footOffset = 0.08f;

    enum State { Idle, Chase, Windup, Punch, Recover, Hurt, Airborne, Down, GetUp, Dead }
    State state = State.Idle;
    float stateTime, animClock, cooldown;
    int health;
    bool facingRight;
    bool moving;
    float height, verticalVel;
    Vector2 knockVel;
    bool punchLanded;
    float flashTimer;
    float groundHeight;
    AudioSource audioSource;
    int lastSound = -1;

    PlayerController player;
    bool awake;
    int attackRank;   // 0 = lähin, 1 = toinen (vastakkaiselta puolelta), 2+ = odottaa vuoroaan

    public int Health => health;
    public bool IsDead => state == State.Dead;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        health = maxHealth;
        PlayerController.SortByFrameNumber(idleSprites);
        PlayerController.SortByFrameNumber(walkSprites);
        PlayerController.SortByFrameNumber(punchSprites);
        PlayerController.SortByFrameNumber(hurtSprites);
        PlayerController.SortByFrameNumber(knockdownSprites);
        PlayerController.SortByFrameNumber(getUpSprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        cooldown = Random.Range(0.5f, 1.2f);
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        groundHeight = TargetGround();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTime += dt;
        animClock += dt;
        cooldown -= dt;
        if (flashTimer > 0f) flashTimer -= dt;

        UpdateGround(dt);

        switch (state)
        {
            case State.Idle:
                moving = false;
                if (!awake && player != null && Mathf.Abs(player.transform.position.x - transform.position.x) <= wakeDistance)
                    awake = true;
                if (awake && stateTime > 0.3f) Enter(State.Chase);
                break;

            case State.Chase:
                Chase(dt);
                break;

            case State.Windup:
                if (stateTime >= windupTime) { punchLanded = false; Enter(State.Punch); }
                break;

            case State.Punch:
                if (!punchLanded) punchLanded = TryHitPlayer();
                if (stateTime >= punchActiveTime) Enter(State.Recover);
                break;

            case State.Recover:
                if (stateTime >= punchRecoverTime)
                {
                    cooldown = attackCooldown * Random.Range(0.7f, 1.3f);
                    Enter(State.Idle);
                }
                break;

            case State.Hurt:
                Move(knockVel * dt);
                knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 12f * dt);
                if (stateTime >= hurtTime) Enter(State.Chase);
                break;

            case State.Airborne:
                Move(knockVel * dt);
                verticalVel -= 30f * dt;
                height += verticalVel * dt;
                if (height <= 0f)
                {
                    height = 0f;
                    Enter(State.Down);
                    if (CameraFollow.Instance != null) CameraFollow.Shake(0.12f, 0.15f);
                }
                break;

            case State.Down:
                if (stateTime >= downTime)
                {
                    if (health <= 0) Enter(State.Dead);
                    else Enter(State.GetUp);
                }
                break;

            case State.GetUp:
                if (stateTime >= getUpTime) { cooldown = Mathf.Max(cooldown, 0.6f); Enter(State.Chase); }
                break;

            case State.Dead:
                // vilkkuu ja katoaa
                if (body != null) body.enabled = Mathf.FloorToInt(stateTime * 12f) % 2 == 0;
                if (stateTime > 1.2f) Destroy(gameObject);
                break;
        }

        ApplyVisual();
    }

    void Enter(State s)
    {
        state = s;
        stateTime = 0f;
        if (s != State.Chase) moving = false;
    }

    // ---------------- Tekoäly ----------------

    void Chase(float dt)
    {
        if (player == null) { moving = false; return; }
        Vector3 p = player.transform.position;
        Vector3 me = transform.position;
        // pelaaja on toisella alueella (esim. sisällä klubissa): odotetaan paikallaan
        if (Mathf.Abs(p.x - me.x) > 30f) { moving = false; return; }

        // ryhmässä: lähin tulee omalta puoleltaan, toinen vastakkaiselta, muut odottavat vähän kauempana
        float myD = Mathf.Abs(me.x - p.x);
        attackRank = 0;
        Enemy first = null; float firstD = float.MaxValue;
        foreach (var e in All)
        {
            if (e == this || !e.awake || e.IsDead) continue;
            float d = Mathf.Abs(e.transform.position.x - p.x);
            if (d < myD || (Mathf.Approximately(d, myD) && e.GetInstanceID() < GetInstanceID()))
            {
                attackRank++;
                if (d < firstD) { firstD = d; first = e; }
            }
        }
        float side = me.x >= p.x ? 1f : -1f;
        float dist = attackRange * 0.8f;
        float yOff = 0f;
        if (attackRank == 1 && first != null) side = first.transform.position.x >= p.x ? -1f : 1f;
        else if (attackRank >= 2)
        {
            dist = 3.4f + (attackRank - 2) * 0.9f;
            yOff = (attackRank % 2 == 0) ? 0.7f : -0.7f;
        }
        Vector2 target = new Vector2(p.x + side * dist, p.y + yOff);
        Vector2 to = target - (Vector2)me;

        facingRight = p.x > me.x;

        bool inRange = Mathf.Abs(me.x - p.x) <= attackRange && Mathf.Abs(me.y - p.y) <= depthTolerance;
        if (inRange && attackRank <= 1 && cooldown <= 0f && !player.IsDown)
        {
            moving = false;
            Enter(State.Windup);
            return;
        }

        if (to.magnitude > 0.08f)
        {
            moving = true;
            Vector2 dir = to.normalized;
            Move(new Vector2(dir.x * moveSpeedX, dir.y * moveSpeedY) * dt);
        }
        else moving = false;
    }

    bool TryHitPlayer()
    {
        if (player == null) return false;
        Vector3 p = player.transform.position, me = transform.position;
        float dx = p.x - me.x;
        bool front = facingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > attackRange + 0.2f) return false;
        if (Mathf.Abs(p.y - me.y) > depthTolerance) return false;
        if (player.AirHeight > 0.9f) return false;   // hypyllä voi väistää
        return player.TakeHit(punchDamage, me.x);
    }

    // ---------------- Osumat ----------------

    /// Pelaajan isku osuu. attackerX = lyöjän sijainti (mistä suunnasta isku tulee).
    public bool TakeHit(int damage, float attackerX, bool knockdown)
    {
        if (state == State.Down || state == State.GetUp || state == State.Dead) return false;
        if (state == State.Airborne && height > 0.1f && !knockdown) return false;

        awake = true;
        health = Mathf.Max(0, health - damage);
        LastHit = this; LastHitTime = Time.time;
        bool fromLeft = attackerX < transform.position.x;
        facingRight = !fromLeft;   // käänny lyöjään päin
        flashTimer = 0.1f;
        PlayHurtSound();

        if (knockdown || health <= 0)
        {
            knockVel = new Vector2(fromLeft ? 4.5f : -4.5f, 0f);
            verticalVel = 7f;
            height = Mathf.Max(height, 0.05f);
            Enter(State.Airborne);
        }
        else
        {
            knockVel = new Vector2(fromLeft ? 2.2f : -2.2f, 0f);
            Enter(State.Hurt);
        }
        return true;
    }

    void PlayHurtSound()
    {
        if (hurtSounds == null || hurtSounds.Length == 0 || audioSource == null) return;
        int i = Random.Range(0, hurtSounds.Length);
        if (hurtSounds.Length > 1 && i == lastSound) i = (i + 1) % hurtSounds.Length;
        lastSound = i;
        if (hurtSounds[i] == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(hurtSounds[i], hurtVolume);
    }

    // ---------------- Liike ja maa ----------------

    void Move(Vector2 delta)
    {
        Vector3 p = transform.position;
        float minY = player != null ? player.minDepthY : -4.3f;
        float maxY = player != null ? player.maxDepthY : -0.8f;
        p.x += delta.x;
        p.y = Mathf.Clamp(p.y + delta.y, minY, maxY);
        transform.position = p;
    }

    float TargetGround()
    {
        if (player == null || !player.useSidewalk) return 0f;
        return transform.position.y > player.curbDepthY ? player.sidewalkHeight : 0f;
    }

    void UpdateGround(float dt)
    {
        float target = TargetGround();
        if (Mathf.Approximately(target, groundHeight)) return;
        if (state == State.Airborne) { height -= target - groundHeight; groundHeight = target; }
        else groundHeight = Mathf.MoveTowards(groundHeight, target, 6f * dt);
    }

    // ---------------- Grafiikka ----------------

    void ApplyVisual()
    {
        if (body == null) return;
        float rot = 0f;
        body.sprite = CurrentSprite(ref rot);
        body.flipX = !facingRight;

        Vector2 pivotFix = Vector2.zero;
        Sprite spr = body.sprite;
        if (spr != null)
        {
            float ppu = spr.pixelsPerUnit;
            pivotFix.x = (spr.pivot.x - spr.rect.width * 0.5f) / ppu;
            pivotFix.y = spr.pivot.y / ppu;
            if (body.flipX) pivotFix.x = -pivotFix.x;
        }
        float shake = (state == State.Hurt && stateTime < 0.15f) ? Mathf.Sin(stateTime * 90f) * 0.05f : 0f;
        body.transform.localPosition = new Vector3(pivotFix.x + shake, groundHeight + height - footOffset + pivotFix.y, 0f);
        // kaatumisen väliaikainen korvike: käännetään kuvaa, kun oikeat kuvat puuttuvat
        body.transform.localRotation = Quaternion.Euler(0f, 0f, facingRight ? rot : -rot);

        body.color = flashTimer > 0f ? new Color(1f, 0.55f, 0.55f) : Color.white;

        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        body.sortingOrder = order;
        if (shadow != null)
        {
            shadow.sortingOrder = order - 1;
            shadow.transform.localPosition = new Vector3(0f, groundHeight, 0f);
            float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 2.5f));
            shadow.transform.localScale = new Vector3(1.6f * s, 0.48f * s, 1f);
            shadow.enabled = state != State.Dead || body.enabled;
        }
    }

    Sprite CurrentSprite(ref float rot)
    {
        switch (state)
        {
            case State.Chase:
                if (moving && Has(walkSprites)) return walkSprites[(int)(animClock / walkFrameTime) % walkSprites.Length];
                return IdleFrame();

            case State.Windup:
                if (Has(punchSprites))
                {
                    // veto taakse: kuvat ennen iskun liikettä (esim. 0 ja 1), jaettuna vetoajalle
                    int wind = Mathf.Clamp(PunchImpact - 1, 1, punchSprites.Length);
                    return punchSprites[Mathf.Min((int)(stateTime / windupTime * wind), wind - 1)];
                }
                return IdleFrame();

            case State.Punch:
                if (Has(punchSprites))
                {
                    // lyhyt välikuva ja sitten täysin ojennettu käsi
                    int imp = PunchImpact;
                    return punchSprites[stateTime < 0.04f && imp > 0 ? imp - 1 : imp];
                }
                return IdleFrame();

            case State.Recover:
                if (Has(punchSprites))
                {
                    // käsi pysyy ojennettuna hetken, sitten palautuskuvat
                    int imp = PunchImpact;
                    int n = punchSprites.Length - imp;
                    return punchSprites[imp + Mathf.Min((int)(stateTime / punchRecoverTime * n), n - 1)];
                }
                return IdleFrame();

            case State.Hurt:
                if (Has(hurtSprites)) return hurtSprites[Mathf.Min((int)(stateTime / hurtTime * hurtSprites.Length), hurtSprites.Length - 1)];
                return FirstIdle();

            case State.Airborne:
                if (Has(knockdownSprites))
                {
                    int airFrames = Mathf.Max(1, knockdownSprites.Length - 1);
                    return knockdownSprites[Mathf.Min((int)(stateTime / 0.1f), airFrames - 1)];
                }
                rot = Mathf.Lerp(20f, 80f, Mathf.Clamp01(stateTime / 0.35f));
                return FirstIdle();

            case State.Down:
            case State.Dead:
                if (Has(knockdownSprites)) return knockdownSprites[knockdownSprites.Length - 1];
                rot = 90f;
                return FirstIdle();

            case State.GetUp:
                if (Has(getUpSprites)) return getUpSprites[Mathf.Min((int)(stateTime / getUpTime * getUpSprites.Length), getUpSprites.Length - 1)];
                rot = Mathf.Lerp(90f, 0f, Mathf.Clamp01(stateTime / getUpTime));
                return FirstIdle();

            default:
                return IdleFrame();
        }
    }

    int PunchImpact => Mathf.Clamp(punchImpactFrame, 0, punchSprites.Length - 1);

    static bool Has(Sprite[] s) => s != null && s.Length > 0;

    Sprite FirstIdle() => Has(idleSprites) ? idleSprites[0] : null;

    Sprite IdleFrame()
    {
        if (!Has(idleSprites)) return null;
        int n = idleSprites.Length;
        if (n == 1) return idleSprites[0];
        int period = 2 * n - 2;
        int i = (int)(animClock / idleFrameTime) % period;
        return idleSprites[i < n ? i : period - i];
    }
}
