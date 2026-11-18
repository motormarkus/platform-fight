using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pomojen pudotukset (suunnitelma: muisti pomopudotukset). Voitettavat pomot järjestyksessä:
///  - parittomat (L): +10 % maksimielämä (pysyvä, kasautuu, täyttää elämät)
///  - Horhe, Romuukko, Yakuza-pomo, Joulupukki: pyörähdyspotkun taso +1 (0-4); taso 4 = täysi voima (nykyiset arvot)
///  - muille pomoille (Temppelin kuningas, Warden, robotti, Kenraali) ei vielä pudotusta; Lokki ei pudota.
/// Tila on staattinen (säilyy kenttien ja uudelleenaloituksen yli) ja nollataan uudessa pelissä (GameMenu). Jaettu molemmille pelaajille.
/// </summary>
public static class BossPerks
{
    public const int MaxSpecialLevel = 4;
    public const float LifeStep = 0.10f;
    public static int LifeCount { get; private set; }
    public static int SpecialLevel { get; private set; }
    static readonly HashSet<string> done = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnLoad() { Reset(); }
    public static void Reset() { LifeCount = 0; SpecialLevel = 0; done.Clear(); bullSprites = null; bullNext = 0f; skelTemplate = null; skelNext = 0f; }

    static readonly string[] LifeBosses = { "Metsuri", "Kapteeni", "Deacon", "Gustav", "Timppa", "Nokkamies", "Koe-olento" };
    static readonly string[] SpecialBosses = { "Horhe", "Romuukko", "Yakuza-pomo", "Joulupukki" };

    /// Erikoisliikkeen voimakerroin tasolla (taso 4 = 1).
    public static float SpecialFactor => Mathf.Lerp(0.4f, 1f, Mathf.Clamp01(SpecialLevel / (float)MaxSpecialLevel));

    /// Lisäenergia peruselämän päälle.
    public static int LifeBonus(int baseMax) => Mathf.RoundToInt(baseMax * LifeStep * LifeCount);

