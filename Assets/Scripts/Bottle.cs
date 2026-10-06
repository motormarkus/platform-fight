using System.Collections.Generic;
using UnityEngine;

/// Pullon pitelijä (hero tai punkkari): käden paikka maailmassa, kallistus ja piirtojärjestys.
/// false = käsi ei vielä ylety (pullo jää lattialle).
public interface IBottleHolder
{
    bool BottleGrip(out Vector3 hand, out float rot, out int order);
}

/// <summary>
/// Pullo baaripöydällä. Kun pöytää lyödään, osa pulloista lentää, osa kaatuu ja tippuu lattialle, osa jää heilumaan.
/// Lattialle osuessa osa hajoaa (kuvat 1–6), osa jää ehjänä kyljelleen. Ehjän pullon voi poimia käteen
/// (kiinniottonappi) ja heittää lyöntinapilla: osuu vihuun, kaataa sen ja hajoaa.
/// Kuvat: pullo_*.png 128x96, 0 pystyssä, 1–6 hajoaa.
/// </summary>
public class Bottle : MonoBehaviour
{
    public static readonly List<Bottle> All = new List<Bottle>();
    /// Pelaajan kädessä oleva pullo (null = ei pulloa).
    public static Bottle Held { get; private set; }

    public Sprite[] sprites;
    [Tooltip("Pöytä, jolla pullo seisoo (null = lattialla).")]
    public Crate table;
    [Tooltip("Paikka pöydällä: x pöydän keskeltä, korkeus pöydän pinnasta (yksikköä).")]
    public float tableX, tableTop = 1.24f;
    public float frameTime = 0.06f;
    public int throwDamage = 18;
    public float throwSpeed = 15f;
    [Tooltip("Läiskän laji (Resources/Tahrat/<laji>_*.png): olut, vodka, sininen, likoori.")]
    public string stainKind = "olut";
    [Tooltip("Kiertopiste (keskikohdan korkeus, yks): pullo 0.3, lasi 0.17.")]
    public float pivotY = 0.3f;
    [Tooltip("Kuvan koko (1 = alkuperäinen).")]
    public float scale = 1.05f;
    [Tooltip("Ruoka (kala): makaa lattialla vaakatasossa, ei hajoa, heitetty osuu kevyesti (ei kaada) ja putoaa lattialle.")]
    public bool food;
    [Tooltip("Lasin särkymisäänet (glass1–4), satunnainen järjestys ja voimakkuus.")]
    public AudioClip[] breakSounds;
    [Tooltip("Lennon kuvat eri kulmista (piirretty valo): kaula ylös, ylös-oikea, oikea, alas, vasen, ylös-vasen. Tyhjä = kuvaa käännetään.")]
    public Sprite[] spinSprites;
    static readonly float[] SpinAngles = { 0f, -35f, -90f, 180f, 90f, 30f };
    [Tooltip("Särkymisen viimeinen kuva jää lattialle (esim. shamppanjapullon sirpaleet). Välikuvat = posahdus ilmassa (vain kun osuu ilmassa).")]
    public bool keepDebris;
    float burstH;
    bool softLanding;
    [Tooltip("Hajoaa aina osuessaan lattiaan (esim. ohut viinilasi).")]
    public bool alwaysBreak;   // pöytä nostettiin: valuu lattialle ehjänä
    static int lastSound = -1;

