using System.Collections;
using UnityEngine;

/// <summary>
/// Alueen loppu, josta ajetaan seuraavaan (esim. kujalta valtatielle): kun pelaaja ajaa prätkällä kohdan x ohi,
/// ruutu pimenee, näytetään otsikko ja pelaaja jatkaa ajoa kohdealueen alusta.
/// </summary>
public class RideExit : MonoBehaviour
{
    public Area target;
    public Vector2 spawnPoint;
    public string title = "Valtatie";
    public float fadeTime = 0.4f, titleTime = 1.6f;
    [Tooltip("Kuinka leveä tunnistusalue on kohdan x oikealla puolella.")]
    public float triggerWidth = 15f;

    PlayerController pc;
    bool busy;
    float fade, titleAlpha;

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || busy || !pc.Riding) return;
        // vain kujan lopussa (kohdealue on samassa scenessä kauempana oikealla)
        float x = pc.transform.position.x;
        if (x >= transform.position.x && x <= transform.position.x + triggerWidth) StartCoroutine(Go());
    }

    IEnumerator Go()
    {
        busy = true;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = t / fadeTime; yield return null; }
        fade = 1f;
        Time.timeScale = 0f;                       // ajo pysähtyy pimennyksen ajaksi
        for (float t = 0f; t < titleTime; t += Time.unscaledDeltaTime)
        {
            titleAlpha = Mathf.Min(1f, Mathf.Min(t / 0.3f, (titleTime - t) / 0.3f));
            yield return null;
        }
        titleAlpha = 0f;
        if (target != null) target.Apply(pc);
        pc.transform.position = new Vector3(spawnPoint.x, spawnPoint.y, 0f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(spawnPoint.x);
        Time.timeScale = 1f;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = 1f - t / fadeTime; yield return null; }
        fade = 0f;
        busy = false;
    }

    static Texture2D black;
    GUIStyle style;

    void OnGUI()
    {
        if (fade <= 0f) return;
        if (black == null) { black = new Texture2D(1, 1); black.SetPixel(0, 0, Color.black); black.Apply(); }
        GUI.depth = -100;
        GUI.color = new Color(1f, 1f, 1f, fade);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), black);
        if (titleAlpha > 0f)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.08f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            style.normal.textColor = new Color(1f, 0.85f, 0.4f, titleAlpha);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), Loc.T(title).ToUpperInvariant(), style);
        }
        GUI.color = Color.white;
    }
}
