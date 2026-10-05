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
    [Tooltip("Laivan keinunta: meri liikkuu rauhallisesti ylös ja alas kanteen nähden (yksikköä, sekuntia).")]
    public float bobAmplitude = 0f;
    public float bobPeriod = 7f;
    SpriteRenderer sr;
    float t, baseY;

    void Awake() { sr = GetComponent<SpriteRenderer>(); baseY = transform.position.y; }

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
        // keinunta: kaksi hidasta aaltoa päällekkäin, ettei liike ole mekaaninen
        float bob = bobAmplitude * (Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, bobPeriod)) * 0.8f
                                  + Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(0.1f, bobPeriod * 2.3f) + 1.3f) * 0.2f);
        transform.position = new Vector3(x, baseY + bob, transform.position.z);
    }
}
