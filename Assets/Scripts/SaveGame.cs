using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tallennus kentän alussa (muisti: tallennus-kentan-alussa). Kun pelaaja saapuu uuteen kenttäsceneen (Aavikko, Thaitown, Vankila ...)
/// ja on vapaana (ei ajoneuvossa, ruutu ei pimeänä), peli tallentaa: paikan, liput/vihjeet, pomopalkinnot, rahat, taskun Böönin/eväät ja tilastot.
/// Game over ja "Jatka" palauttavat tähän kohtaan. Yksi tallennuspaikka (PlayerPrefs).
/// </summary>
public class SaveGame : MonoBehaviour
{
    const string Key = "tallennus_v1";
    /// Scenet, joihin saapuminen EI ole kentän alku (Bricktownin osia ym.).
    static readonly string[] NotLevels = { "Nyrkkeilysali", "PokerTest" };

    [Serializable] public class ClueData { public string id, title, text, art; }
    [Serializable] public class PlayerData { public int index, money, booni; public string food1, food2; }
    [Serializable]
    public class Data
    {
        public int version = 1;
        public string scene, area; public float x, y;
        public int character, difficulty;
        public int lifeCount, specialLevel; public string[] perksDone;
        public ClueData[] clues;
        public PlayerData[] players;
        public float playSeconds; public int kills; public string[] bosses;
        public string savedAt;
    }

    /// Palautus käynnissä: kenttien alkuja ei tallenneta sillä välin.
    public static bool Restoring { get; private set; }
    static Data pending;
    static SaveGame inst;

    static int hasCache = -1;
    public static bool Has { get { if (hasCache < 0) hasCache = Load() != null ? 1 : 0; return hasCache == 1; } }

    /// Tallennettu kenttä (valikon tekstiin), tai null.
    public static string SavedPlace { get { var d = Load(); return d != null ? d.area : null; } }

    public static Data Load()
    {
        try { var s = PlayerPrefs.GetString(Key, ""); if (string.IsNullOrEmpty(s)) return null; var d = JsonUtility.FromJson<Data>(s); return d != null && d.version == 1 && !string.IsNullOrEmpty(d.scene) ? d : null; }
        catch (Exception e) { Debug.LogWarning("[Tallennus] luku epäonnistui: " + e.Message); return null; }
    }

