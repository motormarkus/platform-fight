using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puulaatikko: hajoaa iskuista (oletuksena kolme osumaa). Pelaaja voi nostaa sen pään yli (O)
/// ja heittää (lyönti tai potku). Lentävä laatikko kaataa osuessaan vihollisen ja hajoaa.
/// </summary>
public class Crate : MonoBehaviour
{
    public static readonly List<Crate> All = new List<Crate>();

    [Tooltip("laatikko.png: 0 ehjä, 1 halkeillut, 2 rikki, 3 hajoaa, 4 romukasa.")]
    public Sprite[] sprites;
    [Tooltip("Sirpaleräjähdys (laatikko_sirpaleet.png), näytetään hajotessa.")]
    public Sprite burstSprite;
    public SpriteRenderer body;
    public SpriteRenderer shadow;

    [Header("Kestävyys")]
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

    [Header("Spriten sijoitus")]
    [Tooltip("Ruudussa on 10 px tyhjää laatikon alla (200 px/yksikkö).")]
    public float footOffset = 0.05f;

    enum State { Idle, Carried, Flying, Breaking }
    State state = State.Idle;
    int hits;
    float height, verticalVel, stateTime, shakeTimer;
    Vector2 vel;
    bool thrown;            // heitetty (hajoaa osuessaan) vai vain pudotettu (jää ehjäksi)
    int carriedOrder;
    float groundHeight;
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
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        groundHeight = TargetGround();
    }

    /// Pelaajan isku osuu laatikkoon.
    public bool TakeHit(int damage)
    {
        if (!CanBeHit) return false;
        hits++;
        shakeTimer = 0.15f;
        if (hits >= hitsToBreak) Break();
        return true;
    }

    public void PickUp()
    {
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
    public void Throw(float vx, float up)
    {
        if (state != State.Carried) return;
        vel = new Vector2(vx, 0f);
        verticalVel = up;
        thrown = true;
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
        state = State.Flying;
        stateTime = 0f;
    }

    void Break()
    {
        state = State.Breaking;
        stateTime = 0f;
        height = 0f;
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.12f);
        if (Random.value < moneyChance) Pickup.SpawnMoney(transform.position + new Vector3(0.3f, -0.05f, 0f), 1, false);
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

        switch (state)
        {
            case State.Flying:
            {
                Vector3 p = transform.position;
                p.x += vel.x * dt;
                transform.position = p;
                verticalVel -= 30f * dt;
                height += verticalVel * dt;
                if (thrown && HitEnemyInPath()) { Break(); break; }
                if (height <= 0f)
                {
                    height = 0f;
                    if (thrown) { HitFx.OnHit(false); Break(); }
                    else { state = State.Idle; shakeTimer = 0.1f; }
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
        if (height > 2.8f) return false;   // lentää päiden yli
        Vector3 me = transform.position;
        bool any = false;
        foreach (var e in Enemy.All.ToArray())
        {
            if (e == null || e.IsDead) continue;
            Vector3 p = e.transform.position;
            if (Mathf.Abs(p.x - me.x) > hitRadiusX || Mathf.Abs(p.y - me.y) > hitRadiusY) continue;
            // isku tulee lentosuunnasta: vihollinen kaatuu samaan suuntaan
            if (e.TakeHit(throwDamage, me.x - Mathf.Sign(vel.x), true)) any = true;
        }
        if (any) HitFx.OnHit(true);
        return any;
    }

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
        body.transform.localPosition = new Vector3(pivotFix.x + shake, groundHeight + height - footOffset + pivotFix.y, 0f);
        // lennossa laatikko pyörii hieman
        float rot = state == State.Flying && thrown ? -Mathf.Sign(vel.x) * stateTime * 360f : 0f;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, rot);

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
            shadow.transform.localScale = new Vector3(1.4f * s, 0.42f * s, 1f);
            shadow.enabled = state != State.Breaking;
        }

        if (burst != null)
        {
            // sirpaleet lentävät ulospäin laatikon keskeltä ja häipyvät
            float k = Mathf.Clamp01(stateTime / burstTime);
            burst.transform.localPosition = new Vector3(0f, groundHeight + 0.7f, 0f);
            burst.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, k);
            burst.color = new Color(1f, 1f, 1f, 1f - k);
            burst.sortingOrder = order + 1;
            if (k >= 1f) { Destroy(burst.gameObject); burst = null; }
        }
    }

    Sprite CurrentSprite()
    {
        if (sprites == null || sprites.Length == 0) return null;
        if (state == State.Breaking)
            return sprites[Mathf.Min(stateTime < 0.12f ? 3 : 4, sprites.Length - 1)];
        return sprites[Mathf.Min(hits, Mathf.Min(2, sprites.Length - 1))];
    }
}
