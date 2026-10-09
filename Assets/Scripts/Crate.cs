using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puulaatikko: hajoaa iskuista (oletuksena kolme osumaa). Pelaaja voi nostaa sen pään yli (O)
/// ja heittää (lyönti tai potku). Lentävä laatikko kaataa osuessaan vihollisen ja hajoaa.
/// Tynnyri (breakable = false) ei hajoa: se kaataa kaikki lentoreitillään ja jää maahan uudelleen heitettäväksi.
/// Myös pomo voi heittää tynnyrin; silloin se osuu pelaajaan.
/// </summary>
public class Crate : MonoBehaviour
{
    public static readonly List<Crate> All = new List<Crate>();

    [Tooltip("laatikko.png: 0 ehjä, 1 halkeillut, 2 rikki, 3 hajoaa, 4 romukasa.")]
    public Sprite[] sprites;
    [Tooltip("Sirpaleräjähdys (laatikko_sirpaleet.png), näytetään hajotessa.")]
    public Sprite burstSprite;
    [Tooltip("Hajoamisen kuvasarja (esim. pöytä katkeaa ja kaatuu), toistetaan Breaking-tilassa. Tyhjä = sprites 3–4.")]
    public Sprite[] breakSprites;
    public float breakFrameTime = 0.09f;
    [Tooltip("Varjon leveys (yksikköä).")]
    public float shadowWidth = 1.4f;
    [Tooltip("Kannossa näin paljon alemmas (yks): pöytä kannetaan kannen alta, jalat ylöspäin ei jää ilmaan.")]
    public float carryLower = 0f;
    [Tooltip("Kannetaan ylösalaisin (esim. salin pöytä): kääntyy nostossa, jalat ylöspäin.")]
    public bool carryUpsideDown;
    float flipAngle;
    float spinSink;   // pyörivän kuvan alin kohta keskikohtaa alempana kuin pystyssä (osuu maahan aiemmin)
    [Tooltip("Heitettynä lentää vihujoukon läpi ja kaataa kaikki tieltään (pöydät); hajoaa vasta maahan osuessa.")]
    public bool plowThrough;
    int plowHits;
    bool bounced;      // heitetty pöytä on jo kimmonnut kerran lattiasta
    float bounceRot;   // kimmotessa pöytä pyörähtää kerran
    bool TableFlight => thrown && breakable && plowThrough && carryUpsideDown;   // ylösalaisin kannettu salin pöytä: kimpoaa kerran (muut pöydät kuten ennen)
    [Tooltip("Toisen laatikon päällä (pino). Kun alempi lyödään, nostetaan tai hajoaa, tämä putoaa.")]
    public Crate stackedOn;
    [Tooltip("Pinossa: korkeus alemman laatikon päällä (yks).")]
    public float stackHeight = 1.5f;
    [Tooltip("Hajoamisäänet (puu1, puu2): soi vain kun esine hajoaa, ei osumista.")]
    public AudioClip[] breakSounds;
    static int lastBreakSound = -1;
    /// Kasvaa aina, kun esinettä lyödään, nostetaan tai se hajoaa (pöydän pullot reagoivat).
    public int Disturb { get; private set; }
    public bool Intact => state == State.Idle;
    public bool IsBroken => state == State.Breaking;
    public int SortOrder => body != null ? body.sortingOrder : 0;
    /// Viimeisimmän iskun suunta: +1 isku tuli vasemmalta (esine lentää oikealle), -1 oikealta, 0 = ei tiedossa.
    public float LastHitDir { get; private set; }
    /// Oliko viimeisin isku pelaajan potku.
    public bool LastHitKick { get; private set; }
    /// Oliko viimeisin häiriö raju (potku, heitetty pöytä, päälle lentävä vihu): pöydän annokset lentävät kauas.
    public bool LastViolent { get; private set; }
    /// Kannetaan tai lentää (heitetty / pudotettu).
    public bool Airborne => state == State.Carried || state == State.Flying;
    /// Korkeus maasta (kannossa ja lennossa).
    public float Height => height;
    public SpriteRenderer body;
    public SpriteRenderer shadow;

