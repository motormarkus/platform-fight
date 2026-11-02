using UnityEngine;

/// <summary>Pystyparallaksi hissin ikkunoiden taakse: pitkä kuva (alhaalla vuorimaisema ja taivas, ylhäällä avaruus) vierii alaspäin, kun alue on käytössä. Kuva on kameran lapsi (seuraa kameraa sivusuunnassa),
/// ja progress (0–1) määrää, kuinka pitkälle nousu on edennyt: autoRun kasvattaa sitä itsestään (duration sekuntia), tai taistelu voi asettaa sen. Progress nollautuu, kun alueelle tullaan.</summary>
public class VerticalSkyRide : MonoBehaviour
{
    public Area area;
    public Sprite sky;
    public float duration = 45f;
    public bool autoRun = true;
    [Range(0f, 1f)] public float progress;
    public int sortingOrder = -10150;
    [Header("Kuun pinta (kuva ylösalaisin, katsotaan alta): roikkuu ylhäältä taivaskuvan edessä nousun lopussa")]
    public Sprite moon;
    [Range(0f, 1f)] public float moonStart = 0.7f;
    [Tooltip("Kuun alareunan paikka lopussa: osuus ruudun korkeudesta alareunasta.")]
    public float moonBottomFrac = 0.45f;
    public float moonStartScale = 0.55f;

    SpriteRenderer sr, moonSr; bool wasOn;

    void LateUpdate()
    {
        bool on = area != null && sky != null && Area.Current == area;
        if (!on) { if (sr != null) sr.enabled = false; if (moonSr != null) moonSr.enabled = false; wasOn = false; return; }
        var cam = Camera.main; if (cam == null) return;
        if (!wasOn) { progress = 0f; wasOn = true; }
        if (sr == null || sr.transform.parent != cam.transform)
        {
            if (sr != null) Destroy(sr.gameObject);
            var go = new GameObject("Hissin taivas"); go.transform.SetParent(cam.transform, false); go.transform.localPosition = new Vector3(0f, 0f, 10f);
            sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sky; sr.sortingOrder = sortingOrder;
        }
        sr.enabled = true;
        if (moon != null && (moonSr == null || moonSr.transform.parent != cam.transform))
        {
            if (moonSr != null) Destroy(moonSr.gameObject);
            var mg = new GameObject("Kuun pinta"); mg.transform.SetParent(cam.transform, false);
            moonSr = mg.AddComponent<SpriteRenderer>(); moonSr.sprite = moon; moonSr.sortingOrder = sortingOrder + 10;
        }
        if (autoRun && duration > 0f) progress = Mathf.Min(1f, progress + Time.deltaTime / duration);
        float h = cam.orthographicSize * 2f, w = h * cam.aspect;
        float s = Mathf.Max(1f, w / sky.bounds.size.x), sh = sky.bounds.size.y * s;
        sr.transform.localScale = new Vector3(s, s, 1f);
        float yBottom = -h * 0.5f + sh * 0.5f, yTop = h * 0.5f - sh * 0.5f;   // alku: kuvan alareuna ruudun alareunassa, loppu: yläreuna ylhäällä
        sr.transform.localPosition = new Vector3(0f, Mathf.Lerp(yBottom, yTop, progress), 10f);
        if (moonSr != null)
        {
            float t = Mathf.InverseLerp(moonStart, 1f, progress), e = 1f - (1f - t) * (1f - t);
            moonSr.enabled = t > 0f;
            float fin = Mathf.Max(1f, w / moon.bounds.size.x), sc = Mathf.Max(Mathf.Lerp(moonStartScale, 1f, e), 1.03f) * fin, mh = moon.bounds.size.y * sc;
            float bottomEnd = -h * 0.5f + moonBottomFrac * h, bottomStart = h * 0.5f + mh;   // alku: kuun alareuna juuri ruudun yläreunan yläpuolella
            moonSr.transform.localScale = new Vector3(sc, sc, 1f);
            moonSr.transform.localPosition = new Vector3(0f, Mathf.Lerp(bottomStart, bottomEnd, e) + mh * 0.5f, 9.5f);
        }
    }
}
