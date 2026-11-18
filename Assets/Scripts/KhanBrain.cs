using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Khan (Tsingiskaani, Muurin tornin katon pomo). Tavallinen tekoäly hoitaa kävelyn, juoksun ja iskut (jabi, suora, korkea/matala potku, kierrepotku); MongolBrainin liike: syöksy.
/// Erikoisliike LOHIKÄÄRMEEN HENKÄYS: hidas lataus (vedon kuvat kiihtyvät), sitten nopea purkaus: lohikäärmeen pää ilmestyy ja suusta syöksyy tulta (koodilla, liekkipalat videosta)
/// niin kauas kuin ruutua näkyy ja yli; lopuksi lohikäärmeen pää irtoaa ja lentää ruudun poikki.
/// </summary>
public class KhanBrain : MongolBrain, IEnemyBrain
{
    [Header("Lohikäärmeen henkäys")]
    public Move special;
    public Sprite[] flames; public Sprite head;
    [Tooltip("Erikoisliikkeen kuvaindeksit: tuli alkaa (suu auki) ja päättyy (pää hajoaa liekeiksi).")] public int fireFrom = 48, fireTo = 84;
    [Tooltip("Suun paikka Khanin jaloista (maailmayksikköä, eteenpäin / ylös).")] public Vector2 mouth = new Vector2(1.6f, 2.75f);
    public float specialCooldown = 14f, specialChance = 0.5f, specialMinRange = 2.5f;
    public int fireDamage = 7, headDamage = 26; public float fireTick = 0.22f, beamHalfDepth = 1.15f, flameSpeed = 46f, headSpeed = 24f;
    [Tooltip("Tulen minimipituus (yksikköä) ruudun leveydestä riippumatta.")] public float minBeam = 30f;

    [Header("Puuska (purkausliike, ei pysäytettävissä)")]
    public Move gust;
    [Tooltip("Puuskan kuvaindeksi, jossa purkaus tapahtuu (käsi ojentuu).")] public int gustBlast = 59;
    public float gustCooldown = 17f, gustChance = 0.45f, gustRange = 13f, gustLaunch = 36f, gustUp = 7f, chainBreathChance = 0.65f; public int gustDamage = 14;
    bool gustActive, gustFired, chainPending; float chainAt, nextGust = 6f, windAcc; AudioClip gustBlastClip;

    [Header("Erikoiskombo: jabi, takasuora, etupotku, alapotku, koukku, pitkä suora, kierrepotku, hyppypotku")]
    public Move[] combo;
    public float comboCooldown = 20f, comboChance = 0.4f, comboStartRange = 3.4f;
    static readonly Dictionary<string, AudioClip> voices = new Dictionary<string, AudioClip>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetVoices() { voices.Clear(); }
    void Say(string n) { if (e == null || e.Audio == null) return; if (!voices.TryGetValue(n, out var c)) { c = Resources.Load<AudioClip>("Sfx/Khan/" + n); voices[n] = c; } if (c != null) e.Audio.PlayOneShot(c, 1f); }
    bool wokeSaid;
    bool comboActive; int comboIdx = -1; float nextCombo = 5f, comboLast;

