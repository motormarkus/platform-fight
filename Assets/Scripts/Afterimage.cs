using UnityEngine;

/// <summary>Haamukuva: kopio hahmon kuvasta, joka haalistuu (nopeiden erikoisliikkeiden jälki, latauksen hehku).</summary>
public class Afterimage : MonoBehaviour
{
    SpriteRenderer sr;
    float life, t, a0;
    Color tint;

    /// Kopio lähteen nykyisestä kuvasta samaan kohtaan. scale > 1 kasvattaa kuvaa pivotin (jalkojen) ympäri.
    public static void Spawn(SpriteRenderer src, Color tint, float life, float alpha, float scale = 1f, int orderOffset = -1)
    {
        if (src == null || src.sprite == null) return;
        var go = new GameObject("Haamukuva");
        go.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
        go.transform.localScale = src.transform.lossyScale * scale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = src.sprite; r.flipX = src.flipX; r.flipY = src.flipY;
        r.sortingLayerID = src.sortingLayerID; r.sortingOrder = src.sortingOrder + orderOffset;
        var a = go.AddComponent<Afterimage>();
        a.sr = r; a.life = Mathf.Max(0.01f, life); a.a0 = alpha; a.tint = tint;
        r.color = new Color(tint.r, tint.g, tint.b, alpha);
    }

    void Update()
    {
        t += Time.deltaTime;
        if (sr != null) sr.color = new Color(tint.r, tint.g, tint.b, a0 * Mathf.Clamp01(1f - t / life));
        if (t >= life) Destroy(gameObject);
    }
}
