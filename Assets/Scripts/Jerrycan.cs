using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jerrygallona. Fysiikka ja kantaminen tulevat Crate-komponentista (tynnyrin tavoin): poimintanappi nostaa pään yli, lyönti tai potku heittää
/// yhtä pitkälle kuin heitto, ja osuma/heitto soittaa tynnyrin äänen. Jerrycan piirtää kuvat (pyörii lennossa), sytyttää ja räjäyttää.
/// Sytytys: tulitikut taskussa (ShopCounter.MatchesClueId) ja poimintanapin pito gallonan vieressä -> hero kyykistyy sytyttämään (sytyta_NN-kuvat),
/// sen jälkeen savuaa fuse (3 s) ja räjähtää. Räjähdys vahingoittaa kaikkia säteellä, myös heroa.
/// Kuvat: Resources/Jerrycan/gallona_0, kierto_*, boom_NNN, sytyta_NN (puuttuessa koodilla piirretty paikkakuva).
/// </summary>
[RequireComponent(typeof(Crate))]
[DefaultExecutionOrder(1000)]
public class Jerrycan : MonoBehaviour
{
    public static readonly List<Jerrycan> All = new List<Jerrycan>();
    [Tooltip("Valinnainen: 0 ehjä. Tyhjä = Resources/Jerrycan/gallona_0.")]
    public Sprite[] sprites;
    public float fuse = 3f;
    public float blastRadiusX = 3.2f, blastRadiusY = 1.6f;
    public int blastDamage = 40, heroDamage = 25, bossDamage = 12;
    public float ignitePickRange = 1.3f;
    [Tooltip("Kuvan koko (1 = 200 px/yksikkö: kanisteri 1 yksikön korkuinen).")]
    public float artScale = 1.2f;

