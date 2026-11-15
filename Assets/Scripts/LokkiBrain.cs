using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lokki: MongolBrainin erikoisliikkeet (suorasarjat, potkut, kämmeniskun aalto, heilautus, polvi) + reaktiivinen puolustus kuten Deaconilla:
/// heron iskuun Lokki joko VÄISTÄÄ (ylös tai alas syvyyssuunnassa), TORJUU (suoja-asento), OTTAA KIINNI (kädestä tai jalasta) ja HEITTÄÄ heron toiselle puolelleen,
/// tai harvoin ottaa osuman. Väistön/torjunnan jälkeen heti COUNTER: nopea suora + kaatava lopetus.
/// Lisäksi pako oikealle (Flee). Käytetään ensiesiintymisessä juna-asemalla (LokkiEncounter).
/// </summary>
public class LokkiBrain : MongolBrain, IEnemyBrain
{
    [Tooltip("Täysin voittamaton (kaikki iskut ohi). Oletus pois: Lokkia voi lyödä, mutta hän väistää ja torjuu paljon (reactive).")]
    public bool invulnerable = false;
    [Tooltip("Reaktiivinen puolustus: väistö/torjunta/kiinniotto/osuma todennäköisyyksien mukaan. Pois: kaikki iskut osuvat (tavallinen vihu).")]
    public bool reactive = true;
    [Tooltip("Myös kaatava isku voi osua (landChance); jos pois, kaatavat iskut menevät aina ohi.")]
    public bool landKnockdown = true;
    public Move fleeMove, blockMove, dodgeMove, counterJab;
    public Move[] counterFinishers;
    [Header("Heitto-counter (kiinniotto kädestä tai jalasta)")]
    [Tooltip("Kuvajonot järjestyksessä: käsi = kurotus, vetäminen, selin käännös...; jalka = matala kurotus...")]
    public Move catchHand, catchLeg;
    [Tooltip("Jonon indeksi, jossa ote otetaan ja jossa hero heitetään (käsi / jalka).")]
    public int handGrabPos = 1, handThrowPos = 4, legGrabPos = 2, legThrowPos = 4;
    public int throwDamage = 26; public float throwSpeed = 9f, throwUp = 6f, grabReach = 3.6f;
    [Range(0f, 1f)] public float dodgeChance = 0.35f, blockChance = 0.25f, catchChance = 0.2f, landChance = 0.2f, counterAfterBlock = 0.75f, midAttackLandChance = 0.35f;
    public float reactCooldown = 0.5f, dodgeDepth = 1.0f;

    enum R { None, Dodge, Block, Catch }
    R reaction; float reactT, reactLen, dodgeDirY, nextReactAt;
    readonly Queue<Move> queue = new Queue<Move>();
    bool fleeing; float fleeDir = 1f;
    // heitto
    Move catchCur; bool grabbed, catchDone; PlayerController heldHero; int grabPos, throwPos;

    /// Enemy.TakeHit kutsuu tätä ensimmäisenä: true = isku ohi.
    public new bool InterceptHit(int damage, float attackerX, bool knockdown)
    {
        if (!reactive) return invulnerable;
        if (fleeing) return true;
        if (reaction != R.None && reactT < reactLen * 0.9f) return true;   // reaktion aikana ei osu
        bool busy = cur != null || queue.Count > 0;
        if (busy) return (knockdown && !landKnockdown) || Random.value > midAttackLandChance;   // hyökkäyksen aikana kevyt isku voi osua
        if (Time.time < nextReactAt || e == null || e.Hero == null) return (knockdown && !landKnockdown) || Random.value > landChance;
        bool canCatch = HasSprites(knockdown ? (catchLeg ?? catchHand) : catchHand);
        float cc = canCatch ? catchChance : 0f;
        float r = Random.value * (dodgeChance + blockChance + cc + landChance);
        if (r < dodgeChance && HasSprites(dodgeMove)) { StartDodge(); return true; }
        r -= dodgeChance;
        if (r < blockChance && HasSprites(blockMove)) { StartBlock(); return true; }
        r -= blockChance;
        if (r < cc) { StartCatch(knockdown); return true; }   // kaatava isku (potku) -> jalka, kevyt isku (lyönti) -> käsi
        if (knockdown && !landKnockdown) { if (HasSprites(blockMove)) StartBlock(); return true; }
        return false;   // isku osuu
    }

    static bool HasSprites(Move m) => m != null && m.sprites != null && m.sprites.Length > 0;

    void StartDodge()
    {
        nextReactAt = Time.time + reactCooldown;
        reaction = R.Dodge; reactT = 0f; reactLen = dodgeMove.sprites.Length * dodgeMove.frameTime;
        // väistö ylös tai alas (syvyys): poispäin lähimmästä reunasta, muuten satunnainen
        var a = Area.Current; float y = transform.position.y;
        float up = a != null ? a.MaxDepthAtX(transform.position.x, a.maxDepthY) - y : 1f, down = a != null ? y - a.MinDepthAtX(transform.position.x, a.minDepthY) : 1f;
        dodgeDirY = up < 0.7f ? -1f : down < 0.7f ? 1f : (Random.value < 0.5f ? -1f : 1f);
        Begin(dodgeMove);
        QueueCounter();
    }