    [Header("Tynnyri")]
    [Tooltip("Kyljellään olevan tynnyrin kuvat (tynnyri_pyorii.png): pyöriminen ja kanto. Kuva 0 = kantoasento.")]
    public Sprite[] rollSprites;
    [Tooltip("Todennäköisyys, että pysähtynyt tynnyri pomppaa takaisin pystyyn.")]
    [Range(0f, 1f)] public float standUpChance = 0.2f;
    [Tooltip("Vierimisen hidastuminen (yksikköä/s²).")]
    public float rollFriction = 5f;
    [Tooltip("Lyöty tynnyri lähtee vierimään tällä nopeudella.")]
    public float kickRollSpeed = 9f;

    [Header("Kestävyys")]
    [Tooltip("Pois päältä = tynnyri: ei hajoa iskuista eikä heitosta.")]
    public bool breakable = true;
    public int hitsToBreak = 3;
    [Header("Heitto")]
    public int throwDamage = 16;
    [Tooltip("Kuinka läheltä lentävä laatikko osuu viholliseen (x ja syvyys).")]
    public float hitRadiusX = 0.9f, hitRadiusY = 0.5f;
    [Header("Hajoaminen")]
    [Tooltip("Kuinka kauan romukasa jää maahan ennen häivytystä (s).")]
    public float debrisTime = 1.5f;
    public float burstTime = 0.35f;
    [Tooltip("Todennäköisyys, että laatikosta löytyy seteli.")]
    [Range(0f, 1f)] public float moneyChance = 0.5f;
    [Tooltip("Todennäköisyys, että laatikosta löytyy energiajuoma (stamina).")]
    [Range(0f, 1f)] public float energyChance = 0.3f;
    public int energyStamina = 40;

    [Header("Spriten sijoitus")]
    [Tooltip("Kuvan koko (1 = alkuperäinen). Pohja pysyy maassa.")]
    public float visualScale = 1f;
    [Tooltip("Ruudussa on 10 px tyhjää laatikon alla (200 px/yksikkö).")]
    public float footOffset = 0.05f;

    enum State { Idle, Carried, Flying, Rolling, Breaking }
    State state = State.Idle;
    int hits;
    float height, verticalVel, stateTime, shakeTimer, shakeUntil;
    Vector2 vel;
    bool thrown;            // heitetty (hajoaa osuessaan) vai vain pudotettu (jää ehjäksi)
    int carriedOrder;
    Enemy thrownBy;         // vihollisen heittämä osuu pelaajaan, ei vihollisiin
    int enemyDamage;
    readonly HashSet<Enemy> alreadyHit = new HashSet<Enemy>();
    float groundHeight;
    bool lying;             // tynnyri kyljellään
    float rollDist;         // vieritty matka (kuvan valinta)
    bool HasRoll => !breakable && rollSprites != null && rollSprites.Length > 0;
    SpriteRenderer burst;
    PlayerController player;

    /// Voiko laatikon nostaa (ehjä ja maassa).
    public bool CanPickUp => state == State.Idle;
    /// Voiko laatikkoon lyödä.
    public bool CanBeHit => state == State.Idle;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        PlayerController.SortByFrameNumber(sprites);
        PlayerController.SortByFrameNumber(rollSprites);
        PlayerController.SortByFrameNumber(breakSprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        groundHeight = TargetGround();
    }

