using System.Collections;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Roccon kämpän televisio: kun pelaaja menee telkkarin luo ja painaa E, TV-lähetys (video) alkaa pyöriä koko ruudulla ja selostus soi suomeksi (englanninkielinen peli näyttää englanninkieliset tekstit).
/// Video on lyhyt (6,5 s), joten se pyörii edestakaisin (eteenpäin ja takaperin) selostuksen ajan. Pelaajat pysäytetään, Enter ohittaa.
/// Kuvat: Resources/RoccoTv/f_000.jpg…, selostus: Resources/Sfx/RoccoTv/tv_lahetys.mp3. Lähetyksen voi katsoa uudelleen.
/// </summary>
public class RoccoTv : MonoBehaviour
{
    public Area area;
    [Tooltip("Telkkarin x-koordinaatti.")]
    public float tvX;
    [Tooltip("Etäisyys telkkarista (x), jossa E toimii.")]
    public float range = 2.2f;
    public float fps = 24f;
    public string framesFolder = "RoccoTv";
    public string clipPath = "Sfx/RoccoTv/tv_lahetys";

    /// Englanninkieliset tekstitykset (alku s, loppu s, teksti): ajat mitattu selostuksen puhetauoista.
    static readonly (float a, float b, string text)[] Subs =
    {
        (0.00f, 2.10f, "Riots have broken out in the streets."),
        (2.10f, 4.70f, "Residents are being urged to stay indoors."),
        (5.15f, 7.55f, "The National Guard is expected to arrive soon."),
        (8.10f, 11.05f, "The Gull’s whereabouts remain unknown."),
        (11.50f, 14.90f, "Gangs have taken full control\nof Bricktown’s streets."),
        (14.90f, 18.10f, "Some residents are already taking the law\ninto their own hands."),
        (18.55f, 21.85f, "The unrest is now spreading\nto the resort town of Uccopulco."),
        (22.30f, 25.95f, "Vacationers are urged to stay off the streets\nuntil further notice."),
        (26.40f, 28.45f, "We’ll bring you more on this story shortly."),
        (28.85f, 31.00f, "Now, a word from our sponsors."),
    };

    static readonly (float a, float b, string text)[] NoSubs = new (float, float, string)[0];   // suomeksi selostus ymmärretään ilman tekstejä
    Texture2D[] frames;
    AudioSource voice;
    bool playing, near;
    float t, length, fade;
    Texture2D black;
    GUIStyle promptStyle, subStyle;

    static bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    void Update()
    {
        near = false;
        if (playing || area == null || Area.Current != area) return;
        foreach (var p in Players.All)
        {
            if (p == null || p.GameOver || p.Scripted || Mathf.Abs(p.transform.position.x - tvX) > range) continue;
            near = true;
            if (CoopInput.Pick(p.playerIndex)) { StartCoroutine(Play()); return; }
        }
    }

    IEnumerator Play()
    {
        frames = Resources.LoadAll<Texture2D>(framesFolder).OrderBy(f => f.name).ToArray();
        var clip = Resources.Load<AudioClip>(clipPath);
        if (frames.Length == 0) yield break;
        length = clip != null ? clip.length : frames.Length / fps * 3f;
        if (voice == null) { voice = gameObject.AddComponent<AudioSource>(); voice.spatialBlend = 0f; voice.playOnAwake = false; }
        voice.clip = clip; voice.volume = 1f;
        if (clip != null) voice.Play();
        SetScripted(true);
        playing = true; t = 0f; fade = 0f;
        yield return null;   // E-painallus ei saa ohittaa heti
        while (t < length && !EnterPressed())
        {
            t += Time.unscaledDeltaTime;
            fade = Mathf.Clamp01(Mathf.Min(t / 0.5f, (length - t) / 0.5f));
            SetScripted(true);   // uudet pelaajat (kaksinpeli) mukaan
            yield return null;
        }
        if (voice != null) voice.Stop();
        playing = false;
        SetScripted(false);
    }

    static void SetScripted(bool on)
    {
        foreach (var pc in Players.All)
            if (pc != null) { pc.Scripted = on; if (on) pc.ScriptedMove = Vector2.zero; }
    }

    void OnDisable()
    {
        if (playing) { playing = false; SetScripted(false); if (voice != null) voice.Stop(); }
    }

    void OnGUI()
    {
        if (!playing)
        {
            if (!near) return;
            if (promptStyle == null)
            {
                promptStyle = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                promptStyle.normal.textColor = Color.white;
            }
            string text = "[E]  " + Loc.T("Katso TV");
            float pw = Screen.height * 0.45f, ph = Screen.height * 0.065f;
            GUI.Box(new Rect((Screen.width - pw) * 0.5f, Screen.height * 0.82f, pw, ph), text, promptStyle);
            return;
        }
        if (frames == null || frames.Length == 0) return;
        if (black == null) { black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply(); }
        GUI.depth = -60;
        GUI.color = new Color(1f, 1f, 1f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);
        float w = Screen.width, h = w * 9f / 16f;
        if (h > Screen.height) { h = Screen.height; w = h * 16f / 9f; }
        int idx = Mathf.Clamp(Mathf.RoundToInt(Mathf.PingPong(t * fps, frames.Length - 1)), 0, frames.Length - 1);
        GUI.DrawTexture(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h), frames[idx], ScaleMode.StretchToFill);

        if (subStyle == null)
        {
            subStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = Mathf.RoundToInt(Screen.height * 0.042f), fontStyle = FontStyle.Bold, wordWrap = true };
        }
        foreach (var s in Loc.Current == Loc.Lang.English ? Subs : NoSubs)
        {
            if (t < s.a || t >= s.b) continue;
            var rect = new Rect(Screen.width * 0.05f, 0f, Screen.width * 0.9f, Screen.height * 0.93f);
            subStyle.normal.textColor = new Color(0f, 0f, 0f, fade);   // varjo
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), s.text, subStyle);
            subStyle.normal.textColor = new Color(1f, 1f, 1f, fade);
            GUI.Label(rect, s.text, subStyle);
            break;
        }
        var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontSize = Mathf.Max(12, Screen.height / 50) };
        hint.normal.textColor = new Color(1f, 1f, 1f, 0.6f * fade);
        GUI.Label(new Rect(0, 0, Screen.width - 16f, Screen.height - 6f), "Enter = ohita", hint);
        GUI.color = Color.white;
    }
}
