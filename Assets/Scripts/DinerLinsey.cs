using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Linsey, dinerin asiakas: ei taistelija eikä lyötävissä (ei Enemy-komponenttia). Kävelee lattiaa pitkin, välillä pysähtyy tiskille:
/// kääntyy tiskiä kohti (toCounter), nojaa siihen (atCounter-silmukka) ja lähtee takaisin (toCounter takaperin).
/// Osallistuu piirakkasotaan: poimii piirakan lattialta tai tiskiltä ja heittää sen vihuun (pieThrow-kuvat),
/// ja piirakasta osuessa näyttää oman osumasarjansa (pieHit). Kuvat katsovat oikealle; vasemmalle kuva peilataan.
/// Pysähtyy, kun hero ei ole dinerissä.
/// </summary>
public class DinerLinsey : MonoBehaviour, IBottleHolder
{
    public static readonly List<DinerLinsey> All = new List<DinerLinsey>();
    public Area area;
    public SpriteRenderer body, shadow;
    public Sprite[] idle, walk, toCounter, atCounter;
    [Tooltip("Piirakka naamaan, 4 kuvaa: osuu, roiskuu, valuu, kasvot näkyvät. Tyhjä = piirakka läsähtää omilla kuvillaan.")]
    public Sprite[] pieHit;
    public float[] pieTimes = { 0.12f, 0.14f, 0.5f, 0.9f };
    [Tooltip("Piirakan poiminta ja heitto, 5 kuvaa: 0 kurkottaa, 1–2 piirakka kämmenellä, 3 veto taakse, 4 heitto.")]
    public Sprite[] pieThrow;
    [Tooltip("Piirakan paikka kuvissa (x eteen, y korkeus jaloista yksikköinä), 5 kpl.")]
    public Vector3[] pieGrips;
    public bool throwsPies = true;
    [Tooltip("Etäisyys (yksikköä), jonka sisältä hän huomaa vihun ja piirakan.")]
    public float pieRange = 14f;
    public AudioClip[] hitSounds, speakSounds;
    public AudioClip throwSound;
    public float hitVolume = 1f, throwVolume = 1f, speakVolume = 0.85f;
    public Vector2 speakInterval = new Vector2(12f, 25f);
    public float idleFrameTime = 0.167f, walkFrameTime = 0.083f, toCounterFrameTime = 0.083f, atCounterFrameTime = 0.167f;
    public float speed = 1.5f;
    [Tooltip("Lattian kävelyalue: x-rajat. Syvyys seuraa alueen takarajaa (tiskin ja boksien edessä).")]
    public float laneMinX, laneMaxX;
    [Tooltip("Tiskipaikat (x), joissa hän nojaa tiskiin.")]
    public float[] counterXs;
    public Vector2 waitRange = new Vector2(2f, 5f);
    public Vector2 counterStay = new Vector2(7f, 14f);
    [Range(0f, 1f)] public float counterChance = 0.45f;

    enum S { Idle, Walk, ToCounter, AtCounter, FromCounter, PieHit, GoPie, PiePick, Aim, PieThrow, Stumble }
    S state = S.Idle;
    float t, wait, speakT = 8f;
    Vector2 goal; bool goalIsCounter;
    bool flipped;   // kuva peilattu (katsoo vasemmalle)
    Bottle pie; Enemy target;
    float shoveDir;
    PlayerController hero;
    bool released;
    static readonly float[] PickTimes = { 0.12f, 0.16f };
    static readonly float[] ThrowTimes = { 0.1f, 0.14f, 0.25f };   // kuvat 2, 3, 4: irti kuvan 4 alussa