    public static void Delete() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); hasCache = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Restoring = false; pending = null; inst = null; hasCache = -1; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (inst != null) return;
        var go = new GameObject("Tallennus"); DontDestroyOnLoad(go); inst = go.AddComponent<SaveGame>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // ---------- tallennus ----------

    string visitScene; bool savedThisVisit; float visitSince; Area visitArea;

    void Update()
    {
        if (Restoring || pending != null) return;
        var a = Area.Effective;
        if (a == null) return;
        string sc = a.gameObject.scene.name;
        if (sc != visitScene) { visitScene = sc; savedThisVisit = false; visitSince = Time.unscaledTime; visitArea = a; return; }
        if (savedThisVisit || Array.IndexOf(NotLevels, sc) >= 0) return;
        if (a != visitArea) { visitArea = a; visitSince = Time.unscaledTime; }   // alue vaihtui samassa scenessä: odotetaan taas hetki
        var pc = Players.Primary;
        if (pc == null || !pc.isActiveAndEnabled || pc.GameOver || pc.Riding || Players.AllDown) return;
        if (Time.timeScale <= 0f || ScreenFader.Alpha > 0.05f) return;
        if (Time.unscaledTime - visitSince < 1f) return;   // saapumisen siirtymä ensin valmiiksi
        Save(a, pc);
        savedThisVisit = true;
    }

    static void Save(Area a, PlayerController pc)
    {
        var d = new Data
        {
            scene = a.gameObject.scene.name, area = a.areaName, x = pc.transform.position.x, y = pc.transform.position.y,
            character = GameSettings.Character, difficulty = (int)GameSettings.Difficulty,
            lifeCount = BossPerks.LifeCount, specialLevel = BossPerks.SpecialLevel, perksDone = BossPerks.DoneKeys(),
            playSeconds = GameStats.PlaySeconds, kills = GameStats.Kills, bosses = new List<string>(GameStats.Bosses).ToArray(),
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        };
        var cl = new List<ClueData>();
        foreach (var c in Clues.All) cl.Add(new ClueData { id = c.id, title = c.title, text = c.text, art = c.art });
        d.clues = cl.ToArray();
        var pl = new List<PlayerData>();
        foreach (var p in Players.All) if (p != null) pl.Add(new PlayerData { index = p.playerIndex, money = p.money, booni = p.booniPocket, food1 = p.pocketFood, food2 = p.pocketFood2 });
        d.players = pl.ToArray();
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(d)); PlayerPrefs.Save(); hasCache = 1;
        Debug.Log("[Tallennus] tallennettu: " + d.area + " (" + d.scene + ") x " + d.x.ToString("0.0"));
        shownAt = Time.unscaledTime;   // AUTOSAVE + levyke oikeaan yläkulmaan
    }

    // ---------- palautus ----------

    /// Jatkaa tallennuksesta (aloitusvalikko, tauko tai game over). Palauttaa false, jos tallennusta ei ole.
    public static bool Continue()
    {
        var d = Load();
        if (d == null) return false;
        // pomopalkinnot ja liput ennen latausta: pelaajan Awake laskee elämälisän BossPerksistä
        Clues.Clear();
        if (d.clues != null) foreach (var c in d.clues) Clues.Add(c.id, c.title, c.text, c.art, false);
        BossPerks.Restore(d.lifeCount, d.specialLevel, d.perksDone);
        GameStats.PlaySeconds = d.playSeconds; GameStats.Kills = d.kills; GameStats.Bosses.Clear();
        if (d.bosses != null) foreach (var b in d.bosses) GameStats.Bosses.Add(b);
        GameSettings.Character = d.character;
        GameSettings.Difficulty = (GameSettings.Level)d.difficulty;
        ShowroomShow.ResetShow();
        pending = d; Restoring = true;
        GameMenu.SkipTitleOnce = true;
        Time.timeScale = 1f; AudioListener.pause = false;
        ScreenFader.Alpha = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        return true;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || pending == null || inst == null) return;
        ScreenFader.Alpha = 1f;
        inst.StartCoroutine(inst.RestoreRoutine(pending));
    }

    IEnumerator RestoreRoutine(Data d)
    {
        ScreenFader.Alpha = 1f;
        for (int i = 0; i < 3 || Players.Primary == null; i++) { ScreenFader.Alpha = 1f; if (i > 300) break; yield return null; }
        var pc = Players.Primary;
        if (pc == null) { Debug.LogWarning("[Tallennus] pelaajaa ei löytynyt"); Finish(); yield break; }
        foreach (var q in Players.All) if (q != null) q.enabled = false;

        // pomokyvyt (härkä, luurankokutsu) tarvitsevat mallin sen kentän scenestä
        yield return RecacheAbility("Temppelin kuningas", "Viidakko", BossPerks.HasSkeletonCall, d.scene);
        yield return RecacheAbility("Taikuri", "Vegas", BossPerks.HasBull, d.scene);

        Area dest = Area.Find(d.area);
        if (dest == null && !string.IsNullOrEmpty(d.scene) && !SceneManager.GetSceneByName(d.scene).isLoaded)
        {
            var load = SceneManager.LoadSceneAsync(d.scene, LoadSceneMode.Additive);
            if (load != null) while (!load.isDone) { ScreenFader.Alpha = 1f; yield return null; }
            dest = Area.Find(d.area);
        }
        if (dest == null) Debug.LogWarning("[Tallennus] aluetta ei löytynyt: " + d.area);
        else dest.Apply(pc);
        var pos = new Vector3(d.x, d.y, 0f);
        pc.TeleportTo(pos);
        int n = 0;
        foreach (var o in Players.All) { if (o == null || o == pc) continue; n++; o.TeleportTo(pos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f)); }
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(d.x);
        if (d.players != null)
            foreach (var p in Players.All)
            {
                if (p == null) continue;
                foreach (var pd in d.players) if (pd.index == p.playerIndex) { p.money = pd.money; p.booniPocket = pd.booni; p.pocketFood = pd.food1 ?? ""; p.pocketFood2 = pd.food2 ?? ""; }
                p.Heal(p.maxHealth);
            }

        yield return new WaitForSecondsRealtime(0.1f);
        if (dest != null) yield return Area.FreeOtherScenes(dest, pc);
        foreach (var q in Players.All) if (q != null) q.enabled = true;
        visitScene = dest != null ? dest.gameObject.scene.name : d.scene; visitArea = dest; savedThisVisit = true;   // ei tallenneta heti uudelleen
        Finish();
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / 0.4f; yield return null; }
        ScreenFader.Alpha = 0f;
        Debug.Log("[Tallennus] jatkettu: " + d.area);
    }

    static void Finish() { pending = null; Restoring = false; }

    // ---------- AUTOSAVE-merkki ----------

    static float shownAt = -99f;
    const float ShowTime = 4f;
    static Texture2D white, glow; static GUIStyle style;

    void OnGUI()
    {
        float t = Time.unscaledTime - shownAt;
        if (t < 0f || t > ShowTime) return;
        float a = Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((ShowTime - t) / 0.5f);
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        if (glow == null) glow = MakeGlow(64);
        float s = Screen.height / 1080f;
        float size = 22f * s, pad = 18f * s;
        float x = Screen.width - pad - size, y = pad;
        var old = GUI.color;
        float blink = 0.8f + 0.2f * Mathf.Cos(t * 6f);   // levyke vilkkuu hiukan tallennuksen aikana
        // hehku: sykkivä valokehä levykkeen ja tekstin takana, jotta tallennus huomataan
        float pulse = 0.55f + 0.45f * Mathf.Sin(t * 4.5f - 1.57f);
        float gs = size * (2.6f + 0.5f * pulse);
        GUI.color = new Color(0.45f, 0.85f, 1f, a * (0.35f + 0.5f * pulse));
        GUI.DrawTexture(new Rect(x + size * 0.5f - gs * 0.5f, y + size * 0.5f - gs * 0.5f, gs, gs), glow);
        GUI.color = new Color(0.45f, 0.85f, 1f, a * (0.15f + 0.25f * pulse));
        GUI.DrawTexture(new Rect(x - 150f * s, y - size * 0.6f, 150f * s, size * 2.2f), glow);
        // levyke: runko, metalliläppä aukolla, valkoinen tarra
        Box(x, y, size, size, new Color(0.16f, 0.22f, 0.36f, a * blink));
        Box(x + size * 0.22f, y, size * 0.52f, size * 0.34f, new Color(0.78f, 0.8f, 0.84f, a * blink));
        Box(x + size * 0.54f, y + size * 0.05f, size * 0.12f, size * 0.24f, new Color(0.16f, 0.22f, 0.36f, a * blink));
        Box(x + size * 0.16f, y + size * 0.5f, size * 0.68f, size * 0.42f, new Color(0.95f, 0.95f, 0.92f, a * blink));
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
        style.fontSize = Mathf.Max(10, Mathf.RoundToInt(17f * s));
        var r = new Rect(x - 160f * s - 8f * s, y, 160f * s, size);
        GUI.color = new Color(0f, 0f, 0f, a * 0.7f); GUI.Label(new Rect(r.x + 1.5f * s, r.y + 1.5f * s, r.width, r.height), "AUTOSAVE", style);
        float pulse2 = 0.55f + 0.45f * Mathf.Sin(t * 4.5f - 1.57f);
        GUI.color = Color.Lerp(Color.white, new Color(0.7f, 0.95f, 1f), pulse2) * new Color(1f, 1f, 1f, a); GUI.Label(r, "AUTOSAVE", style);
        GUI.color = old;
    }

    /// Pehmeä pyöreä valopilkku (keskeltä kirkas, reunoilta läpinäkyvä).
    static Texture2D MakeGlow(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n]; float c = (n - 1) * 0.5f;
        for (int j = 0; j < n; j++) for (int i = 0; i < n; i++)
        {
            float d = Mathf.Sqrt((i - c) * (i - c) + (j - c) * (j - c)) / c;
            float v = Mathf.Clamp01(1f - d); v = v * v;
            px[j * n + i] = new Color(1f, 1f, 1f, v);
        }
        tex.SetPixels(px); tex.Apply(); return tex;
    }

    static void Box(float x, float y, float w, float h, Color c)
    {
        if (c.a <= 0f) return;
        GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), white);
    }

    static IEnumerator RecacheAbility(string boss, string scene, bool have, string destScene)
    {
        if (have || !BossPerks.IsDone(boss)) yield break;
        bool had = SceneManager.GetSceneByName(scene).isLoaded;
        if (!had) { var l = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive); if (l != null) while (!l.isDone) { ScreenFader.Alpha = 1f; yield return null; } }
        BossPerks.Recache();
        if (!had && scene != destScene) { var u = SceneManager.UnloadSceneAsync(scene); if (u != null) while (!u.isDone) { ScreenFader.Alpha = 1f; yield return null; } }
    }
}
