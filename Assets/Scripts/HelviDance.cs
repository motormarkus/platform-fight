using UnityEngine;

/// <summary>
/// Helvi tanssii Salsafestin lavan edessä yksin (viisi tanssia vuorotellen). Jos Rocco on ostanut hänelle kaulakorun (panttilainaamosta),
/// E antaa korun ja käynnistää paritanssin: Rocco ja Helvi tanssivat (kuvasarjat 1 ja 2), pelaaja painaa iskunappia tahdissa.
/// Hyvin: suudelma (sarja 3). Täydellisesti: Rocco nostaa Helvin syliin (sarja 4, lopuksi takaperin = lasku). Palkintona vihje Horhesta.
/// Vain Rocco tanssii (Ruby ei).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HelviDance : MonoBehaviour
{
    public const string NecklaceClue = "esine_Kaulakoru";
    public const string HintId = "helvi_horhe";

    [Tooltip("Yksin-tanssit (5 kpl, 80-100 kuvaa, 320 x 400) ja paritanssit (4 kpl, 640 x 400), 10 kuvaa/s.")]
    public Sprite[] solo1, solo2, solo3, solo4, solo5;
    public Sprite[] pair1, pair2, pair3, pair4;
    public float frameTime = 0.1f;
    public float useHalfWidth = 2.4f, useDepth = 1.4f;
    [Tooltip("Osumia vaaditaan: suudelma / nosto (15 tahtia).")]
    public int needKiss = 7, needLift = 12;

    enum Mode { Solo, Talk, Dance, Result }
    Mode mode = Mode.Solo;
    SpriteRenderer sr;
    PlayerController pc;
    Vector3 savedPos;
    bool given, near, nearHasItem;
    float t, talkUntil, resultAt;
    string line = "";
    int soloIdx, ending; // ending: 0 ei mitään, 1 suudelma, 2 nosto
    float hits, stray; int[] state;
    GUIStyle box, big;
    string popup = ""; float popupAt = -9f; Color popupColor = Color.white;

    // tahdit: sarjan 1 loppu (5-9 s) ja sarja 2 (10,5-19,5 s)
    static readonly float[] Beats = { 5f, 6f, 7f, 8f, 9f, 10.5f, 11.5f, 12.5f, 13.5f, 14.5f, 15.5f, 16.5f, 17.5f, 18.5f, 19.5f };
    const float LeadIn = 2.5f, Window = 0.28f, Perfect = 0.12f, DanceLen = 20f;

    Vector3 baseScale = Vector3.one;
    void Awake() { sr = GetComponent<SpriteRenderer>(); baseScale = transform.localScale; }

    Sprite[] Solo(int i) { switch (i % 5) { case 0: return solo1; case 1: return solo2; case 2: return solo3; case 3: return solo4; default: return solo5; } }

    void Update()
    {
        if (mode == Mode.Solo) { UpdateSolo(); return; }
        if (mode == Mode.Talk) { if (Time.unscaledTime > talkUntil) { if (given && !EndLocked) StartDance(); else mode = Mode.Solo; } return; }
        if (pc == null) { Leave(); return; }
        if (mode == Mode.Dance) { UpdateDance(); return; }
        if (Time.unscaledTime - resultAt < 0.4f) return;
        if (CoopInput.Pick(pc.playerIndex) && ending < 0) StartDance();
        else if (CoopInput.Pick(pc.playerIndex) || Cancel()) Leave();
    }

    bool EndLocked => false;
    static bool Cancel() => UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame);

    float soloClock;
    void UpdateSolo()
    {
        var s = Solo(soloIdx);
        if (s != null && s.Length > 0)
        {
            soloClock += Time.deltaTime;
            int i = Mathf.FloorToInt(soloClock / frameTime);
            if (i >= s.Length) { soloIdx++; soloClock = 0f; i = 0; s = Solo(soloIdx); }
            if (s != null && s.Length > 0) sr.sprite = s[Mathf.Min(i, s.Length - 1)];
        }
        near = false;
        if (PokerService.BlocksCombat || GameMenu.IsOpen) return;
        var q = Players.Nearest(transform.position);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 p = q.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) > useHalfWidth || Mathf.Abs(p.y - me.y) > useDepth) return;
        near = true; nearHasItem = Clues.Has(NecklaceClue);
        if (!CoopInput.Pick(q.playerIndex)) return;
        pc = q;
        if (q.AppliedCharacter != 0) { Say("Tämä tanssi on varattu toiselle. Kiitos kuitenkin!"); return; }
        if (!nearHasItem && !given) { Say("Tanssisin kyllä mielelläni. Mutta ilman koruja en lähde tanssimaan. Panttilainaamossa taitaa olla jotain sopivaa."); return; }
        if (!given) { given = true; Say("Kaulakoru! Voi että, juuri tällaista olen haaveillut. No sitten tanssitaan!"); return; }
        StartDance();
    }

    void Say(string s) { line = s; mode = Mode.Talk; talkUntil = Time.unscaledTime + 3.2f; }

    void StartDance()
    {
        if (pc == null || pc.AppliedCharacter != 0) { mode = Mode.Solo; return; }
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        savedPos = pc.transform.position;
        if (pc.BodyRenderer != null) pc.BodyRenderer.enabled = false;
        if (pc.shadow != null) pc.shadow.enabled = false;
        mode = Mode.Dance; t = 0f; hits = 0f; stray = 0f; ending = 0; popup = "";
        state = new int[Beats.Length];
        sr.sprite = pair1[0];
        transform.localScale = baseScale;
    }

    // kuva ajanhetkellä: sarja 1 (0-10 s), sarja 2 (10-20 s); lopetus alkaa 20 s kohdalla
    void UpdateDance()
    {
        t += Time.deltaTime;
        float now = t - LeadIn;
        if (now <= DanceLen)
        {
            float c = Mathf.Max(0f, now);
            int idx = Mathf.FloorToInt(c / frameTime);
            sr.sprite = idx < pair1.Length ? pair1[idx] : pair2[Mathf.Min(idx - pair1.Length, pair2.Length - 1)];
            for (int i = 0; i < Beats.Length; i++)
                if (state[i] == 0 && now - Beats[i] > Window) { state[i] = 2; Pop("OHI", Color.gray); }
            if (now > 3f && CoopInput.Punch(pc.playerIndex)) Press(now);
            if (Cancel()) { Leave(); return; }
            return;
        }
        if (ending == 0)
        {
            float score = hits - 0.5f * stray;
            ending = score >= needLift ? 2 : score >= needKiss ? 1 : -1;
            endT = 0f;
            if (ending < 0) { Finish(); return; }
        }
        endT += Time.deltaTime;
        var s = ending == 2 ? pair4 : pair3;
        int n = s.Length;
        float len = n * frameTime;
        if (ending == 1)
        {
            sr.sprite = s[Mathf.Min(Mathf.FloorToInt(endT / frameTime), n - 1)];
            if (endT > len + 1f) Finish();
        }
        else
        {
            // nosto: eteen, pito, takaisin
            const float hold = 1.5f;
            int i;
            if (endT < len) i = Mathf.FloorToInt(endT / frameTime);
            else if (endT < len + hold) i = n - 1;
            else i = n - 1 - Mathf.FloorToInt((endT - len - hold) / frameTime);
            sr.sprite = s[Mathf.Clamp(i, 0, n - 1)];
            if (endT > 2f * len + hold) Finish();
        }
    }
    float endT;

    void Press(float now)
    {
        int best = -1; float bd = Window;
        for (int i = 0; i < Beats.Length; i++)
        {
            if (state[i] != 0) continue;
            float d = Mathf.Abs(now - Beats[i]);
            if (d <= bd) { bd = d; best = i; }
        }
        if (best >= 0) { state[best] = 1; hits += 1f; Pop(bd <= Perfect ? "TÄYDELLINEN!" : "HYVÄ", bd <= Perfect ? new Color(1f, 0.85f, 0.2f) : Color.white); HitFx.PlayPickup(false); }
        else { stray += 1f; Pop("OHI", new Color(1f, 0.4f, 0.4f)); }
    }

    void Pop(string s, Color c) { popup = s; popupAt = Time.unscaledTime; popupColor = c; }

    void Finish()
    {
        mode = Mode.Result; resultAt = Time.unscaledTime;
        sr.sprite = solo1 != null && solo1.Length > 0 ? solo1[0] : sr.sprite;
        if (ending > 0)
        {
            Clues.Add(HintId, "Helvin juorut Horhesta", "Tanssin jälkeen Helvi kuiskasi: Horhe on El Loipparin yläkerrassa, ja hänellä on taskussaan lippu laivaan. Hän ei ole kova tanssimaan, mutta lyö sitäkin kovempaa.");
            if (ending == 2) pc.AddMoney(100);
        }
    }

    void Leave()
    {
        if (pc != null)
        {
            if (pc.BodyRenderer != null) pc.BodyRenderer.enabled = true;
            if (pc.shadow != null) pc.shadow.enabled = true;
            pc.transform.position = savedPos;
        }
        foreach (var p in Players.All) if (p != null) p.enabled = true;
        mode = Mode.Solo; pc = null; soloClock = 0f;
    }

    void OnDisable() { if (mode == Mode.Dance || mode == Mode.Result) Leave(); }

    void OnGUI()
    {
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            big = new GUIStyle(box) { fontSize = Mathf.RoundToInt(Screen.height * 0.06f) };
        }
        float H = Screen.height, W = Screen.width;
        if (mode == Mode.Solo)
        {
            if (!near) return;
            string pr = !nearHasItem && !given ? "Puhu Helville" : !given ? "Anna kaulakoru Helville" : "Tanssi Helvin kanssa";
            float w = H * 0.6f, h = H * 0.065f;
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), "[E]  " + Loc.T(pr), box);
            return;
        }
        if (mode == Mode.Talk)
        {
            float w = Mathf.Min(W * 0.85f, H * 1.2f), h = H * 0.2f;
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.7f, w, h), "Helvi: " + Loc.T(line), box);
            return;
        }
        if (mode == Mode.Result)
        {
            float w = Mathf.Min(W * 0.8f, H * 1.1f), h = H * 0.3f;
            string txt = ending == 2 ? Loc.T("Täydellinen tanssi! Helvi kuiskaa sinulle juoruja Horhesta. Sait myös 100 markkaa.") + "\n[E / Esc]  " + Loc.T("Valmis")
                : ending == 1 ? Loc.T("Hieno tanssi! Helvi kuiskaa sinulle juoruja Horhesta.") + "\n[E / Esc]  " + Loc.T("Valmis")
                : Loc.T("Jalat menivät sekaisin. Osumia ") + hits + " / " + Beats.Length + ".\n[E]  " + Loc.T("Yritä uudelleen") + "     [Esc]  " + Loc.T("Poistu");
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.35f, w, h), txt, box);
            return;
        }
        float now = t - LeadIn;
        if (now > DanceLen) return;
        float bw = W * 0.7f, bh = H * 0.1f, bx = (W - bw) * 0.5f, by = H * 0.08f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
        float tx = bx + bw * 0.18f, speed = bw * 0.82f / 2.6f;
        GUI.color = new Color(1f, 1f, 1f, 0.9f); GUI.DrawTexture(new Rect(tx - 2f, by, 4f, bh), Texture2D.whiteTexture);
        for (int i = 0; i < Beats.Length; i++)
        {
            float x = tx + (Beats[i] - now) * speed;
            if (x < bx - bh || x > bx + bw) continue;
            Color col = new Color(1f, 0.45f, 0.75f);
            if (state[i] == 1) col = new Color(0.4f, 1f, 0.4f); else if (state[i] == 2) col = new Color(0.4f, 0.4f, 0.4f);
            GUI.color = col; GUI.DrawTexture(new Rect(x - bh * 0.35f, by + bh * 0.15f, bh * 0.7f, bh * 0.7f), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        GUI.Box(new Rect(bx, by + bh + 6f, bw, H * 0.06f), Loc.T("Tanssiaskel") + " [" + (pc != null && pc.playerIndex == 1 ? "Num1" : "J") + "]   —   " + Loc.T("Osumia") + " " + hits, box);
        if (now < 0f) GUI.Label(new Rect(0, H * 0.38f, W, H * 0.15f), Mathf.CeilToInt(-now).ToString(), big);
        if (Time.unscaledTime - popupAt < 0.5f) { var old = big.normal.textColor; big.normal.textColor = popupColor; GUI.Label(new Rect(0, H * 0.3f, W, H * 0.12f), popup, big); big.normal.textColor = old; }
    }
}