    bool HasPieArt => pieThrow != null && pieThrow.Length >= 5 && pieGrips != null && pieGrips.Length >= 5;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); DropPie(); }

    void Awake()
    {
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        PlayerController.SortByFrameNumber(idle); PlayerController.SortByFrameNumber(walk);
        PlayerController.SortByFrameNumber(toCounter); PlayerController.SortByFrameNumber(atCounter);
        PlayerController.SortByFrameNumber(pieHit); PlayerController.SortByFrameNumber(pieThrow);
        if (shadow != null && shadow.sprite == null) shadow.sprite = PlayerController.CreateShadowSprite();
        wait = Random.Range(waitRange.x, waitRange.y);
    }

    float Limit(float x) => area != null ? area.MaxDepthAtX(x, area.maxDepthY) : -1f;
    float Dir => flipped ? -1f : 1f;

    void Update()
    {
        if (body == null || (area != null && Area.Current != area)) return;
        float dt = Time.deltaTime; t += dt;
        Vector3 p = transform.position;
        if (state == S.Idle || state == S.AtCounter) Speak(dt);
        switch (state)
        {
            case S.Idle:
                Show(idle, idleFrameTime);
                wait -= dt * (PieWar.Active ? 4f : 1f);   // piirakkasodassa nopeammin liikkeelle
                if (wait <= 0f && !TryStartPie()) PickGoal();
                break;

            case S.Walk:
                if (MoveToGoal(p, dt))
                {
                    if (goalIsCounter && Has(toCounter)) { state = S.ToCounter; t = 0f; }
                    else { state = S.Idle; t = 0f; wait = Random.Range(waitRange.x, waitRange.y); }
                }
                Show(walk, walkFrameTime);
                break;

            case S.ToCounter:
            {
                int i = (int)(t / toCounterFrameTime);
                if (i >= toCounter.Length) { state = S.AtCounter; t = 0f; wait = Random.Range(counterStay.x, counterStay.y); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.AtCounter:
                Show(atCounter, atCounterFrameTime);
                wait -= dt;
                if (wait <= 0f) { state = S.FromCounter; t = 0f; }
                break;

            case S.FromCounter:
            {
                int i = toCounter.Length - 1 - (int)(t / toCounterFrameTime);
                if (i < 0) { state = S.Idle; t = 0f; wait = Random.Range(0.5f, 1.5f); break; }
                body.sprite = toCounter[i];
                break;
            }

            case S.PieHit:
            {
                float acc = 0f; int fi = pieHit.Length - 1;
                for (int i = 0; i < pieHit.Length && i < pieTimes.Length; i++) { acc += pieTimes[i]; if (t < acc) { fi = i; break; } }
                body.sprite = pieHit[Mathf.Min(fi, pieHit.Length - 1)];
                float total = 0f; foreach (var pt in pieTimes) total += pt;
                if (t >= total) { state = S.Idle; t = 0f; wait = Random.Range(1f, 2.5f); }
                break;
            }

            case S.Stumble:
            {
                // heitetty pöytä osui: säikähtää (järkyttynyt ilme) ja horjahtaa pois päin
                body.sprite = pieHit != null && pieHit.Length >= 4 ? pieHit[3] : (Has(idle) ? idle[0] : body.sprite);
                float shove = shoveDir * 2.5f * Mathf.Max(0f, 1f - t / 0.5f) * dt;
                transform.position = new Vector3(Mathf.Clamp(p.x + shove, laneMinX, laneMaxX), p.y, 0f);
                if (t >= 1.4f) { state = S.Idle; t = 0f; wait = Random.Range(1f, 2.5f); }
                break;
            }

            case S.GoPie:
                if (pie == null || !pie.CanPickUp) { pie = null; target = null; state = S.Idle; t = 0f; wait = 1f; break; }
                if (MoveToGoal(p, dt))
                {
                    flipped = pie.transform.position.x < transform.position.x;
                    if (pie.TakeBy(this)) { state = S.PiePick; t = 0f; }
                    else { pie = null; target = null; state = S.Idle; t = 0f; wait = 1f; }
                }
                Show(walk, walkFrameTime);
                break;

            case S.PiePick:
            {
                int i = ThrowPose.Index(PickTimes, t);
                if (i < 0) { state = S.Aim; t = 0f; break; }
                body.sprite = pieThrow[i];
                break;
            }

            case S.Aim:
                UpdateAim(p, dt);
                break;

            case S.PieThrow:
            {
                int i = ThrowPose.Index(ThrowTimes, t);
                if (!released && t >= ThrowPose.Start(ThrowTimes, 2))
                {
                    released = true;
                    if (pie != null)
                    {
                        pie.Throw(transform.position.y, Dir, false);
                        pie = null;
                        if (throwSound != null) HitFx.PlayClip(throwSound, throwVolume);
                    }
                }
                if (i < 0) { target = null; state = S.Idle; t = 0f; wait = Random.Range(1f, 2.5f); break; }
                body.sprite = pieThrow[2 + i];
                break;
            }
        }
        body.flipX = flipped;
        body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        if (shadow != null) shadow.sortingOrder = body.sortingOrder - 1;
    }

    // ---------------- piirakkasota ----------------

    /// Etsii vihun ja lattialla/tiskillä olevan piirakan; jos löytyy molemmat, kävelee piirakan luo.
    bool TryStartPie()
    {
        if (!throwsPies || !HasPieArt) return false;
        if (hero == null) hero = FindFirstObjectByType<PlayerController>();
        Vector3 me = transform.position;
        Enemy foe = null; float fd = pieRange;
        foreach (var e in Enemy.All)
        {
            if (e == null || e.IsDead || !e.isActiveAndEnabled) continue;
            float d = Vector2.Distance(e.transform.position, me);
            if (d < fd) { fd = d; foe = e; }
        }
        if (foe == null) return false;
        Bottle best = null; float bd = pieRange;
        foreach (var b in Bottle.All)
        {
            if (b == null || !b.pie || !b.CanPickUp) continue;
            float d = Vector2.Distance(b.transform.position, me);
            if (PieWar.Active && hero != null && Vector2.Distance(b.transform.position, hero.transform.position) < d * 0.7f) continue;   // hero on lähempänä: jää hänelle
            if (d < bd) { bd = d; best = b; }
        }
        if (best == null) return false;
        pie = best; target = foe; released = false;
        float side = me.x <= best.transform.position.x ? -1f : 1f;
        goal = new Vector2(best.transform.position.x + side * 0.7f, best.transform.position.y + 0.01f);
        goalIsCounter = false;
        state = S.GoPie; t = 0f;
        return true;
    }

    void UpdateAim(Vector3 p, float dt)
    {
        if (pie == null) { state = S.Idle; t = 0f; wait = 1f; return; }
        bool moving = false;
        if (target != null && !target.IsDead && target.isActiveAndEnabled)
        {
            Vector3 tp = target.transform.position;
            float dx = tp.x - p.x, dy = tp.y - p.y;
            flipped = dx < 0f;
            float step = speed * dt;
            Vector2 move = Vector2.zero;
            if (Mathf.Abs(dy) > 0.25f) move.y = Mathf.Sign(dy) * Mathf.Min(Mathf.Abs(dy), step);
            if (Mathf.Abs(dx) > 7f) move.x = Mathf.Sign(dx) * step;
            else if (Mathf.Abs(dx) < 3f) move.x = -Mathf.Sign(dx) * step;
            moving = move.sqrMagnitude > 0f;
            if (moving)
            {
                Vector3 np = p + (Vector3)move;
                np.y = Mathf.Clamp(np.y, area != null ? area.minDepthY : -9f, Limit(np.x) - 0.08f);
                np.x = Mathf.Clamp(np.x, laneMinX, laneMaxX);
                transform.position = np;
            }
            else if (t > 0.15f) { state = S.PieThrow; t = 0f; released = false; return; }
        }
        else { target = null; state = S.PieThrow; t = 0f; released = false; return; }
        if (t > 4f) { state = S.PieThrow; t = 0f; released = false; return; }
        if (moving) Show(walk, walkFrameTime); else body.sprite = pieThrow[1];
    }

    void DropPie()
    {
        if (pie != null && !released) pie.Drop();
        pie = null;
    }

    /// Heitetty piirakka osuu Linseyyn (häntä ei voi lyödä eikä hän ota vahinkoa). Palauttaa true, jos piirakka-animaatio alkoi.
    public bool PieHit(float attackerX)
    {
        if (pieHit == null || pieHit.Length < 4 || state == S.PieHit) return false;
        DropPie();
        flipped = attackerX < transform.position.x;   // kuvissa piirakka tulee oikealta
        state = S.PieHit; t = 0f;
        if (hitSounds != null && hitSounds.Length > 0) HitFx.PlayClip(hitSounds[Random.Range(0, hitSounds.Length)], hitVolume);
        return true;
    }

    /// Heitetty pöytä osuu Linseyyn (ei vahinkoa). Palauttaa true, jos hän reagoi.
    public bool TableHit(float attackerX)
    {
        if (state == S.PieHit || state == S.Stumble) return false;
        DropPie();
        flipped = attackerX < transform.position.x;   // katsoo pöytään päin
        shoveDir = attackerX < transform.position.x ? 1f : -1f;
        state = S.Stumble; t = 0f;
        if (hitSounds != null && hitSounds.Length > 0) HitFx.PlayClip(hitSounds[Random.Range(0, hitSounds.Length)], hitVolume);
        return true;
    }

    /// Piirakka kädessä (IBottleHolder): käden paikka poiminnan, tähtäyksen ja heiton kuvissa.
    public bool BottleGrip(out Vector3 hand, out float rot, out int order)
    {
        hand = Vector3.zero; rot = 0f;
        order = body != null ? body.sortingOrder + 1 : 0;
        if (body == null || !HasPieArt) return false;
        int fi;
        switch (state)
        {
            case S.PiePick: { int i = ThrowPose.Index(PickTimes, t); fi = i < 0 ? 1 : i; break; }
            case S.Aim: fi = 1; break;
            case S.PieThrow: { int i = ThrowPose.Index(ThrowTimes, t); fi = i < 0 ? 4 : 2 + i; break; }
            default: return false;
        }
        if (fi == 0) return false;   // kurkottaa: piirakka vielä lattialla
        Vector3 g = pieGrips[fi];
        hand = body.transform.position + new Vector3(Dir * g.x, g.y, 0f);
        rot = Dir * g.z;
        return true;
    }

    // ---------------- puhe ----------------

    void Speak(float dt)
    {
        if (speakSounds == null || speakSounds.Length == 0) return;
        speakT -= dt;
        if (speakT > 0f) return;
        speakT = Random.Range(speakInterval.x, speakInterval.y);
        if (hero == null) hero = FindFirstObjectByType<PlayerController>();
        if (hero == null || Mathf.Abs(hero.transform.position.x - transform.position.x) > 12f) return;
        HitFx.PlayClip(speakSounds[Random.Range(0, speakSounds.Length)], speakVolume);
    }

    // ---------------- liike ----------------

    bool MoveToGoal(Vector3 p, float dt)
    {
        Vector2 d = goal - (Vector2)p;
        float step = speed * dt;
        if (Mathf.Abs(d.x) > 0.05f) flipped = d.x < 0f;
        if (d.magnitude <= step) { transform.position = new Vector3(goal.x, goal.y, 0f); return true; }
        transform.position = (Vector2)p + d.normalized * step;
        return false;
    }

    static bool Has(Sprite[] s) => s != null && s.Length > 0;

    void Show(Sprite[] set, float frameTime)
    {
        if (Has(set)) body.sprite = set[(int)(t / frameTime) % set.Length];
    }

    void PickGoal()
    {
        goalIsCounter = counterXs != null && counterXs.Length > 0 && Random.value < counterChance;
        float x;
        if (goalIsCounter) x = counterXs[Random.Range(0, counterXs.Length)];
        else x = Random.Range(laneMinX, laneMaxX);
        float y = goalIsCounter ? Limit(x) - 0.08f : Limit(x) - Random.Range(0.3f, 1.2f);
        goal = new Vector2(x, y);
        state = S.Walk; t = 0f;
    }
}
