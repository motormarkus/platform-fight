using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Piirakkasota dinerissä. Linseyn luona E avaa ikkunan "Aloita piirakkasota?". Sodan aikana kukaan ei taistele: vihut hakevat piirakoita
/// ja heittelevät niitä heroon, hero ja Linsey heittävät vihuihin. 10 piirakkaosumaa pudottaa vihun pelistä pois (häviää näkyvistä);
/// jos hero saa 10 osumaa, sota päättyy ja näkymä palautuu dineriin (vihut takaisin paikoilleen). Kun kaikki vihut ovat poissa, hero voittaa.
/// </summary>
public class PieWar : MonoBehaviour
{
    public static bool Active;
    /// Hero on Linseyn luona ja sota ei ole käynnissä: Linseyn puhe menee ostoskojun edelle.
    public static bool LinseyPrompt;
    /// Piirakan osuma on käynnissä: sodan aikana vain se saa tehdä vahinkoa.
    public static bool PieDamage;
    static PieWar instance;

    public Area area;
    public DinerLinsey linsey;
    public PieCounter counter;
    public int maxHits = 10;

    PlayerController hero;
    int heroHits;
    readonly List<Enemy> foes = new List<Enemy>();
    readonly Dictionary<Enemy, int> hits = new Dictionary<Enemy, int>();
    readonly Dictionary<Enemy, Vector3> home = new Dictionary<Enemy, Vector3>();
    readonly HashSet<Enemy> gone = new HashSet<Enemy>();
    // dinerin vihut: viitteet, kotipaikat ja piilotetut mallit (jos vihu on tuhoutunut, sodan alussa tehdään uusi mallista)
    readonly List<Enemy> roster = new List<Enemy>();
    readonly List<Vector3> rosterHome = new List<Vector3>();
    readonly List<GameObject> rosterTemplate = new List<GameObject>();
    bool captured;
    bool dialogOpen, ending, near;
    float fade, msgUntil;
    string msg = "";
    float oldScale = 1f;
    GUIStyle box, big;
    static Texture2D black;

    void Awake() { instance = this; }
    void OnDestroy() { if (instance == this) instance = null; Active = false; PieDamage = false; }

    // ---------------- osumat ----------------

    /// Onko vihu mukana sodassa ja vielä pelissä (toiset vihut voivat valita sen maaliksi).
    public static bool IsFoe(Enemy e)
    {
        return Active && instance != null && e != null && instance.foes.Contains(e) && !instance.gone.Contains(e);
    }

    public static void OnEnemyHit(Enemy e)
    {
        if (!Active || instance == null || e == null) return;
        e.WarHeal();   // sodassa piirakka ei vie energiaa, vain osumalaskuri
        instance.CountEnemy(e);
    }

    public static void OnHeroHit()
    {
        if (!Active || instance == null) return;
        if (instance.hero != null) instance.hero.WarHeal();
        instance.heroHits++;
        if (instance.heroHits >= instance.maxHits && !instance.ending) instance.StartCoroutine(instance.End(false));
    }

    void CountEnemy(Enemy e)
    {
        if (!hits.ContainsKey(e) || gone.Contains(e)) return;
        hits[e]++;
        if (hits[e] >= maxHits) StartCoroutine(Eliminate(e));
    }

    IEnumerator Eliminate(Enemy e)
    {
        gone.Add(e);
        yield return new WaitForSeconds(1.1f);   // osuma-animaatio ehtii näkyä
        if (e != null) e.gameObject.SetActive(false);
        if (Active && !ending && gone.Count >= foes.Count) StartCoroutine(End(true));
    }

    // ---------------- sodan kulku ----------------

    void Update()
    {
        if (hero == null) hero = FindFirstObjectByType<PlayerController>();
        if (hero == null || area == null) return;
        if (Area.Current != area) { if (Active && !ending) AbortQuietly(); near = false; LinseyPrompt = false; return; }
        if (!captured) CaptureRoster();
        if (ending) return;
        if (dialogOpen)
        {
            if (Yes()) { dialogOpen = false; Time.timeScale = oldScale; if (Active) StartCoroutine(End(false, true)); else StartWar(); }
            else if (No()) { dialogOpen = false; Time.timeScale = oldScale; }
            return;
        }
        near = !Active && linsey != null && !ShopCounter.IsOpen && !hero.Riding && hero.AirHeight <= 0.05f
            && Mathf.Abs(hero.transform.position.x - linsey.transform.position.x) <= 2.4f
            && Mathf.Abs(hero.transform.position.y - linsey.transform.position.y) <= 1.2f;
        LinseyPrompt = near;
        if (near && Pressed()) { dialogOpen = true; oldScale = Time.timeScale; Time.timeScale = 0f; }
    }

