using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// S-Clubin baaritiski: tiskin edessä E avaa valikon, josta ostetaan juomia ja snackeja markoilla.
/// Peli pysähtyy valikon ajaksi.
/// </summary>
public class ShopCounter : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        public string name;
        public int price;
        [Tooltip("Energiaa takaisin. 999 = täyteen.")]
        public int heal;
        [Tooltip("Staminaa takaisin. 999 = täyteen.")]
        public int stamina;
        public string comment;
        [Tooltip("Ääni ostettaessa (Assets/Audio/sfx).")]
        public AudioClip sound;
    }

    public static bool IsOpen;

    public string title = "S-CLUB  BAARI";
    [Tooltip("Myyjän nimi (näkyy vastauksissa).")]
    public string npcName = "Sohvi";
    [Tooltip("Kehote, kun pelaaja on tiskillä.")]
    public string prompt = "Puhu Sohville";
    public Item[] items =
    {
        new Item { name = "Sipsipussi",      price = 1,  heal = 10,  stamina = 10,  comment = "Rapsakka." },
        new Item { name = "Grillimakkara",   price = 2,  heal = 20,  stamina = 15,  comment = "Sinapilla." },
        new Item { name = "Lonkero",         price = 3,  heal = 30,  stamina = 30,  comment = "Kylmä ja kirpeä." },
        new Item { name = "Makkaraperunat",  price = 5,  heal = 50,  stamina = 25,  comment = "Kunnon annos." },
        new Item { name = "Tuoppi",          price = 6,  heal = 60,  stamina = 40,  comment = "Hanasta." },
        new Item { name = "Kossupaukku",     price = 15, heal = 999, stamina = 999, comment = "Täydet voimat!" },
    };

    [Header("Tiskin kohta")]
    public Area here;
    public float halfWidth = 4.5f;
    public float maxDistanceFromWall = 1.0f;

    [Header("Äänet")]
    [Range(0f, 1f)] public float soundVolume = 0.9f;

    AudioSource audioSource;
    PlayerController pc;
    bool near, open;
    int sel;
    string msg; float msgTime;
    float oldTimeScale = 1f;

    void Awake()
    {
        IsOpen = false;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;

        if (!open)
        {
            Vector3 p = pc.transform.position;
            near = !pc.GameOver && pc.enabled
                && Mathf.Abs(p.x - transform.position.x) <= halfWidth
                && (here == null || p.y >= here.MaxDepthAtX(p.x, here.maxDepthY) - maxDistanceFromWall)   // viisto tai porrastettu takaraja (laivan sali)
                && pc.AirHeight <= 0.05f;
            if (near && Pressed(Btn.Open)) Open();
            return;
        }

        if (Pressed(Btn.Up)) sel = (sel + items.Length - 1) % items.Length;
        if (Pressed(Btn.Down)) sel = (sel + 1) % items.Length;
        if (Pressed(Btn.Buy)) Buy(items[sel]);
        else if (Pressed(Btn.Close)) Close();
    }

    void Open()
    {
        open = true; IsOpen = true; near = false;
        msg = npcName + ": " + Loc.T("Mitä saisi olla?"); msgTime = Time.unscaledTime;
        pc.enabled = false;
        oldTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    void Close()
    {
        open = false; IsOpen = false;
        Time.timeScale = oldTimeScale <= 0.05f ? 1f : oldTimeScale;
        pc.enabled = true;
    }

    void Buy(Item it)
    {
        if (pc.health >= pc.maxHealth && (it.stamina <= 0 || pc.stamina >= pc.maxStamina)) { Say(Loc.T("Energia on jo täynnä.")); return; }
        if (pc.money < it.price) { Say(Loc.T("Ei riitä markat!")); return; }
        pc.money -= it.price;
        int got = pc.Heal(it.heal);
        int st = pc.AddStamina(it.stamina);
        if (IsDrink(it) && pc.AppliedCharacter == 1 && pc.heroine != null && pc.heroine.gulpSounds != null && pc.heroine.gulpSounds.Length > 0)
        {
            // Ruby juo: oma nielaisu, perään "aah"
            var g = pc.heroine.gulpSounds[Random.Range(0, pc.heroine.gulpSounds.Length)];
            audioSource.Stop();
            if (g != null) audioSource.PlayOneShot(g, soundVolume * pc.VoiceVolume);
            if (pc.heroine.aahSound != null) StartCoroutine(PlayLater(pc.heroine.aahSound, g != null ? g.length : 0.3f));
        }
        else if (it.sound != null) { audioSource.Stop(); audioSource.PlayOneShot(it.sound, soundVolume); }
        Say(Loc.F("{0}: +{1} energiaa, +{3} staminaa. {2}", Loc.T(it.name), got, Loc.T(it.comment), st));
    }

    /// Juoma (Rubylla omat juomaäänet): lonkero, tuoppi, kossupaukku ja muut juomaäänelliset.
    static bool IsDrink(Item it)
    {
        if (it.name == "Lonkero" || it.name == "Tuoppi" || it.name == "Kossupaukku") return true;
        string sn = it.sound != null ? it.sound.name : "";
        return sn.Contains("lonkero") || sn.Contains("kossu");
    }

    System.Collections.IEnumerator PlayLater(AudioClip c, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);   // kauppa pysäyttää ajan: odotetaan oikeaa aikaa
        if (audioSource != null && c != null) audioSource.PlayOneShot(c, soundVolume * (pc != null ? pc.VoiceVolume : 1f));
    }

    void Say(string s) { msg = s; msgTime = Time.unscaledTime; }

    enum Btn { Open, Up, Down, Buy, Close }

    static bool Pressed(Btn k)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; var g = Gamepad.current;
        switch (k)
        {
            case Btn.Open:  return (kb != null && kb.eKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame);
            case Btn.Up:    return (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.up.wasPressedThisFrame);
            case Btn.Down:  return (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.down.wasPressedThisFrame);
            case Btn.Buy:   return (kb != null && (kb.eKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame);
            default:        return (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) || (g != null && g.buttonEast.wasPressedThisFrame);
        }
#else
        switch (k)
        {
            case Btn.Open:  return Input.GetKeyDown(KeyCode.E);
            case Btn.Up:    return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
            case Btn.Down:  return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
            case Btn.Buy:   return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Return);
            default:        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Backspace);
        }