    bool specialActive, fireOn, headSent; float fireT, nextTick, nextSpecial = 4f, flameAcc;
    public new bool ArmorNow => specialActive || gustActive || comboActive;   // henkäys ja puuska eivät katkea iskuihin
    public new bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    protected override void Update()
    {
        base.Update();
        if (e == null || e.IsDead) { chainPending = false; comboActive = false; return; }
        if (!wokeSaid && e.IsAwake) { wokeSaid = true; Say("khan_sotahuuto"); }   // sotahuuto, kun taistelu alkaa
        if (comboActive && cur == null && Time.time - comboLast > 0.4f) { comboActive = false; comboIdx = -1; }   // kombo katkesi (kaatuminen tms.)
        // ketju: puuskan perään lohikäärmeen henkäys
        if (chainPending && cur == null && Time.time >= chainAt && e.Hero != null && e.BrainIdleOrChase) { chainPending = false; StartSpecial(); return; }
        if (cur != null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || Time.time < nextAny) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        // erikoiskombo: lähietäisyydellä
        if (combo != null && combo.Length > 0 && Time.time >= nextCombo && dx <= comboStartRange && dy <= e.depthTolerance + 0.5f && Random.value < comboChance * Time.deltaTime * 3f)
        {
            nextCombo = Time.time + comboCooldown * Random.Range(0.85f, 1.3f); nextAny = Time.time + 1f;
            comboActive = true; comboIdx = 0; comboLast = Time.time; Begin(combo[0]); if (Random.value < 0.5f) Say("khan_sotahuuto"); return;
        }
        // puuska: lähellä (tai joukko ympärillä), ei pysäytettävissä
        if (gust != null && gust.sprites != null && gust.sprites.Length > 0 && Time.time >= nextGust && dx <= gustRange && Random.value < gustChance * Time.deltaTime * 3f)
        {
            nextGust = Time.time + gustCooldown * Random.Range(0.85f, 1.3f); nextAny = Time.time + 1f;
            gustActive = true; gustFired = false; windAcc = 0f; Begin(gust); Say("khan_henkays_tuuli"); return;
        }
        if (special == null || special.sprites == null || special.sprites.Length == 0 || Time.time < nextSpecial) return;
        if (dx < specialMinRange || dy > beamHalfDepth * 0.8f) return;       // tuli osuu vain samalle syvyydelle
        if (Random.value > specialChance * Time.deltaTime * 4f) return;
        StartSpecial();
    }

    void StartSpecial()
    {
        nextSpecial = Time.time + specialCooldown * Random.Range(0.85f, 1.25f);
        specialActive = true; fireOn = false; headSent = false; fireT = 0f; flameAcc = 0f; nextTick = 0f;
        Begin(special); Say("khan_henkays_lohikaarme");
    }

    Vector3 Mouth() { var s = Mathf.Max(0.1f, e.visualScale); return transform.position + new Vector3(dirSign * mouth.x * s, mouth.y * s, 0f); }

    float BeamLength()
    {
        float len = minBeam; var cam = Camera.main;
        if (cam != null && cam.orthographic) len = Mathf.Max(len, cam.orthographicSize * 2f * cam.aspect + 12f);   // ruudun leveys + yli
        return len;
    }

    public new void Tick(float dt)
    {
        base.Tick(dt);
        if (comboActive) comboLast = Time.time;
        if (cur == null && comboActive)   // kombon seuraava isku heti edellisen perään
        {
            comboIdx++;
            if (comboIdx < combo.Length && !e.IsDead && e.Hero != null) { Begin(combo[comboIdx]); return; }
            comboActive = false; comboIdx = -1;
        }
        if (cur == null)
        {
            if (gustActive)   // puuska päättyi: usein henkäys perään
            {
                gustActive = false;
                if (!e.IsDead && Random.value < chainBreathChance && special != null) { chainPending = true; chainAt = Time.time + 0.3f; }
            }
            specialActive = false; return;
        }
        if (cur == gust) { TickGust(dt); return; }
        if (cur != special) { specialActive = false; return; }
        int i = System.Array.IndexOf(cur.sprites, CurrentSprite);
        if (i >= fireFrom && i < fireTo)
        {
            if (!fireOn) { fireOn = true; fireT = 0f; CameraFollow.Shake(0.25f, 2.4f); }
            fireT += dt; Breathe(dt);
        }
        if (i >= fireTo && !headSent) { headSent = true; LaunchHead(); }
    }

    // ---- puuska: tuulta piirretään koodilla (lataus: pyörre Khanin ympärillä, purkaus: iskuaalto ja tuulenvihurit ruudun päätyihin); kaikki lentävät laitoihin ----
    void TickGust(float dt)
    {
        int i = System.Array.IndexOf(cur.sprites, CurrentSprite); if (i < 0) return;
        Vector3 c = transform.position + new Vector3(0f, 1.7f, 0f);
        if (i < gustBlast)
        {
            if (i >= 6) { windAcc += dt * (18f + 30f * Mathf.Clamp01((i - 6) / (float)Mathf.Max(1, gustBlast - 6))); while (windAcc >= 1f) { windAcc -= 1f; KhanWind.Swirl(c); } }
            return;
        }
        if (!gustFired) { gustFired = true; Blast(c); }
        windAcc += dt * 55f * Mathf.Clamp01(1f - (i - gustBlast) / (float)Mathf.Max(1, cur.sprites.Length - gustBlast));   // purkauksen jälkituuli vaimenee
        while (windAcc >= 1f) { windAcc -= 1f; KhanWind.Rush(c, Random.value < 0.5f ? 1f : -1f); }
    }

