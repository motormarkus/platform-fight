using UnityEngine;

/// <summary>
/// Kaukainen taustakerros (esim. valtatien maisema): liikkuu kameran mukana, mutta vain osan tien vauhdista.
/// Näkyy vain alueella minX…maxX (kameran x), muualla piilossa. Kuvan loppuessa kerros pysähtyy (ei toistoa).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Kuinka paljon kerros liikkuu suhteessa tiehen (0 = paikallaan ruudulla, 1 = tien mukana).")]
    [Range(0f, 1f)] public float factor = 0.08f;
    [Tooltip("Kameran x, jolla kerroksen vasen reuna on ruudun vasemmassa reunassa.")]
    public float startCamX;
    [Tooltip("Alue, jolla kerros näkyy (kameran x).")]
    public float minX, maxX;
    [Tooltip("Junan liike: kerros vierii itsestään vasemmalle näin monta yksikköä sekunnissa, kun juna on liikkeessä (0 = ei). Kuvan on oltava tiled ja yhden laatan verran leveämpi.")]
    public float autoSpeed;
    float autoShift;
    [Tooltip("Junan keinunta: kerros liikkuu pienesti ylös ja alas (yksikköä) junan vauhdin mukaan. Lähikerrokselle suurempi kuin kaukaiselle. 0 = ei.")]
    public float bobAmplitude;
    float baseY = float.NaN;

    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;
        float cx = cam.transform.position.x;
        bool show = cx >= minX - 1f && cx <= maxX + 1f;
        sr.enabled = show;
        if (!show) return;
        float halfScreen = cam.orthographicSize * cam.aspect;
        float halfW = sr.bounds.extents.x;
        // vasen reuna ruudun vasempaan reunaan aloituskohdassa; sitten liukuu hitaasti vasemmalle
        float shift = (cx - startCamX) * factor;
        shift = Mathf.Clamp(shift, 0f, Mathf.Max(0f, 2f * halfW - 2f * halfScreen));   // kuva ei lopu kesken
        if (autoSpeed != 0f)
        {
            float tile = sr.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
            if (tile > 0.01f)
            {
                autoShift = (autoShift + autoSpeed * TrainMotion.Current(Time.deltaTime) * Time.deltaTime) % tile;
                shift += autoShift;
            }
        }
        Vector3 p = transform.position;
        if (float.IsNaN(baseY)) baseY = p.y;
        if (bobAmplitude != 0f)
        {
            // kaksi hidasta siniaaltoa (eri taajuus) näyttää raiteen epätasaisuudelta, ei veneen heilunnalta
            float t = Time.time;
            float bob = Mathf.Sin(t * 3.7f) * 0.65f + Mathf.Sin(t * 8.3f + 1.3f) * 0.35f;
            p.y = baseY + bob * bobAmplitude * TrainMotion.Current(Time.deltaTime);
        }
        p.x = cx - halfScreen + halfW - shift;
        transform.position = p;
    }
}
