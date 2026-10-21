using UnityEngine;

/// <summary>
/// Candin rytmitreeni salilla: E Candin vieressä aloittaa. Candi tekee lyönti- ja potkujumpan (kaksi kuvasarjaa peräkkäin),
/// pelaaja painaa iskunappia lyönnin kohdalla ja potkunappia potkun kohdalla. Tarpeeksi osumia -> treeniohjelma tapauskansioon
/// (Candin treeniohjelma: staminan palautuminen +10 %). Epäonnistuessa saa yrittää heti uudelleen.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CandiTraining : MonoBehaviour
{
    public const string TrainedId = "candi_treeni";

    [Tooltip("Kuvasarja 1 (lyönnit ja potkut) ja 2 (lyönnit, potku, polvi), 100 kuvaa kumpikin, 10 kuvaa/s.")]
    public Sprite[] sheet1, sheet2;
    public float frameTime = 0.1f;
    [Tooltip("Pelaajan paikka Candin oikealla puolella (yksikköä). Candi lyö oikealle.")]
    public float heroDistance = 2.6f;
    public float useHalfWidth = 2.6f, useDepth = 1.4f;
    [Tooltip("Hyväksyttyjen osumien raja (osumat miinus puoli pisteen sakko ylimääräisistä painalluksista).")]
    public float passScore = 16f;

    const float LeadIn = 3f, Window = 0.25f, Perfect = 0.11f;
    // (aika sekunneissa kuvasarjan alusta, laji): true = potku. Ajat kohdistettu Candin iskun/potkun huippukuvaan.
    static readonly float[] BeatTime = { 0.6f, 1.4f, 2.1f, 2.6f, 3.1f, 3.7f, 4.6f, 5.3f, 6.1f, 7.0f, 7.6f, 8.2f, 8.7f, 9.1f, 10.5f, 11.5f, 12.4f, 13.6f, 14.8f, 16.0f, 18.0f };
    static readonly bool[] BeatKick = { false, false, false, true, false, true, false, false, true, true, true, true, false, false, false, false, false, false, false, true, true };

    enum Mode { Idle, Training, Result }
    Mode mode = Mode.Idle;
    SpriteRenderer sr;
    Dancer dancer;
    PlayerController pc;
    float t, resultAt;
    float hits, stray;
    int[] state;          // 0 odottaa, 1 osuma, 2 ohi
    float[] feedbackAt, feedbackQ;
    bool passed, near;
    string popup = ""; float popupAt = -9f;
    GUIStyle box, big;

    float TrainLen => 20f;

    void Awake() { sr = GetComponent<SpriteRenderer>(); dancer = GetComponent<Dancer>(); }

    void Update()
    {
        if (mode == Mode.Idle) { UpdateIdle(); return; }
        if (pc == null) { End(); return; }
        if (mode == Mode.Training) { UpdateTraining(); return; }
        // tulos: E uudestaan, Esc / Q pois
        if (Time.unscaledTime - resultAt < 0.4f) return;
        if (CoopInput.Pick(pc.playerIndex) && !passed) StartTraining(pc);
        else if (CoopInput.Pick(pc.playerIndex) || Cancel()) End();
    }

    static bool Cancel() => UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame);

    void UpdateIdle()
    {
        near = false;
        if (PokerService.BlocksCombat || GameMenu.IsOpen || sheet1 == null || sheet1.Length < 100 || sheet2 == null || sheet2.Length < 100) return;
        var q = Players.Nearest(transform.position);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 p = q.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) > useHalfWidth || Mathf.Abs(p.y - me.y) > useDepth) return;
        near = true;
        if (CoopInput.Pick(q.playerIndex)) StartTraining(q);
    }

    void StartTraining(PlayerController who)
    {
        pc = who;
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        var me = transform.position;
        pc.transform.position = new Vector3(me.x + heroDistance, me.y, pc.transform.position.z);
        pc.FaceTowards(me.x);
        if (dancer != null) dancer.enabled = false;
        mode = Mode.Training; t = 0f; hits = 0f; stray = 0f; passed = false;
        state = new int[BeatTime.Length]; feedbackAt = new float[BeatTime.Length]; feedbackQ = new float[BeatTime.Length];
        popup = ""; sr.sprite = sheet1[0];
    }

    void UpdateTraining()
    {
        t += Time.deltaTime;
        float now = t - LeadIn;
        // Candin kuva
        float c = Mathf.Max(0f, now);
        int idx = Mathf.FloorToInt(c / frameTime);
        sr.sprite = idx < 100 ? sheet1[idx] : (idx < 200 ? sheet2[idx - 100] : sheet2[99]);
        // ohi menneet
        for (int i = 0; i < BeatTime.Length; i++)
            if (state[i] == 0 && now - BeatTime[i] > Window) { state[i] = 2; Pop("OHI", Color.gray); }
        // painallukset
        if (now > -0.5f)
        {
            if (CoopInput.Punch(pc.playerIndex)) Press(false, now);
            if (CoopInput.Kick(pc.playerIndex)) Press(true, now);
        }
        if (Cancel()) { End(); return; }
        if (now > TrainLen + 1f) Finish();
    }

    void Press(bool kick, float now)
    {
        int best = -1; float bd = Window;
        for (int i = 0; i < BeatTime.Length; i++)
        {
            if (state[i] != 0 || BeatKick[i] != kick) continue;
            float d = Mathf.Abs(now - BeatTime[i]);
            if (d <= bd) { bd = d; best = i; }
        }
        if (best >= 0)
        {
            state[best] = 1; hits += 1f;
            if (bd <= Perfect) Pop("TÄYDELLINEN!", new Color(1f, 0.85f, 0.2f)); else Pop("HYVÄ", Color.white);
            HitFx.PlayPickup(false);
        }
        else { stray += 1f; Pop("VÄÄRIN", new Color(1f, 0.4f, 0.4f)); }
    }

    void Pop(string s, Color c) { popup = s; popupAt = Time.unscaledTime; popupColor = c; }
    Color popupColor = Color.white;

    float Score => hits - 0.5f * stray;

    void Finish()
    {
        mode = Mode.Result; resultAt = Time.unscaledTime;
        sr.sprite = sheet2[99];
        passed = Score >= passScore;
        if (passed)
        {
            Clues.Add(TrainedId, "Candin treeniohjelma", "Candi antoi sinulle treeniohjelmansa. Stamina palautuu 10 % nopeammin.", "Kirjeet/candi_treeniohjelma");
        }
    }

    void End()
    {
        if (dancer != null) dancer.enabled = true;
        foreach (var p in Players.All) if (p != null) p.enabled = true;
        mode = Mode.Idle; pc = null;
    }

    void OnDisable() { if (mode != Mode.Idle) End(); }

    static string PunchKey(int pi) => pi == 0 ? "J" : "Num1";
    static string KickKey(int pi) => pi == 0 ? "K" : "Num2";

    void OnGUI()
    {
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            big = new GUIStyle(box) { fontSize = Mathf.RoundToInt(Screen.height * 0.06f) };
        }
        float H = Screen.height, W = Screen.width;
        if (mode == Mode.Idle)
        {
            if (!near) return;
            float w = H * 0.6f, h = H * 0.065f;
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), "[E]  " + Loc.T("Treenaa Candin kanssa"), box);
            return;
        }
        if (mode == Mode.Result)
        {
            float w = Mathf.Min(W * 0.8f, H * 1.1f), h = H * 0.3f;
            string txt = passed
                ? Loc.T("Hyvin tehty! Osumia ") + hits + " / " + BeatTime.Length + Loc.T(". Candi antaa sinulle treeniohjelmansa: stamina palautuu 10 % nopeammin.") + "\n[E / Esc]  " + Loc.T("Valmis")
                : Loc.T("Ei vielä. Osumia ") + hits + " / " + BeatTime.Length + Loc.T(", ylimääräisiä painalluksia ") + stray + ".\n[E]  " + Loc.T("Yritä uudelleen") + "     [Esc]  " + Loc.T("Poistu");
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.35f, w, h), txt, box);
            return;
        }
        float now = t - LeadIn;
        // nuottipalkki ruudun yläosassa: nuotit liikkuvat oikealta vasemmalle kohti osumakohtaa
        float bw = W * 0.7f, bh = H * 0.1f, bx = (W - bw) * 0.5f, by = H * 0.08f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
        float tx = bx + bw * 0.18f, speed = bw * 0.82f / 2.6f;
        GUI.color = new Color(1f, 1f, 1f, 0.9f); GUI.DrawTexture(new Rect(tx - 2f, by, 4f, bh), Texture2D.whiteTexture);
        for (int i = 0; i < BeatTime.Length; i++)
        {
            float x = tx + (BeatTime[i] - now) * speed;
            if (x < bx - bh || x > bx + bw) continue;
            Color col = BeatKick[i] ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.6f, 0.15f);
            if (state[i] == 1) col = new Color(0.4f, 1f, 0.4f); else if (state[i] == 2) col = new Color(0.4f, 0.4f, 0.4f);
            GUI.color = col; GUI.DrawTexture(new Rect(x - bh * 0.35f, by + bh * 0.15f, bh * 0.7f, bh * 0.7f), Texture2D.whiteTexture);
            GUI.color = Color.black; GUI.Label(new Rect(x - bh * 0.35f, by + bh * 0.15f, bh * 0.7f, bh * 0.7f), BeatKick[i] ? "P" : "I", box);
        }
        GUI.color = Color.white;
        int pi = pc != null ? pc.playerIndex : 0;
        GUI.Box(new Rect(bx, by + bh + 6f, bw, H * 0.06f), Loc.T("Isku") + " [" + PunchKey(pi) + "]   |   " + Loc.T("Potku") + " [" + KickKey(pi) + "]   —   " + Loc.T("Osumia") + " " + hits + " / " + BeatTime.Length, box);
        if (now < 0f) GUI.Label(new Rect(0, H * 0.38f, W, H * 0.15f), Mathf.CeilToInt(-now).ToString(), big);
        if (Time.unscaledTime - popupAt < 0.5f) { var old = big.normal.textColor; big.normal.textColor = popupColor; GUI.Label(new Rect(0, H * 0.3f, W, H * 0.12f), popup, big); big.normal.textColor = old; }
    }
}
