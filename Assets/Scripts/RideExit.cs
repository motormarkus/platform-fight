using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Alueen loppu, josta ajetaan seuraavaan (esim. kujalta valtatielle): kun pelaaja ajaa prätkällä kohdan x ohi,
/// ruutu pimenee, näytetään otsikko ja pelaaja jatkaa ajoa kohdealueen alusta.
/// </summary>
public class RideExit : MonoBehaviour
            {
    public Area target;
    [Tooltip("Kohdealueen nimi ja scene, jos kohde on omassa sceneensä (ladataan tarvittaessa).")]
    public string targetName;
    public string targetScene;
    public Area here;
    [Tooltip("Poistutaan kynnyksen vasemmalle puolelle (Peltihalli).")]
    public bool exitToLeft;
    [Tooltip("Ajoreitti jatkuu kohdealueella oikealle; pyörä käännetään pimennyksessä.")]
    public bool turnBikeRight;
    public bool turnBikeLeft;
    public bool returnOnFoot;
    public bool preventParkedRide;
    static readonly System.Collections.Generic.List<Motorbike> parkedTravelBikes = new System.Collections.Generic.List<Motorbike>();   // kaksinpeli: molempien pyörät
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
        if (busy || (target == null && string.IsNullOrEmpty(targetName)) || (here != null && Area.Current != here)) return;
        // kuka tahansa ajava pelaaja (tai jalan palaava) laukaisee siirtymän: kaikki pyörät ja pelaajat siirtyvät yhdessä
        bool walkingReturn = returnOnFoot && parkedTravelBikes.Count > 0 && Area.Current == parkedTravelArea;
        if (walkingReturn)
        {
            foreach (var q in Players.All)
            {
                if (q == null || q.Riding) continue;
                float qx = q.transform.position.x, ex = transform.position.x;
                bool past = exitToLeft ? qx <= ex && qx >= ex - triggerWidth : qx >= ex && qx <= ex + triggerWidth;   // jalan palataan vain kynnyksen yli poistumissuuntaan
                if (past) { StartCoroutine(Go()); return; }
            }
            return;
        }
        foreach (var bike in Motorbike.RidingBikes())
        {
            var rider = bike.Rider;
            if (rider == null) continue;
            // vain kujan lopussa (kohdealue on samassa scenessä kauempana oikealla)
            float x = rider.transform.position.x;
            float threshold = transform.position.x;
            if (CameraFollow.Instance != null && Camera.main != null)
            {
                // Pyörän keskikohta pysähtyy puolen pyörän mitan ennen kuvan reunaa.
                // Poistumiskynnys on aina tämän liikerajan saavutettavalla puolella.
                float halfView = Camera.main.orthographicSize * Camera.main.aspect;
                float bikeLeftLimit = CameraFollow.Instance.minX - halfView + bike.halfLength;
                float bikeRightLimit = CameraFollow.Instance.maxX + halfView - bike.halfLength;
                if (Area.Current != null && Area.Current.walkMaxX != 0f)
                    bikeRightLimit = Mathf.Min(bikeRightLimit, Area.Current.walkMaxX - bike.halfLength * 0.6f);
                threshold = exitToLeft ? Mathf.Max(threshold, bikeLeftLimit + 0.25f)
                    : Mathf.Min(threshold, bikeRightLimit - 0.25f);
            }
            bool crossed = exitToLeft ? x <= threshold && x >= threshold - triggerWidth
                : x >= threshold && x <= threshold + triggerWidth;
            if (crossed) { StartCoroutine(Go()); return; }
        }
    }

    IEnumerator Go()
    {
        var bikes = returnOnFoot ? new System.Collections.Generic.List<Motorbike>(parkedTravelBikes) : Motorbike.RidingBikes();
        if (bikes.Count == 0) yield break;
        busy = true;
        foreach (var bk in bikes) bk.SetTravelLock(true);                 // pyörän selästä ei voi nousta kesken ajosiirtymän
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = t / fadeTime; yield return null; }
        fade = 1f;
        Time.timeScale = 0f;                       // ajo pysähtyy pimennyksen ajaksi
        for (float t = 0f; t < titleTime; t += Time.unscaledDeltaTime)
        {
            titleAlpha = Mathf.Min(1f, Mathf.Min(t / 0.3f, (titleTime - t) / 0.3f));
            yield return null;
        }
        titleAlpha = 0f;
        pc = Players.Primary;
        Area dest = target != null ? target : Area.Find(targetName);
        if (dest == null && !string.IsNullOrEmpty(targetScene))
        {
            var load = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);   // ruutu on mustana latauksen ajan
            if (load != null) while (!load.isDone) yield return null;
            dest = Area.Find(targetName);
        }
        if (dest != null) dest.Apply(pc);
        if (returnOnFoot) { parkedTravelBikes.Clear(); parkedTravelArea = null; }
        // jokainen pyörä omalle kaistalleen (P1 kohtaan spawnPoint / parkPoint, toinen sen viereen)
        for (int i = 0; i < bikes.Count; i++)
        {
            var bike = bikes[i];
            if (bike == null) continue;
            var rider = bike.Rider;
            Vector3 sp = new Vector3(spawnPoint.x - 1.2f * i, spawnPoint.y - 0.9f * i, 0f);
            if (rider != null) sp.y = Mathf.Clamp(sp.y, rider.minDepthY, rider.maxDepthY);
            Vector3 pk = new Vector3(parkPoint.x - 3.2f * i, parkPoint.y, 0f);
            if (rider != null) rider.transform.position = sp;
            if (returnOnFoot)
            {
                bike.ResumeTravel(sp, !turnBikeLeft);
                bike.parkedNoRideArea = null;
            }
            if (!parkBike && turnBikeRight) bike.ContinueRide(sp, true);
            if (!parkBike && turnBikeLeft) bike.ContinueRide(sp, false);
            if (parkBike)
            {
                bike.ParkNow(pk, true);   // pyörä parkkiin, pelaaja jalan vieressä
                if (preventParkedRide)
                {
                    bike.parkedNoRideArea = dest;
                    parkedTravelBikes.Add(bike); parkedTravelArea = dest;
                }
            }
        }
        // jalan olevat pelaajat (esim. toinen ei noussut pyörälle) tulevat mukaan
        int extra = 0;
        foreach (var q in Players.All)
        {
            if (q == null || q.Riding) continue;
            bool isRider = false;
            foreach (var bk in bikes) if (bk != null && bk.Rider == q) isRider = true;
            if (isRider) continue;
            extra++;
            Vector3 at = parkBike ? new Vector3(parkPoint.x - 1.4f * extra, parkPoint.y, 0f) : new Vector3(spawnPoint.x - 1.4f * extra, spawnPoint.y, 0f);
            q.TeleportTo(at);
        }
        if (parkBike && CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(parkPoint.x);
        else if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(spawnPoint.x);
        Time.timeScale = 1f;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { fade = 1f - t / fadeTime; yield return null; }
        fade = 0f;
        foreach (var bk in bikes) if (bk != null) bk.SetTravelLock(false);
        busy = false;
        Area.UnloadOtherScenes(dest, pc);
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
