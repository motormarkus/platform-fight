using UnityEngine;

/// <summary>
/// Pablo (Salsafestin cocktail-kojun myyjä) tanssii Rubyn kanssa. Ruby painaa E:tä Pablon luona: pelaajan hahmo piiloutuu ja kuvasarja
/// "tanssi" (lähestyminen ja tiivis tanssi, 10 s) alkaa. Iskunappi tahdissa: tarpeeksi osumia vie suudelmaan (sarja "suudelma", jonka loppu jää näkyviin).
/// Vain Ruby tanssii (Rocco tanssii Helvin kanssa).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RubyPabloDance : MonoBehaviour
{
    public Sprite[] dance, kiss;
    public float frameTime = 4f / 30f;
    public float useHalfWidth = 2.4f, useDepth = 1.6f;
    [Tooltip("Suudelman alkukuva (indeksi suudelma-sarjassa): halaus, josta suudelma alkaa.")]
    public int kissStart = 13;
    public int needHits = 4;
    public string npcName = "Pablo";

    enum Mode { Idle, Dance, Kiss, Result }
    Mode mode = Mode.Idle;
    SpriteRenderer sr;
    SpriteLoop loop;
    PlayerController pc;
    Vector3 savedPos;
    bool near;
    float t, endT, resultAt;
    float hits, stray; int[] state;
    string popup = ""; float popupAt = -9f; Color popupColor = Color.white;
    GUIStyle box, big;

    // tahdit tanssin aikana (s)
    static readonly float[] Beats = { 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f, 9.5f };
    const float Window = 0.28f, Perfect = 0.12f;

    void Awake() { sr = GetComponent<SpriteRenderer>(); loop = GetComponent<SpriteLoop>(); }

    static bool Cancel() => UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame);

    void Update()
    {
        if (mode == Mode.Idle) { UpdateIdle(); return; }
        if (pc == null) { Leave(); return; }
        if (mode == Mode.Dance) { UpdateDance(); return; }
        if (mode == Mode.Kiss) { UpdateKiss(); return; }
        if (Time.unscaledTime - resultAt < 0.4f) return;
        if (CoopInput.Pick(pc.playerIndex) && hits < needHits) StartDance();
        else if (CoopInput.Pick(pc.playerIndex) || Cancel()) Leave();
    }

    void UpdateIdle()
    {
        near = false;
        if (PokerService.BlocksCombat || GameMenu.IsOpen || dance == null || dance.Length == 0) return;
        var q = Players.Nearest(transform.position);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree || q.AppliedCharacter == 0) return;   // vain Ruby
        Vector3 p = q.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) > useHalfWidth || Mathf.Abs(p.y - me.y) > useDepth) return;
        near = true;
        if (!CoopInput.Pick(q.playerIndex)) return;
        pc = q;
        StartDance();
    }

    void StartDance()
    {
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        if (mode == Mode.Idle) savedPos = pc.transform.position;
        if (pc.BodyRenderer != null) pc.BodyRenderer.enabled = false;
        if (pc.shadow != null) pc.shadow.enabled = false;
        if (loop != null) loop.enabled = false;
        mode = Mode.Dance; t = 0f; hits = 0f; stray = 0f; popup = "";
        state = new int[Beats.Length];
        sr.sprite = dance[0];
    }

    void UpdateDance()
    {
        t += Time.deltaTime;
        int idx = Mathf.Min(Mathf.FloorToInt(t / frameTime), dance.Length - 1);
        sr.sprite = dance[idx];
        for (int i = 0; i < Beats.Length; i++)
            if (state[i] == 0 && t - Beats[i] > Window) { state[i] = 2; Pop("OHI", Color.gray); }
        if (t > 2.5f && CoopInput.Punch(pc.playerIndex)) Press(t);
        if (Cancel()) { Leave(); return; }
        if (t >= dance.Length * frameTime + 0.3f)
        {
            if (hits >= needHits - 0.5f * stray && kiss != null && kiss.Length > kissStart) { mode = Mode.Kiss; endT = 0f; }
            else { mode = Mode.Result; resultAt = Time.unscaledTime; }
        }
    }

    void UpdateKiss()
    {
        endT += Time.deltaTime;
        int n = kiss.Length - kissStart;
        sr.sprite = kiss[kissStart + Mathf.Min(Mathf.FloorToInt(endT / frameTime), n - 1)];
        if (endT > n * frameTime + 1.5f) { mode = Mode.Result; resultAt = Time.unscaledTime; hits = Mathf.Max(hits, needHits); }
    }

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

    void Leave()
    {
        if (pc != null)
        {
            if (pc.BodyRenderer != null) pc.BodyRenderer.enabled = true;
            if (pc.shadow != null) pc.shadow.enabled = true;
            pc.transform.position = savedPos;
        }
        foreach (var p in Players.All) if (p != null) p.enabled = true;
        if (loop != null) { loop.enabled = true; }
        mode = Mode.Idle; pc = null;
    }

    void OnDisable() { if (mode != Mode.Idle) Leave(); }

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
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), "[E]  " + Loc.T("Tanssi Pablon kanssa"), box);
            return;
        }
        if (mode == Mode.Kiss) return;
        if (mode == Mode.Result)
        {
            float w = Mathf.Min(W * 0.8f, H * 1.1f), h = H * 0.3f;
            string txt = hits >= needHits ? Loc.T("Hieno tanssi! Pablo hymyilee: Tämä oli ikimuistoinen ilta.") + "\n[E / Esc]  " + Loc.T("Valmis")
                : Loc.T("Jalat menivät sekaisin. Osumia ") + hits + " / " + Beats.Length + ".\n[E]  " + Loc.T("Yritä uudelleen") + "     [Esc]  " + Loc.T("Poistu");
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.35f, w, h), txt, box);
            return;
        }
        float bw = W * 0.7f, bh = H * 0.1f, bx = (W - bw) * 0.5f, by = H * 0.08f;
        GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
        float tx = bx + bw * 0.18f, speed = bw * 0.82f / 2.6f;
        GUI.color = new Color(1f, 1f, 1f, 0.9f); GUI.DrawTexture(new Rect(tx - 2f, by, 4f, bh), Texture2D.whiteTexture);
        for (int i = 0; i < Beats.Length; i++)
        {
            float x = tx + (Beats[i] - t) * speed;
            if (x < bx - bh || x > bx + bw) continue;
            Color col = new Color(1f, 0.45f, 0.75f);
            if (state[i] == 1) col = new Color(0.4f, 1f, 0.4f); else if (state[i] == 2) col = new Color(0.4f, 0.4f, 0.4f);
            GUI.color = col; GUI.DrawTexture(new Rect(x - bh * 0.35f, by + bh * 0.15f, bh * 0.7f, bh * 0.7f), Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        GUI.Box(new Rect(bx, by + bh + 6f, bw, H * 0.06f), Loc.T("Tanssiaskel") + " [" + (pc != null && pc.playerIndex == 1 ? "Num1" : "J") + "]   —   " + Loc.T("Osumia") + " " + hits + " / " + needHits, box);
        if (Time.unscaledTime - popupAt < 0.5f) { var old = big.normal.textColor; big.normal.textColor = popupColor; GUI.Label(new Rect(0, H * 0.3f, W, H * 0.12f), popup, big); big.normal.textColor = old; }
    }
}
