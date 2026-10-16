using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ovi toiseen alueeseen. Kun pelaaja on oven kohdalla, ruudulla näkyy kehote ja
/// E-näppäin (ohjaimessa B) vie ruudun pimennyksen kautta kohdealueelle.
/// </summary>
public class Door : MonoBehaviour
{
    /// Ovi, josta viimeksi mentiin sisään (paluuovi vie takaisin sen eteen).
    public static Door LastUsed;
    static bool busy;

    public string prompt = "Mene sisään";
    [Tooltip("Alue, jossa ovi on.")]
    public Area here;
    [Tooltip("Alue, johon ovi vie.")]
    public Area target;
    [Tooltip("Mihin pelaaja ilmestyy kohdealueella.")]
    public Vector2 spawnPoint;
    [Tooltip("Palaa sen oven eteen, josta tultiin sisään (spawnPoint ohitetaan).")]
    public bool returnToLastDoor;
    [Tooltip("Sisääntulossa hahmo katsoo vasemmalle (esim. Peltihalli pelataan oikealta vasemmalle).")]
    public bool faceLeftOnArrival;
    [Tooltip("Ovi on ruudun alareunassa (esim. dinerin uloskäynti): käytettävissä alueen etureunassa takarajan sijaan.")]
    public bool atFront;
    [Tooltip("Ovi on lukossa, kunnes piirakkasota on voitettu ja Linsey on antanut romuttamon avaimen.")]
    public bool needsJunkyardKey;
    [Tooltip("Ovi on lukossa, kunnes kaikki nämä viholliset on voitettu (esim. romuttamon pihan vartijat).")]
    public Enemy[] guards;
    public string guardsLockedText = "Ovi on lukossa. Voita ensin pihan vartijat.";
    [Tooltip("Jos asetettu, E kysyy ensin tämän kysymyksen (E / Enter = kyllä, Esc / Q = ei) ennen siirtymää, esim. junamatka.")]
    public string confirmText;
    float lockedMsgUntil;
    bool confirming;

    bool GuardsAlive
    {
        get
        {
            if (guards == null) return false;
            foreach (var g in guards) if (g != null && !g.IsDead) return true;
            return false;
        }
    }

    [Header("Oven kohta")]
    public float halfWidth = 1.2f;
    [Tooltip("Kuinka kaukana seinästä (syvyyssuunnassa) ovea voi vielä käyttää.")]
    public float maxDistanceFromWall = 0.9f;

    [Header("Siirtymä")]
    public float fadeTime = 0.25f;
    [Tooltip("Palotikkaat: pelaaja kiipeää näin monta yksikköä ylös (negatiivinen = alas) ennen pimennystä. 0 = tavallinen ovi.")]
    public float climbHeight = 0f;
    public float climbTime = 0.8f;
    [Tooltip("Avautuva ovi (taustan päällä): aukeaa ennen siirtymää ja sulkeutuu pimennyksessä.")]
    public AnimatedDoor anim;
    public float animWait = 0.7f;
    [Tooltip("Ei käytettävissä, kun lähellä on pystyssä olevia vihollisia (esim. laivan tappelu).")]
    public bool blockedDuringFight;
    [Tooltip("Taistelulukon tarkistusetäisyys (yksikköä).") ]
    public float fightBlockRange = 20f;

    [Header("Viemärin kansi ja tikkaat")]
    [Tooltip("Ovi on maassa (esim. viemärin kansi): käytettävissä, kun pelaaja on tämän kohdalla syvyydessä (takaseinän sijaan).")]
    public bool onGround;
    public float groundHalfDepth = 0.9f;
    [Tooltip("Kansi, joka avataan ennen laskeutumista.")]
    public Manhole manhole;
    public float manholeWait = 0.55f;
    [Tooltip("Kansi, joka avataan perillä (hero nousee kaivosta).")]
    public Manhole emergeManhole;
    [Tooltip("Perillä hero tulee tämän verran yläpuolelta (+, tikkaat alas) tai alapuolelta (-, nousu kaivosta) paikalleen.")]
    public float arrivalOffsetY;
    public float arrivalTime = 0.7f;

