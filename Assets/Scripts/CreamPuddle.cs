using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kermaläikkä lattialla (hääkakku hajoaa: WeddingCake.SpillCream). Käytettävissä missä vain: CreamPuddle.Spawn(paikka).
/// - Läikän päällä astuminen roiskii kermapisaroita.
/// - Liukastuminen: kävelevä vihu (enemySlipPerSec) tai hero (heroSlipPerSec, juostessa heroRunSlipPerSec) liukastuu: muutama kaatumisen alkukuva
///   silmukkana, liukuu eteenpäin ja roiskii, sitten kaatuu selälleen (Enemy.CreamSlip / PlayerController.CreamSlip). Suoja immunity s ylösnousun jälkeen.
/// - Hero liukuu kermassa kuin jäällä (PlayerController.OnIce), kaadettu vihu liukuu pitkälle (Enemy.OnIceNow).
/// Kuvat: Resources/Kerma/kerma_1..3.png (läikät) ja pisara_NN.png (pisarat).
/// </summary>
public class CreamPuddle : MonoBehaviour
{
    public static readonly List<CreamPuddle> All = new List<CreamPuddle>();
    [Tooltip("Liukas alue (ellipsi): puolileveys x ja puolisyvyys y maailmayksikköinä.")]
    public float halfW = 1.6f, halfH = 0.42f;
    [Tooltip("Liukastumisia sekunnissa kermassa kävellessä (todennäköisyys/s).")]
    public float enemySlipPerSec = 1.1f, heroSlipPerSec = 0.3f, heroRunSlipPerSec = 1.2f;
    public float slipTime = 0.75f, slipSpeed = 4.2f, immunity = 2.2f;
    [Tooltip("Vihun liukastuminen: pidempi ja nopeampi liuku kuin herolla.")]
    public float enemySlipTime = 1.1f, enemySlipSpeed = 6.2f;
    public const float WorldWidth = 3.4f;
    /// Läikän venytys pystysuunnassa (syvempi läikkä lattialla).
    public const float StretchY = 1.45f;
    public const int FloorOrder = -9000;   // taustojen (-10000..-9990) päällä, hahmojen alla

