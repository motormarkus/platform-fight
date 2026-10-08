using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kadun moottoripyörä. Pelaaja menee viereen ja painaa E (ohjaimessa ympyrä): nousuanimaatio, käynnistysääni
/// ja ajo. Ajossa vasen/oikea kiihdyttää ja jarruttaa, ylös/alas vaihtaa kaistaa. Vauhdissa pyörä kaataa
/// viholliset ja lennättää laatikot ja tynnyrit. E pysähtyneenä: pelaaja nousee pois ja pyörä jää siihen.
/// </summary>
public class Motorbike : MonoBehaviour
{
    [Tooltip("Pysäköity pyörä (kadun kuva).")]
    public SpriteRenderer parked;
    public Sprite parkedLeft, parkedRight;
    [Tooltip("Ajokuvat (pratka_ajo.png): kuski pyörän selässä, katse oikealle.")]
    public Sprite[] rideSprites;
    [Tooltip("Nousukuvat (pratka_nousu.png), samassa ruudussa kuin ajokuvat.")]
    public Sprite[] mountSprites;
    [Tooltip("Erilliset vanteet, jotka pyörivät koodilla (jos tyhjä, ajokuvissa on pyörät valmiina).")]
    public Sprite rearWheel, frontWheel;
    [Tooltip("Kumit vanteiden päällä: eivät pyöri, jotta valon heijastus pysyy paikallaan.")]
    public Sprite rearTyre, frontTyre;
    [Tooltip("Vanteiden keskipisteet ajokuvan pivotista (yksikköä, keula oikealle).")]
    public Vector2 rearWheelPos = new Vector2(-2.176f, 0.824f), frontWheelPos = new Vector2(2.133f, 0.837f);
    [Tooltip("Kiskaisu (pratka_kiskaisu.png): 0–4 kurotus eteen/sivulle, 5–9 kurotus taakse.")]
    public Sprite[] grabSprites;
    public float grabFrameTime = 0.08f;
    [Tooltip("Kuinka kaukaa vierellä ajavasta vihusta saa otteen (x ja syvyys).")]
    public float grabRangeX = 2.4f, grabRangeY = 1.1f;
    public AudioClip startSound;
    [Range(0f, 1f)] public float startVolume = 0.9f;
    [Tooltip("Käyntiääni, joka soi silmukkana käynnistysäänen loputtua.")]
    public AudioClip engineLoop;
    [Range(0f, 1f)] public float engineVolume = 0.3f;
    [Tooltip("Kuinka monen sekunnin päästä startista käyntiääni alkaa (ristihäivytys starttiäänen hiipuessa).")]
    public float engineLoopDelay = 2.5f;
    [Tooltip("Ristihäivytyksen kesto (s).")]
    public float engineFadeTime = 1.2f;
    [Tooltip("Kiihdytysääni (puskunappi).")]
    public AudioClip boostSound;
    [Range(0f, 1f)] public float boostVolume = 0.55f;

    [Tooltip("Pyörän ja ajokuvien koko valtatiellä (1 = kuvien oma).")]
    public float bikeScale = 0.85f;
    [Tooltip("Koko muualla (parkkipaikka, kuja).")]
    public float streetScale = 1f;
    [Tooltip("Kadun pysäköityjen (ei ajettavien) pyörien koko.")]
    public float propScale = 0.7f;
    [Tooltip("Voiko pyörällä ajaa. Kadun pyörät ovat rekvisiittaa, ajettava on takakujan parkkipaikalla.")]
    public bool rideable = true;
    /// Valtatiellä pienempi koko, muualla isompi.
    float Scale => Area.Current != null && Area.Current.areaName == "Valtatie" ? bikeScale : (rideable ? streetScale : propScale);

