using UnityEngine;

/// <summary>Olion varjo vilahtaa käytävän ikkunan takana (pedon huoneessa): tumma hahmo kävelee ikkunan ohi satunnaisin välein, mieluiten kun valot vilkkuvat (LabAtmosphere.Flickered).
/// Komponentti on ikkunan parallaksikuvan lapsi, joten hahmo liikkuu kuvan mukana ja näkyy vain ikkunan läpi (käytäväkuva peittää sen muualta). Poistuu käytöstä, kun olio on vapautettu (BeastCage.BeastReleased).
/// Paikat annetaan parallaksikuvan paikalliskoordinaatteina.</summary>
public class WindowBeast : MonoBehaviour
{
    public Sprite[] walkSprites;
    public float frameTime = 0.09f;
    public float fromX, toX, footY, scale = 1.5f, walkTime = 1.7f;
    public bool facesRight = true;
    public string[] areaNames = { "Muurin käytävä", "Muurin käytävä 2" };
    public Vector2 gap = new Vector2(7f, 16f);

    SpriteRenderer sr; float nextAt, t0 = -1f; bool dirRight = true, flickerPending;

    void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -10140; sr.color = new Color(0.07f, 0.04f, 0.05f, 1f); sr.enabled = false;
        nextAt = Time.time + Random.Range(gap.x * 0.4f, gap.y * 0.6f);
    }

    void OnEnable() { LabAtmosphere.Flickered += OnFlicker; }
    void OnDisable() { LabAtmosphere.Flickered -= OnFlicker; }
    void OnFlicker() { flickerPending = true; }

    bool InArea()
    {
        var a = Area.Current; if (a == null) return false;
        foreach (var n in areaNames) if (a.areaName == n) return true;
        return false;
    }

    void Update()
    {
        if (walkSprites == null || walkSprites.Length == 0) return;
        if (BeastCage.BeastReleased || !InArea()) { sr.enabled = false; t0 = -1f; flickerPending = false; return; }
        float t = Time.time;
        if (t0 < 0f)
        {
            bool go = t >= nextAt && (flickerPending || t >= nextAt + 6f);   // mieluiten vilkunnan aikana, viimeistään 6 s myöhemmin
            if (!go) { flickerPending = false; return; }
            t0 = t; dirRight = Random.value < 0.5f; flickerPending = false;
        }
        float k = (t - t0) / walkTime;
        if (k >= 1f) { t0 = -1f; sr.enabled = false; nextAt = t + Random.Range(gap.x, gap.y); return; }
        sr.enabled = true;
        float x = Mathf.Lerp(dirRight ? fromX : toX, dirRight ? toX : fromX, k);
        transform.localPosition = new Vector3(x, footY, 0f);   // hahmojen kuvien pivot on jaloissa
        sr.sprite = walkSprites[(int)((t - t0) / frameTime) % walkSprites.Length];
        sr.flipX = dirRight != facesRight;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
