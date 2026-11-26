using UnityEngine;

/// <summary>
/// Juonikeskustelu NPC:lle (NpcStroller-hahmo, esim. Vegasin baarin Violetti ja Candi). Kun requiredClue on auki ja doneClue puuttuu,
/// hahmon kohdalla tulee [E]-kehote; E avaa sivut (E = seuraava). {hero} korvataan nimellä Rocco / Ruby.
/// Valinnainen halaus (hahmo astuu heron viereen hetkeksi), raha, lisävihje (esim. lisätasku) ja ilmoitus ruudun yläosaan lopuksi.
/// Keskustelun ajan NpcStroller pysähtyy ja hahmo puhuu (talkSprites) heroon päin; hero on lukittu (Scripted).
/// </summary>
public class NpcTalk : MonoBehaviour
{
    public string speaker = "Violetti";
    [TextArea] public string[] pages;
    [Tooltip("Keskustelu aukeaa vasta, kun tämä vihje on saatu. Tyhjä = heti.")]
    public string requiredClue;
    [Tooltip("Kirjataan keskustelun lopuksi; kun se on olemassa, hahmo sanoo vain repeatLine.")]
    public string doneClue, doneTitle, doneText;
    public string prompt = "Puhu";
    [Tooltip("Sanotaan (E) keskustelun jälkeen.")]
    public string repeatLine;
    public int rewardMoney;
    [Tooltip("Lisävihje lopuksi (esim. lisätasku PlayerController.ExtraPocketId).")]
    public string extraClue, extraTitle, extraText;
    public string popupText;
    [Tooltip("Halaus ennen viimeistä sivua.")]
    public bool hug;
    public string bannerTitle, bannerInfo;
    public float rangeX = 2.6f, rangeY = 1.6f;
    public Area area;

    NpcStroller stroller;
    SpriteRenderer body;
    PlayerController pc, talkPc;
    bool near, talking;
    int page;
    float t, openedAt, closedAt = -9f, noticeUntil, shownAt = -99f;
    float hugT = -1f; Vector3 hugFrom, hugTo;
    GUIStyle box, nameStyle, titleStyle, infoStyle;
    Texture2D px;

    void Awake()
    {
        stroller = GetComponent<NpcStroller>();
        body = stroller != null && stroller.body != null ? stroller.body : GetComponentInChildren<SpriteRenderer>();
    }

    bool Available => (string.IsNullOrEmpty(requiredClue) || Clues.Has(requiredClue)) && !string.IsNullOrEmpty(doneClue) && !Clues.Has(doneClue);
    bool Finished => !string.IsNullOrEmpty(doneClue) && Clues.Has(doneClue);
    string HeroName(PlayerController p) => p != null && p.AppliedCharacter == 1 ? "Ruby" : "Rocco";

    void OnDisable() { Release(); }

    void Release()
    {
        if (talkPc != null) { talkPc.Scripted = false; talkPc.ScriptedMove = Vector2.zero; }
        talkPc = null; talking = false; hugT = -1f;
        if (stroller != null) stroller.enabled = true;
    }

    void Update()
    {
        if (area != null && Area.Current != area) { near = false; return; }
        t += Time.deltaTime;
        pc = talkPc != null ? talkPc : Players.Nearest(transform.position);
        near = false;
        if (pc == null || pc.GameOver) return;
        Vector3 hp = pc.transform.position, me = transform.position;
        if (!talking)
        {
            bool inRange = Mathf.Abs(hp.x - me.x) <= rangeX && Mathf.Abs(hp.y - me.y) <= rangeY;
            bool can = Available || (Finished && !string.IsNullOrEmpty(repeatLine));
            near = can && inRange && !PokerService.BlocksCombat && !pc.Riding && pc.AirHeight <= 0.05f && Time.unscaledTime - closedAt > 0.4f;
            if (near && CoopInput.Pick(pc.playerIndex))
            {
                if (Available) Open(pc);
                else { noticeUntil = Time.unscaledTime + 4f; closedAt = Time.unscaledTime; }
            }
            return;
        }
        // keskustelu käynnissä
        talkPc.Scripted = true; talkPc.ScriptedMove = Vector2.zero;
        if (body != null)
        {
            body.flipX = !(hp.x > transform.position.x);   // kuvat katsovat oikealle
            var talk = stroller != null && stroller.talkSprites != null && stroller.talkSprites.Length > 0 ? stroller.talkSprites : stroller != null ? stroller.idleSprites : null;
            var idle = stroller != null ? stroller.idleSprites : null;
            var set = hugT >= 0f ? idle : talk;
            float ft = stroller != null ? (hugT >= 0f ? stroller.idleFrameTime : stroller.talkFrameTime) : 0.125f;
            if (set != null && set.Length > 0) body.sprite = set[(int)(t / Mathf.Max(0.02f, ft)) % set.Length];
            int order = Mathf.RoundToInt(-transform.position.y * 100f);
            body.sortingOrder = order;
            if (stroller != null && stroller.shadow != null) stroller.shadow.sortingOrder = order - 1;
        }
        if (hugT >= 0f)
        {
            // halaus: astuu heron viereen, pysyy hetken, palaa
            hugT += Time.deltaTime;
            float k = hugT < 0.5f ? Mathf.SmoothStep(0f, 1f, hugT / 0.5f) : hugT < 2.1f ? 1f : Mathf.SmoothStep(1f, 0f, (hugT - 2.1f) / 0.5f);
            transform.position = Vector3.Lerp(hugFrom, hugTo, k);
            if (hugT >= 2.6f) { transform.position = hugFrom; hugT = -1f; page++; openedAt = Time.unscaledTime; }
            return;
        }
        if (Time.unscaledTime - openedAt > 0.35f && CoopInput.Pick(talkPc.playerIndex))
        {
            bool last = page >= pages.Length - 1;
            if (hug && page == pages.Length - 2) StartHug();
            else if (last) Finish();
            else { page++; openedAt = Time.unscaledTime; }
        }
    }

