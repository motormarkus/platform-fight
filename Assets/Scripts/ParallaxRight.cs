using UnityEngine;

/// <summary>
/// Oikeaan reunaan ankkuroitu parallaksikerros, joka jatkuu vasemmalle peilikopioina (kuva, peilikuva, kuva...: sauma täsmää).
/// Kun kamera on alueen oikeassa päässä (endCamX), kuva on ruudulla täsmälleen kuten alkuperäinen sommittelu (oikea reuna ruudun oikeassa reunassa);
/// kun kamera siirtyy vasemmalle, kerros liukuu hitaasti ja jatkuu peilikopioina. factor: 0 = paikallaan ruudulla, 1 = maailmassa kiinni (kuten ParallaxLayer).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ParallaxRight : MonoBehaviour
{
    [Range(0f, 1f)] public float factor = 0.2f;
    [Tooltip("Kameran x alueen oikeassa päässä (Area.camMaxX): tässä kerros on kuten kuvassa.")] public float endCamX;
    [Tooltip("Peilikopioiden määrä vasemmalle (0 = ei, esim. kuu).")] public int mirrorCopies = 6;
    [Tooltip("Näkyy vain, kun kameran x on välillä rangeMin..rangeMax (alueen sisällä); muuten piilossa (kerros ei vuoda muihin alueisiin).")]
    public bool limitRange; public float rangeMin, rangeMax;
    SpriteRenderer sr; SpriteRenderer[] items; float w;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;
        w = sr.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
        items = new SpriteRenderer[1 + Mathf.Max(0, mirrorCopies)];
        items[0] = sr;
        for (int i = 1; i < items.Length; i++)
        {
            var go = new GameObject(name + " peili " + i); go.transform.SetParent(transform.parent, false); go.transform.localScale = transform.localScale;
            var c = go.AddComponent<SpriteRenderer>();
            c.sprite = sr.sprite; c.color = sr.color; c.sharedMaterial = sr.sharedMaterial; c.sortingLayerID = sr.sortingLayerID; c.sortingOrder = sr.sortingOrder;
            c.flipX = (i % 2 == 1) ? !sr.flipX : sr.flipX;
            items[i] = c;
        }
    }

    void OnDestroy() { if (items != null) for (int i = 1; i < items.Length; i++) if (items[i] != null) Destroy(items[i].gameObject); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || items == null || w < 0.01f) return;
        float x = cam.transform.position.x;
        bool show = !limitRange || (x >= rangeMin && x <= rangeMax);
        for (int i = 0; i < items.Length; i++) if (items[i] != null) items[i].enabled = show;
        if (!show) return;
        float half = cam.orthographicSize * cam.aspect * Mathf.Max(1f, CameraFollow.ZoomMul);
        float center = x + (half - w * 0.5f) + factor * (endCamX - x);
        float y = transform.position.y, z = transform.position.z;
        for (int i = 0; i < items.Length; i++) if (items[i] != null) items[i].transform.position = new Vector3(center - i * w, y, z);
    }
}
