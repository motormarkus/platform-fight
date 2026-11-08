using UnityEngine;

/// <summary>
/// Mainari: osittainen aivo Enemyn päällä. Tavallinen tekoäly hoitaa kävelyn, juoksun, jabin, torjunnan ja kaatumisen; tämä lisää erikoisliikkeen, kuperkeikan:
/// mainari kyykistyy, ponnistaa ja pyörii kerällä heroa kohti (pyöriminen silmukkana, kunnes osuu tai aika loppuu), kaataa osuessaan (ei voi torjua; väistö vain hyppäämällä).
/// Pyörintä piirretään koodilla pyöritettävällä kuvalla (mainari_pallo), alku ja loppu (nousu) ovat omat kuvasarjat.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class MinerBrain : MonoBehaviour, IEnemyBrain, IPartialBrain
{
    public Sprite[] windupSprites, landSprites, ballSprites;
    public float windupFrameTime = 0.05f, landFrameTime = 0.07f, spinDegPerSec = 900f, ballFrameTime = 0.08f;
    public float cooldown = 7f, minRange = 4f, maxRange = 14f, chancePerSec = 0.8f, runChancePerSec = 1.4f;
    [Tooltip("Juoksusta aloitettaessa ponnistus on lyhyt: vain kuvasarjan viimeiset kuvat, ja liike jatkuu eteenpäin.")]
    public int runWindupFrames = 3; public float runWindupFrameTime = 0.035f, runWindupSpeed = 8f;
    public float rollSpeed = 13f, rollMaxTime = 1.7f, hitReach = 1.2f, overshoot = 7f;
    public int damage = 20;
    [Tooltip("Pallon keskipisteen korkeus jaloista (maailmayksikköä).")]
    public float ballHeight = 0.9f;

    enum Ph { None, Windup, Roll, Land }
    bool fromRun; int windupStart; float windupTime;
    Ph ph; float t, rollT, nextRoll = 3f, lastTick, spin, travelled; bool hitDone;
    Enemy e; SpriteRenderer ball; Sprite blank;
    float dirSign = 1f;

    public bool Active => ph != Ph.None;
    public Sprite CurrentSprite { get; private set; }
    public bool HasSprite => ph != Ph.None && CurrentSprite != null;
    public bool ArmorNow => false;   // kuperkeikan voi katkaista iskulla
    public bool InterceptHit(int damage, float attackerX, bool knockdown) => false;

    void Awake()
    {
        e = GetComponent<Enemy>(); nextRoll = Random.Range(2f, 6f);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.SetPixels32(new Color32[4]); tex.Apply();
        blank = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0f), 100f);
    }

    void EnsureBall()
    {
        if (ball != null) return;
        var go = new GameObject("Pyörintä"); go.transform.SetParent(transform, false);
        ball = go.AddComponent<SpriteRenderer>();
        if (e.body != null) { ball.sortingLayerID = e.body.sortingLayerID; }
        ball.enabled = false;
    }

    void OnDisable() { if (ball != null) ball.enabled = false; ph = Ph.None; }

    void Update()
    {
        if (ph != Ph.None && Time.time - lastTick > 0.25f) { End(); return; }   // väliin tuli kaatuminen tms.
        if (ph != Ph.None || e == null || e.Hero == null || !e.IsAwake || !e.BrainIdleOrChase || e.IsDead) return;
        if (windupSprites == null || windupSprites.Length == 0 || ballSprites == null || ballSprites.Length == 0 || Time.time < nextRoll) return;
        float dx = Mathf.Abs(e.Hero.transform.position.x - transform.position.x), dy = Mathf.Abs(e.Hero.transform.position.y - transform.position.y);
        if (dx < minRange || dx > maxRange || dy > e.depthTolerance + 1.2f) return;
        bool running = dx > e.runDistance;   // tulee juosten: kuperkeikka alkaa suoraan juoksusta (lyhyt ponnistus, ei pysähdystä)
        if (Random.value > (running ? runChancePerSec : chancePerSec) * Time.deltaTime) return;
        fromRun = running;
        nextRoll = Time.time + cooldown * Random.Range(0.8f, 1.3f);
        Begin();
    }

    void Begin()
    {
        EnsureBall();
        ph = Ph.Windup; t = 0f; rollT = 0f; spin = 0f; travelled = 0f; hitDone = false; lastTick = Time.time;
        e.FacingRight = e.Hero.transform.position.x > transform.position.x;
        dirSign = e.FacingRight ? 1f : -1f;
        windupStart = fromRun ? Mathf.Max(0, windupSprites.Length - runWindupFrames) : 0;
        windupTime = fromRun ? runWindupFrameTime : windupFrameTime;
        CurrentSprite = windupSprites[windupStart];
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
    }

    void End()
    {
        ph = Ph.None;
        if (ball != null) ball.enabled = false;
        CurrentSprite = null;
        if (e != null && !e.IsDead) e.BrainPhaseSet(Enemy.BrainPhase.Chase);
    }

    public void Tick(float dt)
    {
        if (ph == Ph.None) return;
        lastTick = Time.time; t += dt;
        e.BrainPhaseSet(Enemy.BrainPhase.Windup);
        var hero = e.Hero;
        switch (ph)
        {
            case Ph.Windup:
            {
                int count = windupSprites.Length - windupStart;
                int i = Mathf.Min(windupStart + AttackEase.Wind(t, count * windupTime, count), windupSprites.Length - 1);   // veto kiihtyy
                CurrentSprite = windupSprites[i];
                if (fromRun) e.BrainMove(new Vector2(dirSign * runWindupSpeed * dt, 0f));
                if (hero != null) { e.FacingRight = hero.transform.position.x > transform.position.x; dirSign = e.FacingRight ? 1f : -1f; }
                if (t >= count * windupTime) { ph = Ph.Roll; t = 0f; HitFx.PlayWhoosh(); }
                break;
            }
            case Ph.Roll:
            {
                rollT += dt;
                CurrentSprite = blank;   // oma kuva piilotetaan, pallo piirretään lapsiobjektilla
                spin -= dirSign * spinDegPerSec * dt;
                ball.enabled = true;
                int bi = (int)(rollT / ballFrameTime) % ballSprites.Length;
                ball.sprite = ballSprites[bi];
                ball.transform.localPosition = new Vector3(0f, ballHeight, 0f);
                ball.transform.localRotation = Quaternion.Euler(0f, 0f, spin);
                ball.flipX = false;
                if (e.body != null) ball.sortingOrder = e.body.sortingOrder;
                float step = rollSpeed * dt;
                // pyörii kohti heroa ja seuraa hieman syvyyssuunnassa
                Vector3 me = transform.position;
                float dy = 0f;
                if (hero != null) dy = Mathf.Clamp(hero.transform.position.y - me.y, -2.5f * dt, 2.5f * dt);
                e.BrainMove(new Vector2(dirSign * step, dy));
                travelled += step;
                if (hero != null && !hitDone)
                {
                    Vector3 hp = hero.transform.position; Vector3 mp = transform.position;
                    float dxh = hp.x - mp.x;
                    bool near = Mathf.Abs(dxh) <= hitReach && Mathf.Abs(hp.y - mp.y) <= e.depthTolerance + 0.3f && hero.AirHeight <= 0.9f;   // hyppy väistää
                    if (near)
                    {
                        hitDone = true;
                        if (!hero.TakeKnockdown(damage, mp.x, 9f, 5f, e, true)) hero.TakeHit(damage, mp.x, e, true);
                        StartLand();
                        break;
                    }
                    bool passed = (dirSign > 0f ? dxh < -overshoot * 0.3f : dxh > overshoot * 0.3f) && travelled > 2f;   // meni ohi
                    if (passed && Mathf.Abs(dxh) > overshoot) { StartLand(); break; }
                }
                if (rollT >= rollMaxTime) StartLand();
                break;
            }
            case Ph.Land:
            {
                if (landSprites == null || landSprites.Length == 0) { End(); break; }
                int i = Mathf.Min((int)(t / landFrameTime), landSprites.Length - 1);
                CurrentSprite = landSprites[i];
                if (t >= landSprites.Length * landFrameTime) End();
                break;
            }
        }
    }

    void StartLand()
    {
        ph = Ph.Land; t = 0f;
        if (ball != null) ball.enabled = false;
        CurrentSprite = landSprites != null && landSprites.Length > 0 ? landSprites[0] : blank;
        CameraFollow.Shake(0.05f, 0.15f);
    }
}
