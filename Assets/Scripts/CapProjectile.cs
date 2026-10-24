using UnityEngine;

/// <summary>Pensalenkkarin heittämä lippis: lentää suoraan, nopeasti ja katkeamatta (oma sprite, ei kuvasarjan lakkia), pyörii ja häviää kun osuu tai kantama loppuu.
/// Kuva Resources/Lenkkari/cap.png (Tools/EnemyFixes/cut_lenkkari_cap.py).</summary>
public class CapProjectile : MonoBehaviour
{
    Enemy owner; PlayerController target;
    float dir, depthY, speed, range, travelled, spin;
    int damage; float ownerX;
    SpriteRenderer sr;
    static Sprite sprite; static bool tried;

    public static void Spawn(Enemy owner, PlayerController target, Vector3 start, float depthY, bool right, int damage, float speed, float range, float scale)
    {
        if (!tried)
        {
            tried = true;
            var tx = Resources.Load<Texture2D>("Lenkkari/cap");
            if (tx != null) sprite = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
        if (sprite == null) return;
        var go = new GameObject("Lippis");
        go.transform.position = start;
        go.transform.localScale = Vector3.one * scale;
        var c = go.AddComponent<CapProjectile>();
        c.owner = owner; c.target = target; c.dir = right ? 1f : -1f; c.depthY = depthY; c.damage = damage; c.speed = speed; c.range = range; c.ownerX = start.x;
        c.sr = go.AddComponent<SpriteRenderer>(); c.sr.sprite = sprite; c.sr.flipX = !right;
        c.sr.sortingOrder = Mathf.RoundToInt(-depthY * 100f) + 8;
    }

    void Update()
    {
        float dt = Time.deltaTime, step = speed * dt;
        Vector3 p = transform.position;
        float prevX = p.x;
        p.x += dir * step; travelled += step;
        transform.position = p;
        spin += -dir * 720f * dt;
        transform.rotation = Quaternion.Euler(0f, 0f, spin);
        if (target != null && target.enabled && !target.GameOver)
        {
            Vector3 tp = target.transform.position;
            // osuu, kun lakki ohittaa heron (myös jos askel ylittää hänet), samalla syvyydellä; hypyllä korkealla menee yli
            bool crossed = (tp.x - prevX) * dir >= -0.5f && (tp.x - p.x) * dir <= 0.5f;
            if (crossed && Mathf.Abs(tp.y - depthY) <= 0.9f && target.AirHeight <= 0.9f)
            {
                if (target.TakeHit(damage, ownerX, owner, false)) HitSpark.Spawn(new Vector3(tp.x - dir * 0.3f, tp.y + 2.2f, 0f), false, Mathf.RoundToInt(-tp.y * 100f) + 6);
                Destroy(gameObject);
                return;
            }
        }
        if (travelled >= range) Destroy(gameObject);
    }
}