    PlayerController pc;
    bool near;
    float fade;   // 0 = näkyvä, 1 = musta

    void Update()
    {
        if (busy) { near = false; confirming = false; return; }
        if (!confirming)
        {
            pc = Players.Nearest(transform.position);   // kaksinpeli: ovea käyttää lähin pelaaja, tai se joka painoi nappia
            if (Players.Count > 1)
                foreach (var q in Players.All) if (q != null && !q.GameOver && CoopInput.Pick(q.playerIndex)) { pc = q; break; }
        }
        if (pc == null) { near = false; confirming = false; return; }
        if (confirming)
        {
            if (Pressed() || EnterPressed()) { confirming = false; StartCoroutine(Go()); }
            else if (CancelPressed()) { confirming = false; SetAllEnabled(true); }
            return;
        }

        near = NearOk(pc) && !(blockedDuringFight && Enemy.HostileNear(transform.position, fightBlockRange));
        if (near && Pressed())
        {
            if (manhole != null && !manhole.IsOpen) pc.TryLiftLid(manhole);   // kiinni oleva kansi: ensin nostetaan pois, sitten voi laskeutua
            else if ((needsJunkyardKey && !PieWar.JunkyardKey) || GuardsAlive) lockedMsgUntil = Time.unscaledTime + 3f;
            else if (!AllNear()) partnerMsgUntil = Time.unscaledTime + 2.5f;   // kaksinpeli: molempien pitää olla paikalla
            else if (!string.IsNullOrEmpty(confirmText)) { confirming = true; SetAllEnabled(false); }
            else StartCoroutine(Go());
        }
    }

    static bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return);
#endif
    }

    static bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);
