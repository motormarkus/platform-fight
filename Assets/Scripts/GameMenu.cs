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

    public string gameTitle = "PLATFORM FIGHT";
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
        "#Äänitehosteet\n{0}\nLasin särkyminen: Freesound.org (Creative Commons)\n\n" +
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
        "#Sound effects\n{0}\nGlass breaking: Freesound.org (Creative Commons)\n\n" +
        "#Motorcycle\nPurchased asset (licensed)\n\n" +
        "#Responsible for everything\n{0}\n\n\n" +
        "#Thank you for playing!";

    enum Page { None, Title, Difficulty, Pause, Options, Credits, ConfirmQuit }
    Page page = Page.None, back = Page.None;
    int sel;
    float creditsT;
    float oldTimeScale = 1f;
    GUIStyle titleStyle, itemStyle, smallStyle, headStyle;
    Texture2D white;
    float styleScale = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindFirstObjectByType<GameMenu>() != null) return;
        new GameObject("Valikot").AddComponent<GameMenu>();
    }

    void Start()
    {
        if (SkipTitleOnce) { SkipTitleOnce = false; return; }
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
        if (ConfirmPressed()) Choose(sel);
        else if (BackPressed())
        {
            if (page == Page.Pause) Close();
            else if (page == Page.Title) { }
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
                if (i == 0) { page = Page.Difficulty; sel = (int)GameSettings.Difficulty; }
                else if (i == 1) { back = Page.Title; page = Page.Options; sel = 0; }
                else if (i == 2) { back = Page.Title; page = Page.Credits; creditsT = 0f; }
                else { back = Page.Title; page = Page.ConfirmQuit; sel = 1; }
                break;
            case Page.Difficulty:
                if (i == 3) { page = Page.Title; sel = 0; break; }
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
        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * s), alignment = TextAnchor.MiddleCenter };
    }

    void Shadowed(Rect r, string text, GUIStyle st, Color c)
    {
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f * c.a); GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, st);
        GUI.color = c; GUI.Label(r, text, st);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (page == Page.None) return;
        Styles();
        GUI.depth = -100;
        float w = Screen.width, h = Screen.height, s = styleScale;
        GUI.color = new Color(0f, 0f, 0f, page == Page.Title || page == Page.Credits ? 0.75f : 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), white);
        GUI.color = Color.white;
        var gold = new Color(1f, 0.82f, 0.25f);

        if (page == Page.Credits) { DrawCredits(w, h, s, gold); return; }

        string head = page == Page.Title || page == Page.Difficulty ? gameTitle
                    : page == Page.Pause ? Loc.T("TAUKO") : page == Page.Options ? Loc.T("ASETUKSET") : Loc.T("Lopetetaanko peli?");
        Shadowed(new Rect(0, h * 0.14f, w, 130 * s), head, page == Page.ConfirmQuit ? itemStyle : titleStyle, gold);
        if (page == Page.Difficulty) Shadowed(new Rect(0, h * 0.27f, w, 60 * s), Loc.T("Valitse vaikeustaso"), headStyle, Color.white);

        var items = Items();
        float y0 = h * 0.38f, step = 66 * s;
        for (int i = 0; i < items.Length; i++)
        {
            bool on = i == sel;
            string t = page == Page.Options ? items[i] : Loc.T(items[i]);
            Shadowed(new Rect(0, y0 + i * step, w, step), on ? ">  " + t + "  <" : t, itemStyle, on ? gold : new Color(0.85f, 0.85f, 0.85f));
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
