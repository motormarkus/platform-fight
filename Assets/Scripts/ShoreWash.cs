using UnityEngine;

/// <summary>
/// Rannan aaltojen huuhtelu: luo koodilla vaahtokaistoja (kohinasta generoitu pitsimäinen vaahtotekstuuri), jotka nousevat hiekalle ja vetäytyvät takaisin
/// eri tahtiin (kaksi kerrosta), himmeten ylös mennessä. Ei vaadi spritejä. Leveys = alueen kävelyväli.
/// </summary>
public sealed class ShoreWash : MonoBehaviour
{
    public float x0, x1;                  // alueen vaakaväli (maailma)
    public float waterY;                  // vedenrajan y (hiekan yläreuna)
    public float advance = 0.9f;          // kuinka pitkälle (yksikköä) aalto nousee hiekalle
    public float height = 0.7f;           // vaahtokaistan korkeus
    public int sortingOrder = -9980;
    Transform[] bands; SpriteRenderer[] rends; float[] period = { 7.0f, 9.5f }, offset = { 0f, 3.1f };
    static Sprite foamSprite;

    static Sprite Foam(float h)
    {
        if (foamSprite != null) return foamSprite;
        const int W = 1024, H = 96;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        float[] ph = new float[8]; var rng = new System.Random(7); for (int k = 0; k < 8; k++) ph[k] = (float)rng.NextDouble() * 6.283f;
        float Edge(float u) { float e = 0f; for (int k = 0; k < 4; k++) e += Mathf.Sin(u * 6.283f * (2 + 3 * k) + ph[k]) / (1 + k); return e / 2.2f; }   // jaksollinen epätasainen reuna
        for (int x = 0; x < W; x++)
        {
            float u = x / (float)W; float e = 0.55f + 0.25f * Edge(u);
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);   // 0 = hiekan puoli (etureuna), 1 = vesipuoli
                float lace = Mathf.Sin(u * 6.283f * 37f + v * 9f + ph[4]) * Mathf.Sin(u * 6.283f * 23f - v * 6f + ph[5]);   // pitsimäiset aukot
                float a = Mathf.SmoothStep(0f, 1f, (v - (e - 0.35f)) / 0.45f);                 // vaahdon etureuna epätasaisena
                a *= 1f - 0.45f * Mathf.Clamp01(lace * 0.5f + 0.35f) * Mathf.SmoothStep(0f, 0.7f, 1f - v);   // reuna rikkinäisempi
                a *= 1f - Mathf.SmoothStep(0.78f, 1f, v);                                    // vesipuoli häipyy
                tex.SetPixel(x, y, new Color(0.93f, 0.97f, 1f, Mathf.Clamp01(a) * 0.85f));
            }
        }
        tex.Apply();
        foamSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), H / h, 0, SpriteMeshType.FullRect);   // luonnollinen korkeus = h yksikköä
        return foamSprite;
    }

    void Start()
    {
        int n = period.Length; bands = new Transform[n]; rends = new SpriteRenderer[n];
        float w = Mathf.Max(1f, x1 - x0); var sp = Foam(height);
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Rantavaahto " + (i + 1)); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.drawMode = SpriteDrawMode.Tiled; sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(w, height); go.transform.localScale = new Vector3(1f, i == 0 ? 1f : 0.8f, 1f); sr.sortingOrder = sortingOrder + i; sr.color = new Color(1f, 1f, 1f, 0f);
            go.transform.position = new Vector3((x0 + x1) * 0.5f, waterY, 0f);
            bands[i] = go.transform; rends[i] = sr;
        }
    }

    void LateUpdate()
    {
        if (bands == null) return;
        for (int i = 0; i < bands.Length; i++)
        {
            float u = ((Time.time + offset[i]) % period[i]) / period[i];            // 0..1 aalto
            float adv = u < 0.45f ? Mathf.SmoothStep(0f, 1f, u / 0.45f) : Mathf.SmoothStep(1f, 0f, (u - 0.45f) / 0.55f) * 0.9f + 0.1f * (1f - u) / 0.55f;
            float a = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 1.05f)) * 0.8f;            // läpikuultava nousussa, himmenee vetäytyessä
            var p = bands[i].position; p.y = waterY - adv * advance * (i == 0 ? 1f : 0.75f) + 0.15f * i; bands[i].position = p;
            rends[i].color = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }
    }
}
