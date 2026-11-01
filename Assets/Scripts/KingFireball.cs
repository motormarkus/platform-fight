using UnityEngine;

/// <summary>Temppelin kuninkaan tulipallo: syöksyy erittäin nopeasti heroa kohti, säteilee (kultainen hohto, pyörivät säteet, perässä kipinäjälki) ja osuessaan välähtää koko ruutu, kipinät räjähtävät ja ruutu tärähtää.
/// Hyppäämällä sen yli pääsee (AirHeight). Kuva Resources/Kuningas/tulipallo.png (Tools/KingAssets/make_king_sheets.py); hohto ja säteet piirretään koodissa.</summary>
public class KingFireball : MonoBehaviour
{
    Enemy owner; float dir, depthY, speed, range, travelled, ownerX, baseScale, age, trailT;
    int damage; float knockSpeed, knockUp;
    SpriteRenderer core, core2, glow, rays;
    Transform rayRoot;
    static Sprite sprite; static bool tried;

    public static void Spawn(Enemy owner, Vector3 start, bool right, int damage, float speed, float range, float scale, float knockSpeed, float knockUp)
    {
        if (sprite == null)
        {
            var tx = Resources.Load<Texture2D>("Kuningas/tulipallo");
            if (tx != null) sprite = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 130f, 0, SpriteMeshType.FullRect);
        }
        if (sprite == null) return;
        var go = new GameObject("Tulipallo");
        go.transform.position = start;
        var f = go.AddComponent<KingFireball>();
        f.owner = owner; f.dir = right ? 1f : -1f; f.depthY = owner.transform.position.y; f.damage = damage; f.speed = speed; f.range = range; f.ownerX = start.x;
        f.baseScale = scale; f.knockSpeed = knockSpeed; f.knockUp = knockUp;
        int order = Mathf.RoundToInt(-f.depthY * 100f) + 8;
        var soft = KingDust.Soft();
        f.glow = Layer(go, "Hohto", soft, order - 2, new Color(1f, 0.62f, 0.15f, 0.55f), 3.2f);
        f.rayRoot = new GameObject("Säteet").transform; f.rayRoot.SetParent(go.transform, false);
        for (int i = 0; i < 8; i++)   // pyörivät säteet: ohuet pitkulaiset hohtopalat
        {
            var r = new GameObject("Säde").AddComponent<SpriteRenderer>();
            r.transform.SetParent(f.rayRoot, false); r.sprite = soft; r.sortingOrder = order - 1;
            r.color = new Color(1f, 0.85f, 0.4f, 0.65f);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            r.transform.localScale = new Vector3(0.22f, i % 2 == 0 ? 3.6f : 2.4f, 1f);
        }
        f.core = Layer(go, "Pallo", sprite, order, Color.white, 1f);
        f.core2 = Layer(go, "Pallo 2", sprite, order + 1, new Color(1f, 1f, 0.85f, 0.7f), 0.75f);
        go.transform.localScale = Vector3.one * scale * 0.5f;
        KingFx.Flash(0.18f, 0.12f);   // lähtö: pieni välähdys
    }

    static SpriteRenderer Layer(GameObject parent, string name, Sprite sp, int order, Color c, float scale)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(parent.transform, false); r.sprite = sp; r.sortingOrder = order; r.color = c; r.transform.localScale = Vector3.one * scale;
        return r;
    }

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        Vector3 p = transform.position; float prevX = p.x;
        p.x += dir * speed * dt; travelled += speed * dt;
        transform.position = p;
        float pulse = 1f + 0.12f * Mathf.Sin(age * 40f);
        transform.localScale = Vector3.one * baseScale * Mathf.Min(1f, 0.5f + age * 8f) * pulse;   // kasvaa kämmenen edessä täyteen kokoon hetkessä
        core.transform.localRotation = Quaternion.Euler(0f, 0f, age * 220f * -dir);
        core2.transform.localRotation = Quaternion.Euler(0f, 0f, age * 340f * dir);
        rayRoot.localRotation = Quaternion.Euler(0f, 0f, age * 180f * dir);
        glow.color = new Color(1f, 0.62f, 0.15f, 0.45f + 0.15f * Mathf.Sin(age * 30f));
        // kipinäjälki: pieniä kultaisia hiukkasia perässä
        trailT -= dt;
        if (trailT <= 0f) { trailT = 0.012f; KingFx.Spark(p - new Vector3(dir * 0.8f, 0f, 0f), new Vector2(-dir * Random.Range(1f, 4f), Random.Range(-2f, 2f)), Random.Range(0.25f, 0.5f), Random.Range(0.12f, 0.3f), glow.sortingOrder); }
        foreach (var target in Players.All)
        {
            if (target == null || !target.enabled || target.GameOver) continue;
            Vector3 tp = target.transform.position;
            bool crossed = (tp.x - prevX) * dir >= -0.9f && (tp.x - p.x) * dir <= 0.9f;   // nopea pallo ei hyppää heron yli: ylitys lasketaan osumaksi
            if (crossed && Mathf.Abs(tp.y - depthY) <= 1.2f && target.AirHeight <= 0.9f)
            {
                if (!target.TakeKnockdown(damage, ownerX, knockSpeed, knockUp, owner, true)) target.TakeHit(damage, ownerX, owner, true);
                KingFx.Impact(new Vector3(tp.x - dir * 0.2f, tp.y + 2.0f, 0f), Mathf.RoundToInt(-tp.y * 100f) + 12);
                Destroy(gameObject);
                return;
            }
        }
        if (travelled >= range || owner == null) Destroy(gameObject);
    }
}

