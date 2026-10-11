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
    [Tooltip("Yökuva (kuunvalo), häivytetään päälle ShipNight.Mix mukaan.")]
    public Sprite nightSprite;
    [Tooltip("Rullauksen alkukohta (yksikköä): kuvan voi aloittaa halutusta kohdasta, esim. kuu näkyviin.")]
    public float startPhase;
    SpriteRenderer sr, nightR;
    float t, baseY;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>(); baseY = transform.position.y; t = startPhase;
        // Meri- ja taivaskuvat ovat yhteisiä usean valikon (kansi, sali, komentosilta) kanssa: kun toinen valikko tuo kuvan eri mittakaavassa,
        // kuvan yksikkökoko muuttuu eikä rakennushetken size enää täsmää. Silloin kuva toistuisi pystysuunnassa ja kuvan alaosan aallot
        // piirtyisivät taivaan päälle. Rakennushetkellä size.y = kuvan korkeus, joten sen avulla mittakaava palautetaan.
        if (sr.drawMode == SpriteDrawMode.Tiled && sr.sprite != null)
        {
            float hNow = sr.sprite.bounds.size.y;
            float k = hNow > 0.0001f ? sr.size.y / hNow : 1f;
            if (Mathf.Abs(k - 1f) > 0.01f)
            {
                transform.localScale = transform.localScale * k;
                sr.size = new Vector2(sr.size.x / k, hNow);
            }
        }
        if (nightSprite != null)
        {
            nightR = new GameObject("Yö").AddComponent<SpriteRenderer>();
            nightR.transform.SetParent(transform, false);
            nightR.sprite = nightSprite; nightR.drawMode = sr.drawMode;
            // yökuva samaan maailmankokoon kuin päiväkuva, vaikka sen tuontimittakaava olisi eri (aallot eivät toistu pystyyn)
            var dsz = sr.sprite.bounds.size; var nsz = nightSprite.bounds.size;
            float kx = nsz.x > 0.0001f ? dsz.x / nsz.x : 1f, ky = nsz.y > 0.0001f ? dsz.y / nsz.y : 1f;
            nightR.transform.localScale = new Vector3(kx, ky, 1f);
            nightR.size = new Vector2(sr.size.x / kx, sr.size.y / ky);
            nightR.sortingOrder = sr.sortingOrder + 1;
            nightR.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;
        bool show = area == null || Area.Current == area;
        sr.enabled = show;
        if (nightR != null) { nightR.enabled = show && ShipNight.Mix > 0.001f; nightR.color = new Color(1f, 1f, 1f, ShipNight.Mix); }
        if (!show) return;
        t += Time.deltaTime * autoSpeed;
        float tileW = sr.sprite.bounds.size.x * Mathf.Abs(transform.localScale.x);
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