    /// Pelaajan isku osuu laatikkoon.
    public bool TakeHit(int damage, float attackerX = float.NaN)
    {
        if (!CanBeHit) return false;
        Disturb++;
        LastHitDir = float.IsNaN(attackerX) ? 0f : (transform.position.x >= attackerX ? 1f : -1f);
        LastHitKick = PlayerController.AttackIsKick;
        LastViolent = LastHitKick;
        shakeTimer = 0.15f;
        shakeUntil = HitFx.ShakeUntil(false);
        if (!breakable)
        {
            // tynnyri kaatuu kyljelleen ja lähtee vierimään iskun suuntaan, kaataa vihut tieltään
            if (HasRoll && !float.IsNaN(attackerX))
            {
                lying = true;
                vel = new Vector2((transform.position.x >= attackerX ? 1f : -1f) * kickRollSpeed, 0f);
                thrown = true;
                thrownBy = null;
                alreadyHit.Clear();
                state = State.Rolling;
                stateTime = 0f;
            }
            return true;
        }
        hits++;
        if (hits >= hitsToBreak) Break();
        return true;
    }

    /// Lentävä vihu tai muu iso osuma: hajoaa heti.
    public void Smash()
    {
        if (!breakable || !CanBeHit) return;
        hits = hitsToBreak;
        LastViolent = true;
        HitFx.OnBreak(0.06f);
        Break();
    }

    public void PickUp()
    {
        Disturb++;
        if (HasRoll) { lying = true; rollDist = 0f; }   // tynnyriä kannetaan vaaka-asennossa
        state = State.Carried;
        stateTime = 0f;
    }

    /// Pelaaja kantaa: paikka (x, syvyys), korkeus maasta ja piirtojärjestys (pelaajan päällä).
    public void SetCarried(Vector3 pos, float lift, int order)
    {
        if (state != State.Carried) return;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        height = lift;
        carriedOrder = order;
    }

    /// Heitto: lento vaakanopeudella vx ja ylöspäin up. Hajoaa osuessaan viholliseen tai maahan.
    public void Throw(float vx, float up, Enemy by = null, int damage = 0)
    {
        if (state != State.Carried) return;
        vel = new Vector2(vx, 0f);
        verticalVel = up;
        thrown = true;
        thrownBy = by;
        enemyDamage = damage;
        alreadyHit.Clear();
        alreadyHitPlayer = false;
        plowHits = 0;
        bounced = false; bounceRot = 0f;
        // pään päältä heitetty pikkupöytä (telkkaripöytä ym.) lähtee vähän matalammalta, ettei lennä heti vihujen yli
        if (carryLower > 0f && !carryUpsideDown && by == null) height = Mathf.Max(1.2f, height - 0.6f);
        state = State.Flying;
        stateTime = 0f;
    }

    /// Pelaaja pudottaa laatikon (esim. saa osuman): putoaa ehjänä maahan.
    public void Drop()
    {
        if (state != State.Carried) return;
        vel = Vector2.zero;
        verticalVel = 0f;
        thrown = false;
        thrownBy = null;
        state = State.Flying;
        stateTime = 0f;
    }

