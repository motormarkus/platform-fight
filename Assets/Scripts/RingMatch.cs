using System.Collections;
using UnityEngine;

/// <summary>
/// WTF-areenan kehä (valikko 435): kaksi nyrkkeilijää (Marcus ja Deacon) ottelee keskenään, kun hero tulee katsomoon.
/// Kumpikaan ei kuole (sparraus): ensimmäinen KO ratkaisee voittajan. Häviäjä kannetaan pois, voittaja haastaa heron:
/// kun hero nousee kehään, alkaa ottelu voittajaa vastaan (hero ei kuole; KO = häviö, voi yrittää uudelleen).
/// Väliin meno: jos hero nousee kehään kesken näytöksen, kaikki tappelevat kaikkia vastaan (ottelijat vaihtavat kohdetta toisen ja heron välillä).
/// Kehä on SiteLevels-taso 1 (ottelijat pysyvät siellä: Enemy.stayOnLevel). Aivot ottavat vastustajansa Enemy.duelFoe-kentästä (tyhjä = hero).
/// </summary>
public class RingMatch : MonoBehaviour
{
    public Area area;
    public SiteLevels levels;
    public Enemy fighterA, fighterB;
    [Tooltip("Aloituspaikat kehässä (jalat).")]
    public Vector2 cornerA, cornerB;
    [Tooltip("Näytösottelun energia (pieni: ottelu kestää noin puoli minuuttia).")]
    public int exhibitionHealth = 150;
    [Tooltip("Voittajan energia heroa vastaan.")]
    public int challengeHealth = 420;
    public float startDelay = 2.5f, winnerPause = 3.5f;
    [Tooltip("Häviön jälkeen hero herää tästä (kehän edestä lattialta).")]
    public Vector2 heroWakeSpot;
    public string championClue = "vegas_wtf_mestari";
    [Tooltip("Väliin menossa ottelija vaihtaa kohdetta (toinen ottelija tai hero) näin usein (s).")]
    public Vector2 brawlRetarget = new Vector2(1.2f, 2.4f);
    [Tooltip("Väliin menossa heron etäisyys kerrotaan tällä (< 1 = hero houkuttelevampi kohde).")]
    public float brawlHeroBias = 0.8f;

    enum Phase { Waiting, Exhibition, Victory, Challenge, HeroFight, Brawl, HeroWon, HeroLost }
    Phase phase = Phase.Waiting;
    float timer;
    Enemy winner, loser;
    readonly float[] retargetAt = new float[2];
    readonly float[] koTime = { -1f, -1f };
    string banner, sub;
    float bannerStart = -99f, bannerUntil = -99f;
    GUIStyle style, subStyle;

    Enemy[] Fighters => new[] { fighterA, fighterB };
    static bool Standing(Enemy f) => f != null && f.gameObject.activeSelf && !f.SparKO;
    Enemy Other(Enemy f) => f == fighterA ? fighterB : fighterA;

    void OnEnable() { PlayerController.SparDefeated += OnHeroDefeated; }
    void OnDisable() { PlayerController.SparDefeated -= OnHeroDefeated; if (phase == Phase.HeroFight || phase == Phase.Brawl) PlayerController.Sparring = false; }

    void Start()
    {
        foreach (var f in Fighters)
        {
            if (f == null) continue;
            f.sparring = true; f.sparBelt = false; f.stayOnLevel = true;
            SetHold(f, true);
        }
        Place(fighterA, cornerA, true); Place(fighterB, cornerB, false);
        if (levels != null) levels.laddersLocked = true;
    }

    static void Place(Enemy f, Vector2 p, bool faceRight)
    {
        if (f == null) return;
        f.transform.position = new Vector3(p.x, p.y, 0f);
        f.FacingRight = faceRight;
    }

    static void SetHold(Enemy f, bool on)
    {
        if (f == null) return;
        var m = f.GetComponent<MarcusBoxer>(); if (m != null) { m.hold = on; m.manageHeroSparring = false; }
        var d = f.GetComponent<DeaconBoss>(); if (d != null) d.hold = on;
        if (on) f.IsAwake = false;
    }