    void Blast(Vector3 c)
    {
        if (gustBlastClip == null) gustBlastClip = Resources.Load<AudioClip>("Sfx/Khan/puuska_rajahdys");
        if (gustBlastClip != null) e.PlayExtraSound(gustBlastClip, 1f);
        CameraFollow.Shake(0.45f, 1.4f);
        KhanWind.Ring(c);
        for (int k = 0; k < 14; k++) { KhanWind.Rush(c, 1f); KhanWind.Rush(c, -1f); }
        float kx = transform.position.x;
        foreach (var pl in Players.All)   // kaikki heroit ruudun päätyihin (Khanin vasemmalla puolella vasemmalle, oikealla oikealle)
        {
            if (pl == null || pl.GameOver) continue;
            float d = pl.transform.position.x >= kx ? 1f : -1f;
            if (Mathf.Abs(pl.transform.position.x - kx) < 0.2f) d = dirSign;
            float srcX = pl.transform.position.x - d;
            if (!pl.TakeKnockdown(gustDamage, srcX, gustLaunch, gustUp, e, true)) pl.TakeHit(gustDamage, srcX, e, true);
        }
        foreach (var en in Enemy.All.ToArray())   // muu porukka (myös takana olevat) samoin
        {
            if (en == null || en == e || !en.isActiveAndEnabled || en.IsDead) continue;
            float d = en.transform.position.x >= kx ? 1f : -1f;
            Enemy.EnemyHits(en, 6, en.transform.position.x - d, true);
            en.Launch(d * gustLaunch, gustUp + 2f);
        }
    }

    void Breathe(float dt)
    {
        Vector3 m = Mouth(); float len = BeamLength();
        // liekkipalat: suusta eteenpäin, kasvavat ja häipyvät; elinaika riittää ruudun yli
        if (flames != null && flames.Length > 0)
        {
            flameAcc += dt * 70f;
            while (flameAcc >= 1f)
            {
                flameAcc -= 1f;
                FlamePuff.Spawn(flames[Random.Range(0, flames.Length)], m + new Vector3(0f, Random.Range(-0.35f, 0.35f), 0f), dirSign, flameSpeed * Random.Range(0.85f, 1.15f), len / flameSpeed + 0.35f, Mathf.RoundToInt(-transform.position.y * 100f) + 60);
            }
        }
        // vahinko: palkki kasvaa suusta eteenpäin
        if (Time.time < nextTick) return;
        nextTick = Time.time + fireTick;
        float reach = Mathf.Min(len, flameSpeed * fireT + 2f);
        foreach (var pl in Players.All)
        {
            if (pl == null || pl.GameOver || pl.AirHeight > 2.2f) continue;
            Vector3 p = pl.transform.position;
            float ahead = (p.x - m.x) * dirSign;
            if (ahead < -0.4f || ahead > reach || Mathf.Abs(p.y - transform.position.y) > beamHalfDepth) continue;
            pl.TakeHit(fireDamage, m.x, e, true);
        }
    }

    void LaunchHead()
    {
        if (head == null) return;
        var go = new GameObject("Khanin lohikäärmeen pää"); go.transform.position = Mouth();
        var kh = go.AddComponent<KhanHead>();
        kh.owner = e; kh.dir = dirSign; kh.speed = headSpeed; kh.sprite = head; kh.flames = flames; kh.damage = headDamage; kh.depthY = transform.position.y; kh.halfDepth = beamHalfDepth + 0.2f;
        kh.scale = Mathf.Max(0.1f, e.visualScale) * 1.25f;
        CameraFollow.Shake(0.3f, 0.5f);
    }
}