    void StartBlock()
    {
        nextReactAt = Time.time + reactCooldown;
        reaction = R.Block; reactT = 0f; reactLen = blockMove.sprites.Length * blockMove.frameTime;
        Begin(blockMove);
        if (Random.value < counterAfterBlock) QueueCounter();
    }

    /// Kiinniotto: kurotus kädestä (lyönti) tai jalasta (potku), veto, selin käännös ja heitto heron yli toiselle puolelle.
    void StartCatch(bool leg)
    {
        nextReactAt = Time.time + reactCooldown * 2f;
        catchCur = leg && HasSprites(catchLeg) ? catchLeg : catchHand;
        grabPos = catchCur == catchLeg ? legGrabPos : handGrabPos; throwPos = catchCur == catchLeg ? legThrowPos : handThrowPos;
        reaction = R.Catch; reactT = 0f; reactLen = catchCur.sprites.Length * catchCur.frameTime + 0.15f;
        grabbed = false; catchDone = false; heldHero = e.Hero;
        queue.Clear();
        Begin(catchCur);
    }

    /// Counter heti perään: nopea suora (kurottaa heroa kohti) + kaatava lopetus.
    void QueueCounter()
    {
        queue.Clear();
        if (HasSprites(counterJab)) queue.Enqueue(counterJab);
        if (counterFinishers != null && counterFinishers.Length > 0) { var f = counterFinishers[Random.Range(0, counterFinishers.Length)]; if (HasSprites(f)) queue.Enqueue(f); }
    }

    /// Lokki pakenee: toistaa juoksuliikettä (nopea) annettuun suuntaan, ei enää hyökkää.
    public void Flee(float dir)
    {
        fleeing = true; fleeDir = dir; chancePerSec = 0f; queue.Clear(); ReleaseHeld();
    }

    public bool Fleeing => fleeing;

    void ReleaseHeld()
    {
        if (grabbed && heldHero != null) heldHero.ReleaseGrab();
        grabbed = false;
    }

    /// Heiton vaiheet kuvajonon kohdan mukaan: ote (BeginGrab), veto/nosto (SetHeld) ja heitto (Throw) toiselle puolelle.
    void CatchStep()
    {
        if (catchCur == null || cur != catchCur || e == null || heldHero == null || catchDone) return;
        int pos = System.Array.IndexOf(cur.sprites, CurrentSprite);
        if (pos < 0) return;
        Vector3 me = transform.position, hp = heldHero.transform.position;
        if (!grabbed && pos >= grabPos)
        {
            bool inReach = Mathf.Abs(hp.x - me.x) <= grabReach && Mathf.Abs(hp.y - me.y) <= e.depthTolerance + 0.6f;
            if (inReach && heldHero.BeginGrab(me.x)) grabbed = true; else catchDone = true;   // ohi (hero ilmassa tms.): pelkkä liike
        }
        if (grabbed)
        {
            float u = Mathf.Clamp01((pos - grabPos) / (float)Mathf.Max(1, throwPos - grabPos));
            float dir = dirSign;
            heldHero.SetHeld(new Vector3(me.x + dir * Mathf.Lerp(1.0f, 0f, u), me.y, 0f), Mathf.Lerp(0.2f, 2.3f, u), -dir * Mathf.Lerp(0f, 80f, u));
            if (pos >= throwPos)
            {
                heldHero.Throw(-dir * throwSpeed, throwUp, throwDamage);   // toiselle puolelle (Lokin selän taakse)
                grabbed = false; catchDone = true;
                CameraFollow.Shake(0.1f, 0.15f);
            }
        }
    }

    /// Aivon tikki: väistön syvyysliike, heiton vaiheet ja jonon seuraava liike heti edellisen jälkeen.
    public new void Tick(float dt)
    {
        if (reaction != R.None) reactT += dt;
        if (reaction == R.Dodge && cur == dodgeMove && e != null)
        {
            float u = Mathf.Clamp01(reactT / Mathf.Max(0.01f, reactLen));
            float amp = dodgeDepth * 0.55f;
            e.BrainMove(new Vector2(0f, dodgeDirY * amp * Mathf.PI / Mathf.Max(0.1f, reactLen) * Mathf.Cos(Mathf.PI * u) * dt));   // ulos ja takaisin (sinikäyrä)
        }
        if (reaction == R.Catch) CatchStep();
        base.Tick(dt);
        if (cur == null)
        {
            ReleaseHeld();   // liike päättyi kesken: hero irti
            if (reaction != R.None) reaction = R.None;
            if (queue.Count > 0 && e != null && e.Hero != null && !fleeing) Begin(queue.Dequeue());
        }
    }

    void LateUpdate()
    {
        if (!fleeing || e == null || !HasSprites(fleeMove)) return;
        if (cur != null) { e.FacingRight = fleeDir > 0f; dirSign = fleeDir; return; }
        Begin(fleeMove);
        e.FacingRight = fleeDir > 0f; dirSign = fleeDir;
    }

}