#endif
    }

    bool Pressed() => pc != null && CoopInput.Pick(pc.playerIndex);

    float partnerMsgUntil;

    /// Onko pelaaja oven kohdalla (sama ehto kaikille pelaajille).
    bool NearOk(PlayerController q)
    {
        Vector3 p = q.transform.position;
        return !q.Riding && !q.HoldingLid && Mathf.Abs(p.x - transform.position.x) <= halfWidth
            && (here == null || (onGround ? Mathf.Abs(p.y - transform.position.y) <= groundHalfDepth : atFront ? p.y <= here.minDepthY + maxDistanceFromWall : p.y >= here.MaxDepthAtX(p.x, here.maxDepthY) - maxDistanceFromWall))   // porrastettu takaraja (esim. kadun baari) tai etureuna
            && q.AirHeight <= 0.05f;
    }

    /// Kaksinpeli: kaikki elossa olevat pelaajat ovat oven kohdalla.
    bool AllNear()
    {
        foreach (var q in Players.All) if (q != null && !q.GameOver && !NearOk(q)) return false;
        return true;
    }

    /// Kaikki pelaajat pois ohjauksesta siirtymän ajaksi (kaksinpeli: molemmat siirtyvät yhdessä).
    static void SetAllEnabled(bool on)
    {
        foreach (var p in Players.All) if (p != null) p.enabled = on;
    }

    /// Muut pelaajat oven viereen (kaksinpeli).
    static void PlaceOthers(PlayerController who, Vector3 basePos)
    {
        int n = 0;
        foreach (var p in Players.All)
        {
            if (p == null || p == who) continue;
            n++;
            p.TeleportTo(basePos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f));
        }
    }

    IEnumerator Go()
    {
        busy = true;
        near = false;
        SetAllEnabled(false);

        if (anim != null)
        {
            anim.Open();
            for (float t = 0f; t < animWait; t += Time.unscaledDeltaTime) yield return null;
        }
        if (manhole != null)
        {
            manhole.SetOpen(true);
            for (float t = 0f; t < manholeWait; t += Time.unscaledDeltaTime) yield return null;
        }
        if (climbHeight != 0f)
        {
            // kiipeäminen: ukko siirtyy tikkaiden kohdalle ja nousee, ruutu pimenee loppumatkasta
            Vector3 start = new Vector3(transform.position.x, pc.transform.position.y, 0f);
            for (float t = 0f; t < climbTime; t += Time.unscaledDeltaTime)
            {
                float k = t / climbTime;
                pc.transform.position = start + Vector3.up * climbHeight * k;
                fade = Mathf.Clamp01((k - 0.45f) / 0.55f);
                yield return null;
            }
        }
        else
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = t / fadeTime; yield return null; }
        fade = 1f;

        Area dest = target;
        Vector3 pos = spawnPoint;
        // paluu vain, jos edellinen ovi on alueella, johon tämä vie (muuten esim. kujalta noussut päätyisi katon kadun puoleisista tikkaista kujalle)
        if (returnToLastDoor && LastUsed != null && LastUsed.here == target)
        {
            dest = LastUsed.here;
            pos = LastUsed.transform.position;
        }
        else LastUsed = this;

        if (anim != null) anim.Close();
        if (dest != null) dest.Apply(pc);
        Vector3 end = new Vector3(pos.x, pos.y, 0f);
        pc.TeleportTo(arrivalOffsetY != 0f ? end + Vector3.up * arrivalOffsetY : end);
        PlaceOthers(pc, arrivalOffsetY != 0f ? end + Vector3.up * arrivalOffsetY : end);
        if (faceLeftOnArrival) pc.Face(false);
        if (emergeManhole != null) emergeManhole.SetOpen(true, true);
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);

        yield return new WaitForSecondsRealtime(0.1f);
        if (arrivalOffsetY != 0f)
        {
            // saapuminen: hero liukuu paikalleen (tikkaita alas tai ylös kaivosta) pimennyksen hälvetessä, ohjaus vasta perillä
            float total = Mathf.Max(fadeTime, arrivalTime);
            for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / total);
                Vector3 cur = end + Vector3.up * arrivalOffsetY * (1f - k);
                pc.transform.position = cur;
                foreach (var o in Players.All) if (o != null && o != pc) o.transform.position = new Vector3(o.transform.position.x, cur.y, 0f);
                fade = Mathf.Clamp01(1f - t / fadeTime);
                yield return null;
            }
            pc.transform.position = end;
            foreach (var o in Players.All) if (o != null && o != pc) o.transform.position = new Vector3(o.transform.position.x, end.y, 0f);
            fade = 0f;
            SetAllEnabled(true);
            busy = false;
            yield break;
        }
        SetAllEnabled(true);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = 1f - t / fadeTime; yield return null; }
        fade = 0f;
        busy = false;
    }

    static Texture2D black;
    GUIStyle style;

    void OnGUI()
    {
        if (fade > 0f)
        {
            if (black == null) { black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply(); }
            GUI.depth = -100;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);
            GUI.color = Color.white;
        }
        if (!near && !confirming) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            style.normal.textColor = Color.white;
        }
        string text = Time.unscaledTime < partnerMsgUntil ? Loc.T("Molempien pelaajien pitää olla oven luona.")
            : confirming ? Loc.T(confirmText) + "  [E] Kyllä   [Esc] Ei"
            : needsJunkyardKey && !PieWar.JunkyardKey
            ? (Time.unscaledTime < lockedMsgUntil ? "Portti on lukossa. Voita piirakkasota dinerissä, niin Linsey antaa avaimen." : "[E]  Romuttamon portti (lukossa)")
            : GuardsAlive
            ? (Time.unscaledTime < lockedMsgUntil ? guardsLockedText : "[E]  " + Loc.T(prompt) + " (lukossa)")
            : manhole != null && !manhole.IsOpen ? "[E]  " + Loc.T("Nosta viemärin kansi")
            : "[E]  " + Loc.T(prompt);
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        // pitkä teksti jaetaan usealle riville: laatikko leveämmäksi (enintään 90 % ruudusta) ja tarvittaessa korkeammaksi
        float textW = style.CalcSize(new GUIContent(text)).x + Screen.height * 0.06f;
        if (textW > w) w = Mathf.Min(textW, Screen.width * 0.9f, Screen.height * 0.9f);
        h = Mathf.Max(h, style.CalcHeight(new GUIContent(text), w - Screen.height * 0.02f) + Screen.height * 0.02f);
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
