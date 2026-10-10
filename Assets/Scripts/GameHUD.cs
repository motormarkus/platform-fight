using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kolikkopelityylinen HUD: pelaajan energia, elämät ja rahat, viimeksi lyödyn vihollisen energia,
/// kelluvat "+20 mk" -tekstit ja peli ohi -ruutu (Enter / Start = uusi peli).
/// </summary>
public class GameHUD : MonoBehaviour
{
    public PlayerController player;
    public string playerName = "PELAAJA";
    [Tooltip("Kuinka kauan vihollisen palkki näkyy osuman jälkeen.")]
    public float enemyBarTime = 3f;

    Texture2D white;
    float styleScale = -1f;
    GUIStyle label, popupStyle, bigStyle;

    // ---------------- Kelluvat tekstit ----------------
    struct Pop { public string text; public Vector3 pos; public Color color; public float t0; }
    static readonly List<Pop> pops = new List<Pop>();
    const float PopTime = 1.1f;

    /// Kelluva teksti maailmassa (esim. "+20 mk").
    public static void Popup(string text, Vector3 worldPos, Color color)
    {
        pops.Add(new Pop { text = text, pos = worldPos, color = color, t0 = Time.unscaledTime });
    }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (LanguagePressed())
        {
            Loc.Toggle();
            langShown = Time.unscaledTime;
        }
        if (player != null && player.GameOver && RestartPressed())
        {
            Time.timeScale = 1f;
            pops.Clear();
            GameMenu.SkipTitleOnce = true;   // uusi peli suoraan, ei aloitusvalikkoa
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    float langShown = -10f;

    /// F1 vaihtaa kielen (Suomi / English); valinta muistetaan.
    static bool LanguagePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F1);
#endif
    }

    static bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    void OnGUI()
    {
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        // mittakaava 1920x1080-ruudun mukaan, mutta niin että kapeammallakin ruudulla (esim. editorin Game-ikkuna) kaikki mahtuu
        float s = Mathf.Min(Screen.height / 1080f, Screen.width / 1920f);
        if (label == null || !Mathf.Approximately(s, styleScale))
        {
            styleScale = s;
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30 * s), fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            label.normal.textColor = Color.white;
            popupStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(35 * s) };
            bigStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(119 * s) };
        }

        // reunoista selvä marginaali: editorin Game-ikkuna voi leikata reunoja
        float x = 40 * s + Screen.width * 0.025f, y = 30 * s + Screen.height * 0.035f, w = 420 * s, h = 26 * s;

        if (player != null)
        {
            GUI.Label(new Rect(x, y, w, h * 1.4f), string.IsNullOrEmpty(player.characterName) ? Loc.T(playerName) : player.characterName.ToUpper(), label);
            Bar(new Rect(x, y + h * 1.4f, w, h), player.health / (float)Mathf.Max(1, player.maxHealth), new Color(1f, 0.85f, 0.1f));
            // stamina sinisenä energian alla; vilkkuu, jos liikkeeseen ei riittänyt
            bool empty = Time.time - player.StaminaEmptyTime < 0.5f && Mathf.FloorToInt((Time.time - player.StaminaEmptyTime) * 10f) % 2 == 0;
            Bar(new Rect(x, y + h * 2.4f + 8 * s, w, h * 0.6f), player.stamina / Mathf.Max(1f, player.maxStamina),
                empty ? new Color(1f, 1f, 1f) : new Color(0.2f, 0.55f, 1f), new Color(0.04f, 0.08f, 0.25f));
            // elämät ja rahat palkin oikealla puolella
            label.normal.textColor = Color.white;
            GUI.Label(new Rect(x + w + 24 * s, y + h * 1.25f, 200 * s, h * 1.4f), "x " + Mathf.Max(0, player.lives), label);
            label.normal.textColor = new Color(1f, 0.85f, 0.25f);
            GUI.Label(new Rect(x + w + 110 * s, y + h * 1.25f, 300 * s, h * 1.4f), player.money + " mk", label);
            if (player.Boosted)
            {
                // pärinä: kullanoranssi teksti ja jäljellä oleva aika (känniteksti sen alle)
                label.normal.textColor = new Color(1f, 0.8f, 0.3f);
                GUI.Label(new Rect(x, y + h * 3.4f + 12 * s, 600 * s, h * 1.4f), Loc.T("PÄRINÄ") + "  " + Mathf.CeilToInt(player.BoostLeft), label);
            }
            if (player.IsDrunk)
            {
                // merimieskänni: teksti ja jäljellä oleva aika energiapalkkien alla, keinuu
                label.normal.textColor = new Color(1f, 0.55f, 0.15f);
                float sway = Mathf.Sin(Time.time * 2.3f) * 6f * s;
                GUI.Label(new Rect(x + sway, y + h * (player.Boosted ? 4.8f : 3.4f) + 12 * s, 600 * s, h * 1.4f), Loc.T("MERIMIESKÄNNI") + "  " + Mathf.CeilToInt(player.DrunkLeft), label);
            }
            label.normal.textColor = Color.white;
        }

        // pomon energiapalkki oikeaan yläkulmaan, samankokoinen kuin pelaajan
        var boss = Enemy.ActiveBoss;
        if (boss != null)
        {
            float bx = Screen.width - x - w;
            var right = new GUIStyle(label) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(bx, y, w, h * 1.4f), Loc.T(boss.displayName).ToUpper(), right);
            Bar(new Rect(bx, y + h * 1.4f, w, h), boss.Health / (float)Mathf.Max(1, boss.maxHealth), new Color(0.9f, 0.2f, 0.15f));
        }

        var e = Enemy.LastHit;
        if (e != null && e != boss && Time.time - Enemy.LastHitTime < enemyBarTime)
        {
            float ey = y + h * 3.8f;
            GUI.Label(new Rect(x, ey, w, h * 1.4f), Loc.T(e.displayName).ToUpper(), label);
            Bar(new Rect(x, ey + h * 1.4f, w * 0.8f, h * 0.8f), e.Health / (float)Mathf.Max(1, e.maxHealth), new Color(0.9f, 0.2f, 0.15f));
        }

        // kelluvat tekstit
        var cam = Camera.main;
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            float age = Time.unscaledTime - pops[i].t0;
            if (age > PopTime) { pops.RemoveAt(i); continue; }
            if (cam == null) continue;
            Vector3 sp = cam.WorldToScreenPoint(pops[i].pos + Vector3.up * age * 0.8f);
            var c = pops[i].color; c.a = 1f - age / PopTime;
            Shadowed(new Rect(sp.x - 150 * s, Screen.height - sp.y - 25 * s, 300 * s, 50 * s), pops[i].text, popupStyle, c);
        }

        if (Time.unscaledTime - langShown < 2f)
            Shadowed(new Rect(0, Screen.height * 0.12f, Screen.width, Screen.height * 0.06f), Loc.T("Kieli: Suomi"), popupStyle, Color.white);

        if (player != null && player.GameOver)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), white);
            GUI.color = Color.white;
            Shadowed(new Rect(0, Screen.height * 0.35f, Screen.width, Screen.height * 0.15f), "GAME OVER", bigStyle, new Color(1f, 0.2f, 0.2f));
            Shadowed(new Rect(0, Screen.height * 0.52f, Screen.width, Screen.height * 0.06f), Loc.T("Enter = uusi peli"), popupStyle, Color.white);
        }
    }

    void Shadowed(Rect r, string text, GUIStyle st, Color c)
    {
        var old = st.normal.textColor;
        st.normal.textColor = new Color(0f, 0f, 0f, c.a);
        GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, st);
        st.normal.textColor = c;
        GUI.Label(r, text, st);
        st.normal.textColor = old;
    }

    void Bar(Rect r, float t, Color c) => Bar(r, t, c, new Color(0.25f, 0.05f, 0.05f));

    void Bar(Rect r, float t, Color c, Color back)
    {
        GUI.color = Color.black; GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), white);
        GUI.color = back; GUI.DrawTexture(r, white);
        GUI.color = c; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), white);
        GUI.color = Color.white;
    }
}
