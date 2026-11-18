using UnityEngine;

/// <summary>
/// Violetti kasinolla: päätehtävän (ei sivutehtävä, ei ohitettavissa) antaja. Kasinon poikki kulkeva hero pysähtyy triggerX-rajalla (kumpaan suuntaan tahansa), Violetti ilmestyy hänen eteensä
/// ja pyytää apua häiden estämiseen; ainoa vastaus on "Hoidan homman." — sen jälkeen tehtävä ("Estä häät") kirjautuu vihjeisiin (Clues). Sen jälkeen Violetti jää paikalleen ja E toistaa muistutuksen.
/// Ei taistelija eikä lyötävissä.
/// </summary>
public class VioletQuest : MonoBehaviour
{
    public const string QuestId = "tehtava_haat";
    public SpriteRenderer body, shadow;
    public Sprite[] idleSprites, talkSprites;
    public float idleFrameTime = 0.167f, talkFrameTime = 0.125f;
    public Area area;
    public float rangeX = 3.4f, rangeY = 1.8f;
    [Tooltip("Pakotettu kohtaaminen: tämän x-rajan yli ei pääse ennen tehtävän hyväksymistä.")]
    public float triggerX;
    [Tooltip("Violetin etäisyys herosta ilmestyessään (heron kulkusuunnassa).")]
    public float appearDistance = 2.4f;

    public const string Ask = "Tarvitsen apua! Don aikoo naittaa siskoni saadakseen isäni omaisuuden! Auta meitä! He ovat jo kappelissa, estä heidät, pyydän sinua!";
    public const string Answer = "Hoidan homman.";
    public const string Reminder = "Kappeli on kadulla, kasinon oikealla puolella. Pidä kiirettä, ole kiltti!";

    [Tooltip("Näkyvissä heti (yleisössä), ei pakotettua kohtaamista rajalla.")]
    public bool startVisible;
    [Tooltip("Ei puhu ennen kuin lukko avataan (BeginTalk avaa).")]
    public bool locked;
    /// Aloittaa puheen (showroom: taikurin voiton jälkeen): Violetti kertoo häistä.
    public void BeginTalk(PlayerController p)
    {
        locked = false; appeared = true; SetVisible(true); appearT = 9f;
        if (p != null && !talking) Open(p);
    }

    PlayerController pc, talkPc;
    bool near, talking, appeared;
    int lastSide;
    float t, openedAt, closedAt = -9f, noticeUntil, appearT = 9f;
    GUIStyle box, nameStyle;
    Texture2D px;

    void Awake()
    {
        PlayerController.SortByFrameNumber(idleSprites); PlayerController.SortByFrameNumber(talkSprites);
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        t = Random.value * 3f;
        if (startVisible) { appeared = true; appearT = 9f; SetVisible(true); }
        else SetVisible(false);   // ilmestyy vasta, kun hero yrittää ohittaa
    }

