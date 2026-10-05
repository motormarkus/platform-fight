using UnityEngine;

/// <summary>
/// Laivan kannen yö: häivytys päivästä kuunvaloon (esim. tanssin ajaksi). Meren kerrokset vaihtavat yökuviinsa
/// (ScrollingLayer.nightSprite), kansi ja turistit sävytetään yönsiniseksi.
/// </summary>
public class ShipNight : MonoBehaviour
{
    public static float Mix { get; private set; }
    static float target, fadeTime = 10f;

    [Tooltip("Kannen ja hahmojen sävy yöllä.")]
    public Color nightTint = new Color(0.45f, 0.5f, 0.78f, 1f);
    public SpriteRenderer[] tinted;

    /// Yö tulee (true) tai päivä palaa (false) annetussa ajassa.
    public static void SetNight(bool night, float seconds)
    {
        target = night ? 1f : 0f; fadeTime = Mathf.Max(0.1f, seconds);
    }

    void OnEnable() { Mix = 0f; target = 0f; }

    void Update()
    {
        Mix = Mathf.MoveTowards(Mix, target, Time.deltaTime / fadeTime);
        Color c = Color.Lerp(Color.white, nightTint, Mix);
        if (tinted != null)
            foreach (var r in tinted)
                if (r != null) r.color = new Color(c.r, c.g, c.b, r.color.a);
    }
}
