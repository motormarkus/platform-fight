using UnityEngine;

/// <summary>
/// Osumaläiskä: lyhyt tähtimäinen välähdys osumakohdassa. Kasvaa ja häipyy reaaliajassa,
/// joten se näkyy myös osumapysäytyksen (hitstop) aikana.
/// </summary>
public class HitSpark : MonoBehaviour
{
    static Sprite sprite;
    SpriteRenderer sr;
    float t, life, size;
    Color color;

    /// Luo välähdyksen kohtaan pos. heavy = isompi ja pidempi, blocked = sinertävä (torjuttu isku).
    public static void Spawn(Vector3 pos, bool heavy, int sortingOrder, bool blocked = false)
    {
        var go = new GameObject("Osumaläiskä");
        go.transform.position = pos;
        var s = go.AddComponent<HitSpark>();
        s.sr = go.AddComponent<SpriteRenderer>();
        s.sr.sprite = GetSprite();
        s.sr.sortingOrder = sortingOrder;
        s.life = heavy ? 0.2f : 0.13f;
        s.size = heavy ? 1.6f : 1.0f;
        s.color = blocked ? new Color(0.7f, 0.85f, 1f) : new Color(1f, 0.95f, 0.75f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 45f));
        s.Apply();
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (t >= life) { Destroy(gameObject); return; }
        Apply();
    }

    void Apply()
    {
        float k = t / life;
        // nopea kasvu alussa, sitten häivytys
        float scale = size * Mathf.Lerp(0.5f, 1.1f, Mathf.Sqrt(k));
        transform.localScale = new Vector3(scale, scale, 1f);
        sr.color = new Color(color.r, color.g, color.b, 1f - k * k);
    }

    /// Tähtikuvio koodilla: kahdeksan sädettä ja kirkas keskusta.
    static Sprite GetSprite()
    {
        if (sprite != null) return sprite;
        const int n = 96;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                // säteet: pitkät ja lyhyet vuorotellen
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 4f)), 18f);
                float rayLen = Mathf.Abs(Mathf.Cos(ang * 2f)) > 0.7f ? 1f : 0.7f;
                float rays = ray * Mathf.Clamp01(1f - r / rayLen);
                float core = Mathf.Clamp01(1f - r / 0.35f);
                float a = Mathf.Clamp01(rays * 1.4f + core * core * 1.2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return sprite;
    }
}