    /// Pomo kaatui. Palauttaa true, jos pudotus annettiin.
    public static bool Defeated(string boss, Vector3 pos)
    {
        string scene = Area.Current != null ? Area.Current.areaName : "";
        if (scene.IndexOf("alus", System.StringComparison.OrdinalIgnoreCase) >= 0 || scene.IndexOf("aluksen", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;   // aluksessa (lopputaistelu) ei enää palkintoja (alueiden nimet: "Aluksen cargo" jne.)
        string key = boss == "Kapteeni" ? boss + "|" + scene : boss;   // sama pomo palkitsee vain kerran (Kapteeneja on kaksi eri paikoissa)
        if (done.Contains(key)) return false;
        if (System.Array.IndexOf(LifeBosses, boss) >= 0)
        {
            done.Add(key); LifeCount++;
            foreach (var pc in Players.All)
            {
                if (pc == null) continue;
                int add = Mathf.RoundToInt(GameSettings.StartHealth * LifeStep);
                pc.AddMaxHealth(add);
                pc.Heal(pc.maxHealth);
            }
            Announce("+10 % ENERGIAA", pos, new Color(1f, 0.35f, 0.4f));
            return true;
        }
        if (boss == "Taikuri")
        {
            key = boss;   // sama kyky vain kerran, vaikka taikureita on useassa paikassa
            if (done.Contains(key)) return false;
            if (CacheBull()) { done.Add(key); Announce("HÄRKÄ-KYKY: suojaus + pusku (ohjaimella suojaus + L3)", pos, new Color(1f, 0.5f, 0.3f)); return true; }
            return false;
        }
        if (boss == "Temppelin kuningas")
        {
            if (CacheSkeletons()) { done.Add(key); Announce("LUURANKOJEN KUTSU: suojaus (I) + L", pos, new Color(0.8f, 1f, 0.8f)); return true; }
            return false;
        }
        if (System.Array.IndexOf(SpecialBosses, boss) >= 0 && SpecialLevel < MaxSpecialLevel)
        {
            done.Add(key); SpecialLevel++;
            Announce(SpecialLevel >= MaxSpecialLevel ? "PYÖRÄHDYSPOTKU: TÄYSI VOIMA!" : "PYÖRÄHDYSPOTKU TASO " + SpecialLevel, pos, new Color(1f, 0.85f, 0.25f));
            return true;
        }
        return false;
    }

    // ---- Härkä (Taikuri): hero muuttuu häräksi kerran minuutissa ----
    static Sprite[] bullSprites; static float bullFt = 0.04f, bullSpeed = 15f, bullScale = 1f, bullNext; static int bullFrom;
    public const float BullCooldown = 60f;
    public static bool HasBull => bullSprites != null;

    static bool CacheBull()
    {
        if (bullSprites != null) return true;
        foreach (var mb in Object.FindObjectsByType<MagicianBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null || mb.spells == null) continue;
            foreach (var s in mb.spells)
            {
                if (s == null || s.kind != MagicianBrain.Kind.Bull || s.move == null || s.move.sprites == null || s.move.sprites.Length == 0) continue;
                bullSprites = (Sprite[])s.move.sprites.Clone(); bullFt = Mathf.Max(0.01f, s.move.frameTime); bullFrom = Mathf.Max(0, s.move.moveFrom);
                bullSpeed = Mathf.Max(15f, s.move.moveSpeed);
                var en = mb.GetComponent<Enemy>();
                bullScale = en != null && en.body != null ? Mathf.Max(0.05f, en.body.transform.lossyScale.x) : 1f;
                return true;
            }
        }
        return false;
    }

    public static bool TryBull(PlayerController pc)
    {
        if (pc == null) return false;
        string why = bullSprites == null ? "ei kykyä" : pc.GameOver ? "peli ohi" : pc.BullHidden ? "jo härkä" : pc.Riding ? "ajoneuvossa" : Time.time < bullNext ? "lataus " + Mathf.CeilToInt(bullNext - Time.time) + " s" : null;
        if (why != null) { Debug.Log("[BossPerks] härkä ei lähde: " + why); GameHUD.Popup("HÄRKÄ: " + why, pc.transform.position + new Vector3(0f, 3f, 0f), new Color(1f, 0.6f, 0.4f)); return false; }
        bullNext = Time.time + BullCooldown;
        var go = new GameObject("Heron härkä"); var hb = go.AddComponent<HeroBull>();
        hb.pc = pc; hb.frames = bullSprites; hb.frameTime = bullFt; hb.speed = bullSpeed; hb.scale = bullScale; hb.startFrame = bullFrom; hb.maxTime = 2.4f;
        Debug.Log("[BossPerks] härkä lähti: kuvia " + bullSprites.Length + ", skaala " + bullScale + ", nopeus " + bullSpeed);
        return true;
    }

    // ---- Luurankojen kutsu (Temppelin kuningas) ----
    static GameObject skelTemplate; static float skelNext;
    public const float SkeletonCooldown = 60f, SkeletonLife = 25f; public const int SkeletonCount = 3;
    public static bool HasSkeletonCall => skelTemplate != null;

    /// Kopioi kuninkaan luurankomallin pysyvään muistiin (säilyy kenttien yli), jotta kutsu toimii myöhemmissäkin kentissä.
    static bool CacheSkeletons()
    {
        if (skelTemplate != null) return true;
        foreach (var k in Object.FindObjectsByType<KingBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (k == null || k.summonTemplate == null) continue;
            var c = Object.Instantiate(k.summonTemplate); c.name = "Luurankoapuri (malli)";
            c.transform.SetParent(null, true); c.SetActive(false); Object.DontDestroyOnLoad(c);
            skelTemplate = c; return true;
        }
        return false;
    }

    /// Kutsuu 3 liittolaisluurankoa pelaajan viereen: puolet elämistä (iskut ennallaan), taistelevat 25 s, tauko 30 s.
    public static bool TrySummonSkeletons(PlayerController pc)
    {
        if (skelTemplate == null || pc == null || Time.time < skelNext) return false;
        skelNext = Time.time + SkeletonCooldown;
        var area = Area.Current; float face = pc.FacingRight ? 1f : -1f;
        for (int i = 0; i < SkeletonCount; i++)
        {
            float x = pc.transform.position.x + face * (i == 0 ? 2.4f : i == 1 ? -2.4f : 4.2f), y = pc.transform.position.y + (i - 1) * 0.5f;
            if (area != null) { x = Mathf.Clamp(x, area.walkMinX + 1.5f, area.walkMaxX - 1.5f); y = Mathf.Clamp(y, area.minDepthY + 0.1f, area.maxDepthY - 0.1f); }
            var c = Object.Instantiate(skelTemplate); c.name = "Luurankoapuri " + (i + 1);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(c, pc.gameObject.scene);
            c.transform.position = new Vector3(x, y, 0f);
            var en = c.GetComponent<Enemy>();
            if (en != null)
            {
                en.ally = true; en.fightsEveryone = false; en.huntsBrawlers = false; en.brawlsWithOthers = false; en.joinsFightWhenSquadComes = false;
                en.maxHealth = Mathf.Max(1, en.maxHealth / 2); en.noteValue = 0; en.stackChance = 0f; en.wakeDistance = 60f;
                c.AddComponent<SkeletonAllyLife>().life = SkeletonLife;
            }
            c.SetActive(true);
        }
        HitFx.PlayPickup(false);
        return true;
    }

    /// Testaus: antaa kaikki tähän mennessä saatavat kyvyt (+70 % elämä, pyörähdyspotku täysi, luurankojen kutsu, härkä). Mallit haetaan ladatuista sceneistä (GameStart lataa ne).
    public static void GrantAll()
    {
        Clues.Add(Clues.GlobePass, "Rexin lentolippu", "Maailmanpallo (F8) on auki.", null, false);   // testialoitukset: pallo käytössä ilman Rexiä
        Clues.Add(Clues.WallTicket, "Muurin lippu", "Kiinan muuri on auki pallolla.", null, false);
        Clues.Add(Clues.PrisonPass, "Pääsy vankilaan", "Rekka vie vankilaan.", null, false);
        int add = Mathf.RoundToInt(GameSettings.StartHealth * LifeStep);
        foreach (var b in LifeBosses)
        {
            if (done.Contains(b)) continue;
            done.Add(b); LifeCount++;
            foreach (var pc in Players.All) if (pc != null) pc.AddMaxHealth(add);
        }
        foreach (var b in SpecialBosses) done.Add(b);
        SpecialLevel = MaxSpecialLevel;
        if (CacheSkeletons()) done.Add("Temppelin kuningas");
        if (CacheBull()) done.Add("Taikuri");
        foreach (var pc in Players.All) if (pc != null) pc.Heal(pc.maxHealth);
        Debug.Log("[BossPerks] GrantAll: luurankokutsu " + HasSkeletonCall + ", härkä " + HasBull + ", elämälisä " + LifeCount + ", potkutaso " + SpecialLevel);
    }

    static void Announce(string text, Vector3 pos, Color col)
    {
        GameHUD.Popup(text, pos + new Vector3(0f, 2.4f, 0f), col);
        if (Players.Primary != null) GameHUD.Popup(text, Players.Primary.transform.position + new Vector3(0f, 3.2f, 0f), col);
    }
}