    static string Name(Enemy f) => f != null ? f.displayName.ToUpperInvariant() : "";
    void Banner(string text, string subText = null, float time = 3f)
    {
        banner = text; sub = subText; bannerStart = Time.unscaledTime; bannerUntil = Time.unscaledTime + time;
    }

    bool HeroHere
    {
        get
        {
            var h = Players.Primary;
            return h != null && Area.Current == area && h.transform.position.x >= area.walkMinX - 1f && h.transform.position.x <= area.walkMaxX + 1f;
        }
    }

    bool HeroInRing
    {
        get
        {
            var h = Players.Primary;
            if (h == null || levels == null || levels.levelY == null || levels.levelY.Length < 2) return false;
            float y = h.transform.position.y;
            return Area.Current == area && y >= levels.levelY[1] - levels.bandHalf - 0.02f && y <= levels.levelY[1] + levels.bandHalf + 0.02f && h.AirHeight < 0.1f;
        }
    }

    /// Ottelun aikana kehästä ei pääse pois (portaat kiinni, ei hyppyä edestä); muuten portaat auki.
    void LockRing(bool fight)
    {
        if (levels == null) return;
        levels.laddersLocked = fight; levels.edgeDrop = !fight;
    }

    void Update()
    {
        if (fighterA == null || fighterB == null) return;
        bool here = HeroHere;
        timer += Time.deltaTime;
        switch (phase)
        {
            case Phase.Waiting:
                if (!here) { timer = 0f; break; }
                if (timer >= startDelay) StartExhibition();
                break;

            case Phase.Exhibition:
                // hero lähti katsomosta: ottelu tauolle (muuten ottelijat valuisivat toisen alueen syvyysrajoihin)
                SetHold(fighterA, !here); SetHold(fighterB, !here);
                if (here) { fighterA.IsAwake = true; fighterB.IsAwake = true; }
                if (fighterA.SparKO || fighterB.SparKO) { EndExhibition(fighterA.SparKO ? fighterB : fighterA); break; }
                if (HeroInRing) StartBrawl();   // hero nousi kehään kesken näytöksen: kaikki kaikkia vastaan
                break;

            case Phase.Victory:
                if (timer >= winnerPause && loser != null && loser.gameObject.activeSelf) StartCoroutine(CarryOff(loser));
                if (HeroInRing)
                {
                    if (loser != null && loser.gameObject.activeSelf) StartCoroutine(CarryOff(loser));
                    StartHeroFight();
                    break;
                }
                if (timer >= winnerPause + 1.2f)
                {
                    phase = Phase.Challenge; timer = 0f;
                    Banner(Name(winner) + " CHALLENGES YOU!", "Nouse kehään portaista", 4.5f);
                }
                break;

            case Phase.Challenge:
                SetHold(winner, true);
                if (HeroInRing) StartHeroFight();
                break;

            case Phase.HeroFight:
                PlayerController.Sparring = true;
                if (winner.SparKO) HeroWins("WTF-mestaruus on sinun", "Voitit " + winner.displayName + "in WTF-areenan kehässä Las Vegasissa.");
                break;

            case Phase.Brawl:
                BrawlUpdate();
                break;

            case Phase.HeroWon:
                if (timer >= 4f) foreach (var f in Fighters) if (f != null && f.gameObject.activeSelf && f.SparKO) StartCoroutine(CarryOff(f));
                break;
        }
    }

    void StartExhibition()
    {
        phase = Phase.Exhibition; timer = 0f;
        foreach (var f in Fighters) { f.Renew(exhibitionHealth); f.SparReset(); }
        fighterA.duelFoe = fighterB; fighterB.duelFoe = fighterA;
        SetHold(fighterA, false); SetHold(fighterB, false);
        fighterA.IsAwake = true; fighterB.IsAwake = true;
        LockRing(false);   // väliin saa mennä
        Banner(Name(fighterA) + "  VS  " + Name(fighterB), "Näytösottelu – voit mennä väliin", 3f);
    }

