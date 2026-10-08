using UnityEngine;

/// <summary>
/// Valtatie: skeittari roikkuu vihuprätkän perässä. Kun prätkä ohittaa pelaajan, skeittari päästää irti,
/// liukuu pelaajan prätkän perään, kurottaa ja tarttuu kiinni ja jarruttaa laudalla (pelaajan prätkä hidastuu).
/// Pelaaja ravistaa hänet irti lyönnillä / kiinniotolla (kiskaisu taakse) tai kiihdytyksellä: skeittari kaatuu tielle.
/// Kuvat: skeittari_roikkuu (8, videosta), skeittari_kurotus (5), skeittari_jarrutus (5: 0–2 ote, 3–4 lauta jarruttaa),
/// kaatuminen Skettarin kuvista (skettari_kaatuminen, 11).
/// </summary>
public class SkaterHitch : MonoBehaviour
{
    public static SkaterHitch Attached { get; private set; }

    public SpriteRenderer body, shadow;
    public Sprite[] towSprites, reachSprites, holdSprites, fallSprites;
    [Tooltip("Sama kuin vihuprätkän visualScale: skeittarin kuvat on mitoitettu vihuprätkän kuvien mittakaavaan.")]
    public float visualScale = 0.61f;
    [Tooltip("Kaatumiskuvien (Skettari, 512 × 384) koko suhteessa omiin kuviin.")]
    public float fallScale = 0.8f;
    [Tooltip("Pelaajan prätkän huippunopeus (osuus), kun skeittari jarruttaa perässä.")]
    [Range(0.1f, 1f)] public float dragFactor = 0.45f;
    public int shakeOffMoney = 1;
    public AudioClip brakeLoop;
    [Range(0f, 1f)] public float brakeVolume = 0.35f;

    [HideInInspector] public EnemyBike tower;

    // käden kohta kuvaruudussa (px, ruutu 640 × 448, pivot alareunan keskellä)
    static readonly Vector2[] TowHand = { new Vector2(500, 193), new Vector2(500, 193), new Vector2(500, 200), new Vector2(500, 191), new Vector2(500, 200), new Vector2(500, 201), new Vector2(500, 201), new Vector2(500, 191) };
    static readonly Vector2[] ReachHand = { new Vector2(420, 197), new Vector2(407, 202), new Vector2(466, 180), new Vector2(436, 192), new Vector2(414, 198) };
    static readonly Vector2[] HoldHand = { new Vector2(414, 192), new Vector2(416, 190), new Vector2(412, 188), new Vector2(407, 187), new Vector2(401, 186) };
    // ote vihuprätkän perästä (vihu_pratka-ruutu 768 × 448) ja pelaajan prätkästä (pratka_ajo-ruutu 768 × 448)
    static readonly Vector2 TowGrip = new Vector2(115, 191);
    const float HeroGripX = 200f;   // kuskin selän / satulan kohta

    enum S { Tow, Glide, Reach, Hold, Fall }
    S state = S.Tow;
    float t, anim, speed, height, vy;
    int frame;
    Vector2 hand;
    PlayerController pc;
    AudioSource brake;

    void Start()
    {
        pc = FindFirstObjectByType<PlayerController>();
        PlayerController.SortByFrameNumber(towSprites); PlayerController.SortByFrameNumber(reachSprites);
        PlayerController.SortByFrameNumber(holdSprites); PlayerController.SortByFrameNumber(fallSprites);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        if (brakeLoop != null)
        {
            brake = gameObject.AddComponent<AudioSource>();
            brake.clip = brakeLoop; brake.loop = true; brake.playOnAwake = false; brake.spatialBlend = 0f; brake.volume = 0f;
        }
        anim = Random.Range(0f, 8f);
    }

    void OnDisable() { if (Attached == this) Attached = null; }

    /// Käden maailmapiste, kun skeittarin juuri (alareunan keskikohta, maassa) on kohdassa root.
    Vector3 HandOffset(Vector2 px) => new Vector3((px.x - 320f) / 100f * visualScale, (448f - px.y) / 100f * visualScale - 0.1f * visualScale, 0f);