    void Break()
    {
        Disturb++;
        if (breakSounds != null && breakSounds.Length > 0)
        {
            int i = Random.Range(0, breakSounds.Length);
            if (breakSounds.Length > 1 && i == lastBreakSound) i = (i + 1) % breakSounds.Length;
            lastBreakSound = i;
            HitFx.PlayClip(breakSounds[i], Random.Range(0.85f, 1f));
        }
        state = State.Breaking;
        stateTime = 0f;
        height = 0f;
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.12f);
        if (Random.value < moneyChance) Pickup.SpawnMoney(transform.position + new Vector3(0.3f, -0.05f, 0f), 1, false);
        if (Random.value < energyChance) Pickup.SpawnEnergy(transform.position + new Vector3(-0.3f, -0.05f, 0f), energyStamina);
        if (burstSprite != null)
        {
            var go = new GameObject("Sirpaleet");
            go.transform.SetParent(transform, false);
            burst = go.AddComponent<SpriteRenderer>();
            burst.sprite = burstSprite;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        stateTime += dt;
        if (shakeTimer > 0f) shakeTimer -= dt;
        groundHeight = Mathf.MoveTowards(groundHeight, TargetGround(), 6f * dt);   // kannettaessa sama kuin pelaajalla
        if (stackedOn != null && state == State.Idle)
        {
            // pinossa: pysyy alemman päällä niin kauan kuin se on paikallaan ehjänä
            if (stackedOn.state == State.Idle && stackedOn.height <= 0.01f && stackedOn.shakeTimer <= 0f)
                height = stackHeight * stackedOn.visualScale / 1.2f;
            else
            {
                stackedOn = null;
                vel = new Vector2(Random.Range(-1.2f, 1.2f), 0f);
                verticalVel = 1f;
                thrown = false; thrownBy = null;
                state = State.Flying;          // putoaa ehjänä alas
                stateTime = 0f;
            }
        }

        switch (state)
        {
            case State.Flying:
            {
                Vector3 p = transform.position;
                p.x += vel.x * dt;
                transform.position = p;
                if (thrown) rollDist += Mathf.Abs(vel.x) * dt;   // pyörii lennossa
                verticalVel -= 30f * dt;
                height += verticalVel * dt;
                if (thrown && (thrownBy != null ? HitPlayerInPath() : HitEnemyInPath()))
                {
                    if (breakable && plowThrough)
                    {
                        // pöytä: jatkaa joukon läpi hidastuen, hajoaa neljännestä osumasta tai maahan
                        plowHits++;
                        vel.x *= 0.85f;
                        if (plowHits >= 4) { LastViolent = true; Break(); break; }
                    }
                    else if (breakable) { LastViolent = true; Break(); break; }
                    vel.x *= 0.6f;   // tynnyri jatkaa hidastuen ja kaataa seuraavankin
                }
                if (bounced && bounceRot < 360f) bounceRot = Mathf.Min(360f, bounceRot + 1000f * dt);
                if ((TableFlight ? height : height - spinSink) <= 0f && verticalVel <= 0f)
                {
                    height = 0f;
                    if (TableFlight && !bounced && Mathf.Abs(vel.x) > 3f)
                    {
                        // pöytä kimpoaa kerran lattiasta ja pyörähtää ympäri, hajoaa vasta toisella kerralla
                        bounced = true; bounceRot = 0f;
                        verticalVel = Mathf.Max(5.5f, -verticalVel * 0.5f);
                        vel.x *= 0.85f;
                        height = 0.01f;
                        HitFx.OnHitQuiet();
                        shakeTimer = 0.1f;
                        if (CameraFollow.Instance != null) CameraFollow.Shake(0.06f, 0.1f);
                        DustPuff.Spawn(transform.position, Mathf.RoundToInt(-transform.position.y * 100f) + 2, 1.1f);
                        break;
                    }
                    if (thrown && breakable) { HitFx.OnHit(false); LastViolent = true; Break(); }
                    else if (thrown && verticalVel < -5f)
                    {
                        // tynnyri pomppaa kerran ja vierii vähän
                        verticalVel = -verticalVel * 0.3f;
                        vel.x *= 0.5f;
                        shakeTimer = 0.1f;
                        if (CameraFollow.Instance != null) CameraFollow.Shake(0.06f, 0.1f);
                    }
                    else if (thrown && HasRoll && Mathf.Abs(vel.x) > 0.5f) { state = State.Rolling; stateTime = 0f; }   // vierii eteenpäin
                    else { state = State.Idle; shakeTimer = 0.1f; thrown = false; thrownBy = null; }
                }
                break;
            }

            case State.Rolling:
            {
                Vector3 p = transform.position;
                p.x += vel.x * dt;
                // alueen reunasta kimpoaa takaisin
                var cf = CameraFollow.Instance; var cam = Camera.main;
                if (cf != null && cam != null)
                {
                    float halfW = cam.orthographicSize * cam.aspect - 0.6f;
                    float lo = cf.minX - halfW, hi = cf.maxX + halfW;
                    if (p.x >= lo - 5f && p.x <= hi + 5f && (p.x < lo || p.x > hi)) { p.x = Mathf.Clamp(p.x, lo, hi); vel.x = -vel.x * 0.4f; }
                }
                transform.position = p;
                rollDist += Mathf.Abs(vel.x) * dt;
                if (thrown && Mathf.Abs(vel.x) > 2.5f && (thrownBy != null ? HitPlayerInPath() : HitEnemyInPath())) vel.x *= 0.6f;
                vel.x = Mathf.MoveTowards(vel.x, 0f, rollFriction * dt);
                if (Mathf.Abs(vel.x) < 0.25f)
                {
                    vel = Vector2.zero;
                    state = State.Idle;
                    thrown = false;
                    thrownBy = null;
                    if (Random.value < standUpChance) { lying = false; shakeTimer = 0.15f; }   // pomppaa pystyyn
                }
                break;
            }

            case State.Breaking:
                if (stateTime > debrisTime + 0.5f) Destroy(gameObject);
                break;
        }
        ApplyVisual();
    }