    static Sprite[] puddleSprites, dropSprites;
    static readonly Dictionary<Object, float> immuneUntil = new Dictionary<Object, float>();
    static readonly Dictionary<Object, Vector3> lastPos = new Dictionary<Object, Vector3>();
    static readonly Dictionary<Object, float> dropTimer = new Dictionary<Object, float>();
    static readonly List<(Transform t, float until)> sliding = new List<(Transform, float)>();
    static float slideDropT;
    SpriteRenderer sr;
    float grow = 1f;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    static void EnsureSprites()
    {
        if (puddleSprites != null) return;
        var l = new List<Sprite>();
        for (int i = 1; i <= 3; i++)
        {
            var tx = Resources.Load<Texture2D>("Kerma/kerma_" + i);
            if (tx != null) l.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), tx.width / WorldWidth));
        }
        puddleSprites = l.ToArray();
        var d = new List<Sprite>();
        foreach (var tx in Resources.LoadAll<Texture2D>("Kerma"))
            if (tx.name.StartsWith("pisara_")) d.Add(Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), new Vector2(0.5f, 0.5f), 520f));
        dropSprites = d.ToArray();
    }

    /// Uusi läikkä lattialle (variant -1 = satunnainen 1-3). Ilmestyy nopeasti levittyen ja roiskii pisaroita.
    public static CreamPuddle Spawn(Vector3 pos, int variant = -1, bool flip = false)
    {
        EnsureSprites();
        if (puddleSprites.Length == 0) return null;
        var go = new GameObject("Kermaläikkä");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        var p = go.AddComponent<CreamPuddle>();
        p.sr = go.AddComponent<SpriteRenderer>();
        p.sr.sprite = puddleSprites[variant >= 0 ? Mathf.Clamp(variant, 0, puddleSprites.Length - 1) : Random.Range(0, puddleSprites.Length)];
        p.sr.flipX = flip; p.sr.sortingOrder = FloorOrder + Mathf.RoundToInt(-pos.y * 2f);
        p.halfW = WorldWidth * 0.46f;
        p.halfH = Mathf.Clamp(p.sr.sprite.bounds.size.y * 0.75f * StretchY, 0.4f, 0.8f);
        p.grow = 0f; go.transform.localScale = new Vector3(0.3f, 0.3f * StretchY, 1f);
        Splash(pos + Vector3.up * 0.2f, 16, 4.2f);
        Splash(pos + Vector3.up * 0.4f, 8, 5.5f);
        return p;
    }

    /// Onko kohta jonkin kermaläikän päällä.
    public static bool At(Vector3 p)
    {
        for (int i = 0; i < All.Count; i++) { var c = All[i]; if (c != null && c.Contains(p)) return true; }
        return false;
    }

    public bool Contains(Vector3 p)
    {
        Vector3 c = transform.position;
        float dx = (p.x - c.x) / Mathf.Max(0.01f, halfW * Mathf.Max(0.3f, grow)), dy = (p.y - c.y) / Mathf.Max(0.01f, halfH);
        return dx * dx + dy * dy <= 1f;
    }

    void Update()
    {
        if (grow < 1f) { grow = Mathf.Min(1f, grow + Time.deltaTime * 5f); float s = Mathf.Lerp(0.3f, 1f, 1f - (1f - grow) * (1f - grow)); transform.localScale = new Vector3(s, s * StretchY, 1f); }
        if (All.Count == 0 || All[0] != this) return;   // yhteinen tarkistus kerran ruudussa (ensimmäinen läikkä hoitaa kaikki)
        float dt = Time.deltaTime;
        foreach (var pl in Players.All)
        {
            if (pl == null) continue;
            Vector3 p = pl.transform.position;
            float sp = Speed(pl, p, dt);
            if (!At(p) || pl.AirHeight > 0.05f) continue;
            if (sp > 0.6f) StepDrops(pl, p, dt, sp);
            if (sp > 0.6f && Time.time >= Immune(pl))
            {
                float rate = sp > pl.moveSpeedX * 1.3f ? heroRunSlipPerSecOf() : heroSlipPerSecOf();
                if (Random.value < rate * dt && pl.CreamSlip(Mathf.Sign(DeltaX(pl, p)), slipTime, slipSpeed)) OnSlip(pl, pl.transform, slipTime);
            }
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || !en.isActiveAndEnabled || en.IsDead) continue;
            Vector3 p = en.transform.position;
            float sp = Speed(en, p, dt);
            if (!At(p) || en.IsAirborneNow) continue;
            if (sp > 0.5f) StepDrops(en, p, dt, sp);
            if (sp > 0.5f && en.BrainIdleOrChase && Time.time >= Immune(en) && Random.value < enemySlipPerSecOf() * dt)
                if (en.CreamSlip(Mathf.Sign(DeltaX(en, p)), enemySlipTime, enemySlipSpeed)) OnSlip(en, en.transform, enemySlipTime);
        }
        // liukuvat roiskivat
        sliding.RemoveAll(s => s.t == null || Time.time > s.until);
        slideDropT += dt;
        if (slideDropT > 0.045f && sliding.Count > 0) { slideDropT = 0f; foreach (var s in sliding) Splash(s.t.position + Vector3.up * 0.25f, Random.Range(2, 4), 3f); }
    }

    // ensimmäinen läikkä hoitaa tarkistuksen: arvot siitä
    float heroSlipPerSecOf() => heroSlipPerSec;
    float heroRunSlipPerSecOf() => heroRunSlipPerSec;
    float enemySlipPerSecOf() => enemySlipPerSec;

    static float Immune(Object o) => immuneUntil.TryGetValue(o, out float u) ? u : 0f;
    static float DeltaX(Object o, Vector3 p) { if (lastPos.TryGetValue(o, out var q)) { float d = p.x - q.x; if (Mathf.Abs(d) > 0.0001f) return d; } return Random.value < 0.5f ? -1f : 1f; }

    /// Nopeus edellisestä ruudusta (yksikköä/s); päivittää muistin.
    static float Speed(Object o, Vector3 p, float dt)
    {
        float sp = 0f;
        if (lastPos.TryGetValue(o, out var q) && dt > 0f) sp = new Vector2(p.x - q.x, p.y - q.y).magnitude / dt;
        lastPos[o] = p;
        return sp > 40f ? 0f : sp;   // teleporttaus (ovi) ei ole vauhtia
    }

    static void StepDrops(Object o, Vector3 p, float dt, float sp)
    {
        dropTimer.TryGetValue(o, out float t); t += dt * Mathf.Clamp(sp / 3f, 0.5f, 2f);
        if (t >= 0.12f) { t = 0f; Splash(p + Vector3.up * 0.05f, Random.Range(2, 5), 2.2f); }
        dropTimer[o] = t;
    }

    void OnSlip(Object key, Transform t, float time)
    {
        immuneUntil[key] = Time.time + time + 2.2f + immunity;   // liuku + makuu/ylösnousu + suoja
        sliding.Add((t, Time.time + time));
        Splash(t.position + Vector3.up * 0.3f, 14, 4.4f);
        HitFx.PlayWhoosh();
    }

    // ---------------- pisarat ----------------
    class Drop { public Transform t; public SpriteRenderer sr; public Vector3 v; public float h, vh, age, life; }
    static readonly List<Drop> drops = new List<Drop>();
    static DropRunner runner;

    /// Roiske: n pisaraa lentää kaarella ja häviää.
    public static void Splash(Vector3 at, int n, float power)
    {
        EnsureSprites();
        if (dropSprites == null || dropSprites.Length == 0) return;
        if (runner == null) { var g = new GameObject("Kermapisarat"); runner = g.AddComponent<DropRunner>(); }
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject("Kermapisara"); g.transform.SetParent(runner.transform, false);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = dropSprites[Random.Range(0, dropSprites.Length)];
            sr.flipX = Random.value < 0.5f; sr.sortingOrder = Mathf.RoundToInt(-at.y * 100f) + 6;
            float s = Random.Range(0.6f, 1.15f); g.transform.localScale = new Vector3(s, s, 1f);
            var d = new Drop { t = g.transform, sr = sr, v = new Vector3(Random.Range(-1f, 1f) * power * 0.6f, Random.Range(-0.25f, 0.25f), 0f), h = at.y, vh = Random.Range(0.5f, 1f) * power, life = Random.Range(0.45f, 0.75f) };
            g.transform.position = new Vector3(at.x, at.y, 0f);
            drops.Add(d);
        }
    }

    class DropRunner : MonoBehaviour
    {
        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                var d = drops[i];
                if (d.t == null) { drops.RemoveAt(i); continue; }
                d.age += dt; d.vh -= 14f * dt;
                Vector3 p = d.t.position; p.x += d.v.x * dt; p.y += (d.v.y + d.vh) * dt; d.t.position = p;
                d.t.Rotate(0f, 0f, d.v.x * -90f * dt);
                float k = d.age / d.life;
                d.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.4f - k * 1.4f));
                if (k >= 1f) { Destroy(d.t.gameObject); drops.RemoveAt(i); }
            }
        }
    }
}
