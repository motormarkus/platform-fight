using UnityEngine;

/// <summary>
/// Itsestään rullaava, toistuva taustakerros (esim. meri laivan kannella: laiva liikkuu, tausta virtaa ohi).
/// autoSpeed = rullausnopeus (yksikköä/s, + = oikealle), parallax = kuinka paljon kerros liikkuu kadun mukana
/// (0 = pysyy ruudulla, 1 = liikkuu kadun mukana). Näkyy vain alueella area.
/// SpriteRendererin drawMode on Tiled (kuva toistuu vaakasuunnassa).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ScrollingLayer : MonoBehaviour
{
    public float autoSpeed = 1f;
    [Range(0f, 1f)] public float parallax = 0.1f;
    public Area area;
    SpriteRenderer sr;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;
        bool show = area == null || Area.Current == area;
        sr.enabled = show;
        if (!show) return;
        t += Time.deltaTime * autoSpeed;
        float tileW = sr.sprite.bounds.size.x;
        float cx = cam.transform.position.x;
        // kerroksen "oma" paikka: liikkuu kameran mukana (1 - parallax) ja rullaa; lähin kuvan toisto kameran kohdalle
        float x = cx * (1f - parallax) + t;
        x += Mathf.Round((cx - x) / tileW) * tileW;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
