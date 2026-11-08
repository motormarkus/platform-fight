using UnityEngine;

/// <summary>
/// Dinan äiti (Salsafestin ruokakojun Mama) pyykkituvassa. Kun jengi on kaadettu, hän ilmestyy (ei kävelykuvia: pysyy paikallaan idle-silmukalla),
/// kiittää (E) ja antaa onnenamuletin (isomman rahanipun pudotus +20 %). Amuletti ja valmis tehtävä tallentuvat (PlayerPrefs).
/// </summary>
public class LaundryMama : MonoBehaviour
{
    public Area area;
    public Enemy[] gang;
    public SpriteRenderer body;
    public Sprite[] frames;
    public float fps = 6f;
    public float talkHalfWidth = 2.2f, talkDepth = 2f;
    const string DoneKey = "laundry_done";

    bool appeared, chatting, prompt;
    float t, appearTimer, phaseT;
    int page; int step;   // step 0 kiitos + amuletti, 1 jo tehty
    string[] pages;
    PlayerController pc;
    GUIStyle box, small;

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (PlayerPrefs.GetInt(DoneKey, 0) == 1) appeared = true;
        if (body != null) body.enabled = appeared;
    }

    void Start()
    {
        // tehtävä jo tehty aiemmin: jengi ei enää palaa
        if (PlayerPrefs.GetInt(DoneKey, 0) == 1 && gang != null)
            foreach (var g in gang) if (g != null) g.gameObject.SetActive(false);
    }

    void OnDisable() { if (pc != null) pc.enabled = true; pc = null; chatting = false; }

    bool GangDown()
    {
        if (gang == null) return true;
        foreach (var g in gang)
        {
            if (g == null || !g.gameObject.activeInHierarchy || g.IsDead) continue;
            if (area != null && !Area.IsHere(g.transform.position, 2f)) continue;   // eksynyt pois huoneesta: ei estä
            return false;
        }
        return true;
    }

    void Update()
    {
        prompt = false;
        if (area == null || Area.Current != area || body == null) return;
        if (!appeared)
        {
            if (!GangDown()) { appearTimer = 0f; return; }
            appearTimer += Time.deltaTime;
            if (appearTimer < 2f) return;
            appeared = true; body.enabled = true;
        }
        if (frames != null && frames.Length > 0) { t += Time.deltaTime * fps; body.sprite = frames[(int)t % frames.Length]; }
        phaseT += Time.deltaTime;
        if (chatting) { UpdateChat(); return; }
        if (GameMenu.IsOpen || ShopCounter.IsOpen) return;
        var q = Players.Nearest(transform.position);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 d = q.transform.position - transform.position;
        if (Mathf.Abs(d.x) > talkHalfWidth || Mathf.Abs(d.y) > talkDepth) return;
        prompt = true;
        body.flipX = d.x > 0f ? false : true;   // kuva katsoo pelaajaa päin (kuvien suunta säädettävissä: flipX)
        if (CoopInput.Pick(q.playerIndex)) Begin(q);
    }

    void Begin(PlayerController q)
    {
        pc = q; pc.enabled = false; chatting = true; page = 0; phaseT = 0f;
        if (PlayerPrefs.GetInt(DoneKey, 0) == 1)
            pages = new[] { "Pyykit pyörivät taas rauhassa. Kiitos vielä, kultaseni!" };
        else
            pages = new[] {
                "Voi kiitos, kultaseni! Sinä ajoit ne roistot pois.",
                "Dina kertoi, että tuntee yhden kunnollisen pojan. Hän oli oikeassa.",
                "Tässä, ota tämä onnenamuletti. Se on kulkenut suvussamme sukupolvia.",
                "Se tuo onnea: viholliset pudottavat isoja rahanippuja useammin. (Onnenamuletti saatu!)",
            };
    }

    void UpdateChat()
    {
        if (pc == null) { chatting = false; return; }
        int i = pc.playerIndex;
        if (CoopInput.Kick(i)) { End(false); return; }
        if (phaseT > 0.3f && (CoopInput.Pick(i) || CoopInput.Punch(i)))
        {
            page++; phaseT = 0f;
            if (page >= pages.Length) End(true);
        }
    }

    void End(bool finished)
    {
        if (finished && PlayerPrefs.GetInt(DoneKey, 0) != 1)
        {
            LaundryQuest.GiveAmulet();
            PlayerPrefs.SetInt(DoneKey, 1); PlayerPrefs.Save();
        }
        if (pc != null) pc.enabled = true;
        pc = null; chatting = false;
    }

    void OnGUI()
    {
        if (body == null || area == null || Area.Current != area || !appeared) return;
        if (box == null)
        {
            int fs = Mathf.RoundToInt(Screen.height * 0.032f);
            box = new GUIStyle(GUI.skin.box) { fontSize = fs, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            small = new GUIStyle(box) { fontSize = Mathf.RoundToInt(Screen.height * 0.026f) };
        }
        float W = Screen.width, H = Screen.height;
        if (chatting && pages != null && page < pages.Length)
        {
            float w = H * 1.15f, x = (W - w) * 0.5f, lh = H * 0.075f, y = H * 0.66f;
            GUI.Box(new Rect(x, y, w, lh * 1.7f), Loc.T("Dinan äiti") + ": " + Loc.T(pages[page]), box);
            GUI.Label(new Rect(x, y + lh * 1.75f, w, lh * 0.8f), Loc.T("E jatka, K poistuu"), small);
        }
        else if (prompt)
        {
            float w = H * 0.8f, h = H * 0.065f;
            GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), "[E]  " + Loc.T("Puhu Dinan äidille"), box);
        }
    }
}
