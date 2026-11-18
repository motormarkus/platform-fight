using System.Collections.Generic;
using UnityEngine;

/// <summary>Taikurin peilikuvat (illuusio): 2-3 läpikuultavaa, sinertävää kopiota syntyy taikurin viereen savupuuskassa, juoksee kohteen kimppuun ja iskee (kukin kerran),
/// ja lopuksi oikea taikuri vaihtaa paikkaa yhden kopion kanssa (savupuuska molemmissa paikoissa), joten pelaaja ei tiedä kumpi on oikea.
/// Kopiot ovat vain kuvia (ei Enemy-oliota): ne käyttävät samoja kuvia kuin taikuri (lähestyminen, isku), joten ne ovat käytännössä ilmaisia.
/// Käyttö taikurin aivosta: MirrorImages.Cast(enemy, player, config) -> palauttaa false, jos sama taikuri on jo luonut kopioita.</summary>
public sealed class MirrorImages : MonoBehaviour
{
    [System.Serializable]
    public class Config
    {
        [Tooltip("Lähestymiskuvat (silmukka juostessa), esim. väistö-/kävelykuvat.")] public Sprite[] approach;
        [Tooltip("Iskukuvat; osuma kuvassa hitFrame.")] public Sprite[] strike;
        [Tooltip("Savupuuska (taikuri_poofpieni) syntyessä, katoamisessa ja paikanvaihdossa; alareuna ankkurina.")] public Sprite[] poof;
        public int copies = 3;
        public float speed = 7.5f, stagger = 0.22f, spread = 0.7f, alpha = 0.55f;
        public float approachFrameTime = 0.07f, strikeFrameTime = 0.06f, poofFrameTime = 0.045f, poofScale = 1f;
        public float strikeRange = 1.9f, reach = 2.4f, depthReach = 0.9f;
        public int hitFrame = 2, damage = 12;
        public float swapDelay = 1.4f, life = 4.5f;
        public Color tint = new Color(0.55f, 0.72f, 1f, 1f);
        public AudioClip castSound, hitSound, swapSound; public float volume = 1f;
    }

    class Clone
    {
        public Transform t; public SpriteRenderer sr; public float delay, clock; public int mode; public int frame; public bool hit; public float fade = 1f;   // mode: 0 odottaa, 1 lähestyy, 2 iskee, 3 häipyy, 4 valmis
        public float yJitter;
    }

