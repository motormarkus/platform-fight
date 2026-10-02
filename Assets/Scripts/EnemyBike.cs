using UnityEngine;

/// <summary>
/// Vihu urheilupyörällä (valtatie). Tulee takaa, ohittaa pelaajan, asettuu vähän eteen samalle kaistalle ja
/// potkaisee taaksepäin. Jos pelaaja ajaa kovaa sen perään (törmää takaa), vihu kaatuu ja liukuu pois.
/// </summary>
public class EnemyBike : MonoBehaviour
{
    public SpriteRenderer body, shadow;
    [Tooltip("vihu_pratka.png: 0–9 ajo, 10–17 potku taaksepäin.")]
    public Sprite[] sprites;
    public int rideFrames = 10, kickImpact = 3;
    public float kickFrameTime = 0.06f;
    public int kickDamage = 12;
    [Tooltip("Ohitusnopeus pelaajaan nähden (yks/s).")]
    public float overtakeSpeed = 4f;
    public float depthSpeed = 1.6f;
    public int kicksBeforeLeaving = 3;
    public int crashMoney = 1;

    enum S { Approach, Kick, Leave, Crash }
    S state = S.Approach;
    float speed, t, anim, kickCooldown = 1f, height, vSpin;
    int kicks;
    bool hitDone;
    PlayerController pc;

    void Start()
    {
        pc = FindFirstObjectByType<PlayerController>();
        PlayerController.SortByFrameNumber(sprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        var mb = Motorbike.Current;
        speed = (mb != null ? mb.Speed : 8f) + overtakeSpeed;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        var mb = Motorbike.Current;
        if (pc == null) { Destroy(gameObject); return; }
        Vector3 me = transform.position, p = pc.transform.position;
        float ps = mb != null ? mb.Speed : 0f;
        float ahead = me.x - p.x;

        switch (state)
        {
            case S.Approach:
            {
                // tavoite: 2.6 yksikköä pelaajan edellä, samalla kaistalla
                float targetSpeed = ps + Mathf.Clamp((2.6f - ahead) * 1.5f, -3f, overtakeSpeed);
                speed = Mathf.MoveTowards(speed, targetSpeed, 8f * dt);
                if (ahead > 0.5f) me.y = Mathf.MoveTowards(me.y, p.y, depthSpeed * dt);   // vasta ohitettuaan kaistalle
                kickCooldown -= dt;
                if (kickCooldown <= 0f && ahead > 1.6f && ahead < 3.6f && Mathf.Abs(me.y - p.y) < 0.35f)
                { state = S.Kick; t = 0f; hitDone = false; }
                break;
            }
            case S.Kick:
            {
                speed = Mathf.MoveTowards(speed, ps, 8f * dt);
                int f = (int)(t / kickFrameTime);
                if (!hitDone && f >= kickImpact)
                {
                    hitDone = true;
                    if (mb != null && ahead > 0.8f && ahead < 4.2f && Mathf.Abs(me.y - p.y) < 0.45f) mb.Knock(kickDamage);
                }
                if (f >= sprites.Length - rideFrames)
                {
                    kicks++;
                    kickCooldown = Random.Range(1.2f, 2.2f);
                    state = kicks >= kicksBeforeLeaving ? S.Leave : S.Approach;
                    t = 0f;
                }
                break;
            }
            case S.Leave:
                speed = Mathf.MoveTowards(speed, ps + 9f, 6f * dt);
                if (ahead > 30f) { Destroy(gameObject); return; }
                break;
            case S.Crash:
                speed = Mathf.MoveTowards(speed, 0f, 10f * dt);
                vSpin -= 30f * dt; height = Mathf.Max(0f, height + vSpin * dt);
                if (body != null)
                {
                    body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Min(90f, t * 300f));
                    body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(2.5f - t));
                }
                if (t > 2.5f) { Destroy(gameObject); return; }
                break;
        }

        // törmäys takaa: pelaaja ajaa kovempaa samalla kaistalla vihun perään
        if (state != S.Crash && mb != null && ahead > 0f && ahead < 2.0f && Mathf.Abs(me.y - p.y) < 0.4f && ps > speed + 2f)
        {
            state = S.Crash; t = 0f; vSpin = 6f;
            HitFx.OnHit(true);
            HitSpark.Spawn(me + new Vector3(-1f, 1.2f, 0f), true, Mathf.RoundToInt(-me.y * 100f) + 5);
            if (crashMoney > 0) Pickup.SpawnMoney(me, crashMoney, false);
        }

        me.x += speed * dt;
        transform.position = me;
        ApplyVisual();
        // kaukana takana (pelaaja karkasi): pois
        if (ahead < -30f) Destroy(gameObject);
    }

    void ApplyVisual()
    {
        if (body == null || sprites == null || sprites.Length == 0) return;
        anim += Time.deltaTime * (6f + speed * 1.2f);
        Sprite spr;
        if (state == S.Kick) spr = sprites[Mathf.Min(rideFrames + (int)(t / kickFrameTime), sprites.Length - 1)];
        else spr = sprites[(rideFrames - 1) - (int)anim % rideFrames];   // takaperin: pyörät pyörivät ajosuuntaan
        body.sprite = spr;
        body.transform.localPosition = new Vector3(0f, height - 0.1f, 0f);
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        body.sortingOrder = order;
        if (shadow != null)
        {
            shadow.sortingOrder = order - 1;
            shadow.transform.localScale = new Vector3(3.6f, 0.5f, 1f);
        }
    }
}