/// <summary>Kuninkaan tehosteet: koko ruudun välähdys, kipinät ja osumaräjähdys.</summary>
public class KingFx : MonoBehaviour
{
    float life, age; Vector2 vel; SpriteRenderer sr; Color col; float size; bool ring, flash;
    static Sprite white;

    static Sprite White()
    {
        if (white != null) return white;
        var tx = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) tx.SetPixel(x, y, Color.white);
        tx.Apply();
        white = Sprite.Create(tx, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return white;
    }

    /// <summary>Koko ruudun valkoinen välähdys (alpha = voimakkuus, dur = kesto sekunteina).</summary>
    public static void Flash(float alpha, float dur)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var go = new GameObject("Välähdys");
        go.transform.SetParent(cam.transform, false); go.transform.localPosition = new Vector3(0f, 0f, 2f);
        float h = cam.orthographicSize * 2f; go.transform.localScale = new Vector3(h * cam.aspect * 1.2f, h * 1.2f, 1f);
        var fx = go.AddComponent<KingFx>();
        fx.sr = go.AddComponent<SpriteRenderer>(); fx.sr.sprite = White(); fx.sr.sortingOrder = 9500; fx.flash = true;
        fx.col = new Color(1f, 0.95f, 0.8f, alpha); fx.life = dur; fx.sr.color = fx.col;
    }

    public static void Spark(Vector3 pos, Vector2 vel, float life, float size, int order)
    {
        var go = new GameObject("Kipinä"); go.transform.position = pos;
        var fx = go.AddComponent<KingFx>();
        fx.sr = go.AddComponent<SpriteRenderer>(); fx.sr.sprite = KingDust.Soft(); fx.sr.sortingOrder = order;
        fx.vel = vel; fx.life = life; fx.size = size; fx.col = new Color(1f, Random.Range(0.65f, 0.95f), Random.Range(0.2f, 0.5f), 0.9f);
        go.transform.localScale = Vector3.one * size;
    }

    /// <summary>Osuma: kirkas välähdys, laajeneva rengas ja säteittäin lentävät kipinät; ruutu tärähtää.</summary>
    public static void Impact(Vector3 pos, int order)
    {
        Flash(0.85f, 0.35f);
        CameraFollow.Shake(0.55f, 0.5f);
        HitSpark.Spawn(pos, true, order + 2);
        var go = new GameObject("Osumarengas"); go.transform.position = pos;
        var fx = go.AddComponent<KingFx>();
        fx.sr = go.AddComponent<SpriteRenderer>(); fx.sr.sprite = KingDust.Soft(); fx.sr.sortingOrder = order;
        fx.ring = true; fx.life = 0.4f; fx.size = 1f; fx.col = new Color(1f, 0.9f, 0.55f, 0.95f);
        for (int i = 0; i < 36; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f), v = Random.Range(5f, 16f);
            Spark(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v, Random.Range(0.3f, 0.7f), Random.Range(0.15f, 0.45f), order + 1);
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        float u = Mathf.Clamp01(age / Mathf.Max(0.01f, life));
        if (flash) { sr.color = new Color(col.r, col.g, col.b, col.a * (1f - u) * (1f - u)); }
        else if (ring) { transform.localScale = Vector3.one * Mathf.Lerp(1f, 11f, 1f - (1f - u) * (1f - u)); sr.color = new Color(col.r, col.g, col.b, col.a * (1f - u)); }
        else { transform.position += (Vector3)(vel * Time.deltaTime); vel *= Mathf.Pow(0.05f, Time.deltaTime); sr.color = new Color(col.r, col.g, col.b, col.a * (1f - u)); transform.localScale = Vector3.one * size * (1f - 0.6f * u); }
        if (age >= life) Destroy(gameObject);
    }
}
