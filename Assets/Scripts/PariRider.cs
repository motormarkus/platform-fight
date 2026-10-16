using System;
using System.Linq;
using UnityEngine;

/// <summary>Motoristi ja rokkimimmi heron pyörällä. EnemyBike-rajapinta säilyttää heron riutaisun.</summary>
public class PariRider : EnemyBike
{
    public Area area;
    public SpriteRenderer motorcycle;
    public Sprite[] idleSprites, punchSprites;
    public float fps = 15f;
    [Range(8, 20)] public int punchImpact = 14;
    public int punchDamage = 12;
    public float punchReach = 3.2f, punchDepth = 0.55f;
    public Vector2 punchDelay = new Vector2(2.5f, 4.5f);
    public float followGap = 2.1f;   // jää heron nykyisen 2.4 yksikön riutaisu-ulottuvuuden sisään
    public float riderScale = 0.72f, motorcycleScale = 0.576f;
    public Vector2 riderOffset = new Vector2(-1.296f, 0.576f);
    public Color motorcycleTint = new Color(0.45f, 0.48f, 0.52f, 1f);
    [Header("Heron pyörän pyörivät vanteet")]
    public Sprite rearRim, frontRim;
    public Sprite chassisSprite;
    public Vector2 rearRimPosition = new Vector2(-3.045f, 1.045f), frontRimPosition = new Vector2(2.995f, 1.065f);
    public float rearRimScale = 1f, frontRimScale = 1f;
    [Header("Heron riutaisu")]
    [Range(0f, 1f)] public float yankSpeedMultiplier = 0.18f;
    public float yankBraking = 3f;
    public float yankFlashTime = 0.35f;

    enum Mode { Chase, Punch, Flee, Slowed }
    Mode mode;
    PlayerController player;
    AudioSource motor;
    SpriteRenderer rearRimRenderer, frontRimRenderer;
    float rearAngle, frontAngle;
    float pariSpeed, idleClock, punchClock, cooldown = 2f, fleeTime, flashTime;
    bool pariHitDone;
    float direction = 1f;
    public override bool CanBeGrabbed => mode == Mode.Chase || mode == Mode.Punch;

    public static Sprite[] Frames(Sprite[] all, string prefix) => all
        .Where(s => s != null && s.name.StartsWith(prefix, StringComparison.Ordinal))
        .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();

    void Start()
    {
        player = Players.Primary;
        var all = Resources.LoadAll<Sprite>("AjoPari");
        if (idleSprites == null || idleSprites.Length == 0) idleSprites = Frames(all, "ajo_idle_");
        if (punchSprites == null || punchSprites.Length == 0) punchSprites = Frames(all, "lyonti_");
        if (body == null || motorcycle == null || idleSprites.Length == 0 || punchSprites.Length == 0)
        { Debug.LogWarning("Ajoparin pyörä tai AjoPari-kuvat puuttuvat.", this); enabled = false; Destroy(gameObject); return; }
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        if (chassisSprite != null) motorcycle.sprite = chassisSprite;
        rearRimRenderer = MakeRim("Takavanne", rearRim, rearRimScale);
        frontRimRenderer = MakeRim("Etuvanne", frontRim, frontRimScale);
        pariSpeed = Motorbike.Current != null ? Motorbike.Current.Speed + overtakeSpeed : overtakeSpeed;
        if (engineLoop != null)
        {
            motor = gameObject.AddComponent<AudioSource>();
            motor.clip = engineLoop; motor.loop = true; motor.playOnAwake = false;
            motor.spatialBlend = 0f; motor.volume = 0f; motor.Play();
        }
        ApplyArt();
    }

    void Update()
    {
        if (player == null) return;
        if (area != null && Area.Current != area) { Destroy(gameObject); return; }
        float dt = Time.deltaTime;
        flashTime = Mathf.Max(0f, flashTime - dt);
        var heroBike = Motorbike.Current;
        if (heroBike == null && mode != Mode.Slowed) mode = Mode.Flee;
        else if (heroBike != null && mode != Mode.Slowed) direction = heroBike.FacingRight ? 1f : -1f;
        Vector3 p = player.transform.position, me = transform.position;
        float ahead = (me.x - p.x) * direction;
        float heroSpeed = heroBike != null ? heroBike.Speed : 0f;
        idleClock += dt;
        if (mode == Mode.Slowed)
        {
            // Riutaisu katkaisee takaa-ajon: pyörä hidastuu eikä kiihdytä takaisin heron perään.
            pariSpeed = Mathf.MoveTowards(pariSpeed, 0f, Mathf.Max(0f, yankBraking) * dt);
            var cam = Camera.main;
            if (cam != null)
            {
                float behindCamera = (me.x - cam.transform.position.x) * direction;
                if (behindCamera < -cam.orthographicSize * cam.aspect - 4f)
                { Destroy(gameObject); return; }
            }
        }
        else if (mode == Mode.Flee)
        {
            fleeTime += dt;
            pariSpeed = Mathf.MoveTowards(pariSpeed, heroSpeed + 16f, 18f * dt);
            if (fleeTime > 5f || ahead > 30f) { Destroy(gameObject); return; }
        }
        else
        {
            float catchup = Mathf.Clamp((followGap - ahead) * 2f, -4f, overtakeSpeed);
            pariSpeed = Mathf.MoveTowards(pariSpeed, Mathf.Max(0f, heroSpeed + catchup), 20f * dt);
            me.y = Mathf.MoveTowards(me.y, Mathf.Clamp(p.y + 0.25f, player.minDepthY, player.maxDepthY), depthSpeed * dt);
            cooldown -= dt;
            bool inRange = Mathf.Abs(ahead) <= punchReach && Mathf.Abs(me.y - p.y) <= punchDepth;
            if (mode == Mode.Chase && cooldown <= 0f && inRange)
            { mode = Mode.Punch; punchClock = 0f; pariHitDone = false; }
            if (mode == Mode.Punch)
            {
                punchClock += dt;
                int frame = (int)(punchClock * Mathf.Max(1f, fps));
                if (!pariHitDone && frame >= Mathf.Clamp(punchImpact, 0, punchSprites.Length - 1))
                {
                    pariHitDone = true;
                    if (heroBike != null && inRange) heroBike.Knock(punchDamage);
                }
                if (frame >= punchSprites.Length)
                { mode = Mode.Chase; cooldown = UnityEngine.Random.Range(punchDelay.x, punchDelay.y); }
            }
        }
        me.x += direction * pariSpeed * dt;
        rearAngle = RollAngle(rearAngle, pariSpeed, direction, RimRadius(rearRim, rearRimScale), dt);
        frontAngle = RollAngle(frontAngle, pariSpeed, direction, RimRadius(frontRim, frontRimScale), dt);
        transform.position = me;
        if (ahead < -40f) { Destroy(gameObject); return; }
        if (motor != null)
        {
            motor.volume = engineVolume * Mathf.Clamp01(1f - Mathf.Abs(ahead) / 22f) * GameSettings.SfxVolume;
            motor.pitch = Mathf.Clamp(0.85f + pariSpeed * 0.02f, 0.8f, 1.5f);
            motor.panStereo = Mathf.Clamp((me.x - p.x) / 14f, -0.8f, 0.8f);
        }
        ApplyArt();
    }

