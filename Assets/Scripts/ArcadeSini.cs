using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelihallin Sini ja Dina (persona-kenttä; ei taistelijoita eikä lyötävissä). Kuvat Resources/Sini/&lt;sarja&gt;_NN (leikattu videoista, Tools/SiniAssets):
/// idle, talk, walk, toCounter, counter, kiss, dance. Kuvat katsovat oikealle; vasemmalle kuva peilataan.
/// Seisoskelee, kävelee, käy kanttiinin tiskillä, tanssii ja pysähtyy juttelemaan toisen hallin hahmon kanssa.
/// E lähellä: juttelu, jossa Sinin mielialaa nostetaan kolmella vastauksella; jos onnistuu, hän antaa lentosuukon ja tanssahtaa.
/// Tappelun alkaessa hän väistää hallin laitaan.
/// </summary>
public class ArcadeSini : MonoBehaviour
{
    public enum Persona { Sini, Dina }
    public Persona persona = Persona.Sini;
    [Tooltip("Kuvakansio Resources-kansion alla (Sini / Dina).")]
    public string folder = "Sini";
    public string npcName = "Sini";
    [Tooltip("Toinen hallin hahmo (Sini <-> Dina): he juttelevat keskenään.")]
    public ArcadeSini friend;
    [Tooltip("Dina: hänen Tähtipyssy-ennätyksensä. Kun pelaajan paras ylittää tämän, Dina palkitsee.")]
    public int dinaRecord = 30000;
    [Tooltip("Dina: rahapalkinto ennätyksen rikkomisesta (0 = ei rahaa; palkinto on vielä auki).")]
    public int rewardMoney = 0;
    public static event System.Action<PlayerController> RecordBeaten;
    public Area area;
    public SalsaBrawl brawl;
    [Tooltip("Kämppä (pelihallin yläkerta): Sini keittää kahvit (E), Dina juttelee kuten hallissa. Ei tappelua, ei kutsua.")]
    public bool home;
    public const string InviteClue = "sini_kutsu";
    bool coffeePages;
    bool treeMode, treeChoose, treeFade, inviteTree, inviteAccepted;
    int node, treeSel; float treeLastY;
    Texture2D fadeTex, kissTex;
    UnityEngine.Video.VideoPlayer kissVp; RenderTexture kissRt; AudioSource kissAudio; bool kissDone, kissVideo;
    static int coffeeVisits;
    static bool afterKiss;   // makkarikohtaus nähty: kämpän Sini antaa lentosuukon
    [Tooltip("Hahmon korkeus yksikköinä.")]
    public float height = 3.0f;
    [Tooltip("Koon kerroin (1.2 = 20 % isompi). Uusi kenttä: vanhoissa scene-olioissa saa oletuksen.")]
    public float sizeMul = 1.2f;
    public float speed = 1.4f;
    public float laneMinX, laneMaxX;
    [Tooltip("Tiskipaikat (x), joissa hän nojaa tiskiin.")]
    public float[] counterXs;
    public Vector2 waitRange = new Vector2(2f, 6f);
    public Vector2 counterStay = new Vector2(8f, 16f);
    public Vector2 danceTime = new Vector2(9f, 18f), talkTime = new Vector2(9f, 16f);
    [Range(0f, 1f)] public float counterChance = 0.28f, danceChance = 0.22f, talkChance = 0.22f;
    public float idleFrameTime = 0.3f, walkFrameTime = 0.114f, talkFrameTime = 0.167f, toCounterFrameTime = 0.1f,
                 counterFrameTime = 0.25f, kissFrameTime = 0.133f, danceFrameTime = 0.167f;
    [Tooltip("Pelaajan etäisyys, jolta E aloittaa juttelun.")]
    public float talkHalfWidth = 2.1f, talkDepth = 1.7f;

    [Header("Sohva (kämpän kahvikohtaus: Rocco ja Sini istuvat vastakkain)")]
    public bool hasSofa;
    public float sofaY, sofaRoccoX, sofaSiniX;
    int sitPhase;                       // 0 ei, 1 Rocco kävelee, 2 Rocco istuu, 3 Sini kävelee, 4 Sini istuu, 5 istuvat (dialogi), 6 nousevat
    float sitT; SpriteRenderer sitterR, sitterS; Sprite[] sitR, sitS; PlayerController sitPc;
    const float SitSiniFt = 1f / 12f, SitRoccoFt = 1f / 12f;
    public static float SitRoccoScale = 1.18f;   // videon Rocco on Siniä pienempi; kerroin isontaa hänet (jalat pysyvät paikallaan)

    const float PadPx = 24f, CharPx = 360f;
    enum S { Idle, Walk, ToCounter, AtCounter, FromCounter, Dance, TalkNpc, Listen, Chat, Kiss, Hide, ToDoor, AtDoor }
    S state = S.Idle;
    Sprite[] idle, walk, talk, toCounter, atCounter, kiss, dance;
    SpriteRenderer body, shadow;
    float t, wait, stateLeft;
    Vector2 goal; int goalKind;   // 0 vaeltaa, 1 tiski, 2 tanssi, 3 juttelu
    bool flipped;
    Enemy partner; ArcadeSini friendTarget, listenTo;
    float counterX = float.NaN;
    static readonly List<ArcadeSini> All = new List<ArcadeSini>();
    string[] pages; int page;      // Dinan juttelu
    float chatterAt; string bubble; float bubbleUntil;

    // juttelu
    PlayerController chatPc;
    int stage;                       // 0..2 kierros
    enum Ph { Say, Choose, React, Verdict }
    Ph phase;
    float phaseT;
    int sel; float lastY;
    float mood;
    int[] order = new int[3];        // vastausvaihtoehtojen järjestys näytöllä
    string reaction = "";
    bool cheered;                    // onnistui: hän on hyvällä tuulella
    float cheeredUntil, chatCooldown, nextGift;
    bool near;
    GUIStyle box, small;

    struct Opt { public string text, reply; public int gain; }
    static readonly string[] Asks =
    {
        "Kone söi viimeiset kolikkoni. Taas.",
        "Kaikki muut voittavat täällä, vaan en minä.",
        "Olisi kiva tanssia, mutta ei jaksa yhtään.",
    };
    static readonly Opt[][] Rounds =
    {
        new[] {
            new Opt { text = "Ostetaan sulle kanttiinista jotain juotavaa.", reply = "Tuo oli kiltisti sanottu.", gain = 35 },
            new Opt { text = "Kone ei ole sinua vastaan.", reply = "No, ehkä. Ei se silti auta.", gain = 12 },
            new Opt { text = "Ei kannata pelata, jos ei osaa.", reply = "Kiitti vaan. Todella hauska.", gain = -18 } },
        new[] {
            new Opt { text = "Sinun takiasi tänne tullaan, ei koneiden.", reply = "Hah. Sanoit sen ihan vakavalla naamalla.", gain = 35 },
            new Opt { text = "Tuuri kääntyy kyllä, usko pois.", reply = "Toivottavasti sinulla on oikeus.", gain = 12 },
            new Opt { text = "Voitto on aina vain tuuria.", reply = "Aina yhtä lohdullista.", gain = -18 } },
        new[] {
            new Opt { text = "Yksi tanssi. Minä vain katson.", reply = "Okei, okei. Se riittää.", gain = 35 },
            new Opt { text = "Istu sitten vähän aikaa.", reply = "Tai niin.", gain = 12 },
            new Opt { text = "Ei kukaan jaksa, ihan sama.", reply = "Tuo ei auttanut yhtään.", gain = -18 } },
    };
    static readonly string[] Chatter = { "Ai niinkö?", "Hahaha!", "Ei kai!", "Kerro lisää.", "Totta kai.", "Oho." };

