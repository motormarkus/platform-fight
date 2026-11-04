using UnityEngine;

/// <summary>
/// Käyskentelevä NPC (lentoemäntä): seisoo hetken (idle-kuvat), kävelee satunnaiseen kohtaan kaistalla (kävelykuvat) ja pysähtyy taas. Ei taistelija eikä lyötävissä.
/// Kuvat katsovat oikealle; vasemmalle kävellessä kuva peilataan. Liikkuu vain, kun hero on samalla alueella.
/// </summary>
public class NpcStroller : MonoBehaviour
{
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] idleSprites, walkSprites, talkSprites, danceSprites, danceSprites2;
    [Tooltip("Todennäköisyys, että seisomisen jälkeen hän puhuu (puhekuvat kerran läpi) kävelyn sijaan.")]
    public float talkChance = 0.25f, talkFrameTime = 0.125f;
    [Tooltip("Todennäköisyys, että seisomisen jälkeen hän tanssii (tanssikuvat kerran läpi), ellei puhu.")]
    public float danceChance = 0f, danceFrameTime = 0.125f, danceFrameTime2 = 0.125f;   // toinen tanssi (valinnainen): arvotaan puoliksi ensimmäisen kanssa
    public float idleFrameTime = 0.167f, walkFrameTime = 0.06f, speed = 1.9f;   // nopeampi: hidas vauhti näytti paikallaan kävelyltä
    public float laneMinX, laneMaxX;
    public Vector2 idleSeconds = new Vector2(1.5f, 4f), walkDistance = new Vector2(5f, 14f);   // kävelee usein ja pitkään
    [Header("Tiskille nojaaminen (valinnainen): kävelee tiskille (leanMinX–leanMaxX, syvyys leanY), kääntyy selin, nojaa ja kääntyy takaisin")]
    public Sprite[] leanTurnSprites, leanIdleSprites;
    public float leanChance = 0f, leanTurnFrameTime = 0.067f, leanIdleFrameTime = 0.05f, leanMinX, leanMaxX, leanY;
    public Vector2 leanSeconds = new Vector2(6f, 12f);
    int leanPhase;   // 0 ei, 1 kävelee tiskille, 2 kääntyy, 3 nojaa, 4 kääntyy takaisin
    bool returning; float homeY, leanUntil;
    bool walking, talking, faceRight = true;
    Sprite[] act; float actFrameTime;
    float t, until, target;
    double px;   // tarkka x (kone on x ≈ 170000: float ei erota pientä askelta, joten kävely jäi paikalleen ja kuvat värähtelivät)

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idleSprites); PlayerController.SortByFrameNumber(walkSprites); PlayerController.SortByFrameNumber(talkSprites); PlayerController.SortByFrameNumber(danceSprites); PlayerController.SortByFrameNumber(danceSprites2); PlayerController.SortByFrameNumber(leanTurnSprites); PlayerController.SortByFrameNumber(leanIdleSprites);
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
        if (leanPhase >= 2)
        {
            int n = leanTurnSprites.Length;
            if (leanPhase == 2) { int f = (int)(t / leanTurnFrameTime); if (f >= n) { leanPhase = 3; t = 0f; leanUntil = Random.Range(leanSeconds.x, leanSeconds.y); } else body.sprite = leanTurnSprites[f]; }
            else if (leanPhase == 3) { body.sprite = leanIdleSprites[(int)(t / leanIdleFrameTime) % leanIdleSprites.Length]; if (t >= leanUntil) { leanPhase = 4; t = 0f; } }
            else
            {
                int f = (int)(t / leanTurnFrameTime);
                if (f >= n)
                {
                    leanPhase = 0; returning = true; t = 0f; px = p.x;
                    float d = Random.Range(1.5f, 4f); bool right = Random.value < 0.5f; if (p.x + (right ? d : -d) > laneMaxX || p.x - d < laneMinX) right = p.x - laneMinX > laneMaxX - p.x ? false : true;
                    target = Mathf.Clamp(p.x + (right ? d : -d), laneMinX, laneMaxX); faceRight = target >= p.x; walking = true;
                }
                else body.sprite = leanTurnSprites[n - 1 - f];
            }
        }
        else if (talking)
        {
            int f = (int)(t / actFrameTime);
            if (f >= act.Length) { talking = false; t = 0f; until = Random.Range(idleSeconds.x, idleSeconds.y); }
            else body.sprite = act[f];
        }
        else if (!walking)
        {
            body.sprite = idleSprites[(int)(t / idleFrameTime) % idleSprites.Length];
            if (t >= until && leanChance > 0f && leanTurnSprites != null && leanTurnSprites.Length > 0 && leanIdleSprites != null && leanIdleSprites.Length > 0 && walkSprites != null && walkSprites.Length > 0 && Random.value < leanChance)
            { leanPhase = 1; homeY = p.y; target = Random.Range(leanMinX, leanMaxX); faceRight = target > p.x; walking = true; t = 0f; px = p.x; }
            else if (t >= until && talkSprites != null && talkSprites.Length > 0 && Random.value < talkChance) { talking = true; t = 0f; act = talkSprites; actFrameTime = talkFrameTime; }
            else if (t >= until && danceSprites != null && danceSprites.Length > 0 && Random.value < danceChance) { talking = true; t = 0f; bool second = danceSprites2 != null && danceSprites2.Length > 0 && Random.value < 0.5f; act = second ? danceSprites2 : danceSprites; actFrameTime = second ? danceFrameTime2 : danceFrameTime; }
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
            if (leanPhase == 1) p.y = Mathf.MoveTowards(p.y, leanY, speed * 0.6f * Time.deltaTime);
            else if (returning) p.y = Mathf.MoveTowards(p.y, homeY, speed * 0.6f * Time.deltaTime);
            if (System.Math.Abs(px - target) < 0.01)
            {
                if (leanPhase == 1) { if (Mathf.Abs(p.y - leanY) < 0.02f) { walking = false; leanPhase = 2; t = 0f; } }
                else if (!returning || Mathf.Abs(p.y - homeY) < 0.02f) { walking = false; returning = false; t = 0f; until = Random.Range(idleSeconds.x, idleSeconds.y); }
            }
        }
        p.x = Mathf.Clamp(p.x, laneMinX, laneMaxX);
        transform.position = p;
        body.flipX = !faceRight;
        body.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }
}
