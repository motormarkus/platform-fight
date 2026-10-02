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

    [Header("Oven kohta")]
    public float halfWidth = 1.2f;
    [Tooltip("Kuinka kaukana seinästä (syvyyssuunnassa) ovea voi vielä käyttää.")]
    public float maxDistanceFromWall = 0.9f;

    [Header("Siirtymä")]
    public float fadeTime = 0.25f;
    [Tooltip("Palotikkaat: pelaaja kiipeää näin monta yksikköä ylös (negatiivinen = alas) ennen pimennystä. 0 = tavallinen ovi.")]
    public float climbHeight = 0f;
    public float climbTime = 0.8f;

    PlayerController pc;
    bool near;
    float fade;   // 0 = näkyvä, 1 = musta

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || busy) { near = false; return; }

        Vector3 p = pc.transform.position;
        near = !pc.Riding && Mathf.Abs(p.x - transform.position.x) <= halfWidth
            && (here == null || p.y >= here.maxDepthY - maxDistanceFromWall)
            && pc.AirHeight <= 0.05f;
        if (near && Pressed()) StartCoroutine(Go());
    }

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    IEnumerator Go()
    {
        busy = true;
        near = false;
        pc.enabled = false;

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

        if (dest != null) dest.Apply(pc);
        pc.TeleportTo(new Vector3(pos.x, pos.y, 0f));
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);

        yield return new WaitForSecondsRealtime(0.1f);
        pc.enabled = true;
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
        if (!near) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
        string text = "[E]  " + Loc.T(prompt);
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
