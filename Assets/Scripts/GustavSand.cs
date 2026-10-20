using UnityEngine;

/// <summary>
/// Gustavin hiekka: lentää taskusta heitettynä eteenpäin. Osuessaan huumaa pelaajan (PlayerController.Daze), ellei tämä suojaa edestä,
/// hyppää yli tai väistä alas. Tulos ilmoitetaan aivolle (osuma -> Gustav hakee heiton).
/// Kuvat: gustav_hiekkapilvet.png (5 puuskaa kasvavassa koossa + pilvi).
/// </summary>
public class GustavSand : MonoBehaviour
{
    PlayerController hero;
    Enemy owner;
    Sprite[] puffs;
    Sprite cloud;
    System.Action<bool> result;
    float dir, speed, life, t, stunSeconds, hitHalfWidth, hitHalfDepth;
    SpriteRenderer sr;
    int order;
    bool done;

    public static void Spawn(Vector3 start, float dirX, PlayerController hero, Enemy owner, Sprite[] puffs, Sprite cloud, float speed, float life, float stunSeconds, System.Action<bool> result)
    {
        var go = new GameObject("Gustavin hiekka");
        go.transform.position = start;
        var s = go.AddComponent<GustavSand>();
        s.sr = go.AddComponent<SpriteRenderer>();
        s.hero = hero; s.owner = owner; s.puffs = puffs; s.cloud = cloud; s.result = result;
        s.dir = dirX >= 0f ? 1f : -1f; s.speed = speed; s.life = life; s.stunSeconds = stunSeconds;
        s.hitHalfWidth = 0.95f; s.hitHalfDepth = 0.75f;
        s.sr.flipX = s.dir < 0f;
        s.order = Mathf.RoundToInt(-owner.transform.position.y * 100f) + 6;
        s.sr.sortingOrder = s.order;
        s.Show();
    }

    void Show()
    {
        if (puffs != null && puffs.Length > 0)
        {
            // puuskat kasvavat ensimmäisen 0,3 sekunnin ajan, sen jälkeen pilvi
            float grow = 0.3f;
            if (t < grow || cloud == null) { sr.sprite = puffs[Mathf.Min((int)(t / grow * puffs.Length), puffs.Length - 1)]; return; }
        }
        if (cloud != null) sr.sprite = cloud;
    }

    void Update()
    {
        if (done) return;
        t += Time.deltaTime;
        transform.position += new Vector3(dir * speed * Time.deltaTime, 0f, 0f);
        Show();
        if (hero != null && !hero.GameOver)
        {
            Vector3 p = transform.position, hp = hero.transform.position;
            bool near = Mathf.Abs(hp.x - p.x) < hitHalfWidth && Mathf.Abs(hp.y - owner.transform.position.y) < hitHalfDepth;
            if (near && hero.AirHeight < 1.0f && !hero.Ducking)
            {
                done = true;
                bool blocked = hero.BlocksFrom(owner.transform.position.x);
                if (blocked) HitSpark.Spawn(hp + new Vector3(-dir * 0.5f, 2.0f, 0f), false, order + 1, true);
                else hero.Daze(stunSeconds);
                if (result != null) result(!blocked);
                Destroy(gameObject);
                return;
            }
        }
        if (t >= life) { done = true; if (result != null) result(false); Destroy(gameObject); }
    }
}
