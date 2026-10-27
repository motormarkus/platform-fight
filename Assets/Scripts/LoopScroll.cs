using UnityEngine;

/// <summary>
/// Saumattomasti toistuva vaakakerros (koneen ikkunoiden taivas ja pilvikerros): kolme vierekkäistä kopiota seuraavat kameraa ja vierivät vasemmalle nopeudella speed
/// (kone lentää oikealle). parallax = kuinka paljon kerros seuraa maailmaa kameran liikkuessa (0 = pysyy ruudulla, 1 = maailman mukana). Pystykeinunta tulee CloudDrift-skriptistä (speed 0) ylätason oliolla.
/// Lapset: kuvan kopiot (SpriteRenderer), tileWidth = yhden kopion leveys maailmayksikköinä.
/// </summary>
public class LoopScroll : MonoBehaviour
{
    public float speed = 1f, parallax = 0.1f, tileWidth = 20f, startCamX;
    [Tooltip("Jos asetettu, kerros näkyy vain tässä alueessa (muuten se seuraisi kameraa kaikkialla).")]
    public Area onlyIn;
    float scroll;
    bool shown = true;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || tileWidth < 0.1f) return;
        bool near = Mathf.Abs(cam.transform.position.x - startCamX) < 300f;   // alueet ovat tuhansien yksiköiden päässä toisistaan: kerros näkyy vain omalla alueellaan (ei tarvita valikon uudelleenajoa)
        // näkyvyys joka kuvassa (myös uudelleen näkyviin tullessa)
        {
            bool show = near && (onlyIn == null || Area.Current == onlyIn);
            if (show != shown) { foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = show; shown = show; }
            if (!show) return;
        }
        scroll = (scroll + speed * Time.deltaTime) % tileWidth;
        float cx = cam.transform.position.x;
        float baseX = cx - (cx - startCamX) * parallax - scroll;   // pysyy ruudulla, valuu vain osan kameran liikkeestä
        // siirretään sopivaan jaksoon kameran ympärille
        float rel = Mathf.Repeat(baseX - cx + tileWidth * 0.5f, tileWidth) - tileWidth * 0.5f;   // keskikopion paikka kameraan nähden: -w/2 … w/2, naapurikopiot sen molemmin puolin
        int n = transform.childCount;
        for (int i = 0; i < n; i++)
        {
            var c = transform.GetChild(i);
            var p = c.position; p.x = cx + rel + (i - 1) * tileWidth; c.position = p;
        }
    }
}
