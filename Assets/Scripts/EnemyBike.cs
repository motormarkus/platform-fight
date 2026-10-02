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
    [Tooltip("Kuski lentää pyörältä (vihu_lento.png, 10 kuvaa) ja tyhjä pyörä (vihu_pyora_tyhja.png).")]
    public Sprite[] flySprites;
    public Sprite emptyBike;
    [Tooltip("Tyhjän pyörän kaatuminen ja räjähdys (vihu_pyora_kaatuu.png, 10 kuvaa, ruutu 768x640).")]
    public Sprite[] crashSprites;
    public float crashFrameTime = 0.09f;
    public int yankDamage = 0;

    /// Voiko kuskin kiskaista pyörältä.
    public bool CanBeGrabbed => state != S.Crash;

    /// Pelaaja kiskaisee kuskin niskasta pyörän selästä: kuski lentää tielle, pyörä jatkaa tyhjänä ja kaatuu.
    public void YankOff(float playerX, float playerDir)
    {
        if (state == S.Crash) return;
        Vector3 me = transform.position;
        state = S.Crash; t = 0f; vSpin = 0f; riderless = true;
        HitFx.OnHit(true);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.1f, 0.2f);
        if (crashMoney > 0) Pickup.SpawnMoney(me, crashMoney, false);
        if (flySprites != null && flySprites.Length > 0)
        {
            var go = new GameObject("Vihu lentää");
            go.transform.position = new Vector3(me.x, me.y, 0f);
            var fr = go.AddComponent<FlyingRider>();
            fr.sprites = flySprites;
            // kiskaisu taaksepäin pelaajan ohi: kuski jää jälkeen (hidastuu nopeasti)
            fr.velocity = new Vector2(speed * 0.35f - playerDir * 3f, 0f);
            fr.up = 7f;
            fr.startHeight = 1.6f;
            fr.flip = false;
        }
    }
    bool riderless, exploded;

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
                    // tyhjä pyörä horjuu hetken ja kaatuu kyljelleen (taaksepäin), liukuu ja häipyy
                    bool art = riderless && crashSprites != null && crashSprites.Length > 0;
                    float tilt = art ? 0f : riderless ? (t < 0.35f ? Mathf.Sin(t * 30f) * 6f : Mathf.Min(85f, (t - 0.35f) * 260f)) : Mathf.Min(90f, t * 300f);
                    body.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                    body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(2.5f - t));
                    if (art && !exploded && t >= crashFrameTime * (crashSprites.Length - 2))
                    {
                        exploded = true;   // räjähdys
                        HitFx.OnHit(true);
                        if (CameraFollow.Instance != null) CameraFollow.Shake(0.2f, 0.35f);
                    }
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

    /// Pelaajan lyönti: vihu lentää pyörältä ja pyörä kaatuu.
    public void KnockOff(float attackerX)
    {
        // lyönti: sama lento kuin kiskaisussa, mutta kuski lentää eteenpäin (iskun suuntaan)
        if (state == S.Crash) return;
        if (flySprites != null && flySprites.Length > 0) { YankOff(attackerX, -1f); return; }
        Vector3 me = transform.position;
        state = S.Crash; t = 0f; vSpin = 8f;
        HitFx.OnHit(true);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.1f, 0.2f);
        HitSpark.Spawn(me + new Vector3(0f, 1.8f, 0f), true, Mathf.RoundToInt(-me.y * 100f) + 5);
        if (crashMoney > 0) Pickup.SpawnMoney(me, crashMoney, false);
    }

    void ApplyVisual()
    {
        if (body == null || sprites == null || sprites.Length == 0) return;
        anim += Time.deltaTime * (6f + speed * 1.2f);
        Sprite spr;
        if (state == S.Crash && riderless && crashSprites != null && crashSprites.Length > 0)
            spr = crashSprites[Mathf.Min((int)(t / crashFrameTime), crashSprites.Length - 1)];
        else if (state == S.Crash && riderless && emptyBike != null) spr = emptyBike;
        else if (state == S.Kick) spr = sprites[Mathf.Min(rideFrames + (int)(t / kickFrameTime), sprites.Length - 1)];
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
