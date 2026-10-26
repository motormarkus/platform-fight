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
    public float talkChance = 0.4f, talkFrameTime = 0.09f;
    public float idleFrameTime = 0.1f, walkFrameTime = 0.045f, speed = 1.3f;
    public float laneMinX, laneMaxX;
    public Vector2 idleSeconds = new Vector2(3f, 8f), walkDistance = new Vector2(3f, 10f);
    bool walking, talking, faceRight = true;
    float t, until, target;

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
                walking = true; t = 0f;
            }
        }
        else
        {
            body.sprite = walkSprites[(int)(t / walkFrameTime) % walkSprites.Length];
            float step = speed * Time.deltaTime;
            p.x = Mathf.MoveTowards(p.x, target, step);
            if (Mathf.Abs(p.x - target) < 0.01f) { walking = false; t = 0f; until = Random.Range(idleSeconds.x, idleSeconds.y); }
        }
        p.x = Mathf.Clamp(p.x, laneMinX, laneMaxX);
        transform.position = p;
        body.flipX = !faceRight;
        body.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }
}