    void Open(PlayerController p)
    {
        if (pages == null || pages.Length == 0) { talkPc = p; Finish(); return; }
        talking = true; talkPc = p; page = 0; openedAt = Time.unscaledTime; t = 0f;
        if (stroller != null) stroller.enabled = false;
        p.Scripted = true; p.ScriptedMove = Vector2.zero;
        if (hug && pages.Length == 1) StartHug();
    }

    void StartHug()
    {
        hugT = 0f; hugFrom = transform.position;
        Vector3 hp = talkPc.transform.position;
        float side = hugFrom.x < hp.x ? -1f : 1f;
        hugTo = new Vector3(hp.x + side * 0.55f, hp.y + 0.02f, hugFrom.z);
        GameHUD.Popup("♥", hp + Vector3.up * 3.4f, new Color(1f, 0.45f, 0.6f));
    }

    void Finish()
    {
        var p = talkPc;
        if (!string.IsNullOrEmpty(doneClue)) Clues.Add(doneClue, string.IsNullOrEmpty(doneTitle) ? speaker : doneTitle, Fill(doneText, p), null, false);
        if (!string.IsNullOrEmpty(extraClue)) Clues.Add(extraClue, extraTitle, Fill(extraText, p), null, false);
        if (rewardMoney > 0)
        {
            foreach (var q in Players.All) if (q != null) q.AddMoney(rewardMoney);
            if (p != null) GameHUD.Popup("+" + rewardMoney + " MK", p.transform.position + Vector3.up * 3.6f, new Color(1f, 0.9f, 0.35f));
            HitFx.PlayPickup(true);
        }
        if (!string.IsNullOrEmpty(popupText) && p != null) GameHUD.Popup(Loc.T(popupText), p.transform.position + Vector3.up * 4.2f, new Color(0.6f, 1f, 0.6f));
        if (!string.IsNullOrEmpty(bannerTitle)) shownAt = Time.unscaledTime;
        Release(); closedAt = Time.unscaledTime;
    }

    string Fill(string s, PlayerController p) => string.IsNullOrEmpty(s) ? s : s.Replace("{hero}", HeroName(p));

    void OnGUI()
    {
        float H = Screen.height, W = Screen.width;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold }; box.normal.textColor = Color.white;
            nameStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold }; nameStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            infoStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
        }
        box.fontSize = Mathf.RoundToInt(H * 0.032f); nameStyle.fontSize = Mathf.RoundToInt(H * 0.03f);
        // ilmoitus keskustelun jälkeen (kuten "HÄÄT ESTETTY")
        float age = Time.unscaledTime - shownAt;
        if (age >= 0f && age <= 6f)
        {
            float a = Mathf.Clamp01(Mathf.Min(age * 3f, (6f - age) * 2f));
            titleStyle.fontSize = Mathf.RoundToInt(H * 0.065f); infoStyle.fontSize = Mathf.RoundToInt(H * 0.036f);
            var title = new Rect(0, H * 0.17f, W, H * 0.09f); var info = new Rect(0, H * 0.26f, W, H * 0.06f);
            string tt = Loc.T(bannerTitle).ToUpper(), it = Loc.T(bannerInfo);
            titleStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(title.x + 3, title.y + 3, title.width, title.height), tt, titleStyle);
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.25f, a); GUI.Label(title, tt, titleStyle);
            if (!string.IsNullOrEmpty(it))
            {
                infoStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(info.x + 2, info.y + 2, info.width, info.height), it, infoStyle);
                infoStyle.normal.textColor = new Color(1f, 1f, 1f, a); GUI.Label(info, it, infoStyle);
            }
        }
        if (talking && hugT < 0f && pages != null && page < pages.Length)
        {
            float w = Mathf.Min(W * 0.9f, H * 1.25f), x = (W - w) * 0.5f;
            string text = Fill(Loc.T(pages[page]), talkPc);
            float h = box.CalcHeight(new GUIContent(text), w - H * 0.04f) + H * 0.05f;
            float y = H * 0.56f;
            GUI.color = new Color(0f, 0f, 0f, 0.82f); GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, h + H * 0.1f + 12), px); GUI.color = Color.white;
            GUI.Label(new Rect(x + H * 0.02f, y - H * 0.005f, w, H * 0.045f), Loc.T(speaker).ToUpper(), nameStyle);
            GUI.Label(new Rect(x, y + H * 0.04f, w, h), text, box);
            GUI.color = new Color(1f, 0.85f, 0.25f, 1f);
            GUI.Label(new Rect(x, y + H * 0.04f + h, w - H * 0.02f, H * 0.045f), "[E]  " + Loc.T(page >= pages.Length - 1 ? "Selvä" : "Seuraava") + "  ", new GUIStyle(nameStyle) { alignment = TextAnchor.MiddleRight });
            GUI.color = Color.white;
            return;
        }
        if (Time.unscaledTime < noticeUntil && !string.IsNullOrEmpty(repeatLine))
        {
            float w = Mathf.Min(W * 0.9f, H * 1.1f);
            string s = Loc.T(speaker) + ": " + Fill(Loc.T(repeatLine), pc);
            float h = Mathf.Max(H * 0.085f, box.CalcHeight(new GUIContent(s), w - H * 0.04f) + H * 0.03f);
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.74f, w, h), s, box);
        }
        else if (near)
        {
            string s = "[E]  " + Loc.T(prompt);
            float w = Mathf.Max(H * 0.5f, box.CalcSize(new GUIContent(s)).x + H * 0.06f);
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, H * 0.065f), s, box);
        }
    }
}
