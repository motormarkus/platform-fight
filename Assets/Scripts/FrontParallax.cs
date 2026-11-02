using UnityEngine;

/// <summary>Etuparallaksi: kerros liikkuu ruudulla nopeammin kuin maailma (factor yli 1), esim. iso verkkoaita etualalla. Kerros on luonnollisella paikallaan, kun kameran x on anchorCamX; näkyy vain visibleRange:n sisällä.</summary>
public class FrontParallax : MonoBehaviour
{
    public float factor = 1.35f, anchorCamX, visibleRange = 30f;
    Vector3 basePos; bool init;
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (!init) { basePos = transform.position; init = true; }
        float cx = cam.transform.position.x;
        bool show = Mathf.Abs(cx - anchorCamX) <= visibleRange;
        if (sr != null) sr.enabled = show;
        if (!show) return;
        transform.position = new Vector3(basePos.x + (cx - anchorCamX) * (1f - factor), basePos.y, basePos.z);
    }
}
