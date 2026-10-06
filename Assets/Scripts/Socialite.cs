using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Salin seurapiirinainen: valitsee satunnaisesti vapaan paikan (baaritiski Sohvin edessä tai keskustelupaikka salissa),
/// kävelee sinne ja juttelee hetken. Baarissa katsoo Sohvia, muualla lähintä toista naista. Paikka on varattu, kun joku on siellä.
/// Kuvat katsovat oletuksena suuntaan facesRight.
/// </summary>
public class Socialite : MonoBehaviour
{
    [System.Serializable]
    public struct Spot { public Vector2 pos; public bool atBar; }

    static readonly List<Socialite> all = new List<Socialite>();
    static readonly HashSet<int> taken = new HashSet<int>();

    public Sprite[] idle, walk;
    public float idleFrameTime = 0.12f, walkFrameTime = 0.085f, speed = 1.3f;
    public bool facesRight = true;
    [Tooltip("Yhteiset paikat (kaikilla naisilla sama lista).")]
    public Spot[] spots;
    public Vector2 sohvi;
    public Vector2 waitRange = new Vector2(6f, 12f);
    public int startSpot;
    SpriteRenderer sr;
    int cur = -1; float t, waitLeft; bool walking, fleeing;
    [Tooltip("Etäisyys tappelusta, jonka sisällä nainen siirtyy pois (ja jonne hän ei mene).")]
    public float fightAvoid = 5f;
    float checkT;

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); if (cur >= 0) taken.Remove(cur); }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idle); PlayerController.SortByFrameNumber(walk);
        t = Random.Range(0f, 2f);
    }

    void Start()
    {
        if (spots == null || spots.Length == 0) return;
        cur = Mathf.Clamp(startSpot, 0, spots.Length - 1);
        taken.Add(cur);
        transform.position = spots[cur].pos;
        waitLeft = Random.Range(waitRange.x, waitRange.y);
    }

    void Update()
    {
        if (spots == null || spots.Length == 0 || sr == null || cur < 0) return;
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        // tappelu lähellä: pois rauhallisempaan paikkaan; matkalla ei kävellä kohti tappelua
        checkT -= dt;
        if (checkT <= 0f)
        {
            checkT = 0.3f;
            bool danger = Enemy.FightNear(p, fightAvoid);
            bool goalBad = walking && Enemy.FightNear(spots[cur].pos, fightAvoid);
            if ((!walking && danger) || goalBad) { PickNext(true); }
            else if (!danger && fleeing && !walking) fleeing = false;
        }
        if (!walking)
        {
            Face(LookTarget().x - p.x);
            if (idle != null && idle.Length > 0) sr.sprite = idle[(int)(t / idleFrameTime) % idle.Length];
            waitLeft -= dt;
            if (waitLeft <= 0f) PickNext();
        }
        else
        {
            Vector2 goal = spots[cur].pos;
            Vector2 d = goal - (Vector2)p;
            Face(d.x);
            float step = speed * (fleeing ? 1.7f : 1f) * dt;
            if (d.magnitude <= step) { transform.position = goal; walking = false; waitLeft = Random.Range(waitRange.x, waitRange.y); }
            else transform.position = (Vector2)p + d.normalized * step;
            if (walk != null && walk.Length > 0) sr.sprite = walk[(int)(t / (walkFrameTime / (fleeing ? 1.5f : 1f))) % walk.Length];
        }
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
    }

    void PickNext(bool flee = false)
    {
        var free = new List<int>();
        for (int i = 0; i < spots.Length; i++)
            if (i != cur && !taken.Contains(i) && !Enemy.FightNear(spots[i].pos, fightAvoid + 1f)) free.Add(i);
        if (free.Count == 0) { waitLeft = 2f; return; }
        if (flee)
        {
            // kauimmas tappelusta: paikka, jonka lähellä ei ole ketään hereillä olevaa
            int far = free[0]; float fd = -1f;
            foreach (int i in free)
            {
                float m = 99f;
                foreach (var e in Enemy.All) if (e != null && e.isActiveAndEnabled && !e.IsDead) m = Mathf.Min(m, Vector2.Distance(e.transform.position, spots[i].pos));
                if (m > fd) { fd = m; far = i; }
            }
            taken.Remove(cur); cur = far; taken.Add(cur); walking = true; fleeing = true;
            return;
        }
        // suositaan paikkaa toisen naisen vierestä (keskustelu)
        int pick = free[Random.Range(0, free.Count)];
        foreach (int i in free)
            foreach (var o in all)
                if (o != this && o.cur >= 0 && !o.walking && Vector2.Distance(spots[i].pos, o.spots[o.cur].pos) < 3f && Random.value < 0.5f) pick = i;
        taken.Remove(cur); cur = pick; taken.Add(cur); walking = true;
    }

    Vector2 LookTarget()
    {
        if (spots[cur].atBar) return sohvi;
        Socialite best = null; float bd = 99f;
        foreach (var o in all)
        {
            if (o == this) continue;
            float d = Vector2.Distance(o.transform.position, transform.position);
            if (d < bd) { bd = d; best = o; }
        }
        return best != null && bd < 5f ? (Vector2)best.transform.position : sohvi;
    }

    void Face(float dx) { if (Mathf.Abs(dx) > 0.05f) sr.flipX = (dx > 0f) != facesRight; }
}
