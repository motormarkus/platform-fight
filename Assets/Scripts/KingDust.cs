using UnityEngine;

/// <summary>Pölyä ja pieniä kivenmuruja putoaa katosta (kuninkaan maahanisku): hiukkaset syntyvät ruudun yläreunasta ja putoavat hajaantuen ja himmeten. Oma pehmeä pyöreä kuva luodaan koodissa.</summary>
public class KingDust : MonoBehaviour
{
    static Sprite soft;
    float vx, vy, life, age, size, spin; SpriteRenderer sr; Color col;

    public static Sprite Soft()
    {
        if (soft != null) return soft;
        const int N = 32;
        var tx = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float d = Mathf.Sqrt((x - N / 2f + 0.5f) * (x - N / 2f + 0.5f) + (y - N / 2f + 0.5f) * (y - N / 2f + 0.5f)) / (N / 2f);
            tx.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d * 0.3f)));
        }
        tx.Apply();
        soft = Sprite.Create(tx, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 32f);
        return soft;
    }

    /// <param name="count">hiukkasten määrä</param>
    public static void Rain(int count = 90, float duration = 2.6f)
    {
        var cam = Camera.main;
        if (cam == null) return;
        float half = cam.orthographicSize, w = half * cam.aspect * 2f;
        Vector3 c = cam.transform.position;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Pöly");
            var d = go.AddComponent<KingDust>();
            d.sr = go.AddComponent<SpriteRenderer>(); d.sr.sprite = Soft(); d.sr.sortingOrder = 4000;
            go.transform.position = new Vector3(c.x + (Random.value - 0.5f) * w * 1.05f, c.y + half + Random.value * 1.2f, 0f);
            d.size = Random.Range(0.08f, 0.38f);
            go.transform.localScale = Vector3.one * d.size;
            d.vx = Random.Range(-0.5f, 0.5f); d.vy = -Random.Range(2.5f, 8.5f);
            d.life = Random.Range(0.55f, 1f) * duration; d.spin = Random.Range(-90f, 90f);
            float g = Random.Range(0.45f, 0.62f); d.col = new Color(g + 0.06f, g, g - 0.08f, Random.Range(0.35f, 0.7f));
            d.sr.color = new Color(d.col.r, d.col.g, d.col.b, 0f);
            d.age = -Random.value * 0.35f;   // putoaminen alkaa porrastetusti
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age < 0f) return;
        transform.position += new Vector3(vx, vy, 0f) * Time.deltaTime;
        vy = Mathf.Max(vy - 3f * Time.deltaTime, -9f);   // lähes tasainen putoaminen
        transform.Rotate(0f, 0f, spin * Time.deltaTime);
        transform.localScale = Vector3.one * size * (1f + age * 0.5f);   // leviää pölypilveksi
        float u = age / life;
        float a = u < 0.12f ? u / 0.12f : Mathf.Clamp01((1f - u) / 0.6f);
        sr.color = new Color(col.r, col.g, col.b, col.a * a);
        if (age >= life) Destroy(gameObject);
    }
}
