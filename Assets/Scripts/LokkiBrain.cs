using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lokki: MongolBrainin erikoisliikkeet (suorasarjat, potkut, kämmeniskun aalto, heilautus, polvi) + reaktiivinen puolustus kuten Deaconilla:
/// heron iskuun Lokki joko VÄISTÄÄ (ylös tai alas syvyyssuunnassa), TORJUU (suoja-asento), OTTAA KIINNI (kädestä tai jalasta) ja HEITTÄÄ heron toiselle puolelleen,
/// tai harvoin ottaa osuman. Väistön/torjunnan jälkeen heti COUNTER: nopea suora + kaatava lopetus.
/// Lisäksi pako oikealle (Flee). Käytetään ensiesiintymisessä juna-asemalla (LokkiEncounter).
/// </summary>
public class LokkiBrain : MongolBrain, IEnemyBrain, ICornerBurstBrain
{
    [Tooltip("Täysin voittamaton (kaikki iskut ohi). Oletus pois: Lokkia voi lyödä, mutta hän väistää ja torjuu paljon (reactive).")]
    public bool invulnerable = false;
    [Tooltip("Reaktiivinen puolustus: väistö/torjunta/kiinniotto/osuma todennäköisyyksien mukaan. Pois: kaikki iskut osuvat (tavallinen vihu).")]
    public bool reactive = true;
    [Tooltip("Myös kaatava isku voi osua (landChance); jos pois, kaatavat iskut menevät aina ohi.")]
    public bool landKnockdown = true;
    [Tooltip("Lopputaistelu (linnakkeen katto/alus): heron KAATAVAT iskut menevät perille useammin (finaleKnockLand / finaleKnockLandMid) ja kaatavat Lokin; kevyet iskut pysyvät reaktiivisina. Ei pakoa (ei Flee).")]
    public bool finale = false;
    [Range(0f, 1f)] public float finaleKnockLand = 0.8f, finaleKnockLandMid = 0.85f;
    [Tooltip("Lopputaistelun kombojen tahti: veto (kerroin) ja palautus (osuus) valitaan niin, että askel kestää noin 0,25–0,3 s eli GIF-esikatselun tahdin (nopeampi ei saa olla).")]
    public float finaleComboWindupScale = 1.22f, finaleComboRecoverFraction = 0.37f;
    public Move fleeMove, blockMove, dodgeMove, counterJab;
    public Move[] counterFinishers;
    [Tooltip("Nurkkapurkaus (pomo ajettu nurkkaan): jono liikkeitä järjestyksessä (kyynärpääisku + lennättävä tuplahyppypotku). Keskeyttämätön, ei torjuttavissa: Enemy.CornerBurstOn lennättää heron kauas.")]
    public Move[] burstMoves;
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
        bool blocked = InterceptCore(damage, attackerX, knockdown);
        if (!blocked && !fleeing)
        {
            // isku osuu: oma liike ja jono katkeavat, jotta osumareaktio (kipu/kaatuminen) ehtii loppuun eikä Lokki mätki takaisin kesken sen
            queue.Clear(); ReleaseHeld(); reaction = R.None;
            if (cur != null) End();
        }
        return blocked;
    }

    /// Nurkkapurkaus: kyynärpääisku ja heti perään lennättävä tuplahyppypotku (burstMoves), ei keskeytettävissä.
    public new void CornerBurst()
    {
        if (e == null || e.Hero == null || cur != null) return;
        if (burstMoves == null || burstMoves.Length == 0 || !HasSprites(burstMoves[0])) { base.CornerBurst(); return; }
        queue.Clear(); reaction = R.None; ReleaseHeld(); fleeing = false;
        for (int i = 1; i < burstMoves.Length; i++) if (HasSprites(burstMoves[i])) queue.Enqueue(burstMoves[i]);
        Begin(burstMoves[0]);
    }

    bool InterceptCore(int damage, float attackerX, bool knockdown)
    {
        if (e != null && e.CornerBurstOn) return true;   // purkauksen aikana iskut eivät keskeytä (eikä Lokki torju/väistä/ota kiinni)
        if (!reactive) return invulnerable;
        if (fleeing) return true;
        if (reaction != R.None && reactT < reactLen * 0.9f) return true;   // reaktion aikana ei osu
        if (finale && Enemy.ChainJuggle) return false;   // heron kombo käynnissä: Lokki ei väistä/torju/ota kiinni kesken sen (kombot menevät perille)
        float lc = finale ? Mathf.Max(landChance, knockdown ? finaleKnockLand : 0.6f) : landChance, mlc = finale ? Mathf.Max(midAttackLandChance, knockdown ? finaleKnockLandMid : 0.8f) : midAttackLandChance;
        bool busy = cur != null || queue.Count > 0;
        if (busy) return (knockdown && !landKnockdown) || Random.value > mlc;   // hyökkäyksen aikana kevyt isku voi osua
        if (Time.time < nextReactAt || e == null || e.Hero == null) return (knockdown && !landKnockdown) || Random.value > lc;
        bool canCatch = HasSprites(knockdown ? (catchLeg ?? catchHand) : catchHand);
        float fs = finale ? 0.2f : 1f;   // lopputaistelu: väistö, torjunta ja kiinniotto harvinaisempia -> iskut menevät perille
        float dch = dodgeChance * fs, bch = blockChance * fs;
        float cc = canCatch ? catchChance * fs : 0f;
        float r = Random.value * (dch + bch + cc + lc);
        if (r < dch && HasSprites(dodgeMove)) { StartDodge(); return true; }
        r -= dch;
        if (r < bch && HasSprites(blockMove)) { StartBlock(); return true; }
        r -= bch;
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

    /// Heron counter-isku (Enemy.CounterHit) tai muu keskeytys: liike katkeaa (Tick ei enää tule) -> jonossa odottava counter/kombo peruuntuu, jotta Lokki ei jatka sarjaansa horjahduksen jälkeen.
    bool finaleApplied;
    protected override void Update()
    {
        if (!finaleApplied && finale && e != null) { finaleApplied = true; e.comboInOrder = true; e.comboWindupScale = finaleComboWindupScale; e.comboRecoverFraction = finaleComboRecoverFraction; }   // lopputaistelu: pitkät kombot aina samassa järjestyksessä -> rytmi opittavissa
        if (cur != null && Time.time - lastTick > 0.25f) { queue.Clear(); ReleaseHeld(); if (reaction != R.None) reaction = R.None; }
        base.Update();
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
