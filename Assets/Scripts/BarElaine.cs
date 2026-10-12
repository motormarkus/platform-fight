using UnityEngine;

/// <summary>
/// Baarin asiakas (Elaine ja Shanice): ei taistelija eikä lyötävissä. Seisoskelee (idle), kävelee lattialla, pysähtyy välillä tiskille
/// (kääntyy tiskiä kohti, nojaa siihen, kääntyy takaisin) ja menee välillä lavan eteen tanssimaan.
/// Jos partner ja puhekuvat on annettu, hän käy välillä juttelemassa partnerin vieressä (partner kuuntelee idlessä).
/// Kuvat katsovat oikealle; vasemmalle kuva peilataan. Toimii vain, kun hänen alueensa on aktiivinen.
/// </summary>
public class BarElaine : MonoBehaviour
{
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] idle, walk, toCounter, atCounter, dance, talk;
    [Tooltip("Tanssi toistetaan edestakaisin (kun kuvasarja ei ole silmukka).")]
    public bool dancePingPong;
    [Tooltip("Hän, jonka luo juttelemaan mennään.")]
    public BarElaine partner;
    public float idleFrameTime = 0.208f, walkFrameTime = 0.073f, toCounterFrameTime = 0.083f, atCounterFrameTime = 0.167f, danceFrameTime = 0.15f, talkFrameTime = 0.125f;
    public float speed = 1.5f;
    [Tooltip("Lattian kävelyalue (x).")]
    public float laneMinX, laneMaxX;
    [Tooltip("Tiskipaikat (x), joissa hän nojaa tiskiin.")]
    public float[] counterXs;
    [Tooltip("Tanssilattia lavan edessä: x-väli ja y-väli.")]
    public Vector2 danceX, danceY;
    public Vector2 waitRange = new Vector2(2f, 6f);
    public Vector2 counterStay = new Vector2(7f, 14f);
    public Vector2 danceTime = new Vector2(10f, 22f), talkTime = new Vector2(8f, 16f);
    [Range(0f, 1f)] public float counterChance = 0.3f, danceChance = 0.35f, talkChance = 0f;

    enum S { Idle, Walk, ToCounter, AtCounter, FromCounter, Dance, Talk }
    S state = S.Idle;
    float t, wait;
    Vector2 goal; bool goalIsCounter, goalIsDance, goalIsTalk;
    bool flipped;
    float danceLeft;

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idle); PlayerController.SortByFrameNumber(walk);
        PlayerController.SortByFrameNumber(toCounter); PlayerController.SortByFrameNumber(atCounter);
        PlayerController.SortByFrameNumber(dance); PlayerController.SortByFrameNumber(talk);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        wait = Random.Range(waitRange.x, waitRange.y);
    }

    float Limit(float x) => area != null ? area.MaxDepthAtX(x, area.maxDepthY) : -1f;
    static bool Has(Sprite[] s) => s != null && s.Length > 0;

    void Update()
    {
        if (body == null || (area != null && Area.Current != area)) return;
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        switch (state)
        {
            case S.Idle:
                Show(idle, idleFrameTime);
                wait -= dt;
                if (wait <= 0f) PickGoal();
                break;

            case S.Walk:
                if (MoveToGoal(p, dt))
                {
                    if (goalIsCounter && Has(toCounter)) { state = S.ToCounter; t = 0f; }
                    else if (goalIsDance && Has(dance)) { state = S.Dance; t = 0f; danceLeft = Random.Range(danceTime.x, danceTime.y); flipped = false; }
                    else if (goalIsTalk && Has(talk) && partner != null) { state = S.Talk; t = 0f; danceLeft = Random.Range(talkTime.x, talkTime.y); }
                    else { state = S.Idle; t = 0f; wait = Random.Range(waitRange.x, waitRange.y); }
                }
                Show(walk, walkFrameTime);
                break;

            case S.ToCounter:
            {
                int i = (int)(t / toCounterFrameTime);
                if (i >= toCounter.Length) { state = S.AtCounter; t = 0f; wait = Random.Range(counterStay.x, counterStay.y); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.AtCounter:
                Show(atCounter, atCounterFrameTime);
                wait -= dt;
                if (wait <= 0f) { state = S.FromCounter; t = 0f; }
                break;

            case S.FromCounter:
            {
                int i = toCounter.Length - 1 - (int)(t / toCounterFrameTime);
                if (i < 0) { state = S.Idle; t = 0f; wait = Random.Range(0.5f, 1.5f); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.Talk:
                if (partner != null) flipped = partner.transform.position.x < p.x;
                Show(talk, talkFrameTime);
                danceLeft -= dt;
                if (danceLeft <= 0f) { state = S.Idle; t = 0f; wait = Random.Range(waitRange.x, waitRange.y); }
                break;

            case S.Dance:
                if (dancePingPong && dance.Length > 1)
                {
                    int cycle = 2 * dance.Length - 2, k = (int)(t / danceFrameTime) % cycle;
                    body.sprite = dance[k < dance.Length ? k : cycle - k];
                }
                else Show(dance, danceFrameTime);
                danceLeft -= dt;
                if (danceLeft <= 0f) { state = S.Idle; t = 0f; wait = Random.Range(waitRange.x, waitRange.y); }
                break;
        }
        body.flipX = flipped;
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }

    bool MoveToGoal(Vector3 p, float dt)
    {
        Vector2 d = goal - (Vector2)p;
        float step = speed * dt;
        if (Mathf.Abs(d.x) > 0.05f) flipped = d.x < 0f;
        if (d.magnitude <= step) { transform.position = new Vector3(goal.x, goal.y, 0f); return true; }
        transform.position = (Vector2)p + d.normalized * step;
        return false;
    }

    void Show(Sprite[] set, float frameTime)
    {
        if (Has(set)) body.sprite = set[(int)(t / frameTime) % set.Length];
    }

    void PickGoal()
    {
        float r = Random.value;
        goalIsCounter = counterXs != null && counterXs.Length > 0 && r < counterChance;
        goalIsDance = !goalIsCounter && Has(dance) && r < counterChance + danceChance;
        goalIsTalk = !goalIsCounter && !goalIsDance && partner != null && Has(talk) && r < counterChance + danceChance + talkChance;
        float x, y;
        if (goalIsCounter) { x = counterXs[Random.Range(0, counterXs.Length)]; y = Limit(x) - 0.08f; }
        else if (goalIsTalk) { Vector3 pp = partner.transform.position; x = pp.x + (Random.value < 0.5f ? -1.4f : 1.4f); y = pp.y; }
        else if (goalIsDance) { x = Random.Range(danceX.x, danceX.y); y = Random.Range(danceY.x, danceY.y); }
        else { x = Random.Range(laneMinX, laneMaxX); y = Limit(x) - Random.Range(0.3f, 1.2f); }
        goal = new Vector2(x, y);
        state = S.Walk; t = 0f;
    }
}
