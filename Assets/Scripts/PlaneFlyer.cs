using UnityEngine;

/// <summary>
/// Taustalla lentävä kone (kiitotie): lipuu ruudun poikki omaan tahtiinsa (speed, yksikköä/s; negatiivinen = vasemmalle) ja seuraa kameraa vain osan (factor) tien vauhdista,
/// joten eri koneet liikkuvat taustalla eri tahtiin. Näkyy vain, kun kamera on välillä minX…maxX. Kun kone poistuu ruudusta, se palaa toiselta puolelta (pauseSeconds tauon jälkeen).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlaneFlyer : MonoBehaviour
{
    public float speed = 1.5f, factor = 0.2f, startCamX, minX, maxX, pauseSeconds = 8f, startOffset;
    float offset, pauseUntil;
    SpriteRenderer sr;
    bool init;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;
        float cx = cam.transform.position.x;
        bool show = cx >= minX - 1f && cx <= maxX + 1f;
        if (!show) { sr.enabled = false; return; }
        float halfScreen = cam.orthographicSize * cam.aspect, w = sr.bounds.extents.x, lim = halfScreen + w + 1f;
        if (!init) { offset = startOffset; init = true; }
        if (Time.time >= pauseUntil)
        {
            offset += speed * Time.deltaTime;
            if (speed > 0f && offset > lim) { offset = -lim; pauseUntil = Time.time + pauseSeconds; }
            else if (speed < 0f && offset < -lim) { offset = lim; pauseUntil = Time.time + pauseSeconds; }
        }
        sr.enabled = Time.time >= pauseUntil;
        var p = transform.position;
        p.x = cx + offset - (cx - startCamX) * factor;
        transform.position = p;
    }
}
