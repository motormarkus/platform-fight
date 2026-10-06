using UnityEngine;

/// <summary>
/// Salin seurapiirinainen: kiertää pysähdyspaikkoja (keskustelee seisten, kävelee seuraavaan).
/// Kuvat katsovat oletuksena suuntaan facesRight; pysähdyksessä käännytään lookAt-pisteeseen (esim. toinen nainen tai Sohvi).
/// </summary>
public class Socialite : MonoBehaviour
{
    [System.Serializable]
    public struct Stop { public Vector2 pos; public float wait; public Vector2 lookAt; }

    public Sprite[] idle, walk;
    public float idleFrameTime = 0.12f, walkFrameTime = 0.085f, speed = 1.3f;
    public bool facesRight = true;
    public Stop[] stops;
    [Tooltip("Aloituspysähdys (eri naisilla eri, jotta kulkevat yhdessä).")]
    public int startStop;
    SpriteRenderer sr;
    int cur; float t, waitLeft; bool walking;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idle); PlayerController.SortByFrameNumber(walk);
        cur = Mathf.Clamp(startStop, 0, Mathf.Max(0, (stops?.Length ?? 1) - 1));
        if (stops != null && stops.Length > 0) { transform.position = stops[cur].pos; waitLeft = stops[cur].wait; }
        t = Random.Range(0f, 2f);
    }

    void Update()
    {
        if (stops == null || stops.Length == 0 || sr == null) return;
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        if (!walking)
        {
            Face(stops[cur].lookAt.x - p.x);
            if (idle != null && idle.Length > 0) sr.sprite = idle[(int)(t / idleFrameTime) % idle.Length];
            waitLeft -= dt;
            if (waitLeft <= 0f) { cur = (cur + 1) % stops.Length; walking = true; }
        }
        else
        {
            Vector2 goal = stops[cur].pos;
            Vector2 d = goal - (Vector2)p;
            Face(d.x);
            float step = speed * dt;
            if (d.magnitude <= step) { transform.position = goal; walking = false; waitLeft = stops[cur].wait; }
            else transform.position = (Vector2)p + d.normalized * step;
            if (walk != null && walk.Length > 0) sr.sprite = walk[(int)(t / walkFrameTime) % walk.Length];
        }
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    void Face(float dx) { if (Mathf.Abs(dx) > 0.05f) sr.flipX = (dx > 0f) != facesRight; }
}