    /// Tallentaa dinerin vihut ja piilotetut mallit ensimmäisellä kerralla, kun hero on dinerissä.
    void CaptureRoster()
    {
        captured = true;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || !e.isActiveAndEnabled) continue;
            if (e.transform.position.x < area.walkMinX - 6f || e.transform.position.x > area.walkMaxX + 6f) continue;
            var t = Instantiate(e.gameObject, transform);
            t.name = e.gameObject.name + " (malli)";
            t.SetActive(false);
            roster.Add(e); rosterHome.Add(e.transform.position); rosterTemplate.Add(t);
        }
    }

    void StartWar()
    {
        foes.Clear(); hits.Clear(); home.Clear(); gone.Clear();
        // kaikki dinerin vihut mukaan: poissa olevat (pudotetut, kaadetut, tuhoutuneet) ilmestyvät takaisin peliin
        for (int i = 0; i < roster.Count; i++)
        {
            var e = roster[i];
            if (e == null && rosterTemplate[i] != null)
            {
                var go = Instantiate(rosterTemplate[i]);
                go.name = rosterTemplate[i].name.Replace(" (malli)", "");
                e = go.GetComponent<Enemy>();
                roster[i] = e;
            }
            if (e == null) continue;
            e.WarReset(rosterHome[i]);
            foes.Add(e); hits[e] = 0; home[e] = rosterHome[i];
            e.WarWake();
        }
        var respawner = FindFirstObjectByType<DinerTableRespawn>();
        if (respawner != null) respawner.RespawnMissingNow();   // raivattu paikka: pöydät takaisin ehjinä
        heroHits = 0;
        Active = true;
        if (counter == null) counter = FindFirstObjectByType<PieCounter>();
        if (counter != null) counter.Reseed();
        msg = foes.Count > 0 ? "Piirakkasota alkoi!" : "Ei vastustajia dinerissä.";
        msgUntil = Time.unscaledTime + 2.5f;
        if (foes.Count == 0) Active = false;
    }

    /// Hero poistui dinerista kesken sodan: sota loppuu hiljaa, vihut takaisin paikoilleen.
    void AbortQuietly()
    {
        Active = false; PieDamage = false;
        foreach (var e in foes) if (e != null) e.WarReset(home.ContainsKey(e) ? home[e] : e.transform.position);
    }

    IEnumerator End(bool won, bool quit = false)
    {
        ending = true;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { fade = t / 0.4f; yield return null; }
        fade = 1f;
        Active = false; PieDamage = false;
        // vihut takaisin paikoilleen ja täysillä voimilla, hero Linseyn viereen
        foreach (var e in foes)
        {
            if (e == null) continue;
            e.WarReset(home.ContainsKey(e) ? home[e] : e.transform.position);
        }
        if (hero != null)
        {
            hero.WarHeal();
            Vector3 lp = linsey != null ? linsey.transform.position : hero.transform.position;
            hero.TeleportTo(new Vector3(lp.x - 1.8f, Mathf.Min(lp.y, area.maxDepthY - 0.3f), 0f));
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(hero.transform.position.x);
        }
        msg = quit ? "Piirakkasota päättyi." : won ? "Voitit piirakkasodan!" : "Sait 10 piirakkaa naamaan. Piirakkasota päättyi.";
        msgUntil = Time.unscaledTime + 3f;
        yield return new WaitForSecondsRealtime(0.15f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { fade = 1f - t / 0.4f; yield return null; }
        fade = 0f; ending = false;
    }

    // ---------------- syöte ----------------

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    static bool Yes()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
#endif
    }

    static bool No()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    // ---------------- ruutu ----------------

    void OnGUI()
    {
        if (black == null) { black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply(); }
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            box.normal.textColor = Color.white;
            big = new GUIStyle(box);
        }
        box.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);
        big.fontSize = Mathf.RoundToInt(Screen.height * 0.04f);
        if (Area.Current != area) return;
        float w = Screen.height * 0.6f, h = Screen.height * 0.065f;
        if (Active)
        {
            int alive = foes.Count - gone.Count;
            GUI.Box(new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.02f, w, h), "PIIRAKKASOTA   osumia sinuun " + heroHits + "/" + maxHits + "   vastustajia " + alive, box);
        }
        if (near && !dialogOpen && !ending)
            GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), "[E]  " + (Active ? "Lopeta piirakkasota" : "Puhu Linseylle"), box);
        if (dialogOpen)
        {
            float dw = Screen.height * 0.9f, dh = Screen.height * 0.32f;
            var r = new Rect((Screen.width - dw) * 0.5f, (Screen.height - dh) * 0.5f, dw, dh);
            GUI.Box(r, "", box);
            GUI.Label(new Rect(r.x, r.y + dh * 0.08f, dw, dh * 0.4f),
                Active ? "Lopetetaanko piirakkasota?" : "Linsey: \"Aloitetaanko piirakkasota?\"\nKukaan ei taistele, vain heitellään piirakoita. 10 osumaa pudottaa pois.", box);
            if (GUI.Button(new Rect(r.x + dw * 0.1f, r.y + dh * 0.58f, dw * 0.35f, dh * 0.28f), "[E] Kyllä")) { Time.timeScale = oldScale; dialogOpen = false; if (Active) StartCoroutine(End(false, true)); else StartWar(); }
            if (GUI.Button(new Rect(r.x + dw * 0.55f, r.y + dh * 0.58f, dw * 0.35f, dh * 0.28f), "[Q] Ei")) { Time.timeScale = oldScale; dialogOpen = false; }
        }
        if (Time.unscaledTime < msgUntil)
            GUI.Box(new Rect((Screen.width - w * 1.4f) * 0.5f, Screen.height * 0.14f, w * 1.4f, h * 1.3f), msg, big);
        if (fade > 0f)
        {
            GUI.depth = -100;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);
            GUI.color = Color.white;
        }
    }
}
