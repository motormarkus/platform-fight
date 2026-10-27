using UnityEngine;

/// <summary>
/// Käyskentelevä NPC (lentoemäntä): seisoo hetken (idle-kuvat), kävelee satunnaiseen kohtaan kaistalla (kävelykuvat) ja pysähtyy taas. Ei taistelija eikä lyötävissä.
/// Kuvat katsovat oikealle; vasemmalle kävellessä kuva peilataan. Liikkuu vain, kun hero on samalla alueella.
/// </summary>
public class NpcStroller : MonoBehaviour
{
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] idleSprites, walkSprites, talkSprites;
    [Tooltip("Todennäköisyys, että seisomisen jälkeen hän puhuu (puhekuvat kerran läpi) kävelyn sijaan.")]
    public float talkChance = 0.25f, talkFrameTime = 0.125f;
    public float idleFrameTime = 0.167f, walkFrameTime = 0.06f, speed = 1.9f;   // nopeampi: hidas vauhti näytti paikallaan kävelyltä
    public float laneMinX, laneMaxX;
    public Vector2 idleSeconds = new Vector2(1.5f, 4f), walkDistance = new Vector2(5f, 14f);   // kävelee usein ja pitkään
    bool walking, talking, faceRight = true;
    float t, until, target;
    double px;   // tarkka x (kone on x ≈ 170000: float ei erota pientä askelta, joten kävely jäi paikalleen ja kuvat värähtelivät)

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idleSprites); PlayerController.SortByFrameNumber(walkSprites); PlayerController.SortByFrameNumber(talkSprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        until = Random.Range(idleSeconds.x, idleSeconds.y);
        t = Random.value * 3f;
    }

    void Update()
    {
        if (body == null || idleSprites == null || idleSprites.Length == 0) return;
        if (area != null && Area.Current != area) return;
        t += Time.deltaTime;
        Vector3 p = transform.position;
        if (talking)
        {
            int f = (int)(t / talkFrameTime);
            if (f >= talkSprites.Length) { talking = false; t = 0f; until = Random.Range(idleSeconds.x, idleSeconds.y); }
            else body.sprite = talkSprites[f];
        }
        else if (!walking)
        {
            body.sprite = idleSprites[(int)(t / idleFrameTime) % idleSprites.Length];
            if (t >= until && talkSprites != null && talkSprites.Length > 0 && Random.value < talkChance) { talking = true; t = 0f; }
            else if (t >= until && walkSprites != null && walkSprites.Length > 0)
            {
                float left = p.x - laneMinX, right = laneMaxX - p.x;
                bool goLeft = (left > right) == (Random.value < 0.75f);
                float d = Mathf.Min(Random.Range(walkDistance.x, walkDistance.y), goLeft ? left : right);
                if (d < 1f) { goLeft = !goLeft; d = Mathf.Min(Random.Range(walkDistance.x, walkDistance.y), goLeft ? left : right); }
                faceRight = !goLeft; target = p.x + (goLeft ? -d : d);
                walking = true; t = 0f; px = p.x;
            }
        }
        else
        {
            body.sprite = walkSprites[(int)(t / walkFrameTime) % walkSprites.Length];
            float step = speed * Time.deltaTime;
            double stepD = speed * (double)Time.deltaTime, diff = target - px;
            px = System.Math.Abs(diff) <= stepD ? target : px + System.Math.Sign(diff) * stepD;
            p.x = (float)px;
            if (System.Math.Abs(px - target) < 0.01) { walking = false; t = 0f; until = Random.Range(idleSeconds.x, idleSeconds.y); }
        }
        p.x = Mathf.Clamp(p.x, laneMinX, laneMaxX);
        transform.position = p;
        body.flipX = !faceRight;
        body.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }
}