/// <summary>Yksi liekkipala: lentää suuntaan dir, kasvaa ja häipyy.</summary>
public sealed class FlamePuff : MonoBehaviour
{
    SpriteRenderer sr; float dir, speed, life, age, rot, s0, s1; Vector3 baseScale;
    public static void Spawn(Sprite sp, Vector3 pos, float dir, float speed, float life, int order)
    {
        var go = new GameObject("Liekki"); go.transform.position = pos;
        var f = go.AddComponent<FlamePuff>(); f.sr = go.AddComponent<SpriteRenderer>(); f.sr.sprite = sp; f.sr.sortingOrder = order;
        f.dir = dir; f.speed = speed; f.life = life; f.rot = Random.Range(-18f, 18f); f.s0 = Random.Range(0.55f, 0.8f); f.s1 = Random.Range(1.5f, 2.3f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f)); f.sr.flipX = dir < 0f;
    }
    void Update()
    {
        age += Time.deltaTime; if (age >= life) { Destroy(gameObject); return; }
        float u = age / life;
        transform.position += new Vector3(dir * speed * Time.deltaTime, Mathf.Sin(age * 9f + rot) * 0.4f * Time.deltaTime, 0f);
        transform.localScale = Vector3.one * Mathf.Lerp(s0, s1, Mathf.Sqrt(u));
        transform.Rotate(0f, 0f, rot * Time.deltaTime);
        float a = u < 0.08f ? u / 0.08f : (u > 0.78f ? (1f - u) / 0.22f : 1f);
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(a) * 0.95f);
    }
}

/// <summary>Lohikäärmeen pää irtoaa henkäyksen lopussa ja lentää ruudun poikki tulta perässään; osuu heroon kerran (kaataa).</summary>
public sealed class KhanHead : MonoBehaviour
{
    public Enemy owner; public float dir = 1f, speed = 24f, scale = 1.25f, depthY, halfDepth = 1.4f; public Sprite sprite; public Sprite[] flames; public int damage = 26;
    SpriteRenderer sr; float age, acc; readonly HashSet<Object> hit = new HashSet<Object>();
    void Start()
    {
        sr = gameObject.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.flipX = dir < 0f; sr.sortingOrder = Mathf.RoundToInt(-depthY * 100f) + 80;
        transform.localScale = Vector3.one * scale;
    }
    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        float sp = speed * Mathf.Lerp(0.55f, 1.35f, Mathf.Clamp01(age / 0.6f));      // lähtee kiihtyen
        Vector3 p = transform.position; p.x += dir * sp * dt; p.y += Mathf.Sin(age * 7f) * 0.9f * dt; transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 9f) * 5f);
        if (flames != null && flames.Length > 0)
        {
            acc += dt * 45f;
            while (acc >= 1f) { acc -= 1f; FlamePuff.Spawn(flames[Random.Range(0, flames.Length)], p + new Vector3(-dir * 1.0f, Random.Range(-0.5f, 0.5f), 0f), -dir, 4f, 0.55f, sr.sortingOrder - 1); }
        }
        foreach (var pl in Players.All)
        {
            if (pl == null || pl.GameOver || hit.Contains(pl) || pl.AirHeight > 2.6f) continue;
            Vector3 q = pl.transform.position;
            if (Mathf.Abs(q.x - p.x) > 1.6f || Mathf.Abs(q.y - depthY) > halfDepth) continue;
            hit.Add(pl);
            if (!pl.TakeKnockdown(damage, p.x - dir, 16f, 6f, owner, true)) pl.TakeHit(damage, p.x - dir, owner, true);
            CameraFollow.Shake(0.3f, 0.35f);
        }
        var cam = Camera.main; float edge = cam != null ? cam.transform.position.x + dir * (cam.orthographicSize * cam.aspect + 6f) : p.x + dir * 40f;
        if ((p.x - edge) * dir > 0f || age > 6f) Destroy(gameObject);
    }
}

/// <summary>Tuulen piirto koodilla (Khanin puuska): koodilla tehdyt kaarevat tuulenvirrat ja iskuaalto (renkaan kuva).</summary>
public static class KhanWind
{
    static Sprite streak, ring;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { streak = null; ring = null; }

