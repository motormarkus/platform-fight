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
    [Tooltip("Iso vastus (Kovis): pelaajan vastaheitto on kuperkeikkaheitto, muille niskalenkki.")]
    public bool bigBody;
    [Tooltip("Oma kuvasarja pelaajan kuperkeikkaheittoon (vihu_kuperkeikka.png, 8 kuvaa): 0 ote, 1 veto, 2 nosto jaloille, 3 lento, 4–7 alastulo selälleen.")]
    public Sprite[] flipThrownSprites;
    [Tooltip("Oma kuvasarja pelaajan niskalenkkiin (punk_niskalenkki.png, 8 kuvaa): 0 lyönti, 1 ote, 2 veto, 3 askel, 4 olan yli, 5 ylösalaisin, 6–7 alastulo selälleen.")]
    public Sprite[] headlockThrownSprites;

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
    [Tooltip("Toisen hyökkäyksen syöksy eteen (yksikköä/s). 0 = ei syöksyä. Syöksy pysähtyy osumaan.")]
    public float altLungeSpeed = 0f;
    [Tooltip("Kuinka kauan syöksy kestää (s). Hyökkäys on aktiivinen koko syöksyn ajan.")]
    public float altLungeTime = 0.25f;

    [Tooltip("Toinen hyökkäys kaataa pelaajan (taklaus): lento taaksepäin.")]
    public bool altKnockdown;
    public float altKnockSpeed = 7f, altKnockUp = 6f;
    [Tooltip("Rynnäkkö: toinen hyökkäys aloitetaan jo näin kaukaa (x), ja syöksy kantaa pelaajaan asti. 0 = ei käytössä.")]
    public float chargeRange = 0f;
    [Tooltip("Rynnäkkö aloitetaan aikaisintaan tältä etäisyydeltä (lähempänä lyö tavallisesti).")]
    public float chargeMinRange = 2.6f;
    [Tooltip("Toisen hyökkäyksen veto ja palautus kerrotaan tällä (0.5 = kaksi kertaa nopeampi).")]
    public float altTimeScale = 1f;

    [Header("Taktiikka")]
    [Tooltip("Kaukana pelaajasta liikutaan näin paljon nopeammin (juoksu).")]
    public float runSpeedMultiplier = 1.3f;
    [Tooltip("Kuinka usein lähin vihu kiertää pelaajan selän taakse (0–1).")]
    [Range(0f, 1f)] public float flankChance = 0.25f;
    [Tooltip("Kierrettäessä kaarretaan syvyyssuunnassa näin kauas pelaajan ohi.")]
    public float flankArcDepth = 1.3f;
    [Tooltip("Kuinka usein hyökkäyksen jälkeen perääntyy hetkeksi (0–1).")]
    [Range(0f, 1f)] public float retreatChance = 0.2f;
    public float retreatTime = 0.7f;

    [Header("Torjunta (vapaaehtoinen)")]
    [Tooltip("Torjuntakuvat: 0 suoja ylös, 1 torjunta, 2 paluu. Jos tyhjä, käytetään idle-kuvaa.")]
    public Sprite[] blockSprites;
    [Tooltip("Kuinka usein edestä tuleva isku torjutaan (0–1). 0 = ei torju koskaan.")]
    [Range(0f, 1f)] public float blockChance = 0f;
    [Tooltip("Montako iskua peräkkäin voi torjua; seuraava menee läpi.")]
    public int maxBlocksInRow = 2;
    public float blockTime = 0.35f;

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
    [Tooltip("Heittää eteenpäin (ruudun poikki) eikä selän taakse.")]
    public bool throwForward;
    [Tooltip("Heiton vauhti vaakaan ja ylös.")]
    public float throwSpeed = 5.5f, throwUp = 4f;
    [Tooltip("Jos pelaaja pysyy näin kauan (s) aivan vieressä, vihu tarttuu heti. 0 = ei käytössä.")]
    public float grabWhenCloseTime = 0f;

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

    enum State { Idle, Block, Chase, Windup, Punch, Recover, Hurt, Airborne, Down, GetUp, Dead, GrabReach, GrabLift, GrabThrow, Held }
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
    float closeTimer;       // kauanko pelaaja on ollut aivan vieressä
    bool flanking;          // tämä hyökkäys tehdään pelaajan selän takaa
    float retreatTimer;     // perääntyy hetken hyökkäyksen jälkeen
    int blocksInRow;
    /// Torjuiko vihu juuri saamansa iskun (pelaaja näyttää sinisen läiskän).
    public bool JustBlocked { get; private set; }
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
        PlayerController.SortByFrameNumber(blockSprites);
        PlayerController.SortByFrameNumber(knockdownSprites);
        PlayerController.SortByFrameNumber(getUpSprites);
        PlayerController.SortByFrameNumber(flipThrownSprites);
        PlayerController.SortByFrameNumber(headlockThrownSprites);
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
            {
                bool lunging = usingAlt && altLungeSpeed > 0f;
                // pusku: syöksy eteen, kunnes osuu tai aika loppuu
                if (lunging && !punchLanded && stateTime < altLungeTime)
                    Move(new Vector2((facingRight ? 1f : -1f) * altLungeSpeed * dt, 0f));
                if (!punchLanded) punchLanded = TryHitPlayer();
                if (state != State.Punch) break;   // pelaaja nappasi kädestä kiinni
                if (stateTime >= (lunging ? Mathf.Max(punchActiveTime, altLungeTime) : punchActiveTime)) Enter(State.Recover);
            }
                break;

            case State.Recover:
                if (stateTime >= CurrentRecover)
                {
                    cooldown = attackCooldown * Random.Range(0.7f, 1.3f);
                    if (Random.value < retreatChance) retreatTimer = retreatTime;   // iske ja vetäydy
                    Enter(State.Idle);
                }
                break;

            case State.Block:
                Move(knockVel * dt);
                knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 10f * dt);
                if (stateTime >= blockTime)
                {
                    cooldown = Mathf.Min(cooldown, 0.1f);   // vastaisku heti torjunnan perään
                    attackRolled = false;
                    Enter(State.Chase);
                }
                break;

            case State.Hurt:
                Move(knockVel * dt);
                knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 12f * dt);
                if (stateTime >= hurtTime) Enter(State.Chase);
                break;

            case State.Held:
                // pelaaja liikuttaa (SetHeldByPlayer); varmuuden vuoksi irti, jos heitto jää kesken
                if (stateTime > 3f) ReleaseThrow(0f, 2f, 0);
                break;

            case State.Airborne:
                Move(knockVel * dt);
                verticalVel -= (thrownByPlayer ? 48f : 30f) * dt;   // heitetty iskeytyy maahan nopeasti
                height += verticalVel * dt;
                if (thrownByPlayer && artThrow)
                {
                    // omat kuvat: kiertokuva kiertyy eteenpäin (kuperkeikassa hetken, sitten suora lentokuva)
                    float dir = facingRight ? -1f : 1f;   // eteenpäin kaatuminen pelaajan yli
                    if (artFlightPose < 0 || stateTime < ArtSpinTime)
                        spinRot = Mathf.MoveTowards(spinRot, dir * artSpinTarget, artSpinRate * dt);
                    else spinRot = 0f;
                }
                else if (thrownByPlayer)
                {
                    // pyörähdys jatkuu selälleen, kiertopiste laskeutuu kohti maata
                    float dir = facingRight ? -1f : 1f;
                    spinRot = Mathf.MoveTowards(spinRot, dir * 265f, 650f * dt);
                    spinCenter = Mathf.MoveTowards(spinCenter, 0.4f, 3f * dt);
                }
                if (height <= 0f)
                {
                    height = 0f;
                    if (thrownByPlayer)
                    {
                        flipLanded = artThrow;   // omat alastulokuvat
                        artThrow = false;
                        spinRot = 0f;
                        thrownByPlayer = false;
                        PlayHurtSound();
                        HitFx.OnHit(true);
                        knockVel = Vector2.zero;
                    }
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
                    float dir = (facingRight ? 1f : -1f) * (throwForward ? 1f : -1f);   // eteen tai selän taakse
                    player.Throw(dir * throwSpeed, throwUp, throwDamage);
                }
                if (stateTime >= ThrowSwing + 0.15f + 0.4f)
                {
                    if (!throwForward) facingRight = !facingRight;       // Kovis on kääntynyt heittosuuntaan
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
        if (throwForward) { artKeys[4] = new Vector3(1.3f, 2.0f, 0f); rotKeys[4] = new Vector3(1.6f, 2.4f, 60f); }   // heitto eteen: pelaaja lähtee käsistä edestä
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
        if (s != State.Down && s != State.Dead) flipLanded = false;
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

        // liian kauan aivan vieressä: tarttuu heti (pomo)
        bool close = Mathf.Abs(p.x - me.x) <= grabRange + 0.3f && Mathf.Abs(p.y - me.y) <= depthTolerance;
        closeTimer = close ? closeTimer + dt : Mathf.Max(0f, closeTimer - dt);
        if (grabWhenCloseTime > 0f && closeTimer >= grabWhenCloseTime && Has(grabSprites) && !player.IsDown)
        {
            closeTimer = 0f;
            facingRight = p.x > me.x;
            if (CanGrabPlayer()) { moving = false; grabIntent = false; attackRolled = false; Enter(State.GrabReach); return; }
        }
        // kun hyökkäys on taas mahdollinen, arvotaan kerran: lyönti vai heittoyritys
        if (cooldown <= 0f && !attackRolled)
        {
            grabIntent = Has(grabSprites) && Random.value < grabChance;
            flanking = Random.value < flankChance;
            attackRolled = true;
        }
        float dist = grabIntent ? grabRange * 0.6f : attackRange * 0.8f;   // heittoa varten mennään aivan viereen
        float yOff = 0f;
        if (attackRank == 0 && flanking) side = player.FacingRight ? -1f : 1f;   // pelaajan selän taakse
        else if (attackRank == 1 && first != null) side = first.transform.position.x >= p.x ? -1f : 1f;
        else if (attackRank >= 2)
        {
            dist = 3.4f + (attackRank - 2) * 0.9f;
            yOff = (attackRank % 2 == 0) ? 0.7f : -0.7f;
            yOff += Mathf.Sin(Time.time * 1.1f + GetInstanceID() * 0.37f) * 0.5f;   // vuoroaan odottavat liikehtivät
        }
        if (retreatTimer > 0f)
        {
            // iske ja vetäydy: hetki kauempana ennen seuraavaa hyökkäystä
            retreatTimer -= dt;
            dist = 3.6f;
        }
        Vector2 target = new Vector2(p.x + side * dist, p.y + yOff);
        // toiselle puolelle mennessä kaarretaan pelaajan ohi syvyyssuunnassa, ei kävellä läpi
        bool crossing = Mathf.Sign(me.x - p.x) != side && Mathf.Abs(me.x - p.x) < dist + 1.5f;
        if (crossing)
        {
            float minY = player.minDepthY, maxY = player.maxDepthY;
            float arc = me.y >= p.y ? 1f : -1f;
            if (p.y + arc * flankArcDepth > maxY || p.y + arc * flankArcDepth < minY) arc = -arc;
            target.y = Mathf.Clamp(p.y + arc * flankArcDepth, minY, maxY);
        }
        Vector2 to = target - (Vector2)me;

        facingRight = p.x > me.x;

        // rynnäkkö (taklaus): samalla syvyydellä matkan päässä -> syöksy pelaajaa kohti
        float adx = Mathf.Abs(me.x - p.x);
        if (chargeRange > 0f && Has(altAttackSprites) && attackRank <= 1 && cooldown <= 0f && retreatTimer <= 0f && !player.IsDown
            && !grabIntent && adx >= chargeMinRange && adx <= chargeRange && Mathf.Abs(me.y - p.y) <= depthTolerance * 0.8f
            && Random.value < altChance * dt * 3f)
        {
            moving = false;
            attackRolled = false;
            usingAlt = true;
            Enter(State.Windup);
            return;
        }

        bool inRange = Mathf.Abs(me.x - p.x) <= (grabIntent ? grabRange : attackRange) && Mathf.Abs(me.y - p.y) <= depthTolerance;
        if (inRange && attackRank <= 1 && cooldown <= 0f && retreatTimer <= 0f && !player.IsDown)
        {
            moving = false;
            attackRolled = false;                 // seuraava hyökkäys arvotaan uudelleen
            if (grabIntent) { Enter(State.GrabReach); return; }
            usingAlt = chargeRange <= 0f && Has(altAttackSprites) && Random.value < altChance;   // rynnäkkö vain kaukaa
            Enter(State.Windup);
            return;
        }

        if (to.magnitude > 0.08f)
        {
            moving = true;
            Vector2 dir = to.normalized;
            // kaukana juostaan (myös kierrettäessä selän taakse)
            float run = to.magnitude > 3.5f || crossing ? runSpeedMultiplier : 1f;
            if (run > 1f) animClock += dt * (run - 1f);   // askeleet tihenevät
            Move(new Vector2(dir.x * moveSpeedX, dir.y * moveSpeedY) * run * dt);
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
        if (usingAlt && altKnockdown) return player.TakeKnockdown(altDamage, me.x, altKnockSpeed, altKnockUp, this);
        return player.TakeHit(usingAlt ? altDamage : punchDamage, me.x, this);
    }

    // ---------------- Osumat ----------------

    /// Pelaajan isku osuu. attackerX = lyöjän sijainti (mistä suunnasta isku tulee).
    public bool TakeHit(int damage, float attackerX, bool knockdown)
    {
        if (state == State.Down || state == State.GetUp || state == State.Dead) return false;
        if (state == State.GrabLift || state == State.GrabThrow || state == State.Held) return false;   // heiton aikana ei keskeytetä
        if (state == State.Airborne && height > 0.1f && !knockdown) return false;

        // torjunta: vain edestä, kun ei olla itse kesken iskun; muutaman torjunnan jälkeen suoja murtuu
        bool facingAttacker = (attackerX > transform.position.x) == facingRight;
        bool guardState = state == State.Chase || state == State.Recover || state == State.Block || (state == State.Idle && awake);
        if (blockChance > 0f && facingAttacker && guardState && blocksInRow < maxBlocksInRow
            && (state == State.Block || Random.value < blockChance))
        {
            blocksInRow++;
            JustBlocked = true;
            knockVel = new Vector2(attackerX < transform.position.x ? 1.6f : -1.6f, 0f);
            shakeUntil = HitFx.ShakeUntil(false);
            Enter(State.Block);
            return true;
        }
        JustBlocked = false;
        blocksInRow = 0;

        awake = true;
        health = Mathf.Max(0, health - damage);
        LastHit = this; LastHitTime = Time.time;
        bool fromLeft = attackerX < transform.position.x;
        facingRight = !fromLeft;   // käänny lyöjään päin
        flashTimer = 0.1f;
        shakeUntil = HitFx.ShakeUntil(knockdown || health <= 0);   // tärisee osumapysäytyksen ajan
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
            knockVel = new Vector2(fromLeft ? 3.2f : -3.2f, 0f);   // tuntuva työntö osumasta
            Enter(State.Hurt);
        }
        return true;
    }

    // ---------------- Pelaajan vastaheitto ----------------

    bool thrownByPlayer;
    float shakeUntil;       // osuman tärinä (reaaliaikaa)
    bool artThrow;          // heitetään omilla kuvilla (artSet)
    bool flipLanded;        // maassa omilla alastulokuvilla
    int heldPose = -1;      // mikä omista kuvista näytetään otteessa (-1 = osumakuva + kierto)
    const float ArtSpinTime = 0.1f;

    // Käytössä oleva kuvasarja ja sen rakenne (kuperkeikka tai niskalenkki)
    Sprite[] artSet;
    int artSpinPose, artFlightPose, artLandFirst;
    float artSpinTarget, artSpinRate, artLandFrameTime;

    public bool HasFlipArt => flipThrownSprites != null && flipThrownSprites.Length >= 8;
    public bool HasHeadlockArt => headlockThrownSprites != null && headlockThrownSprites.Length >= 8;
    /// Onko vastuksella omat kuvat tähän heittoon.
    public bool HasArtFor(bool monkeyFlip) => monkeyFlip ? HasFlipArt : HasHeadlockArt;
    float spinRot;          // kierto (astetta, maailman z), kun pelaaja pitää tai heittää
    float spinCenter;       // kiertopisteen korkeus jaloista (yksikköä)

    /// Voiko pelaaja napata kiinni (vain kesken lyönnin, ei heiton tai kaatuneena).
    public bool CanBeCaught => state == State.Punch;

    /// Pelaaja nappaa lyövästä kädestä kiinni.
    public void BeginHeldByPlayer(float playerX, bool monkeyFlip = false)
    {
        artSet = null;
        if (monkeyFlip && HasFlipArt)
        {
            // kuperkeikka: nosto (2) kiertyy puolikkaan, lento (3) suorana, alastulo 4–7
            artSet = flipThrownSprites;
            artSpinPose = 2; artFlightPose = 3; artSpinTarget = 180f; artSpinRate = 700f;
            artLandFirst = 4; artLandFrameTime = 0.08f;
        }
        else if (!monkeyFlip && HasHeadlockArt)
        {
            // niskalenkki: ylösalaisin-kuva (5) kiertyy lennon aikana selälleen päin, alastulo 6–7
            artSet = headlockThrownSprites;
            artSpinPose = 5; artFlightPose = -1; artSpinTarget = 45f; artSpinRate = 300f;
            artLandFirst = 6; artLandFrameTime = 0.12f;
        }
        facingRight = playerX > transform.position.x;   // kasvot pelaajaan päin
        knockVel = Vector2.zero;
        spinRot = 0f;
        spinCenter = 1.5f;
        heldPose = -1;
        Enter(State.Held);
    }

    /// Pelaaja liikuttaa: kehon keskikohta (x, syvyys), korkeus ja kierto keskikohdan ympäri.
    public void SetHeldByPlayer(Vector3 pos, float lift, float rot, int pose = -1)
    {
        if (state != State.Held) return;
        Vector3 p = transform.position;
        float minY = player != null ? player.minDepthY : -4.3f;
        float maxY = player != null ? player.maxDepthY : -0.8f;
        transform.position = new Vector3(pos.x, Mathf.Clamp(pos.y, minY, maxY), p.z);
        height = lift;
        spinRot = rot;
        heldPose = artSet != null ? pose : -1;
    }

    /// Pelaaja heittää: lento vaakanopeudella vx, ylös up; vahinko heti, tärähdys maahan osuessa.
    public void ReleaseThrow(float vx, float up, int damage)
    {
        if (state != State.Held) return;
        awake = true;
        health = Mathf.Max(0, health - damage);
        LastHit = this; LastHitTime = Time.time;
        knockVel = new Vector2(vx, 0f);
        verticalVel = up;
        height = Mathf.Max(height, 0.05f);
        thrownByPlayer = true;
        artThrow = heldPose >= 0;
        Enter(State.Airborne);
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
        float shake = HitFx.ShakeOffset(shakeUntil);
        body.transform.localPosition = new Vector3(pivotFix.x + shake, groundHeight + height - footOffset + pivotFix.y, 0f);
        // kaatumisen väliaikainen korvike: käännetään kuvaa, kun oikeat kuvat puuttuvat
        body.transform.localRotation = Quaternion.Euler(0f, 0f, facingRight ? rot : -rot);
        if (state == State.Held || (state == State.Airborne && thrownByPlayer))
        {
            // pelaajan heitossa kierretään kehon keskikohdan ympäri, ei jalkojen
            var q = Quaternion.Euler(0f, 0f, spinRot);
            Vector3 c = new Vector3(0f, spinCenter, 0f);
            body.transform.localRotation = q;
            body.transform.localPosition += c - q * c;
        }

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
                    return AtkSprites[imp + Mathf.Min((int)(stateTime / CurrentRecover * n), n - 1)];
                }
                return IdleFrame();

            case State.Block:
                if (Has(blockSprites)) return blockSprites[Mathf.Min((int)(stateTime / blockTime * blockSprites.Length), blockSprites.Length - 1)];
                return FirstIdle();

            case State.Hurt:
                if (Has(hurtSprites)) return hurtSprites[Mathf.Min((int)(stateTime / hurtTime * hurtSprites.Length), hurtSprites.Length - 1)];
                return FirstIdle();

            case State.Airborne:
                if (thrownByPlayer && artThrow)
                    return artSet[artFlightPose >= 0 && stateTime >= ArtSpinTime ? artFlightPose : artSpinPose];
                if (thrownByPlayer) return Has(hurtSprites) ? hurtSprites[0] : FirstIdle();
                if (Has(knockdownSprites))
                {
                    int airFrames = Mathf.Max(1, knockdownSprites.Length - 1);
                    return knockdownSprites[Mathf.Min((int)(stateTime / 0.1f), airFrames - 1)];
                }
                rot = Mathf.Lerp(20f, 80f, Mathf.Clamp01(stateTime / 0.35f));
                return FirstIdle();

            case State.Held:
                if (heldPose >= 0) return artSet[Mathf.Clamp(heldPose, 0, artSpinPose)];
                return Has(hurtSprites) ? hurtSprites[0] : FirstIdle();

            case State.Down:
            case State.Dead:
                if (flipLanded && artSet != null)
                    return artSet[Mathf.Min(artLandFirst + (int)(stateTime / artLandFrameTime), artSet.Length - 1)];
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
    float CurrentWindup => usingAlt ? (windupTime + altExtraWindup) * altTimeScale : windupTime;
    float CurrentRecover => usingAlt ? punchRecoverTime * altTimeScale : punchRecoverTime;
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
