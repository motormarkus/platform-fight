using UnityEngine;

/// <summary>
/// Lumirinteen vino, saumaton kerros (lumipinta, lumipöly): kuvan kopiot (cols x rows) täyttävät ruudun ja liukuvat vinosti ylös ja vasemmalle
/// rinteen kulmassa (angleDeg, rinne laskee oikealle alas), joten hahmo näyttää laskevan alas oikealle. Kuvan pitää toistua saumattomasti sekä vaaka- että pystysuunnassa.
/// Lapset (SpriteRenderer) luodaan valikossa; tileW/tileH ovat yhden kopion koko maailmayksikköinä.
/// </summary>
public class SlopeLoop : MonoBehaviour
{
    public float angleDeg = 14f, speed = 8f, tileW = 10f, tileH = 10f;
    public int cols = 3, rows = 3;
    public float phase;   // aloituskohta (0..1 kuvan koosta), jotta kerrokset eivät lähde samasta kohdasta

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || tileW < 0.1f || tileH < 0.1f) return;
        float a = angleDeg * Mathf.Deg2Rad, t = Time.time * speed + phase * tileW;
        float offX = Mathf.Repeat(-t * Mathf.Cos(a), tileW), offY = Mathf.Repeat(t * Mathf.Sin(a), tileH);
        float cx = cam.transform.position.x, cy = cam.transform.position.y;
        int n = transform.childCount;
        for (int k = 0; k < n; k++)
        {
            int i = k % cols, j = k / cols;
            var p = transform.GetChild(k).position;
            p.x = cx + offX + (i - (cols - 1) * 0.5f) * tileW;
            p.y = cy + offY + (j - (rows - 1) * 0.5f) * tileH;
            transform.GetChild(k).position = p;
        }
    }
}
