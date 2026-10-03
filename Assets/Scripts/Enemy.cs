using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Beat 'em up -vihollinen: lähestyy pelaajaa, asettuu lyöntietäisyydelle ja lyö.
/// Ottaa osumia, kaatuu, nousee ylös ja kuolee. Puuttuvat animaatiot korvataan
/// väliaikaisesti idle-kuvalla (osuma = väläys ja tärinä, kaatuminen = kuvan kääntö).
/// </summary>
public class Enemy : MonoBehaviour, IBottleHolder
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
    [Tooltip("Kuperkeikan lentokuvien määrä (kuvasta 3 alkaen). 1 = yksi lentokuva, jota kierretään (Kovis); useampi = kuvat piirretty valmiiksi pyörimään.")]
    public int flipFlightFrames = 1;
    public float flipFlightFrameTime = 0.05f, flipLandFrameTime = 0.08f;
    [Tooltip("Oma kuvasarja pelaajan niskalenkkiin (punk_niskalenkki.png, 8 kuvaa): 0 lyönti, 1 ote, 2 veto, 3 askel, 4 olan yli, 5 ylösalaisin, 6–7 alastulo selälleen.")]
    public Sprite[] headlockThrownSprites;
    [Tooltip("Niskalenkin lento ja alastulo (punk_niskalenkki_lento.png, 6 kuvaa): 0 ylösalaisin, 1 lento, 2 juuri ennen maata, 3 isku, 4 pomppu, 5 makaa.")]
    public Sprite[] headlockFlightSprites;
    [Tooltip("Polvi päähän (Skettari): otteessa 5 kuvaa (0 asento, 1 ote päästä, 2–3 kumarassa, 4 isku).")]
    public Sprite[] kneeHeldSprites;
    [Tooltip("Polven jälkeen: 0 horjahtaa, 1 lentää, 2 juuri ennen maata, 3 isku maahan, 4 maassa, 5… kierähdys ja makaa.")]
    public Sprite[] kneeFlightSprites;
    public bool HasKneeArt => kneeHeldSprites != null && kneeHeldSprites.Length >= 5 && kneeFlightSprites != null && kneeFlightSprites.Length >= 6;
    bool kneeMode;
    Sprite[] FlightSet => kneeMode ? kneeFlightSprites : headlockFlightSprites;

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
    [Tooltip("Kombon toinen osuma samassa lyöntisarjassa (esim. jab + suora). -1 = ei toista osumaa.")]
    public int secondImpactFrame = -1;
    bool secondHitDone;

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
    [Tooltip("Toinen hyökkäys hypyllä (esim. hyppypotku): hypyn korkeus yksikköinä (0 = ei hyppyä).")]
    public float altJumpHeight = 0f;

    [Tooltip("Tavallinen lyönti kaataa pelaajan (pomon isku ylhäältä).")]
    public bool punchKnockdown;
    [Tooltip("Lyönnin osuessa maahan kamera tärähtää (voimakkuus). 0 = ei.")]
    public float punchShake = 0f;
    [Tooltip("Toinen hyökkäys kaataa pelaajan (taklaus): lento taaksepäin.")]
    public bool altKnockdown;
    public float altKnockSpeed = 7f, altKnockUp = 6f;
    [Tooltip("Rynnäkkö: toinen hyökkäys aloitetaan jo näin kaukaa (x), ja syöksy kantaa pelaajaan asti. 0 = ei käytössä.")]
    public float chargeRange = 0f;
    [Tooltip("Rynnäkkö aloitetaan aikaisintaan tältä etäisyydeltä (lähempänä lyö tavallisesti).")]
    public float chargeMinRange = 2.6f;
    [Tooltip("Toisen hyökkäyksen veto ja palautus kerrotaan tällä (0.5 = kaksi kertaa nopeampi).")]
    public float altTimeScale = 1f;

    [Header("Tynnyrit (pomo)")]
    [Tooltip("Hakee lähellä olevan tynnyrin ja heittää sen pelaajaa kohti (heittokuvat: 2–3 nosto, 4 veto, 5 heitto, 7 jälkeen).")]
    public bool throwsBarrels;
    public float barrelThrowSpeed = 16f, barrelThrowUp = 4f;
    public int barrelDamage = 20;
    [Tooltip("Kuinka usein (1/s) tynnyriä lähdetään hakemaan, kun pelaaja on kaukana.")]
    public float barrelRate = 0.35f;

    [Header("Pullot (punkkari)")]
    [Tooltip("punk_pullo.png: 13 kuvaa (ThrowPose: 1–5 nosto, 6–7 veto, 9 heitto, 10–12 paluu). Tyhjä = ei hae pulloja.")]
    public Sprite[] bottleSprites;
    [Tooltip("Kuinka usein (1/s) lähdetään hakemaan lattialla olevaa ehjää pulloa, kun pelaaja ei ole aivan vieressä.")]
    public float bottleRate = 0.3f;
    Bottle targetBottle, heldBottle;
    bool bottleThrown;
    bool HasBottleSprites => bottleSprites != null && bottleSprites.Length >= 13;

    [Header("Kaikkien kimppuun (portsari)")]
    [Tooltip("Hyökkää lähimmän kimppuun: pelaaja tai muut vihut (ei omiaan). Lyödyt vihut lyövät takaisin.")]
    public bool fightsEveryone;
    Enemy enemyTarget;          // toinen vihu, jota jahdataan (portsari tai kosto)
    float grudgeUntil, retargetTime;

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
    [Header("Vatsatöytäisy (pomo)")]
    [Tooltip("8 kuvaa: 0–1 lataus, 2–3 maha eteen (3 = isku), 4 palautus, 5–6 nauru, 7 asento.")]
    public Sprite[] bellySprites;
    [Tooltip("Lähietäisyydellä osa hyökkäyksistä on vatsatöytäisyjä.")]
    [Range(0f, 1f)] public float bellyChance = 0f;
    public float bellyRange = 1.6f;
    [Tooltip("Kombon keskellä: torjuu iskun ja vastaa töytäisyllä (todennäköisyys per isku, kun kombossa on jo bellyCounterAfterHits osumaa).")]
    [Range(0f, 1f)] public float bellyCounterChance = 0f;
    public int bellyCounterAfterHits = 2;
    public int bellyDamage = 14;
    [Tooltip("Pelaaja lentää kauas.")]
    public float bellyKnockSpeed = 14f, bellyKnockUp = 5.5f;
    [Tooltip("Töytäisyn kuvat (0–4) ja naurun kuvat (5,6,5,6,7): nauru kestää vähän töytäisyä pidempään.")]
    public float bellyPumpFrameTime = 0.08f, bellyLaughFrameTime = 0.13f;
    [Tooltip("Nauru töytäisyn jälkeen.")]
    public AudioClip laughSound;
    [Range(0f, 1f)] public float laughVolume = 1f;

    [Header("Töminä (pomon taklausjuoksu)")]
    public AudioClip[] stompSounds;
    [Range(0f, 1f)] public float stompVolume = 0.85f;
    [Tooltip("Askelväli taklauksen juoksussa (s).")]
    public float stompInterval = 0.2f;
    [Tooltip("Ruudun tärinä jokaisella askeleella.")]
    public float stompShake = 0f;
    float stompTimer;

    [Header("Rullalauta (Skettari)")]
    [Tooltip("Irtolauta: kun laudalla liikkuva vihu kaatuu, lauta irtoaa ja jatkaa matkaa. Tyhjä = ei lautaa.")]
    public Sprite looseBoardSprite;
    [Tooltip("Kierähdys maassa kaatumisen jälkeen (viimeinen = makaa).")]
    public Sprite[] landSprites;
    public float landFrameTime = 0.08f;
    [Tooltip("Ilman lautaa (noustua): idle/tappeluasento, kävely, lyönti, osuma. Tyhjät korvataan tappeluasennolla.")]
    public Sprite[] footIdleSprites, footWalkSprites, footPunchSprites, footHurtSprites;
    public int footPunchImpactFrame = 3;
    [Tooltip("Jalan lyöntisarjan toinen osuma (jab + suora), -1 = ei.")]
    public int footSecondImpactFrame = -1;
    public float footWindupTime = 0.15f, footPunchRecoverTime = 0.6f;
    [Tooltip("Hyppypotku jalan (loikka eteen, osuma kuvassa footKickImpactFrame).")]
    public Sprite[] footKickSprites;
    public int footKickImpactFrame = 5;
    public float footKickLunge = 6f;
    public float footKickJump = 1.3f;
    public float footMoveSpeedX = 2.8f, footMoveSpeedY = 1.6f;
    public float footWalkFrameTime = 0.05f;
    [Tooltip("Laudalla: ajaa kovaa edestakaisin pelaajan ohi (kääntyy toisella puolella) ja lyö ohittaessaan.")]
    public bool skatePass;
    [Tooltip("Kuinka pitkälle pelaajan ohi ajetaan ennen kääntymistä (yksikköä).")]
    public float passOvershoot = 6.5f;
    int passDir;
    float lastTurnTime = -10f;
    [Tooltip("Käännösten vähimmäisväli laudalla (s).")]
    public float passTurnInterval = 2.5f;
    bool boardLost;
    float lastMoveX = 1f;

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
    [Tooltip("Huudot iskun lähtiessä (esim. attack1–3).")]
    public AudioClip[] attackSounds;
    [Range(0f, 1f)] public float attackVolume = 0.9f;
    [Tooltip("Kuinka usein isku saa huudon (0–1), ettei se toistu joka lyönnillä.")]
    [Range(0f, 1f)] public float attackSoundChance = 0.6f;

    [Header("Spriten sijoitus")]
    public float footOffset = 0.08f;

    enum State { Idle, Block, Belly, BarrelLift, BarrelThrow, BottlePick, BottleThrow, Chase, Windup, Punch, Recover, Hurt, Airborne, Down, GetUp, Dead, GrabReach, GrabLift, GrabThrow, Held }
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
    Crate targetBarrel;     // tynnyri, jota ollaan hakemassa
    Crate heldBarrel;       // tynnyri käsissä
    float closeTimer;       // kauanko pelaaja on ollut aivan vieressä
    bool flanking;          // tämä hyökkäys tehdään pelaajan selän takaa
    float retreatTimer;     // perääntyy hetken hyökkäyksen jälkeen
    int blocksInRow;
    int comboHits; float lastHitTime;   // pelaajan kombo (vatsatöytäisyn vastaisku)
    bool bellyHit, bellyLaughed;
    static readonly int[] BellyPump = { 0, 1, 2, 3, 4 }, BellyLaugh = { 5, 6, 5, 6, 7 };
    float BellyImpactTime => 3f * bellyPumpFrameTime;
    float BellyPumpTime => BellyPump.Length * bellyPumpFrameTime;
    float BellyTotalTime => BellyPumpTime + BellyLaugh.Length * bellyLaughFrameTime;
    /// Torjuiko vihu juuri saamansa iskun (pelaaja näyttää sinisen läiskän).
    public bool JustBlocked { get; private set; }
    int attackRank;   // 0 = lähin, 1 = toinen (vastakkaiselta puolelta), 2+ = odottaa vuoroaan

    public int Health => health;
    public bool IsDead => state == State.Dead;

    void OnEnable() { All.Add(this); }
    /// Herää heti (portsarit tulevat ovesta tappelun alkaessa).
    public void WakeUp() { awake = true; }
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
        PlayerController.SortByFrameNumber(bellySprites);
        PlayerController.SortByFrameNumber(knockdownSprites);
        PlayerController.SortByFrameNumber(getUpSprites);
        PlayerController.SortByFrameNumber(flipThrownSprites);
        PlayerController.SortByFrameNumber(headlockThrownSprites);
        PlayerController.SortByFrameNumber(headlockFlightSprites);
        PlayerController.SortByFrameNumber(kneeHeldSprites);
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
        Stomp(dt);

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
                if (stateTime >= CurrentWindup)
                {
                    punchLanded = false;
                    if (!usingAlt && punchShake > 0f && CameraFollow.Instance != null) CameraFollow.Shake(0.15f, punchShake);   // isku maahan
                    if (Random.value < attackSoundChance) PlayAttackSound();
                    Enter(State.Punch);
                }
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
                // kombon toinen isku (lyöntisarjan myöhempi kuva)
                if (!usingAlt && secondImpactFrame > PunchImpact && !secondHitDone && Has(AtkSprites))
                {
                    int n = AtkSprites.Length - PunchImpact;
                    int f = PunchImpact + (int)(stateTime / CurrentRecover * n);
                    if (f >= secondImpactFrame)
                    {
                        secondHitDone = true;
                        if (Random.value < attackSoundChance) PlayAttackSound();
                        TryHitPlayer(true);
                    }
                }
                if (stateTime >= CurrentRecover)
                {
                    cooldown = attackCooldown * Random.Range(0.7f, 1.3f);
                    if (Random.value < retreatChance) retreatTimer = retreatTime;   // iske ja vetäydy
                    Enter(State.Idle);
                }
                break;

            case State.BarrelLift:
            {
                // nosto pään yli ja tähtäys: siirtyy samalle syvyydelle pelaajan kanssa
                float k = Mathf.Clamp01(stateTime / 0.45f);
                if (player != null)
                {
                    facingRight = player.transform.position.x > transform.position.x;
                    float dy = player.transform.position.y - transform.position.y;
                    Move(new Vector2(0f, Mathf.Clamp(dy, -moveSpeedY * dt, moveSpeedY * dt)));
                }
                HoldBarrel(Mathf.Lerp(0.9f, 0.1f, k), Mathf.Lerp(0.3f, 3.5f, k));
                if (stateTime >= 0.7f) Enter(State.BarrelThrow);
            }
                break;

            case State.BarrelThrow:
                if (stateTime < 0.15f) HoldBarrel(-0.3f, 3.6f);                 // veto taakse
                else if (heldBarrel != null)
                {
                    HoldBarrel(0.7f, 3.0f);
                    float dir = facingRight ? 1f : -1f;
                    heldBarrel.Throw(dir * barrelThrowSpeed, barrelThrowUp, this, barrelDamage);
                    heldBarrel = null;
                    if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.12f);
                }
                if (stateTime >= 0.6f)
                {
                    cooldown = attackCooldown * Random.Range(0.9f, 1.3f);
                    Enter(State.Idle);
                }
                break;

            case State.BottlePick:
                if (ThrowPose.Index(ThrowPose.PickTimes, stateTime) < 0)
                {
                    if (heldBottle == null) { Enter(State.Idle); break; }
                    Enter(State.BottleThrow);
                }
                break;

            case State.BottleThrow:
                // tähtäys: kääntyy pelaajaan ja hakeutuu samalle syvyydelle vedon aikana
                if (!bottleThrown && player != null)
                {
                    facingRight = player.transform.position.x > transform.position.x;
                    float dy = player.transform.position.y - transform.position.y;
                    Move(new Vector2(0f, Mathf.Clamp(dy, -moveSpeedY * dt, moveSpeedY * dt)));
                }
                if (!bottleThrown && stateTime >= ThrowPose.Start(ThrowPose.PunkThrowTimes, ThrowPose.ReleaseIndex))
                {
                    bottleThrown = true;
                    if (heldBottle != null)
                    {
                        heldBottle.Throw(transform.position.y, facingRight ? 1f : -1f, true);
                        heldBottle = null;
                        if (Random.value < attackSoundChance) PlayAttackSound();
                    }
                }
                if (ThrowPose.Index(ThrowPose.PunkThrowTimes, stateTime) < 0)
                {
                    cooldown = attackCooldown * Random.Range(0.9f, 1.3f);
                    Enter(State.Idle);
                }
                break;

            case State.Belly:
                if (!bellyHit && stateTime >= BellyImpactTime)
                {
                    bellyHit = true;
                    if (Random.value < attackSoundChance) PlayAttackSound();
                    if (CameraFollow.Instance != null) CameraFollow.Shake(0.1f, 0.12f);
                    TryBellyHit();
                }
                if (!bellyLaughed && stateTime >= BellyPumpTime)
                {
                    bellyLaughed = true;
                    if (laughSound != null && audioSource != null) { audioSource.pitch = 1f; audioSource.PlayOneShot(laughSound, laughVolume); }
                }
                if (stateTime >= BellyTotalTime)
                {
                    cooldown = attackCooldown * Random.Range(0.8f, 1.2f);
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
                // lentävä vihu rikkoo pöydät (ja niiden pullot) tieltään
                if (height < 1.8f && Mathf.Abs(knockVel.x) > 2f)
                    for (int ci = Crate.All.Count - 1; ci >= 0; ci--)
                    {
                        var c = Crate.All[ci];
                        if (c == null || !c.breakable || !c.CanBeHit || c.breakSprites == null || c.breakSprites.Length == 0) continue;
                        Vector3 q = c.transform.position, me2 = transform.position;
                        if (Mathf.Abs(q.x - me2.x) < 1.2f && Mathf.Abs(q.y - me2.y) < 0.5f) c.Smash();
                    }
                verticalVel -= (thrownByPlayer ? 48f : 30f) * dt;   // heitetty iskeytyy maahan nopeasti
                height += verticalVel * dt;
                if (thrownByPlayer && flightArt)
                    spinRot = 0f;   // lentokuvat piirretty valmiiksi oikeaan asentoon
                else if (thrownByPlayer && artThrow)
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
                    if (thrownByPlayer && flightArt)
                    {
                        // niskalenkin paiskaus: isku, pomppu ja makuu omilla kuvilla, kunnon tärähdys ja pöly
                        slamLanded = true;
                        flightArt = false; artThrow = false;
                        spinRot = 0f;
                        thrownByPlayer = false;
                        PlayHurtSound();
                        HitFx.OnHit(true);
                        HitFx.PlayClip(SlamSound, 1f);
                        DustPuff.Spawn(transform.position, Mathf.RoundToInt(-transform.position.y * 100f) + 2, 1.3f);
                        knockVel *= 0.35f;   // liukuu vähän iskun jälkeen
                        Enter(State.Down);
                        if (CameraFollow.Instance != null) CameraFollow.Shake(0.22f, 0.25f);
                        break;
                    }
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
                if (slamLanded)
                {
                    Move(knockVel * dt);
                    knockVel = Vector2.MoveTowards(knockVel, Vector2.zero, 9f * dt);
                }
                if (stateTime >= downTime)
                {
                    if (health <= 0) { Enter(State.Dead); bool rare = Random.value < stackChance; Pickup.SpawnMoney(transform.position, rare ? stackValue : noteValue, rare); }
                    else Enter(State.GetUp);
                }
                break;

            case State.GetUp:
                if (stateTime >= getUpTime) { cooldown = Mathf.Max(cooldown, 0.6f); if (boardLost) GoOnFoot(); Enter(State.Chase); }
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
                HoldPlayer(LiftEase * 3f);        // avainasennot 0..3 (kuvat 1..4), nosto pehmeästi kiihtyen ja hidastuen
                if (stateTime >= grabLiftTime) Enter(State.GrabThrow);
                break;

            case State.GrabThrow:
                if (stateTime < ThrowSwing) { float k = stateTime / ThrowSwing; HoldPlayer(3f + k * k); }   // heilautus kiihtyy loppua kohti
                else if (!thrown)
                {
                    thrown = true;
                    float dir = (facingRight ? 1f : -1f) * (throwForward ? 1f : -1f);   // eteen tai selän taakse
                    player.Throw(dir * throwSpeed, throwUp, throwDamage);
                }
                if (stateTime >= ThrowSwing + 0.15f + 0.3f)
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

    const float ThrowSwing = 0.13f;
    /// Noston eteneminen 0..1: rauhallinen alku, vauhti keskellä, pieni pysähdys pään yllä ennen heilautusta.
    float LiftEase { get { float t = Mathf.Clamp01(stateTime / grabLiftTime); return t * t * (3f - 2f * t); } }
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
        if (throwForward) { artKeys[4] = new Vector3(1.3f, 2.0f, 0f); rotKeys[4] = new Vector3(1.6f, 2.4f, 60f); }   // heitto eteen: pelaaja lähtee käsistä edestä
        Vector3[] keys = art ? artKeys : rotKeys;
        int i = Mathf.Clamp(Mathf.FloorToInt(k), 0, keys.Length - 2);
        int pose = k < 0.5f ? 0 : k < 1f ? 1 : k < 2f ? 2 : 3;
        Vector3 v = Vector3.Lerp(keys[i], keys[i + 1], Mathf.Clamp01(k - i));
        float dir = facingRight ? 1f : -1f;
        Vector3 me = transform.position;
        player.SetHeld(new Vector3(me.x + dir * v.x, me.y - 0.05f, 0f), v.y, dir * v.z, pose);
    }

    bool BottleRun(float dt, Vector3 p, Vector3 me)
    {
        if (targetBottle != null && !targetBottle.CanPickUp) targetBottle = null;
        if (targetBottle == null && cooldown <= 0f && Mathf.Abs(p.x - me.x) > 2.2f && Random.value < bottleRate * dt)
        {
            float best = 5f;
            foreach (var b in Bottle.All)
            {
                if (b == null || !b.CanPickUp) continue;
                bool taken = false;
                foreach (var e in All) if (e != this && e.targetBottle == b) { taken = true; break; }
                if (taken) continue;
                float d = Vector2.Distance(b.transform.position, me);
                if (d < best) { best = d; targetBottle = b; }
            }
        }
        if (targetBottle == null) return false;
        if (Mathf.Abs(p.x - me.x) < 1.2f && Mathf.Abs(p.y - me.y) < depthTolerance) { targetBottle = null; return false; }   // pelaaja kimpussa: tappelee
        Vector3 bp = targetBottle.transform.position;
        float side = me.x <= bp.x ? -1f : 1f;
        Vector2 spot = new Vector2(bp.x + side * 0.7f, bp.y + 0.01f);   // käsi ylettyy pulloon (kuvat 2–3)
        Vector2 to = spot - (Vector2)me;
        if (to.magnitude < 0.15f)
        {
            facingRight = bp.x > me.x;
            if (targetBottle.TakeBy(this)) { heldBottle = targetBottle; moving = false; targetBottle = null; Enter(State.BottlePick); return true; }
            targetBottle = null;
            return false;
        }
        facingRight = to.x > 0f;
        moving = true;
        float stepX = Mathf.Min(Mathf.Abs(to.x), moveSpeedX * runSpeedMultiplier * dt);
        float stepY = Mathf.Min(Mathf.Abs(to.y), moveSpeedY * runSpeedMultiplier * dt);
        Move(new Vector2(Mathf.Sign(to.x) * stepX, Mathf.Sign(to.y) * stepY));
        animClock += dt * (runSpeedMultiplier - 1f);
        return true;
    }

    int BottleFrame()
    {
        bool pick = state == State.BottlePick;
        float[] times = pick ? ThrowPose.PickTimes : ThrowPose.PunkThrowTimes;
        int[] frames = pick ? ThrowPose.PunkPickFrames : ThrowPose.PunkThrowFrames;
        int i = ThrowPose.Index(times, stateTime);
        return frames[i < 0 ? frames.Length - 1 : i];
    }

    /// Pullo punkkarin käden takana (nosto- ja heittokuvat).
    public bool BottleGrip(out Vector3 hand, out float rot, out int order)
    {
        float dir = facingRight ? 1f : -1f;
        order = body != null ? body.sortingOrder - 1 : Mathf.RoundToInt(-transform.position.y * 100f) - 1;
        if (body == null || (state != State.BottlePick && state != State.BottleThrow)
            || !ThrowPose.Punk.TryGetValue(BottleFrame(), out var g)) { hand = Vector3.zero; rot = 0f; return false; }
        hand = body.transform.position + new Vector3(dir * g.hand.x, g.hand.y, 0f);
        rot = dir * g.rot;
        return true;
    }

    void HoldBarrel(float forward, float lift)
    {
        if (heldBarrel == null) return;
        Vector3 me = transform.position;
        heldBarrel.SetCarried(new Vector3(me.x + (facingRight ? forward : -forward), me.y - 0.01f, 0f), lift,
                              Mathf.RoundToInt(-me.y * 100f) + 2);
    }

    void DropBarrel()
    {
        if (heldBarrel != null) heldBarrel.Drop();
        heldBarrel = null;
    }

    /// Hyppyhyökkäyksen nousu: kaari vedon loppupuolelta iskun yli palautuksen alkuun.
    float AttackJumpLift()
    {
        if (!usingAlt || altJumpHeight <= 0f) return 0f;
        float W = CurrentWindup, P = Mathf.Max(punchActiveTime, altLungeTime), R = CurrentRecover;
        float e;
        if (state == State.Windup) e = stateTime - 0.5f * W;
        else if (state == State.Punch) e = 0.5f * W + stateTime;
        else if (state == State.Recover) e = 0.5f * W + P + stateTime;
        else return 0f;
        float J = 0.5f * W + P + 0.6f * R;
        if (e <= 0f || e >= J) return 0f;
        return Mathf.Sin(e / J * Mathf.PI) * altJumpHeight;
    }

    /// Laudalta tippuminen: lauta jatkaa matkaa omaan suuntaansa ja jää maahan.
    void DropBoard()
    {
        if (boardLost || looseBoardSprite == null) return;
        boardLost = true;
        LooseBoard.Spawn(looseBoardSprite, transform.position, lastMoveX * 6.5f, this);
    }

    /// Noustua jatketaan tappelua jalan (lauta jäi maahan).
    void GoOnFoot()
    {
        Sprite[] stance = Has(getUpSprites) ? new[] { getUpSprites[getUpSprites.Length - 1] } : idleSprites;
        idleSprites = Has(footIdleSprites) ? footIdleSprites : stance;
        walkSprites = Has(footWalkSprites) ? footWalkSprites : idleSprites;
        if (Has(footWalkSprites)) walkFrameTime = footWalkFrameTime;
        idleFrameTime = 0.12f;
        punchSprites = Has(footPunchSprites) ? footPunchSprites : Has(footKickSprites) ? footKickSprites : idleSprites;
        punchImpactFrame = Has(footPunchSprites) ? footPunchImpactFrame : Has(footKickSprites) ? footKickImpactFrame : 0;
        if (Has(footPunchSprites))
        {
            secondImpactFrame = footSecondImpactFrame;    // jab ja heti perään suora
            windupTime = footWindupTime; punchRecoverTime = footPunchRecoverTime;
        }
        if (Has(footHurtSprites)) hurtSprites = footHurtSprites;
        chargeRange = 0f;                            // syöksylyönti vain laudalla
        skatePass = false;
        if (Has(footKickSprites))
        {
            // hyppypotku: loikka eteen ja potku
            altAttackSprites = footKickSprites; altImpactFrame = footKickImpactFrame;
            altChance = Has(footPunchSprites) ? 0.5f : 1f;
            altLungeSpeed = footKickLunge; altLungeTime = 0.3f; altReach = 2.4f;
            altTimeScale = 1f; altExtraWindup = 0.1f;
            altJumpHeight = footKickJump;
        }
        else altChance = 0f;
        moveSpeedX = footMoveSpeedX; moveSpeedY = footMoveSpeedY;
    }

    void Enter(State s)
    {
        if (s == State.Airborne || s == State.Held) DropBoard();
        if (s == State.Punch) secondHitDone = false;
        if (s != State.BarrelLift && s != State.BarrelThrow) DropBarrel();   // osuma tms. keskeyttää: tynnyri putoaa
        if (s != State.BottlePick && s != State.BottleThrow && heldBottle != null) { heldBottle.Drop(); heldBottle = null; }
        if (s == State.BottleThrow) bottleThrown = false;
        if (s == State.GrabThrow) thrown = false;
        if (s != State.Down && s != State.Dead) { flipLanded = false; slamLanded = false; }
        state = s;
        stateTime = 0f;
        if (s != State.Chase) moving = false;
    }

    // ---------------- Tekoäly ----------------

    void Chase(float dt)
    {
        if (player == null) { moving = false; return; }
        if (UpdateEnemyTarget()) { ChaseEnemy(dt); return; }   // portsari tai kosto: toisen vihun kimppuun
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

        // tynnyri: pelaajan ollessa kaukana haetaan lähin tynnyri ja heitetään se
        if (throwsBarrels)
        {
            if (targetBarrel != null && !targetBarrel.CanPickUp) targetBarrel = null;
            if (targetBarrel == null && cooldown <= 0f && Mathf.Abs(p.x - me.x) > 3.5f && Random.value < barrelRate * dt)
            {
                float best = 6f;
                foreach (var c in Crate.All)
                {
                    if (c == null || c.breakable || !c.CanPickUp) continue;
                    float d = Vector2.Distance(c.transform.position, me);
                    if (d < best) { best = d; targetBarrel = c; }
                }
            }
            if (targetBarrel != null)
            {
                Vector3 b = targetBarrel.transform.position;
                float toPlayer = p.x > b.x ? 1f : -1f;
                Vector2 spot = new Vector2(b.x - toPlayer * 0.8f, b.y);   // tynnyrin taakse, pelaajasta katsoen
                Vector2 tb = spot - (Vector2)me;
                facingRight = b.x > me.x;
                if (tb.magnitude < 0.15f)
                {
                    targetBarrel.PickUp();
                    heldBarrel = targetBarrel;
                    targetBarrel = null;
                    moving = false;
                    Enter(State.BarrelLift);
                    return;
                }
                moving = true;
                float stepX = Mathf.Min(Mathf.Abs(tb.x), moveSpeedX * runSpeedMultiplier * dt);
                float stepY = Mathf.Min(Mathf.Abs(tb.y), moveSpeedY * runSpeedMultiplier * dt);
                Move(new Vector2(Mathf.Sign(tb.x) * stepX, Mathf.Sign(tb.y) * stepY));
                animClock += dt * (runSpeedMultiplier - 1f);
                return;
            }
        }

        // pullo: lattialla ehjä pullo lähellä ja pelaaja ei aivan vieressä -> haetaan, nostetaan ja heitetään
        if (HasBottleSprites && BottleRun(dt, p, me)) return;

        // liian kauan aivan vieressä: tarttuu heti (pomo)
        bool close = Mathf.Abs(p.x - me.x) <= grabRange + 0.3f && Mathf.Abs(p.y - me.y) <= depthTolerance;
        closeTimer = close ? closeTimer + dt : Mathf.Max(0f, closeTimer - dt);
        if (grabWhenCloseTime > 0f && closeTimer >= grabWhenCloseTime && Has(grabSprites) && !player.IsDown)
        {
            closeTimer = 0f;
            facingRight = p.x > me.x;
            if (CanGrabPlayer()) { moving = false; grabIntent = false; attackRolled = false; Enter(State.GrabReach); return; }
        }
        if (skatePass && !boardLost) { SkatePass(dt, p, me); return; }   // laudalla aina ohiajossa (ei vaihtelua jahtiin -> ei välkettä)

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
            if (Has(bellySprites) && bellySprites.Length >= 8 && Mathf.Abs(me.x - p.x) <= bellyRange && Random.value < bellyChance)
            { StartBelly(false); return; }
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

    /// Taklauksen juoksu (vauhdinotto ja liuku): tömisevät askeleet ja ruudun tärinä.
    void Stomp(float dt)
    {
        bool charging = usingAlt && altLungeSpeed > 0f
            && (state == State.Windup || (state == State.Punch && !punchLanded && stateTime < altLungeTime));
        bool hasStomp = stompSounds != null && stompSounds.Length > 0;
        if (!charging || (stompShake <= 0f && !hasStomp)) { stompTimer = 0f; return; }
        stompTimer -= dt;
        if (stompTimer > 0f) return;
        stompTimer = stompInterval * Random.Range(0.9f, 1.1f);
        if (hasStomp && audioSource != null) audioSource.PlayOneShot(stompSounds[Random.Range(0, stompSounds.Length)], stompVolume);
        if (stompShake > 0f && CameraFollow.Instance != null) CameraFollow.Shake(stompShake, 0.1f);
    }

    /// Skettari laudalla: kova vauhti pelaajan ohi, käännös toisella puolella, lyönti ohituksessa.
    void SkatePass(float dt, Vector3 p, Vector3 me)
    {
        if (passDir == 0) passDir = me.x < p.x ? 1 : -1;
        facingRight = passDir > 0;
        moving = true;
        float spd = moveSpeedX * runSpeedMultiplier;
        float dy = p.y - me.y;
        Move(new Vector2(passDir * spd * dt, Mathf.Clamp(dy, -moveSpeedY * dt, moveSpeedY * dt)));
        animClock += dt * (runSpeedMultiplier - 1f);
        float moved = Mathf.Abs(transform.position.x - me.x);
        float past = (me.x - p.x) * passDir;   // > 0: pelaajan ohi
        // käännös: vasta reilusti pelaajan ohi (tai ruudun reunassa ohi ajettua), ja enintään yksi käännös / passTurnInterval
        bool farPast = past >= passOvershoot;
        bool blocked = past > 1.5f && moved < spd * dt * 0.3f;
        if ((farPast || blocked) && Time.time - lastTurnTime >= passTurnInterval)
        {
            passDir = -passDir;
            lastTurnTime = Time.time;
        }
        // lyönti ohituksessa: pelaaja edessä lyöntietäisyydellä samalla syvyydellä
        float ahead = -past;
        if (attackRank <= 1 && cooldown <= 0f && retreatTimer <= 0f && !player.IsDown && ahead > 0.4f && ahead < attackRange + 0.8f && Mathf.Abs(dy) <= depthTolerance)
        {
            moving = false;
            attackRolled = false;
            usingAlt = Has(altAttackSprites);   // syöksylyönti: vauhti jatkuu iskussa
            Enter(State.Windup);
        }
    }

    void StartBelly(bool counter)
    {
        moving = false;
        bellyHit = false; bellyLaughed = false;
        if (player != null) facingRight = player.transform.position.x > transform.position.x;
        Enter(State.Belly);
        if (counter) stateTime = bellyPumpFrameTime;   // vastaisku alkaa suoraan latauksesta
    }

    void TryBellyHit()
    {
        if (player == null) return;
        Vector3 p = player.transform.position, me = transform.position;
        float dx = p.x - me.x;
        bool front = facingRight ? dx >= -0.3f : dx <= 0.3f;
        if (!front || Mathf.Abs(dx) > bellyRange + 0.3f || Mathf.Abs(p.y - me.y) > depthTolerance) return;
        if (player.AirHeight > 0.9f) return;
        player.TakeKnockdown(bellyDamage, me.x, bellyKnockSpeed, bellyKnockUp, this);
    }

    // ---------------- Vihut toisiaan vastaan ----------------

    bool TargetDown(Enemy e) => e.state == State.Down || e.state == State.GetUp || e.state == State.Dead || e.state == State.Held;

    /// Valitsee kohteen: portsari lähimmän (pelaaja tai muu kuin portsari), muut vain kostavat lyöjälleen hetken.
    bool UpdateEnemyTarget()
    {
        if (enemyTarget != null && (!enemyTarget.isActiveAndEnabled || enemyTarget.IsDead || (!fightsEveryone && Time.time > grudgeUntil)))
            enemyTarget = null;
        if (fightsEveryone && Time.time >= retargetTime)
        {
            retargetTime = Time.time + 0.6f;
            Vector3 me = transform.position;
            Vector3 pp = player.transform.position;
            float best = player.IsDown ? float.MaxValue : Mathf.Abs(pp.x - me.x) + Mathf.Abs(pp.y - me.y) * 2f;
            Enemy pick = null;
            foreach (var e in All)
            {
                if (e == this || e.fightsEveryone || e.IsDead || TargetDown(e)) continue;
                Vector3 q = e.transform.position;
                if (Mathf.Abs(q.x - me.x) > 12f) continue;
                float d = Mathf.Abs(q.x - me.x) + Mathf.Abs(q.y - me.y) * 2f;
                if (d < best - 0.5f) { best = d; pick = e; }
            }
            if (Time.time > grudgeUntil || enemyTarget == null) enemyTarget = pick;
        }
        return enemyTarget != null;
    }

    void ChaseEnemy(float dt)
    {
        Vector3 t = enemyTarget.transform.position, me = transform.position;
        float side = me.x >= t.x ? 1f : -1f;
        Vector2 to = new Vector2(t.x + side * attackRange * 0.8f, t.y) - (Vector2)me;
        facingRight = t.x > me.x;
        bool inRange = Mathf.Abs(me.x - t.x) <= attackRange && Mathf.Abs(me.y - t.y) <= depthTolerance;
        if (inRange && cooldown <= 0f && !TargetDown(enemyTarget))
        {
            moving = false;
            attackRolled = false;
            usingAlt = Has(altAttackSprites) && Random.value < altChance;
            Enter(State.Windup);
            return;
        }
        if (to.magnitude > 0.08f)
        {
            moving = true;
            float run = to.magnitude > 3.5f ? runSpeedMultiplier : 1f;
            if (run > 1f) animClock += dt * (run - 1f);
            Vector2 dir = to.normalized;
            Move(new Vector2(dir.x * moveSpeedX, dir.y * moveSpeedY) * run * dt);
        }
        else moving = false;
    }

    bool TryHitEnemy(Enemy e)
    {
        Vector3 p = e.transform.position, me = transform.position;
        float dx = p.x - me.x;
        bool front = facingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > CurrentReach + 0.2f || Mathf.Abs(p.y - me.y) > depthTolerance) return false;
        bool kd = usingAlt ? altKnockdown : punchKnockdown;
        if (!e.TakeHit(usingAlt ? altDamage : punchDamage, me.x, kd)) return false;
        e.GotHitBy(this);
        HitFx.OnHit(kd);
        HitSpark.Spawn(new Vector3(p.x, p.y + 2.3f, 0f), kd, Mathf.RoundToInt(-p.y * 100f) + 5, e.JustBlocked);
        return true;
    }

    /// Toinen vihu löi: kosto hetkeksi (portsari vaihtaa kohteen lyöjään).
    void GotHitBy(Enemy a)
    {
        if (a == null || a == this) return;
        if (fightsEveryone && a.fightsEveryone) return;
        enemyTarget = a;
        grudgeUntil = Time.time + 5f;
        retargetTime = Time.time + 1.5f;
    }

    bool TryHitPlayer(bool comboFollow = false)
    {
        if (enemyTarget != null) return TryHitEnemy(enemyTarget);
        if (player == null) return false;
        Vector3 p = player.transform.position, me = transform.position;
        float dx = p.x - me.x;
        bool front = facingRight ? dx >= -0.2f : dx <= 0.2f;
        if (!front || Mathf.Abs(dx) > CurrentReach + 0.2f) return false;
        if (Mathf.Abs(p.y - me.y) > depthTolerance) return false;
        if (player.AirHeight > 0.9f) return false;   // hypyllä voi väistää
        if (usingAlt && altKnockdown) return player.TakeKnockdown(altDamage, me.x, altKnockSpeed, altKnockUp, this);
        if (!usingAlt && punchKnockdown) return player.TakeKnockdown(punchDamage, me.x, 3.5f, 4.5f, this);
        return player.TakeHit(usingAlt ? altDamage : punchDamage, me.x, this, comboFollow);
    }

    // ---------------- Osumat ----------------

    /// Pelaajan isku osuu. attackerX = lyöjän sijainti (mistä suunnasta isku tulee).
    public bool TakeHit(int damage, float attackerX, bool knockdown)
    {
        if (state == State.Down || state == State.GetUp || state == State.Dead) return false;
        if (state == State.GrabLift || state == State.GrabThrow || state == State.Held) return false;   // heiton aikana ei keskeytetä
        if (state == State.Airborne && height > 0.1f && !knockdown) return false;

        // vatsatöytäisy: iskuun asti maha ottaa iskut vastaan (torjunta); naurun aikana saa osua
        if (state == State.Belly && stateTime < BellyImpactTime)
        {
            JustBlocked = true;
            shakeUntil = HitFx.ShakeUntil(false);
            return true;
        }
        // kombon keskellä: torjuu ja vastaa vatsatöytäisyllä
        if (Time.time - lastHitTime > 0.9f) comboHits = 0;
        if (bellyCounterChance > 0f && Has(bellySprites) && bellySprites.Length >= 8 && state == State.Hurt && !knockdown
            && comboHits >= bellyCounterAfterHits && Random.value < bellyCounterChance)
        {
            comboHits = 0;
            JustBlocked = true;
            shakeUntil = HitFx.ShakeUntil(false);
            StartBelly(true);
            return true;
        }
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
        comboHits++; lastHitTime = Time.time;
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
    bool flightArt;         // niskalenkin lento omilla lentokuvilla (headlockFlightSprites)
    bool slamLanded;        // paiskattu maahan: isku, pomppu ja makuu lentokuvista
    public bool HasFlightArt => FlightSet != null && FlightSet.Length >= 6;
    const float SlamImpactTime = 0.12f, SlamBounceTime = 0.26f;
    static AudioClip slamSound; static bool slamLoaded;
    static AudioClip SlamSound { get { if (!slamLoaded) { slamSound = Resources.Load<AudioClip>("Sfx/paiskaus"); slamLoaded = true; } return slamSound; } }
    int heldPose = -1;      // mikä omista kuvista näytetään otteessa (-1 = osumakuva + kierto)
    const float ArtSpinTime = 0.1f;

    // Käytössä oleva kuvasarja ja sen rakenne (kuperkeikka tai niskalenkki)
    Sprite[] artSet;
    int artSpinPose, artFlightPose, artLandFirst;
    float artSpinTarget, artSpinRate, artLandFrameTime;

    public bool HasFlipArt => flipThrownSprites != null && flipThrownSprites.Length >= 6 + Mathf.Max(1, flipFlightFrames);   // ote 3 + lento + alastulo ≥ 3
    public bool HasHeadlockArt => headlockThrownSprites != null && headlockThrownSprites.Length >= 8;
    /// Onko vastuksella omat kuvat tähän heittoon.
    public bool HasArtFor(bool monkeyFlip) => monkeyFlip ? HasFlipArt : HasHeadlockArt;
    float spinRot;          // kierto (astetta, maailman z), kun pelaaja pitää tai heittää
    float spinCenter;       // kiertopisteen korkeus jaloista (yksikköä)

    /// Voiko pelaaja napata kiinni (vain kesken lyönnin, ei heiton tai kaatuneena).
    public bool CanBeCaught => state == State.Punch;

    /// Pelaaja nappaa lyövästä kädestä kiinni.
    public void BeginHeldByPlayer(float playerX, bool monkeyFlip = false, bool knee = false)
    {
        artSet = null;
        kneeMode = knee && HasKneeArt;
        if (monkeyFlip && HasFlipArt)
        {
            // kuperkeikka: nosto (2) kiertyy puolikkaan, lento (3) suorana, alastulo 4–7
            artSet = flipThrownSprites;
            artSpinPose = 2; artFlightPose = 3; artSpinTarget = 180f; artSpinRate = 700f;
            artLandFirst = 3 + Mathf.Max(1, flipFlightFrames); artLandFrameTime = flipLandFrameTime;
            if (flipFlightFrames > 1) artSpinTarget = 0f;   // pyöriminen piirretty kuviin: ei kierretä
        }
        else if (!monkeyFlip && HasHeadlockArt)
        {
            // niskalenkki: ylösalaisin-kuva (5) kiertyy lennon aikana selälleen päin, alastulo 6–7
            artSet = headlockThrownSprites;
            artSpinPose = 5; artFlightPose = -1; artSpinTarget = 45f; artSpinRate = 300f;
            artLandFirst = 6; artLandFrameTime = 0.12f;
        }
        flightArt = !monkeyFlip && HasFlightArt;
        if (kneeMode)
        {
            // polvi päähän: otteen kuvat pelaajan ohjaamina, sitten oma lento ja alastulo
            artSet = kneeHeldSprites;
            artSpinPose = kneeHeldSprites.Length - 1; artFlightPose = -1; artSpinTarget = 0f; artSpinRate = 0f;
            flightArt = true;
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

    int lastAttackSound = -1;
    void PlayAttackSound()
    {
        if (attackSounds == null || attackSounds.Length == 0 || audioSource == null) return;
        int i = Random.Range(0, attackSounds.Length);
        if (attackSounds.Length > 1 && i == lastAttackSound) i = (i + 1) % attackSounds.Length;
        lastAttackSound = i;
        if (attackSounds[i] == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(attackSounds[i], attackVolume);
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
        if (Mathf.Abs(delta.x) > 0.0001f && (state == State.Chase || state == State.Punch)) lastMoveX = Mathf.Sign(delta.x);
        Vector3 p = transform.position;
        float minY = player != null ? player.minDepthY : -4.3f;
        float maxY = player != null ? player.maxDepthY : -0.8f;
        p.x += delta.x;
        p.y = Mathf.Clamp(p.y + delta.y, minY, maxY);
        // pelaajan alueella pysytään taustakuvan sisällä (esim. taklaus ei liu'u katolta yli)
        var cf = CameraFollow.Instance; var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - 0.5f;
            float lo = cf.minX - halfW, hi = cf.maxX + halfW;
            if (p.x >= lo - 3f && p.x <= hi + 3f) p.x = Mathf.Clamp(p.x, lo, hi);
        }
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
        float bounce = AttackJumpLift();
        if (state == State.Down && slamLanded && stateTime >= SlamImpactTime && stateTime < SlamImpactTime + SlamBounceTime)
            bounce = Mathf.Sin((stateTime - SlamImpactTime) / SlamBounceTime * Mathf.PI) * 0.3f;   // pomppu iskun jälkeen
        body.transform.localPosition = new Vector3(pivotFix.x + shake, groundHeight + height + bounce - footOffset + pivotFix.y, 0f);
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

            case State.BottlePick:
            case State.BottleThrow:
                if (HasBottleSprites) return bottleSprites[BottleFrame()];
                return IdleFrame();

            case State.BarrelLift:
                if (Has(grabSprites) && grabSprites.Length >= 8) return grabSprites[stateTime < 0.2f ? 1 : stateTime < 0.45f ? 2 : 3];
                return IdleFrame();

            case State.BarrelThrow:
                if (Has(grabSprites) && grabSprites.Length >= 8) return grabSprites[stateTime < 0.15f ? 4 : stateTime < 0.3f ? 5 : 7];
                return IdleFrame();

            case State.Belly:
                if (Has(bellySprites) && bellySprites.Length >= 8)
                {
                    int i = stateTime < BellyPumpTime
                        ? BellyPump[Mathf.Min((int)(stateTime / bellyPumpFrameTime), BellyPump.Length - 1)]
                        : BellyLaugh[Mathf.Min((int)((stateTime - BellyPumpTime) / bellyLaughFrameTime), BellyLaugh.Length - 1)];
                    return bellySprites[i];
                }
                return IdleFrame();

            case State.Block:
                if (Has(blockSprites)) return blockSprites[Mathf.Min((int)(stateTime / blockTime * blockSprites.Length), blockSprites.Length - 1)];
                return FirstIdle();

            case State.Hurt:
                if (Has(hurtSprites)) return hurtSprites[Mathf.Min((int)(stateTime / hurtTime * hurtSprites.Length), hurtSprites.Length - 1)];
                return FirstIdle();

            case State.Airborne:
                if (thrownByPlayer && flightArt)
                {
                    // 0 ylösalaisin irrotessa, 1 lento, 2 juuri ennen maata
                    int fi = stateTime < 0.1f ? 0 : height > 0.9f ? 1 : 2;
                    return FlightSet[fi];
                }
                if (thrownByPlayer && artThrow)
                {
                    if (artFlightPose < 0 || stateTime < ArtSpinTime) return artSet[artSpinPose];
                    int nf = artSet == flipThrownSprites ? Mathf.Max(1, flipFlightFrames) : 1;
                    int fi = Mathf.Min((int)((stateTime - ArtSpinTime) / Mathf.Max(0.01f, flipFlightFrameTime)), nf - 1);
                    return artSet[Mathf.Min(artFlightPose + fi, artSet.Length - 1)];
                }
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
                if (Has(landSprites) && !slamLanded && !flipLanded)
                    return landSprites[Mathf.Min((int)((state == State.Down ? stateTime : 99f) / landFrameTime), landSprites.Length - 1)];
                if (slamLanded && HasFlightArt)
                {
                    var fs = FlightSet;
                    if (state == State.Down && stateTime < SlamImpactTime) return fs[3];               // isku
                    if (state == State.Down && stateTime < SlamImpactTime + SlamBounceTime) return fs[4]; // pomppu
                    if (state != State.Down) return fs[fs.Length - 1];
                    // pidempi sarja: loput kuvat (kierähdys) tasaisesti, viimeinen jää
                    return fs[Mathf.Min(5 + (int)((stateTime - SlamImpactTime - SlamBounceTime) / 0.1f), fs.Length - 1)];
                }
                if (flipLanded && artSet != null)
                    return artSet[Mathf.Min(artLandFirst + (int)(stateTime / artLandFrameTime), artSet.Length - 1)];
                if (Has(knockdownSprites)) return knockdownSprites[knockdownSprites.Length - 1];
                rot = 90f;
                return FirstIdle();

            case State.GrabReach:
                return Has(grabSprites) ? grabSprites[0] : IdleFrame();

            case State.GrabLift:
                if (Has(grabSprites)) return grabSprites[Mathf.Clamp(1 + (int)(LiftEase * 4f), 1, 4)];
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