    void Awake()
    {
        var vis = new GameObject("Visual"); vis.transform.SetParent(transform, false);
        body = vis.AddComponent<SpriteRenderer>();
        var sh = new GameObject("Shadow"); sh.transform.SetParent(transform, false);
        shadow = sh.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(1.3f, 0.42f, 1f);
        idle = Load("idle"); walk = Load("walk"); talk = Load("talk"); toCounter = Load("toCounter");
        atCounter = Load("counter"); kiss = Load("kiss"); dance = Load("dance");
        if (persona == Persona.Dina && walk.Length >= 16)   // Dinan kävelyn neljä ensimmäistä kuvaa ovat käännös kameraan päin: ne saavat jalat liukumaan, joten silmukka alkaa sivuprofiilista
        {
            var w = new Sprite[walk.Length - 4]; System.Array.Copy(walk, 4, w, 0, w.Length); walk = w;
            walkFrameTime = Mathf.Max(walkFrameTime, 0.15f);   // askel vastaa kävelyvauhtia (1,4)
        }
        if (idle.Length > 0) body.sprite = idle[0];
        else Debug.LogWarning("ArcadeSini (" + npcName + "): kuvia ei löytynyt kansiosta Resources/" + folder + " (tuonti kesken tai kansio puuttuu?).", this);
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        shadow.sortingOrder = body.sortingOrder - 1;
        wait = Random.Range(waitRange.x, waitRange.y);
        mood = 20f;
        All.Add(this);
    }

