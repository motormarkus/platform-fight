using System.Collections;
using UnityEngine;

/// <summary>
/// Alueen loppu, josta ajetaan seuraavaan (esim. kujalta valtatielle): kun pelaaja ajaa prätkällä kohdan x ohi,
/// ruutu pimenee, näytetään otsikko ja pelaaja jatkaa ajoa kohdealueen alusta.
/// </summary>
public class RideExit : MonoBehaviour
{
    public Area target;
    public Area here;
    [Tooltip("Poistutaan kynnyksen vasemmalle puolelle (Peltihalli).")]
    public bool exitToLeft;
    [Tooltip("Ajoreitti jatkuu kohdealueella oikealle; pyörä käännetään pimennyksessä.")]
    public bool turnBikeRight;
    public bool turnBikeLeft;
    public bool returnOnFoot;
    public bool preventParkedRide;
    static Motorbike parkedTravelBike;
    static Area parkedTravelArea;
    public Vector2 spawnPoint;
    public string title = "Valtatie";
    public float fadeTime = 0.4f, titleTime = 1.6f;
    [Tooltip("Pyörä jää parkkiin kohdealueen alkuun (pelaaja jatkaa jalan).")]
    public bool parkBike;
    [Tooltip("Pysäköintikohta kohdealueella (pyörä), jos parkBike.")]
    public Vector2 parkPoint;
    [Tooltip("Kuinka leveä tunnistusalue on kohdan x oikealla puolella.")]
    public float triggerWidth = 15f;

    PlayerController pc;
    bool busy;
    float fade, titleAlpha;

    void Update()
    {
        if (pc == null) pc = Players.Primary;
        bool walkingReturn = pc != null && returnOnFoot && !pc.Riding && parkedTravelBike != null && Area.Current == parkedTravelArea;
        if (pc == null || busy || (!walkingReturn && (!pc.Riding || Motorbike.Current == null)) || target == null || (here != null && Area.Current != here)) return;
        // vain kujan lopussa (kohdealue on samassa scenessä kauempana oikealla)
        float x = pc.transform.position.x;
        float threshold = transform.position.x;
        if (!walkingReturn && CameraFollow.Instance != null && Camera.main != null)
        {
            // Pyörän keskikohta pysähtyy puolen pyörän mitan ennen kuvan reunaa.
            // Poistumiskynnys on aina tämän liikerajan saavutettavalla puolella.
            float halfView = Camera.main.orthographicSize * Camera.main.aspect;
            float bikeLeftLimit = CameraFollow.Instance.minX - halfView + Motorbike.Current.halfLength;
            float bikeRightLimit = CameraFollow.Instance.maxX + halfView - Motorbike.Current.halfLength;
            if (Area.Current != null && Area.Current.walkMaxX != 0f)
                bikeRightLimit = Mathf.Min(bikeRightLimit, Area.Current.walkMaxX - Motorbike.Current.halfLength * 0.6f);
            threshold = exitToLeft ? Mathf.Max(threshold, bikeLeftLimit + 0.25f)
                : Mathf.Min(threshold, bikeRightLimit - 0.25f);
        }
        bool crossed = exitToLeft ? x <= threshold && x >= threshold - triggerWidth
            : x >= threshold && x <= threshold + triggerWidth;
        if (crossed) StartCoroutine(Go());
    }

    IEnumerator Go()
    {
        var bike = returnOnFoot ? parkedTravelBike : Motorbike.Current;
        if (bike == null) yield break;
        busy = true;
        bike.SetTravelLock(true);                 // pyörän selästä ei voi nousta kesken ajosiirtymän
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
        if (returnOnFoot)
        {
            bike.ResumeTravel(new Vector3(spawnPoint.x, spawnPoint.y, 0f), !turnBikeLeft);
            bike.parkedNoRideArea = null;
            parkedTravelBike = null; parkedTravelArea = null;
        }
        if (!parkBike && turnBikeRight && bike != null)
            bike.ContinueRide(new Vector3(spawnPoint.x, spawnPoint.y, 0f), true);
        if (!parkBike && turnBikeLeft && bike != null)
            bike.ContinueRide(new Vector3(spawnPoint.x, spawnPoint.y, 0f), false);
        if (parkBike && bike != null)
        {
            bike.ParkNow(new Vector3(parkPoint.x, parkPoint.y, 0f), true);   // pyörä parkkiin, pelaaja jalan vieressä
            if (preventParkedRide)
            {
                bike.parkedNoRideArea = target;
                parkedTravelBike = bike; parkedTravelArea = target;
            }
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(parkPoint.x);
        }
        else if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(spawnPoint.x);
        Time.timeScale = 1f;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = 1f - t / fadeTime; yield return null; }
        fade = 0f;
        if (bike != null) bike.SetTravelLock(false);
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
