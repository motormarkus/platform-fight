using UnityEngine;

/// <summary>
/// Horhen heittämä kultaharkko: lentää kovaa suoraan eteenpäin pään korkeudella pyörien, kaataa osuessaan
/// (suojaus torjuu). Osuman tai lennon jälkeen putoaa lattialle ja muuttuu rahaksi, jonka pelaaja voi poimia.
/// </summary>
public class GoldBar : MonoBehaviour
{
    public SpriteRenderer body, shadow;
    public float speed = 23f, maxDistance = 16f;
    public int damage = 16, value = 50;
    float dir, height, vy, travelled, rot;
    bool flying = true, falling;
    Enemy thrower;
    PlayerController pc;

    public static void Spawn(Sprite sprite, Vector3 groundPos, float height, float dir, Enemy by, int damage)
    {
        var go = new GameObject("Kultaharkko");
        go.transform.position = groundPos;
        var g = go.AddComponent<GoldBar>();
        var b = new GameObject("Visual").AddComponent<SpriteRenderer>(); b.transform.SetParent(go.transform, false);
        b.sprite = sprite; b.flipX = dir < 0f;
        var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
        sh.sprite = PlayerController.CreateShadowSprite(); sh.color = new Color(0f, 0f, 0f, 0.3f);
        g.body = b; g.shadow = sh; g.dir = dir; g.height = height; g.thrower = by; g.damage = damage;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 p = transform.position;
        if (flying)
        {
            float step = speed * dt;
            p.x += dir * step; travelled += step;
            rot -= dir * 900f * dt;
            transform.position = p;
            if (pc == null) pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                Vector3 q = pc.transform.position;
                if (Mathf.Abs(q.x - p.x) < 0.7f && Mathf.Abs(q.y - p.y) < 0.5f && pc.AirHeight < height + 0.6f)
                {
                    pc.TakeKnockdown(damage, p.x - dir, 6f, 4f, thrower);
                    HitSpark.Spawn(new Vector3(p.x, p.y + height, 0f), true, Mathf.RoundToInt(-p.y * 100f) + 6);
                    Drop(-dir * 2f);
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
                height = 0f; falling = false;
                HitFx.OnHitQuiet();
                Pickup.SpawnMoney(transform.position, value, true);   // harkko lattialla: poimittavaa rahaa
                Destroy(gameObject);
                return;
            }
        }
        if (body != null)
        {
            body.transform.localPosition = new Vector3(0f, height, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 3;
        }
        if (shadow != null)
        {
            shadow.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) - 1;
            shadow.transform.localScale = new Vector3(0.5f, 0.15f, 1f);
        }
    }
    float vx, spin;

    void Drop(float sideways)
    {
        flying = false; falling = true;
        vx = sideways; vy = 4f; spin = Random.Range(-600f, 600f);
    }
}
