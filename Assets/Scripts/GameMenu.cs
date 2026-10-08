using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Aloitusvalikko, taukovalikko (Esc / Start), asetukset, vaikeustaso ja tekijäluettelo.
/// Luodaan automaattisesti, kun scene latautuu. Peli on pysähdyksissä valikon ollessa auki.
/// Ohjaus: nuolet / tatti / ristiohjain, Enter / Space / A = valitse, Esc / B = takaisin.
/// </summary>
public class GameMenu : MonoBehaviour
{
    static bool open;
    static int closedFrame = -1;
    /// Valikko auki (myös sulkemisruudulla, ettei valinta-painallus laukea hypyksi tai lyönniksi).
    public static bool IsOpen => open || Time.frameCount <= closedFrame;
    int shopOpenFrame = -10;
    /// Uusi peli game overin jälkeen: aloitusvalikko ohitetaan kerran.
    public static bool SkipTitleOnce;
    /// Hahmo vaihtui valinnassa: scene ladataan uudelleen ja jatketaan suoraan vaikeustason valintaan.
    static bool difficultyOnce;

    public string gameTitle = "TOTAL FIST";
    [Tooltip("Tekijän nimi tekijäluetteloon.")]
    public string author = "Tekijä";
    [Tooltip("Tekijäluettelon rivit. {0} = tekijän nimi. Tyhjä rivi = väli, # alussa = otsikko.")]
    [TextArea(8, 30)]
    public string credits =
        "#{1}\n\n" +
        "#Idea, suunnittelu, ohjaus ja tuotanto\n{0}\n\n" +
        "#Grafiikka ja animaatiot\nTekoälyllä tuotettu, ohjannut ja valinnut {0}\n\n" +
        "#Ohjelmointi\nTekoälyllä tuotettu (Claude), ohjannut ja testannut {0}\n\n" +
        "#Musiikki\nSävellys {0}\n\n" +
        "#Äänitehosteet\n{0}\nLasin särkyminen: Freesound.org (CC0)\n\n" +
        "#Moottoripyörä\nOstettu valmis grafiikka (lisensoitu)\n\n" +
        "#Vastuu kaikesta\n{0}\n\n\n" +
        "#Kiitos pelaamisesta!";
    [TextArea(8, 30)]
    public string creditsEn =
        "#{1}\n\n" +
        "#Idea, design, direction and production\n{0}\n\n" +
        "#Graphics and animation\nAI-generated, directed and curated by {0}\n\n" +
        "#Programming\nAI-generated (Claude), directed and tested by {0}\n\n" +
        "#Music\nComposed by {0}\n\n" +
        "#Sound effects\n{0}\nGlass breaking: Freesound.org (CC0)\n\n" +
        "#Motorcycle\nPurchased asset (licensed)\n\n" +
        "#Responsible for everything\n{0}\n\n\n" +
        "#Thank you for playing!";

    enum Page { None, Title, Character, Difficulty, Pause, Options, Credits, ConfirmQuit }
    Page page = Page.None, back = Page.None;
    int sel;
    float creditsT;
    float oldTimeScale = 1f;
    GUIStyle titleStyle, itemStyle, smallStyle, headStyle;
    Texture2D white, titleArt, fadeDown, logo;
    float styleScale = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        Ensure();
        // myös uudelleenlatauksen jälkeen (alusta, päävalikkoon, hahmon vaihto)
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    static void Ensure()
    {
        if (FindFirstObjectByType<GameMenu>() != null) return;
        new GameObject("Valikot").AddComponent<GameMenu>();
    }

    void Start()
    {
        if (SkipTitleOnce) { SkipTitleOnce = false; return; }
        if (difficultyOnce) { difficultyOnce = false; Open(Page.Difficulty); sel = (int)GameSettings.Difficulty; return; }
        Open(Page.Title);
    }

    void OnDestroy() { if (open) { open = false; Time.timeScale = 1f; AudioListener.pause = false; } }

    void Open(Page p)
    {
        if (!open) { oldTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; AudioListener.pause = p == Page.Pause; }
        open = true; page = p; sel = 0; creditsT = 0f;
    }