    static Sprite Streak()
    {
        if (streak != null) return streak;
        const int W = 256, H = 24; var t = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float u = x / (W - 1f), v = (y - (H - 1) * 0.5f) / (H * 0.5f);
            float along = Mathf.Sin(Mathf.PI * u); along = along * along;          // kapenee päistä
            float across = Mathf.Exp(-v * v * 5f);
            t.SetPixel(x, y, new Color(1f, 1f, 1f, along * across * (0.55f + 0.45f * u)));
        }
        t.Apply(); streak = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 64f); return streak;
    }
    static Sprite Ring()
    {
        if (ring != null) return ring;
        const int N = 256; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2((N - 1) * 0.5f, (N - 1) * 0.5f)) / (N * 0.5f);
            float a = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.08f, 2f)) * 0.9f + Mathf.Exp(-Mathf.Pow((r - 0.62f) / 0.12f, 2f)) * 0.25f;
            t.SetPixel(x, y, new Color(1f, 1f, 1f, a * Mathf.Clamp01((1f - r) * 8f + 0.1f)));
        }
        t.Apply(); ring = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 64f); return ring;
    }

    /// Lataus: tuulenvirta kaartaa Khanin ympärillä (pyörre).
    public static void Swirl(Vector3 c)
    {
        float ang = Random.value * Mathf.PI * 2f, rad = Random.Range(2.5f, 7f);
        var p = c + new Vector3(Mathf.Cos(ang) * rad * 1.5f, Mathf.Sin(ang) * rad * 0.55f, 0f);
        Wind.Spawn(Streak(), p, c, Random.value > 0.5f ? 7f : -7f, Random.Range(0.7f, 1.1f), Random.Range(0.8f, 1.7f), Random.Range(0.35f, 0.65f), 9000);
    }
    /// Purkaus: vihuri suoraan sivuille ruudun yli.
    public static void Rush(Vector3 c, float dir)
    {
        var p = c + new Vector3(dir * Random.Range(0.5f, 3f), Random.Range(-1.8f, 2.6f), 0f);
        Wind.SpawnLinear(Streak(), p, dir * Random.Range(55f, 85f), Random.Range(0.55f, 0.9f), Random.Range(1.4f, 3.2f), Random.Range(0.45f, 0.8f), 9000);
    }
    public static void Ring(Vector3 c) { Wind.SpawnRing(Ring(), c, 9001); }
}

public sealed class Wind : MonoBehaviour
{
    SpriteRenderer sr; float age, life, alpha; Vector3 vel; Vector3 center; float swirl, grow; bool ringMode;
    public static void Spawn(Sprite sp, Vector3 pos, Vector3 around, float swirlSpeed, float life, float scale, float alpha, int order)
    {
        var w = Make(sp, pos, life, scale, alpha, order); w.center = around; w.swirl = swirlSpeed;
    }
    public static void SpawnLinear(Sprite sp, Vector3 pos, float vx, float life, float scale, float alpha, int order)
    {
        var w = Make(sp, pos, life, scale, alpha, order); w.vel = new Vector3(vx, Random.Range(-0.6f, 0.6f), 0f);
        w.sr.flipX = vx < 0f; w.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-4f, 4f));
    }
    public static void SpawnRing(Sprite sp, Vector3 pos, int order)
    {
        var w = Make(sp, pos, 0.8f, 0.4f, 0.95f, order); w.ringMode = true; w.grow = 34f; w.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
    }
    static Wind Make(Sprite sp, Vector3 pos, float life, float scale, float alpha, int order)
    {
        var go = new GameObject("Tuuli"); go.transform.position = pos;
        var w = go.AddComponent<Wind>(); w.sr = go.AddComponent<SpriteRenderer>(); w.sr.sprite = sp; w.sr.sortingOrder = order; w.life = life; w.alpha = alpha;
        go.transform.localScale = new Vector3(scale, Mathf.Max(0.5f, scale * 0.6f), 1f); return w;
    }
    void Update()
    {
        float dt = Time.deltaTime; age += dt; if (age >= life) { Destroy(gameObject); return; }
        float u = age / life;
        if (ringMode) { float s = 0.4f + grow * u; transform.localScale = new Vector3(s * 1.6f, s * 0.7f, 1f); }
        else if (swirl != 0f)
        {
            Vector3 d = transform.position - center; float a = swirl * dt; float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            d = new Vector3(d.x * ca - d.y * sa * 2.2f, d.y * ca + d.x * sa * 0.45f, 0f) * (1f - 0.55f * dt);   // kaartuu sisäänpäin pyörien (ellipsi)
            transform.position = center + d; transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 90f);
        }
        else transform.position += vel * dt;
        float f = u < 0.15f ? u / 0.15f : (u > 0.6f ? (1f - u) / 0.4f : 1f);
        sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(f) * alpha);
    }
}
