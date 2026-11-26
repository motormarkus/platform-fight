using UnityEngine;

/// <summary>
/// Kun kaikki listan vihut on kaadettu (kuollut tai poistettu), kirjataan vihje (esim. oven avaus tai kassakaapin avain) ja ruudulle tulee ilmoitus
/// (otsikko kullalla, alle ohje). Esim. Vegasin motelli: piha tyhjä -> "Mene huoneeseen 203"; huone 203 tyhjä -> kassakaapin avain.
/// Ei laukea uudelleen, jos vihje on jo olemassa (tallennus).
/// </summary>
public class ClearWatch : MonoBehaviour
{
    public Enemy[] enemies;
    public string clueId, clueTitle, clueText;
    [Tooltip("Näytä tavallinen UUSI VIHJE -ponnahdus (avain). Muuten kirjaus on hiljainen.")]
    public bool notify;
    public string bannerTitle, bannerInfo;
    public float delay = 1.5f;
    [Tooltip("Ponnahdus viimeisen vihun kohdalle (esim. \"KASSAKAAPIN AVAIN\").")]
    public string dropPopup;

    bool done;
    float clearFor, shownAt = -99f;
    Vector3 lastPos;
    GUIStyle titleStyle, infoStyle;

    void Start() { if (!string.IsNullOrEmpty(clueId) && Clues.Has(clueId)) done = true; }

    void Update()
    {
        if (done || enemies == null || enemies.Length == 0) return;
        bool any = false;
        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            any = true; lastPos = e.transform.position;
        }
        if (any) { clearFor = 0f; return; }
        clearFor += Time.deltaTime;
        if (clearFor < delay) return;
        done = true; shownAt = Time.unscaledTime;
        if (!string.IsNullOrEmpty(clueId)) Clues.Add(clueId, clueTitle, clueText, null, notify);
        if (!string.IsNullOrEmpty(dropPopup))
        {
            GameHUD.Popup(Loc.T(dropPopup), lastPos + Vector3.up * 2.4f, new Color(1f, 0.9f, 0.35f));
            HitFx.PlayPickup(true);
        }
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(bannerTitle)) return;
        float age = Time.unscaledTime - shownAt;
        if (age < 0f || age > 6f) return;
        float H = Screen.height, W = Screen.width;
        float a = Mathf.Clamp01(Mathf.Min(age * 3f, (6f - age) * 2f));
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            infoStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        titleStyle.fontSize = Mathf.RoundToInt(H * 0.065f); infoStyle.fontSize = Mathf.RoundToInt(H * 0.036f);
        var title = new Rect(0, H * 0.17f, W, H * 0.09f); var info = new Rect(0, H * 0.26f, W, H * 0.06f);
        string tt = Loc.T(bannerTitle).ToUpper(), it = Loc.T(bannerInfo);
        titleStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(title.x + 3, title.y + 3, title.width, title.height), tt, titleStyle);
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.25f, a); GUI.Label(title, tt, titleStyle);
        if (string.IsNullOrEmpty(it)) return;
        infoStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(info.x + 2, info.y + 2, info.width, info.height), it, infoStyle);
        infoStyle.normal.textColor = new Color(1f, 1f, 1f, a); GUI.Label(info, it, infoStyle);
    }
}