    void Close()
    {
        open = false; closedFrame = Time.frameCount + 1; page = Page.None; Time.timeScale = oldTimeScale; AudioListener.pause = false;
    }

    string[] Items()
    {
        switch (page)
        {
            case Page.Title: return new[] { "Aloita peli", "Asetukset", "Tekijät", "Lopeta" };
            case Page.Character: return new[] { "Rocco", HeroineName(), "Takaisin" };
            case Page.Difficulty: return new[] { "Helppo", "Normaali", "Vaikea", "Takaisin" };
            case Page.Pause: return new[] { "Jatka", "Asetukset", "Aloita alusta", "Päävalikkoon", "Lopeta peli" };
            case Page.Options:
                return new[] {
                    Loc.T("Äänet") + "  <  " + Mathf.RoundToInt(GameSettings.MasterVolume * 10) + "  >",
                    Loc.T("Musiikki") + "  <  " + Mathf.RoundToInt(GameSettings.MusicVolume * 10) + "  >",
                    Loc.T("Koko näyttö") + ":  " + Loc.T(Screen.fullScreen ? "Päällä" : "Pois"),
                    Loc.T("Resoluutio") + ":  " + Screen.width + " x " + Screen.height,
                    Loc.T("Kieli") + ":  " + (Loc.Current == Loc.Lang.Suomi ? "Suomi" : "English"),
                    Loc.T("Vaikeustaso") + ":  " + Loc.T(GameSettings.Difficulty.ToString()),
                    "Takaisin" };
            case Page.ConfirmQuit: return new[] { "Kyllä, lopeta", "Ei" };
            default: return new string[0];
        }
    }

