using System.Collections.Generic;
using UnityEngine;

/// <summary>Poimittava pannu: 10 osunutta lyontia tai 3 heittoa, myos uudelleen poimittaessa.</summary>
public class Pan : MonoBehaviour
{
    public static readonly List<Pan> All = new List<Pan>();
    public SpriteRenderer body;
    public Sprite[] spinSprites;
    public Crate table;
    public float tableX, tableTop = 1.65f;
    public int hitDurability = 10, throwDurability = 3, throwDamage = 24;
    public float artScale = 0.7f;
    int hits, throws;
    static AudioClip[] hitSounds;
    static void PlayImpact()
    {
        if (hitSounds == null) hitSounds = Resources.LoadAll<AudioClip>("Sfx/Pan");
        if (hitSounds.Length > 0) HitFx.PlayClip(hitSounds[Random.Range(0, hitSounds.Length)], 1f);
    }
    PlayerController holder;
    bool flying, broken, dropped;
    float groundY, height, vx, vy, clock;
    readonly HashSet<Enemy> struck = new HashSet<Enemy>();
    public static Pan HeldBy(PlayerController player)
    {
        foreach (var pan in All) if (pan != null && pan.holder == player) return pan;
        return null;
    }
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); holder = null; }
    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        if (spinSprites == null || spinSprites.Length == 0)
        {
            var frames = new List<Sprite>();
            foreach (var sprite in Resources.LoadAll<Sprite>("Pan"))
                if (sprite.name.StartsWith("pannu_kuvakulmat_")) frames.Add(sprite);
            frames.Sort((a, b) => Number(a).CompareTo(Number(b))); spinSprites = frames.ToArray();
        }
        groundY = transform.position.y;
        if (body != null) { body.transform.localScale = Vector3.one * artScale; if (spinSprites.Length > 0) body.sprite = spinSprites[0]; }
    }
    static int Number(Sprite sprite) { int.TryParse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1), out int n); return n; }
    /// Uusi pannu putoaa lattialle annettuun kohtaan (ostettu pannu): poimittavissa kuten pöydältä.
    public static Pan SpawnDropped(Vector3 pos)
    {
        var go = new GameObject("Paistinpannu");
        go.transform.position = pos;
        var vis = new GameObject("Visual"); vis.transform.SetParent(go.transform, false);
        vis.AddComponent<SpriteRenderer>();
        var pan = go.AddComponent<Pan>();
        pan.dropped = true; pan.Launch(pos, 1.4f, 0f, 1f);
        return pan;
    }
    public static bool TryPickUp(PlayerController player)
    {
        if (HeldBy(player) != null) return false;
        Pan best = null; float distance = float.MaxValue;
        foreach (var pan in All)
        {
            if (pan == null || pan.holder != null || pan.flying || pan.broken || !Area.IsHere(pan.transform.position, 0f)) continue;
            Vector3 d = pan.transform.position - player.transform.position;
            if (Mathf.Abs(d.x) > 1.7f || Mathf.Abs(d.y) > 0.65f) continue;
            if (d.sqrMagnitude < distance) { distance = d.sqrMagnitude; best = pan; }
        }
        if (best == null) return false;
        best.table = null; best.holder = player; best.clock = 0f; return true;
    }
    public static void UseHeld(PlayerController player)
    {
        var pan = HeldBy(player); if (pan == null) return;
        PlayImpact();
        if (++pan.hits >= pan.hitDurability) pan.Break(player.transform.position, 1.5f);
    }
    public static void ThrowHeld(PlayerController player)
    {
        var pan = HeldBy(player); if (pan == null) return;
        pan.holder = null; pan.throws++; pan.dropped = false; pan.struck.Clear();
        pan.Launch(player.transform.position + Vector3.right * (player.FacingRight ? 2.2f : -2.2f), 2.4f, player.FacingRight ? 14f : -14f, 3.2f);
    }
    public static void DropHeld(PlayerController player)
    {
        var pan = HeldBy(player); if (pan == null) return;
        pan.holder = null; pan.dropped = true; pan.Launch(player.transform.position, 1.3f, 0f, 1f);
        player.OnPanDropped();
    }
    void Launch(Vector3 at, float h, float speed, float up)
    {
        transform.position = at; groundY = at.y; height = h; vx = speed; vy = up; flying = true; clock = 0f;
        if (body != null) { body.enabled = true; body.color = Color.white; }
    }
    void Break(Vector3 at, float h)
    {
        var previousHolder = holder;
        holder = null; broken = true; Launch(at, h, 1f, 2f);
        if (previousHolder != null) previousHolder.OnPanDropped();
        HitFx.OnBreak(0.04f);
    }
    void Update()
    {
        if (table != null && holder == null && !flying)
        {
            transform.position = table.transform.position + Vector3.right * tableX;
            groundY = transform.position.y; height = tableTop;
            if (table.Disturb == 0 && table.Intact) return;
            float dir = table.LastHitDir == 0f ? 1f : table.LastHitDir;
            table = null; dropped = true; Launch(transform.position, tableTop, dir * 2.5f, 2f);
        }
        if (holder != null || !flying) return;
        float dt = Time.deltaTime; clock += dt;
        float oldX = transform.position.x;
        transform.position += Vector3.right * vx * dt;
        var area = Area.Current;
        if (area != null && area.walkMaxX > area.walkMinX && Area.IsHere(transform.position, 20f))
        {
            Vector3 clamped = transform.position;
            clamped.x = Mathf.Clamp(clamped.x, area.walkMinX + 0.3f, area.walkMaxX - 0.3f);
            if (!Mathf.Approximately(clamped.x, transform.position.x)) vx *= -0.25f;
            transform.position = clamped;
        }
        vy -= 18f * dt; height = Mathf.Max(0f, height + vy * dt);
        if (!broken && !dropped && height < 3.2f)
        {
            float lo = Mathf.Min(oldX, transform.position.x) - 0.65f, hi = Mathf.Max(oldX, transform.position.x) + 0.65f;
            for (int i = Enemy.All.Count - 1; i >= 0; i--)
            {
                if (i >= Enemy.All.Count) continue;
                var enemy = Enemy.All[i];
                if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled || struck.Contains(enemy)) continue;
                Vector3 p = enemy.transform.position;
                if (p.x < lo || p.x > hi || Mathf.Abs(p.y - groundY) > 0.55f) continue;
                struck.Add(enemy);
                if (enemy.TakeHit(throwDamage, p.x - Mathf.Sign(vx), true))
                {
                    PlayImpact(); HitFx.OnHit(true); HitSpark.Spawn(p + Vector3.up * 1.7f, true, Mathf.RoundToInt(-p.y * 100f) + 5);
                    vx *= 0.45f; vy = Mathf.Min(vy, 0f);
                }
            }
        }
        if (height <= 0f)
        {
            if (!broken) PlayImpact();
            if (!broken && throws >= throwDurability) { Break(transform.position, 0.1f); return; }
            if (!broken) { flying = false; dropped = false; vx = 0f; clock = 0f; }
        }
        if (broken && clock > 0.4f) Destroy(gameObject);
    }
    void LateUpdate()
    {
        if (body == null) return;
        if (holder != null && !holder.CanHoldPan) DropHeld(holder);
        if (holder != null)
        {
            transform.position = holder.transform.position;
            body.enabled = !holder.PanArtContainsWeapon;
            body.transform.localPosition = new Vector3(holder.FacingRight ? 0.75f : -0.75f, 2f, 0f);
            body.flipX = !holder.FacingRight;
            body.sortingOrder = holder.BodySortOrder + 1;
        }
        else
        {
            body.enabled = true; body.flipX = false;
            body.transform.localPosition = new Vector3(0f, height + 0.1f, 0f);
            body.sortingOrder = Mathf.RoundToInt(-groundY * 100f) + 1;
        }
        if (spinSprites != null && spinSprites.Length > 0)
            body.sprite = spinSprites[flying ? (int)(clock * 24f) % spinSprites.Length : 0];
        body.transform.localRotation = Quaternion.Euler(0f, 0f, flying ? clock * 360f * (vx < 0f ? 1f : -1f) : 0f);
        if (broken) body.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1f - clock / 0.4f));
    }
}