    Vector3 HeroGrip(Motorbike mb)
    {
        float s = mb.VisualScale, d = mb.FacingRight ? 1f : -1f;
        return pc.transform.position + new Vector3((HeroGripX - 384f) / 100f * s * d, 0f, 0f);
    }

    void Update()
    {
        float dt = Time.deltaTime; t += dt;
        if (pc == null) { Destroy(gameObject); return; }
        var mb = Motorbike.Current;
        Vector3 me = transform.position, p = pc.transform.position;
        float ps = mb != null ? mb.Speed : 0f;

        switch (state)
        {
            case S.Tow:
            {
                if (tower == null || !tower.CanBeGrabbed) { StartFall(speed, 0f); break; }   // kuski kiskaistiin: skeittari jää tielle
                anim += dt * 8f;
                frame = (int)anim % Mathf.Max(1, towSprites.Length);
                hand = TowHand[Mathf.Min(frame, TowHand.Length - 1)];
                Vector3 b = tower.transform.position;
                float bs = tower.visualScale;
                // käsi vihuprätkän perässä
                Vector3 grip = b + new Vector3((TowGrip.x - 384f) / 100f * bs, 0f, 0f);
                speed = tower.CurrentSpeed;
                me = new Vector3(grip.x - HandOffset(hand).x, b.y, 0f);
                // irti, kun ohitus osuu pelaajan perään samalla kaistalla
                if (mb != null && Mathf.Abs(me.y - p.y) < 0.6f && Mathf.Abs(grip.x - HeroGrip(mb).x) < 0.6f) { state = S.Glide; t = 0f; }
                break;
            }
            case S.Glide:
            {
                if (mb == null) { StartFall(speed, 0f); break; }
                frame = 0; hand = ReachHand[0];
                Vector3 g = HeroGrip(mb);
                float d = mb.FacingRight ? 1f : -1f;
                float gap = (g.x - (me.x + HandOffset(hand).x * d)) * d;     // > 0: pelaaja edellä
                speed = Mathf.MoveTowards(speed, ps + Mathf.Clamp(gap * 3f, -4f, 7f), 16f * dt);
                me.y = Mathf.MoveTowards(me.y, p.y - 0.12f, 1.4f * dt);      // vähän lähempänä katsojaa
                if (Mathf.Abs(gap) < 0.35f && Mathf.Abs(me.y - (p.y - 0.12f)) < 0.15f) { state = S.Reach; t = 0f; }
                if (gap > 9f || t > 7f) { state = S.Fall; StartFall(speed * 0.5f, 0f); }   // pelaaja karkasi
                break;
            }
            case S.Reach:
            {
                if (mb == null) { StartFall(speed, 0f); break; }
                frame = Mathf.Min((int)(t / 0.08f), reachSprites.Length - 1);
                hand = ReachHand[Mathf.Min(frame, ReachHand.Length - 1)];
                Vector3 g = HeroGrip(mb);
                float d = mb.FacingRight ? 1f : -1f;
                speed = ps;
                me.x = Mathf.MoveTowards(me.x + ps * dt * d, g.x - HandOffset(hand).x * d, 6f * dt);   // pysyy pelaajan vauhdissa ja hivuttautuu otteeseen
                me.y = Mathf.MoveTowards(me.y, p.y - 0.12f, 1.4f * dt);
                if (t >= 0.08f * reachSprites.Length)
                {
                    if (Mathf.Abs(me.y - (p.y - 0.12f)) < 0.3f && Attached == null) { state = S.Hold; t = 0f; Attached = this; if (brake != null) brake.Play(); }
                    else { state = S.Glide; t = 0f; }
                }
                break;
            }
            case S.Hold:
            {
                if (mb == null || Attached != this) { StartFall(speed, 0f); break; }
                // 0–2 ote (kerran), sitten lauta jarruttaa (3–4 vuorotellen)
                frame = t < 0.36f ? Mathf.Min((int)(t / 0.12f), 2) : 3 + ((int)((t - 0.36f) / 0.1f) % 2);
                hand = HoldHand[Mathf.Min(frame, HoldHand.Length - 1)];
                float d = mb.FacingRight ? 1f : -1f;
                Vector3 g = HeroGrip(mb);
                speed = ps;
                me = new Vector3(g.x - HandOffset(hand).x * d, p.y - 0.12f, 0f);
                if (brake != null) brake.volume = Mathf.MoveTowards(brake.volume, frame >= 3 ? brakeVolume * Mathf.Clamp01(ps / 10f) : 0f, 2f * dt);
                break;
            }
            case S.Fall:
            {
                speed = Mathf.MoveTowards(speed, 0f, (height > 0f ? 4f : 14f) * dt);
                if (height > 0f || vy > 0f) { vy -= 24f * dt; height = Mathf.Max(0f, height + vy * dt); }
                int n = fallSprites != null ? fallSprites.Length : 0;
                frame = n == 0 ? 0 : height > 0f ? Mathf.Min((int)(t / 0.08f), Mathf.Min(4, n - 1)) : Mathf.Min(5 + (int)(Mathf.Max(0f, t - 0.4f) / 0.08f), n - 1);
                if (brake != null) brake.volume = Mathf.MoveTowards(brake.volume, 0f, 3f * dt);
                if (t > 3f) { Destroy(gameObject); return; }
                break;
            }
        }
        if (state == S.Glide || state == S.Fall) me.x += speed * dt * (mb != null && !mb.FacingRight ? -1f : 1f);
        transform.position = me;
        ApplyVisual();
        if (mb != null && Mathf.Abs(me.x - p.x) > 32f) Destroy(gameObject);
    }

