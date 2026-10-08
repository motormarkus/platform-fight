using UnityEngine;

/// <summary>Pyörältä lentävä vihu: kaari ilmassa kuvasarjan läpi, osuu maahan, liukuu ja häipyy.</summary>
public class FlyingRider : MonoBehaviour
{
    public Sprite[] sprites;
    public Vector2 velocity;
    public float up = 6f, startHeight = 1.5f, frameTime = 0.07f;
    public bool flip;
    [Tooltip("Kuvan koko (vihuprätkän visualScale).")]
    public float scale = 1f;
    SpriteRenderer sr, shadow;
    float t, height, vy;
    bool landed;

    void Start()
    {
        PlayerController.SortByFrameNumber(sprites);
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        sr = v.AddComponent<SpriteRenderer>(); sr.flipX = flip;
        v.transform.localScale = new Vector3(scale, scale, 1f);
        var s = new GameObject("Shadow"); s.transform.SetParent(transform, false);
        shadow = s.AddComponent<SpriteRenderer>(); shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.4f);
        height = startHeight; vy = up;
    }

    void Update()
    {
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        p.x += velocity.x * dt;
        velocity.x = Mathf.MoveTowards(velocity.x, 0f, (landed ? 14f : 2f) * dt);
        transform.position = p;
        if (!landed)
        {
            vy -= 26f * dt; height += vy * dt;
            if (height <= 0.6f * scale) { height = 0.6f * scale; landed = true; HitFx.OnLand(); }
        }
        int n = sprites.Length;
        int f = landed ? n - 1 : Mathf.Min((int)(t / frameTime), n - 2);
        sr.sprite = sprites[f];
        // ruudun keskikohta = vartalo: sprite-pivot alareunassa, ruutu 512 px -> keskikohta 2.56 yks pivotin yläpuolella
        sr.transform.localPosition = new Vector3(0f, height - 2.56f * scale, 0f);
        int order = Mathf.RoundToInt(-p.y * 100f);
        sr.sortingOrder = order;
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(3f - t));
        shadow.sortingOrder = order - 1;
        float k = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(height / 3f));
        shadow.transform.localScale = new Vector3(2.2f * k * scale, 0.5f * k * scale, 1f);
        if (t > 3f) Destroy(gameObject);
    }
}
