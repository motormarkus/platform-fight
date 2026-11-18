using UnityEngine;

/// <summary>
/// Aluksen ikkunoiden takana lentävät tähdet: avaruuskuva vierii jatkuvasti vasemmalle (alus lentää oikealle) ja toistuu saumattomasti (peilattu kopio joka toinen).
/// Pysyy kameran mukana (ei riipu hahmon liikkeestä). Boost-kerroin: ShipFinale nopeuttaa syöksyssä.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class StarDrift : MonoBehaviour
{
    public float speed = 5f;                 // yksikköä sekunnissa
    public float minX, maxX;                 // kameran x-väli, jolla näkyy (alueen kamera)
    public static float Boost = 1f;
    SpriteRenderer sr; SpriteRenderer[] copies; float tile, phase;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;
        tile = sr.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
        copies = new SpriteRenderer[4];
        copies[0] = sr;
        for (int i = 1; i < 4; i++)
        {
            var go = new GameObject("Tähdet " + (i + 1)); go.transform.SetParent(transform.parent, false);
            go.transform.localScale = transform.localScale;
            var c = go.AddComponent<SpriteRenderer>();
            c.sprite = sr.sprite; c.color = sr.color; c.sharedMaterial = sr.sharedMaterial; c.sortingLayerID = sr.sortingLayerID; c.sortingOrder = sr.sortingOrder;
            copies[i] = c;
        }
        for (int i = 0; i < 4; i++) copies[i].flipX = (i % 2 == 1) ? !sr.flipX : sr.flipX;   // joka toinen peilattu: sauma täsmää
    }

    void OnDestroy() { if (copies != null) for (int i = 1; i < copies.Length; i++) if (copies[i] != null) Destroy(copies[i].gameObject); }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || copies == null || tile < 0.01f) return;
        float cx = cam.transform.position.x;
        bool show = cx >= minX - 1f && cx <= maxX + 1f;
        foreach (var c in copies) if (c != null) c.enabled = show;
        if (!show) return;
        phase = (phase + speed * Boost * Time.deltaTime) % (2f * tile);
        float left = cx - cam.orthographicSize * cam.aspect * Mathf.Max(1f, CameraFollow.ZoomMul);
        float y = transform.position.y; float z = transform.position.z;
        for (int i = 0; i < 4; i++)
            if (copies[i] != null) copies[i].transform.position = new Vector3(left - phase + i * tile + tile * 0.5f, y, z);
    }
}
