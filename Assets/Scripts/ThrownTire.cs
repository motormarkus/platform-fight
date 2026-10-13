using UnityEngine;

/// <summary>
/// Vihun heittämä autonrengas: lentää pyörien suoraan eteenpäin, kaataa osuessaan (suojaus torjuu) ja putoaa lattialle,
/// jossa se vierii vähän ja häviää. Pudotettu rengas (SpawnDropped) ei vahingoita.
/// </summary>
public class ThrownTire : MonoBehaviour
{
    public SpriteRenderer body, shadow;
    public float speed = 14f, maxDistance = 14f;
    public int damage = 18;
    float dir, height, vy, vx, travelled, rot, spin, landedTime;
    bool flying, falling, landed;
    Enemy thrower;
    Sprite floorSprite, rollingSprite, airSprite;   // lattialle jäävä rengas (poimittava)
    PlayerController pc;

    public static ThrownTire Spawn(Sprite sprite, Vector3 groundPos, float height, float dir, Enemy by, int damage, Sprite floor = null, Sprite rolling = null)
    {
        var t = Create(sprite, groundPos, height, dir);
        t.floorSprite = floor; t.rollingSprite = rolling; t.airSprite = sprite;
        t.thrower = by; t.damage = damage; t.flying = true;
        return t;
    }

    /// Pudotettu rengas: putoaa ja vierii omia aikojaan.
    public static ThrownTire SpawnDropped(Sprite sprite, Vector3 groundPos, float height, float dir, Sprite floor = null, Sprite rolling = null)
    {
        var t = Create(sprite, groundPos, height, dir);
        t.floorSprite = floor; t.rollingSprite = rolling; t.airSprite = sprite;
        t.falling = true; t.vx = dir * 1.5f; t.vy = 2f; t.spin = -dir * 300f;
        return t;
    }

    static ThrownTire Create(Sprite sprite, Vector3 groundPos, float height, float dir)
    {
        var go = new GameObject("Heitetty rengas");
        go.transform.position = groundPos;
        var t = go.AddComponent<ThrownTire>();
        var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(go.transform, false);
        b.sprite = sprite;
        var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
        sh.sprite = PlayerController.CreateShadowSprite(); sh.color = new Color(0f, 0f, 0f, 0.3f);
        t.body = b; t.shadow = sh; t.dir = dir; t.height = height;
        return t;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 p = transform.position;
        if (flying)
        {
            float step = speed * dt;
            p.x += dir * step; travelled += step;
            rot -= dir * 540f * dt;
            transform.position = p;
            if (pc == null) pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                Vector3 q = pc.transform.position;
                if (Mathf.Abs(q.x - p.x) < 0.8f && Mathf.Abs(q.y - p.y) < 0.55f && pc.AirHeight < height + 0.7f)
                {
                    pc.TakeKnockdown(damage, p.x - dir, 6f, 4f, thrower);
                    HitSpark.Spawn(new Vector3(p.x, p.y + height, 0f), true, Mathf.RoundToInt(-p.y * 100f) + 6);
                    Drop(-dir * 2.5f);
                }
            }
            if (flying && travelled > maxDistance) Drop(dir * 3f);
        }
        else if (falling)
        {
            p.x += vx * dt; transform.position = p;
            vy -= 30f * dt; height += vy * dt; rot += spin * dt;
            if (height <= 0f)
            {
                height = 0f; falling = false; landed = true; landedTime = 0f;
                HitFx.OnHitQuiet();
                if (floorSprite != null && rollingSprite != null && airSprite != null && Area.Current != null)
                {
                    // jää lattialle tavallisena renkaana (heron poimittavissa)
                    LifeRing.CreateTire(floorSprite, rollingSprite, airSprite, Area.Current, transform.position);
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else if (landed)
        {
            // lattialla: vierii hidastuen ja häipyy
            landedTime += dt;
            vx = Mathf.MoveTowards(vx, 0f, 4f * dt);
            p.x += vx * dt; transform.position = p;
            rot += vx * 120f * dt;
            float a = Mathf.Clamp01(1f - (landedTime - 1.5f) / 1f);
            if (body != null) body.color = new Color(1f, 1f, 1f, a);
            if (landedTime > 2.5f) { Destroy(gameObject); return; }
        }
        if (body != null)
        {
            body.transform.localPosition = new Vector3(0f, height + (landed ? 0.9f : 0f), 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 3;
        }
        if (shadow != null)
        {
            shadow.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) - 1;
            shadow.transform.localScale = new Vector3(0.7f, 0.2f, 1f);
        }
    }

    void Drop(float sideways)
    {
        flying = false; falling = true;
        vx = sideways; vy = 4f; spin = Random.Range(-500f, 500f);
    }
}
