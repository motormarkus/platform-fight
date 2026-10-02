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
        Vector3 p = transform.position;
        p.x = cx - halfScreen + halfW - shift;
        transform.position = p;
    }
}