    void EndExhibition(Enemy w)
    {
        winner = w; loser = Other(w);
        phase = Phase.Victory; timer = 0f;
        winner.duelFoe = null; loser.duelFoe = null;
        SetHold(winner, true);
        Banner("KO!", Name(winner) + " WINS", winnerPause);
    }

    void StartHeroFight()
    {
        phase = Phase.HeroFight; timer = 0f;
        LockRing(true);
        winner.Renew(challengeHealth); winner.SparReset();
        winner.duelFoe = null;
        SetHold(winner, false); winner.IsAwake = true;
        PlayerController.Sparring = true;
        Banner("FIGHT!", null, 1.6f);
    }

    // ---------------- väliin meno: kaikki kaikkia vastaan ----------------

    void StartBrawl()
    {
        phase = Phase.Brawl; timer = 0f;
        LockRing(true);
        koTime[0] = koTime[1] = -1f;
        retargetAt[0] = retargetAt[1] = 0f;
        PlayerController.Sparring = true;
        Banner("BRAWL!", "Kaikki kaikkia vastaan", 2.2f);
    }

    void BrawlUpdate()
    {
        PlayerController.Sparring = true;
        var hero = Players.Primary;
        var fs = Fighters;
        for (int i = 0; i < 2; i++)
        {
            var f = fs[i];
            if (!Standing(f))
            {
                if (f != null && f.gameObject.activeSelf && f.SparKO)
                {
                    if (koTime[i] < 0f) { koTime[i] = Time.time; f.duelFoe = null; Banner("KO!", Name(f) + " IS DOWN", 2f); }
                    else if (Time.time - koTime[i] > 3f) StartCoroutine(CarryOff(f));
                }
                continue;
            }
            SetHold(f, false); f.IsAwake = true;
            if (Time.time < retargetAt[i] && (f.duelFoe == null || Standing(f.duelFoe))) continue;
            retargetAt[i] = Time.time + Random.Range(brawlRetarget.x, brawlRetarget.y);
            // kohde: toinen ottelija (jos pystyssä) tai hero; lähempi voittaa, hero hieman houkuttelevampi, vähän satunnaisuutta
            var o = Other(f);
            Vector3 me = f.transform.position;
            float dOther = Standing(o) ? Mathf.Abs(o.transform.position.x - me.x) + 2f * Mathf.Abs(o.transform.position.y - me.y) : float.MaxValue;
            float dHero = hero != null ? (Mathf.Abs(hero.transform.position.x - me.x) + 2f * Mathf.Abs(hero.transform.position.y - me.y)) * brawlHeroBias * Random.Range(0.75f, 1.25f) : float.MaxValue;
            f.duelFoe = dOther < dHero ? o : null;
        }
        if (!Standing(fighterA) && !Standing(fighterB))
            HeroWins("Kaadoit molemmat", "Menit väliin WTF-areenan näytösotteluun Las Vegasissa ja kaadoit sekä Marcusin että Deaconin.");
    }

    void HeroWins(string subText, string clueText)
    {
        phase = Phase.HeroWon; timer = 0f;
        PlayerController.Sparring = false;
        foreach (var f in Fighters) if (f != null) { f.duelFoe = null; SetHold(f, true); }
        LockRing(false);
        Banner("YOU WIN!", subText, 4f);
        if (!string.IsNullOrEmpty(championClue)) Clues.Add(championClue, "WTF-mestaruus", clueText);
    }

    void OnHeroDefeated(PlayerController hero)
    {
        if ((phase != Phase.HeroFight && phase != Phase.Brawl) || hero == null) return;
        phase = Phase.HeroLost;
        StartCoroutine(HeroLostRoutine(hero));
    }