    enum S { OnTable, Wobble, Falling, Lying, Held, Thrown, Breaking }
    S state = S.OnTable;
    SpriteRenderer sr, shadow;
    PlayerController pc;
    IBottleHolder holder;
    bool enemyThrow;
    int seenDisturb;
    float t, height, vy, vx, rot, spin, wobbleT;
    bool fastFall, gripped;
    // heron kädessä: nyrkki piirretään pullon päälle (kopio heron kuvasta maskattuna nyrkin kohdalle)
    SpriteRenderer fistOverlay;
    SpriteMask fistMask;
    Vector3 handPos;
    static Sprite circleSprite;
    int heldOrder;
    readonly HashSet<Object> hitList = new HashSet<Object>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); if (Held == this) Held = null; }

    void Awake()
    {
        PlayerController.SortByFrameNumber(sprites);
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        sr = v.AddComponent<SpriteRenderer>();
        var s = new GameObject("Shadow"); s.transform.SetParent(transform, false);
        shadow = s.AddComponent<SpriteRenderer>();
        shadow.sprite = PlayerController.CreateShadowSprite();
        shadow.color = new Color(0f, 0f, 0f, 0.3f);
        shadow.transform.localScale = new Vector3(0.35f, 0.12f, 1f);
    }

    void Start()
    {
        pc = FindFirstObjectByType<PlayerController>();
        if (table != null) { seenDisturb = table.Disturb; height = tableTop; }
        else { state = S.Lying; rot = food ? 0f : 90f; }
    }

    // ---------------- pelaajan käsi ----------------

    /// Kiinniottonappi: poimi lähin ehjä lattialla oleva pullo.
    public static bool TryPickUp(PlayerController p)
    {
        if (Held != null || p == null) return false;
        Vector3 me = p.transform.position;
        Bottle best = null; float bd = 99f;
        foreach (var b in All)
        {
            if (b == null || b.state != S.Lying) continue;
            Vector3 q = b.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 1.1f || dy > 0.5f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = b; }
        }
        if (best == null) return false;
        best.state = S.Held; best.t = 0f; best.holder = p; best.gripped = false;
        Held = best;
        HitFx.PlayPickup(false);
        return true;
    }

    /// Lyöntinappi pullo kädessä: heitto eteen.
    public static bool ThrowHeld(PlayerController p)
    {
        if (Held == null || p == null) return false;
        var b = Held; Held = null;
        b.Throw(p.transform.position.y, p.FacingRight ? 1f : -1f, false);
        return true;
    }

    /// Ehjä lattialla oleva pullo (vihun haettavaksi).
    public bool CanPickUp => state == S.Lying;

    /// Vihu ottaa pullon (nostokuvat: pullo nousee käden mukana, kun käsi ylettyy).
    public bool TakeBy(IBottleHolder h)
    {
        if (state != S.Lying) return false;
        state = S.Held; t = 0f; holder = h; gripped = false;
        return true;
    }

    /// Heitto pitelijän kädestä (enemy = osuu heroon, muuten vihuihin).
    public void Throw(float groundY, float dir, bool enemy)
    {
        if (holder != null && holder.BottleGrip(out Vector3 hand, out _, out _))
        {
            transform.position = new Vector3(hand.x, groundY, 0f);
            height = Mathf.Clamp(hand.y - groundY - 0.2f, 1.2f, 3.5f);
        }
        else height = 1.9f;
        holder = null;
        if (Held == this) Held = null;
        enemyThrow = enemy;
        vx = dir * throwSpeed; vy = 1.5f;
        spin = -dir * 900f; hitList.Clear();
        state = S.Thrown; t = 0f;
    }

    /// Pitelijä sai osuman: pullo putoaa.
    public void Drop()
    {
        if (state != S.Held) return;
        if (Held == this) Held = null;
        holder = null;
        vx = Random.Range(-1.5f, 1.5f); vy = 2f; spin = Random.Range(-500f, 500f);
        fastFall = false; state = S.Falling; t = 0f;
    }

    /// Pelaaja saa osuman: pullo putoaa kädestä.
    public static void DropHeld()
    {
        if (Held != null) Held.Drop();
    }

    // ---------------- päivitys ----------------

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        Vector3 p = transform.position;
        switch (state)
        {
            case S.OnTable:
            case S.Wobble:
                if (table == null || table.Disturb != seenDisturb)
                {
                    bool gone = table == null || !table.Intact;   // pöytä hajosi tai nostettiin
                    if (table != null) seenDisturb = table.Disturb;
                    if (table != null && table.LastViolent && !(table.Airborne && !table.IsBroken)) { Launch(table.LastHitDir); break; }   // potku: kaikki lentää ilmaan
                    if (table != null && table.carryUpsideDown && table.Airborne && !table.IsBroken)
                    {
                        // pöytä nostetaan ylösalaisin: pullo putoaa lattialle ehjänä
                        float sd = Random.value < 0.5f ? -1f : 1f;
                        vx = sd * Random.Range(0.4f, 1.2f); vy = Random.Range(0.5f, 1.5f); spin = -sd * Random.Range(150f, 300f);
                        fastFall = false; softLanding = true; table = null; state = S.Falling; t = 0f;
                        break;
                    }
                    React(gone);
                    break;
                }
                p = table.transform.position + new Vector3(tableX, 0f, 0f);
                p.y -= 0.01f;
                transform.position = p;
                height = tableTop;
                if (state == S.Wobble)
                {
                    rot = Mathf.Sin(t * 28f) * 14f * Mathf.Max(0f, 1f - t / 0.8f);
                    if (t > 0.8f) { state = S.OnTable; rot = 0f; }
                }
                break;

            case S.Falling:
                p.x += vx * dt; vx = Mathf.MoveTowards(vx, 0f, 0.5f * dt);
                transform.position = p;
                vy -= 30f * dt; height += vy * dt; rot += spin * dt;
                if (height <= 0f)
                {
                    height = 0f;
                    float breakChance = fastFall ? 0.7f : 0.4f;
                    if (!food && (alwaysBreak || (!softLanding && Random.value < breakChance))) Shatter();
                    else { softLanding = false; state = S.Lying; rot = food ? Random.Range(-8f, 8f) : (Random.value < 0.5f ? 90f : -90f); t = 0f; }   // jää ehjänä kyljelleen
                }
                break;

            case S.Lying:
                break;

            case S.Held:
                HoldInHand();
                break;

            case S.Thrown:
                p.x += vx * dt;
                transform.position = p;
                vy -= 6f * dt; height = Mathf.Max(0f, height + vy * dt);
                rot += spin * dt;
                if (food)
                {
                    // kala: osuu ja kimpoaa, putoaa lattialle ehjänä
                    if (HitInPath()) { vx = -vx * 0.15f; vy = 2.5f; spin *= 0.3f; fastFall = false; state = S.Falling; t = 0f; }
                    else if (height <= 0.05f || t > 1.0f) { vy = 0f; vx *= 0.3f; fastFall = false; state = S.Falling; t = 0f; }
                }
                else if (HitInPath() || height <= 0.05f || t > 1.0f) Shatter();
                break;

            case S.Breaking:
                if (!keepDebris && t > 1.6f) Destroy(gameObject);
                break;
        }
        ApplyVisual();
    }

    /// Pöytää lyötiin: lentää, kaatuu ja tippuu tai jää heilumaan.
    /// Potku pöytään (tai pöytä hajoaa rajusti): pullo lentää ilmaan potkun suuntaan.
    void Launch(float dir)
    {
        float side = dir != 0f ? dir : (Random.value < 0.5f ? -1f : 1f);
        t = 0f;
        vx = side * Random.Range(2.5f, 6f) + Random.Range(-1f, 1f); vy = Random.Range(6f, 10f);
        spin = -side * Random.Range(600f, 1200f); fastFall = true;
        table = null;
        state = S.Falling;
    }

    void React(bool tableGone)
    {
        float r = Random.value;
        float side = Random.value < 0.5f ? -1f : 1f;
        t = 0f;
        if (!tableGone && r < 0.3f) { state = S.Wobble; return; }
        if (r < 0.65f)
        {
            // lentää
            vx = side * Random.Range(2f, 4.5f); vy = Random.Range(3.5f, 6f);
            spin = -side * Random.Range(400f, 900f); fastFall = true;
        }
        else
        {
            // kaatuu ja tippuu reunalta
            vx = side * Random.Range(0.8f, 1.6f); vy = Random.Range(0.5f, 1.5f);
            spin = -side * Random.Range(150f, 300f); fastFall = false;
        }
        table = null;
        state = S.Falling;
    }

    bool HitInPath()
    {
        Vector3 me = transform.position;
        if (enemyThrow)
        {
            // vihun heittämä: osuu vain heroon
            if (pc == null || hitList.Contains(pc)) return false;
            Vector3 q = pc.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.7f || Mathf.Abs(q.y - me.y) > 0.45f || height > 3.6f) return false;
            hitList.Add(pc);
            if (pc.TakeHit(throwDamage, me.x - Mathf.Sign(vx)))
            {
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.2f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                return true;
            }
            return false;
        }
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead || hitList.Contains(e)) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.7f || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            hitList.Add(e);
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vx), !food))
            {
                HitFx.OnHit(true);
                HitSpark.Spawn(new Vector3(q.x, q.y + 2.2f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                return true;
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit || hitList.Contains(c)) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > 0.9f || Mathf.Abs(q.y - me.y) > 0.5f || height > 1.6f) continue;
            hitList.Add(c);
            c.TakeHit(10, me.x - Mathf.Sign(vx));
            return true;
        }
        return false;
    }

    void Shatter()
    {
        burstH = height;
        state = S.Breaking; t = 0f; height = 0f; rot = 0f;
        // lattialle pudonnut (ei osumaa ilmassa): suoraan sirpaleisiin
        if (keepDebris && (burstH < 0.4f || sprites.Length <= 2)) t = frameTime * Mathf.Max(0, sprites.Length - 2);
        HitFx.OnBreak(0f);   // vain lasiääni: ei osumapysäytystä (monta pulloa peräkkäin nyki)
        if (breakSounds != null && breakSounds.Length > 0)
        {
            int i = Random.Range(0, breakSounds.Length);
            if (breakSounds.Length > 1 && i == lastSound) i = (i + 1 + Random.Range(0, breakSounds.Length - 1)) % breakSounds.Length;
            lastSound = i;
            HitFx.PlayClip(breakSounds[i], Random.Range(0.55f, 1f));
        }
        BarStain.SpawnAt(transform.position, stainKind);
    }

    void HoldInHand()
    {
        if (holder == null || (holder is Object o && o == null)) { holder = null; Drop(); return; }
        // kädessä: kuva seuraa kättä joka framessa, kiertopiste (pullon keskiosa) kädessä
        if (!holder.BottleGrip(out Vector3 hand, out float r, out heldOrder)) return;   // käsi ei vielä ylety: lattialla
        Component c = holder as Component;
        float gy = c != null ? c.transform.position.y : hand.y;
        transform.position = new Vector3(hand.x, gy - 0.01f, 0f);
        height = hand.y - gy - (pivotY - 0.04f) * scale;
        rot = r;
        gripped = true;
        handPos = hand;
    }

    // pitelijän kuva on jo päivitetty tässä vaiheessa: pullo ja nyrkki samaan kuvaan (ei viivettä)
    void LateUpdate()
    {
        if (state != S.Held) return;
        HoldInHand();
        if (state == S.Held) ApplyVisual();
    }

    /// Nyrkki pullon päälle: heron nykyinen kuva uudestaan, näkyvissä vain nyrkin kohdalla (pullo näyttää olevan kädessä).
    void UpdateFistOverlay(bool on, int bottleOrder)
    {
        var body = on ? ((PlayerController)holder).BodyRenderer : null;
        if (body == null || !body.enabled || body.sprite == null)
        {
            if (fistOverlay != null) { fistOverlay.enabled = false; fistMask.enabled = false; }
            return;
        }
        if (fistOverlay == null)
        {
            var o = new GameObject("Nyrkki");
            fistOverlay = o.AddComponent<SpriteRenderer>();
            fistOverlay.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            var m = new GameObject("NyrkkiMaski");
            fistMask = m.AddComponent<SpriteMask>();
            fistMask.sprite = CircleSprite();
            fistMask.isCustomRangeActive = true;
        }
        fistOverlay.enabled = true; fistMask.enabled = true;
        var bt = body.transform;
        fistOverlay.transform.SetPositionAndRotation(bt.position, bt.rotation);
        fistOverlay.transform.localScale = bt.lossyScale;
        fistOverlay.sprite = body.sprite;
        fistOverlay.flipX = body.flipX;
        fistOverlay.color = body.color;
        fistOverlay.sortingLayerID = body.sortingLayerID;
        fistOverlay.sortingOrder = bottleOrder + 1;
        fistMask.transform.position = handPos;
        fistMask.transform.localScale = Vector3.one * 0.34f;   // nyrkin kokoinen ympyrä (halkaisija yks)
        fistMask.frontSortingLayerID = fistMask.backSortingLayerID = body.sortingLayerID;
        fistMask.frontSortingOrder = bottleOrder + 1;
        fistMask.backSortingOrder = bottleOrder;
    }

    static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                px[y * n + x] = dx * dx + dy * dy <= 0.25f ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px); tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 yksikön halkaisija
        return circleSprite;
    }

    void OnDestroy()
    {
        if (fistOverlay != null) Destroy(fistOverlay.gameObject);
        if (fistMask != null) Destroy(fistMask.gameObject);
    }

    void ApplyVisual()
    {
        if (sprites == null || sprites.Length == 0) return;
        float drawRot = state == S.Breaking ? 0f : rot;
        int bi = 0;
        if (state == S.Breaking)
        {
            bi = Mathf.Min(1 + (int)(t / frameTime), sprites.Length - 1);
            sr.sprite = sprites[bi];
            sr.color = keepDebris ? Color.white : new Color(1f, 1f, 1f, Mathf.Clamp01((1.6f - t) / 0.5f));
        }
        else if (spinSprites != null && spinSprites.Length >= 6 && !food && state != S.OnTable && state != S.Wobble && state != S.Held)
        {
            // lähin piirretty kulma, loppu käännetään
            float a = Mathf.Repeat(rot + 180f, 360f) - 180f; int best = 0; float bd = 999f;
            for (int i = 0; i < SpinAngles.Length; i++) { float d = Mathf.Abs(Mathf.DeltaAngle(a, SpinAngles[i])); if (d < bd) { bd = d; best = i; } }
            sr.sprite = spinSprites[best];
            drawRot = Mathf.DeltaAngle(SpinAngles[best], a);
        }
        else sr.sprite = sprites[0];
        // kierto pullon keskikohdan ympäri (kuva alareunan keskellä, pullo n. 0.58 yks korkea)
        var q = Quaternion.Euler(0f, 0f, drawRot);
        Vector3 c = new Vector3(0f, pivotY * scale, 0f);
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.localRotation = q;
        sr.transform.localPosition = food
            ? new Vector3(0f, height + (sr.sprite != null ? sr.sprite.bounds.extents.y * scale * 0.55f : 0.2f), 0f)   // kala (keskipiste): lattian päällä
            : new Vector3(0f, height - 0.04f * scale, 0f) + c - q * c;
        if (state == S.Breaking && keepDebris && bi < sprites.Length - 1) sr.transform.localPosition += new Vector3(0f, burstH * 0.8f, 0f);   // posahdus ilmassa
        int order;
        if ((state == S.OnTable || state == S.Wobble) && table != null) order = table.SortOrder + 1;
        else if (state == S.Held && gripped) order = heldOrder;   // pitelijän (käden) taakse
        else order = Mathf.RoundToInt(-transform.position.y * 100f);
        sr.sortingOrder = order;
        if (state == S.Breaking && keepDebris) { sr.sortingOrder = bi < sprites.Length - 1 ? Mathf.RoundToInt(-transform.position.y * 100f) + 5 : -7990; }
        shadow.sortingOrder = order - 1;
        shadow.enabled = state != S.OnTable && state != S.Wobble && state != S.Breaking && (state != S.Held || !gripped);
        UpdateFistOverlay(state == S.Held && gripped && holder is PlayerController, order);
    }
}
