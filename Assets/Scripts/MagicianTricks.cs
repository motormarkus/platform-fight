using UnityEngine;

/// <summary>
/// Taikurin rauhallisen esityksen lisäkuvat: videoiden kyyhky ja hattu lentävät ruudun ulkopuolelle, joten kun ne katoavat kuvasta, tämä jatkaa niiden lentoa koodilla.
///  - KYYHKY (calmIdleSprites 0-119): kuvista 86-101 kyyhky piirretään videon omilla lentokuvilla (siivet liikkuvat), sen jälkeen lento jatkuu läppäyksellä ruudun ulkopuolelle.
///  - HATTU (calmIdleSprites 120-239, silinteri): kuvasta 32 hattu lähtee kädestä, leijuu taikurista kauempana ja lentää takaisin päähän (kuva 77, jossa hattu on jo päässä).
/// Kuvaindeksi luetaan Enemyn kehon kuvasta. Resources/Taikuri/kyyhky_lento_NN.png + kyyhky_lento.txt ja hattu.png (ppu 108).
/// </summary>
[RequireComponent(typeof(Enemy))]
public sealed class MagicianTricks : MonoBehaviour
{
    const float Ppu = 108f;
    const int HatGone = 33, HatBack = 77, SilOffset = 120;
    static readonly Vector2 HatStart = new Vector2(234f, 65f), HatEnd = new Vector2(97f, 52f), DoveStart = new Vector2(316f, 87f);   // kuvapikseleitä (x oikealle, y alas) silinteri-/kyyhky-kuvissa
    const int DoveFirst = 86, DoveLast = 101;
    static Sprite[] doveFr; static Vector2[] doveC; static Sprite hatSp;

    Enemy e; SpriteRenderer dove, hat;
    int prevIdx = -1; float idxSince; float doveT = -1f; Vector3 doveP, doveV; float doveRot;

