using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Beat 'em up -pelaaja.
/// Liikkuu kadun tasolla (x = vasen/oikea, y = syvyys), hyppää erillisellä korkeusakselilla
/// ja soittaa ruutuanimaatioita leikatuista sprite sheeteistä.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Spritet (raahaa kaikki leikatut spritet, järjestys korjataan automaattisesti)")]
    public Sprite[] idleSprites;     // idle.png: 6 kuvaa
    public Sprite[] actionSprites;   // hahmo_spritesheet.png: 19 kuvaa

    [Header("Viittaukset")]
    public SpriteRenderer body;      // lapsi "Visual"
    public SpriteRenderer shadow;    // lapsi "Shadow" (varjo luodaan koodilla jos tyhjä)

    [Header("Liike")]
    public float moveSpeedX = 3.5f;
    public float moveSpeedY = 2f;
    public float minDepthY = -3.5f;
    public float maxDepthY = -0.5f;
    [Tooltip("Pois päältä = ukko liukuu idle-animaatiolla. Päällä = käytetään juoksun 3 kuvaa.")]
    public bool useRunFramesWhenMoving = false;
    [Tooltip("Kävelyn kuvat (kavely.png). Jos tyhjä, käytetään yllä olevaa valintaa.")]
    public Sprite[] walkSprites;
    public float walkFrameTime = 0.11f;

    [Header("Juoksu (Shift / ohjaimen RB)")]
    [Tooltip("Juoksun kuvat (juoksu.png).")]
    public Sprite[] runSprites;
    public float runSpriteFrameTime = 0.075f;
    [Tooltip("Kuinka monta kertaa kävelyä nopeammin juostaan sivusuunnassa.")]
    public float runSpeedMultiplier = 2f;
    [Tooltip("Syvyyssuunnan nopeuskerroin juostessa.")]
    public float runDepthMultiplier = 1.2f;

    [Header("Jalkakäytävä (kynnys)")]
    [Tooltip("Onko kentässä korotettu jalkakäytävä.")]
    public bool useSidewalk = true;
    [Tooltip("Syvyys (y), josta jalkakäytävä alkaa: tätä ylempänä (seinän puolella) ollaan jalkakäytävällä.")]
    public float curbDepthY = -1.20f;
    [Tooltip("Kuinka paljon jalkakäytävä on ajorataa korkeammalla (yksikköä).")]
    public float sidewalkHeight = 0.42f;
    [Tooltip("Kuinka nopeasti kynnyksen yli astutaan (sekuntia).")]
    public float stepTime = 0.07f;

    [Header("Hyppy")]
    [Tooltip("Hypyn lähtönopeus. Korkeus = nopeus² / (2 × painovoima): 12.5 → n. 2.6 yksikköä.")]
    public float jumpVelocity = 12.5f;
    public float gravity = 30f;
    public float jumpSquatTime = 0.08f;
    public float landingTime = 0.14f;

    [Header("Hyökkäykset")]
    public float kickTime = 0.38f;

    [Header("Lyöntikombo (hakkaa lyöntinappia)")]
    [Tooltip("Kombon iskut järjestyksessä. Täytä valikosta Beat em up → 3. Päivitä lyöntikombo.")]
    public ComboHit[] punchCombo =
    {
        new ComboHit { name = "Jab",       windupTime = 0.04f, frame = 8, duration = 0.16f, lunge = 0.18f },
        new ComboHit { name = "Takasuora", windupTime = 0.05f, frame = 8, duration = 0.20f, lunge = 0.26f },
        new ComboHit { name = "Jab",       windupTime = 0.04f, frame = 8, duration = 0.16f, lunge = 0.18f },
        new ComboHit { name = "Uppercut",  windupTime = 0.06f, frame = 8, duration = 0.30f, lunge = 0.30f, knockdown = true },
    };
    [Tooltip("Kuinka aikaisin iskun aikana seuraava painallus jo hyväksytään (0 = heti, 1 = vasta lopussa).")]
    [Range(0f, 1f)] public float comboInputFrom = 0.25f;
    [Tooltip("Pieni tauko kombon viimeisen iskun jälkeen ennen kuin voi taas liikkua.")]
    public float comboRecovery = 0.12f;

    [Header("Kestävyys")]
    public int maxHealth = 100;
    [HideInInspector] public int health;

    [Header("Stamina")]
    public float maxStamina = 100f;
    [HideInInspector] public float stamina;
    [Tooltip("Hyppy (myös saksipotkun hyppy) kuluttaa tämän verran.")]
    public float jumpStamina = 12f;
    [Tooltip("Erikoisliike (tuulimylly) kuluttaa tämän verran.")]
    public float specialStamina = 30f;
    [Tooltip("Pusku kuluttaa tämän verran.")]
    public float pushStamina = 15f;
    [Tooltip("Laatikon tai tynnyrin heitto kuluttaa tämän verran (heitto onnistuu aina, stamina voi mennä nollaan).")]
    public float throwStamina = 15f;
    [Tooltip("Juoksu kuluttaa sekunnissa.")]
    public float runStaminaPerSecond = 14f;
    [Tooltip("Palautuu sekunnissa (hitaasti), kun staminaa ei ole hetkeen käytetty.")]
    public float staminaRegenPerSecond = 3f;
    public float staminaRegenWait = 1.5f;
    float staminaRest;
    /// Milloin viimeksi yritettiin liikettä ilman staminaa (HUD vilkuttaa mittaria).
    public float StaminaEmptyTime { get; private set; } = -10f;

    /// Kuluttaa staminaa, jos sitä on tarpeeksi. Palauttaa, onnistuiko.
    public bool UseStamina(float cost)
    {
        if (stamina < cost) { StaminaEmptyTime = Time.time; return false; }
        stamina -= cost;
        staminaRest = staminaRegenWait;
        return true;
    }

    /// Staminan lisäys (Sohvin tuotteet, energiajuoma). Palauttaa todellisen lisäyksen.
    public int AddStamina(float amount)
    {
        float before = stamina;
        stamina = Mathf.Min(maxStamina, stamina + amount);
        return Mathf.RoundToInt(stamina - before);
    }

    void UpdateStamina(float dt)
    {
        if (running)
        {
            stamina = Mathf.Max(0f, stamina - runStaminaPerSecond * dt);
            staminaRest = staminaRegenWait;
            return;
        }
        if (staminaRest > 0f) { staminaRest -= dt; return; }
        stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * dt);
    }
    [Tooltip("Elämiä pelin alussa. Kun energia loppuu, menee yksi elämä ja energia täyttyy.")]
    public int lives = 3;
    [Tooltip("Rahat (markat): vihollisista putoaa, klubin baarissa voi ostaa.")]
    public int money;
    [Tooltip("Suoja-aika (s) elämän menettämisen jälkeen.")]
    public float respawnInvulnerable = 2.5f;
    public float hurtTime = 0.35f;
    [Tooltip("Pelaajan omat kipuäänet (valinnainen).")]
    public AudioClip[] hurtSounds;

    [Header("Potkujen osumat")]
    public int kickDamage = 10;
    public float kickReach = 1.9f;
    public int jumpKickDamage = 12;
    public float jumpKickReach = 1.9f;
    [Tooltip("Kuinka lähellä syvyyssuunnassa vihollisen pitää olla, jotta isku osuu.")]
    public float attackDepth = 0.4f;

    [Header("Saksipotku (hyppy + K): kaksi potkua ilmassa vuorojaloin")]
    [Tooltip("saksipotku.png: 8 kuvaa (0 asento, 1 ponnistus, 2 nousu, 3 1. potku, 4 vaihto, 5 2. potku, 6 lasku, 7 asento).")]
    public Sprite[] scissorSprites;
    public float scissorFrameTime = 0.06f;
    [Tooltip("Kuinka kauan kumpaakin potkua pidetään (osuma-aika).")]
    public float scissorKickHold = 0.12f;
    public int scissorDamage1 = 8;
    public int scissorDamage2 = 12;
    public float scissorReach = 2.1f;
    [Tooltip("Painovoiman kerroin potkujen aikana: ukko leijuu hetken, jotta molemmat potkut ehtivät.")]
    [Range(0.1f, 1f)] public float scissorGravityScale = 0.5f;
    [Tooltip("Ponnistuksen nopeus maasta (ylös, alas, K, K). 8 = n. 1.6 yksikön loikka; tavallinen hyppy on Jump Velocity.")]
    public float scissorJumpVelocity = 8f;
    [Tooltip("Maasta: ylös, alas, K, K. Aikaikkuna ylös→alas ja alas→potkut (s).")]
    public float scissorInputWindow = 0.45f;
    bool scissor;           // saksipotku käynnissä ilmassa
    bool scissorJump;       // ponnistus maasta saksipotkuun (ylös, alas, K, K)
    bool landedFromScissor;
    float lastUpTime = -10f, upDownTime = -10f, scissorArmTime = -10f;
    float prevMoveY;
    bool scissorArmed;      // ylös-alas tehty ja ensimmäinen K painettu: toinen K laukaisee
    float scissorTime;
    bool scissorHit1, scissorHit2;

    [Header("Sivupotku (etupotku, potkukombon keskimmäinen)")]
    public Sprite[] sideKickSprites;
    public float sideKickFrameTime = 0.06f;
    [Tooltip("Kuva, jossa jalka on ojennettuna (0 = ensimmäinen).")]
    public int sideKickImpactFrame = 3;
    [Tooltip("Kuinka kauan ojennettu jalka pidetään (osuma-aika).")]
    public float sideKickImpactHold = 0.14f;
    public int sideKickDamage = 12;
    public float sideKickReach = 2.5f;
    [Tooltip("Sheetissä kuvat on siirretty vasemmalle, jotta potku mahtuu ruutuun (yksikköä); korjataan tässä.")]
    public float sideKickArtOffset = 0.43f;

    [Header("Potkukombo (hakkaa K): matala potku → etupotku (sivupotku) → korkea potku")]
    [Tooltip("korkea_potku.png: 6 kuvaa (0 asento, 1–2 nosto, 3 potku, 4 lasku, 5 asento).")]
    public Sprite[] hiKickSprites;
    public float hiKickFrameTime = 0.05f;
    public int hiKickImpactFrame = 3;
    public float hiKickImpactHold = 0.14f;
    public int hiKickDamage = 10;
    public float hiKickReach = 2.1f;
    [Tooltip("Sheetissä kuvat on siirretty vasemmalle, jotta potku mahtuu ruutuun (yksikköä); korjataan tässä.")]
    public float hiKickArtOffset = 0.64f;
    [Tooltip("Kaataako kombon viimeinen potku (korkea potku) vihollisen.")]
    public bool kickFinisherKnockdown = true;
    [Header("Liuku eteenpäin iskun aikana (yksikköä), antaa iskuille painoa")]
    public float hiKickLunge = 0.22f;
    public float sideKickLunge = 0.3f;
    public float lowKickLunge = 0.2f;
    int kickComboIndex;     // 0 matala, 1 etupotku, 2 korkea
    bool kickQueued;

    [Header("Flurry: J, J, K, K (iskut katkaistaan heti osuman jälkeen)")]
    [Tooltip("Monennesta lyönnistä alkaen potku ketjuttuu flurryksi (1 = toinen lyönti, eli J, J, K).")]
    public int flurryFromPunch = 1;
    [Tooltip("Flurryssa isku katkaistaan näin pian osumakuvan alun jälkeen (s), palautusta ei odoteta.")]
    public float flurryCancelAfterImpact = 0.06f;
    bool flurry;            // ketju käynnissä: lyönneistä potkuihin, korkea potku ilman nostoa
    bool flurryKickQueued;  // lyönnin aikana painettiin potkua

    [Header("Erikoisliike: pyörähdyspotku (L / ohjaimen LB), osuu joka suuntaan")]
    public Sprite[] specialSprites;
    public float specialFrameTime = 0.055f;
    public int specialDamage = 16;
    [Tooltip("Tuulimylly: osuu uudestaan tämän välein (s). 0 = vain kerran.")]
    public float specialHitEvery = 0f;
    int specialRound = -1;
    [Tooltip("Ulottuvuus molempiin suuntiin (yksikköä).")]
    public float specialReach = 2.4f;
    [Tooltip("Aikaikkuna, jolloin potkut osuvat (s liikkeen alusta): ensin taakse, sitten eteen.")]
    public float specialHitFrom = 0.30f;
    public float specialHitTo = 0.88f;

    [Header("Pusku (U / ohjaimen RT, tai lyönti suojauksesta): työntää vihollisen kumoon")]
    [Tooltip("pusku.png: 6 kuvaa (0 asento, 1–2 vauhti, 3 pusku, 4–5 paluu).")]
    public Sprite[] pushSprites;
    public float pushFrameTime = 0.06f;
    [Tooltip("Kuva, jossa olkapää osuu (0 = ensimmäinen).")]
    public int pushImpactFrame = 3;
    [Tooltip("Kuinka kauan osumakuvaa pidetään.")]
    public float pushImpactHold = 0.14f;
    public int pushDamage = 8;
    public float pushReach = 1.6f;
    [Tooltip("Kuinka paljon ukko syöksyy eteenpäin puskun aikana (yksikköä).")]
    public float pushLunge = 0.45f;

    [Header("Suojaus (pidä I / ohjaimen LT)")]
    [Tooltip("suojaus.png: 5 kuvaa (0 asento, 1 nosto, 2 suoja, 3 lasku, 4 asento).")]
    public Sprite[] blockSprites;
    [Tooltip("Kädet nousevat suojaan (s). Suoja on voimassa heti painalluksesta.")]
    public float blockRaiseTime = 0.06f;
    [Tooltip("Aika per kuva, kun kädet lasketaan.")]
    public float blockLowerFrameTime = 0.06f;
    [Tooltip("Kuinka kauan torjuttu isku pitää suojassa (s).")]
    public float blockStunTime = 0.2f;
    [Tooltip("Torjutun iskun työntö taaksepäin (yksikköä/s).")]
    public float blockPushback = 1.8f;
    [Tooltip("Osuus vahingosta, joka menee suojan läpi (0 = ei mitään).")]
    [Range(0f, 1f)] public float blockDamageFactor = 0f;
    bool blockReleasing;
    float blockReleaseTime, blockStun;

    [Header("Vastaheitto (O / ohjaimen oikean tatin painallus): nappaa lyövästä kädestä ja heittää")]
    [Tooltip("heitto.png: 8 kuvaa (0 kurotus, 1 ote, 2 veto, 3 kädet ristissä, 4 olan yli, 5 heitto, 6 jälkiliike, 7 asento).")]
    public Sprite[] counterThrowSprites;
    [Tooltip("Kuinka kauan kurotus nappaa kiinni (s). Painettava juuri ennen kuin lyönti osuu.")]
    public float catchWindowTime = 0.5f;
    [Tooltip("Ohi menneen kurotuksen palautus (s), jonka aikana olet altis.")]
    public float catchMissRecovery = 0.25f;
    [Tooltip("Aika per heittokuva.")]
    public float counterThrowFrameTime = 0.11f;
    [Tooltip("Kuinka kauan jälkiliikettä pidetään heiton jälkeen.")]
    public float counterThrowEndHold = 0.3f;
    public int counterThrowDamage = 20;
    [Tooltip("Lentonopeus selän taakse (yksikköä/s).")]
    public float counterThrowSpeed = 7f;
    [Tooltip("Niskalenkin lennon nousunopeus (pieni = matala ja nopea isku maahan).")]
    public float counterThrowUp = 1.5f;
    [Tooltip("Niskalenkissä hero liukuu heiton aikana näin paljon eteenpäin (yksikköä).")]
    public float counterThrowSlide = 0.8f;

    [Header("Kuperkeikkaheitto isoille vastuksille (Kovis): sama nappi, valitaan automaattisesti")]
    [Tooltip("kuperkeikka.png: 8 kuvaa (0 ote, 1 kyykky, 2 istahdus, 3 selälleen, 4 jalat vatsaan, 5 potku pään yli, 6–7 makaa). Lopuksi kip-up.")]
    public Sprite[] monkeyFlipSprites;
    public float monkeyFlipFrameTime = 0.1f;
    [Tooltip("Kuinka kauan maataan viimeisessä kuvassa ennen kip-upia.")]
    public float monkeyFlipEndHold = 0.25f;
    public int monkeyFlipDamage = 26;
    [Tooltip("Lentonopeus pään yli taakse (yksikköä/s).")]
    public float monkeyFlipSpeed = 13f;
    [Tooltip("Lennon nousunopeus ylöspäin (suurempi = korkeampi ja pidempi kaari).")]
    public float monkeyFlipUp = 6f;
    Enemy heldEnemy;
    bool counterReleased;
    bool monkeyFlip;     // käynnissä oleva vastaheitto on kuperkeikka
    bool kipUpAfterOwnThrow;   // kip-up kuperkeikan jälkeen: ei suoja-aikaa eikä välkettä

    [Header("Laatikon nosto ja heitto (O laatikon vieressä nostaa, lyönti/potku/O heittää)")]
    [Tooltip("nosto_heitto.png: 6 kuvaa (0 kyykky, 1 nousu, 2 pään yllä, 3 veto taakse, 4 heitto, 5 asento).")]
    public Sprite[] carrySprites;
    [Tooltip("Kävely laatikko pään yllä (kanto_kavely.png). Käyttää kävelyn tahtia (Walk Frame Time). Jos tyhjä, liukuu kantoasennossa.")]
    public Sprite[] carryWalkSprites;
    [Tooltip("Kantoasento seisoessa (kanto.png), jos täyttä nosto/heitto-sarjaa ei ole.")]
    public Sprite carryPoseSprite;
    [Tooltip("Laatikon keinunta askelten tahdissa kävellessä (yksikköä).")]
    public float carryBob = 0.06f;
    public float liftTime = 0.3f;
    [Tooltip("Kävelyn nopeuskerroin laatikkoa kantaessa.")]
    public float carrySpeedFactor = 0.75f;
    [Tooltip("Laatikon korkeus pään yllä (yksikköä maasta).")]
    public float carryHeight = 3.45f;
    public float crateThrowTime = 0.32f;
    public float crateThrowSpeed = 9f;
    public float crateThrowUp = 3f;
    Crate carried;
    bool crateReleased;

    [Header("Äänet")]
    [Tooltip("Iskujen gruntit (grunt1–grunt6). Täytä valikosta Beat em up → 6. Päivitä äänet.")]
    public AudioClip[] attackGrunts;
    [Range(0f, 1f)] public float gruntVolume = 0.9f;
    [Tooltip("Satunnainen sävelkorkeuden vaihtelu, ettei sama ääni toistu samanlaisena.")]
    [Range(0f, 0.3f)] public float gruntPitchVariation = 0.06f;

    [Header("Animaation nopeus")]
    public float idleFrameTime = 0.15f;
    public float runFrameTime = 0.11f;

    [Header("Spriten sijoitus")]
    [Tooltip("Ruudussa on 8 px tyhjää maiharien alla (PPU 100 = 0.08 yksikköä).")]
    public float footOffset = 0.08f;

    // hahmo_spritesheet.png -ruutujen numerot
    const int F_STAND = 0, F_RUN1 = 1, F_CROUCH = 4, F_JUMP_UP = 5, F_JUMP_TOP = 6, F_JUMP_DOWN = 7;
    const int F_PUNCH = 8, F_HURT = 9, F_KNEE = 10, F_KICK = 11, F_LAND1 = 12, F_LAND2 = 13, F_JUMPKICK1 = 14;

    [Serializable]
    public class ComboHit
    {
        public string name;

        [Header("Oma animaatio (jos tyhjä, käytetään alla olevaa yhtä kuvaa)")]
        public Sprite[] sprites;
        [Tooltip("Aika per kuva, paitsi osumakuva.")]
        public float frameTime = 0.05f;
        [Tooltip("Monesko kuva (0 = ensimmäinen) on osumahetki, jota pidetään pidempään.")]
        public int impactFrame = 2;
        [Tooltip("Kuinka kauan osumakuvaa näytetään.")]
        public float impactHold = 0.10f;

        [Header("Varakuva hahmo_spritesheetistä")]
        [Tooltip("Lyhyt käden veto taakse ennen iskua (näytetään seisontakuva). 0 = ei vetoa.")]
        public float windupTime;
        public int frame;
        public float duration;

        [Header("Liike")]
        [Tooltip("Kuinka paljon ukko liukuu eteenpäin iskun aikana (yksikköä).")]
        public float lunge;

        [Header("Osuma")]
        public int damage = 8;
        [Tooltip("Kuinka kauas isku ulottuu (yksikköä).")]
        public float reach = 1.7f;
        [Tooltip("Kaataako isku vihollisen.")]
        public bool knockdown;

        public bool HasAnimation => sprites != null && sprites.Length > 0;

        /// Iskun kokonaiskesto sekunteina.
        public float TotalTime =>
            HasAnimation ? frameTime * (sprites.Length - 1) + impactHold : windupTime + duration;

        /// Aika alusta osumahetkeen (liukuminen tapahtuu tänä aikana).
        public float ImpactTime =>
            HasAnimation ? frameTime * Mathf.Clamp(impactFrame, 0, sprites.Length - 1) : windupTime + duration * 0.4f;

        /// Mikä animaation kuva näytetään hetkellä t (-1 = käytä varakuvaa).
        public int SpriteIndexAt(float t)
        {
            if (!HasAnimation) return -1;
            int impact = Mathf.Clamp(impactFrame, 0, sprites.Length - 1);
            float impactStart = frameTime * impact;
            if (t < impactStart) return Mathf.Min((int)(t / frameTime), impact);
            if (t < impactStart + impactHold) return impact;
            int after = impact + 1 + (int)((t - impactStart - impactHold) / frameTime);
            return Mathf.Min(after, sprites.Length - 1);
        }
    }

    enum State { Ground, JumpSquat, Air, Landing, Punch, Kick, Recovery, Hurt, Special, SideKick, Grabbed, Thrown, Down, KipUp, Push, Block, Catch, CounterThrow, Lift, Carry, CrateThrow, HiKick }

    int comboIndex;
    bool comboQueued;

    State state = State.Ground;
    float stateTime;
    float height;          // korkeus maasta (hyppy)
    float verticalVel;
    Vector2 airVel;
    bool jumpKick;
    float jumpKickTime;
    bool facingRight = true;
    bool moving;
    float animClock;

    void Awake()
    {
        SortByFrameNumber(idleSprites);
        SortByFrameNumber(actionSprites);
        SortByFrameNumber(walkSprites);
        SortByFrameNumber(runSprites);
        SortByFrameNumber(specialSprites);
        SortByFrameNumber(thrownSprites);
        SortByFrameNumber(kipUpSprites);
        SortByFrameNumber(pushSprites);
        SortByFrameNumber(blockSprites);
        SortByFrameNumber(counterThrowSprites);
        SortByFrameNumber(monkeyFlipSprites);
        SortByFrameNumber(carrySprites);
        SortByFrameNumber(hiKickSprites);
        SortByFrameNumber(scissorSprites);
        SortByFrameNumber(carryWalkSprites);
        groundHeight = TargetGroundHeight();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;   // 2D-ääni
        health = maxHealth;
        stamina = maxStamina;
        if (shadow != null && shadow.sprite == null)
            shadow.sprite = CreateShadowSprite();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTime += dt;
        animClock += dt;

        if (invulnTimer > 0f)
        {
            invulnTimer -= dt;
            if (body != null) body.enabled = invulnTimer <= 0f || Mathf.FloorToInt(invulnTimer * 12f) % 2 == 0;
        }
        if (GameOver) { ApplyVisual(); return; }
        UpdateStamina(dt);

        Vector2 move = ReadMove();
        // ylös → alas -liike saksipotkua varten
        if (move.y > 0.5f && prevMoveY <= 0.5f) lastUpTime = Time.time;
        if (move.y < -0.5f && prevMoveY >= -0.5f && Time.time - lastUpTime <= scissorInputWindow) upDownTime = Time.time;
        prevMoveY = move.y;
        bool jumpPressed = JumpPressed();
        bool punchPressed = PunchPressed();
        bool kickPressed = KickPressed();
        bool specialPressed = SpecialPressed();
        bool pushPressed = PushPressed();
        bool blockHeld = BlockHeld();
        bool catchPressed = CatchPressed();

        UpdateGroundHeight(dt);

        switch (state)
        {
            case State.Ground:
                if (jumpPressed && UseStamina(jumpStamina))
                {
                    // muista juoksu ja vauhti, jotta ne säilyvät hypyssä
                    jumpFromRun = running;
                    squatVel = moving ? lastGroundVel : Vector2.zero;
                    Enter(State.JumpSquat);
                    break;
                }
                if (blockHeld && HasBlock) { StartBlock(); break; }
                if (pushPressed && HasPush && UseStamina(pushStamina)) { StartPush(); break; }
                if (catchPressed)
                {
                    Crate c = NearbyCrate();
                    if (c != null) { StartLift(c); break; }                 // laatikko vieressä: nosto
                    if (HasCounterThrow) { Enter(State.Catch); break; }
                }
                if (punchPressed && punchCombo.Length > 0) { StartComboHit(0); break; }
                if (kickPressed)
                {
                    if (scissorArmed && Time.time - scissorArmTime <= scissorInputWindow && HasScissor && UseStamina(jumpStamina)) { StartScissorJump(); break; }
                    ArmScissor();
                    StartKick(0);
                    break;
                }
                if (specialPressed && specialSprites != null && specialSprites.Length > 0 && UseStamina(specialStamina))
                {
                    Enter(State.Special); specialHits.Clear(); specialCrates.Clear(); PlayGrunt(); break;
                }
                Walk(move, dt);
                break;

            case State.JumpSquat:
                MoveOnGround(squatVel * dt);   // vauhti ei katkea ponnistuksen ajaksi
                if (stateTime >= jumpSquatTime)
                {
                    verticalVel = jumpVelocity;
                    bool runJump = jumpFromRun || (RunHeld() && Mathf.Abs(move.x) > 0.1f);
                    float sx = moveSpeedX * (runJump ? runSpeedMultiplier : 1f);
                    float sy = moveSpeedY * (runJump ? runDepthMultiplier : 1f);
                    airVel = new Vector2(move.x * sx, move.y * sy);
                    if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0;
                    jumpKick = false;
                    scissor = false;
                    if (scissorJump)
                    {
                        // saksipotku maasta: lyhyt loikka eteen, potkut alkavat heti
                        scissorJump = false;
                        verticalVel = scissorJumpVelocity;
                        airVel = new Vector2((facingRight ? 1f : -1f) * moveSpeedX * 0.8f, 0f);
                        scissor = true; scissorTime = 0f; scissorHit1 = scissorHit2 = false;
                    }
                    Enter(State.Air);
                }
                break;

            case State.Air:
                MoveOnGround(airVel * dt);
                // saksipotkun aikana leijutaan laskussa hetki, jotta molemmat potkut ehtivät (ei nosta lisää korkeutta)
                float g = scissor && scissorTime < ScissorKick2End && verticalVel < 0f ? gravity * scissorGravityScale : gravity;
                verticalVel -= g * dt;
                height += verticalVel * dt;
                if (!jumpKick && !scissor && kickPressed && HasScissor)
                {
                    scissor = true; scissorTime = 0f; scissorHit1 = scissorHit2 = false; PlayGrunt();
                    if (verticalVel < 0f) verticalVel *= 0.3f;   // laskussa aloitettu: pieni pysähdys ilmassa
                }
                else if (!jumpKick && !scissor && (kickPressed || punchPressed)) { jumpKick = true; jumpKickTime = 0f; attackHit = false; PlayGrunt(); }
                if (scissor)
                {
                    scissorTime += dt;
                    float k1 = ScissorKick1Start, k2 = ScissorKick2Start;
                    if (!scissorHit1 && scissorTime >= k1 && scissorTime <= k1 + scissorKickHold)
                        scissorHit1 = AttackEnemies(scissorReach, scissorDamage1, false, 2.6f);
                    if (!scissorHit2 && scissorTime >= k2 && scissorTime <= k2 + scissorKickHold)
                    {
                        if (AttackEnemies(scissorReach, scissorDamage2, true, 2.6f)) { scissorHit2 = true; PlayGrunt(); }
                    }
                }
                if (jumpKick)
                {
                    jumpKickTime += dt;
                    if (!attackHit && jumpKickTime >= 0.08f && jumpKickTime <= 0.45f)
                        attackHit = AttackEnemies(jumpKickReach, jumpKickDamage, true, 2.2f);
                }
                if (height <= 0f) { height = 0f; landedFromScissor = scissor; scissor = false; Enter(State.Landing); }
                break;

            case State.Landing:
                if (stateTime >= landingTime) Enter(State.Ground);
                break;

            case State.Punch:
            {
                ComboHit hit = punchCombo[comboIndex];
                float total = hit.TotalTime;

                // eteenpäin liukuminen osumahetkeen asti
                float lungeTime = Mathf.Max(hit.ImpactTime, 0.03f);
                if (stateTime <= lungeTime && lungeTime > 0f)
                    MoveOnGround(new Vector2((facingRight ? 1f : -1f) * attackLunge / lungeTime * dt, 0f));

                // osumahetki: tarkistetaan lyhyen aikaikkunan ajan, osuuko isku
                if (!attackHit && stateTime >= hit.ImpactTime && stateTime <= hit.ImpactTime + 0.08f)
                    attackHit = AttackEnemies(hit.reach, hit.damage, hit.knockdown, hit.knockdown ? 2.9f : 2.4f);   // uppercut leukaan

                // seuraava painallus puskuriin, kun isku on tarpeeksi pitkällä
                if (punchPressed && stateTime >= total * comboInputFrom) comboQueued = true;
                // flurry: potku lyönnin aikana (toisesta lyönnistä alkaen) ketjuttaa matalaan potkuun
                if (kickPressed && comboIndex >= flurryFromPunch && stateTime >= total * comboInputFrom) flurryKickQueued = true;

                // ketjussa isku katkaistaan heti osuman jälkeen, palautusta ei odoteta
                float cancelAt = hit.ImpactTime + flurryCancelAfterImpact;
                if (flurryKickQueued && stateTime >= cancelAt)
                {
                    flurry = true;
                    StartKick(0);
                    break;
                }
                bool hasNextPunch = comboIndex + 1 < punchCombo.Length;
                // seuraava lyönti jo painettu: katkaistaan heti osuman jälkeen (nopea sarja)
                if (comboQueued && hasNextPunch && stateTime >= cancelAt) { StartComboHit(comboIndex + 1); break; }

                if (stateTime >= total)
                {
                    bool hasNext = comboIndex + 1 < punchCombo.Length;
                    if (comboQueued && hasNext) StartComboHit(comboIndex + 1);
                    else if (!hasNext) Enter(State.Recovery);
                    else Enter(State.Ground);
                }
                break;
            }

            case State.Recovery:
                if (stateTime >= comboRecovery) Enter(State.Ground);
                break;

            case State.SideKick:
            {
                float hitFrom = sideKickImpactFrame * sideKickFrameTime;
                float hitTo = hitFrom + sideKickImpactHold;
                Lunge(attackLunge, hitFrom, dt);
                if (!attackHit && stateTime >= hitFrom && stateTime <= hitTo)
                    attackHit = AttackEnemies(sideKickReach, sideKickDamage, false, 2.0f);
                float sideTotal = hitTo + (sideKickSprites.Length - sideKickImpactFrame - 1) * sideKickFrameTime;
                if (kickPressed && stateTime >= sideTotal * comboInputFrom) kickQueued = true;
                if (stateTime >= sideTotal) EndKick();
                break;
            }

            case State.Push:
            {
                float impact = pushImpactFrame * pushFrameTime;
                // syöksy eteenpäin osumahetkeen asti
                if (stateTime <= impact && impact > 0f)
                    MoveOnGround(new Vector2((facingRight ? 1f : -1f) * pushLunge / impact * dt, 0f));
                if (!attackHit && stateTime >= impact && stateTime <= impact + pushImpactHold)
                    attackHit = AttackEnemies(pushReach, pushDamage, true, 2.1f);
                if (stateTime >= PushTotalTime) Enter(State.Ground);
                break;
            }

            case State.Lift:
            {
                // kyykky, sitten laatikko nousee pään yli
                float k = Mathf.Clamp01((stateTime - liftTime * 0.35f) / (liftTime * 0.65f));
                float dir = facingRight ? 1f : -1f;
                HoldCrate(Mathf.Lerp(0.9f, 0.05f, k) * dir, Mathf.Lerp(0f, carryHeight, k));
                if (stateTime >= liftTime) Enter(State.Carry);
                break;
            }

            case State.Carry:
                if (carried == null) { Enter(State.Ground); break; }
                if (punchPressed || kickPressed || catchPressed || pushPressed)
                {
                    crateReleased = false;
                    stamina = Mathf.Max(0f, stamina - throwStamina); staminaRest = staminaRegenWait;
                    PlayGrunt();
                    Enter(State.CrateThrow);
                    break;
                }
                // kävely hitaammin, ei juoksua eikä hyppyä
                moving = move.sqrMagnitude > 0.01f;
                if (moving)
                {
                    if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0;
                    MoveOnGround(new Vector2(move.x * moveSpeedX, move.y * moveSpeedY) * carrySpeedFactor * dt);
                }
                // laatikko keinuu askelten tahdissa (kaksi keinahdusta kävelysyklissä)
                float bob = 0f;
                if (moving && HasCarryWalk)
                {
                    float cycle = walkFrameTime * carryWalkSprites.Length;
                    bob = -Mathf.Abs(Mathf.Sin(animClock / cycle * Mathf.PI * 2f)) * carryBob;
                }
                HoldCrate((facingRight ? 1f : -1f) * 0.05f, carryHeight + bob);
                break;

            case State.CrateThrow:
            {
                float dir = facingRight ? 1f : -1f;
                float release = crateThrowTime * 0.45f;    // irti heittokuvan alussa
                if (!crateReleased)
                {
                    // veto taakse, sitten eteen
                    float k = Mathf.Clamp01(stateTime / release);
                    HoldCrate(Mathf.Lerp(-0.25f, 0.6f, k) * dir, Mathf.Lerp(carryHeight + 0.1f, carryHeight - 0.4f, k));
                    if (stateTime >= release && carried != null)
                    {
                        carried.Throw(dir * crateThrowSpeed, crateThrowUp);
                        carried = null;
                        crateReleased = true;
                    }
                }
                if (stateTime >= crateThrowTime) Enter(State.Ground);
                break;
            }

            case State.Catch:
                if (stateTime >= catchWindowTime + catchMissRecovery) Enter(State.Ground);   // ohi
                break;

            case State.CounterThrow:
                UpdateCounterThrow();
                break;

            case State.Block:
                MoveOnGround(hurtVel * dt);   // torjutun iskun työntö
                hurtVel = Vector2.MoveTowards(hurtVel, Vector2.zero, 10f * dt);
                blockStun -= dt;
                if (!blockReleasing)
                {
                    // suojasta suoraan puskuun (lyönti tai puskunappi)
                    if ((pushPressed || punchPressed) && HasPush && blockStun <= 0f && UseStamina(pushStamina)) { StartPush(); break; }
                    if (!blockHeld && stateTime >= blockRaiseTime && blockStun <= 0f)
                    {
                        blockReleasing = true;
                        blockReleaseTime = 0f;
                    }
                }
                else
                {
                    blockReleaseTime += dt;
                    if (blockHeld) { blockReleasing = false; stateTime = blockRaiseTime; }   // takaisin suojaan
                    else if (blockReleaseTime >= blockLowerFrameTime * 2f) Enter(State.Ground);
                }
                break;

            case State.Special:
                // tuulimylly: osumat nollataan joka kierroksella, joten jokainen kierros osuu uudestaan
                if (specialHitEvery > 0f && stateTime >= specialHitFrom)
                {
                    int round = (int)((stateTime - specialHitFrom) / specialHitEvery);
                    if (round != specialRound) { specialRound = round; specialHits.Clear(); specialCrates.Clear(); }
                }
                if (stateTime >= specialHitFrom && stateTime <= specialHitTo) AttackAround();
                if (stateTime >= specialSprites.Length * specialFrameTime) Enter(State.Ground);
                break;

            case State.Kick:   // matala potku, kombon ensimmäinen
                if (kickPressed && scissorArmed && Time.time - scissorArmTime <= scissorInputWindow && HasScissor)
                {
                    StartScissorJump();   // ylös, alas, K, K: matala potku katkeaa saksipotkuksi
                    break;
                }
                Lunge(attackLunge, 0.1f, dt);
                if (!attackHit && stateTime >= 0.07f && stateTime <= 0.2f)
                    attackHit = AttackEnemies(kickReach, kickDamage, false, 1.2f);
                if (kickPressed && stateTime >= kickTime * comboInputFrom) kickQueued = true;
                if (flurry && kickQueued && stateTime >= 0.07f + flurryCancelAfterImpact) { EndKick(); break; }   // flurry: heti perään
                if (stateTime >= kickTime) EndKick();
                break;

            case State.HiKick:
            {
                float hitFrom = hiKickImpactFrame * hiKickFrameTime;
                Lunge(attackLunge, hitFrom, dt);
                if (!attackHit && stateTime >= hitFrom && stateTime <= hitFrom + hiKickImpactHold)
                    attackHit = AttackEnemies(hiKickReach, hiKickDamage, kickFinisherKnockdown, 2.8f);   // kombon viimeinen
                if (stateTime >= HiKickTotal) Enter(State.Ground);
                break;
            }

            case State.Grabbed:
                break;   // tarttuja liikuttaa (SetHeld)

            case State.Thrown:
                MoveOnGround(airVel * dt);
                verticalVel -= gravity * 1.6f * dt;   // heitetty iskeytyy maahan nopeasti
                height += verticalVel * dt;
                // kierähtää lennossa vaaka-asentoon (pää lentosuuntaan)
                heldRot = Mathf.MoveTowards(heldRot, airVel.x < 0f ? 90f : -90f, 360f * dt);
                if (height <= 0f)
                {
                    height = 0f;
                    heldRot = airVel.x < 0f ? 90f : -90f;
                    PlayClip(hurtSounds);
                    HitFx.OnHit(true);
                    if (CameraFollow.Instance != null) CameraFollow.Shake(0.2f, 0.25f);
                    ApplyDamage(pendingDamage);
                    Enter(State.Down);
                }
                break;

            case State.Down:
                if (stateTime >= thrownDownTime)
                {
                    heldRot = 0f;
                    if (kipUpSprites != null && kipUpSprites.Length > 0) { kipUpAfterOwnThrow = false; Enter(State.KipUp); }   // ponnistaa jaloilleen
                    else
                    {
                        invulnTimer = Mathf.Max(invulnTimer, 1.0f);   // hetki suojaa noustessa
                        Enter(State.Ground);
                    }
                }
                break;

            case State.KipUp:
                if (stateTime >= kipUpSprites.Length * kipUpFrameTime)
                {
                    // heitetyksi joutumisen jälkeen hetki suojaa; oman heiton jälkeen ei (ei välkettä)
                    if (!kipUpAfterOwnThrow) invulnTimer = Mathf.Max(invulnTimer, 0.6f);
                    kipUpAfterOwnThrow = false;
                    Enter(State.Ground);
                }
                break;

            case State.Hurt:
                MoveOnGround(hurtVel * dt);
                hurtVel = Vector2.MoveTowards(hurtVel, Vector2.zero, 10f * dt);
                if (height > 0f)   // osuma ilmassa: pudotaan maahan
                {
                    verticalVel -= gravity * dt;
                    height = Mathf.Max(0f, height + verticalVel * dt);
                }
                if (stateTime >= hurtTime && height <= 0f) Enter(State.Ground);
                break;
        }

        ApplyVisual();
    }

    void Enter(State s)
    {
        state = s;
        stateTime = 0f;
        if (s != State.Punch && s != State.Kick && s != State.HiKick && s != State.SideKick) flurry = false;
        if (s != State.Punch) flurryKickQueued = false;
        if (s != State.JumpSquat) scissorJump = false;
        if (s != State.Ground) { moving = false; running = false; }
    }

    AudioSource audioSource;
    int lastGrunt = -1;

    /// Soittaa satunnaisen gruntin (ei samaa kahdesti peräkkäin) pienellä sävelkorkeuden vaihtelulla.
    void PlayGrunt()
    {
        if (audioSource == null || attackGrunts == null || attackGrunts.Length == 0) return;
        int i = UnityEngine.Random.Range(0, attackGrunts.Length);
        if (attackGrunts.Length > 1 && i == lastGrunt) i = (i + 1) % attackGrunts.Length;
        lastGrunt = i;
        if (attackGrunts[i] == null) return;
        audioSource.pitch = 1f + UnityEngine.Random.Range(-gruntPitchVariation, gruntPitchVariation);
        audioSource.PlayOneShot(attackGrunts[i], gruntVolume);
    }

    // ---------------- Taistelu ----------------

    bool attackHit;
    Vector2 hurtVel;
    float shakeUntil;       // osuman tärinä (reaaliaikaa, näkyy osumapysäytyksen aikana)

    /// Onko pelaaja maassa tai osuman kourissa (viholliset eivät silloin aloita uutta lyöntiä).
    public bool IsDown => state == State.Hurt || state == State.CounterThrow || state == State.Grabbed || state == State.Thrown || state == State.Down || state == State.KipUp
                          || GameOver || invulnTimer > 0f;

    // ---------------- Heitto (vihollinen tarttuu kiinni) ----------------
    [Header("Heitetyksi joutuminen")]
    [Tooltip("Kuinka kauan maataan heiton jälkeen.")]
    public float thrownDownTime = 0.9f;
    [Tooltip("8 kuvaa: 0–1 ote, 2 nostettuna, 3 pään yllä, 4 lento, 5 isku maahan, 6 pomppu, 7 makaa.")]
    public Sprite[] thrownSprites;
    public bool HasThrowSprites => thrownSprites != null && thrownSprites.Length >= 8;
    [Tooltip("Kip-up-nousu maasta (6 kuvaa).")]
    public Sprite[] kipUpSprites;
    public float kipUpFrameTime = 0.1f;
    float heldRot;          // kuvan kierto nostossa ja lennossa (astetta), jos omia kuvia ei ole
    int heldPose;           // mikä kuvista 0–3 näytetään nostossa
    int pendingDamage;

    /// Vihollinen yrittää tarttua. Onnistuu vain, kun pelaaja on maassa eikä kesken erikoisliikkeen.
    public bool BeginGrab(float enemyX)
    {
        if (Riding) return false;
        if (GameOver || invulnTimer > 0f || height > 0.3f) return false;
        if (state == State.Hurt || state == State.Special || state == State.Grabbed || state == State.Thrown || state == State.Down
            || state == State.Air || state == State.JumpSquat || state == State.CounterThrow) return false;
        facingRight = enemyX > transform.position.x;    // kasvot tarttujaan päin
        DropCrate();
        Enter(State.Grabbed);
        heldRot = 0f;
        return true;
    }

    /// Tarttuja liikuttaa pelaajaa: paikka (x, syvyys), korkeus maasta ja kierto.
    public void SetHeld(Vector3 pos, float lift, float rot, int pose = 0)
    {
        if (state != State.Grabbed) return;
        transform.position = new Vector3(pos.x, Mathf.Clamp(pos.y, minDepthY, maxDepthY), 0f);
        height = lift;
        heldRot = rot;
        heldPose = pose;
    }

    /// Tarttuja heittää: lento vaakanopeudella vx, ylöspäin up; vahinko tulee maahan osuessa.
    public void Throw(float vx, float up, int damage)
    {
        if (state != State.Grabbed) return;
        airVel = new Vector2(vx, 0f);
        verticalVel = up;
        pendingDamage = damage;
        Enter(State.Thrown);
    }

    /// Osuma ajon aikana (esim. vihun potku prätkän selästä): vahinko ja ääni, ei kaatumista.
    public void HitWhileRiding(int damage)
    {
        if (GameOver || invulnTimer > 0f) return;
        PlayClip(hurtSounds);
        ApplyDamage(damage);
        invulnTimer = Mathf.Max(invulnTimer, 0.8f);
    }

    void ApplyDamage(int damage)
    {
        health = Mathf.Max(0, health - damage);
        if (health <= 0)
        {
            lives--;
            if (lives > 0) { health = maxHealth; stamina = maxStamina; invulnTimer = respawnInvulnerable; }
            else GameOver = true;
        }
    }
    /// Elämät loppuivat.
    public bool GameOver { get; private set; }
    float invulnTimer;

    /// Energian palautus (ruoka ja juoma). Palauttaa todellisen lisäyksen.
    public int Heal(int amount)
    {
        int before = health;
        health = Mathf.Min(maxHealth, health + amount);
        return health - before;
    }

    public void AddMoney(int amount) { money += amount; }
    /// Kuinka korkealla pelaaja on hypyssä (vihollisen lyönti menee ali, jos korkealla).
    public float AirHeight => height;

    /// Tarkistaa, osuuko pelaajan isku viholliseen. Palauttaa true, jos osui ainakin yhteen.
    /// sparkHeight = osumaläiskän korkeus maasta (pää n. 2.8, vatsa 2.0, jalat 1.2).
    bool AttackEnemies(float reach, int damage, bool knockdown, float sparkHeight = 2.2f)
    {
        float side = facingRight ? 1f : -1f;
        Vector3 me = transform.position;
        bool any = false, heavy = false;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 p = e.transform.position;
            float dx = p.x - me.x;
            bool inFront = facingRight ? dx >= -0.3f && dx <= reach : dx <= 0.3f && dx >= -reach;
            if (!inFront || Mathf.Abs(p.y - me.y) > attackDepth) continue;
            if (e.TakeHit(damage, me.x, knockdown))
            {
                any = true;
                // torjuttu isku: sinertävä pieni läiskä
                HitSpark.Spawn(new Vector3(p.x - side * 0.35f, p.y + sparkHeight, 0f), knockdown && !e.JustBlocked, Mathf.RoundToInt(-p.y * 100f) + 5, e.JustBlocked);
                if (knockdown && !e.JustBlocked) heavy = true;   // torjuttu isku: kevyt pysäytys
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit) continue;
            Vector3 p = c.transform.position;
            float dx = p.x - me.x;
            bool inFront = facingRight ? dx >= -0.3f && dx <= reach + 0.3f : dx <= 0.3f && dx >= -reach - 0.3f;
            if (!inFront || Mathf.Abs(p.y - me.y) > attackDepth) continue;
            if (c.TakeHit(damage, me.x))
            {
                any = true;
                heavy |= knockdown;
                HitSpark.Spawn(new Vector3(p.x - side * 0.4f, p.y + 0.8f, 0f), false, Mathf.RoundToInt(-p.y * 100f) + 5);
            }
        }
        if (any) HitFx.OnHit(heavy);
        return any;
    }

    /// Katsooko pelaaja oikealle (viholliset kiertävät selän taakse).
    public bool FacingRight => facingRight;

    /// Siirtää pelaajan heti uuteen paikkaan (ovet): maahan, perustilaan.
    public void TeleportTo(Vector3 pos)
    {
        transform.position = pos;
        height = 0f;
        verticalVel = 0f;
        airVel = Vector2.zero;
        hurtVel = Vector2.zero;
        jumpKick = false;
        DropCrate();
        Enter(State.Ground);
        groundHeight = TargetGroundHeight();
        ApplyVisual();
    }

    readonly System.Collections.Generic.HashSet<Enemy> specialHits = new System.Collections.Generic.HashSet<Enemy>();
    readonly System.Collections.Generic.HashSet<Crate> specialCrates = new System.Collections.Generic.HashSet<Crate>();

    /// Pyörähdyspotku: osuu kaikkiin lähellä oleviin molemmin puolin, kerran kuhunkin, ja kaataa.
    void AttackAround()
    {
        Vector3 me = transform.position;
        bool any = false;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || specialHits.Contains(e)) continue;
            Vector3 p = e.transform.position;
            if (Mathf.Abs(p.x - me.x) > specialReach || Mathf.Abs(p.y - me.y) > attackDepth + 0.15f) continue;
            if (e.TakeHit(specialDamage, me.x, true)) { specialHits.Add(e); any = true; }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit || specialCrates.Contains(c)) continue;
            Vector3 p = c.transform.position;
            if (Mathf.Abs(p.x - me.x) > specialReach || Mathf.Abs(p.y - me.y) > attackDepth + 0.15f) continue;
            if (c.TakeHit(specialDamage)) { specialCrates.Add(c); any = true; }
        }
        if (any) HitFx.OnHit(true);
    }

    /// Vihollisen isku osuu pelaajaan. attackerX = lyöjän x-sijainti.
    /// Kaatava isku (esim. pomon taklaus): pelaaja lentää taaksepäin ja kaatuu. Suojaus torjuu edestä.
    public bool TakeKnockdown(int damage, float attackerX, float speed, float up, Enemy attacker = null)
    {
        if (Riding) return false;
        if (state == State.Block) return TakeHit(damage, attackerX, attacker);
        if (state == State.Hurt || state == State.Special || state == State.CounterThrow) return false;
        if (state == State.Grabbed || state == State.Thrown || state == State.Down || state == State.KipUp) return false;
        if (GameOver || invulnTimer > 0f) return false;
        bool fromRight = attackerX > transform.position.x;
        DropCrate();
        facingRight = fromRight;
        heldRot = 0f;
        height = Mathf.Max(height, 0.05f);
        airVel = new Vector2(fromRight ? -speed : speed, 0f);
        verticalVel = up;
        pendingDamage = damage;
        HitFx.OnHit(true);
        HitSpark.Spawn(transform.position + new Vector3(fromRight ? 0.4f : -0.4f, 2.0f, 0f), true, Mathf.RoundToInt(-transform.position.y * 100f) + 5);
        Enter(State.Thrown);
        return true;
    }

    /// Vapaana maassa (ei iskussa, kantamassa tai ilmassa): voi käyttää esineitä kuten moottoripyörää.
    public bool IsFree => state == State.Ground && carried == null && height <= 0.05f && !GameOver;

    /// Ajaa moottoripyörää (Motorbike ohjaa liikettä ja kuvaa; iskut eivät osu).
    public bool Riding { get; set; }

    public bool TakeHit(int damage, float attackerX, Enemy attacker = null)
    {
        if (Riding) return false;
        if (state == State.Hurt || state == State.Special || state == State.CounterThrow) return false;   // pyörähdyksen ja heiton aikana ei voi lyödä
        if (state == State.Grabbed || state == State.Thrown || state == State.Down || state == State.KipUp) return false;
        if (GameOver || invulnTimer > 0f) return false;
        bool fromRight = attackerX > transform.position.x;
        // ajoitettu kurotus: napataan lyövästä kädestä kiinni ja heitetään
        if (state == State.Catch && stateTime <= catchWindowTime && fromRight == facingRight
            && attacker != null && attacker.CanBeCaught)
        {
            StartCounterThrow(attacker);
            return true;
        }
        // suojaus torjuu edestä tulevat iskut (ei selän takaa)
        if (state == State.Block && !blockReleasing && fromRight == facingRight)
        {
            hurtVel = new Vector2(fromRight ? -blockPushback : blockPushback, 0f);
            blockStun = blockStunTime;
            stateTime = Mathf.Max(stateTime, blockRaiseTime);   // kädet heti ylös
            HitFx.OnHit(false);
            shakeUntil = HitFx.ShakeUntil(false);
            HitSpark.Spawn(transform.position + new Vector3((fromRight ? 0.5f : -0.5f), 2.3f, 0f), false,
                           Mathf.RoundToInt(-transform.position.y * 100f) + 5, true);
            int chip = Mathf.RoundToInt(damage * blockDamageFactor);
            if (chip > 0) ApplyDamage(chip);
            return true;
        }
        DropCrate();
        facingRight = fromRight;                    // käänny lyöjää kohti
        hurtVel = new Vector2(fromRight ? -2.5f : 2.5f, 0f);
        if (height > 0f) verticalVel = Mathf.Min(verticalVel, 0f);
        Enter(State.Hurt);
        PlayClip(hurtSounds);
        HitFx.OnHit(false);
        shakeUntil = HitFx.ShakeUntil(false);
        HitSpark.Spawn(transform.position + new Vector3((fromRight ? 0.35f : -0.35f), height + 2.2f, 0f), false,
                       Mathf.RoundToInt(-transform.position.y * 100f) + 5);
        ApplyDamage(damage);
        return true;
    }

    void PlayClip(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        var c = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (c == null) return;
        audioSource.pitch = 1f + UnityEngine.Random.Range(-0.05f, 0.05f);
        audioSource.PlayOneShot(c, 0.9f);
    }

    bool HasPush => pushSprites != null && pushSprites.Length > 0;
    bool HasBlock => blockSprites != null && blockSprites.Length >= 5;
    float PushTotalTime => pushFrameTime * (pushSprites.Length - 1) + pushImpactHold;

    bool HasCarryWalk => carryWalkSprites != null && carryWalkSprites.Length > 0;
    bool HasCarrySprites => carrySprites != null && carrySprites.Length >= 6;
    bool HasCounterThrow => counterThrowSprites != null && counterThrowSprites.Length >= 7;
    bool HasMonkeyFlip => monkeyFlipSprites != null && monkeyFlipSprites.Length >= 8;

    // Heiton kuvat järjestyksessä (heitto.png) ja vihollisen kehon keskikohta kussakin:
    // (eteenpäin pelaajasta, korkeus maasta, kierto astetta)
    static readonly int[] CounterFrames = { 1, 3, 4, 5, 6 };
    static readonly Vector3[] CounterKeys =
    {
        new Vector3( 0.95f, 1.50f,   0f),   // ote ranteesta
        new Vector3( 0.60f, 1.65f,  15f),   // veto lähelle
        new Vector3( 0.25f, 2.30f,  70f),   // selkään
        new Vector3(-0.20f, 2.80f, 130f),   // olan yli
        new Vector3(-1.00f, 2.00f, 175f),   // irti, selän taakse
    };

    // Kuperkeikkaheitto (kuperkeikka.png): kuvat 0–7, irti kuvan 5 alussa
    static readonly int[] FlipFrames = { 0, 1, 2, 3, 4, 5, 6, 7 };
    static readonly Vector3[] FlipKeys =
    {
        new Vector3( 1.00f, 1.50f,   0f),   // ote
        new Vector3( 0.85f, 1.45f,  10f),   // kyykky, veto
        new Vector3( 0.60f, 1.35f,  30f),   // istahdus, vastus kallistuu
        new Vector3( 0.35f, 1.30f,  55f),   // selälleen, vastus kaatuu päälle
        new Vector3( 0.10f, 1.60f,  95f),   // jalat vatsassa, vastus vaakatasossa
        new Vector3(-0.70f, 2.40f, 150f),   // potku pään yli, irti
    };

    // Sama heitto, kun vastuksella on omat kuvat (Kovis): kuvat hoitavat asennon, kiertoa vain potkussa.
    // Vastuksen kuva kussakin avainkohdassa: 0 ote, 1 veto, 2 nostettuna jaloille.
    static readonly Vector3[] FlipKeysArt =
    {
        new Vector3( 1.00f, 1.50f,   0f),
        new Vector3( 0.85f, 1.50f,   0f),
        new Vector3( 0.60f, 1.50f,   0f),
        new Vector3( 0.35f, 1.80f,   0f),
        new Vector3( 0.10f, 2.10f,  30f),
        new Vector3(-0.60f, 2.60f, 110f),
    };
    static readonly int[] FlipPosesArt = { 0, 1, 1, 2, 2, 2 };
    // Niskalenkki, kun vastuksella on omat kuvat (Punkkari): 1 ote, 2 veto, 3 askel, 4 olan yli, 5 ylösalaisin
    static readonly Vector3[] CounterKeysArt =
    {
        new Vector3( 0.95f, 1.50f,   0f),
        new Vector3( 0.60f, 1.50f,   0f),
        new Vector3( 0.30f, 2.20f,  10f),   // jalat irti maasta
        new Vector3(-0.20f, 3.40f,   5f),   // korkealla olan yli
        new Vector3(-1.00f, 2.70f,  10f),
    };
    static readonly int[] CounterPosesArt = { 1, 2, 3, 4, 5 };
    bool heldArt;        // vastus käyttää omia heittokuviaan

    int[] CurFrames => monkeyFlip ? FlipFrames : CounterFrames;
    Vector3[] CurKeys => monkeyFlip ? (heldArt ? FlipKeysArt : FlipKeys) : (heldArt ? CounterKeysArt : CounterKeys);
    int[] CurPoses => monkeyFlip ? FlipPosesArt : CounterPosesArt;
    Sprite[] CurThrowSprites => monkeyFlip ? monkeyFlipSprites : counterThrowSprites;
    float CurThrowFrameTime => Mathf.Max(monkeyFlip ? monkeyFlipFrameTime : counterThrowFrameTime, 0.01f);

    // ---------------- Laatikko ----------------

    /// Ehjä laatikko edessä nostoetäisyydellä.
    Crate NearbyCrate()
    {
        Vector3 me = transform.position;
        Crate best = null; float bestD = float.MaxValue;
        foreach (var c in Crate.All)
        {
            if (c == null || !c.CanPickUp) continue;
            Vector3 p = c.transform.position;
            float dx = p.x - me.x;
            bool front = facingRight ? dx >= -0.4f && dx <= 1.5f : dx <= 0.4f && dx >= -1.5f;
            if (!front || Mathf.Abs(p.y - me.y) > 0.45f) continue;
            if (Mathf.Abs(dx) < bestD) { bestD = Mathf.Abs(dx); best = c; }
        }
        return best;
    }

    void StartLift(Crate c)
    {
        carried = c;
        c.PickUp();
        Enter(State.Lift);
    }

    /// Pitää laatikkoa: dx vaakasuunnassa pelaajasta, korkeus pelaajan jaloista.
    void HoldCrate(float dx, float lift)
    {
        if (carried == null) return;
        Vector3 me = transform.position;
        carried.SetCarried(new Vector3(me.x + dx, me.y - 0.01f, 0f), height + lift,
                           Mathf.RoundToInt(-me.y * 100f) + 1);
    }

    /// Laatikko putoaa käsistä (osuma, tarttuminen, ovi).
    void DropCrate()
    {
        if (carried != null) carried.Drop();
        carried = null;
    }

    void StartCounterThrow(Enemy e)
    {
        heldEnemy = e;
        counterReleased = false;
        monkeyFlip = e.bigBody && HasMonkeyFlip;   // isot vastukset kuperkeikalla, muut niskalenkillä
        heldArt = e.HasArtFor(monkeyFlip);
        // vihu liukuu otekohtaan omasta paikastaan (ei hyppää), eikä otteessa ole osumapysäytystä
        grabStartOffset = (e.transform.position.x - transform.position.x) * (facingRight ? 1f : -1f);
        grabStartDepth = e.transform.position.y - transform.position.y;
        e.BeginHeldByPlayer(transform.position.x, monkeyFlip);
        PlayGrunt();
        Enter(State.CounterThrow);
        UpdateCounterThrow();
    }

    // Avainvälien kestot (× kuvan aika): nosto rauhallisesti, heitto lyhyt. Kaksi viimeistä väliä kiihtyvät (ease-in).
    static readonly float[] CounterSegs = { 1.3f, 1.3f, 1.0f, 0.55f };
    static readonly float[] FlipSegs = { 1.2f, 1.2f, 1.1f, 0.9f, 0.5f };
    float[] CurSegs => monkeyFlip ? FlipSegs : CounterSegs;
    float ThrowReleaseTime { get { float s = 0f; foreach (var d in CurSegs) s += d; return s * CurThrowFrameTime; } }

    /// Heiton eteneminen avainasentoina (0 … avaimia-1) hetkellä t, irrotuksen jälkeen kuvat jatkuvat tasaisesti.
    float ThrowKeyAt(float t)
    {
        float ft = CurThrowFrameTime;
        float[] segs = CurSegs;
        for (int i = 0; i < segs.Length; i++)
        {
            float d = segs[i] * ft;
            if (t < d)
            {
                float f = t / d;
                if (i >= segs.Length - 2) f *= f;   // heittovaihe kiihtyy loppua kohti
                return i + f;
            }
            t -= d;
        }
        return segs.Length + t / ft;   // irrotuksen jälkeen: loput kuvat tasaisesti
    }

    void UpdateCounterThrow()
    {
        float ft = CurThrowFrameTime;
        Vector3[] keys = CurKeys;
        float releaseAt = ThrowReleaseTime;
        float dir = facingRight ? 1f : -1f;
        if (!counterReleased && heldEnemy != null)
        {
            float k = Mathf.Clamp(ThrowKeyAt(stateTime), 0f, keys.Length - 1);
            int i = Mathf.Min((int)k, keys.Length - 2);
            // pehmeä kaari avainkohtien läpi (ei kulmikkaita suoria pätkiä), kierto tasaisesti
            Vector3 v = CatmullRom(keys[Mathf.Max(i - 1, 0)], keys[i], keys[i + 1], keys[Mathf.Min(i + 2, keys.Length - 1)], k - i);
            v.z = Mathf.Lerp(keys[i].z, keys[i + 1].z, k - i);
            // ensimmäisen välin aikana vihu liukuu omasta paikastaan otekohtaan
            float blend = Mathf.Clamp01(k);
            blend = blend * blend * (3f - 2f * blend);
            v.x = Mathf.Lerp(grabStartOffset, v.x, blend);
            Vector3 me = transform.position;
            me.y += grabStartDepth * (1f - blend);
            int[] poses = CurPoses;
            int pose = heldArt ? poses[Mathf.Min((int)k, poses.Length - 1)] : -1;
            heldEnemy.SetHeldByPlayer(new Vector3(me.x + dir * v.x, me.y - 0.02f, 0f), v.y - 1.5f, dir * v.z, pose);
            if (stateTime >= releaseAt)
            {
                counterReleased = true;
                if (monkeyFlip) heldEnemy.ReleaseThrow(-dir * monkeyFlipSpeed, monkeyFlipUp, monkeyFlipDamage);
                else heldEnemy.ReleaseThrow(-dir * counterThrowSpeed, counterThrowUp, counterThrowDamage);
                heldEnemy = null;
            }
        }
        if (monkeyFlip)
        {
            // kuvat loppuun, hetki makuulla ja kip-upilla ylös (kuvat jatkuvat samasta asennosta)
            if (stateTime >= releaseAt + ft * (FlipFrames.Length - 1 - FlipSegs.Length) + monkeyFlipEndHold)
            {
                if (kipUpSprites != null && kipUpSprites.Length > 0) { kipUpAfterOwnThrow = true; Enter(State.KipUp); }
                else Enter(State.Ground);
            }
            return;
        }
        // niskalenkki: hero liukuu heiton voimasta eteenpäin (loivenee loppua kohti)
        if (counterThrowSlide > 0f)
        {
            float slideTime = releaseAt + counterThrowEndHold * 0.5f;
            float a0 = Mathf.Clamp01((stateTime - Time.deltaTime) / slideTime), a1 = Mathf.Clamp01(stateTime / slideTime);
            float e0 = 1f - (1f - a0) * (1f - a0), e1 = 1f - (1f - a1) * (1f - a1);
            MoveOnGround(new Vector2(dir * counterThrowSlide * (e1 - e0), 0f));
        }
        if (stateTime >= releaseAt + counterThrowEndHold)
        {
            facingRight = !facingRight;   // heiton jälkeen katsotaan heittosuuntaan
            Enter(State.Ground);
        }
    }

    float grabStartOffset, grabStartDepth;

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    void StartPush()
    {
        attackHit = false;
        PlayGrunt();
        Enter(State.Push);
    }

    void StartBlock()
    {
        blockReleasing = false;
        blockStun = 0f;
        hurtVel = Vector2.zero;
        Enter(State.Block);
    }

    /// Ylös-alas tehty juuri ennen potkua: seuraava K (pian) laukaisee saksipotkun.
    void ArmScissor()
    {
        scissorArmed = Time.time - upDownTime <= scissorInputWindow;
        scissorArmTime = Time.time;
    }

    void StartScissorJump()
    {
        scissorArmed = false;
        scissorJump = true;
        jumpFromRun = false;
        squatVel = Vector2.zero;
        PlayGrunt();
        Enter(State.JumpSquat);
    }

    bool HasScissor => scissorSprites != null && scissorSprites.Length >= 8;
    float ScissorKick1Start => scissorFrameTime;
    float ScissorKick2Start => ScissorKick1Start + scissorKickHold + scissorFrameTime;
    float ScissorKick2End => ScissorKick2Start + scissorKickHold;
    bool HasHiKick => hiKickSprites != null && hiKickSprites.Length > hiKickImpactFrame;
    bool HasSideKick => sideKickSprites != null && sideKickSprites.Length > 0;
    float HiKickTotal => hiKickFrameTime * (hiKickSprites.Length - 1) + hiKickImpactHold;

    /// Potkukombon isku: 0 matala, 1 etupotku (sivupotku), 2 korkea (viimeinen, kaataa).
    void StartKick(int index)
    {
        if (index == 1 && !HasSideKick) index = 2;   // ei sivupotkun kuvia: suoraan korkeaan
        if (index == 2 && !HasHiKick) { Enter(State.Ground); return; }
        kickComboIndex = index;
        kickQueued = false;
        attackHit = false;
        PlayGrunt();
        Enter(index == 0 ? State.Kick : index == 1 ? State.SideKick : State.HiKick);
        attackLunge = index == 0 ? ChaseLunge(lowKickLunge, kickReach)
                    : index == 1 ? ChaseLunge(sideKickLunge, sideKickReach)
                    : ChaseLunge(hiKickLunge, hiKickReach);
    }

    /// Liukuu eteenpäin yhteensä distance yksikköä ajassa until (iskun osumahetkeen asti), hidastuen loppua kohti.
    void Lunge(float distance, float until, float dt)
    {
        if (distance <= 0f || until <= 0f || stateTime > until) return;
        float k = stateTime / until;
        float speed = distance / until * 2f * (1f - k);   // alussa nopea, osumahetkellä pysähtyy
        MoveOnGround(new Vector2((facingRight ? 1f : -1f) * speed * dt, 0f));
    }

    /// Potku loppui: seuraava kombossa, jos K painettiin ajoissa, muuten perusasentoon (kombo alkaa alusta).
    void EndKick()
    {
        if (flurry && kickQueued && kickComboIndex == 0 && HasHiKick)
        {
            // flurry: korkea potku heti matalan perään, nostovaihe ohitetaan (alkaa osumakuvasta)
            StartKick(2);
            stateTime = hiKickImpactFrame * hiKickFrameTime;
            return;
        }
        if (kickQueued && kickComboIndex < 2) StartKick(kickComboIndex + 1);
        else Enter(State.Ground);
    }

    void StartComboHit(int index)
    {
        attackHit = false;
        PlayGrunt();
        comboIndex = index;
        comboQueued = false;
        Enter(State.Punch);
        attackLunge = ChaseLunge(punchCombo[index].lunge, punchCombo[index].reach);
    }

    [Header("Liuku seuraa vihollista")]
    [Tooltip("Kuinka pitkälle isku saa enintään liukua vihollisen perään (yksikköä), jotta kombo pysyy kasassa.")]
    public float maxChaseLunge = 0.9f;
    [Tooltip("Mihin osaan ulottuvuudesta liu'utaan (0.7 = vihollinen jää 70 % ulottuvuuden päähän).")]
    [Range(0.3f, 1f)] public float chaseReachFactor = 0.7f;
    float attackLunge;      // käynnissä olevan iskun liuku

    /// Liuku iskun alussa: vähintään baseLunge, ja tarvittaessa enemmän, jotta lähin edessä oleva
    /// vihollinen (esim. edellisen osuman työntämä) on ulottuvilla.
    float ChaseLunge(float baseLunge, float reach)
    {
        Vector3 me = transform.position;
        float best = float.MaxValue;
        foreach (var e in Enemy.All)
        {
            if (e == null || e.IsDead) continue;
            Vector3 p = e.transform.position;
            float dx = (p.x - me.x) * (facingRight ? 1f : -1f);
            if (dx < -0.3f || dx > reach + maxChaseLunge + 0.5f || Mathf.Abs(p.y - me.y) > attackDepth) continue;
            best = Mathf.Min(best, dx);
        }
        if (best == float.MaxValue) return baseLunge;
        return Mathf.Clamp(best - reach * chaseReachFactor, baseLunge, Mathf.Max(baseLunge, maxChaseLunge));
    }

    bool running;
    bool jumpFromRun;
    Vector2 lastGroundVel, squatVel;

    void Walk(Vector2 move, float dt)
    {
        moving = move.sqrMagnitude > 0.01f;
        running = moving && RunHeld() && Mathf.Abs(move.x) > 0.1f && stamina > 0f;   // stamina loppu: kävellään
        if (!moving) return;
        if (Mathf.Abs(move.x) > 0.1f) facingRight = move.x > 0;
        float sx = moveSpeedX * (running ? runSpeedMultiplier : 1f);
        float sy = moveSpeedY * (running ? runDepthMultiplier : 1f);
        lastGroundVel = new Vector2(move.x * sx, move.y * sy);
        MoveOnGround(lastGroundVel * dt);
    }

    void MoveOnGround(Vector2 delta)
    {
        Vector3 p = transform.position;
        p.x += delta.x;
        p.y = Mathf.Clamp(p.y + delta.y, minDepthY, maxDepthY);
        // ei kävellä alueen taustakuvan ulkopuolelle (kameran rajat + puoli ruutua)
        var cf = CameraFollow.Instance;
        var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - 0.6f;
            p.x = Mathf.Clamp(p.x, cf.minX - halfW, cf.maxX + halfW);
        }
        transform.position = p;
    }

    // ---------------- Grafiikka ----------------

    float groundHeight;   // maan korkeus ukon kohdalla (0 = ajorata, sidewalkHeight = jalkakäytävä)

    float TargetGroundHeight()
    {
        return useSidewalk && transform.position.y > curbDepthY ? sidewalkHeight : 0f;
    }

    /// Kynnyksen yli astuminen. Maassa noustaan/laskeudutaan nopealla askeleella;
    /// ilmassa ukon oikea korkeus säilyy, jolloin hyppy jalkakäytävälle laskeutuu sen pinnalle.
    void UpdateGroundHeight(float dt)
    {
        float target = TargetGroundHeight();
        if (Mathf.Approximately(target, groundHeight)) return;

        if (state == State.Air)
        {
            height -= target - groundHeight;   // pidä ukko samassa kohdassa ruudulla
            groundHeight = target;
        }
        else
        {
            float speed = sidewalkHeight / Mathf.Max(stepTime, 0.001f);
            groundHeight = Mathf.MoveTowards(groundHeight, target, speed * dt);
        }
    }

    void ApplyVisual()
    {
        if (body == null) return;

        body.sprite = CurrentSprite();
        body.flipX = !facingRight;
        // Korjaa spriten kiinnityspisteen (pivot) vaikutus: jalat keskelle alas joka kuvassa,
        // leikattiinpa kuva millä pivot-asetuksella tahansa.
        Vector2 pivotFix = Vector2.zero;
        Sprite spr = body.sprite;
        if (spr != null)
        {
            float ppu = spr.pixelsPerUnit;
            pivotFix.x = (spr.pivot.x - spr.rect.width * 0.5f) / ppu;
            pivotFix.y = spr.pivot.y / ppu;
            if (state == State.SideKick) pivotFix.x += sideKickArtOffset;   // kuvat on siirretty sheetissä vasemmalle -> takaisin oikealle
            if (state == State.HiKick) pivotFix.x += hiKickArtOffset;
            if (body.flipX) pivotFix.x = -pivotFix.x;
        }
        body.transform.localPosition = new Vector3(pivotFix.x + HitFx.ShakeOffset(shakeUntil), groundHeight + height - footOffset + pivotFix.y, 0f);
        bool held = state == State.Grabbed || state == State.Thrown || state == State.Down;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, held && !HasThrowSprites ? heldRot : 0f);
        if (shadow != null) shadow.transform.localPosition = new Vector3(0f, groundHeight, 0f);

        // lähempänä kameraa (pienempi y) piirretään päälle
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        body.sortingOrder = order;

        if (shadow != null)
        {
            shadow.sortingOrder = order - 1;
            float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 2.5f));
            shadow.transform.localScale = new Vector3(1.5f * s, 0.45f * s, 1f);
        }
    }

    Sprite CurrentSprite()
    {
        switch (state)
        {
            case State.JumpSquat:
                if (scissorJump && HasScissor) return scissorSprites[stateTime < jumpSquatTime * 0.5f ? 0 : 1];
                return Action(F_CROUCH);

            case State.Air:
                if (scissor)
                {
                    // 2 nousu, 3 ensimmäinen potku, 4 vaihto, 5 toinen potku, 6 lasku
                    float t = scissorTime;
                    if (t < ScissorKick1Start) return scissorSprites[2];
                    if (t < ScissorKick1Start + scissorKickHold) return scissorSprites[3];
                    if (t < ScissorKick2Start) return scissorSprites[4];
                    if (t < ScissorKick2End) return scissorSprites[5];
                    return scissorSprites[6];
                }
                if (jumpKick)
                {
                    float t = jumpKickTime;
                    if (t < 0.05f) return Action(F_JUMPKICK1);
                    if (t < 0.10f) return Action(F_JUMPKICK1 + 1);
                    if (t < 0.40f) return Action(F_JUMPKICK1 + 2);   // täysi potku
                    if (t < 0.50f) return Action(F_JUMPKICK1 + 3);
                    return Action(F_JUMPKICK1 + 4);
                }
                if (verticalVel > 3f) return Action(F_JUMP_UP);
                if (verticalVel > -3f) return Action(F_JUMP_TOP);
                return Action(F_JUMP_DOWN);

            case State.Landing:
                if (landedFromScissor && HasScissor && stateTime >= landingTime * 0.5f) return scissorSprites[7];
                return Action(stateTime < landingTime * 0.5f ? F_LAND1 : F_LAND2);

            case State.Punch:
            {
                ComboHit hit = punchCombo[comboIndex];
                int idx = hit.SpriteIndexAt(stateTime);
                if (idx >= 0 && hit.sprites[idx] != null) return hit.sprites[idx];
                return Action(stateTime < hit.windupTime ? F_STAND : hit.frame);
            }

            case State.Recovery:
            {
                if (punchCombo.Length == 0) return Action(F_STAND);
                ComboHit last = punchCombo[punchCombo.Length - 1];
                if (last.HasAnimation) return last.sprites[last.sprites.Length - 1];
                return Action(last.frame);
            }

            case State.Grabbed:
                if (HasThrowSprites) return thrownSprites[Mathf.Clamp(heldPose, 0, 3)];
                return Action(F_HURT);
            case State.Thrown:
                if (HasThrowSprites) return thrownSprites[4];
                return Action(F_HURT);
            case State.Down:
                if (HasThrowSprites) return thrownSprites[stateTime < 0.08f ? 5 : stateTime < 0.2f ? 6 : 7];
                return Action(F_HURT);
            case State.KipUp:
                return kipUpSprites[Mathf.Min((int)(stateTime / kipUpFrameTime), kipUpSprites.Length - 1)];
            case State.Hurt:
                return Action(F_HURT);

            case State.SideKick:
            {
                float hitFrom = sideKickImpactFrame * sideKickFrameTime;
                float hitTo = hitFrom + sideKickImpactHold;
                int i;
                if (stateTime < hitFrom) i = (int)(stateTime / sideKickFrameTime);
                else if (stateTime < hitTo) i = sideKickImpactFrame;
                else i = sideKickImpactFrame + 1 + (int)((stateTime - hitTo) / sideKickFrameTime);
                return sideKickSprites[Mathf.Clamp(i, 0, sideKickSprites.Length - 1)];
            }

            case State.Push:
            {
                float hitFrom = pushImpactFrame * pushFrameTime;
                float hitTo = hitFrom + pushImpactHold;
                int i;
                if (stateTime < hitFrom) i = (int)(stateTime / pushFrameTime);
                else if (stateTime < hitTo) i = pushImpactFrame;
                else i = pushImpactFrame + 1 + (int)((stateTime - hitTo) / pushFrameTime);
                return pushSprites[Mathf.Clamp(i, 0, pushSprites.Length - 1)];
            }

            case State.Lift:
                if (HasCarrySprites) return carrySprites[stateTime < liftTime * 0.35f ? 0 : stateTime < liftTime * 0.7f ? 1 : 2];
                if (carryPoseSprite != null && stateTime >= liftTime * 0.5f) return carryPoseSprite;
                return Action(F_CROUCH);

            case State.Carry:
                if (moving && HasCarryWalk)
                    return carryWalkSprites[(int)(animClock / walkFrameTime) % carryWalkSprites.Length];
                if (HasCarrySprites) return carrySprites[2];   // laatikko pään yllä
                if (carryPoseSprite != null) return carryPoseSprite;
                if (moving && walkSprites != null && walkSprites.Length > 0)
                    return walkSprites[(int)(animClock / walkFrameTime) % walkSprites.Length];
                return IdleFrame();

            case State.CrateThrow:
                if (HasCarrySprites)
                {
                    float r = crateThrowTime * 0.45f;
                    return carrySprites[stateTime < r ? 3 : stateTime < crateThrowTime * 0.85f ? 4 : 5];
                }
                if (punchCombo.Length > 1 && punchCombo[1].HasAnimation)
                {
                    var hs = punchCombo[1].sprites;
                    return hs[Mathf.Min((int)(stateTime / crateThrowTime * hs.Length), hs.Length - 1)];
                }
                return Action(F_PUNCH);

            case State.Catch:
                return counterThrowSprites[stateTime <= catchWindowTime ? 0 : 7 % counterThrowSprites.Length];

            case State.CounterThrow:
            {
                int[] fr = CurFrames;
                Sprite[] sp = CurThrowSprites;
                int k = Mathf.Min((int)ThrowKeyAt(stateTime), fr.Length - 1);
                return sp[Mathf.Min(fr[k], sp.Length - 1)];
            }

            case State.Block:
                if (blockReleasing)
                    return blockSprites[Mathf.Min(3 + (int)(blockReleaseTime / blockLowerFrameTime), 4)];
                return blockSprites[stateTime < blockRaiseTime ? 1 : 2];

            case State.HiKick:
            {
                float hitFrom = hiKickImpactFrame * hiKickFrameTime;
                float hitTo = hitFrom + hiKickImpactHold;
                int i;
                if (stateTime < hitFrom) i = (int)(stateTime / hiKickFrameTime);
                else if (stateTime < hitTo) i = hiKickImpactFrame;
                else i = hiKickImpactFrame + 1 + (int)((stateTime - hitTo) / hiKickFrameTime);
                return hiKickSprites[Mathf.Clamp(i, 0, hiKickSprites.Length - 1)];
            }

            case State.Special:
                return specialSprites[Mathf.Min((int)(stateTime / specialFrameTime), specialSprites.Length - 1)];

            case State.Kick:
                if (stateTime < 0.07f) return Action(F_KNEE);
                if (stateTime < 0.30f) return Action(F_KICK);
                return Action(F_KNEE);

            default: // Ground
                if (running && runSprites != null && runSprites.Length > 0)
                    return runSprites[(int)(animClock / runSpriteFrameTime) % runSprites.Length];
                if (moving && walkSprites != null && walkSprites.Length > 0)
                    return walkSprites[(int)(animClock / walkFrameTime) % walkSprites.Length];
                if (moving && useRunFramesWhenMoving)
                    return Action(F_RUN1 + (int)(animClock / runFrameTime) % 3);
                return IdleFrame();
        }
    }

    Sprite IdleFrame()
    {
        if (idleSprites == null || idleSprites.Length == 0) return Action(F_STAND);
        int n = idleSprites.Length;
        if (n == 1) return idleSprites[0];
        // edestakaisin: 0,1,...,n-1,n-2,...,1
        int period = 2 * n - 2;
        int i = (int)(animClock / idleFrameTime) % period;
        return idleSprites[i < n ? i : period - i];
    }

    Sprite Action(int index)
    {
        if (actionSprites == null || actionSprites.Length == 0) return null;
        return actionSprites[Mathf.Clamp(index, 0, actionSprites.Length - 1)];
    }

    // ---------------- Syöte (näppäimistö + peliohjain) ----------------

    Vector2 ReadMove() => ReadMoveInput();

    /// Liikesyöte (näppäimistö + peliohjain), myös muiden skriptien käyttöön (esim. moottoripyörä).
    public static Vector2 ReadMoveInput()
    {
        Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
        }
        var g = Gamepad.current;
        if (g != null)
        {
            Vector2 stick = g.leftStick.ReadValue();
            if (stick.magnitude > 0.25f) v += stick;
            v += g.dpad.ReadValue();
        }