    void Update()
    {
        if (page == Page.None)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (ShopCounter.IsOpen) shopOpenFrame = Time.frameCount;
            bool shopJustClosed = Time.frameCount - shopOpenFrame <= 1;   // kaupan sulkeva Esc ei avaa taukoa
            if (PausePressed() && !ShopCounter.IsOpen && !shopJustClosed && (pc == null || !pc.GameOver)) Open(Page.Pause);
            return;
        }
        if (page == Page.Credits)
        {
            creditsT += Time.unscaledDeltaTime;
            if (ConfirmPressed() || BackPressed()) { page = back; sel = 0; }
            return;
        }
        var items = Items();
        int dy = NavY();
        if (dy != 0) sel = (sel + dy + items.Length) % items.Length;
        int dx = NavX();
        if (page == Page.Options && dx != 0) Adjust(sel, dx);
        if (page == Page.Character && dx != 0 && sel < 2) sel = 1 - sel;
        if (ConfirmPressed()) Choose(sel);
        else if (BackPressed())
        {
            if (page == Page.Pause) Close();
            else if (page == Page.Title) { }
            else if (page == Page.Character) { page = Page.Title; sel = 0; }
            else if (page == Page.Difficulty) { page = Page.Character; sel = GameSettings.Character; }
            else { page = back == Page.None ? Page.Title : back; sel = 0; }
        }
    }

    void Adjust(int i, int dx)
    {
        if (i == 0) GameSettings.MasterVolume += dx * 0.1f;
        else if (i == 1) GameSettings.MusicVolume += dx * 0.1f;
        else if (i == 3) CycleResolution(dx);
        else Choose(i);
    }

    void CycleResolution(int dx)
    {
        var rs = Screen.resolutions;
        if (rs.Length == 0) return;
        int cur = 0;
        for (int k = 0; k < rs.Length; k++) if (rs[k].width == Screen.width && rs[k].height == Screen.height) cur = k;
        var r = rs[(cur + dx + rs.Length) % rs.Length];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
    }

    void Choose(int i)
    {
        switch (page)
        {
            case Page.Title:
                if (i == 0) { page = Page.Character; sel = Mathf.Clamp(GameSettings.Character, 0, 1); }
                else if (i == 1) { back = Page.Title; page = Page.Options; sel = 0; }
                else if (i == 2) { back = Page.Title; page = Page.Credits; creditsT = 0f; }
                else { back = Page.Title; page = Page.ConfirmQuit; sel = 1; }
                break;
            case Page.Character:
            {
                if (i == 2) { page = Page.Title; sel = 0; break; }
                GameSettings.Character = i;
                var cur = FindFirstObjectByType<PlayerController>();
                // hahmo vaihtui: kuvat vaihdetaan pelaajan herätessä, joten scene ladataan uudelleen
                if (cur != null && cur.AppliedCharacter != i) { difficultyOnce = true; Restart(); break; }
                page = Page.Difficulty; sel = (int)GameSettings.Difficulty;
                break;
            }
            case Page.Difficulty:
                if (i == 3) { page = Page.Character; sel = GameSettings.Character; break; }
                GameSettings.Difficulty = (GameSettings.Level)i;
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) pc.lives = GameSettings.Lives;
                Close();
                break;
            case Page.Pause:
                if (i == 0) Close();
                else if (i == 1) { back = Page.Pause; page = Page.Options; sel = 0; }
                else if (i == 2) { SkipTitleOnce = true; Restart(); }
                else if (i == 3) Restart();
                else { back = Page.Pause; page = Page.ConfirmQuit; sel = 1; }
                break;
            case Page.Options:
                if (i == 0) GameSettings.MasterVolume = GameSettings.MasterVolume >= 0.99f ? 0f : GameSettings.MasterVolume + 0.1f;
                else if (i == 1) GameSettings.MusicVolume = GameSettings.MusicVolume >= 0.99f ? 0f : GameSettings.MusicVolume + 0.1f;
                else if (i == 2) Screen.fullScreen = !Screen.fullScreen;
                else if (i == 3) CycleResolution(1);
                else if (i == 4) Loc.Toggle();
                else if (i == 5) GameSettings.Difficulty = (GameSettings.Level)(((int)GameSettings.Difficulty + 1) % 3);   // elämät vaihtuvat seuraavassa pelissä
                else { page = back; sel = 0; }
                break;
            case Page.ConfirmQuit:
                if (i == 0) Quit();
                else { page = back; sel = 0; }
                break;
        }
    }

    void Restart()
    {
        open = false; Time.timeScale = 1f; AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- Piirto ----------------

    void Styles()
    {
        float s = Screen.height / 1080f;
        if (Mathf.Approximately(s, styleScale) && titleStyle != null) return;
        styleScale = s;
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(96 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        itemStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(44 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headStyle = new GUIStyle(itemStyle) { fontSize = Mathf.RoundToInt(36 * s) };
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(32 * s), alignment = TextAnchor.MiddleCenter };
        // teemafontit (Resources/Valikko): otsikot ja valinnat Luckiest Guy, alatekstit Bangers
        if (menuFont == null) menuFont = Resources.Load<Font>("Valikko/LuckiestGuy");
        if (smallFont == null) smallFont = Resources.Load<Font>("Valikko/Bangers");
        if (menuFont != null) { titleStyle.font = itemStyle.font = headStyle.font = menuFont; titleStyle.fontStyle = itemStyle.fontStyle = headStyle.fontStyle = FontStyle.Normal; }
        if (smallFont != null) smallStyle.font = smallFont;
    }

    Font menuFont, smallFont;
    static readonly Color Purple = new Color(0.49f, 0.23f, 0.84f), Lime = new Color(0.75f, 0.94f, 0.08f), Yellow = new Color(1f, 0.84f, 0.24f);

    /// Teksti ääriviivalla (8 suuntaan) ja mustalla varjolla, logon tyyliin.
    void Outlined(Rect r, string text, GUIStyle st, Color fill, Color line, float ow)
    {
        var old = GUI.color;
        float sh = ow + 3f * styleScale;
        GUI.color = new Color(0f, 0f, 0f, 0.85f * fill.a); GUI.Label(new Rect(r.x + sh, r.y + sh, r.width, r.height), text, st);
        GUI.color = new Color(line.r, line.g, line.b, line.a * fill.a);
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            GUI.Label(new Rect(r.x + Mathf.Cos(a) * ow, r.y + Mathf.Sin(a) * ow, r.width, r.height), text, st);
        }
        GUI.color = fill; GUI.Label(r, text, st);
        GUI.color = old;
    }

    /// Valikon rivi: valittu keltainen mustalla reunalla, muut violetti vihreällä reunalla.
    void Item(Rect r, string text, bool on)
    {
        if (on) Outlined(r, text, itemStyle, Yellow, Color.black, 3f * styleScale);
        else Outlined(r, text, itemStyle, Purple, Lime, 2.5f * styleScale);
    }

    /// Pelin nimi tekstinä (jos logokuvaa ei löydy): logon värit.
    void Title(Rect r, string text, GUIStyle st) => Outlined(r, text, st, Purple, Lime, 3.5f * styleScale);

    /// Logo (Resources/Valikko/logo.png) keskitettynä; palauttaa piirretyn korkeuden (0 = ei logoa, piirretään nimi tekstinä).
    float Logo(float w, float top, float width)
    {
        if (logo == null) logo = Resources.Load<Texture2D>("Valikko/logo");
        if (logo == null) return 0f;
        float lh = width * logo.height / logo.width;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect((w - width) * 0.5f, top, width, lh), logo, ScaleMode.ScaleToFit);
        return lh;
    }

    void Shadowed(Rect r, string text, GUIStyle st, Color c) => Outlined(r, text, st, c, Color.black, 2f * styleScale);

    void OnGUI()
    {
        if (page == Page.None) return;
        Styles();
        GUI.depth = -100;
        float w = Screen.width, h = Screen.height, s = styleScale;
        var gold = new Color(1f, 0.82f, 0.25f);
        if (page == Page.Title && DrawTitleArt(w, h)) { DrawTitle(w, h, s, gold); return; }
        GUI.color = new Color(0f, 0f, 0f, page == Page.Title || page == Page.Credits ? 0.75f : 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), white);
        GUI.color = Color.white;

        if (page == Page.Credits) { DrawCredits(w, h, s, gold); return; }

        if (page == Page.Character) { DrawCharacters(w, h, s, gold); return; }

        string head = page == Page.Title || page == Page.Difficulty ? gameTitle
                    : page == Page.Pause ? Loc.T("TAUKO") : page == Page.Options ? Loc.T("ASETUKSET") : Loc.T("Lopetetaanko peli?");
        if (head != gameTitle || Logo(w, h * (page == Page.Difficulty ? 0.08f : 0.10f), w * 0.36f) <= 0f)
            Title(new Rect(0, h * 0.14f, w, 130 * s), head, page == Page.ConfirmQuit ? itemStyle : titleStyle);
        if (page == Page.Difficulty) Shadowed(new Rect(0, h * 0.27f, w, 60 * s), Loc.T("Valitse vaikeustaso"), headStyle, Color.white);

        var items = Items();
        float y0 = h * 0.38f, step = 66 * s;
        for (int i = 0; i < items.Length; i++)
        {
            bool on = i == sel;
            string t = page == Page.Options ? items[i] : Loc.T(items[i]);
            Item(new Rect(0, y0 + i * step, w, step), t, on);
        }
        if (page == Page.Difficulty && sel < 3)
        {
            string[] info = {
                "Viholliset lyövät heikommin, iskusi tehoavat enemmän. 5 elämää.",
                "Tasapainoinen haaste. 3 elämää.",
                "Viholliset lyövät kovempaa ja kestävät enemmän. 2 elämää." };
            Shadowed(new Rect(0, y0 + items.Length * step + 20 * s, w, 50 * s), Loc.T(info[sel]), smallStyle, Color.white);
        }
        string help = page == Page.Options ? "Ylös / alas valitse   Vasen / oikea säädä   Enter / A muuta   Esc / B takaisin" : "Ylös / alas valitse   Enter / A hyväksy   Esc / B takaisin";
        Shadowed(new Rect(0, h - 70 * s, w, 50 * s), Loc.T(help), smallStyle, new Color(1f, 1f, 1f, 0.7f));
    }

    /// Alkuvalikon taustakuva (Resources/Valikko/alkuvalikko.png) koko ruudulle, alaosaan tumma liukuma tekstien taakse.
    bool DrawTitleArt(float w, float h)
    {
        if (titleArt == null) titleArt = Resources.Load<Texture2D>("Valikko/alkuvalikko");
        if (titleArt == null) return false;
        if (fadeDown == null)
        {
            fadeDown = new Texture2D(1, 64) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) { float t = 1f - y / 63f; fadeDown.SetPixel(0, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 0.88f, t))); }   // y=0 alhaalla
            fadeDown.Apply();
        }
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0, 0, w, h), titleArt, ScaleMode.ScaleAndCrop);
        GUI.DrawTexture(new Rect(0, h * 0.45f, w, h * 0.55f), fadeDown);
        return true;
    }

    /// Alkuvalikko taustakuvan päällä: nimi ja valinnat alaosassa, ettei hahmojen kasvot peity.
    void DrawTitle(float w, float h, float s, Color gold)
    {
        float lh = Logo(w, h * 0.50f, w * 0.42f);
        if (lh <= 0f) Title(new Rect(0, h * 0.56f, w, 130 * s), gameTitle, titleStyle);
        var items = Items();
        float y0 = lh > 0f ? h * 0.50f + lh + 18f * s : h * 0.69f, step = 58 * s;
        for (int i = 0; i < items.Length; i++)
        {
            bool on = i == sel;
            string t = Loc.T(items[i]);
            Item(new Rect(0, y0 + i * step, w, step), t, on);
        }
        Shadowed(new Rect(0, h - 52 * s, w, 44 * s), Loc.T("Ylös / alas valitse   Enter / A hyväksy   Esc / B takaisin"), smallStyle, new Color(1f, 1f, 1f, 0.6f));
    }

    static string HeroineName()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        return pc != null && pc.heroine != null && !string.IsNullOrEmpty(pc.heroine.name) ? pc.heroine.name : "Ruby";
    }

    /// Hahmonvalinta: Rocco vasemmalla, Ruby oikealla (katsovat toisiaan), valittu korostettuna.
    int lastCharSel = -1; float charSelT;

    void DrawCharacters(float w, float h, float s, Color gold)
    {
        if (Logo(w, h * 0.05f, w * 0.30f) <= 0f) Title(new Rect(0, h * 0.10f, w, 130 * s), gameTitle, titleStyle);
        Shadowed(new Rect(0, h * 0.23f, w, 60 * s), Loc.T("Valitse hahmo"), headStyle, Color.white);
        var pc = FindFirstObjectByType<PlayerController>();
        Sprite[][] sets = { pc != null ? pc.HeroIdle : null, pc != null && pc.heroine != null ? pc.heroine.idle : null };
        var hr = pc != null ? pc.heroine : null;
        Sprite[][] combos = { pc != null ? pc.HeroCombo : null, hr != null ? PlayerController.ComboFrames(hr.jab, hr.cross, hr.leadHook, hr.rearHook) : null };
        if (sel != lastCharSel) { lastCharSel = sel; charSelT = Time.unscaledTime; }
        string[] names = { "Rocco", HeroineName() };
        float boxW = w * 0.3f, boxH = h * 0.44f, top = h * 0.29f;
        for (int k = 0; k < 2; k++)
        {
            bool on = sel == k;
            float cx = w * (k == 0 ? 0.32f : 0.68f);
            var set = sets[k];
            if (set != null && set.Length > 0)
            {
                Sprite sp = on ? set[(int)(Time.unscaledTime / 0.15f) % set.Length] : set[0];
                // valittu hahmo lyö kombon heti valittaessa ja sitten n. 4 s välein, välillä idle
                var cb = combos[k];
                if (on && cb != null && cb.Length > 0)
                {
                    float ct = (Time.unscaledTime - charSelT) % 4.5f;
                    int ci = (int)(ct / 0.06f);
                    if (ci < cb.Length) sp = cb[ci];
                }
                DrawSprite(new Rect(cx - boxW * 0.5f, top, boxW, boxH), sp, k == 1, on ? Color.white : new Color(0.35f, 0.35f, 0.35f), set[0]);
            }
            else Shadowed(new Rect(cx - boxW * 0.5f, top, boxW, boxH), "?", titleStyle, new Color(0.5f, 0.5f, 0.5f));
            Item(new Rect(cx - boxW * 0.5f, top + boxH + 6 * s, boxW, 66 * s), names[k].ToUpper(), on);
        }
        bool backOn = sel == 2;
        string b = Loc.T("Takaisin");
        Item(new Rect(0, top + boxH + 90 * s, w, 66 * s), b, backOn);
        Shadowed(new Rect(0, h - 70 * s, w, 50 * s), Loc.T("Ylös / alas valitse   Enter / A hyväksy   Esc / B takaisin"), smallStyle, new Color(1f, 1f, 1f, 0.7f));
    }

    /// Piirtää spriten laatikkoon jalat alareunassa, mittasuhteet säilyttäen (flip = peilikuva).
    /// Piirtää spriten laatikkoon. Mittakaava tulee vertailukuvasta (ref, esim. idlen 1. kuva), jotta eri kokoiset
    /// ruudut (kombon kuvat, poikkeava idle-ruutu) näkyvät samassa koossa; jalat (pivot) laatikon alareunan keskelle.
    static void DrawSprite(Rect box, Sprite sp, bool flip, Color tint, Sprite reference = null)
    {
        if (sp == null || sp.texture == null) return;
        Texture2D tex = sp.texture;
        Rect tr = sp.textureRect;
        var rf = reference != null ? reference : sp;
        float k = Mathf.Min(box.width / (rf.rect.width / rf.pixelsPerUnit), box.height / (rf.rect.height / rf.pixelsPerUnit));   // pikseliä / yksikkö
        float dw = sp.rect.width / sp.pixelsPerUnit * k, dh = sp.rect.height / sp.pixelsPerUnit * k;
        float px = sp.pivot.x / sp.rect.width;
        if (flip) px = 1f - px;
        var r = new Rect(box.center.x - dw * px, box.y + box.height - dh, dw, dh);
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        if (flip) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
        var old = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(r, tex, uv);
        GUI.color = old;
    }

    void DrawCredits(float w, float h, float s, Color gold)
    {
        string text = string.Format(Loc.Current == Loc.Lang.English ? creditsEn : credits, author, gameTitle);
        var lines = text.Split('\n');
        float lineH = 54 * s, total = lines.Length * lineH;
        float y = h - creditsT * 70f * s;   // rullaa ylös
        if (y + total < 0f) creditsT = 0f;  // alusta uudelleen
        foreach (var raw in lines)
        {
            bool headLine = raw.StartsWith("#");
            string t = headLine ? raw.Substring(1) : raw;
            if (y > -lineH && y < h) Shadowed(new Rect(0, y, w, lineH), t, headLine ? itemStyle : smallStyle, headLine ? gold : Color.white);
            y += lineH;
        }
        Shadowed(new Rect(0, h - 70 * s, w, 50 * s), Loc.T("Enter / A / Esc takaisin"), smallStyle, new Color(1f, 1f, 1f, 0.7f));
    }

    // ---------------- Syöte (näppäimistö ja ohjain) ----------------

    float navRepeat;
    int NavY()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var g = Gamepad.current;
        if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return -1;
        if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) return 1;
        if (g != null)
        {
            if (g.dpad.up.wasPressedThisFrame) return -1;
            if (g.dpad.down.wasPressedThisFrame) return 1;
            float v = g.leftStick.ReadValue().y;
            if (Mathf.Abs(v) < 0.5f) { navRepeat = 0f; return 0; }
            navRepeat -= Time.unscaledDeltaTime;
            if (navRepeat > 0f) return 0;
            navRepeat = 0.25f; return v > 0f ? -1 : 1;
        }
        return 0;
#else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return -1;
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return 1;
        return 0;
#endif
    }

    int NavX()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var g = Gamepad.current;
        if (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) return -1;
        if (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) return 1;
        if (g != null && g.dpad.left.wasPressedThisFrame) return -1;
        if (g != null && g.dpad.right.wasPressedThisFrame) return 1;
        return 0;
#else
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return -1;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return 1;
        return 0;
#endif
    }

    static bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var g = Gamepad.current;
        return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            || (g != null && g.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    static bool BackPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var g = Gamepad.current;
        return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) || (g != null && g.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
#endif
    }

    static bool PausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }
}
