using UnityEngine;

/// <summary>Joulumuori afterski-baarissa: seisoo rauhassa, kun tappelu alkaa kävelee esiintymislavan eteen (target) ja tanssii. Tanssi soitetaan edestakaisin (ei sauman paikkaa).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class MuoriDancer : MonoBehaviour
{
    public SalsaBrawl brawl;
    public Sprite idle;
    public Sprite[] walk, dance;
    public float frameTime = 0.1f, walkFrameTime = 0.067f, walkSpeed = 2.2f;
    public float danceScale = 1.65f, walkScale = 0.78f;
    public Vector2 target;
    SpriteRenderer sr;
    float clock;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        if (dance == null || dance.Length < 2 || brawl == null || !brawl.Started)
        {
            if (idle != null) sr.sprite = idle;
            transform.localScale = Vector3.one * danceScale; sr.flipX = false;
            return;
        }
        clock += Time.deltaTime;
        Vector2 p = transform.position, d = target - p;
        if (d.magnitude > 0.08f && walk != null && walk.Length > 0)
        {
            Vector2 step = Vector2.MoveTowards(p, target, walkSpeed * Time.deltaTime);
            transform.position = new Vector3(step.x, step.y, 0f);
            if (Mathf.Abs(d.x) > 0.05f) sr.flipX = d.x < 0f;
            sr.sprite = walk[(int)(clock / walkFrameTime) % walk.Length];
            transform.localScale = Vector3.one * walkScale;
            return;
        }
        if (target != Vector2.zero && d.magnitude <= 0.08f && transform.localScale.x != danceScale) clock = 0f;
        transform.localScale = Vector3.one * danceScale; sr.flipX = false;
        int n = dance.Length, i = (int)(clock / frameTime) % (2 * n - 2);
        sr.sprite = dance[i < n ? i : 2 * n - 2 - i];
    }
}