    static void Load()
    {
        if (doveFr == null)
        {
            // kyyhkysen lento videosta (Tools/MagicianAssets/make_dove_flight.py): kuvat 86-101 pienenä kuvana + keskipiste kyyhky-kuvan pikseleinä
            var ta = Resources.Load<TextAsset>("Taikuri/kyyhky_lento");
            var l = new System.Collections.Generic.List<Sprite>(); var c = new System.Collections.Generic.List<Vector2>();
            if (ta != null)
                foreach (var line in ta.text.Split('\n'))
                {
                    var pr = line.Trim().Split(' '); if (pr.Length < 3) continue;
                    var t = Resources.Load<Texture2D>("Taikuri/kyyhky_lento_" + pr[0]); if (t == null) continue;
                    t.filterMode = FilterMode.Bilinear;
                    l.Add(Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), Ppu));
                    c.Add(new Vector2(float.Parse(pr[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(pr[2], System.Globalization.CultureInfo.InvariantCulture)));
                }
            doveFr = l.ToArray(); doveC = c.ToArray();
        }
        if (hatSp == null) { var t = Resources.Load<Texture2D>("Taikuri/hattu"); if (t != null) { t.filterMode = FilterMode.Bilinear; hatSp = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), Ppu); } }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { doveFr = null; hatSp = null; }

    void Awake() { e = GetComponent<Enemy>(); }
    void OnDisable() { Kill(); }
    void Kill() { if (dove != null) Destroy(dove.gameObject); if (hat != null) Destroy(hat.gameObject); dove = hat = null; doveT = -1f; doveFree = false; lastDoveIdx = -1; }

    // kuvapikseli (x, y alas) -> maailma, nykyisen kehon kuvan pivotin mukaan
    Vector3 W(Vector2 px, Sprite s, SpriteRenderer body)
    {
        float lx = (px.x - s.pivot.x) / Ppu, ly = ((s.rect.height - px.y) - s.pivot.y) / Ppu;
        if (body.flipX) lx = -lx;
        return body.transform.TransformPoint(new Vector3(lx, ly, 0f));
    }

    SpriteRenderer NewProp(string n, Sprite sp, SpriteRenderer body)
    {
        var go = new GameObject(n); var r = go.AddComponent<SpriteRenderer>(); r.sprite = sp; r.sortingOrder = body.sortingOrder + 3;
        go.transform.localScale = Vector3.one * Mathf.Max(0.01f, body.transform.lossyScale.x); return r;
    }

    void LateUpdate()
    {
        var body = e != null ? e.body : null; var cs = e != null ? e.calmIdleSprites : null;
        if (body == null || cs == null || cs.Length < 240 || e.IsDead) { Kill(); return; }
        Load();
        // kyyhky lentää omalla ajallaan (jatkuu, vaikka tempun kuvat jo vaihtuisivat)
        if (doveFree) TickDove(body);
        int idx = e.IsAwake ? -1 : System.Array.IndexOf(cs, body.sprite);
        if (idx < 0) { if (hat != null) { Destroy(hat.gameObject); hat = null; } if (!doveFree && dove != null) { Destroy(dove.gameObject); dove = null; doveT = -1f; lastDoveIdx = -1; } prevIdx = -1; return; }
        if (idx != prevIdx) { prevIdx = idx; idxSince = Time.time; }
        TrackDove(body, idx);
        // hattu
        if (idx >= SilOffset && hatSp != null)
        {
            int si = idx - SilOffset;
            if (si >= HatGone && si < HatBack)
            {
                float ft = Mathf.Max(0.01f, e.idleFrameTime), f = si + Mathf.Clamp01((Time.time - idxSince) / ft);
                if (hat == null) hat = NewProp("Taikurin leijuva hattu", hatSp, body);
                PlaceHat(body, (f - (HatGone - 1)) / (HatBack - (HatGone - 1)));
                hat.sortingOrder = body.sortingOrder + 3;
                return;
            }
        }
        if (hat != null) { Destroy(hat.gameObject); hat = null; }
    }

    void PlaceHat(SpriteRenderer body, float u)
    {
        u = Mathf.Clamp01(u);
        Sprite cur = body.sprite; float d = body.flipX ? -1f : 1f;
        // maailma-pisteet: lähtö (kädestä), leijunta (kaukana taikurista, ylhäällä) ja paluu (päähän). Kuvan 77 hattu on pään päällä.
        Vector3 p0 = W(HatStart, cur, body), p1 = W(HatEnd, cur, body);
        Vector3 hover = p1 + new Vector3(d * 3.3f, 2.6f, 0f);
        const float a = 0.26f, b = 0.72f;
        Vector3 p; float ang;
        if (u < a)
        {
            float k = u / a; float ke = 1f - (1f - k) * (1f - k);
            p = Vector3.Lerp(p0, hover, ke) + new Vector3(0f, 1.2f * Mathf.Sin(k * Mathf.PI), 0f); ang = Mathf.Lerp(-25f, -720f * d, ke);
        }
        else if (u < b)
        {
            float k = (u - a) / (b - a);
            p = hover + new Vector3(Mathf.Sin(k * 5.2f) * 0.35f, Mathf.Sin(k * 7.4f) * 0.28f, 0f); ang = -720f * d + Mathf.Sin(k * 6.3f) * 14f;
        }
        else
        {
            float k = (u - b) / (1f - b); float ke = k * k * (3f - 2f * k);
            Vector3 ph = hover + new Vector3(Mathf.Sin((1f) * 5.2f) * 0.35f, Mathf.Sin(7.4f) * 0.28f, 0f);
            p = Vector3.Lerp(ph, p1, ke) + new Vector3(0f, 1.6f * Mathf.Sin(k * Mathf.PI), 0f); ang = Mathf.Lerp(-720f * d, -1080f * d, ke);
        }
        hat.transform.position = new Vector3(p.x, p.y, 0f); hat.transform.rotation = Quaternion.Euler(0f, 0f, ang);
        hat.flipX = false;
    }

    // kuvat 86-101: kyyhky piirretään videon omilla kuvilla paikkaansa (tiedoston keskipisteet); sen jälkeen lento jatkuu vapaana (läppäys + vauhti) kunnes ruudun ulkopuolella
    void TrackDove(SpriteRenderer body, int idx)
    {
        if (doveFr == null || doveFr.Length == 0) return;
        if (idx >= DoveFirst && idx <= DoveLast && doveT < 0f && !doveFree) { }
        if (idx >= DoveFirst && idx <= DoveLast && !doveFree)
        {
            int k = Mathf.Clamp(idx - DoveFirst, 0, doveFr.Length - 1);
            if (dove == null) dove = NewProp("Taikurin kyyhky", doveFr[k], body);
            dove.sprite = doveFr[k]; dove.flipX = body.flipX; dove.sortingOrder = body.sortingOrder + 3;
            dove.transform.position = Z(W(doveC[k], body.sprite, body)); dove.transform.rotation = Quaternion.identity;
            lastDoveIdx = idx; doveT = 0f; return;
        }
        if (idx > DoveLast && dove != null && !doveFree && lastDoveIdx == DoveLast)
        {
            int n = doveFr.Length; Vector3 a = W(doveC[n - 1], body.sprite, body), b = W(doveC[Mathf.Max(0, n - 4)], body.sprite, body);
            float ft = Mathf.Max(0.01f, e.idleFrameTime); Vector3 v = (a - b) / (3f * ft); float d = body.flipX ? -1f : 1f;
            doveV = new Vector3(d * Mathf.Max(Mathf.Abs(v.x), 4.5f), Mathf.Max(v.y, 2.8f), 0f); doveP = a; doveFree = true; doveT = 0f;
        }
        if (!doveFree && idx >= 0 && (idx < DoveFirst || idx > DoveLast + 30) && dove != null) { Destroy(dove.gameObject); dove = null; doveT = -1f; }
    }
    static Vector3 Z(Vector3 p) { return new Vector3(p.x, p.y, 0f); }
    bool doveFree; int lastDoveIdx = -1;

    void TickDove(SpriteRenderer body)
    {
        if (!doveFree || dove == null) return;
        float dt = Time.deltaTime; doveT += dt;
        doveV.y += 3.0f * dt; doveV.x *= 1f + 0.4f * dt; doveP += doveV * dt;
        int n = doveFr.Length, lo = Mathf.Max(0, n - 10);
        dove.sprite = doveFr[lo + (int)(doveT * 14f) % (n - lo)];                 // läppäys videon omilla kuvilla
        dove.transform.position = Z(doveP); dove.transform.rotation = Quaternion.identity;
        if (doveT > 3.5f) { Destroy(dove.gameObject); dove = null; doveT = -1f; doveFree = false; lastDoveIdx = -1; }
    }
}
