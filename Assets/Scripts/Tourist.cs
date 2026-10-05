using UnityEngine;

/// <summary>
/// Istuva turisti, joka nousee tuolilta, kun pelaaja tulee lähelle: nousuanimaatio, hetki seisten,
/// kävely annettuun kohtaan (esim. baarille) ja seisova idle siellä.
/// Kaikki kuvasarjat samassa koordinaatistossa (pivot ruudun alakeskellä); seisovat kuvat siirretään standOffsetY alemmas.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Tourist : MonoBehaviour
{
    public Sprite[] sitLoop;
    public float sitFrameTime = 0.22f;
    public int sitStartFrame;
    public Sprite[] standUp, idle, walk;
    public float standFrameTime = 0.11f, idleFrameTime = 0.16f, walkFrameTime = 0.065f;
    [Tooltip("Seisovien kuvien siirto alaspäin istumakuviin nähden (yksikköä).")]
    public float standOffsetY;
    [Tooltip("Kuinka läheltä (x) pelaaja herättää turistin.")]
    public float triggerDistance = 3.5f;
    [Tooltip("Mihin x:ään kävellään nousun jälkeen (0 = jää paikalleen).")]
    public float walkToX;
    public float walkSpeed = 1.4f;
    public float idleBeforeWalk = 1.2f;

    enum S { Sit, StandUp, Wait, Walk, Idle }
    S state = S.Sit;
    SpriteRenderer sr;
    float t, sitY;
    PlayerController pc;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sitY = transform.position.y;
        t = sitStartFrame * sitFrameTime;
    }

    static Sprite Loop(Sprite[] s, float t, float ft) => s[(int)(t / ft) % s.Length];

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        float dt = Time.deltaTime;
        t += dt;
        switch (state)
        {
            case S.Sit:
                if (sitLoop != null && sitLoop.Length > 0) sr.sprite = Loop(sitLoop, t, sitFrameTime);
                if (pc != null && standUp != null && standUp.Length > 0 && Mathf.Abs(pc.transform.position.x - transform.position.x) < triggerDistance)
                {
                    state = S.StandUp; t = 0f;
                    Vector3 p = transform.position; p.y = sitY - standOffsetY; transform.position = p;
                    sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
                }
                break;
            case S.StandUp:
            {
                int f = (int)(t / standFrameTime);
                if (f >= standUp.Length) { state = S.Wait; t = 0f; break; }
                sr.sprite = standUp[f];
                break;
            }
            case S.Wait:
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                if (t >= idleBeforeWalk) { state = walkToX != 0f && walk != null && walk.Length > 0 ? S.Walk : S.Idle; t = 0f; }
                break;
            case S.Walk:
            {
                Vector3 p = transform.position;
                float dir = Mathf.Sign(walkToX - p.x);
                sr.flipX = dir < 0f;                     // kuvat katsovat oikealle
                p.x = Mathf.MoveTowards(p.x, walkToX, walkSpeed * dt);
                transform.position = p;
                sr.sprite = Loop(walk, t, walkFrameTime);
                if (Mathf.Abs(p.x - walkToX) < 0.01f) { state = S.Idle; t = 0f; }
                break;
            }
            case S.Idle:
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                break;
        }
    }
}
