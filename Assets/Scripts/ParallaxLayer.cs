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

    [Tooltip("Peilikopiot: kuva jatkuu oikealle peilattuina kopioina (kuva, peilikuva, kuva...), jolloin sauma on aina yhteensopiva. 0 = ei.")]
    public int mirrorCopies;

    [Tooltip("Lisäkuvat: jatkuvat peilikopioiden jälkeen oikealle perätysten (eri levyisiä, korkeus sovitettava tuonnissa).")]
    public Sprite[] extraSprites;

    [Tooltip("Lisää kerroksen loppuun yksi peilattu kopio viimeisestä kuvasta (sauma täsmää, koska peilataan liitosreunan yli).")]
    public bool mirrorEnd;

    SpriteRenderer sr;
    SpriteRenderer[] copies;
    float totalLocalW;   // koko kerroksen leveys paikallisina yksikköinä

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;
        float w = sr.sprite.bounds.size.x;   // paikallinen leveys (skaala periytyy lapsille)
        float right = w * 0.5f;              // oikea reuna juuren keskipisteestä
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        SpriteRenderer Add(string n, Sprite sp, bool flip)
        {
            float ew = sp.bounds.size.x;
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(right + ew * 0.5f, 0f, 0f);
            var c = go.AddComponent<SpriteRenderer>();
            c.sprite = sp; c.color = sr.color; c.sharedMaterial = sr.sharedMaterial;
            c.sortingLayerID = sr.sortingLayerID; c.sortingOrder = sr.sortingOrder; c.flipX = flip;
            right += ew;
            list.Add(c);
            return c;
        }
        for (int i = 0; i < mirrorCopies; i++) Add("Peili " + (i + 1), sr.sprite, (i % 2 == 0) ? !sr.flipX : sr.flipX);   // 1. kopio peilattu, 2. alkuperäinen suunta, ...
        if (extraSprites != null) for (int i = 0; i < extraSprites.Length; i++) if (extraSprites[i] != null) Add("Jatko " + (i + 1), extraSprites[i], false);
        if (mirrorEnd)
        {
            Sprite last = sr.sprite;
            if (extraSprites != null) for (int i = extraSprites.Length - 1; i >= 0; i--) if (extraSprites[i] != null) { last = extraSprites[i]; break; }
            Add("Peili loppu", last, true);
        }
        totalLocalW = right + w * 0.5f;
        if (list.Count > 0) copies = list.ToArray();
    }

    void Start()
    {
        // Kerros näkyy koko alueen kameraväli: jos kerros luotiin ennen kuin alueen kameran oikea reuna asetettiin (pitkä kenttä, esim. Kaivos),
        // sen maxX jäi liian pieneksi ja kerros katosi yhtäkkiä kesken kentän. Haetaan alue, jonka kameran vasen reuna on kerroksen aloituskohta.
        foreach (var a in FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a != null && Mathf.Abs(a.camMinX - startCamX) < 0.01f && a.camMaxX + 2f > maxX) { maxX = a.camMaxX + 2f; break; }
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || sr.sprite == null) return;
        float cx = cam.transform.position.x;
        bool show = cx >= minX - 1f && cx <= maxX + 1f;
        sr.enabled = show;
        if (copies != null) foreach (var c in copies) if (c != null) c.enabled = show;
        if (!show) return;
        float halfScreen = cam.orthographicSize * cam.aspect;
        float halfW = sr.bounds.extents.x;                                   // ensimmäisen kuvan puolikas (juuri on sen keskellä)
        float totalW = totalLocalW * Mathf.Abs(transform.lossyScale.x);
        // vasen reuna ruudun vasempaan reunaan aloituskohdassa; sitten liukuu hitaasti vasemmalle
        float shift = (cx - startCamX) * factor;
        shift = Mathf.Clamp(shift, 0f, Mathf.Max(0f, totalW - 2f * halfScreen));   // kuva ei lopu kesken
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
