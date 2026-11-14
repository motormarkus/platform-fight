using UnityEngine;

/// <summary>
/// Hääkappelin kohtauksen jälkeen (rähinä alkanut ja päättynyt: kaikki kappelin vihut kaadettu) ruudulle tulee ilmoitus "HÄÄT ESTETTY" ja sen alla "Puhu Violetille baarissa" (kasinolta vasemmalle).
/// Ei muita ilmoituksia: vihje kirjataan hiljaa (ei "UUSI VIHJE" -ponnahdusta, ei ääntä).
/// </summary>
public class ChapelAftermath : MonoBehaviour
{
    public SalsaBrawl brawl;
    public const string NextId = "tehtava_baari";
    public const string Title = "Häät estetty", Info = "Puhu Violetille baarissa (kasinolta vasemmalle)";
    float startedAt = -1f, quietFor, shownAt = -99f;
    bool done;
    GUIStyle titleStyle, infoStyle;

    void Update()
    {
        if (done || brawl == null) return;
        if (!brawl.Started) return;
        if (startedAt < 0f) startedAt = Time.time;
        if (Time.time - startedAt < 5f) return;
        if (brawl.Ongoing) { quietFor = 0f; return; }
        quietFor += Time.deltaTime;
        if (quietFor < 2f) return;
        done = true; shownAt = Time.unscaledTime;
        Clues.Add(NextId, Title, Info + ". Häät on estetty; Violetti odottaa baarissa.", null, false);   // hiljainen kirjaus tapauskansioon
    }

    void OnGUI()
    {
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
        titleStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(title.x + 3, title.y + 3, title.width, title.height), Loc.T(Title).ToUpper(), titleStyle);
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.25f, a); GUI.Label(title, Loc.T(Title).ToUpper(), titleStyle);
        infoStyle.normal.textColor = new Color(0f, 0f, 0f, a); GUI.Label(new Rect(info.x + 2, info.y + 2, info.width, info.height), Loc.T(Info), infoStyle);
        infoStyle.normal.textColor = new Color(1f, 1f, 1f, a); GUI.Label(info, Loc.T(Info), infoStyle);
    }
}
