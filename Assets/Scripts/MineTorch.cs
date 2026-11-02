using UnityEngine;

/// <summary>Seinäsoihtu: kuva jaetaan pitelimeen ja liekkiin. Liekki piirretään ohuina vaakaviipaleina, joita heilutetaan sivulle (sitä enemmän, mitä ylempänä) ja venytetään,
/// joten liekki väreilee. Lisäksi kaksi pehmeää hehkua lepattaa. Komponentti rakentaa lapsiobjektit itse; sijoita se parallaksikerroksen lapseksi, niin soihtu liikkuu kerroksen mukana.
/// Alkupiste on soihdun alareuna keskellä.</summary>
public class MineTorch : MonoBehaviour
{
    public Sprite sprite;
    [Tooltip("Liekkiosan osuus kuvan korkeudesta ylhäältä (0–1).")]
    public float flameFraction = 0.40f;
    public int strips = 16;
    [Tooltip("Liekin sivuttaisheilunta kärjessä, yksikköinä.")]
    public float sway = 0.09f;
    public float glowRadius = 6.5f;
    public int sortingOrder = -10099;
    public Color glowColor = new Color(1f, 0.55f, 0.2f, 1f);

    SpriteRenderer[] flame; SpriteRenderer glowBig, glowSmall;
    float unitH, flameBase, seed; static Sprite glowSprite;

    void Start()
    {
        if (sprite == null || sprite.texture == null) return;
        seed = Random.value * 100f;
        float ppu = sprite.pixelsPerUnit; var rect = sprite.rect;
        float flamePx = Mathf.Round(rect.height * flameFraction), holderPx = rect.height - flamePx;
        float stripPx = flamePx / strips; unitH = stripPx / ppu; flameBase = holderPx / ppu;

        var holder = NewRenderer("Pidin", Sprite.Create(sprite.texture, new Rect(rect.x, rect.y, rect.width, holderPx), new Vector2(0.5f, 0f), ppu), 0);
        holder.transform.localPosition = Vector3.zero;

        flame = new SpriteRenderer[strips];
        for (int i = 0; i < strips; i++)   // i = 0 on liekin kärki
        {
            var r = new Rect(rect.x, rect.y + holderPx + (strips - 1 - i) * stripPx, rect.width, stripPx);
            flame[i] = NewRenderer("Liekki " + i, Sprite.Create(sprite.texture, r, new Vector2(0.5f, 0f), ppu), 1);
        }

        if (glowSprite == null) glowSprite = MakeGlow();
        float cy = flameBase + flamePx / ppu * 0.45f;
        glowBig = NewRenderer("Hehku", glowSprite, 2); glowBig.transform.localPosition = new Vector3(0f, cy, 0f);
        glowSmall = NewRenderer("Hehku sisä", glowSprite, 3); glowSmall.transform.localPosition = new Vector3(0f, cy, 0f);
    }

    SpriteRenderer NewRenderer(string n, Sprite sp, int orderOffset)
    {
        var go = new GameObject(n); go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = sortingOrder + orderOffset;
        return sr;
    }

    void LateUpdate()
    {
        if (flame == null) return;
        float t = Time.time + seed;
        float fy = 1f + 0.06f * Mathf.Sin(t * 7.1f) + 0.04f * Mathf.Sin(t * 13.7f + 1.3f);   // liekin korkeus lepattaa
        for (int i = 0; i < strips; i++)
        {
            float up = 1f - (float)i / (strips - 1);            // i = 0 (kärki) -> 1
            float dx = sway * up * up * (0.6f * Mathf.Sin(t * 5.3f + i * 0.55f) + 0.4f * Mathf.Sin(t * 9.1f + i * 1.1f + 2f));
            var tr = flame[i].transform;
            tr.localPosition = new Vector3(dx, flameBase + (strips - 1 - i) * unitH * fy, 0f);
            tr.localScale = new Vector3(1f + 0.03f * up * Mathf.Sin(t * 8.3f + i), fy * 1.04f, 1f);   // pieni limitys, ettei viipaleiden väliin jää rakoa
        }
        float flick = 0.82f + 0.12f * Mathf.Sin(t * 11f) + 0.08f * Mathf.Sin(t * 23f + 1f) + 0.05f * Mathf.Sin(t * 5.3f + 2f);
        var c = glowColor; c.a = 0.22f * flick;
        glowBig.color = c; glowBig.transform.localScale = Vector3.one * glowRadius * (0.95f + 0.07f * flick);
        var c2 = Color.Lerp(glowColor, new Color(1f, 0.9f, 0.55f), 0.6f); c2.a = 0.38f * flick;
        glowSmall.color = c2; glowSmall.transform.localScale = Vector3.one * glowRadius * 0.33f * (0.95f + 0.07f * flick);
    }

    static Sprite MakeGlow()
    {
        const int n = 64;
        var tx = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                tx.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 yksikön levyinen; skaalataan säteellä
    }
}
