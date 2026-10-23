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

    const float PadPx = 24f, CharPx = 360f;
    enum S { Idle, Walk, ToCounter, AtCounter, FromCounter, Dance, TalkNpc, Listen, Chat, Kiss, Hide }
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
    void ReleaseHero() { if (chatPc != null) chatPc.enabled = true; chatPc = null; }

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

            case S.Hide:
                if (!MoveToGoal(p, dt, speed * 1.7f) ) { if (Has(walk)) body.sprite = Loop(walk, t, walkFrameTime * 0.7f); }
                else if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
                break;
        }
        UpdatePrompt(p);
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

    void EnterHide()
    {
        ReleaseHero(); FreeCounter(); EndTalkWithFriend(); listenTo = null;
        Set(S.Hide);
        goal = new Vector2(laneMinX + 0.8f, Limit(laneMinX) - 0.6f);
        Say("Apua!", 2f);
    }

    void Say(string s, float len) { bubble = Loc.T(s); bubbleUntil = Time.time + len; }

    // ---------- juttelu heron kanssa ----------
    void UpdatePrompt(Vector3 p)
    {
        near = false;
        if (state == S.Chat || state == S.Hide || state == S.Kiss || state == S.ToCounter || state == S.FromCounter) return;
        if ((StarShooter.Active || KarateGame.Active) || PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen) return;
        var q = Players.Nearest(p);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 hp = q.transform.position;
        if (Mathf.Abs(hp.x - p.x) > talkHalfWidth || Mathf.Abs(hp.y - p.y) > talkDepth) return;
        if (chatCooldown > 0f) return;
        near = true;
        if (!CoopInput.Pick(q.playerIndex)) return;
        if (persona == Persona.Sini && cheered && Time.time < cheeredUntil)
        {
            // hyvällä tuulella: vielä yksi lentosuukko tapaamisen kunniaksi (ei useammin kuin 40 s välein)
            if (Time.time >= nextGift) { flipped = hp.x < p.x; nextGift = Time.time + 40f; Say("Sinä taas! Tässä sulle.", 1.6f); Set(S.Kiss); }
            else Say("Hei! Mulla menee tosi hyvin.", 1.8f);
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
                if (BestScore() > dinaRecord && !Claimed)
                {
                    var pc = chatPc;
                    PlayerPrefs.SetInt(ClaimKey, 1); PlayerPrefs.Save();
                    if (rewardMoney > 0) pc.AddMoney(rewardMoney);
                    RecordBeaten?.Invoke(pc);
                }
                EndChat("Nähdään!", false);
            }
        }
        if (Mathf.Abs(chatPc.transform.position.x - p.x) > 6f) EndChat(null, false);
    }

    void DrawDinaChat(float W, float H)
    {
        if (pages == null || page >= pages.Length) return;
        float w = H * 1.15f, x = (W - w) * 0.5f, lh = H * 0.075f, y = H * 0.66f;
        GUI.Box(new Rect(x, y, w, lh * 1.7f), npcName + ": " + Loc.T(pages[page]), box);
        GUI.Label(new Rect(x, y + lh * 1.75f, w, lh * 0.8f), Loc.T("E jatka, K poistuu"), small);
    }

    void BeginSay() { phase = Ph.Say; phaseT = 0f; }

    void UpdateChat(Vector3 p)
    {
        if (persona == Persona.Dina) { UpdateChatDina(p); return; }
        if (chatPc == null) { Set(S.Idle); return; }
        int i = chatPc.playerIndex;
        bool speaking = phase == Ph.Say || phase == Ph.React || phase == Ph.Verdict;
        if (speaking && Has(talk)) body.sprite = Loop(talk, t, talkFrameTime);
        else if (Has(idle)) body.sprite = PingPong(idle, t, idleFrameTime);
        switch (phase)
        {
            case Ph.Say:
                if (phaseT > 2.4f)
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
                    reaction = o.reply; phase = Ph.React; phaseT = 0f;
                }
                if (Mathf.Abs(chatPc.transform.position.x - p.x) > 6f) EndChat("Hei, minne sinä menet?", false);
                break;
            }
            case Ph.React:
                if (phaseT > 2.1f)
                {
                    stage++;
                    if (stage >= Rounds.Length) { phase = Ph.Verdict; phaseT = 0f; }
                    else BeginSay();
                }
                break;
            case Ph.Verdict:
                if (phaseT > 2.4f)
                {
                    if (mood >= 70f)
                    {
                        EndChat(null, true);
                    }
                    else EndChat(null, false);
                }
                break;
        }
    }

    void EndChat(string line, bool success)
    {
        ReleaseHero();
        if (success)
        {
            cheered = true; cheeredUntil = Time.time + 150f; nextGift = Time.time + 40f;
            Set(S.Kiss);
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
        if ((StarShooter.Active || KarateGame.Active) || body == null || (area != null && Area.Current != area)) return;
        if (box == null)
        {
            int fs = Mathf.RoundToInt(Screen.height * 0.032f);
            box = new GUIStyle(GUI.skin.box) { fontSize = fs, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            small = new GUIStyle(box) { fontSize = Mathf.RoundToInt(Screen.height * 0.026f) };
        }
        float W = Screen.width, H = Screen.height;
        if (state == S.Chat) { if (persona == Persona.Dina) DrawDinaChat(W, H); else DrawChat(W, H); return; }
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
