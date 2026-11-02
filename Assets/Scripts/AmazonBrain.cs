using UnityEngine;

/// <summary>
/// Amatsoni: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa liikkeen, lyönnit ja potkut; tämä ottaa vain väistön: kun heroa lyödään,
/// hän väistää satunnaisesti (iskut menevät ohi) ja pamauttaa perään takasuoran, joka kaataa. Kuvat: amatsoni_vaisto (19 kuvaa) ja amatsoni_suora2 (18 kuvaa).
/// </summary>
[RequireComponent(typeof(Enemy))]
public class AmazonBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    public Sprite[] dodge, counter;
    [Tooltip("Erikoisliike: tuplakyrkkihyppy kuperkeikalla (46 kuvaa, kuvat 0-2 ja 20-45 kestävät 2/30 s, 3-19 kestävät 1/30 s; osumahetki kuvat 5-17).")]
    public Sprite[] leap;
    public float leapCooldown = 5f, leapMinRange = 1.8f, leapMaxRange = 13f, leapReach = 2.9f, leapChancePerSec = 0.8f;
    [Tooltip("Oma heitto: 50 kuvaa (kurotus 0-14, ote kuvassa 14, nosto pään yli 15-35, heitto kuvassa 36, toipuminen). Hero lentää kauas ja liukuu kaataen tiellä olevat.")]
    public Sprite[] grab;
    public float grabRange = 2.6f, grabCooldown = 4f, grabChancePerSec = 1.5f, grabSpeed = 1.15f;   // heittää usein
    public int grabDamage = 28, grabCatchFrame = 14, grabThrowFrame = 36;
    public float throwSpeed = 13f, throwUp = 7f, throwSlide = 1.2f;
    public int leapDamage = 42, leapHitFrom = 5, leapHitTo = 17;
    public float leapKnockSpeed = 11f, leapKnockUp = 7f;
    [Range(0f, 1f)] public float dodgeChance = 0.3f;
    public float dodgeCooldown = 1.8f, dodgeRange = 3f;
    public float dodgeFrameTime = 0.032f, counterFrameTime = 0.034f;
    public int counterImpactFrame = 3, counterDamage = 32;
    public float counterReach = 2.5f, counterKnockSpeed = 9f, counterKnockUp = 6f;

    Enemy e;
    enum Phase { None, Dodge, Counter, Leap, Grab }
    float lastTick, nextGrab = 3f; bool holding;
    Enemy grabFoe;   // heitettävä toinen vihu (kun hero ei ole ulottuvilla)
    float nextLeap = 4f, leapDist;
    [Tooltip("Hypyn kulkema matka: pysähtyy tämän verran heron eteen, enintään leapMaxTravel.")]
    public float leapStopGap = 1.7f, leapMaxTravel = 14f;
    Phase phase;
    float t, nextDodge;
    bool counterHit;

    public bool Active => phase != Phase.None;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => Active && CurrentSprite != null;
    public bool ArmorNow => false;

    AudioClip shout;   // kaatoisku-huuto (Resources/Sfx/Amatsoni/kaatoisku): vastaisku, erikoisliike ja heitto
    static bool testGranted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetTest() { testGranted = false; }
    void Start()
    {
        // TESTAUS: otsapanta heti mukaan kerran peliä kohti (poista tämä kun testaus on ohi; muuten se ostetaan Helvin tiskiltä)
        if (!testGranted) { testGranted = true; Clues.Add(HeadbandId, HeadbandName, "Amazon-otsapanta. Mene amatsonin viereen ja paina E, niin hän taistelee rinnallasi.", "Kirjeet/amazon_otsapanta", false); }
    }
    void Awake() { grabSpeed = 1.75f;   /* heitto 50 % nopeampi (sceneen tallennettu vanha arvo ohitetaan) */
        kissHealFrac = 1f; kissCooldown = 8f; kissCostFrac = 0.25f;   /* suudelma voi täyttää energian; sceneen tallennetut vanhat arvot ohitetaan */
        e = GetComponent<Enemy>(); shout = Resources.Load<AudioClip>("Sfx/Amatsoni/kaatoisku"); }
    void Shout() { if (shout != null && e != null && e.Audio != null) e.Audio.PlayOneShot(shout, 1f); }

    void Abort()
    {
        if (holding && grabFoe != null) grabFoe.ReleaseThrow((e.FacingRight ? 1f : -1f) * 3f, 3f, 0);
        else if (holding && e != null && e.Hero != null) e.Hero.ReleaseGrab();
        grabFoe = null; holding = false; phase = Phase.None;
    }

    // ---- lahjonta: Amazon-otsapanta inventaariossa -> E amatsonin vieressä antaa sen hänelle, ja hän taistelee heron rinnalla ----
    public const string HeadbandName = "Amazon-otsapanta", HeadbandId = "esine_" + HeadbandName;
    public float recruitRange = 2.8f;
    PlayerController recruitNear;
    float noticeUntil;
    GUIStyle box;

    static readonly System.Collections.Generic.List<AmazonBrain> allies = new System.Collections.Generic.List<AmazonBrain>();
    [Tooltip("Suukko: liittolaisamatsoni imee heron elämää itselleen (hero maksaa kissCostFrac enimmäiselämistä, amatsoni saa kissHealFrac omistaan). Hero täyttää elämänsä Sohvilla.")]
    public float kissCostFrac = 0.2f, kissHealFrac = 0.4f, kissCooldown = 20f;
    float nextKiss;
    static bool kissLineFlip;   // repliikit vuorotellen: kumpikin kuuluu
    bool kissNear;

    /// Onko heron rinnalla lahjottu amatsoni (silloin muita amatsoneja ei ilmesty ruudulle).
    public static bool HasAlly { get { allies.RemoveAll(a => a == null || a.e == null || a.e.IsDead || !a.e.ally); return allies.Count > 0; } }

    void OnEnable() { if (!allies.Contains(this) && e != null && e.ally) allies.Add(this); }
    void OnDisable() { allies.Remove(this); if (kissing) EndKiss(); }
    void OnDestroy() { if (e != null && e.ally && suppressed.Count > 0) ReleaseSuppressed(); }

    void AllyUpdate()
    {
        kissNear = false;
        var hero = e.Hero;
        if (e.IsDead) { ReleaseSuppressed(); return; }   // apuri kaatui: piilotetut amatsonit palaavat
        if (hero == null) return;
        // seuraa heroa alueelta toiselle (myös toiseen sceneen): kaukana oleva liittolainen hyppää heron viereen
        if (Mathf.Abs(hero.transform.position.x - transform.position.x) > 28f || gameObject.scene != hero.gameObject.scene)
        {
            if (gameObject.scene != hero.gameObject.scene) { transform.SetParent(null, true); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, hero.gameObject.scene); }
            transform.position = new Vector3(hero.transform.position.x - 2.2f, hero.transform.position.y, 0f);
            Abort();
        }
        if (phase != Phase.None || hero.GameOver || hero.IsGrabbed || hero.IsFloored || hero.Scripted) return;
        Vector3 hp = hero.transform.position, me = transform.position;
        float yTol = e.depthTolerance + 0.6f + (hero.IsDrunk ? 1.2f : 0f);   // kännissä horjuminen heittää syvyyssuunnassa
        if (Mathf.Abs(hp.x - me.x) > recruitRange + (hero.IsDrunk ? 0.8f : 0f) || Mathf.Abs(hp.y - me.y) > yTol) return;
        if (e.HealthFraction > 0.9f || Time.time < nextKiss || hero.health <= Mathf.RoundToInt(hero.maxHealth * (kissCostFrac + 0.1f))) return;
        kissNear = true;
        if (CoopInput.Pick(hero.playerIndex)) StartKiss(hero);
    }

    // ---- suudelma: Rocco ja amatsoni piirretään yhtenä parikuvana (Resources/Suukko/suukko_NN, videosta); elämä siirtyy suudelman keskellä ----
    [Tooltip("Suudelmakuvan leveyskerroin (1 = ei venytystä): hahmot olivat liian hoikkia.")]
    public float kissWidth = 1.1f;
    const float KissFrameTime = 0.13f;   // 81 kuvaa: suudelma kestää yli 10 s
    const int KissEffectFrame = 18;   // kasvot kohtaavat: tästä alkaen elämää imetään tasaisesti loppuun asti
    const int KissTicks = 10;
    int kissTicksDone;
    AudioClip smack;
    bool kissing, kissApplied;
    float kissT; int kissHeroHealth;
    PlayerController kissHero;
    SpriteRenderer kissSr;
    Sprite[] kissSprs;

    void StartKiss(PlayerController hero)
    {
        var hb = hero.BodyRenderer;
        if (kissSprs == null)
        {
            float ppu = hb != null && hb.sprite != null ? hb.sprite.pixelsPerUnit : 100f;
            var list = new System.Collections.Generic.List<Sprite>();
            for (int n = 0; n < 200; n++)
            {
                var tex = Resources.Load<Texture2D>("Suukko/suukko_" + n.ToString("00"));
                if (tex == null) break;
                list.Add(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), ppu));
            }
            kissSprs = list.ToArray();
        }
        if (kissSprs.Length == 0 || hb == null || e.body == null) { KissEffect(hero); return; }   // ei kuvia: pelkkä vaikutus
        kissing = true; kissRewind = false; kissApplied = false; kissTicksDone = 0; kissT = 0f; kissHero = hero; kissHeroHealth = hero.health;
        nextKiss = Time.time + kissCooldown + kissSprs.Length * KissFrameTime;
        kissNear = false; Abort();
        hero.Scripted = true; hero.ScriptedMove = Vector2.zero; hb.enabled = false;
        e.body.enabled = false;   // amatsonin kuva piiloon: parikuva piirtää molemmat
        kissSr = new GameObject("Suukko").AddComponent<SpriteRenderer>();
        var speech = Resources.Load<AudioClip>((kissLineFlip = !kissLineFlip) ? "Sfx/Amatsoni/suudelma" : "Sfx/Amatsoni/suudelma2");   // amatsonin repliikki suudelman alkaessa (kaksi vaihtoehtoa; ei taisteluhuutoa)
        if (speech != null && e.Audio != null) e.Audio.PlayOneShot(speech, 1f);
    }

    void KissEffect(PlayerController hero, float share = 1f)
    {
        int cost = Mathf.RoundToInt(hero.maxHealth * kissCostFrac * share);
        hero.health -= cost;
        e.HealFraction(kissHealFrac * share);
        if (share >= 1f) HitFx.PlayPickup(false);
        else if (kissTicksDone % 2 == 1)
        {
            if (smack == null) smack = Resources.Load<AudioClip>("Sfx/Amatsoni/suukko");   // suukon läiskäys joka toisella imuerällä
            if (smack != null && e.Audio != null) e.Audio.PlayOneShot(smack, 1f); else HitFx.PlayPickup(false);
        }
        GameHUD.Popup("♥", transform.position + Vector3.up * 3.4f, new Color(1f, 0.4f, 0.6f));
        GameHUD.Popup("-" + cost, hero.transform.position + Vector3.up * 3.4f, new Color(1f, 0.4f, 0.4f));
    }

    void EndKiss()
    {
        kissing = false;
        if (kissHero != null)
        {
            kissHero.Scripted = false;
            if (kissHero.BodyRenderer != null) kissHero.BodyRenderer.enabled = true;
            e.FacingRight = kissHero.transform.position.x > transform.position.x;
        }
        if (e != null && e.body != null) e.body.enabled = true;
        if (kissSr != null) Destroy(kissSr.gameObject);
        kissHero = null;
    }

    // suudelman jälkeen alku kelataan taaksepäin: hahmot erkanevat toisistaan (rewindFrom = kuva, josta kelaus alkaa)
    bool kissRewind; float rewindT; int rewindFrom;
    const float RewindFrameTime = 0.07f;

    void StopKiss(int fi)
    {
        kissRewind = true; rewindT = 0f; rewindFrom = Mathf.Min(fi, KissEffectFrame + 2);
    }

    void KissTick(float dt)
    {
        if (e != null) e.DelayTaunt(1.5f);   // ei muita repliikkejä suudelman aikana: vain alun suudelmapuhe
        var hero = kissHero; var hb = hero != null ? hero.BodyRenderer : null;
        if (hero == null || hb == null || hero.GameOver || hero.health < kissHeroHealth || e.IsDead) { EndKiss(); return; }   // osuma keskeyttää
        int fi;
        if (kissRewind)
        {
            rewindT += dt;
            fi = rewindFrom - (int)(rewindT / RewindFrameTime);
            if (fi < 0) { EndKiss(); return; }
        }
        else
        {
            kissT += dt;
            fi = (int)(kissT / KissFrameTime);
            if (kissT > 0.4f && CoopInput.Pick(hero.playerIndex)) { StopKiss(fi); return; }   // pelaaja lopettaa suudelman aiemmin: siihenastinen imu jää voimaan
            // elämän imeminen tasaisesti kasvojen kohtaamisesta loppuun (KissTicks erää)
            int wantTicks = fi < KissEffectFrame ? 0 : Mathf.Min(KissTicks, 1 + (fi - KissEffectFrame) * KissTicks / Mathf.Max(1, kissSprs.Length - KissEffectFrame));
            while (kissTicksDone < wantTicks) { kissTicksDone++; KissEffect(hero, 1f / KissTicks); kissHeroHealth = hero.health; }
            if (kissTicksDone > 0 && e.HealthFraction >= 0.995f) { StopKiss(fi); return; }   // energia täynnä: suudelma loppuu, hero ei maksa turhaan
            if (fi >= kissSprs.Length) { while (kissTicksDone < KissTicks) { kissTicksDone++; KissEffect(hero, 1f / KissTicks); } kissHeroHealth = hero.health; StopKiss(kissSprs.Length - 1); return; }
        }
        hero.ScriptedMove = Vector2.zero;
        kissSr.sprite = kissSprs[Mathf.Clamp(fi, 0, kissSprs.Length - 1)];
        Vector3 hp = hb.transform.position, me = transform.position;
        kissSr.transform.position = new Vector3((hp.x + me.x) * 0.5f, hp.y, 0f);
        var ls = hb.transform.lossyScale; kissSr.transform.localScale = new Vector3(Mathf.Abs(ls.x) * kissWidth, Mathf.Abs(ls.y), 1f);   // heron kuvan skaala (visualScale, syvyys)
        kissSr.flipX = hero.transform.position.x > me.x;   // videossa Rocco on vasemmalla
        kissSr.sortingLayerID = hb.sortingLayerID; kissSr.sortingOrder = hb.sortingOrder;
    }

    void TryRecruit()
    {
        recruitNear = null;
        if (e != null && e.ally && e.IsDead && suppressed.Count > 0) ReleaseSuppressed();   // apuri kaatui
        if (e != null && e.ally && allies.Contains(this)) { AllyUpdate(); return; }
        if (e == null || e.ally || e.IsDead || !Clues.Has(HeadbandId) || phase != Phase.None) return;
        allies.RemoveAll(a => a == null || a.e == null || a.e.IsDead || !a.e.ally);
        if (allies.Count > 0) return;   // vain yksi amatsoni kerrallaan rinnalla
        foreach (var p in Players.All)
        {
            if (p == null || p.GameOver || p.IsGrabbed || p.IsFloored || p.Scripted) continue;
            Vector3 hp = p.transform.position, me = transform.position;
            if (Mathf.Abs(hp.x - me.x) > recruitRange || Mathf.Abs(hp.y - me.y) > e.depthTolerance + 0.6f) continue;
            recruitNear = p;
            if (CoopInput.Pick(p.playerIndex)) { Recruit(p); return; }
            return;
        }
    }

    /// Testialoitus (AmazonStartAlly): amatsoni liittyy heron rinnalle ilman otsapantaa.
    public void JoinAsAlly(PlayerController p) { if (e != null && !e.ally) Recruit(p); }

    void Recruit(PlayerController p)
    {
        Clues.All.RemoveAll(c => c.id == HeadbandId);   // otsapanta annettu: pois inventaariosta
        Abort();
        transform.SetParent(null, true);   // juuriolioksi, jotta seuraaminen toiseen sceneen onnistuu
        if (gameObject.scene != p.gameObject.scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, p.gameObject.scene);   // heron sceneen: ei katoa, kun vanha kenttä vapautetaan
        if (!allies.Contains(this)) allies.Add(this);
        e.ally = true; e.fightsEveryone = false; e.huntsBrawlers = false; e.brawlsWithOthers = false; e.joinsFightWhenSquadComes = false;
        e.IsAwake = true; e.wakeDistance = 60f;
        e.FacingRight = p.transform.position.x > transform.position.x;
        SuppressOthers(this);
        Shout(); HitFx.PlayPickup(false);
        noticeUntil = Time.time + 3f; recruitNear = null;
    }

    void OnGUI()
    {
        if (e != null && e.ally && !e.IsDead && allies.Contains(this)) DrawAllyBar();
        bool prompt = recruitNear != null || kissNear || (kissing && !kissRewind), notice = Time.time < noticeUntil && !kissing;
        if (!prompt && !notice) return;
        if (box == null) { box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; box.normal.textColor = Color.white; }
        float H = Screen.height, W = Screen.width, w = H * 0.8f, h = H * 0.065f;
        GUI.Box(new Rect((W - w) * 0.5f, notice ? H * 0.74f : H * 0.82f, w, h), notice ? Loc.T("Amatsoni liittyi seuraasi!") : "[E]  " + Loc.T(kissing ? "Lopeta suudelma" : kissNear ? "Suukko amatsonille (hän imee elämääsi)" : "Anna otsapanta amatsonille"), box);
    }

    static Texture2D px;
    /// Liittolaisen elämäpalkki koko ajan ruudulla (heron palkkien alla vasemmalla).
    void DrawAllyBar()
    {
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        float H = Screen.height, W = Screen.width, x = W * 0.045f, y = H * 0.18f, bw = W * 0.2f, bh = H * 0.022f;
        var f = e.HealthFraction;
        if (box == null) { box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(H * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter }; box.normal.textColor = Color.white; }
        var lbl = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.03f), fontStyle = FontStyle.Bold };
        lbl.normal.textColor = Color.black; GUI.Label(new Rect(x + 2, y - H * 0.042f + 2, bw, H * 0.04f), Loc.T("AMATSONI"), lbl);
        lbl.normal.textColor = Color.white; GUI.Label(new Rect(x, y - H * 0.042f, bw, H * 0.04f), Loc.T("AMATSONI"), lbl);
        GUI.color = Color.black; GUI.DrawTexture(new Rect(x - 3, y - 3, bw + 6, bh + 6), px);
        GUI.color = new Color(0.35f, 0.05f, 0.05f); GUI.DrawTexture(new Rect(x, y, bw, bh), px);
        GUI.color = Color.Lerp(new Color(0.95f, 0.15f, 0.1f), new Color(0.3f, 0.85f, 0.25f), f); GUI.DrawTexture(new Rect(x, y, bw * f, bh), px);
        GUI.color = Color.white;
    }

    // ---- liittolaisen ollessa mukana muut amatsonit piiloon (ei toista amatsonia ruudulle) ----
    static readonly System.Collections.Generic.List<GameObject> suppressed = new System.Collections.Generic.List<GameObject>();
    static void SuppressOthers(AmazonBrain keep)
    {
        foreach (var b in FindObjectsByType<AmazonBrain>(FindObjectsSortMode.None))
        {
            if (b == keep || b.e == null || b.e.ally || b.e.IsDead) continue;
            suppressed.Add(b.gameObject); b.gameObject.SetActive(false);
        }
    }
    static void ReleaseSuppressed()
    {
        foreach (var g in suppressed) if (g != null) g.SetActive(true);
        suppressed.Clear();
    }

    void Update()
    {
        if (kissing) { KissTick(Time.deltaTime); return; }
        TryRecruit();
        if (phase != Phase.None && Time.time - lastTick > 0.25f) { Abort(); return; }   // väliin tuli osuma, kaatuminen tms.
        if (phase == Phase.None && !e.ally && grab != null && grab.Length >= 40 && e != null && e.Hero != null && e.IsAwake && e.BrainIdleOrChase && Time.time >= nextGrab && !e.Hero.IsFloored && !e.Hero.IsGrabbed)
        {
            float gdx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), gdy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
            if (gdx <= grabRange && gdy <= e.depthTolerance + 0.1f && Random.value < grabChancePerSec * Time.deltaTime)
            {
                nextGrab = Time.time + grabCooldown; nextLeap = Mathf.Max(nextLeap, Time.time + 3f);
                phase = Phase.Grab; t = 0f; holding = false; lastTick = Time.time;
                e.FacingRight = e.Hero.transform.position.x > transform.position.x;
                e.BrainPhaseSet(Enemy.BrainPhase.Punch);
                CurrentSprite = grab[0];
                return;
            }
        }
        // toinen vihu ulottuvilla (kun hän taistelee kaikkia vastaan): napataan ja paiskataan kuin hero
        if (phase == Phase.None && grab != null && grab.Length >= 40 && e != null && e.IsAwake && e.BrainIdleOrChase && Time.time >= nextGrab)
        {
            var foe = e.TargetEnemy;
            if (foe != null && !foe.IsDead && foe.CanBeGrabbed && Mathf.Abs(foe.transform.position.x - transform.position.x) <= grabRange && Mathf.Abs(foe.transform.position.y - transform.position.y) <= e.depthTolerance + 0.1f
                && Random.value < grabChancePerSec * Time.deltaTime)
            {
                nextGrab = Time.time + grabCooldown; nextLeap = Mathf.Max(nextLeap, Time.time + 3f);
                grabFoe = foe; phase = Phase.Grab; t = 0f; holding = false; lastTick = Time.time;
                e.FacingRight = foe.transform.position.x > transform.position.x;
                e.BrainPhaseSet(Enemy.BrainPhase.Punch);
                CurrentSprite = grab[0];
                return;
            }
        }
        if (phase != Phase.None || e.ally || leap == null || leap.Length == 0 || e == null || e.Hero == null || !e.IsAwake || !e.BrainNeutral) return;
        if (Time.time < nextLeap) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        if (dx < leapMinRange || dx > leapMaxRange || dy > e.depthTolerance + 0.1f) return;
        if (Random.value > leapChancePerSec * Time.deltaTime) return;
        nextLeap = Time.time + leapCooldown;
        leapDist = Mathf.Clamp(dx - leapStopGap, 0f, leapMaxTravel);   // hyppy kuljettaa hänet heron eteen
        phase = Phase.Leap; t = 0f; counterHit = false; lastTick = Time.time; Shout();
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        CurrentSprite = leap[0];
    }

    float LeapDuration() { float d = 0f; for (int i = 0; i < leap.Length; i++) d += LeapFrameTime(i); return d; }
    static float LeapFrameTime(int i) => i <= 2 ? 1f / 30f : i <= 19 ? 0.6f / 30f : 0.8f / 30f;   // ponnistus ja lento nopeita, alastulo ja nousu ennen 2/30 s -> 0,8/30 s
    int LeapIndex(float time) { float a = 0f; for (int i = 0; i < leap.Length; i++) { a += LeapFrameTime(i); if (time < a) return i; } return leap.Length - 1; }

    public bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (phase == Phase.Leap) return false;
        if (phase == Phase.Dodge) return t < dodge.Length * dodgeFrameTime * 0.75f;   // väistön aikana iskut menevät ohi
        if (phase != Phase.None || e == null || e.ally || e.Hero == null || dodge == null || dodge.Length == 0 || counter == null || counter.Length == 0) return false;
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

    // heitto: nostovaihe (otteesta heittoon) kulkee nopeammin, jotta hän ehtii viedä sen loppuun
    const float LiftSpeedMul = 2.2f;
    float GrabFrameTime(int i, float ft) => i >= grabCatchFrame && i < grabThrowFrame ? ft / LiftSpeedMul : ft;
    int GrabIdx(float time, float ft)
    {
        float a = 0f;
        for (int i = 0; i < grab.Length; i++) { a += GrabFrameTime(i, ft); if (time < a) return i; }
        return grab.Length - 1;
    }
    float GrabDuration(float ft) { float a = 0f; for (int i = 0; i < grab.Length; i++) a += GrabFrameTime(i, ft); return a; }

    void GrabUpdate(PlayerController hero)
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        float ft = 2f / 30f / grabSpeed;
        int i = GrabIdx(t, ft);
        CurrentSprite = grab[i];
        float face = e.FacingRight ? 1f : -1f; Vector3 me = transform.position;
        if (!holding && i < grabThrowFrame && i >= grabCatchFrame && phase == Phase.Grab)
        {
            if (hero.IsGrabbed) return;
            if (Mathf.Abs(hero.transform.position.x - me.x) > grabRange + 0.8f || !hero.BeginGrab(me.x)) { Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }   // hero ehti pois
            holding = true;
        }
        if (holding && i < grabThrowFrame)
        {
            if (!hero.IsGrabbed) { holding = false; Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }
            float u = Mathf.Clamp01((i - grabCatchFrame) / (float)(grabThrowFrame - grabCatchFrame));
            // edessä rinnuksilla -> pään yli ylös
            float lift = Mathf.Lerp(0.5f, 2.7f, u * u), x = me.x + face * Mathf.Lerp(1.0f, 0.2f, u);
            hero.SetHeld(new Vector3(x, me.y - 0.02f, 0f), lift, 0f, 0);
        }
        else if (holding)
        {
            holding = false; Shout();
            hero.Throw(face * throwSpeed, throwUp, grabDamage, true, throwSlide);   // lentää kauas ja liukuu kaataen tiellä olevat
        }
        if (i >= grab.Length - 1 && t >= GrabDuration(ft)) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    void GrabFoeUpdate()
    {
        e.BrainPhaseSet(Enemy.BrainPhase.Punch);
        float ft = 2f / 30f / grabSpeed;
        int i = GrabIdx(t, ft);
        CurrentSprite = grab[i];
        float face = e.FacingRight ? 1f : -1f; Vector3 me = transform.position;
        if (grabFoe == null || grabFoe.IsDead) { holding = false; Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }
        if (!holding && i >= grabCatchFrame && i < grabThrowFrame)
        {
            if (Mathf.Abs(grabFoe.transform.position.x - me.x) > grabRange + 0.8f || !grabFoe.CanBeGrabbed) { Abort(); e.BrainPhaseSet(Enemy.BrainPhase.Chase); return; }
            grabFoe.BeginHeldByPlayer(me.x);
            grabFoe.heldLying = true;
            holding = true;
        }
        if (holding && i < grabThrowFrame)
        {
            float u = Mathf.Clamp01((i - grabCatchFrame) / (float)(grabThrowFrame - grabCatchFrame));
            float lift = Mathf.Lerp(0.5f, 2.7f, u * u), x = me.x + face * Mathf.Lerp(1.0f, 0.2f, u);
            bool foeArt = grabFoe.HasFlipArt || grabFoe.HasHeadlockArt;   // omat otekuvat, tai muuten kierto kuten pelaajan heitossa (ei jää seisoma-asentoon)
            float spin = 0f;   // ei pyöritetä pystyssä: käytetään kaatumiskuvaa
            grabFoe.SetHeldByPlayer(new Vector3(x, me.y - 0.02f, 0f), lift, spin, foeArt ? (u < 0.5f ? 1 : 2) : -1);
        }
        else if (holding)
        {
            holding = false; Shout();
            grabFoe.thrownByEnemy = true;
            grabFoe.thrownBy = e;   // heitetty vihu ei kaada heittäjää itseään (sama kuin Tomilla)
            grabFoe.ReleaseThrow(face * throwSpeed, throwUp, grabDamage, true, throwSlide);   // liukuu maassa kuin hero
            grabFoe = null;
        }
        if (i >= grab.Length - 1 && t >= GrabDuration(ft)) { grabFoe = null; phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
    }

    public void Tick(float dt)
    {
        if (phase == Phase.None) return;
        var hero = e.Hero;
        if (hero == null) { Abort(); return; }
        lastTick = Time.time;
        t += dt;
        if (phase == Phase.Grab) { if (grabFoe != null) GrabFoeUpdate(); else GrabUpdate(hero); return; }
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        if (phase == Phase.Leap)
        {
            int li = LeapIndex(t); CurrentSprite = leap[li];
            if (leapDist > 0f)   // ponnistuksesta lentoon: matka kasvaa neliöllisesti (kiihtyy), lentovaihe ruudut 3-19 (6/30 s alku, 17/30 s lento)
            {
                float t0 = 3f / 30f, flight = 17f * 0.6f / 30f;
                float u0 = Mathf.Clamp01((t - dt - t0) / flight), u1 = Mathf.Clamp01((t - t0) / flight);
                float d = leapDist * (u1 * u1 - u0 * u0);
                if (d > 0f) e.BrainMove(new Vector2((e.FacingRight ? 1f : -1f) * d, 0f));
            }
            if (!counterHit && li >= leapHitFrom && li <= leapHitTo)
            {
                Vector3 me = transform.position, hp = hero.transform.position;
                float dxl = hp.x - me.x;
                bool front = e.FacingRight ? dxl >= -0.2f : dxl <= 0.2f;
                if (front && Mathf.Abs(dxl) <= leapReach && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.1f && hero.AirHeight <= 0.9f)
                {
                    counterHit = true;   // ei torjuttavissa
                    if (!hero.TakeKnockdown(leapDamage, me.x, leapKnockSpeed, leapKnockUp, e, true)) hero.TakeHit(leapDamage, me.x, e, true);
                }
            }
            if (t >= LeapDuration()) { phase = Phase.None; e.BrainPhaseSet(Enemy.BrainPhase.Chase); }
            return;
        }
        float face = e.FacingRight ? 1f : -1f;
        if (phase == Phase.Dodge)
        {
            int i = Mathf.Min((int)(t / dodgeFrameTime), dodge.Length - 1);
            CurrentSprite = dodge[i];
            if (t < 0.15f) e.BrainMove(new Vector2(-face * 1.4f * dt, 0f));   // hieman taaksepäin
            if (t >= dodge.Length * dodgeFrameTime) { phase = Phase.Counter; t = 0f; Shout(); e.FacingRight = hero.transform.position.x > transform.position.x; }
            return;
        }
        int k = Mathf.Min((int)(t / counterFrameTime), counter.Length - 1);
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