    bool HitEnemyInPath()
    {
        if (height > 3.4f) return false;   // lentää päiden yli (heitetään pään yläpuolelta: osuu korkealtakin)
        Vector3 me = transform.position;
        bool any = false;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 p = e.transform.position;
            if (Mathf.Abs(p.x - me.x) > hitRadiusX || Mathf.Abs(p.y - me.y) > hitRadiusY) continue;
            if (alreadyHit.Contains(e)) continue;
            // isku tulee lentosuunnasta: vihollinen kaatuu samaan suuntaan
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vel.x), true)) { any = true; alreadyHit.Add(e); }
        }
        if (any) HitFx.OnHit(true);
        return any;
    }

    /// Vihollisen (pomon) heittämä: kaataa pelaajan.
    bool HitPlayerInPath()
    {
        if (player == null || height > 3.4f || alreadyHitPlayer) return false;
        Vector3 me = transform.position, p = player.transform.position;
        if (Mathf.Abs(p.x - me.x) > hitRadiusX || Mathf.Abs(p.y - me.y) > hitRadiusY) return false;
        if (player.AirHeight > height + 1.2f) return false;   // hypyllä yli
        if (!player.TakeKnockdown(enemyDamage, me.x - Mathf.Sign(vel.x), 7f, 5f)) return false;
        alreadyHitPlayer = true;
        return true;
    }
    bool alreadyHitPlayer;

    float TargetGround()
    {
        if (player == null || !player.useSidewalk) return 0f;
        return transform.position.y > player.curbDepthY ? player.sidewalkHeight : 0f;
    }

    void ApplyVisual()
    {
        if (body == null) return;
        Sprite spr = CurrentSprite();
        body.sprite = spr;
        Vector2 pivotFix = Vector2.zero;
        if (spr != null)
        {
            float ppu = spr.pixelsPerUnit;
            pivotFix.x = (spr.pivot.x - spr.rect.width * 0.5f) / ppu;
            pivotFix.y = spr.pivot.y / ppu;
        }
        float shake = shakeTimer > 0f ? Mathf.Sin(shakeTimer * 120f) * 0.05f : 0f;
        shake += HitFx.ShakeOffset(shakeUntil, 0.05f);   // tärisee myös osumapysäytyksen aikana
        body.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        body.transform.localPosition = new Vector3(pivotFix.x * visualScale + shake, groundHeight + height - footOffset * visualScale + pivotFix.y * visualScale, 0f);
        // lennossa laatikko pyörii hieman
        // ylösalaisin kannettu leveä pöytä lentää kansi alaspäin pyörimättä (pyöriessä kulma osui lattiaan heti ja lento jäi lyhyeksi)
        float rot = state == State.Flying && thrown && !HasRoll && !carryUpsideDown ? -Mathf.Sign(vel.x) * stateTime * (breakable ? 360f : 540f) : 0f;
        if (state == State.Flying && bounced) rot += -Mathf.Sign(vel.x) * bounceRot;   // kimmotessa pyörähdys
        if (carryUpsideDown)
        {
            // nostossa kääntyy ylösalaisin (keskikohdan ympäri), lennossa pysyy ylösalaisin, maassa taas pystyssä
            float goal = state == State.Carried || (state == State.Flying && thrown) ? 180f : 0f;
            flipAngle = state == State.Idle || state == State.Breaking ? 0f : Mathf.MoveTowards(flipAngle, goal, 720f * Time.deltaTime);
            rot += flipAngle;
            if (spr != null)
            {
                var q = Quaternion.Euler(0f, 0f, rot);
                Vector3 c = new Vector3(0f, spr.bounds.extents.y * visualScale, 0f);
                body.transform.localPosition += c - q * c;
            }
        }
        else if (rot != 0f && spr != null)
        {
            // heitetty pöytä / laatikko pyörii keskikohtansa ympäri (ei jalkojen), ettei puolet painu maan alle
            var q = Quaternion.Euler(0f, 0f, rot);
            Vector3 c = new Vector3(0f, spr.bounds.extents.y * visualScale, 0f);
            body.transform.localPosition += c - q * c;
        }
        body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);
        spinSink = 0f;
        if (spr != null && Mathf.Abs(Mathf.DeltaAngle(0f, rot)) > 0.5f)
        {
            float a = rot * Mathf.Deg2Rad, ex = spr.bounds.extents.x * visualScale, ey = spr.bounds.extents.y * visualScale;
            spinSink = Mathf.Max(0f, Mathf.Abs(ex * Mathf.Sin(a)) + Mathf.Abs(ey * Mathf.Cos(a)) - ey);
        }
        if (state == State.Flying && TableFlight) body.transform.localPosition += new Vector3(0f, spinSink, 0f);   // pyörivä pöytä ei uppoa lattiaan

        int order = state == State.Carried ? carriedOrder : Mathf.RoundToInt(-transform.position.y * 100f);
        body.sortingOrder = order;

        // romukasa häipyy lopuksi
        float fade = state == State.Breaking ? Mathf.Clamp01(1f - (stateTime - debrisTime) / 0.5f) : 1f;
        body.color = new Color(1f, 1f, 1f, fade);

        if (shadow != null)
        {
            shadow.sortingOrder = order - 1;
            shadow.transform.localPosition = new Vector3(0f, groundHeight, 0f);
            float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(height / 3f));
            shadow.transform.localScale = new Vector3(shadowWidth * s * visualScale, 0.42f * s * visualScale * Mathf.Max(1f, shadowWidth / 2.2f), 1f);
            shadow.enabled = state != State.Breaking;
        }

        if (burst != null)
        {
            // sirpaleet lentävät ulospäin laatikon keskeltä ja häipyvät
            float k = Mathf.Clamp01(stateTime / burstTime);
            burst.transform.localPosition = new Vector3(0f, groundHeight + 0.7f * visualScale, 0f);
            burst.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, k) * visualScale;
            burst.color = new Color(1f, 1f, 1f, 1f - k);
            burst.sortingOrder = order + 1;
            if (k >= 1f) { Destroy(burst.gameObject); burst = null; }
        }
    }

    Sprite CurrentSprite()
    {
        if (HasRoll && lying)
            return rollSprites[state == State.Carried ? 0 : (int)(rollDist / 0.3f) % rollSprites.Length];
        if (sprites == null || sprites.Length == 0) return null;
        if (state == State.Breaking && breakSprites != null && breakSprites.Length > 0)
            return breakSprites[Mathf.Min((int)(stateTime / breakFrameTime), breakSprites.Length - 1)];
        if (state == State.Breaking)
            return sprites[Mathf.Min(stateTime < 0.12f ? 3 : 4, sprites.Length - 1)];
        return sprites[Mathf.Min(hits, Mathf.Min(2, sprites.Length - 1))];
    }
}
