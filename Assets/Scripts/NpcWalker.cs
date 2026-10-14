using UnityEngine;

/// <summary>
/// Kävelevä NPC (John, Jade): ei taistelija eikä lyötävissä. Kuvat on leikattu yhdestä videosta ja ne jatkuvat toisiinsa:
/// seisoo (kello, käsi taskussa) → lähtee kävelemään → kävelee jonkin matkaa → pysähtyy → seisoo taas.
/// Kuvat katsovat vasemmalle; oikealle kävellessä kuva peilataan. Liikkuu vain, kun hero on samalla alueella.
/// Kuvajärjestys: stand, start, walk, stop (määrät kenttinä).
/// </summary>
public class NpcWalker : MonoBehaviour
{
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] sprites;
    public int standCount = 34, startCount = 14, walkCount = 23, stopCount = 31;
    [Tooltip("Pysähtymissarjan alkuosa (kuvia), jonka aikana vauhti hidastuu nollaan; loppuosa on seisoma-asennon palautumista.")]
    public int stopMoveCount = 17;
    public float standFrameTime = 0.1f, startFrameTime = 0.067f, walkFrameTime = 0.067f, stopFrameTime = 0.067f;
    public float speed = 1.5f;
    [Tooltip("Kävelyalue (x).")]
    public float laneMinX, laneMaxX;
    [Tooltip("Kuinka monta kertaa seisomasarja toistetaan ennen kävelyä (satunnainen väliltä).")]
    public Vector2Int standLoops = new Vector2Int(1, 1);
    [Tooltip("Kävelyn pituus (yksikköä).")]
    public Vector2 walkDistance = new Vector2(3f, 7f);

    enum S { Stand, Start, Walk, Stop }
    S state = S.Stand;
    float t, targetDist, walked;
    int standLeft, dir = -1;

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        PlayerController.SortByFrameNumber(sprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        standLeft = Random.Range(standLoops.x, standLoops.y + 1);
    }

    Sprite At(int offset, int count, float time)
    {
        if (sprites == null || count <= 0) return null;
        int i = Mathf.Min((int)(t / time), count - 1);
        return sprites[Mathf.Min(offset + i, sprites.Length - 1)];
    }

    void Update()
    {
        if (body == null || sprites == null || sprites.Length == 0) return;
        if (area != null && Area.Current != area) return;
        t += Time.deltaTime;
        Vector3 p = transform.position;
        int startOff = standCount, walkOff = standCount + startCount, stopOff = walkOff + walkCount;
        switch (state)
        {
            case S.Stand:
                body.sprite = At(0, standCount, standFrameTime);
                if (t >= standCount * standFrameTime)
                {
                    t = 0f;
                    if (--standLeft <= 0) BeginWalk();
                }
                break;
            case S.Start:
                body.sprite = At(startOff, startCount, startFrameTime);
                Move(ref p, Mathf.Clamp01(t / (startCount * startFrameTime)));
                if (t >= startCount * startFrameTime) { state = S.Walk; t = 0f; }
                break;
            case S.Walk:
            {
                float cyc = walkCount * walkFrameTime;
                body.sprite = sprites[Mathf.Min(walkOff + (int)((t % cyc) / walkFrameTime) % walkCount, sprites.Length - 1)];
                Move(ref p, 1f);
                float stopDist = speed * 0.5f * stopMoveCount * stopFrameTime;   // pysähtymiseen kuluva matka
                bool edge = (dir < 0 && p.x - stopDist <= laneMinX) || (dir > 0 && p.x + stopDist >= laneMaxX);
                if (walked + stopDist >= targetDist || edge) { state = S.Stop; t = 0f; }
                break;
            }
            case S.Stop:
                body.sprite = At(stopOff, stopCount, stopFrameTime);
                Move(ref p, 1f - Mathf.Clamp01(t / (stopMoveCount * stopFrameTime)));
                if (t >= stopCount * stopFrameTime)
                {
                    state = S.Stand; t = 0f;
                    standLeft = Random.Range(standLoops.x, standLoops.y + 1);
                }
                break;
        }
        p.x = Mathf.Clamp(p.x, laneMinX, laneMaxX);
        transform.position = p;
        body.flipX = dir > 0;
        body.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }

    void BeginWalk()
    {
        // suunta: kohti isompaa tilaa, joskus toiseen suuntaan
        float x = transform.position.x;
        float left = x - laneMinX, right = laneMaxX - x;
        dir = Random.value < left / Mathf.Max(0.01f, left + right) ? -1 : 1;
        targetDist = Random.Range(walkDistance.x, walkDistance.y);
        walked = 0f; state = S.Start; t = 0f;
    }

    void Move(ref Vector3 p, float k)
    {
        float d = speed * k * Time.deltaTime;
        p.x += dir * d; walked += d;
    }
}
