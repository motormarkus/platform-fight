using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Taikurin pudottama kyky: hero muuttuu häräksi (kuvat taikurin härkä-muodonmuutoksesta, BossPerks.TryBull). Hero piiloutuu, härkä syöksyy eteenpäin ja lennättää kaikki tieltään;
/// hero on suojassa koko ajan ja palaa normaaliksi savupuuskassa. Kerran minuutissa.
/// </summary>
public sealed class HeroBull : MonoBehaviour
{
    public PlayerController pc; public Sprite[] frames; public float frameTime = 0.04f, speed = 15f, scale = 1f, maxTime = 2.6f;
    public int startFrame = 0, damage = 28; public float launch = 30f, lift = 11f, jumpClear = 1.0f;

    SpriteRenderer sr; float t, dir; Vector3 pos;
    readonly List<SpriteRenderer> hidden = new List<SpriteRenderer>(); readonly HashSet<Object> hit = new HashSet<Object>();
    bool ended;

    void Start()
    {
        if (pc == null || frames == null || frames.Length == 0) { Destroy(gameObject); return; }
        dir = pc.FacingRight ? 1f : -1f; pos = pc.transform.position;
        sr = gameObject.AddComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);
        foreach (var r in pc.GetComponentsInChildren<SpriteRenderer>(true)) if (r.enabled) { hidden.Add(r); r.enabled = false; }
        pc.BullHidden = true; pc.Scripted = true; pc.ScriptedMove = Vector2.zero; pc.GrantInvulnerable(maxTime + 1f);
        var ch = Resources.Load<AudioClip>("Sfx/Taikuri/harka_muutos"); if (ch != null) HitFx.PlayClip(ch, 1f);
        var run = Resources.Load<AudioClip>("Sfx/Taikuri/harka_juoksu"); if (run != null) HitFx.PlayClip(run, 1.1f);
        CameraFollow.Shake(0.2f, 0.4f);
        Apply(0);
    }

    void Apply(int i)
    {
        int idx = Mathf.Clamp(startFrame + i, 0, frames.Length - 1);
        sr.sprite = frames[idx]; sr.flipX = dir < 0f;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 100f) + 4;
        pc.transform.position = new Vector3(pos.x, pos.y, pc.transform.position.z);
    }

    void LateUpdate()
    {
        if (ended) return;
        foreach (var r in hidden) if (r != null) r.enabled = false;   // hero pysyy piilossa (kuvapäivitys saattaa kytkeä näkyviin)
        if (pc == null || pc.GameOver) { End(); return; }
        float dt = Time.deltaTime; t += dt;
        pc.Scripted = true; pc.ScriptedMove = Vector2.zero; pc.GrantInvulnerable(0.5f);
        int i = Mathf.FloorToInt(t / frameTime);
        bool done = t >= maxTime || startFrame + i >= frames.Length;
        float charge = Mathf.Clamp01(t / 0.35f);                        // lähtö kiihtyy savusta
        pos.x += dir * speed * charge * dt;
        var area = Area.Current;
        if (area != null && area.walkMaxX != 0f) { float nx = Mathf.Clamp(pos.x, area.walkMinX + 0.3f, area.walkMaxX - 0.3f); if (nx != pos.x) { pos.x = nx; done = true; CameraFollow.Shake(0.3f, 0.3f); } }
        Apply(i);
        Sweep();
        if (done) End();
    }

    void Sweep()
    {
        if (t < 0.25f) return;   // vahinko vasta, kun härkä on muotoutunut
        Vector3 c = pos + new Vector3(dir * 1.3f, 0f, 0f);
        foreach (var en in Enemy.All.ToArray())
        {
            if (en == null || en.ally || !en.isActiveAndEnabled || en.IsDead || hit.Contains(en)) continue;
            Vector3 p = en.transform.position;
            if (Mathf.Abs(p.x - c.x) > 3.3f || Mathf.Abs(p.y - c.y) > 1.5f || en.IsAirborneNow) continue;
            hit.Add(en); Enemy.EnemyHits(en, damage, p.x - dir, true);
            en.Launch(dir * launch, lift);
            CameraFollow.Shake(0.2f, 0.25f);
        }
    }

    void End()
    {
        if (ended) return; ended = true;
        if (pc != null)
        {
            foreach (var r in hidden) if (r != null) r.enabled = true;
            pc.BullHidden = false; pc.Scripted = false; pc.ScriptedMove = Vector2.zero; pc.GrantInvulnerable(0.8f);
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (!ended && pc != null) { foreach (var r in hidden) if (r != null) r.enabled = true; pc.BullHidden = false; pc.Scripted = false; }
    }
}
