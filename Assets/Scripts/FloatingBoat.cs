using UnityEngine;

/// <summary>
/// Veteen kellumaan jäävä esine (vesitaso rannalla): keinuu hitaasti ylös–alas ja kallistuu aaltojen mukana. Vesi pysyy paikallaan: läpikuultava vesikaista
/// kellukkeiden päällä (kone nousee ja laskee sen läpi) ja kaksi laajenevaa vesirengasta kellukkeiden ympärillä. Kaikki generoidaan koodilla.
/// </summary>
public sealed class FloatingBoat : MonoBehaviour
{
    public SpriteRenderer body;          // varsinainen sprite (juuri = kellukkeiden pohja)
    public float waterY;                 // veden pinnan y (maailma): kellukkeiden pohjan lepotaso
    public float bobAmp = 0.09f, bobSpeed = 0.85f, rollDeg = 1.5f, swayAmp = 0.05f;
    public float width = 8f;             // kellukkeiden leveys (yksikköä): vesirenkaat ja vesikaista
    public int bodyOrder = -10100;
    Transform band, ring1, ring2; SpriteRenderer bandSr, ring1Sr, ring2Sr;
    static Sprite bandSprite, ringSprite;
    float baseX;

    static Sprite Band()
    {
        if (bandSprite != null) return bandSprite;
        const int W = 128, H = 64;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float v = y / (float)(H - 1);                                    // 0 = alareuna, 1 = pinta
            float edge = 0.78f + 0.06f * Mathf.Sin(x * 0.35f) + 0.05f * Mathf.Sin(x * 0.9f + 1.7f);   // aaltoileva pinta
            float a = v < edge ? Mathf.Lerp(0.78f, 0.55f, v / edge) : Mathf.Clamp01(1f - (v - edge) / 0.18f) * 0.55f;
            float fx = Mathf.Min(x, W - 1 - x) / (W * 0.18f); a *= Mathf.Clamp01(fx);   // reunat häipyvät
            t.SetPixel(x, y, new Color(0.04f, 0.26f, 0.36f, a));
        }
        t.Apply(); bandSprite = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 64f); return bandSprite;
    }

    static Sprite Ring()
    {
        if (ringSprite != null) return ringSprite;
        const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f);
            float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.07f) * 0.7f * Mathf.Clamp01((1f - r) * 6f + 0.4f);   // ohut rengas
            t.SetPixel(x, y, new Color(0.85f, 0.95f, 1f, a));
        }
        t.Apply(); ringSprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 64f); return ringSprite;
    }

    void Start()
    {
        baseX = transform.position.x;
        var g = new GameObject("Vesikaista"); g.transform.SetParent(transform.parent, false);
        bandSr = g.AddComponent<SpriteRenderer>(); bandSr.sprite = Band(); bandSr.sortingOrder = bodyOrder + 2; band = g.transform;
        band.localScale = new Vector3(width * 1.15f / (128f / 64f), 0.55f / (64f / 64f) * 0.9f, 1f);
        band.position = new Vector3(baseX, waterY - 0.12f, 0f);
        for (int i = 0; i < 2; i++)
        {
            var r = new GameObject("Vesirengas " + (i + 1)); r.transform.SetParent(transform.parent, false);
            var sr = r.AddComponent<SpriteRenderer>(); sr.sprite = Ring(); sr.sortingOrder = bodyOrder - 1; sr.color = new Color(1f, 1f, 1f, 0f);
            r.transform.position = new Vector3(baseX, waterY + 0.02f, 0f);
            if (i == 0) { ring1 = r.transform; ring1Sr = sr; } else { ring2 = r.transform; ring2Sr = sr; }
        }
    }

    void OnDestroy() { foreach (var t in new[] { band, ring1, ring2 }) if (t != null) Destroy(t.gameObject); }

    void LateUpdate()
    {
        float t = Time.time;
        float bob = Mathf.Sin(t * bobSpeed) * bobAmp + Mathf.Sin(t * bobSpeed * 2.3f + 1.1f) * bobAmp * 0.25f;
        transform.position = new Vector3(baseX + Mathf.Sin(t * 0.37f) * swayAmp, waterY + bob, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * bobSpeed * 0.8f + 0.5f) * rollDeg);
        if (body != null) body.sortingOrder = bodyOrder;
        if (band != null) band.position = new Vector3(baseX, waterY - 0.12f, 0f);   // vesi pysyy paikallaan
        for (int i = 0; i < 2; i++)
        {
            float u = ((t * 0.18f) + i * 0.5f) % 1f;
            var tr = i == 0 ? ring1 : ring2; var sr = i == 0 ? ring1Sr : ring2Sr; if (tr == null) continue;
            float w = Mathf.Lerp(width * 0.85f, width * 1.45f, u), h = w * 0.16f;
            tr.localScale = new Vector3(w / (128f / 64f), h / (128f / 64f), 1f);
            sr.color = new Color(1f, 1f, 1f, Mathf.Sin(Mathf.PI * u) * 0.55f);
        }
    }
}
