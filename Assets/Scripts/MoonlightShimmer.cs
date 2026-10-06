using UnityEngine;

/// <summary>Kuunvalon voimakkuus vaihtelee hillitysti (ohuet pilvet ja laivan keinunta).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MoonlightShimmer : MonoBehaviour
{
    [Range(0f, 1f)] public float baseAlpha = 0.55f;
    [Range(0f, 1f)] public float variation = 0.25f;   // osuus perusvoimakkuudesta
    public float speed = 0.12f;
    SpriteRenderer sr; float seed;

    void Awake() { sr = GetComponent<SpriteRenderer>(); seed = Random.value * 100f; }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;          // hidas pilvien vaihtelu
        float w = Mathf.Sin(Time.time * 0.9f) * 0.25f;                           // keinunta
        float a = baseAlpha * (1f + variation * Mathf.Clamp(n + w * 0.5f, -1f, 1f));
        var c = sr.color; c.a = Mathf.Clamp01(a); sr.color = c;
    }
}
