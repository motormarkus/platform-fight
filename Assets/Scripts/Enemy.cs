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

    [Header("Toinen hyökkäys (esim. pusku) – vapaaehtoinen")]
    public Sprite[] altAttackSprites;
    [Tooltip("Kuva, jossa isku osuu (0 = ensimmäinen).")]
    public int altImpactFrame = 4;
    public int altDamage = 12;
    [Tooltip("Kuinka usein toista hyökkäystä käytetään (0–1).")]
    [Range(0f, 1f)] public float altChance = 0.35f;
    [Tooltip("Veto kestää tämän verran pidempään (pelaaja ehtii väistää).")]
    public float altExtraWindup = 0.15f;
    [Tooltip("Ulottuvuus (pusku syöksyy pidemmälle kuin lyönti).")]
    public float altReach = 2.3f;

    [Header("Heitto (vapaaehtoinen): tarttuu, nostaa pään yli ja heittää taakse")]
    [Tooltip("8 kuvaa: 0 kurotus, 1 ote, 2–4 nosto, 5–6 heitto, 7 asento heiton jälkeen (katsoo heittosuuntaan).")]
    public Sprite[] grabSprites;
    [Tooltip("Kuinka usein hyökkäys on heitto (0–1).")]
    [Range(0f, 1f)] public float grabChance = 0.3f;
    [Tooltip("Kuinka läheltä tarttuminen onnistuu.")]
    public float grabRange = 1.3f;
    public float grabReachTime = 0.3f;
    public float grabLiftTime = 0.8f;
    public int throwDamage = 18;

    [Header("Kestävyys")]
    public int maxHealth = 60;
    public float hurtTime = 0.35f;
    public float downTime = 1.0f;
    public float getUpTime = 0.5f;

    [Header("Pudotus (kun kaatuu lopullisesti)")]
    [Tooltip("Tavallinen pudotus: yksi seteli.")]
    public int noteValue = 1;
    [Tooltip("Harvinainen pudotus: setelitukku.")]
    public int stackValue = 50;
    [Tooltip("Tukun todennäköisyys (0.04 = 1/25).")]
    [Range(0f, 1f)] public float stackChance = 0.04f;

    [Header("Äänet")]
    public AudioClip[] hurtSounds;
    [Range(0f, 1f)] public float hurtVolume = 0.99f;

    [Header("Spriten sijoitus")]
    public float footOffset = 0.08f;

    enum State { Idle, Chase, Windup, Punch, Recover, Hurt, Airborne, Down, GetUp, Dead, GrabReach, GrabLift, GrabThrow }
    bool grabIntent;   // seuraava hyökkäys on heittoyritys
    bool attackRolled; // onko seuraavan hyökkäyksen tyyppi jo arvottu
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
        PlayerController.SortByFrameNumber(altAttackSprites);
        PlayerController.SortByFrameNumber(grabSprites);
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
                if (stateTime >= CurrentWindup) { punchLanded = false; Enter(State.Punch); }
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
                    if (health <= 0) { Enter(State.Dead); bool rare = Random.value < stackChance; Pickup.SpawnMoney(transform.position, rare ? stackValue : noteValue, rare); }
                    else Enter(State.GetUp);
                }
                break;

            case State.GetUp:
                if (stateTime >= getUpTime) { cooldown = Mathf.Max(cooldown, 0.6f); Enter(State.Chase); }
                break;

            case State.GrabReach:
                if (stateTime >= grabReachTime)
                {
                    grabIntent = false;
                    if (CanGrabPlayer() && player.BeginGrab(transform.position.x)) Enter(State.GrabLift);
                    else Enter(State.Recover);           // ohi
                }
                break;

            case State.GrabLift:
                HoldPlayer(Mathf.Clamp01(stateTime / grabLiftTime) * 3f);        // avainasennot 0..3 (kuvat 1..4)
                if (stateTime >= grabLiftTime) Enter(State.GrabThrow);
                break;

            case State.GrabThrow:
                if (stateTime < ThrowSwing) HoldPlayer(3f + stateTime / ThrowSwing);   // heilautus taakse
                else if (!thrown)
                {
                    thrown = true;
                    float dir = facingRight ? -1f : 1f;                  // heitetään selän taakse
                    player.Throw(dir * 5.5f, 4f, throwDamage);
                }
                if (stateTime >= ThrowSwing + 0.15f + 0.4f)
                {
                    facingRight = !facingRight;                          // Kovis on kääntynyt heittosuuntaan
                    cooldown = attackCooldown * Random.Range(0.9f, 1.3f);
                    grabIntent = false;
                    Enter(State.Idle);
                }
                break;

            case State.Dead:
                // vilkkuu ja katoaa
                if (body != null) body.enabled = Mathf.FloorToInt(stateTime * 12f) % 2 == 0;
                if (stateTime > 1.2f) Destroy(gameObject);
                break;
        }

        ApplyVisual();
    }

    const float ThrowSwing = 0.18f;
    bool thrown;

    bool CanGrabPlayer()
    {
        if (player == null || player.IsDown) return false;
        Vector3 p = player.transform.position, me = transform.position;
        float dx = p.x - me.x;
        bool front = facingRight ? dx >= -0.2f : dx <= 0.2f;
        return front && Mathf.Abs(dx) <= grabRange + 0.3f && Mathf.Abs(p.y - me.y) <= depthTolerance && player.AirHeight <= 0.3f;
    }

    /// Pidä pelaajaa käsissä. k = 0 ote, 1 nosto, 2–3 pään yllä, 4 heilautus taakse.
    void HoldPlayer(float k)
    {
        // avainasennot: (x eteenpäin, korkeus, kierto) Koviksen katsesuunnan mukaan
        bool art = player.HasThrowSprites;   // pelaajalla omat heittokuvat -> ei kiertoa, kuvat hoitavat asennon
        Vector3[] artKeys =
        {
            new Vector3(1.0f, 0.0f, 0f),     // ote
            new Vector3(0.9f, 0.3f, 0f),     // nostettuna, jalat irti
            new Vector3(0.3f, 2.2f, 0f),     // pään yllä vaakatasossa
            new Vector3(0.0f, 2.4f, 0f),
            new Vector3(-0.6f, 2.5f, 0f),    // heilautus selän taakse
        };
        Vector3[] rotKeys =
        {
            new Vector3(0.9f, 0.0f, 0f),
            new Vector3(0.8f, 0.9f, 25f),
            new Vector3(1.5f, 3.0f, 90f),
            new Vector3(1.4f, 3.2f, 95f),
            new Vector3(-0.3f, 3.1f, 130f),
        };
        Vector3[] keys = art ? artKeys : rotKeys;
        int i = Mathf.Clamp(Mathf.FloorToInt(k), 0, keys.Length - 2);
        int pose = k < 0.5f ? 0 : k < 1f ? 1 : k < 2f ? 2 : 3;
        Vector3 v = Vector3.Lerp(keys[i], keys[i + 1], Mathf.Clamp01(k - i));
        float dir = facingRight ? 1f : -1f;
        Vector3 me = transform.position;
        player.SetHeld(new Vector3(me.x + dir * v.x, me.y - 0.05f, 0f), v.y, dir * v.z, pose);
    }

    void Enter(State s)
    {
        if (s == State.GrabThrow) thrown = false;
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
        // kun hyökkäys on taas mahdollinen, arvotaan kerran: lyönti vai heittoyritys
        if (cooldown <= 0f && !attackRolled)
        {
            grabIntent = Has(grabSprites) && Random.value < grabChance;
            attackRolled = true;
        }
        float dist = grabIntent ? grabRange * 0.6f : attackRange * 0.8f;   // heittoa varten mennään aivan viereen
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

        bool inRange = Mathf.Abs(me.x - p.x) <= (grabIntent ? grabRange : attackRange) && Mathf.Abs(me.y - p.y) <= depthTolerance;
        if (inRange && attackRank <= 1 && cooldown <= 0f && !player.IsDown)
        {
            moving = false;
            attackRolled = false;                 // seuraava hyökkäys arvotaan uudelleen
            if (grabIntent) { Enter(State.GrabReach); return; }
            usingAlt = Has(altAttackSprites) && Random.value < altChance;
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
        if (!front || Mathf.Abs(dx) > CurrentReach + 0.2f) return false;
        if (Mathf.Abs(p.y - me.y) > depthTolerance) return false;
        if (player.AirHeight > 0.9f) return false;   // hypyllä voi väistää
        return player.TakeHit(usingAlt ? altDamage : punchDamage, me.x);
    }

    // ---------------- Osumat ----------------

    /// Pelaajan isku osuu. attackerX = lyöjän sijainti (mistä suunnasta isku tulee).
    public bool TakeHit(int damage, float attackerX, bool knockdown)
    {
        if (state == State.Down || state == State.GetUp || state == State.Dead) return false;
        if (state == State.GrabLift || state == State.GrabThrow) return false;   // heiton aikana ei keskeytetä
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
                if (Has(AtkSprites))
                {
                    // veto taakse: kuvat ennen iskun liikettä (esim. 0 ja 1), jaettuna vetoajalle
                    int wind = Mathf.Clamp(PunchImpact - 1, 1, AtkSprites.Length);
                    return AtkSprites[Mathf.Min((int)(stateTime / CurrentWindup * wind), wind - 1)];
                }
                return IdleFrame();

            case State.Punch:
                if (Has(AtkSprites))
                {
                    // lyhyt välikuva ja sitten täysin ojennettu käsi
                    int imp = PunchImpact;
                    return AtkSprites[stateTime < 0.04f && imp > 0 ? imp - 1 : imp];
                }
                return IdleFrame();

            case State.Recover:
                if (Has(AtkSprites))
                {
                    // käsi pysyy ojennettuna hetken, sitten palautuskuvat
                    int imp = PunchImpact;
                    int n = AtkSprites.Length - imp;
                    return AtkSprites[imp + Mathf.Min((int)(stateTime / punchRecoverTime * n), n - 1)];
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

            case State.GrabReach:
                return Has(grabSprites) ? grabSprites[0] : IdleFrame();

            case State.GrabLift:
                if (Has(grabSprites)) return grabSprites[Mathf.Clamp(1 + (int)(stateTime / grabLiftTime * 4f), 1, 4)];
                return IdleFrame();

            case State.GrabThrow:
                if (Has(grabSprites))
                {
                    if (stateTime < ThrowSwing) return grabSprites[Mathf.Min(5, grabSprites.Length - 1)];
                    if (stateTime < ThrowSwing + 0.15f) return grabSprites[Mathf.Min(6, grabSprites.Length - 1)];
                    return grabSprites[grabSprites.Length - 1];
                }
                return IdleFrame();

            case State.GetUp:
                if (Has(getUpSprites)) return getUpSprites[Mathf.Min((int)(stateTime / getUpTime * getUpSprites.Length), getUpSprites.Length - 1)];
                rot = Mathf.Lerp(90f, 0f, Mathf.Clamp01(stateTime / getUpTime));
                return FirstIdle();

            default:
                return IdleFrame();
        }
    }

    bool usingAlt;   // onko käynnissä toinen hyökkäys (pusku)
    Sprite[] AtkSprites => usingAlt ? altAttackSprites : punchSprites;
    int PunchImpact => Mathf.Clamp(usingAlt ? altImpactFrame : punchImpactFrame, 0, AtkSprites.Length - 1);
    float CurrentWindup => windupTime + (usingAlt ? altExtraWindup : 0f);
    float CurrentReach => usingAlt ? altReach : attackRange;

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
