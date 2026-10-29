using UnityEngine;

/// <summary>
/// Junan katon kaukainen taivas: junamatka_vaihe1…9 peräkkäin yhtenä nauhana, joka vierii vasemmalle junan vauhdilla (TrainMotion) ja etenee järjestyksessä 1 → 9 ja pysähtyy kuvaan 9
/// Nauha alkaa alusta (kuva 1), kun katolle tullaan. Kuvia ei peilata eikä toisteta päällekkäin. Lapset: kuvat vasemmalta oikealle.
/// RoofProgress (0–1) kertoo, missä kohdassa kierrosta ollaan; katon lähimaasto (JourneyPhaseLayer.useRoofProgress) vaihtuu sen mukana samaa tahtia.
/// </summary>
public class JourneyStrip : MonoBehaviour
{
    [Tooltip("Alue, jolla nauha näkyy (junan katto).")]
    public Area area;
    [Tooltip("Vierimisnopeus (yksikköä sekunnissa) täydellä junan vauhdilla.")]
    public float speed = 4f;
    [Tooltip("Kuinka paljon nauha seuraa lisäksi kameran liikettä (0 = ei).")]
    [Range(0f, 1f)] public float parallax = 0.15f;
    public float startCamX;
    public static float RoofProgress;
    SpriteRenderer[] parts;
    float scroll;
    bool shown;

    void Awake()
    {
        parts = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in parts) r.enabled = false;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || parts == null || parts.Length == 0) return;
        bool here = area == null || Area.Current == area;
        if (here != shown)
        {
            shown = here;
            if (here) { scroll = 0f; RoofProgress = 0f; }   // katolle tultaessa alkaa alusta
            foreach (var r in parts) r.enabled = here;
        }
        if (!here) return;
        float tileW = 0f;
        foreach (var r in parts) if (r.sprite != null) { tileW = r.sprite.bounds.size.x * Mathf.Abs(r.transform.lossyScale.x); break; }
        if (tileW < 0.01f) return;
        float len = tileW * parts.Length, halfScreen = cam.orthographicSize * cam.aspect;
        float maxScroll = Mathf.Max(0.01f, len - 2f * halfScreen);   // nauha päättyy, kun kuva 9 täyttää ruudun: ei toistoa
        scroll = Mathf.Min(maxScroll, scroll + speed * TrainMotion.Current(Time.deltaTime) * Time.deltaTime);
        RoofProgress = scroll / maxScroll;
        float cx = cam.transform.position.x;
        float offset = Mathf.Min(maxScroll, scroll + Mathf.Max(0f, cx - startCamX) * parallax);
        for (int i = 0; i < parts.Length; i++)
        {
            var t = parts[i].transform;
            t.position = new Vector3(cx - halfScreen - offset + (i + 0.5f) * tileW, t.position.y, t.position.z);   // kuva 1 vasemmalla, sitten 2 … 9 oikealle
        }
    }
}