    Sprite[] Load(string n)
    {
        var l = new List<Sprite>();
        for (int i = 0; ; i++)
        {
            var tx = Resources.Load<Texture2D>(folder + "/" + n + "_" + i.ToString("00"));
            if (tx == null) break;
            l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, PadPx / tx.height), CharPx / (height * sizeMul), 0, SpriteMeshType.FullRect));
        }
        return l.ToArray();
    }

    void OnDisable() { ReleaseHero(); }
    void OnDestroy() { ReleaseHero(); All.Remove(this); FreeCounter(); }
    void ReleaseHero() { SitCleanup(); if (chatPc != null) chatPc.enabled = true; chatPc = null; }

    // ---------- istuminen sohvalle (videoista: Resources/Kampa/sini_sit_NN, rocco_sit_NN; Tools/FlatAssets/make_sofa_frames.py) ----------
    Sprite[] LoadSit(string prefix, Vector2 pivot, float scale = 1f)
    {
        var l = new List<Sprite>();
        for (int i = 0; ; i++)
        {
            var tx = Resources.Load<Texture2D>("Kampa/" + prefix + "_sit_" + i.ToString("00"));
            if (tx == null) break;
            l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), pivot, CharPx / (height * sizeMul) / scale, 0, SpriteMeshType.FullRect));
        }
        return l.ToArray();
    }

    SpriteRenderer MakeSitter(string n, float x, bool flip, float lift = 0f)
    {
        var go = new GameObject(n);
        var sr = go.AddComponent<SpriteRenderer>();
        go.transform.position = new Vector3(x, sofaY + lift, 0f);
        sr.flipX = flip; sr.sortingOrder = Mathf.RoundToInt(-sofaY * 100f) + 1;
        return sr;
    }

    bool StartSit()
    {
        if (!hasSofa || chatPc == null) return false;
        if (sitS == null) sitS = LoadSit("sini", new Vector2(0.5377f, 0.0127f));
        if (sitR == null) sitR = LoadSit("rocco", new Vector2(0.5881f, 0.0318f), SitRoccoScale);
        if (sitS.Length == 0 || sitR.Length == 0) return false;
        sitPc = chatPc; sitPc.enabled = true; sitPc.Scripted = false;   // pelaaja kävelee itse sohvalle
        sitPhase = 1; sitT = 0f;
        return true;
    }

    void SitCleanup()
    {
        if (sitterR != null) Destroy(sitterR.gameObject);
        if (sitterS != null) Destroy(sitterS.gameObject);
        sitterR = sitterS = null;
        if (sitPc != null)
        {
            sitPc.Scripted = false; sitPc.ScriptedMove = Vector2.zero;
            if (sitPc.BodyRenderer != null) sitPc.BodyRenderer.enabled = true;
            if (sitPc.shadow != null) sitPc.shadow.enabled = true;
            sitPc = null;
        }
        if (sitPhase != 0 && body != null) { body.enabled = true; shadow.enabled = true; }
        sitPhase = 0;
    }

    static Sprite Hold(Sprite[] s, float t, float ft, bool reverse)
    {
        int i = Mathf.Min(s.Length - 1, (int)(t / ft));
        return s[reverse ? s.Length - 1 - i : i];
    }

    /// Rocco kävelee sohvalle ja istuu, sitten Sini tulee perässä; sitten dialogi jatkuu (solmu 1).
    void UpdateSit(Vector3 p, int pi)
    {
        sitT += Time.deltaTime;
        if (sitPhase != 6 && sitPhase != 1 && CoopInput.Kick(pi)) { SitCleanup(); treeMode = false; EndChat("Nähdään!", false); return; }
        switch (sitPhase)
        {
            case 1:
            {
                Vector2 d = new Vector2(sofaRoccoX, sofaY) - (Vector2)sitPc.transform.position;
                if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
                if (sitT > 25f) { sitPc.Scripted = true; sitPc.ScriptedMove = d.normalized; sitPc.Face(d.x >= 0f); }   // jos ei löydä: ohjataan perille
                if ((Mathf.Abs(d.x) < 1.0f && Mathf.Abs(d.y) < 0.9f) || sitT > 40f)
                {
                    sitPc.Scripted = false; sitPc.ScriptedMove = Vector2.zero; sitPc.enabled = false;
                    sitPc.transform.position = new Vector3(sofaRoccoX, sofaY, sitPc.transform.position.z);
                    if (sitPc.BodyRenderer != null) sitPc.BodyRenderer.enabled = false;
                    if (sitPc.shadow != null) sitPc.shadow.enabled = false;
                    sitterR = MakeSitter("Rocco sohvalla", sofaRoccoX, false, 0.15f);
                    sitterR.sprite = sitR[0];
                    sitPhase = 2; sitT = 0f;
                }
                break;
            }
            case 2:
                sitterR.sprite = Hold(sitR, sitT, SitRoccoFt, false);
                if (sitT > sitR.Length * SitRoccoFt + 0.3f) { goal = new Vector2(sofaSiniX, sofaY); sitPhase = 3; sitT = 0f; }
                break;
            case 3:
                if (Has(walk)) body.sprite = Loop(walk, sitT, walkFrameTime);
                if (MoveToGoal(p, Time.deltaTime, speed * 1.2f) || sitT > 12f)
                {
                    transform.position = new Vector3(sofaSiniX, sofaY, 0f);
                    body.enabled = false; shadow.enabled = false;
                    sitterS = MakeSitter("Sini sohvalla", sofaSiniX, true);   // peilattu: istuvat vastakkain
                    sitterS.sprite = sitS[0];
                    sitPhase = 4; sitT = 0f;
                }
                break;
            case 4:
                sitterS.sprite = Hold(sitS, sitT, SitSiniFt, false);
                if (sitT > sitS.Length * SitSiniFt + 0.4f) { sitPhase = 5; GoNode(1); }
                break;
            case 6:
                sitterR.sprite = Hold(sitR, sitT, SitRoccoFt, true);
                sitterS.sprite = Hold(sitS, sitT, SitSiniFt, true);
                if (sitT > Mathf.Max(sitR.Length * SitRoccoFt, sitS.Length * SitSiniFt) + 0.3f) { SitCleanup(); FinishTree(); }
                break;
        }
    }

    float Limit(float x) => area != null ? area.MaxDepthAtX(x, area.maxDepthY) : -1f;
    static bool Has(Sprite[] s) => s != null && s.Length > 0;

    static Sprite Loop(Sprite[] s, float t, float ft) => s[(int)(t / ft) % s.Length];
    static Sprite PingPong(Sprite[] s, float t, float ft)
    {
        if (s.Length < 2) return s[0];
        int cyc = 2 * s.Length - 2, k = (int)(t / ft) % cyc;
        return s[k < s.Length ? k : cyc - k];
    }

    void Update()
    {
        if (body == null || (area != null && Area.Current != area)) return;
        float dt = Time.deltaTime; t += dt; phaseT += dt;
        if (chatCooldown > 0f) chatCooldown -= dt;
        if (brawl != null && brawl.Started && state != S.Hide) EnterHide();
        Vector3 p = transform.position;
        switch (state)
        {
            case S.Idle:
                if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
                FaceHeroIfNear(p);
                wait -= dt;
                if (wait <= 0f) PickGoal();
                break;

            case S.Walk:
                if (Has(walk)) body.sprite = Loop(walk, t, walkFrameTime);
                if (MoveToGoal(p, dt, speed)) Arrive();
                break;

            case S.ToCounter:
            {
                int i = (int)(t / toCounterFrameTime);
                if (i >= toCounter.Length) { Set(S.AtCounter); wait = Random.Range(counterStay.x, counterStay.y); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.AtCounter:
                body.sprite = PingPong(atCounter, t, counterFrameTime);
                wait -= dt;
                if (wait <= 0f) Set(S.FromCounter);
                break;

            case S.FromCounter:
            {
                int i = toCounter.Length - 1 - (int)(t / toCounterFrameTime);
                if (i < 0) { FreeCounter(); Set(S.Idle); wait = Random.Range(0.5f, 1.5f); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.Listen:
                if (listenTo == null) { Set(S.Idle); wait = 1f; break; }
                flipped = listenTo.transform.position.x < p.x;
                body.sprite = (Time.time * 0.3f) % 1f < 0.5f && Has(talk) ? Loop(talk, t, talkFrameTime) : PingPong(idle, t, idleFrameTime);
                stateLeft -= dt;
                if (stateLeft <= 0f) { listenTo = null; Set(S.Idle); wait = Random.Range(waitRange.x, waitRange.y); }
                break;

            case S.Dance:
                body.sprite = PingPong(dance, t, danceFrameTime);
                stateLeft -= dt;
                if (stateLeft <= 0f) { Set(S.Idle); wait = Random.Range(waitRange.x, waitRange.y); }
                break;

            case S.TalkNpc:
                {
                    Transform pt = friendTarget != null ? friendTarget.transform : (partner != null ? partner.transform : null);
                    bool lost = pt == null || (partner != null && partner.IsDead) || Mathf.Abs(pt.position.x - p.x) > 4f || (friendTarget != null && friendTarget.state != S.Listen);
                    if (lost) { EndTalkWithFriend(); Set(S.Idle); wait = 1f; break; }
                    flipped = pt.position.x < p.x;
                }
                body.sprite = Loop(talk, t, talkFrameTime);
                if (Time.time >= chatterAt) { Say(Chatter[Random.Range(0, Chatter.Length)], 1.6f); chatterAt = Time.time + Random.Range(2.5f, 5f); }
                stateLeft -= dt;
                if (stateLeft <= 0f) { EndTalkWithFriend(); Set(S.Idle); wait = Random.Range(waitRange.x, waitRange.y); }
                break;

            case S.Chat: UpdateChat(p); break;

            case S.Kiss:
            {
                int i = (int)(t / kissFrameTime);
                if (i >= kiss.Length)
                {
                    Set(S.Dance); stateLeft = Random.Range(8f, 12f);
                    break;
                }
                body.sprite = kiss[i];
                break;
            }

            case S.ToDoor:
                if (Has(walk)) body.sprite = Loop(walk, t, walkFrameTime);
                if (MoveToGoal(p, dt, speed * 1.2f))
                {
                    // sisään ovesta: piiloon yläkertaan (kämpän Sini odottaa sisällä)
                    body.enabled = false; shadow.enabled = false; Set(S.AtDoor); stateLeft = 90f;
                }
                break;

            case S.AtDoor:
                stateLeft -= dt;
                if (stateLeft <= 0f) { body.enabled = true; shadow.enabled = true; Set(S.Idle); wait = 1f; }
                break;

            case S.Hide:
                if (!MoveToGoal(p, dt, speed * 1.7f) ) { if (Has(walk)) body.sprite = Loop(walk, t, walkFrameTime * 0.7f); }
                else if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
                break;
        }
        if (state != S.AtDoor) UpdatePrompt(p);
        body.flipX = flipped != (persona == Persona.Dina);   // Dinan kuvat katsovat vasemmalle, Sinin oikealle
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        shadow.sortingOrder = body.sortingOrder - 1;
    }

    void Set(S s) { state = s; t = 0f; }

    void FaceHeroIfNear(Vector3 p)
    {
        var q = Players.Nearest(p);
        if (q == null) return;
        Vector3 hp = q.transform.position;
        if (Mathf.Abs(hp.x - p.x) < 4f && Mathf.Abs(hp.y - p.y) < 2.5f) flipped = hp.x < p.x;
    }

    bool MoveToGoal(Vector3 p, float dt, float sp)
    {
        Vector2 d = goal - (Vector2)p;
        float step = sp * dt;
        if (Mathf.Abs(d.x) > 0.05f) flipped = d.x < 0f;
        if (d.magnitude <= step) { transform.position = new Vector3(goal.x, goal.y, 0f); return true; }
        transform.position = (Vector2)p + d.normalized * step;
        return false;
    }

    void Arrive()
    {
        if (goalKind == 1 && Has(toCounter) && Has(atCounter)) Set(S.ToCounter);
        else if (goalKind == 2 && Has(dance)) { Set(S.Dance); stateLeft = Random.Range(danceTime.x, danceTime.y); flipped = false; }
        else if (goalKind == 3 && Has(talk) && (partner != null || friendTarget != null))
        {
            Set(S.TalkNpc); stateLeft = Random.Range(talkTime.x, talkTime.y); chatterAt = Time.time + 1.5f;
            if (friendTarget != null) { if (friendTarget.Available()) friendTarget.BeginListen(this, stateLeft); else { friendTarget = null; Set(S.Idle); wait = 1f; } }
        }
        else { Set(S.Idle); wait = Random.Range(waitRange.x, waitRange.y); }
    }

    Enemy FindPartner()
    {
        var list = new List<Enemy>();
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (e == null || e.IsDead || !e.isActiveAndEnabled) continue;
            Vector3 q = e.transform.position;
            if (q.x < laneMinX || q.x > laneMaxX || Mathf.Abs(q.y - transform.position.y) > 4f) continue;
            list.Add(e);
        }
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    void PickGoal()
    {
        float r = Random.value;
        if (cheered && Time.time < cheeredUntil) r = r < 0.5f ? 0.4f : r;   // hyvällä tuulella tanssii useammin
        goalKind = 0; partner = null; friendTarget = null; FreeCounter();
        float x, y;
        if (counterXs != null && counterXs.Length > 0 && r < counterChance && Has(toCounter) && Has(atCounter))
        {
            x = FreeCounterX();
            if (!float.IsNaN(x)) { goalKind = 1; counterX = x; y = Limit(x) - 0.08f; }
            else { x = Random.Range(laneMinX, laneMaxX); y = Limit(x) - Random.Range(0.5f, 2.4f); }
        }
        else if (Has(dance) && r < counterChance + danceChance)
        {
            goalKind = 2; x = Random.Range(laneMinX + 1f, laneMaxX - 1f); y = Limit(x) - Random.Range(0.9f, 2.2f);
        }
        else if (Has(talk) && r < counterChance + danceChance + talkChance && PickPartner())
        {
            goalKind = 3;
            Vector3 pp = friendTarget != null ? friendTarget.transform.position : partner.transform.position;
            x = Mathf.Clamp(pp.x + (Random.value < 0.5f ? -1.5f : 1.5f), laneMinX, laneMaxX); y = pp.y;
        }
        else
        {
            x = Random.Range(laneMinX, laneMaxX); y = Limit(x) - Random.Range(0.5f, 2.4f);
        }
        goal = new Vector2(x, y);
        Set(S.Walk);
    }

    bool PickPartner()
    {
        partner = null; friendTarget = null;
        if (friend != null && friend.Available() && Random.value < 0.55f) { friendTarget = friend; return true; }
        partner = FindPartner();
        return partner != null;
    }

    /// Voiko toinen hahmo tulla juttelemaan (ei ole tiskillä, jutussa, suukossa eikä piilossa).
    public bool Available() => state == S.Idle || state == S.Walk || state == S.Dance;

    void BeginListen(ArcadeSini visitor, float seconds)
    {
        listenTo = visitor; stateLeft = seconds; Set(S.Listen); FreeCounter();
    }

    void EndTalkWithFriend()
    {
        if (friendTarget != null && friendTarget.listenTo == this) { friendTarget.listenTo = null; }
        friendTarget = null;
    }

    float FreeCounterX()
    {
        var free = new List<float>();
        foreach (float cx in counterXs)
        {
            bool taken = false;
            foreach (var o in All) if (o != this && o != null && !float.IsNaN(o.counterX) && Mathf.Abs(o.counterX - cx) < 0.5f) taken = true;
            if (!taken) free.Add(cx);
        }
        return free.Count == 0 ? float.NaN : free[Random.Range(0, free.Count)];
    }

    void FreeCounter() { counterX = float.NaN; }

    /// Sini kävelee hallin yläkerran ovelle ("seuraa mua"). Palauttaa false, jos ovea ei löydy.
    bool WalkToDoor()
    {
        foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d == null || d.name != "Pelihallin yläkerran ovi") continue;
            ReleaseHero(); FreeCounter(); EndTalkWithFriend(); listenTo = null;
            goal = new Vector2(d.transform.position.x, Limit(d.transform.position.x) - 0.15f);
            goalKind = 4;
            Set(S.ToDoor);
            return true;
        }
        return false;
    }

    void EnterHide()
    {
        ReleaseHero(); FreeCounter(); EndTalkWithFriend(); listenTo = null;
        Set(S.Hide);
        goal = new Vector2(laneMinX + 0.8f, Limit(laneMinX) - 0.6f);
        Say("Apua!", 2f);
    }

    void Say(string s, float len)
    {
        bubble = Loc.T(s);
        Voice(s);
        bubbleUntil = Time.time + Mathf.Max(len, voiceLen + 0.3f);
    }

    // ---------- Sinin ääninäyttelijän repliikit (Resources/Sfx/sini/) ----------
    static readonly System.Collections.Generic.Dictionary<string, string> Voices = new System.Collections.Generic.Dictionary<string, string>
    {
        { "Kone söi viimeiset kolikkoni. Taas.", "sini_ask_1" },
        { "Kaikki muut voittavat täällä, vaan en minä.", "sini_ask_2" },
        { "Olisi kiva tanssia, mutta ei jaksa yhtään.", "sini_ask_3" },
        { "Tuo oli kiltisti sanottu.", "sini_r1_hyva" },
        { "No, ehkä. Ei se silti auta.", "sini_r1_kesk" },
        { "Kiitti vaan. Todella hauska.", "sini_r1_huono" },
        { "Hah. Sanoit sen ihan vakavalla naamalla.", "sini_r2_hyva" },
        { "Toivottavasti sinulla on oikeus.", "sini_r2_kesk" },
        { "Aina yhtä lohdullista.", "sini_r2_huono" },
        { "Okei, okei. Se riittää.", "sini_r3_hyva" },
        { "Tai niin.", "sini_r3_kesk" },
        { "Tuo ei auttanut yhtään.", "sini_r3_huono" },
        { "Kiitos. Tarvitsin tuota todella paljon.", "sini_verdict_hyva" },
        { "Hmm. En nyt oikein jaksa. Mene vaan.", "sini_verdict_huono" },
        { "Hei! Mulla menee tosi hyvin.", "sini_hyva" },
        { "Nähdään!", "sini_heippa" },
        { "No heippa sitten.", "sini_heippa2" },
        { "Hei, minne sinä menet?", "sini_minne" },
    };
    AudioSource voiceSrc;
    float voiceLen;

    /// Soittaa repliikin äänen, jos sellainen on (vain Sini; Dinalla ei vielä ääniä). Tallentaa kestonsa, jotta puhe ei katkea seuraavaan.
    void Voice(string line)
    {
        voiceLen = 0f;
        if (persona != Persona.Sini || line == null || !Voices.TryGetValue(line, out var key)) return;
        VoiceKey(key);
    }

    void VoiceKey(string key)
    {
        voiceLen = 0f;
        if (persona != Persona.Sini || string.IsNullOrEmpty(key)) return;
        var clip = Resources.Load<AudioClip>("Sfx/sini/" + key);
        if (clip == null) return;
        if (voiceSrc == null)
        {
            voiceSrc = gameObject.AddComponent<AudioSource>();
            voiceSrc.playOnAwake = false; voiceSrc.spatialBlend = 0f; voiceSrc.volume = 0.9f;
        }
        voiceSrc.Stop();
        voiceSrc.clip = clip;
        voiceSrc.Play();
        voiceLen = clip.length;
    }

    // ---------- juttelu heron kanssa ----------
    void UpdatePrompt(Vector3 p)
    {
        near = false;
        if (state == S.Chat || state == S.Hide || state == S.Kiss || state == S.ToCounter || state == S.FromCounter) return;
        if ((StarShooter.Active || KarateGame.Active || FlipperGame.Active) || PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen) return;
        var q = Players.Nearest(p);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 hp = q.transform.position;
        if (Mathf.Abs(hp.x - p.x) > talkHalfWidth || Mathf.Abs(hp.y - p.y) > talkDepth) return;
        if (chatCooldown > 0f) return;
        near = true;
        if (!CoopInput.Pick(q.playerIndex)) return;
        if (persona == Persona.Sini && home && afterKiss && Time.time >= nextGift)
        {
            // makkariepisodin jälkeen kämpässä: lentosuukko (ei useammin kuin 40 s välein); hallissa Sini juttelee normaalisti
            flipped = hp.x < p.x; nextGift = Time.time + 40f; Say("Sinä taas! Tässä sulle.", 1.6f); Set(S.Kiss);
            return;
        }
        StartChat(q);
    }

    void StartChat(PlayerController q)
    {
        EndTalkWithFriend(); listenTo = null;
        chatPc = q; chatPc.enabled = false;
        flipped = q.transform.position.x < transform.position.x;
        Set(S.Chat);
        stage = 0; mood = 20f; sel = 0; lastY = 0f;
        coffeePages = false;
        treeMode = false;
        if (persona == Persona.Sini && home && coffeeVisits == 0) { BeginTree(false); return; }
        if (persona == Persona.Sini && home) { coffeePages = true; pages = BuildCoffeePages(); page = 0; phaseT = 0f; return; }
        if (persona == Persona.Dina) { pages = BuildDinaPages(); page = 0; phaseT = 0f; return; }
        BeginSay();
    }

    // ---------- Dina: Tähtipyssy-ennätys ----------
    const string ClaimKey = "dina_record_claimed";
    static int BestScore() => PlayerPrefs.GetInt("tahtipyssy_hi", 0);
    bool Claimed => PlayerPrefs.GetInt(ClaimKey, 0) == 1;

    string[] BuildDinaPages()
    {
        int hi = BestScore();
        if (hi > dinaRecord && !Claimed)
            return new[] { "Hetkinen... " + hi.ToString("N0") + " pistettä? Sinä rikoit ennätykseni!", "Reilusti voitettu. Sovittu on sovittu: tämä on sinulle." + (rewardMoney > 0 ? " (+" + rewardMoney + ")" : "") };
        if (Claimed)
            return new[] { "Sinä olet nyt Tähtipyssyn kuningas. Ennätys on sinun: " + hi.ToString("N0") + ".", "Minä harjoittelen vielä. Kyllä minä sen vielä takaisin otan." };
        return new[] {
            "Moi, olen Dina.",
            "Pelaan Tähtipyssyä täällä melkein joka ilta.",
            "Ennätykseni on " + dinaRecord.ToString("N0") + " pistettä." + (hi > 0 ? " Sinun parhaasi on " + hi.ToString("N0") + "." : " Etkö ole vielä pelannut?"),
            "Kone on tuolla seinän vieressä. Rikot ennätykseni, niin saat multa jotain.",
        };
    }

    // ---------- puhepuu: kutsu ja ensimmäinen kahvikerta (Sini kysyy, Rocco valitsee) ----------
    // next: seuraava solmu; -1 = loppu, -2 = loppu: ruutu himmenee (makuuhuoneen kuva: Resources/Kampa/suudelma)
    class TNode { public string line, voice; public string[] opts; public int[] next; public int auto = -1; }

    static readonly TNode[] InviteTree =
    {
        new TNode { line = "Hei kuule, haluaisitko tulla käymään kahvilla? Me asutaan Dinan kanssa tässä yläkerrassa.", voice = "sini_kutsu_0",
            opts = new[] { "Mielelläni, kiitos.", "Ehkä joku toinen kerta." }, next = new[] { 1, 2 } },
        new TNode { line = "Kiva, seuraa mua!", voice = "sini_kutsu_joo", auto = -1 },
        new TNode { line = "Okei, joku toinen kerta sitten.", voice = "sini_kutsu_ei", auto = -1 },
    };

    static readonly TNode[] CoffeeTree =
    {
        /*0*/ new TNode { line = "Tervetuloa meidän majaan! Mä laitan kahvit tulille. Istu vaikka sohvalle.", voice = "sini_k_tervetuloa", auto = 1 },
        /*1*/ new TNode { line = "On varmaan rankkaa mätkiä noita Lokin kätyreitä?", voice = "sini_k_lokki",
            opts = new[] { "No käyhän se työstä.", "Menee treenistä.", "No kyllä mieluummin istun tässä sun kanssa." }, next = new[] { 2, 2, 3 } },
        /*2*/ new TNode { line = "Haha, sä oot kyl hauska! Oot muuten käynyt sparraamassa Marcusta vastaan salilla?", voice = "sini_k_hauska",
            opts = new[] { "Joo kävin. Annoin köniin.", "Joo kävin. Hiton kova jätkä, täytyy reenata lisää sen kanssa.", "En oo käynyt vielä." }, next = new[] { 6, 6, 6 } },   // Marcus-haara: Sini lähtee levolle (solmu 6)
        /*3*/ new TNode { line = "Jospa kadut rauhoittuisi pian... Kuule Rocco, onko sulla ketään? Niinku sillee, no tiiätsä mitä mä tarkotan.", voice = "sini_k_rauhoittuisi",
            opts = new[] { "No onhan mulla. Ollut jo pidemmän aikaa.", "Ei ole tällä hetkellä." }, next = new[] { 6, 7 } },
        /*4*/ new TNode { line = "Kuule Rocco, onko sulla ketään? Niinku sillee, no tiiätsä mitä mä tarkotan.", voice = "sini_k_ketaan",
            opts = new[] { "No onhan mulla. Ollut jo pidemmän aikaa.", "Ei ole tällä hetkellä." }, next = new[] { 6, 7 } },
        /*5*/ null,
        /*6*/ new TNode { line = "No hyvä! Mun täytyy varmaan lähteä nyt levolle. Olipa kiva kun kävit, käy uudestaan!", voice = "sini_k_levolle", auto = -1 },
        /*7*/ new TNode { line = "Nooo, tuu nyt jätkä vähän lähemmäs sitten!", voice = "sini_k_lahemmas",
            opts = new[] { "Mun täytyy mennä nyt.", "Täältä mää tuun." }, next = new[] { 8, -2 } },
        /*8*/ new TNode { line = "Nähdään taas!", voice = "sini_k_nahdaan", auto = -1 },
    };

    TNode[] Tree => inviteTree ? InviteTree : CoffeeTree;

    void BeginTree(bool invite)
    {
        treeMode = true; inviteTree = invite; inviteAccepted = false; treeChoose = false; treeFade = false; node = 0; phaseT = 0f; treeSel = 0;
        VoiceKey(Tree[0].voice);
    }

    void GoNode(int n)
    {
        if (n < 0 && n != -2 && sitPhase == 5) { sitPhase = 6; sitT = 0f; treeChoose = false; return; }   // nousevat ylös ennen lähtöä
        if (n == -2) { SitCleanup(); treeFade = true; phaseT = 0f; treeChoose = false; StartKiss(); return; }
        if (n < 0 || Tree[n] == null) { FinishTree(); return; }
        node = n; treeChoose = false; phaseT = 0f; treeSel = 0;
        if (inviteTree && n == 1) inviteAccepted = true;   // "Kiva, seuraa mua!"
        VoiceKey(Tree[n].voice);
    }

    // makuuhuonekohtaus: Assets/StreamingAssets/Kampa/suudelma_1.mp4, _2, _3 ... toistetaan peräkkäin (Tools/FlatAssets/make_kiss_video.py)
    string[] kissPaths; int kissIndex; AudioClip kissMusic; bool kissMusicStarted;

    void StartKiss()
    {
        kissDone = false; kissVideo = false; kissIndex = 0;
        var list = new List<string>();
        for (int k = 1; k < 20; k++)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Kampa/suudelma_" + k + ".mp4");
            if (System.IO.File.Exists(path)) list.Add(path); else break;
        }
        if (list.Count == 0) return;
        kissPaths = list.ToArray();
        kissRt = new RenderTexture(1280, 720, 0);
        kissVp = gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
        kissVp.playOnAwake = false; kissVp.isLooping = false;
        kissVp.source = UnityEngine.Video.VideoSource.Url;
        kissVp.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture; kissVp.targetTexture = kissRt;
        kissVp.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;   // taustamusiikki (Resources/Music/rocco_ja_sini) hoitaa äänen
        kissVp.errorReceived += (v, m) => { Debug.LogWarning("Suudelmavideo: " + m); NextKiss(); };
        kissVp.loopPointReached += v => NextKiss();
        kissVp.prepareCompleted += v => v.Play();
        kissVp.url = kissPaths[0];
        kissVp.isLooping = kissPaths.Length == 1;
        kissVp.Prepare();
        kissVideo = true;
        // taustamusiikki: kohtaus päättyy, kun kappale loppuu (tai kun pelaaja keskeyttää)
        kissMusic = Resources.Load<AudioClip>("Music/rocco_ja_sini");
        kissMusicStarted = false;
        if (kissMusic != null) { MusicPlayer.PlayOverride(kissMusic, 2f, false); kissMusicStarted = true; }
    }

    void NextKiss()
    {
        kissIndex++;
        if (kissVp != null && kissVp.isLooping) { kissIndex--; return; }   // viimeinen video pyörii silmukassa: loppuu vasta musiikin loppuessa tai keskeytyksellä
        if (kissVp == null || kissPaths == null || kissIndex >= kissPaths.Length) { kissDone = true; return; }
        kissVp.Stop();
        kissVp.url = kissPaths[kissIndex];
        kissVp.isLooping = kissIndex == kissPaths.Length - 1;   // viimeinen (tanssi) pyörii edestakaisin, kunnes musiikki loppuu tai pelaaja keskeyttää
        kissVp.Prepare();
    }

    void StopKiss()
    {
        if (kissVp != null) { kissVp.Stop(); Destroy(kissVp); kissVp = null; }
        if (kissMusicStarted) { MusicPlayer.StopOverride(1.5f); kissMusicStarted = false; }
        if (kissRt != null) { kissRt.Release(); Destroy(kissRt); kissRt = null; }
        kissVideo = false;
    }

    void FinishTree()
    {
        StopKiss();
        bool invite = inviteTree, fromKiss = treeFade;
        if (fromKiss) afterKiss = true;
        var pc = chatPc;
        treeMode = false; treeFade = false;
        if (invite) { EndChat(null, true); return; }
        Coffee(pc);
        EndChat(null, false);
        if (fromKiss && pc != null)
        {
            // kohtauksen jälkeen peli jatkuu makkarista (siellä voi kävellä; ovi vie olohuoneeseen ja sieltä halliin)
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (d != null && d.name == "Olohuoneesta makkariin") { d.GoNow(pc); return; }
            foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (d != null && d.here == area && d.returnToLastDoor) { d.GoNow(pc); break; }
        }
    }

    void UpdateTree(Vector3 p)
    {
        if (chatPc == null) { treeMode = false; Set(S.Idle); return; }
        int i = chatPc.playerIndex;
        if (sitPhase != 0 && sitPhase != 5) { UpdateSit(p, i); return; }
        if (sitPhase == 5)
        {
            sitterS.sprite = sitS[sitS.Length - 1]; sitterR.sprite = sitR[sitR.Length - 1];
            if (CoopInput.Kick(i)) { SitCleanup(); treeMode = false; EndChat("Nähdään!", false); return; }
        }
        if (treeFade)
        {
            if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
            if (kissVideo)
            {
                // videot soivat peräkkäin; mikä tahansa nappi lopettaa heti ja palaa halliin; varalta aikakatkaisu
                bool skipKiss = phaseT > 0.5f && (CoopInput.Pick(i) || CoopInput.Punch(i) || CoopInput.Kick(i) || CoopInput.StartPressed(i));
                bool musicEnded = kissMusicStarted && phaseT > 3f && !MusicPlayer.OverridePlaying;
                if (kissDone || musicEnded || skipKiss || phaseT > (kissMusic != null ? kissMusic.length + 10f : 40f)) FinishTree();
            }
            else if (phaseT > 4.5f) FinishTree();
            return;
        }
        var nd = Tree[node];
        if (!treeChoose && Has(talk)) body.sprite = Loop(talk, t, talkFrameTime);
        else if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
        if (CoopInput.Kick(i)) { if (inviteTree) FinishTree(); else { treeMode = false; EndChat("Nähdään!", false); } return; }
        if (!treeChoose)
        {
            float dur = voiceLen > 0f ? voiceLen + 0.5f : Mathf.Max(2.4f, nd.line.Length * 0.06f);
            bool skip = phaseT > 0.6f && (CoopInput.Pick(i) || CoopInput.Punch(i));
            if (phaseT > dur || skip)
            {
                if (nd.opts != null) { treeChoose = true; phaseT = 0f; treeSel = 0; treeLastY = 0f; }
                else if (!inviteTree && node == 0 && sitPhase == 0 && StartSit()) { }   // "istu sohvalle": kävellään istumaan, dialogi jatkuu sen jälkeen
                else GoNode(nd.auto);
            }
        }
        else
        {
            int n = nd.opts.Length;
            float y = CoopInput.Move(i).y;
            if (y > 0.6f && treeLastY <= 0.6f) treeSel = (treeSel + n - 1) % n;
            else if (y < -0.6f && treeLastY >= -0.6f) treeSel = (treeSel + 1) % n;
            treeLastY = y;
            if (phaseT > 0.25f && (CoopInput.Pick(i) || CoopInput.Punch(i))) GoNode(nd.next[treeSel]);
        }
        if (chatPc != null && Mathf.Abs(chatPc.transform.position.x - p.x) > 6f) { treeMode = false; EndChat("Hei, minne sinä menet?", false); }
    }

    void DrawTree(float W, float H)
    {
        if (treeFade)
        {
            if (fadeTex == null) { fadeTex = new Texture2D(1, 1); fadeTex.SetPixel(0, 0, Color.black); fadeTex.Apply(); }
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, Mathf.Clamp01(phaseT / 1.2f));
            GUI.DrawTexture(new Rect(0, 0, W, H), fadeTex);
            if (kissVideo && kissRt != null && kissVp != null && kissVp.isPlaying)
            {
                GUI.color = Color.white;
                float vh = H, vw = vh * 16f / 9f;
                if (vw > W) { vw = W; vh = vw * 9f / 16f; }
                GUI.DrawTexture(new Rect((W - vw) * 0.5f, (H - vh) * 0.5f, vw, vh), kissRt, ScaleMode.StretchToFill, false);
            }
            else if (!kissVideo && (kissTex != null || (kissTex = Resources.Load<Texture2D>("Kampa/suudelma")) != null))
            {
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01((phaseT - 0.8f) / 1f));
                float ih = H * 0.9f, iw = ih * kissTex.width / kissTex.height;
                GUI.DrawTexture(new Rect((W - iw) * 0.5f, (H - ih) * 0.5f, iw, ih), kissTex);
            }
            GUI.color = old;
            return;
        }
        if (sitPhase == 1) { GUI.Box(new Rect(W * 0.3f, H * 0.86f, W * 0.4f, H * 0.06f), Loc.T("Kävele sohvalle"), box); return; }
        if (sitPhase != 0 && sitPhase != 5) return;   // istuminen: ei puhekuplaa
        var nd = Tree[node];
        float w = H * 1.15f, x = (W - w) * 0.5f, lh = H * 0.075f, y = H * 0.56f;
        GUI.Box(new Rect(x, y, w, lh * 1.5f), npcName + ": " + Loc.T(nd.line), box);
        y += lh * 1.6f;
        if (treeChoose && nd.opts != null)
        {
            for (int k = 0; k < nd.opts.Length; k++)
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = k == treeSel ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
                GUI.Box(new Rect(x, y, w, lh), (k == treeSel ? "▶  " : "    ") + Loc.T(nd.opts[k]), small);
                GUI.backgroundColor = old;
                y += lh * 1.05f;
            }
        }
    }

    string[] BuildCoffeePages()
    {
        if (coffeeVisits == 0)
            return new[] {
                "Tervetuloa. Älä katso pyykkiä. Se ei ole siinä.",
                "Istu. Siinä tuolissa. Toinen heiluu.",
                "Kuule, sinä olet ensimmäinen, joka on tullut tänne asti.",
                "Täällä on yleensä niin meluisaa. Koneet, tappelut, Dina huutaa pisteitään... tykkään siitä, mutta joskus tarvitsee hiljaisuutta.",
                "Dina pelaa Tähtipyssyä melkein joka ilta. Hän ei sano sitä ääneen, mutta hän haluaisi, että joku rikkoo hänen ennätyksensä.",
                "Sinä et ole ollenkaan sellainen kuin luulin. Hyvä.",
                "Tässä, kahvi. Kiitos kun tulit.",
            };
        return new[] { "Taas sinä. Pannu on kuuma.", "Istu vaan. Ei tarvitse puhua, jos ei huvita.", "Tässä, kahvi. Hyvä, että tulit." };
    }

    /// Kahvi: ensimmäisellä kerralla täysi lepo, myöhemmin vähän.
    void Coffee(PlayerController pc)
    {
        if (pc == null) return;
        pc.Heal(coffeeVisits == 0 ? pc.maxHealth : 40);
        pc.stamina = pc.maxStamina;
        coffeeVisits++;
    }

    void UpdateChatDina(Vector3 p)
    {
        if (chatPc == null) { Set(S.Idle); return; }
        int i = chatPc.playerIndex;
        if (Has(talk)) body.sprite = Loop(talk, t, talkFrameTime);
        if (CoopInput.Kick(i)) { EndChat("Nähdään!", false); return; }
        if (phaseT > 0.3f && (CoopInput.Pick(i) || CoopInput.Punch(i)))
        {
            page++; phaseT = 0f;
            if (page >= pages.Length)
            {
                if (coffeePages) Coffee(chatPc);
                else if (BestScore() > dinaRecord && !Claimed)
                {
                    var pc = chatPc;
                    PlayerPrefs.SetInt(ClaimKey, 1); PlayerPrefs.Save();
                    if (rewardMoney > 0) pc.AddMoney(rewardMoney);
                    RecordBeaten?.Invoke(pc);
                }
                EndChat("Nähdään!", false);
                return;   // EndChat nollaa chatPc:n
            }
        }
        if (chatPc != null && Mathf.Abs(chatPc.transform.position.x - p.x) > 6f) EndChat(null, false);
    }

    void DrawDinaChat(float W, float H)
    {
        if (pages == null || page >= pages.Length) return;
        float w = H * 1.15f, x = (W - w) * 0.5f, lh = H * 0.075f, y = H * 0.66f;
        GUI.Box(new Rect(x, y, w, lh * 1.7f), npcName + ": " + Loc.T(pages[page]), box);
        GUI.Label(new Rect(x, y + lh * 1.75f, w, lh * 0.8f), Loc.T("E jatka, K poistuu"), small);
    }

    void BeginSay() { phase = Ph.Say; phaseT = 0f; Voice(Asks[Mathf.Min(stage, Asks.Length - 1)]); }

    void UpdateChat(Vector3 p)
    {
        if (treeMode) { UpdateTree(p); return; }
        if (persona == Persona.Dina || coffeePages) { UpdateChatDina(p); return; }
        if (chatPc == null) { Set(S.Idle); return; }
        int i = chatPc.playerIndex;
        bool speaking = phase == Ph.Say || phase == Ph.React || phase == Ph.Verdict;
        if (speaking && Has(talk)) body.sprite = Loop(talk, t, talkFrameTime);
        else if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
        switch (phase)
        {
            case Ph.Say:
                if (phaseT > Mathf.Max(2.4f, voiceLen + 0.5f))
                {
                    var rnd = new System.Random();
                    order[0] = 0; order[1] = 1; order[2] = 2;
                    for (int a = 2; a > 0; a--) { int b = rnd.Next(a + 1); int tmp = order[a]; order[a] = order[b]; order[b] = tmp; }
                    sel = 0; phase = Ph.Choose; phaseT = 0f; lastY = 0f;
                }
                break;
            case Ph.Choose:
            {
                float y = CoopInput.Move(i).y;
                if (y > 0.6f && lastY <= 0.6f) sel = (sel + 2) % 3;
                else if (y < -0.6f && lastY >= -0.6f) sel = (sel + 1) % 3;
                lastY = y;
                if (CoopInput.Kick(i)) { EndChat("No heippa sitten.", false); break; }
                if (phaseT > 0.25f && (CoopInput.Pick(i) || CoopInput.Punch(i)))
                {
                    var o = Rounds[stage][order[sel]];
                    mood = Mathf.Clamp(mood + o.gain, 0f, 100f);
                    reaction = o.reply; phase = Ph.React; phaseT = 0f; Voice(o.reply);
                }
                if (chatPc != null && Mathf.Abs(chatPc.transform.position.x - p.x) > 6f) EndChat("Hei, minne sinä menet?", false);
                break;
            }
            case Ph.React:
                if (phaseT > Mathf.Max(2.1f, voiceLen + 0.4f))
                {
                    stage++;
                    if (stage >= Rounds.Length) { phase = Ph.Verdict; phaseT = 0f; Voice(mood >= 70f ? "Kiitos. Tarvitsin tuota todella paljon." : "Hmm. En nyt oikein jaksa. Mene vaan."); }
                    else BeginSay();
                }
                break;
            case Ph.Verdict:
                if (phaseT > Mathf.Max(2.4f, voiceLen + 0.5f))
                {
                    if (mood >= 70f)
                    {
                        if (!home && !Clues.Has(InviteClue)) BeginTree(true);   // kutsu kahville (valinta)
                        else EndChat(null, true);
                    }
                    else EndChat(null, false);
                }
                break;
        }
    }

    void EndChat(string line, bool success)
    {
        coffeePages = false; treeMode = false;
        ReleaseHero();
        if (success)
        {
            cheered = true; cheeredUntil = Time.time + 150f; nextGift = Time.time + 40f;
            if (inviteAccepted && !home && WalkToDoor()) { inviteAccepted = false; }
            else Set(S.Kiss);
            if (!home) Clues.Add(InviteClue, "Kutsu kahville", "Sini kutsui sinut kahville. Hän asuu pelihallin yläkerrassa: ovi on hallin oikeassa reunassa.");
        }
        else
        {
            chatCooldown = 25f;
            Set(S.Idle); wait = 3f;
            if (line != null) Say(line, 2f);
        }
        phase = Ph.Say;
    }

    // ---------- piirto ----------
    void OnGUI()
    {
        if ((StarShooter.Active || KarateGame.Active || FlipperGame.Active) || body == null || (area != null && Area.Current != area)) return;
        if (box == null)
        {
            int fs = Mathf.RoundToInt(Screen.height * 0.032f);
            box = new GUIStyle(GUI.skin.box) { fontSize = fs, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            small = new GUIStyle(box) { fontSize = Mathf.RoundToInt(Screen.height * 0.026f) };
        }
        float W = Screen.width, H = Screen.height;
        if (state == S.Chat) { if (treeMode) DrawTree(W, H); else if (persona == Persona.Dina || coffeePages) DrawDinaChat(W, H); else DrawChat(W, H); return; }
        if (bubble != null && Time.time < bubbleUntil)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * (height * sizeMul + 0.1f));
                if (sp.z > 0f)
                {
                    float w = H * 0.34f, h = H * 0.07f;
                    GUI.Box(new Rect(sp.x - w * 0.5f, H - sp.y - h, w, h), bubble, small);
                }
            }
        }
        if (near)
        {
            float w = H * 0.8f, h = H * 0.065f;
            string txt = persona == Persona.Dina ? "[E]  " + Loc.T("Juttele Dinan kanssa") : (cheered && Time.time < cheeredUntil) ? "[E]  " + Loc.T("Moikkaa Siniä") : "[E]  " + Loc.T("Juttele Sinin kanssa");
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), txt, box);
        }
    }

    void DrawChat(float W, float H)
    {
        float w = H * 1.15f, x = (W - w) * 0.5f, lh = H * 0.075f;
        float y = H * 0.56f;
        string line = "";
        if (phase == Ph.Say || phase == Ph.Choose) line = Asks[Mathf.Min(stage, Asks.Length - 1)];
        else if (phase == Ph.React) line = reaction;
        else line = mood >= 70f ? "Kiitos. Tarvitsin tuota todella paljon." : "Hmm. En nyt oikein jaksa. Mene vaan.";
        GUI.Box(new Rect(x, y, w, lh * 1.25f), npcName + ": " + Loc.T(line), box);
        y += lh * 1.35f;
        if (phase == Ph.Choose)
        {
            for (int k = 0; k < 3; k++)
            {
                var o = Rounds[stage][order[k]];
                var old = GUI.backgroundColor;
                GUI.backgroundColor = k == sel ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
                GUI.Box(new Rect(x, y, w, lh), (k == sel ? "▶  " : "    ") + Loc.T(o.text), small);
                GUI.backgroundColor = old;
                y += lh * 1.05f;
            }
            GUI.Label(new Rect(x, y, w, lh * 0.8f), Loc.T("Ylös/alas valitse, E vastaa, K poistuu"), small);
        }
        // mielialamittari
        float bw = w * 0.5f, bh = H * 0.018f, bx = (W - bw) * 0.5f, by = H * 0.52f;
        GUI.Box(new Rect(bx - 2, by - 2, bw + 4, bh + 4), GUIContent.none);
        var oc = GUI.color; GUI.color = Color.Lerp(new Color(0.8f, 0.3f, 0.3f), new Color(0.4f, 0.9f, 0.5f), mood / 100f);
        GUI.DrawTexture(new Rect(bx, by, bw * mood / 100f, bh), Texture2D.whiteTexture);
        GUI.color = oc;
        GUI.Label(new Rect(bx, by - H * 0.035f, bw, H * 0.035f), Loc.T("Sinin mieliala"), small);
    }
}
