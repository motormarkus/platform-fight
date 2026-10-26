using UnityEngine;

/// <summary>
/// Lumirinteen vinosti ohi liukuvat kappaleet (puut, kivet, etuparallaksin oksat): syntyvät rinteen alaoikealta, kulkevat rinteen suuntaisesti ylävasemmalle
/// (hahmo pysyy paikallaan) ja palaavat oikealle uusina (satunnainen kuva, syvyys ja koko). Kappaleet kallistetaan rinteen kulmaan (tiltDeg): latva oikealle, kohtisuoraan rinteeseen nähden.
/// Syvyys d = kuinka monta yksikköä rinteen reunan alapuolella kappaleen juuri on; mitä syvemmällä, sitä nopeampi (perspektiivi).
/// Sijainti: x = lineLeftX + s*cos(a), y = lineLeftY - s*sin(a) - d. Kappaleet luodaan Startissa.
/// </summary>
public class SlopeFlow : MonoBehaviour
{
    public Sprite[] sprites;
    public int count = 8;
    public float angleDeg = 14f, tiltDeg = 14f;
    public float speed = 10f, speedDepthGain = 0.5f;             // nopeus = speed * (1 + gain * (d - dMin) / (dMax - dMin))
    public float lineLeftX, lineLeftY, spanX = 18f, margin = 4f;  // rinteen reunan piste ruudun vasemmassa reunassa, ruudun leveys, ylimääräinen reuna
    public float dMin = 0.3f, dMax = 3f, scaleMin = 0.8f, scaleMax = 1.2f;
    public bool dynamicSort = true;          // järjestys -y*100 kuten hahmoilla (kappale menee hahmon eteen/taakse oikein); muuten sortingBase
    public int sortingBase = -9980;
    public Color tint = Color.white;

    class Item { public Transform t; public SpriteRenderer sr; public float s, d, k; }
    Item[] items;
    float total;

    void Start()
    {
        if (sprites == null || sprites.Length == 0) return;
        total = (spanX + 2f * margin) / Mathf.Cos(angleDeg * Mathf.Deg2Rad);
        items = new Item[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Kappale " + (i + 1));
            go.transform.SetParent(transform, false);
            var it = new Item { t = go.transform, sr = go.AddComponent<SpriteRenderer>() };
            it.s = (i + Random.Range(0.1f, 0.9f)) / count * total;
            Randomize(it);
            items[i] = it;
        }
    }

    void Randomize(Item it)
    {
        it.sr.sprite = sprites[Random.Range(0, sprites.Length)];
        it.d = Random.Range(dMin, dMax);
        float sc = Random.Range(scaleMin, scaleMax);
        it.t.localScale = new Vector3(sc * (Random.value < 0.5f ? -1f : 1f), sc, 1f);   // peilaus vaihtelua varten (kallistus ei muutu: peilaus on ennen kiertoa)
        it.t.rotation = Quaternion.Euler(0f, 0f, -tiltDeg);
        it.sr.color = tint;
        it.k = 1f + speedDepthGain * Mathf.InverseLerp(dMin, dMax, it.d);
    }

    void LateUpdate()
    {
        if (items == null) return;
        float a = angleDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a), dt = Time.deltaTime;
        float xl = lineLeftX - margin;
        foreach (var it in items)
        {
            it.s -= speed * it.k * dt;
            if (it.s < 0f) { it.s += total; Randomize(it); }
            float x = xl + it.s * ca, y = lineLeftY + margin * Mathf.Tan(a) - it.s * sa - it.d;   // reunan viiva jatkuu ruudun vasemman laidan yli ylemmäs
            it.t.position = new Vector3(x, y, 0f);
            it.sr.sortingOrder = dynamicSort ? Mathf.RoundToInt(-y * 100f) : sortingBase + Mathf.RoundToInt(it.d * 10f);
        }
    }
}