    enum S { Idle, Lighting, Boom }
    S state = S.Idle;
    bool lit;
    float t, boomT, smokeT;
    Crate crate;
    SpriteRenderer body, flame, boom, shadow;
    static Sprite canSpr, flameSpr, boomSpr, softSpr;
    static Sprite[] boomSprs;   // räjähdysanimaatio (Resources/Jerrycan/boom_NNN): videon ruudut BoomSrc, 30 kuvaa/s
    static readonly int[] BoomSrc = { 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 43, 47, 51, 55, 59, 63, 67, 71, 75, 79, 83, 87, 91, 95, 99, 103, 107, 111, 115, 119, 123, 127, 131, 135, 139, 143, 147, 151, 155, 159 };
    static AudioClip boomClip;
    static Sprite[] spinSprs;   // lennon kuvat (Resources/Jerrycan/kierto_*): keskipiste kiertopisteenä
    static Sprite LoadSprite(string name, Vector2 pivot, float ppu = 200f)
    {
        var tex = Resources.Load<Texture2D>("Jerrycan/" + name);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, ppu);
    }

    // sytytysanimaatio
    PlayerController lighter;
    int lighterHealth;
    float lightT, lightRefH;
    const float IgniteFrame = 12f;   // sytytyskuva, jossa tikku raapaistaan saappaaseen (kipinä) = gallona syttyy
    Sprite[] lightSprs;
    SpriteRenderer lightSr;
    const float LightFps = 26f;

    class Puff { public GameObject go; public SpriteRenderer sr; public Vector3 v; public float age, life, s0, s1; }
    readonly List<Puff> puffs = new List<Puff>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        crate = GetComponent<Crate>();
        if (boomClip == null) { boomClip = Resources.Load<AudioClip>("Sfx/Rajahdys/rajahdys"); if (boomClip != null) boomClip.LoadAudioData(); }   // esiladataan: ensimmäinen räjähdys ei saa viivästyä tai mykistyä latauksen takia
        if (Mathf.Approximately(artScale, 0.8f)) artScale = 1.2f;   // sceneen tallentunut vanha oletus -> uusi koko ilman valikon ajoa
        EnsureSprites();
        // Crate hoitaa kantamisen, heiton ja äänet: tynnyri (ei hajoa), potku lennättää kuin heitto
        crate.breakable = false;
        crate.kickLaunch = true;
        crate.throwDamage = 5;
        crate.standUpChance = 0f;
        crate.rollSprites = new Sprite[] { canSpr };
        crate.hitRadiusX = 1.05f;
        var b = new GameObject("Visual"); b.transform.SetParent(transform, false);
        body = b.AddComponent<SpriteRenderer>();
        body.transform.localScale = Vector3.one * artScale;
        var sh = new GameObject("Varjo"); sh.transform.SetParent(transform, false);
        shadow = sh.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        var f = new GameObject("Liekki"); f.transform.SetParent(transform, false);
        flame = f.AddComponent<SpriteRenderer>(); flame.enabled = false; flame.sprite = flameSpr;
        var e = new GameObject("Rajahdys"); e.transform.SetParent(transform, false);
        boom = e.AddComponent<SpriteRenderer>(); boom.enabled = false; boom.sprite = boomSpr;
    }

    void Start()
    {
        var p = Players.Primary;
        if (p != null) crate.kickFlySpeed = p.crateThrowSpeed;
        // TESTAUS: tulitikut heti mukaan, kun kentällä on gallona (poista tämä kun testaus on ohi; muuten ne ostetaan Helvin tiskiltä)
        if (!Clues.Has(ShopCounter.MatchesClueId))
            Clues.Add(ShopCounter.MatchesClueId, ShopCounter.MatchesName, "Tulitikut. Pidä poimintanappia pohjassa jerrygallonan vieressä, niin se syttyy. Sen jälkeen aikaa on 3 sekuntia.", null, false);
    }

    // ---------------- hero ----------------

    /// Poimintanapin painallus: jos tulitikut ovat taskussa ja sammunut gallona on edessä, painallus odottaa:
    /// pito (HoldTime) sytyttää, lyhyt painallus nostaa gallonan kuten ennen. Ilman tikkuja nappi nostaa heti.
    public static bool BeginHold(PlayerController p)
    {
        if (p == null || p.Scripted || !Clues.Has(ShopCounter.MatchesClueId)) return false;
        Vector3 me = p.transform.position;
        Jerrycan best = null; float bd = 99f;
        foreach (var j in All)
        {
            if (j == null || j.state != S.Idle || j.lit || j.crate == null || !j.crate.CanPickUp) continue;
            Vector3 q = j.transform.position;
            float dx = q.x - me.x, dy = Mathf.Abs(q.y - me.y);
            bool front = p.FacingRight ? dx >= -0.4f : dx <= 0.4f;
            if (Mathf.Abs(dx) > j.ignitePickRange || dy > 0.7f || !front) continue;
            if (Mathf.Abs(dx) + dy < bd) { bd = Mathf.Abs(dx) + dy; best = j; }
        }
        if (best == null) return false;
        holdP = p; holdCan = best; holdT = 0f;
        return true;
    }

    public const float HoldTime = 0.4f;   // pitoaika sytytykseen (s)
    static PlayerController holdP; static Jerrycan holdCan; static float holdT;

    void TickHold(float dt)
    {
        var p = holdP;
        if (p == null || p.Scripted || p.GameOver || crate == null || !crate.CanPickUp) { holdP = null; holdCan = null; return; }
        holdT += dt;
        if (holdT < 0.06f || CoopInput.PickHeld(p.playerIndex))
        {
            if (holdT >= HoldTime) { holdP = null; holdCan = null; BeginLighting(p); }
            return;
        }
        // nappi irti ennen pitoaikaa: lyhyt painallus = nosto
        holdP = null; holdCan = null;
        p.TryLiftNearby();
    }

    void BeginLighting(PlayerController p)
    {
        // kuvat tehdään heron kuvan mittakaavassa ja piirretään sen paikalle
        float ppu = p.BodyRenderer != null && p.BodyRenderer.sprite != null ? p.BodyRenderer.sprite.pixelsPerUnit : 100f;
        var list = new List<Sprite>();
        for (int i = 0; i < 200; i++) { var sp = LoadSprite("sytyta_" + i.ToString("00"), new Vector2(0.5f, 0f), ppu); if (sp == null) break; list.Add(sp); }   // pivot jalkojen kohdalla kuten heron kuvissa
        HitFx.PlayPickup(false);
        if (list.Count == 0 || p.BodyRenderer == null) { lit = true; t = 0f; return; }   // ei kuvia: syttyy heti
        lightSprs = list.ToArray();
        lighter = p; lighterHealth = p.health; lightT = 0f;
        var hb = p.BodyRenderer;
        lightRefH = hb.sprite != null ? hb.sprite.bounds.size.y * Mathf.Abs(hb.transform.lossyScale.y) : 0f;   // heron kuvan korkeus maailmassa; sytytyskuvat (sama ruudun korkeus) skaalataan siihen
        p.Scripted = true; p.ScriptedMove = Vector2.zero;
        p.BodyRenderer.enabled = false;
        var go = new GameObject("Sytytys"); lightSr = go.AddComponent<SpriteRenderer>();
        state = S.Lighting;
    }

    void UpdateLighting(float dt)
    {
        lightT += dt;
        // gallona syttyy heti, kun tikku on raapaistu (kuva IgniteFrame); sen jälkeen osuma ei enää estä sytytystä
        if (!lit && lightT * LightFps >= IgniteFrame) { lit = true; t = 0f; }
        if (lighter == null || lighter.GameOver || lighter.health < lighterHealth) { EndLighting(lit); return; }   // osuma keskeyttää vain ennen sytytystä
        var b = lighter.BodyRenderer;
        int fi = Mathf.Min((int)(lightT * LightFps), lightSprs.Length - 1);
        lightSr.sprite = lightSprs[fi];
        lightSr.transform.position = b.transform.position;
        float k = lightRefH > 0f ? lightRefH / lightSr.sprite.bounds.size.y : Mathf.Abs(b.transform.lossyScale.y);
        lightSr.transform.localScale = new Vector3(k, k, 1f);
        lightSr.flipX = b.flipX;
        lightSr.color = b.color;
        lightSr.sortingLayerID = b.sortingLayerID;
        lightSr.sortingOrder = b.sortingOrder;
        lighter.ScriptedMove = Vector2.zero;
        if (lightT * LightFps >= lightSprs.Length) EndLighting(true);
    }

    void EndLighting(bool ok)
    {
        if (lighter != null)
        {
            lighter.Scripted = false;
            if (lighter.BodyRenderer != null) lighter.BodyRenderer.enabled = true;
        }
        lighter = null;
        if (lightSr != null) Destroy(lightSr.gameObject);
        state = S.Idle;
        if (ok && !lit) { lit = true; t = 0f; }
    }

    // ---------------- päivitys ----------------

    void LateUpdate()
    {
        // heron oma kuva pysyy piilossa koko sytytyksen ajan (pelaajan koodi voi kytkeä sen takaisin päälle, jolloin Rocco näkyisi kahtena)
        if (state == S.Lighting && lighter != null && lighter.BodyRenderer != null) lighter.BodyRenderer.enabled = false;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        UpdatePuffs(dt);
        if (holdCan == this) TickHold(dt);
        if (state == S.Boom) { DrawBoom(dt); return; }
        if (state == S.Lighting) UpdateLighting(dt);
        if (lit)
        {
            t += dt;
            if (t >= fuse) { Explode(); return; }
        }
        Draw(dt);
    }

    void Explode()
    {
        if (state == S.Lighting) EndLighting(true);
        state = S.Boom; lit = false; boomT = 0f;
        Vector3 me = transform.position;
        if (boomClip == null) boomClip = Resources.Load<AudioClip>("Sfx/Rajahdys/rajahdys");
        if (boomClip != null) HitFx.PlayClip(boomClip, 1.5f); else HitFx.OnBreak(0.12f);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.25f, 0.35f);
        // hero ensin: kaatuessaan hän pudottaa kantamansa gallonan
        foreach (var p in Players.All.ToArray())
        {
            if (p == null || p.GameOver) continue;
            Vector3 q = p.transform.position;
            if (Mathf.Abs(q.x - me.x) > blastRadiusX || Mathf.Abs(q.y - me.y) > blastRadiusY) continue;
            p.TakeKnockdown(heroDamage, me.x, 7f, 6f);
        }
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > blastRadiusX || Mathf.Abs(q.y - me.y) > blastRadiusY) continue;
            bool boss = e.maxHealth >= 300;
            float near = 1f - Mathf.Clamp01(Mathf.Abs(q.x - me.x) / blastRadiusX);   // 1 = keskellä, 0 = reunalla
            bool center = Mathf.Abs(q.x - me.x) < blastRadiusX * 0.4f && Mathf.Abs(q.y - me.y) < blastRadiusY * 0.6f;
            int dmg = boss ? bossDamage : Mathf.RoundToInt(blastDamage * (center ? 1.5f : 1f));
            e.TakeHit(dmg, me.x, true);
            float dir = Mathf.Abs(q.x - me.x) < 0.3f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(q.x - me.x);
            // keskelle osunut lentää ruudun laidoille asti, reunalla oleva vain pienen matkan
            float vx = boss ? 5f : center ? 22f : Mathf.Lerp(7f, 16f, near);
            e.Launch(dir * vx, boss ? 6f : center ? 12f : 9f);
        }
        foreach (var j in All.ToArray())
        {
            // ketjureaktio: räjähdyksen ulottuvilla oleva toinen gallona syttyy ja posahtaa pian perässä
            if (j == null || j == this || j.state == S.Boom) continue;
            Vector3 q = j.transform.position;
            if (Mathf.Abs(q.x - me.x) <= blastRadiusX * 1.4f && Mathf.Abs(q.y - me.y) <= blastRadiusY * 1.4f)
            { j.t = Mathf.Max(j.lit ? j.t : 0f, j.fuse - Random.Range(0.1f, 0.3f)); j.lit = true; }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) <= blastRadiusX && Mathf.Abs(q.y - me.y) <= blastRadiusY) c.Smash();
        }
        if (crate != null) crate.enabled = false;   // pois kantamis- ja heittologiikasta (Destroy ei onnistu, koska Jerrycan vaatii Caten)
        body.enabled = false; flame.enabled = false; shadow.enabled = false; boom.enabled = true;
    }

    // ---------------- piirto ----------------

    float ang; bool wasSpinning;
    const float BoomSpeed = 1f;      // räjähdysanimaation nopeus (1 = videon nopeus)
    const float FlashTime = 0.45f;   // ruudun valkea välähdys räjähtäessä

    void OnGUI()
    {
        if (state != S.Boom || boomT >= FlashTime) return;
        float a = 1f - boomT / FlashTime;
        GUI.depth = -1000;
        GUI.color = new Color(1f, 1f, 1f, a * a * 0.95f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Draw(float dt)
    {
        bool air = crate != null && crate.Airborne;
        float h = air ? crate.Height : 0f;
        float vx = crate != null ? crate.VelX : 0f;
        bool moving = Mathf.Abs(vx) > 0.3f;
        bool spin = spinSprs != null && air && moving;   // kiertokuvat vain ilmassa
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        if (air && !moving) order += 40;   // kannossa heron edessä
        Vector3 fpGround = Vector3.zero;
        if (spin)
        {
            wasSpinning = true;
            body.sprite = spinSprs[(int)(Time.time * 20f) % spinSprs.Length];
            body.flipX = vx < 0f;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localPosition = new Vector3(0f, h + 0.4f * artScale * 1.2f, 0f);   // keskipiste kiertopisteenä
        }
        else
        {
            if (wasSpinning)
            {   // laskeutui: pyörähtää maassa satunnaisesta kulmasta ja jää pystyyn tai kyljelleen
                wasSpinning = false;
                ang = Random.Range(0f, 360f);
            }
            if (air) ang = 0f;   // kannossa pystyssä
            else if (moving) ang -= Mathf.Sign(vx) * Mathf.Abs(vx) * dt * 95f;   // vierii eteenpäin
            else { float tgt = Mathf.Round(ang / 90f) * 90f; ang = Mathf.MoveTowards(ang, tgt, 420f * dt); }   // asettuu pystyyn / kyljelleen / ylösalaisin
            var sp = sprites != null && sprites.Length > 0 ? sprites[0] : canSpr;
            body.sprite = sp; body.flipX = false;
            var q = Quaternion.Euler(0f, 0f, ang);
            Vector3 c = sp.bounds.center * artScale, ex = sp.bounds.extents * artScale;
            float r = ang * Mathf.Deg2Rad;
            float ext = ex.y * Mathf.Abs(Mathf.Cos(r)) + ex.x * Mathf.Abs(Mathf.Sin(r));   // alin kohta lattialla
            body.transform.localRotation = q;
            body.transform.localPosition = new Vector3(0f, h + ext, 0f) - q * c;
            fpGround = body.transform.localPosition + q * new Vector3(0.2f * artScale / 0.8f, 0.8f * artScale + 0.1f, 0f);
        }
        body.sortingOrder = order;
        bool blink = lit && Mathf.FloorToInt(t * 10f) % 2 == 0;
        body.color = blink ? new Color(1f, 0.6f, 0.5f) : Color.white;
        shadow.transform.localPosition = Vector3.zero;
        float ss = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(h / 3f));
        shadow.transform.localScale = new Vector3(0.5f * ss * artScale / 0.8f, 0.16f * ss * artScale / 0.8f, 1f);
        shadow.sortingOrder = order - 1;
        flame.enabled = lit;
        if (lit)
        {
            Vector3 fp = spin ? new Vector3(0f, h + 0.4f * artScale * 1.2f + 0.45f, 0f) : fpGround;
            flame.transform.localPosition = fp + new Vector3(0f, Mathf.Sin(t * 40f) * 0.04f, 0f);
            flame.transform.localScale = Vector3.one * (0.8f + 0.3f * Mathf.Sin(t * 35f));
            flame.sortingOrder = order + 1;
            EmitSmoke(dt, transform.position + fp + new Vector3(0f, 0.12f, 0f), order + 2);
        }
    }

    void DrawBoom(float dt)
    {
        boomT += dt;
        int order = Mathf.RoundToInt(-transform.position.y * 100f) + 5;
        boom.sortingOrder = order;
        if (boomSprs != null)
        {
            float srcFrame = BoomSrc[0] + boomT * 30f * BoomSpeed;
            int idx = 0;
            while (idx + 1 < BoomSrc.Length && BoomSrc[idx + 1] <= srcFrame) idx++;
            if (srcFrame > BoomSrc[BoomSrc.Length - 1] + 4f) { Destroy(gameObject); return; }
            boom.sprite = boomSprs[Mathf.Min(idx, boomSprs.Length - 1)];
            float w = boom.sprite.bounds.size.x;
            boom.transform.localScale = Vector3.one * (blastRadiusX * 2.6f / w);
            boom.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            boom.color = Color.white;
            return;
        }
        float k = Mathf.Clamp01(boomT / 0.45f);
        boom.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        boom.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, blastRadiusX * 1.1f, k);
        boom.color = new Color(1f, 1f, 1f, 1f - k);
        if (k >= 1f) Destroy(gameObject);
    }

    // ---------------- savu ----------------

    void EmitSmoke(float dt, Vector3 at, int order)
    {
        smokeT += dt;
        while (smokeT >= 0.04f)
        {
            smokeT -= 0.04f;
            var go = new GameObject("Savu");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = softSpr; sr.sortingOrder = order;
            go.transform.position = at + new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);
            puffs.Add(new Puff { go = go, sr = sr, v = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(1.2f, 2.0f), 0f), life = Random.Range(0.6f, 1.0f), s0 = Random.Range(0.15f, 0.25f), s1 = Random.Range(0.55f, 0.85f) });
        }
    }

    void UpdatePuffs(float dt)
    {
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var p = puffs[i];
            p.age += dt;
            if (p.go == null || p.age >= p.life) { if (p.go != null) Destroy(p.go); puffs.RemoveAt(i); continue; }
            float k = p.age / p.life;
            p.go.transform.position += p.v * dt;
            p.go.transform.localScale = Vector3.one * Mathf.Lerp(p.s0, p.s1, k);
            float g = Mathf.Lerp(0.25f, 0.7f, k);
            p.sr.color = new Color(g, g, g, (1f - k) * 0.7f);
        }
    }

    void OnDestroy()
    {
        foreach (var p in puffs) if (p.go != null) Destroy(p.go);
        if (lightSr != null) Destroy(lightSr.gameObject);
        if (lighter != null) { lighter.Scripted = false; if (lighter.BodyRenderer != null) lighter.BodyRenderer.enabled = true; }
    }

    // ---------------- kuvat ----------------

    static void EnsureSprites()
    {
        if (canSpr != null) return;
        string[] order = { "kierto_a0", "kierto_a1", "kierto_a2", "kierto_a3", "kierto_a4", "kierto_a5", "kierto_a6", "kierto_a7", "kierto_b1", "kierto_b2", "kierto_b4", "kierto_b5" };
        var list = new List<Sprite>();
        foreach (var n in order) { var sp = LoadSprite(n, new Vector2(0.5f, 0.5f)); if (sp != null) list.Add(sp); }
        spinSprs = list.Count > 0 ? list.ToArray() : null;
        var bl = new List<Sprite>();
        for (int i = 0; i < BoomSrc.Length; i++) { var sp = LoadSprite("boom_" + i.ToString("000"), new Vector2(0.5f, 0.5f)); if (sp == null) { bl.Clear(); break; } bl.Add(sp); }
        boomSprs = bl.Count > 0 ? bl.ToArray() : null;
        canSpr = LoadSprite("gallona_0", new Vector2(0.5f, 0f));
        if (canSpr == null) canSpr = MakePlaceholderCan();
        if (flameSpr == null) MakePlaceholders();
    }

    static Sprite MakePlaceholderCan()
    {
        return Make(48, 64, (x, y, w, h) =>
        {
            bool bodyPx = x >= 6 && x < w - 6 && y >= 2 && y < h - 14;
            bool spout = x >= w - 18 && x < w - 8 && y >= h - 14 && y < h - 4;
            bool handle = x >= 10 && x < w - 20 && y >= h - 10 && y < h - 4 && !(x >= 16 && x < w - 26 && y >= h - 8 && y < h - 6);
            if (!(bodyPx || spout || handle)) return new Color32(0, 0, 0, 0);
            bool edge = x == 6 || x == w - 7 || y == 2 || (bodyPx && (x == 7 || x == w - 8));
            bool cross = Mathf.Abs(x - y * w / (float)h) < 2f || Mathf.Abs((w - x) - y * w / (float)h) < 2f;
            if (edge) return new Color32(70, 15, 10, 255);
            if (cross && bodyPx) return new Color32(150, 35, 25, 255);
            return new Color32(200, 55, 40, 255);
        });
    }

    static void MakePlaceholders()
    {
        flameSpr = Make(24, 32, (x, y, w, h) =>
        {
            float cx = (x - w * 0.5f) / (w * 0.5f), cy = y / (float)h;
            float width = Mathf.Lerp(1f, 0f, cy);
            if (Mathf.Abs(cx) > width * 0.9f) return new Color32(0, 0, 0, 0);
            return cy < 0.5f ? new Color32(255, 220, 80, 255) : new Color32(255, 130, 30, 255);
        });
        boomSpr = Make(128, 128, (x, y, w, h) =>
        {
            float dx = (x - w * 0.5f) / (w * 0.5f), dy = (y - h * 0.5f) / (h * 0.5f);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 1f) return new Color32(0, 0, 0, 0);
            if (d < 0.35f) return new Color32(255, 245, 190, 255);
            if (d < 0.7f) return new Color32(255, 150, 40, 235);
            return new Color32(120, 40, 20, 170);
        });
        // pehmeä savupallo
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var px = new Color32[32 * 32];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
        {
            float dx = (x + 0.5f) / 16f - 1f, dy = (y + 0.5f) / 16f - 1f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            px[y * 32 + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
        }
        tex.SetPixels32(px); tex.Apply();
        softSpr = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    static Sprite Make(int w, int h, System.Func<int, int, int, int, Color32> f)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = f(x, y, w, h);
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 64f);
    }
}