#endif
    }

    // ---------------- Piirto ----------------

    static Texture2D white;
    GUIStyle big, row, small, box;

    void Styles()
    {
        if (white == null) { white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply(); }
        float u = Screen.height / 1080f;
        big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(52 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        big.normal.textColor = new Color(1f, 0.3f, 0.75f);
        row = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(34 * u), fontStyle = FontStyle.Bold };
        small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * u), alignment = TextAnchor.MiddleCenter };
        small.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
        box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(34 * u), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        box.normal.textColor = Color.white;
    }

    void Fill(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, white); GUI.color = Color.white; }

    void OnGUI()
    {
        if (!near && !open) return;
        if (big == null) Styles();
        float u = Screen.height / 1080f;

        if (!open)
        {
            float w = 520 * u, h = 70 * u;
            GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), "[E]  " + Loc.T(prompt), box);
            return;
        }

        GUI.depth = -50;
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.55f));
        float pw = 1180 * u, ph = (260 + items.Length * 62) * u;
        var panel = new Rect((Screen.width - pw) * 0.5f, (Screen.height - ph) * 0.5f, pw, ph);
        Fill(new Rect(panel.x - 4 * u, panel.y - 4 * u, panel.width + 8 * u, panel.height + 8 * u), new Color(1f, 0.25f, 0.7f));
        Fill(panel, new Color(0.08f, 0.03f, 0.08f, 0.97f));

        GUI.Label(new Rect(panel.x, panel.y + 18 * u, pw, 70 * u), Loc.T(title), big);

        float y = panel.y + 110 * u;
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            bool s = i == sel;
            if (s) Fill(new Rect(panel.x + 30 * u, y - 4 * u, pw - 60 * u, 56 * u), new Color(1f, 0.25f, 0.7f, 0.35f));
            bool affordable = pc.money >= it.price;
            row.normal.textColor = s ? Color.white : (affordable ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.5f, 0.45f, 0.5f));
            row.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(panel.x + 60 * u, y, 700 * u, 48 * u), (s ? "> " : "   ") + Loc.T(it.name), row);
            row.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(panel.x + 740 * u, y, 170 * u, 48 * u), it.heal >= 999 ? Loc.T("TÄYSI") : "+" + it.heal, row);
            GUI.Label(new Rect(panel.x + 930 * u, y, 190 * u, 48 * u), it.price + " mk", row);
            y += 62 * u;
        }

        y += 14 * u;
        small.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(panel.x, y, pw, 40 * u), Loc.F("Rahaa: {0} mk      Energia: {1} / {2}      Stamina: {3} / {4}", pc.money, pc.health, pc.maxHealth, Mathf.RoundToInt(pc.stamina), Mathf.RoundToInt(pc.maxStamina)), small);
        small.normal.textColor = Color.white;
        GUI.Label(new Rect(panel.x, y + 42 * u, pw, 40 * u), msg, small);
        small.normal.textColor = new Color(0.7f, 0.7f, 0.75f);
        GUI.Label(new Rect(panel.x, panel.yMax - 50 * u, pw, 40 * u), Loc.T("W/S valitse   ·   E osta   ·   Esc poistu"), small);
    }
}
