using UnityEngine;

/// <summary>Mongolisoturin heittämä kivi: lentää suoraan eteenpäin rintakorkeudella pyöriessään; osuu heroon, ellei tämä ole ilmassa riittävän korkealla (hyppy väistää).
/// Katoaa osuessaan tai kun aika on täynnä.</summary>
public class MongolRock : MonoBehaviour
{
    Enemy owner; float dir, speed, height, groundY, life; int damage; SpriteRenderer sr;

    public static void Throw(Enemy owner, Sprite sprite, Vector3 start, float height, float dir, float speed, int damage)
    {
        var go = new GameObject("Kivi");
        var r = go.AddComponent<MongolRock>();
        r.owner = owner; r.dir = dir; r.speed = speed; r.height = height; r.damage = damage; r.groundY = start.y;
        r.sr = go.AddComponent<SpriteRenderer>(); r.sr.sprite = sprite;
        if (owner != null && owner.body != null) { r.sr.sortingLayerID = owner.body.sortingLayerID; r.sr.sortingOrder = owner.body.sortingOrder + 2; }
        go.transform.position = new Vector3(start.x, start.y + height, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime; life += dt;
        transform.position += new Vector3(dir * speed * dt, 0f, 0f);
        transform.Rotate(0f, 0f, -dir * 540f * dt);
        height = Mathf.Max(0.5f, height - 0.15f * dt);
        transform.position = new Vector3(transform.position.x, groundY + height, 0f);
        if (life > 3.5f) { Destroy(gameObject); return; }
        foreach (var p in Players.All)
        {
            if (p == null || !p.enabled) continue;
            Vector3 hp = p.transform.position;
            if (Mathf.Abs(hp.x - transform.position.x) > 0.55f || Mathf.Abs(hp.y - groundY) > 0.9f || p.AirHeight > 1.0f) continue;
            p.TakeHit(damage, transform.position.x - dir, owner, false);
            CameraFollow.Shake(0.04f, 0.1f);
            Destroy(gameObject);
            return;
        }
    }
}