    void SetVisible(bool v) { if (body != null) body.enabled = v; if (shadow != null) shadow.enabled = v; }

    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }
    void Release() { if (talkPc != null) { talkPc.Scripted = false; talkPc.ScriptedMove = Vector2.zero; } talkPc = null; talking = false; }

    void Update()
    {
        if (body == null || idleSprites == null || idleSprites.Length == 0) return;
        if (area != null && Area.Current != area) { near = false; lastSide = 0; return; }
        t += Time.deltaTime;
        bool done = Clues.Has(QuestId);
        if (!appeared && done) { appeared = true; SetVisible(true); }   // tehtävä jo hyväksytty: Violetti on paikallaan
        if (!appeared && !talking && triggerX != 0f)
        {
            foreach (var q in Players.All)
            {
                if (q == null || q.GameOver) continue;
                int side = q.transform.position.x < triggerX ? -1 : 1;
                if (lastSide == 0) { lastSide = side; continue; }
                if (side != lastSide) { AppearBefore(q, side < 0 ? -1 : 1); break; }
            }
        }
        if (!appeared) return;
        if (appearT < 0.45f) { appearT += Time.deltaTime; if (body != null) body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(appearT / 0.45f)); }
        pc = talkPc != null ? talkPc : Players.Nearest(transform.position);
        near = false;
        if (pc != null && !pc.GameOver)
        {
            Vector3 hp = pc.transform.position, me = transform.position;
            bool inRange = Mathf.Abs(hp.x - me.x) <= rangeX && Mathf.Abs(hp.y - me.y) <= rangeY;
            near = inRange && !locked && !talking && !PokerService.BlocksCombat && !pc.Riding && pc.AirHeight <= 0.05f && Time.unscaledTime - closedAt > 0.4f;
            body.flipX = !(hp.x > me.x);   // kuvat katsovat oikealle
            if (near && CoopInput.Pick(pc.playerIndex)) Open(pc);
            else if (talking && Time.unscaledTime - openedAt > 0.4f && CoopInput.Pick(pc.playerIndex)) Accept();
        }
        var set = talking && talkSprites != null && talkSprites.Length > 0 ? talkSprites : idleSprites;
        float ft = talking ? talkFrameTime : idleFrameTime;
        body.sprite = set[(int)(t / ft) % set.Length];
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        body.sortingOrder = order; if (shadow != null) shadow.sortingOrder = order - 1;
        if (talking && talkPc != null) { talkPc.Scripted = true; talkPc.ScriptedMove = Vector2.zero; }
    }

    /// Hero ylitti rajan (kulkusuunta dir): pysäytetään rajalle ja Violetti ilmestyy hänen eteensä; puhe alkaa heti.
    void AppearBefore(PlayerController p, int dir)
    {
        Vector3 hp = p.transform.position;
        hp.x = triggerX - dir * 0.05f; p.transform.position = hp;
        var me = new Vector3(triggerX + dir * appearDistance, hp.y, 0f);
        if (area != null) me.y = Mathf.Clamp(me.y, area.MinDepthAtX(me.x, area.minDepthY) + 0.05f, area.MaxDepthAtX(me.x, area.maxDepthY) - 0.05f);
        transform.position = me;
        appeared = true; SetVisible(true); appearT = 0f; if (body != null) body.color = new Color(1f, 1f, 1f, 0f);
        Open(p);
    }

    void Open(PlayerController p)
    {
        if (Clues.Has(QuestId)) { noticeUntil = Time.unscaledTime + 4f; closedAt = Time.unscaledTime; return; }   // tehtävä jo annettu: pelkkä muistutus
        talking = true; talkPc = p; openedAt = Time.unscaledTime; t = 0f;
        p.Scripted = true; p.ScriptedMove = Vector2.zero;
    }

    void Accept()
    {
        Clues.Add(QuestId, "Estä häät",
            "Violetti pyysi apua: Don aikoo naittaa hänen siskonsa saadakseen Violetin isän omaisuuden. Häät ovat kappelissa kadulla, kasinon oikealla puolella. Estä ne!");
        HitFx.PlayPickup(false);
        Release(); closedAt = Time.unscaledTime;
    }

    void OnGUI()
    {
        float H = Screen.height, W = Screen.width;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold }; box.normal.textColor = Color.white;
            nameStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold }; nameStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
        }
        box.fontSize = Mathf.RoundToInt(H * 0.032f); nameStyle.fontSize = Mathf.RoundToInt(H * 0.03f);
        if (talking)
        {
            float w = Mathf.Min(W * 0.9f, H * 1.25f), x = (W - w) * 0.5f;
            string ask = Loc.T(Ask);
            float hAsk = box.CalcHeight(new GUIContent(ask), w - H * 0.04f) + H * 0.05f;
            float y = H * 0.56f;
            GUI.color = new Color(0f, 0f, 0f, 0.82f); GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, hAsk + H * 0.12f + 12), px); GUI.color = Color.white;
            GUI.Label(new Rect(x + H * 0.02f, y - H * 0.005f, w, H * 0.045f), Loc.T("VIOLETTI"), nameStyle);
            GUI.Label(new Rect(x, y + H * 0.04f, w, hAsk), ask, box);
            // ainoa vastaus
            GUI.color = new Color(1f, 0.85f, 0.25f, 1f);
            GUI.Box(new Rect(x + w * 0.1f, y + H * 0.04f + hAsk + H * 0.01f, w * 0.8f, H * 0.06f), "[E]  " + Loc.T(Answer), box);
            GUI.color = Color.white;
            return;
        }
        if (Time.unscaledTime < noticeUntil)
        {
            float w = Mathf.Min(W * 0.9f, H * 1.1f);
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.74f, w, H * 0.085f), Loc.T("Violetti") + ": " + Loc.T(Reminder), box);
        }
        else if (near)
        {
            float w = H * 0.6f;
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, H * 0.065f), "[E]  " + Loc.T("Puhu Violetille"), box);
        }
    }
}