    [Header("Ajo")]
    public float maxSpeed = 27.5f;
    public float acceleration = 18f;
    public float braking = 18f;
    public float depthSpeed = 2.6f;
    public float mountFrameTime = 0.11f;
    [Tooltip("Ajon idle-sarjan tahti (takin lepatus, hiukset), kuvaa sekunnissa.")]
    public float rideFps = 8f;
    [Header("Kiihdytys (puskunappi): keulii ja kiihtyy hetken huippunopeuden yli")]
    public float boostTime = 2.5f;
    [Tooltip("Lisänopeus huippunopeuden osuutena (0.4 = +40 %).")]
    public float boostExtra = 0.4f;
    public float boostAcceleration = 26f;
    [Tooltip("Kuinka hitaasti ylinopeus laskee takaisin huippunopeuteen (yksikköä/s²).")]
    public float boostFalloff = 2.5f;
    [Tooltip("Keulimiskulma (astetta).")]
    public float wheelieAngle = 9f;
    [Header("Yliajo")]
    public int runOverDamage = 25;
    public float runOverMinSpeed = 3f;
    [Tooltip("Pyörän puolipituus (yksikköä): tämän matkan sisällä keskeltä osuu.")]
    public float halfLength = 2.3f;
    [Header("Käyttö")]
    public float useHalfWidth = 2.2f;
    public float useDepth = 0.8f;
    public string prompt = "Nouse pyörän selkään";

    PlayerController pc;
    AudioSource audioSrc;
    bool near, busy, riding, facingRight;
    float speed, animClock, rideClock, groundHeight;
    readonly Dictionary<Object, float> lastHit = new Dictionary<Object, float>();
    static Motorbike active;
    /// Pyörä, jota pelaaja ajaa (null jos ei aja).
    public static Motorbike Current => active != null && active.riding ? active : null;
    public float Speed => speed;
    public float VisualScale => Scale;
    public bool FacingRight => facingRight;
    public bool FacingRight => facingRight;
    float wobble, wheelAngle, boostT = -1f, wheelie;
    AudioSource loopSrc, startSrc;
    float loopAt = -1f;
    float grabT = -1f; bool grabBack, grabDone; EnemyBike grabTarget;
    SpriteRenderer rearR, frontR, rearT, frontT;

    void EnsureWheels()
    {
        if (rearWheel == null || pc == null || pc.body == null) return;
        if (rearR == null)
        {
            rearR = new GameObject("Takavanne").AddComponent<SpriteRenderer>();
            frontR = new GameObject("Etuvanne").AddComponent<SpriteRenderer>();
            rearR.sprite = rearWheel; frontR.sprite = frontWheel;
            if (rearTyre != null && frontTyre != null)
            {
                rearT = new GameObject("Takakumi").AddComponent<SpriteRenderer>();
                frontT = new GameObject("Etukumi").AddComponent<SpriteRenderer>();
                rearT.sprite = rearTyre; frontT.sprite = frontTyre;
            }
        }
        rearR.transform.SetParent(pc.body.transform, false);
        frontR.transform.SetParent(pc.body.transform, false);
        if (rearT != null) { rearT.transform.SetParent(pc.body.transform, false); frontT.transform.SetParent(pc.body.transform, false); }
    }

    void UpdateWheels(bool show, float dt)
    {
        if (rearR == null) return;
        rearR.enabled = frontR.enabled = show;
        if (rearT != null) rearT.enabled = frontT.enabled = show;
        if (!show) return;
        // kehänopeus = ajonopeus: kulmanopeus = v / r (rad/s)
        float r = rearWheel.rect.width / rearWheel.pixelsPerUnit * 0.5f;
        wheelAngle -= speed / Mathf.Max(0.1f, r) * Mathf.Rad2Deg * dt;
        float sx = facingRight ? 1f : -1f;
        rearR.transform.localPosition = new Vector3(rearWheelPos.x * sx, rearWheelPos.y, 0f);
        frontR.transform.localPosition = new Vector3(frontWheelPos.x * sx, frontWheelPos.y, 0f);
        float ang = wheelAngle * sx;
        rearR.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
        frontR.transform.localRotation = Quaternion.Euler(0f, 0f, ang * r / Mathf.Max(0.1f, frontWheel.rect.width / frontWheel.pixelsPerUnit * 0.5f));
        rearR.flipX = frontR.flipX = !facingRight;
        rearR.sortingOrder = frontR.sortingOrder = pc.body.sortingOrder - 1;   // rungon (haarukka, pakoputket) takana
        rearR.color = frontR.color = pc.body.color;
        if (rearT != null)
        {
            rearT.transform.localPosition = rearR.transform.localPosition;
            frontT.transform.localPosition = frontR.transform.localPosition;
            rearT.flipX = frontT.flipX = !facingRight;
            rearT.sortingOrder = frontT.sortingOrder = pc.body.sortingOrder - 1;
            rearT.color = frontT.color = pc.body.color;
        }
    }