    /// Pelaaja ravisti irti (kiskaisu taakse tai kiihdytys): kaatuu tielle ja jää jälkeen.
    public void ShakeOff(float dir)
    {
        if (state == S.Fall) return;
        HitFx.OnHit(true);
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.15f);
        if (shakeOffMoney > 0) Pickup.SpawnMoney(transform.position, shakeOffMoney, false);
        StartFall(speed - 4f, 5f);
    }

    void StartFall(float sp, float up)
    {
        if (Attached == this) Attached = null;
        state = S.Fall; t = 0f; speed = sp; vy = up; height = up > 0f ? 0.01f : 0f;
        if (fallSprites == null || fallSprites.Length == 0) { Destroy(gameObject); }
    }

    /// Pelaajan prätkän huippunopeuden kerroin (jarrutus perässä).
    public static float Drag => Attached != null && Attached.state == S.Hold && Attached.frame >= 3 ? Attached.dragFactor : 1f;

    void ApplyVisual()
    {
        if (body == null) return;
        var mb = Motorbike.Current;
        bool flip = mb != null && !mb.FacingRight && state != S.Tow;
        Sprite[] set = state == S.Tow ? towSprites : state == S.Glide || state == S.Reach ? reachSprites : state == S.Hold ? holdSprites : fallSprites;
        if (set == null || set.Length == 0) return;
        body.sprite = set[Mathf.Clamp(frame, 0, set.Length - 1)];
        body.flipX = flip;
        float sc = state == S.Fall ? visualScale * fallScale / 0.61f : visualScale;
        body.transform.localScale = new Vector3(sc, sc, 1f);
        body.transform.localPosition = new Vector3(0f, height - 0.1f * visualScale, 0f);
        body.color = new Color(1f, 1f, 1f, state == S.Fall ? Mathf.Clamp01(3f - t) : 1f);
        // ote pelaajasta: kädet kuskin päällä
        int order = Mathf.RoundToInt(-transform.position.y * 100f) + (state == S.Hold || state == S.Reach ? 2 : 1);
        body.sortingOrder = order;
        if (shadow != null)
        {
            shadow.sortingOrder = order - 2;
            shadow.transform.localScale = new Vector3(2.2f * visualScale, 0.45f * visualScale, 1f);
        }
    }
}