#else
        v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        return Vector2.ClampMagnitude(v, 1f);
    }

    bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    bool PunchPressed() => PunchInput();

    /// Lyöntinappi (J / ohjaimen neliö), myös muiden skriptien käyttöön (esim. prätkä).
    public static bool PunchInput()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.J);
#endif
    }

    bool RunHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            || (Gamepad.current != null && Gamepad.current.rightShoulder.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }

    bool KickPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.K);
#endif
    }

    bool SpecialPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.L);
#endif
    }

    bool PushPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.U);
#endif
    }

    bool CatchPressed() => CatchInput();

    /// Nappaus/nosto-nappi (O / ohjaimen R3), myös muiden skriptien käyttöön (esim. kiskaisu prätkän selästä).
    public static bool CatchInput()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.O);
#endif
    }

    bool BlockHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.iKey.isPressed)
            || (Gamepad.current != null && Gamepad.current.leftTrigger.isPressed);
#else
        return Input.GetKey(KeyCode.I);
#endif
    }

    // ---------------- Apufunktiot ----------------

    /// Unity nimeää leikatut spritet "tiedosto_0", "tiedosto_1"... Järjestetään numeron mukaan,
    /// ettei raahausjärjestyksellä ole väliä (muuten _10 voisi tulla ennen _2).
    public static void SortByFrameNumber(Sprite[] sprites)
    {
        if (sprites == null) return;
        Array.Sort(sprites, (a, b) => FrameNumber(a).CompareTo(FrameNumber(b)));
    }

    static int FrameNumber(Sprite s)
    {
        if (s == null) return int.MaxValue;
        int u = s.name.LastIndexOf('_');
        return u >= 0 && int.TryParse(s.name.Substring(u + 1), out int n) ? n : 0;
    }

    public static Sprite CreateShadowSprite()
    {
        const int w = 64, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f) / w * 2f - 1f, dy = (y + 0.5f) / h * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - d) * 3f) * 0.45f;
                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}