    /// Vihun potku: vauhti putoaa, pyörä heiluu, pelaaja ottaa vahinkoa.
    public void Knock(int damage)
    {
        if (!riding || wobble > 0f) return;
        speed *= 0.25f;
        wobble = 0.6f;
        pc.HitWhileRiding(damage);
        HitFx.OnHit(true);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.12f, 0.25f);
        HitSpark.Spawn(pc.transform.position + new Vector3(-Dir * 0.8f, 1.8f, 0f), true, Mathf.RoundToInt(-pc.transform.position.y * 100f) + 5);
    }

    void Awake()
    {
        PlayerController.SortByFrameNumber(rideSprites);
        PlayerController.SortByFrameNumber(mountSprites);
        PlayerController.SortByFrameNumber(grabSprites);
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.spatialBlend = 0f;
        loopSrc = gameObject.AddComponent<AudioSource>();
        loopSrc.playOnAwake = false; loopSrc.loop = true; loopSrc.spatialBlend = 0f;
        startSrc = gameObject.AddComponent<AudioSource>();
        startSrc.playOnAwake = false; startSrc.spatialBlend = 0f;
        if (parked != null)
        {
            bool spriteRight = parkedRight != null && parked.sprite == parkedRight;
            facingRight = spriteRight != parked.flipX;
        }
    }

    void Update()
    {
        // pysäköity kuva (3D-render) samaan kokoon kuin kadun pyörät; ajokuvat käyttävät omaa kokoaan (Scale)
        float ps = Mathf.Min(Scale, propScale);
        if (parked != null) parked.transform.localScale = new Vector3(ps, ps, 1f);
        if (!rideable) return;
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        if (riding) { Ride(Time.deltaTime); return; }
        near = false;
        if (busy || active != null || !pc.enabled || !pc.IsFree) return;
        Vector3 p = pc.transform.position, me = transform.position;
        near = Mathf.Abs(p.x - me.x) <= useHalfWidth && Mathf.Abs(p.y - me.y) <= useDepth;
        if (near && UsePressed()) StartCoroutine(Mount());
    }

    static bool UsePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    float Dir => facingRight ? 1f : -1f;

    Sprite[] roccoRide, roccoMount, roccoGrab;
    /// Ruby: omat ajo-, nousu- ja kiskaisukuvat (sama pyörän runko), jos ne on asetettu.
    void UseCharacterSprites()
    {
        if (roccoRide == null) { roccoRide = rideSprites; roccoMount = mountSprites; roccoGrab = grabSprites; }
        var h = pc != null && GameSettings.Character == 1 ? pc.heroine : null;
        bool ruby = h != null && h.bikeRide != null && h.bikeRide.Length > 0 && h.bikeMount != null && h.bikeMount.Length > 0;
        rideSprites = ruby ? h.bikeRide : roccoRide;
        mountSprites = ruby ? h.bikeMount : roccoMount;
        grabSprites = ruby && h.bikeGrab != null && h.bikeGrab.Length >= 10 ? h.bikeGrab : roccoGrab;
    }

    IEnumerator Mount()
    {
        busy = true; active = this; near = false;
        UseCharacterSprites();
        pc.Riding = true;
        pc.enabled = false;
        Vector3 me = transform.position;
        pc.transform.position = new Vector3(me.x, me.y, 0f);
        groundHeight = GroundAt(me.y);
        bool bodyInFrames = mountSprites != null && mountSprites.Length > 0;
        if (parked != null && bodyInFrames) parked.enabled = false;   // nousukuvissa on pyörän runko mukana
        EnsureWheels();
        if (mountSprites != null)
            foreach (var s in mountSprites) { ShowRider(s); UpdateWheels(true, 0f); yield return new WaitForSeconds(mountFrameTime); }
        if (startSound != null) { startSrc.clip = startSound; startSrc.volume = startVolume; startSrc.Play(); }
        loopAt = Time.time + (startSound != null ? Mathf.Min(engineLoopDelay, startSound.length) : 0f);   // käyntiääni ristihäivytyksellä startin perään
        if (parked != null) parked.enabled = false;
        speed = 0f; animClock = 0f; boostT = -1f; wheelie = 0f;
        EnsureWheels();
        riding = true; busy = false;
    }

    IEnumerator Dismount()
    {
        busy = true; riding = false;
        StopEngine();
        Vector3 p = pc.transform.position;
        // pysäköity pyörä tähän, samaan suuntaan
        transform.position = new Vector3(p.x, p.y, 0f);
        if (parked != null)
        {
            if (parkedRight != null) { parked.sprite = facingRight ? parkedRight : parkedLeft; parked.flipX = false; }
            else parked.flipX = facingRight;
            parked.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
        }
        if (mountSprites != null)
            for (int i = mountSprites.Length - 1; i >= 0; i--) { ShowRider(mountSprites[i]); UpdateWheels(true, 0f); yield return new WaitForSeconds(mountFrameTime * 0.8f); }
        UpdateWheels(false, 0f);
        if (parked != null) parked.enabled = true;   // nousukuvissa oli runko mukana: pysäköity kuva vasta lopuksi
        pc.Riding = false;
        if (pc.body != null) pc.body.transform.localScale = Vector3.one;
        pc.enabled = true;
        pc.TeleportTo(new Vector3(p.x - Dir * 1.4f, p.y, 0f));   // seisoo pyörän vieressä
        active = null; busy = false;
    }

    /// Pysäköi heti ilman laskeutumisanimaatiota (esim. uuden alueen alussa pimennyksen aikana): pyörä kohtaan pos, pelaaja viereen.
    public void ParkNow(Vector3 pos, bool faceRight)
    {
        StopAllCoroutines();
        busy = false; riding = false; speed = 0f;
        StopEngine();
        facingRight = faceRight;
        UpdateWheels(false, 0f);
        transform.position = new Vector3(pos.x, pos.y, 0f);
        if (parked != null)
        {
            if (parkedRight != null) { parked.sprite = facingRight ? parkedRight : parkedLeft; parked.flipX = false; }
            else parked.flipX = facingRight;
            parked.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            parked.enabled = true;
        }
        if (pc != null)
        {
            pc.Riding = false;
            if (pc.body != null) { pc.body.transform.localScale = Vector3.one; pc.body.transform.localRotation = Quaternion.identity; }
            pc.enabled = true;
            pc.TeleportTo(new Vector3(pos.x - Dir * 1.6f, pos.y - 0.25f, 0f));
        }
        if (active == this) active = null;
    }

    void StopEngine()
    {
        loopAt = -1f; boostT = -1f; wheelie = 0f;
        if (loopSrc != null) loopSrc.Stop();
        if (startSrc != null) startSrc.Stop();
        if (audioSrc != null) audioSrc.Stop();
    }

    void OnDisable() => StopEngine();

    void ShowRider(Sprite s)
    {
        if (pc.body == null) return;
        pc.body.transform.localScale = new Vector3(Scale, Scale, 1f);
        pc.body.sprite = s;
        pc.body.flipX = !facingRight;
        pc.body.transform.localPosition = new Vector3(0f, groundHeight - 0.1f, 0f);   // ruudussa 10 px tyhjää alla
        pc.body.transform.localRotation = Quaternion.identity;
        if (riding && wheelie > 0.01f && rearWheel != null)
        {
            // keuliminen: runko kiertyy takarenkaan kosketuspisteen ympäri
            float sx = facingRight ? 1f : -1f, a = wheelie * wheelieAngle * sx;
            float r = rearWheel.rect.width / rearWheel.pixelsPerUnit * 0.5f;
            if (rearTyre != null) r = rearTyre.rect.width / rearTyre.pixelsPerUnit * 0.5f;
            Vector3 c = new Vector3(rearWheelPos.x * sx, rearWheelPos.y - r, 0f) * Scale;
            pc.body.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            pc.body.transform.localPosition += c - Quaternion.Euler(0f, 0f, a) * c;
        }
        pc.body.color = Color.white;
        int order = Mathf.RoundToInt(-pc.transform.position.y * 100f);
        pc.body.sortingOrder = order;
        if (pc.shadow != null)
        {
            pc.shadow.enabled = true;
            pc.shadow.sortingOrder = order - 1;
            pc.shadow.transform.localPosition = new Vector3(0f, groundHeight, 0f);
            pc.shadow.transform.localScale = new Vector3(3.6f * Scale, 0.5f * Scale, 1f);
        }
    }

    float GroundAt(float y) => pc.useSidewalk && y > pc.curbDepthY ? pc.sidewalkHeight : 0f;

    void Ride(float dt)
    {
        if (busy) return;
        Vector2 input = PlayerController.ReadMoveInput();
        if (wobble > 0f)
        {
            // potkun jälkeen hetki hallitsematonta heilumista
            wobble -= dt;
            input.y += Mathf.Sin(wobble * 30f) * 0.8f;
            input.x = Mathf.Min(input.x * Dir, 0.3f) * Dir;
        }
        // suunnanvaihto vain lähes pysähdyksissä
        if (Mathf.Abs(speed) < 1f && Mathf.Abs(input.x) > 0.3f && Mathf.Sign(input.x) != Dir) { facingRight = input.x > 0f; speed = 0f; }
        // kiihdytys (puskunappi): keulii ja kiihtyy hetken huippunopeuden yli
        if (boostT < 0f && grabT < 0f && PlayerController.PushInput())
        {
            boostT = 0f;
            if (SkaterHitch.Attached != null) SkaterHitch.Attached.ShakeOff(Dir);   // kiihdytys repäisee perässä roikkujan irti
            if (boostSound != null) audioSrc.PlayOneShot(boostSound, boostVolume);
        }
        bool boosting = boostT >= 0f && boostT < boostTime;
        if (boostT >= 0f) { boostT += dt; if (boostT >= boostTime) boostT = -1f; }
        // keulinta: nopeasti ylös, hetki ylhäällä, laskeutuu ennen kiihdytyksen loppua
        float wTarget = boosting && boostT < boostTime * 0.45f ? 1f : 0f;
        wheelie = Mathf.MoveTowards(wheelie, wTarget, (wTarget > wheelie ? 5f : 1.6f) * dt);

        float target = Mathf.Max(0f, input.x * Dir) * maxSpeed * SkaterHitch.Drag;   // eteenpäin kaasu, taaksepäin jarru (perässä jarruttava skeittari hidastaa)
        float rate = target > speed ? acceleration : (input.x * Dir < -0.3f ? braking : acceleration * 0.6f);
        if (boosting) { target = maxSpeed * (1f + boostExtra); rate = boostAcceleration; }
        else if (speed > maxSpeed && input.x * Dir > -0.3f) rate = boostFalloff;   // ylinopeus laskee hitaasti
        speed = Mathf.MoveTowards(speed, target, rate * dt);

        // käyntiääni: silmukka starttiäänen jälkeen, kierrokset nousevat vauhdin mukana
        if (engineLoop != null && loopAt >= 0f && Time.time >= loopAt && !loopSrc.isPlaying)
        {
            loopSrc.clip = engineLoop; loopSrc.volume = 0f; loopSrc.Play();
        }
        if (loopAt >= 0f && Time.time >= loopAt)
        {
            // ristihäivytys: käyntiääni nousee, starttiääni hiipuu
            float f = Mathf.Clamp01((Time.time - loopAt) / Mathf.Max(0.01f, engineFadeTime));
            if (loopSrc != null) loopSrc.volume = engineVolume * f;
            if (startSrc != null && startSrc.isPlaying) { startSrc.volume = startVolume * (1f - f); if (f >= 1f) startSrc.Stop(); }
        }
        if (loopSrc != null && loopSrc.isPlaying)
            loopSrc.pitch = Mathf.Lerp(loopSrc.pitch, 0.9f + 0.35f * Mathf.Clamp01(speed / Mathf.Max(1f, maxSpeed * (1f + boostExtra))), 4f * dt);

        Vector3 p = pc.transform.position;
        p.x += Dir * speed * dt;
        p.y = Mathf.Clamp(p.y + input.y * depthSpeed * dt, pc.minDepthY, pc.maxDepthY);
        var cf = CameraFollow.Instance; var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - halfLength;
            float lo = cf.minX - halfW, hi = cf.maxX + halfW;
            if (p.x < lo || p.x > hi) { p.x = Mathf.Clamp(p.x, lo, hi); speed = 0f; }
        }
        if (Area.Current != null && Area.Current.walkMaxX != 0f && p.x > Area.Current.walkMaxX - halfLength * 0.6f)
        { p.x = Area.Current.walkMaxX - halfLength * 0.6f; speed = 0f; }   // laiturin reuna
        pc.transform.position = p;
        groundHeight = Mathf.MoveTowards(groundHeight, GroundAt(p.y), 3f * dt);   // reunakiven yli

        // moottori käy: kuvat pyörivät hitaasti paikallaan, vauhdissa nopeammin
        animClock += dt * (rearWheel != null ? 24f : 6f + speed * 1.4f);   // moottorin tärinä
        rideClock += dt * (rearWheel != null ? rideFps : 6f + speed * 1.4f);
        UpdateWheels(true, dt);
        // kiskaisu (lyöntinappi): kurotus vierellä ajavaan vihuun, ote niskasta ja riuhtaisu irti pyörästä
        if (grabT < 0f && grabSprites != null && grabSprites.Length >= 10 && (PlayerController.PunchInput() || PlayerController.CatchInput()))
        {
            grabTarget = null; float best = 99f;
            foreach (var eb in FindObjectsByType<EnemyBike>(FindObjectsSortMode.None))
            {
                if (!eb.CanBeGrabbed) continue;
                Vector3 q = eb.transform.position;
                float dx = (q.x - p.x) * Dir, dy = Mathf.Abs(q.y - p.y);
                if (Mathf.Abs(dx) <= grabRangeX && dy <= grabRangeY && Mathf.Abs(dx) + dy < best) { best = Mathf.Abs(dx) + dy; grabTarget = eb; }
            }
            grabBack = grabTarget != null && (grabTarget.transform.position.x - p.x) * Dir < -0.4f;
            if (SkaterHitch.Attached != null) { grabTarget = null; grabBack = true; }   // perässä roikkuva skeittari ensin
            grabT = 0f; grabDone = false;
        }
        if (grabT >= 0f)
        {
            grabT += dt;
            int f = (int)(grabT / grabFrameTime);
            if (!grabDone && f >= 3)
            {
                grabDone = true;
                if (grabTarget != null && grabTarget.CanBeGrabbed) grabTarget.YankOff(p.x, Dir);
                else if (grabBack && SkaterHitch.Attached != null) SkaterHitch.Attached.ShakeOff(Dir);
            }
            if (f >= 5) grabT = -1f;
            else { ShowRider(grabSprites[(grabBack ? 5 : 0) + f]); return; }
        }
        if (rideSprites != null && rideSprites.Length > 0)
        {
            // erilliset vanteet: kuvat eteenpäin (takin lepatus); muuten takaperin, jotta kuvien pyörät pyörivät ajosuuntaan
            int n = rideSprites.Length;
            ShowRider(rideSprites[rearWheel != null ? (int)rideClock % n : n - 1 - (int)rideClock % n]);
            if (wobble > 0f && pc.body != null)
            {
                pc.body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(wobble * 30f) * 6f);
                pc.body.color = Mathf.FloorToInt(wobble * 14f) % 2 == 0 ? new Color(1f, 0.6f, 0.6f) : Color.white;
            }
        }

        if (speed >= runOverMinSpeed) RunOver(p);
        if (UsePressed() && speed < 1.5f) StartCoroutine(Dismount());
    }

    void RunOver(Vector3 me)
    {
        float now = Time.time;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 q = e.transform.position;
            if (Mathf.Abs(q.x - me.x) > halfLength || Mathf.Abs(q.y - me.y) > 0.45f) continue;
            if (lastHit.TryGetValue(e, out float t) && now - t < 0.8f) continue;
            if (e.TakeHit(runOverDamage, me.x - Dir * halfLength, true))
            {
                lastHit[e] = now;
                HitSpark.Spawn(new Vector3(q.x, q.y + 1.6f, 0f), true, Mathf.RoundToInt(-q.y * 100f) + 5);
                HitFx.OnHit(true);
                speed *= 0.85f;
            }
        }
        foreach (var c in Crate.All.ToArray())
        {
            if (c == null || !c.CanBeHit) continue;
            Vector3 q = c.transform.position;
            if (Mathf.Abs(q.x - me.x) > halfLength || Mathf.Abs(q.y - me.y) > 0.5f) continue;
            if (lastHit.TryGetValue(c, out float t) && now - t < 0.8f) continue;
            lastHit[c] = now;
            c.TakeHit(20, me.x - Dir * halfLength);
            c.TakeHit(20, me.x - Dir * halfLength);
            c.TakeHit(20, me.x - Dir * halfLength);   // laatikko hajoaa kerralla
        }
    }

    GUIStyle style;
    void OnGUI()
    {
        bool show = near || (riding && !busy && speed < 1.5f && pc != null);
        if (!show) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
        string text = "[E]  " + Loc.T(riding ? "Nouse pyörän selästä" : prompt);
        float w = Screen.height * 0.5f, h = Screen.height * 0.065f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
