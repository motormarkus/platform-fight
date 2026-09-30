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
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.028f), fontStyle = FontStyle.Bold };
            label.normal.textColor = Color.white;
            popupStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.032f) };
            bigStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.11f) };
        }

        float s = Screen.height / 1080f;
        float x = 40 * s, y = 30 * s, w = 420 * s, h = 26 * s;

        if (player != null)
        {
            GUI.Label(new Rect(x, y, w, h * 1.4f), Loc.T(playerName), label);
            Bar(new Rect(x, y + h * 1.4f, w, h), player.health / (float)Mathf.Max(1, player.maxHealth), new Color(1f, 0.85f, 0.1f));
            // elämät ja rahat palkin oikealla puolella
            label.normal.textColor = Color.white;
            GUI.Label(new Rect(x + w + 24 * s, y + h * 1.25f, 200 * s, h * 1.4f), "x " + Mathf.Max(0, player.lives), label);
            label.normal.textColor = new Color(1f, 0.85f, 0.25f);
            GUI.Label(new Rect(x + w + 110 * s, y + h * 1.25f, 300 * s, h * 1.4f), player.money + " mk", label);
            label.normal.textColor = Color.white;
        }

        var e = Enemy.LastHit;
        if (e != null && Time.time - Enemy.LastHitTime < enemyBarTime)
        {
            float ey = y + h * 3.2f;
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

    void Bar(Rect r, float t, Color c)
    {
        GUI.color = Color.black; GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), white);
        GUI.color = new Color(0.25f, 0.05f, 0.05f); GUI.DrawTexture(r, white);
        GUI.color = c; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), white);
        GUI.color = Color.white;
    }
}