    static readonly Dictionary<Enemy, MirrorImages> active = new Dictionary<Enemy, MirrorImages>();
    static readonly List<MirrorImages> all = new List<MirrorImages>();
    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    /// <summary>Heron isku (PlayerController.AttackEnemies): jokainen etualan kopio, johon isku yltää, häviää savuna ilmaan (ei vahinkoa, ei nokkimisjärjestystä). Palauttaa true, jos osui johonkin.</summary>
    public static bool TryHit(Vector3 me, bool facingRight, float reach, float depth)
    {
        bool any = false;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var m = all[i]; if (m == null) continue;
            foreach (var k in m.clones)
            {
                if (k.mode > 2 || k.t == null || !k.sr.enabled || k.sr.color.a < 0.2f) continue;
                Vector3 p = k.t.position; float dx = p.x - me.x;
                bool inFront = facingRight ? dx >= -0.3f && dx <= reach : dx <= 0.3f && dx >= -reach;
                if (!inFront || Mathf.Abs(p.y - me.y) > depth + 0.2f) continue;
                m.Puff(p); m.Sound(m.c.swapSound);
                k.mode = 4; k.sr.enabled = false; any = true;
                HitSpark.Spawn(new Vector3(p.x, p.y + 1.6f, 0f), false, Mathf.RoundToInt(-p.y * 100f) + 5);
            }
        }
        return any;
    }
    Enemy owner; PlayerController target; Config c; readonly List<Clone> clones = new List<Clone>();
    float time; bool swapped; bool dead;
    public static bool IsCasting(Enemy owner) => owner != null && active.ContainsKey(owner) && active[owner] != null;
    /// <summary>Tapahtuu, kun taikuri on vaihtanut paikkaa (aivo voi esim. keskeyttää tai jatkaa liikettä uudesta paikasta).</summary>
    public System.Action<Vector3> onSwapped;

    public static MirrorImages Cast(Enemy owner, PlayerController target, Config config)
    {
        if (owner == null || target == null || config == null || owner.body == null || config.approach == null || config.approach.Length == 0) return null;
        if (IsCasting(owner)) return null;
        var go = new GameObject("Peilikuvat"); var m = go.AddComponent<MirrorImages>();
        m.owner = owner; m.target = target; m.c = config; active[owner] = m;
        m.Spawn();
        return m;
    }

    void Spawn()
    {
        Vector3 basePos = owner.transform.position;
        int n = Mathf.Clamp(c.copies, 1, 5);
        for (int i = 0; i < n; i++)
        {
            float side = n == 1 ? 0f : Mathf.Lerp(-1f, 1f, i / (float)(n - 1));
            var go = new GameObject("Peilikuva " + i); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = owner.body.sprite != null ? owner.body.sprite : c.approach[0];
            sr.flipX = owner.body.flipX; sr.color = new Color(c.tint.r, c.tint.g, c.tint.b, 0f);
            go.transform.localScale = owner.body.transform.lossyScale;
            Vector3 p = basePos + new Vector3(side * c.spread * 0.4f, side * c.spread * 0.5f, 0f); go.transform.position = p;
            clones.Add(new Clone { t = go.transform, sr = sr, delay = i * c.stagger, mode = 0, yJitter = Random.Range(-0.25f, 0.25f) });
            Puff(p);
        }
        Sound(c.castSound);
    }

    void Sound(AudioClip clip) { if (clip != null) AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position, c.volume * GameSettings.SfxVolume); }

    void Puff(Vector3 pos)
    {
        if (c.poof == null || c.poof.Length == 0) return;
        var go = new GameObject("Savu"); go.transform.position = pos;
        go.transform.localScale = Vector3.one * c.poofScale * Mathf.Max(0.01f, owner.body.transform.lossyScale.x);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = c.poof[0]; sr.sortingOrder = owner.body.sortingOrder + 2;
        var a = go.AddComponent<PoofAnim>(); a.frames = c.poof; a.frameTime = c.poofFrameTime;
    }
    sealed class PoofAnim : MonoBehaviour
    {
        public Sprite[] frames; public float frameTime; float t; SpriteRenderer sr;
        void Awake() { sr = GetComponent<SpriteRenderer>(); }
        void Update()
        {
            t += Time.deltaTime; int i = (int)(t / frameTime);
            if (i >= frames.Length) { Destroy(gameObject); return; }
            sr.sprite = frames[i];
        }
    }

    void Update()
    {
        if (owner == null || target == null) { Finish(); return; }
        float dt = Time.deltaTime; time += dt;
        bool allDone = true;
        foreach (var k in clones)
        {
            if (k.mode == 4) continue;
            allDone = false; Step(k, dt);
        }
        if (!swapped && time >= c.swapDelay) Swap();
        if ((allDone && swapped) || time > c.life) Finish();
    }

    void Step(Clone k, float dt)
    {
        k.clock += dt;
        Vector3 pos = k.t.position, tp = target.transform.position;
        float order = owner.body.sortingOrder + Mathf.Clamp(Mathf.RoundToInt((owner.transform.position.y - pos.y) * 10f), -5, 5);
        k.sr.sortingOrder = (int)order;
        switch (k.mode)
        {
            case 0:
                if (k.clock >= k.delay) { k.mode = 1; k.clock = 0f; }
                else { Fade(k, k.clock / Mathf.Max(0.05f, k.delay + 0.1f)); }
                break;
            case 1:
                {
                    Fade(k, 1f);
                    float dx = tp.x - pos.x, dy = tp.y + k.yJitter - pos.y;
                    k.sr.flipX = dx < 0f;
                    Vector3 move = new Vector3(Mathf.Sign(dx) * Mathf.Min(Mathf.Abs(dx), c.speed * dt), Mathf.Clamp(dy, -c.speed * 0.5f * dt, c.speed * 0.5f * dt), 0f);
                    k.t.position = pos + move;
                    k.sr.sprite = c.approach[(int)(k.clock / c.approachFrameTime) % c.approach.Length];
                    if (Mathf.Abs(dx) <= c.strikeRange && c.strike != null && c.strike.Length > 0) { k.mode = 2; k.clock = 0f; k.frame = 0; }
                    else if (k.clock > c.life) k.mode = 3;
                    break;
                }
            case 2:
                {
                    k.frame = (int)(k.clock / c.strikeFrameTime); k.sr.flipX = tp.x < pos.x;
                    if (k.frame >= c.strike.Length) { k.mode = 3; k.clock = 0f; Puff(k.t.position); break; }
                    k.sr.sprite = c.strike[k.frame];
                    if (!k.hit && k.frame >= c.hitFrame)
                    {
                        k.hit = true;
                        float dx = Mathf.Abs(tp.x - pos.x), dy = Mathf.Abs(tp.y - pos.y);
                        if (dx <= c.reach && dy <= c.depthReach && !target.GameOver && target.AirHeight < 1.2f)
                        { if (target.TakeHit(c.damage, pos.x, owner)) Sound(c.hitSound); }
                    }
                    break;
                }
            case 3:
                k.fade -= dt * 4f; Fade(k, k.fade);
                if (k.fade <= 0f) { k.mode = 4; k.sr.enabled = false; }
                break;
        }
    }
    void Fade(Clone k, float a) { var col = k.sr.color; col.a = c.alpha * Mathf.Clamp01(a); k.sr.color = col; }

    void Swap()
    {
        swapped = true;
        var cand = clones.FindAll(x => x.mode == 1 || x.mode == 2 || x.mode == 0);
        if (cand.Count == 0) return;
        var k = cand[Random.Range(0, cand.Count)];
        Vector3 a = owner.transform.position, b = k.t.position;
        Puff(a); Puff(b);
        owner.transform.position = new Vector3(b.x, b.y, a.z); k.t.position = new Vector3(a.x, a.y, b.z);
        Sound(c.swapSound);
        if (onSwapped != null) onSwapped(owner.transform.position);
    }

    void Finish()
    {
        if (dead) return; dead = true;
        foreach (var k in clones) if (k.t != null) Puff(k.t.position);
        if (owner != null) active.Remove(owner);
        Destroy(gameObject);
    }
    void OnDestroy() { if (owner != null && active.TryGetValue(owner, out var m) && m == this) active.Remove(owner); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { active.Clear(); }
}
