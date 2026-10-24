using UnityEngine;

/// <summary>Sinin ja Dinan kämpän kissa: istuu ja nuolee tassua, kävelee välillä toiseen kohtaan ja istahtaa taas.
/// Kuvat Resources/Kissa (Tools/FlatAssets/make_cat_frames.py): idle, start (nousee), walk, stop (istuu).</summary>
public class KampaKissa : MonoBehaviour
{
    public Area area;
    public float laneMinX, laneMaxX;
    public float speed = 1.6f;
    public Vector2 waitRange = new Vector2(8f, 22f);
    public float catHeight = 0.96f;            // istuvan kissan korkeus maailman yksiköinä

    const float SrcH = 198f, SitPx = 174f, PivotY = 0.076f;
    const float IdleFt = 1f / 8f, MoveFt = 1f / 12f;
    enum S { Idle, Start, Walk, Stop }

    SpriteRenderer body, shadow;
    Sprite[] idle, start, walk, stop;
    S state; float t, wait, goalX;

    void Awake()
    {
        var vis = new GameObject("Visual"); vis.transform.SetParent(transform, false);
        body = vis.AddComponent<SpriteRenderer>();
        var sh = new GameObject("Shadow"); sh.transform.SetParent(transform, false);
        shadow = sh.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(0.9f, 0.3f, 1f);
        idle = Load("idle"); start = Load("start"); walk = Load("walk"); stop = Load("stop");
        if (idle.Length > 0) body.sprite = idle[0];
        else Debug.LogWarning("KampaKissa: kuvia ei löytynyt kansiosta Resources/Kissa.", this);
        wait = Random.Range(waitRange.x, waitRange.y);
    }

    Sprite[] Load(string n)
    {
        var l = new System.Collections.Generic.List<Sprite>();
        float ppu = SitPx / catHeight;
        for (int i = 0; ; i++)
        {
            var tx = Resources.Load<Texture2D>("Kissa/" + n + "_" + i.ToString("00"));
            if (tx == null) break;
            l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, PivotY), ppu, 0, SpriteMeshType.FullRect));
        }
        return l.ToArray();
    }

    void Set(S s) { state = s; t = 0f; }

    void Update()
    {
        if (body == null || idle.Length == 0 || (area != null && Area.Current != area)) return;
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        switch (state)
        {
            case S.Idle:
                body.sprite = idle[(int)(t / IdleFt) % idle.Length];
                wait -= dt;
                if (wait <= 0f && t > 4f && start.Length > 0 && walk.Length > 0)
                {
                    goalX = Random.Range(laneMinX, laneMaxX);
                    if (Mathf.Abs(goalX - p.x) < 1.5f) goalX = Mathf.Clamp(p.x + (p.x > (laneMinX + laneMaxX) * 0.5f ? -1f : 1f) * Random.Range(3f, 6f), laneMinX, laneMaxX);
                    body.flipX = goalX < p.x;
                    Set(S.Start);
                }
                break;
            case S.Start:
                body.sprite = start[Mathf.Min(start.Length - 1, (int)(t / MoveFt))];
                if (t >= start.Length * MoveFt) Set(S.Walk);
                break;
            case S.Walk:
                body.sprite = walk[(int)(t / MoveFt) % walk.Length];
                float dir = Mathf.Sign(goalX - p.x);
                p.x += dir * speed * dt;
                if (Mathf.Abs(goalX - p.x) <= speed * dt * 1.5f) { p.x = goalX; transform.position = p; Set(stop.Length > 0 ? S.Stop : S.Idle); wait = Random.Range(waitRange.x, waitRange.y); break; }
                transform.position = p;
                break;
            case S.Stop:
                body.sprite = stop[Mathf.Min(stop.Length - 1, (int)(t / MoveFt))];
                if (t >= stop.Length * MoveFt) { Set(S.Idle); wait = Random.Range(waitRange.x, waitRange.y); }
                break;
        }
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        shadow.sortingOrder = body.sortingOrder - 1;
    }
}