    IEnumerator HeroLostRoutine(PlayerController hero)
    {
        PlayerController.Sparring = false;
        var standing = System.Array.FindAll(Fighters, Standing);
        foreach (var f in Fighters) if (f != null) { f.duelFoe = null; SetHold(f, true); }
        Enemy near = null;
        foreach (var f in standing)
            if (near == null || Mathf.Abs(f.transform.position.x - hero.transform.position.x) < Mathf.Abs(near.transform.position.x - hero.transform.position.x)) near = f;
        hero.SparKnockOut(near != null ? near.transform.position.x : hero.transform.position.x + 1f);
        Banner("YOU LOSE", standing.Length == 1 ? Name(standing[0]) + " WINS" : null, 3.5f);
        yield return new WaitForSecondsRealtime(1.2f);
        hero.enabled = false;
        yield return new WaitForSecondsRealtime(1.6f);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = t / 0.5f; yield return null; }
        ScreenFader.Alpha = 1f;
        var pos = new Vector3(heroWakeSpot.x, heroWakeSpot.y, 0f);
        hero.TeleportTo(pos);
        hero.enabled = true;
        hero.health = Mathf.Max(hero.health, hero.maxHealth / 2);
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);
        foreach (var f in Fighters) if (f != null && f.gameObject.activeSelf && f.SparKO) StartCoroutine(CarryOff(f));   // kanveesiin jääneet viedään pois
        foreach (var f in standing) { f.WarHeal(); f.SparReset(); Place(f, f == fighterA ? cornerA : cornerB, f == fighterA); }
        LockRing(false);
        yield return new WaitForSecondsRealtime(0.15f);
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / 0.4f; yield return null; }
        ScreenFader.Alpha = 0f;
        timer = 0f;
        if (standing.Length >= 2) StartExhibition();   // molemmat pystyssä: näytös alkaa alusta
        else if (standing.Length == 1)
        {
            winner = standing[0]; loser = Other(winner);
            phase = Phase.Challenge;
            Banner(Name(winner) + " WAITS", "Nouse kehään uudelleen", 3f);
        }
        else phase = Phase.HeroWon;
    }

    /// Kanveesiin jäänyt ottelija viedään pois (häivytys).
    IEnumerator CarryOff(Enemy f)
    {
        if (f == null || !f.enabled) yield break;   // jo menossa
        f.enabled = false;   // Enemy värittää kehon joka ruudulla: pysäytetään, jotta häivytys näkyy
        var rs = f.GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            foreach (var r in rs) if (r != null) { var c = r.color; c.a = 1f - t / 0.8f; r.color = c; }
            yield return null;
        }
        f.gameObject.SetActive(false);
    }

    void OnGUI()
    {
        if (banner == null || Time.unscaledTime > bannerUntil) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            subStyle = new GUIStyle(style) { fontStyle = FontStyle.Bold };
        }
        style.fontSize = Mathf.RoundToInt(Screen.height * 0.11f);
        subStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.045f);
        float a = Mathf.Clamp01((Time.unscaledTime - bannerStart) / 0.3f) * Mathf.Clamp01((bannerUntil - Time.unscaledTime) / 0.4f);
        GUI.depth = -300;
        var r = new Rect(0f, Screen.height * 0.22f, Screen.width, Screen.height * 0.18f);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
        GUI.Label(new Rect(r.x + 4f, r.y + 5f, r.width, r.height), banner, style);
        style.normal.textColor = new Color(1f, 0.82f, 0.2f, a);
        GUI.Label(r, banner, style);
        if (!string.IsNullOrEmpty(sub))
        {
            var r2 = new Rect(0f, r.yMax, Screen.width, Screen.height * 0.07f);
            subStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
            GUI.Label(new Rect(r2.x + 3f, r2.y + 3f, r2.width, r2.height), sub, subStyle);
            subStyle.normal.textColor = new Color(1f, 1f, 1f, a);
            GUI.Label(r2, sub, subStyle);
        }
    }
}