    public override void YankOff(float playerX, float playerDir)
    {
        if (!CanBeGrabbed) return;
        mode = Mode.Slowed; pariHitDone = true;
        var heroBike = Motorbike.Current;
        float beforeHit = heroBike != null ? Mathf.Min(pariSpeed, heroBike.Speed) : pariSpeed;
        pariSpeed = Mathf.Max(0f, beforeHit) * Mathf.Clamp01(yankSpeedMultiplier);
        flashTime = Mathf.Max(0f, yankFlashTime);
        direction = playerDir >= 0f ? 1f : -1f;
        HitFx.OnHit(true);
        if (yankSound != null) HitFx.PlayClip(yankSound, yankVolume);
        if (crashMoney > 0) Pickup.SpawnMoney(transform.position, crashMoney, false);
        ApplyArt();
    }

    public override void KnockOff(float attackerX) => YankOff(attackerX, direction);

    SpriteRenderer MakeRim(string name, Sprite sprite, float scale)
    {
        if (sprite == null) return null;
        var rim = new GameObject(name).AddComponent<SpriteRenderer>();
        rim.transform.SetParent(motorcycle.transform, false);
        rim.sprite = sprite; rim.transform.localScale = Vector3.one * scale;
        return rim;
    }

    static float RollAngle(float angle, float velocity, float direction, float radius, float dt) =>
        Mathf.Repeat(angle - velocity * direction / Mathf.Max(0.1f, radius) * Mathf.Rad2Deg * dt, 360f);

    float RimRadius(Sprite sprite, float scale) => sprite != null
        ? sprite.rect.width / sprite.pixelsPerUnit * 0.5f * scale * motorcycleScale : 0.5f;

    void ShowRim(SpriteRenderer rim, Vector2 position, float angle, int order)
    {
        if (rim == null) return;
        rim.transform.localPosition = new Vector3(position.x * direction, position.y, 0f);
        rim.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        rim.flipX = direction < 0f; rim.color = motorcycle.color; rim.sortingOrder = order;
    }

    void ApplyArt()
    {
        if (body == null || motorcycle == null || idleSprites == null || idleSprites.Length == 0) return;
        int frame = mode == Mode.Punch ? Mathf.Min((int)(punchClock * Mathf.Max(1f, fps)), punchSprites.Length - 1)
            : (int)(idleClock * Mathf.Max(1f, fps)) % idleSprites.Length;
        body.sprite = mode == Mode.Punch ? punchSprites[frame] : idleSprites[frame];
        body.flipX = motorcycle.flipX = direction < 0f;
        // Saappaat lisättiin kasvattamalla alkuperäistä 378x263-ruutua oikealle ja alas.
        // Säilytä istuimen ja käsien paikka BottomCenter-pivotin siirtymisestä huolimatta.
        float pixelsPerUnit = body.sprite.pixelsPerUnit;
        Vector2 canvasOffset = new Vector2((body.sprite.rect.width - 378f) * 0.5f,
            263f - body.sprite.rect.height) / pixelsPerUnit * riderScale;
        body.transform.localPosition = new Vector3((riderOffset.x + canvasOffset.x) * direction,
            riderOffset.y + canvasOffset.y, 0f);
        body.transform.localScale = Vector3.one * riderScale;
        motorcycle.transform.localScale = Vector3.one * motorcycleScale;
        float flash = yankFlashTime > 0f ? Mathf.Clamp01(flashTime / yankFlashTime) : 0f;
        motorcycle.color = Color.Lerp(motorcycleTint, new Color(1f, 0.12f, 0.12f, 1f), flash);
        int order = Mathf.RoundToInt(-transform.position.y * 100f);
        motorcycle.sortingOrder = order; body.sortingOrder = order + 1;
        ShowRim(rearRimRenderer, rearRimPosition, rearAngle, order - 1);
        ShowRim(frontRimRenderer, frontRimPosition, frontAngle, order - 1);
        if (shadow != null)
        { shadow.sortingOrder = order - 2; shadow.transform.localScale = new Vector3(4f, 0.4f, 1f); }
    }
}
