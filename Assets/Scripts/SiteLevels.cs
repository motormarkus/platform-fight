using System.Collections.Generic;
using UnityEngine;

/// Monikerroksinen rakennustyömaa (Vegas): katutaso + kolme kerrosta laattojen päällä, kerrokset yhdistävät tikkaat.
/// Alueen y-raja kattaa kaikki kerrokset (Area.depthLimits); tämä komponentti pitää hahmot omalla kerroksellaan (kapea kaista laatalla)
/// ja hoitaa tikkaat: pelaaja menee tikkaille painamalla ylös (tai alas ylhäällä) tikkaiden kohdalla; viholliset kävelevät lähimmälle tikkaalle
/// ja kiipeävät itse, kun pelaaja on toisessa kerroksessa. Kamera siirtyy kerroksen mukaan (levelCamOffset).
public class SiteLevels : MonoBehaviour
{
    [System.Serializable]
    public struct Ladder
    {
        public float x; public int lo, hi;
        [Tooltip("Portaat: yläpään x (alaspäin lähtö tästä, ylöspäin päätyy tähän). Ilman useTop:ia suora tikas (x sama päissä).")]
        public float topX; public bool useTop;
        public float TopX => useTop ? topX : x;
    }

    public Area area;
    [Tooltip("Kerrosten lattiakorkeudet alhaalta ylös; 0 = katutaso (kaista streetMin..streetMax), muut = laatan päällä.")]
    public float[] levelY;
    public float streetMin, streetMax;
    public float bandHalf = 0.28f;
    public float[] levelCamOffset;
    public Ladder[] ladders;
    public float climbSpeed = 3.4f;
    [Tooltip("Työmaan x-alue kadun jatkona (vihut käyttävät tikkaita vain täällä) ja laattojen x-rajat (ylhäällä ei kävellä reunan yli).")]
    public float siteMinX, siteMaxX, slabMinX, slabMaxX;
    [Tooltip("Kerroskohtaiset x-rajat (kerros 0 ohitetaan); jos tyhjä, käytetään slabMinX/slabMaxX kaikille kerroksille.")]
    public float[] levelMinX, levelMaxX;
    [Tooltip("Komponentti toimii vain tällä x-alueella (useampi SiteLevels samalla alueella: työmaa ja konttori). 0/0 = ei rajaa.")]
    public float zoneMinX, zoneMaxX;
    [Tooltip("Reunalta putoaminen: laatan pään yli kävelevä tai lyöty hahmo putoaa kadulle. noDropMin/noDropMax[kerros] = true: tuo pää on seinää vasten (ei pudota).")]
    public bool edgeDrop = true;
    public bool[] noDropMin, noDropMax;
    [Tooltip("Pudotuksen vahinko yksikköä kohden (herolle ja vihuille); hero ei kuole pudotukseen (vähintään 1).")]
    public float fallDamagePerUnit = 1.2f;
    [Tooltip("Pitämällä alas laatan etureunalla (ei tikkaita lähellä) näin kauan hero hyppää alempaan kerrokseen.")]
    public float frontDropHold = 0.3f;

    bool InZone(float x) => zoneMaxX <= zoneMinX || (x >= zoneMinX && x <= zoneMaxX);
    float ClampSlabX(int lv, float x)
    {
        if (!SlabRange(lv, out float lo, out float hi)) return x;
        return Mathf.Clamp(x, lo, hi);
    }
    bool SlabRange(int lv, out float lo, out float hi)
    {
        lo = hi = 0f;
        if (lv <= 0) return false;
        if (levelMinX != null && levelMaxX != null && lv < levelMinX.Length && lv < levelMaxX.Length && levelMaxX[lv] > levelMinX[lv]) { lo = levelMinX[lv]; hi = levelMaxX[lv]; return true; }
        if (slabMaxX > slabMinX) { lo = slabMinX; hi = slabMaxX; return true; }
        return false;
    }
    static bool Flag(bool[] a, int i) => a != null && i < a.Length && a[i];
    /// Onko x laatan pään yli kohdassa, josta voi pudota (ei seinää vasten).
    bool OverDropEdge(int lv, float x)
    {
        if (!edgeDrop || !SlabRange(lv, out float lo, out float hi)) return false;
        return (x < lo && !Flag(noDropMin, lv)) || (x > hi && !Flag(noDropMax, lv));
    }
    float LandY(int lv, float y) => lv == 0 ? Mathf.Clamp(streetMax - 0.2f, streetMin + 0.2f, streetMax) : levelY[lv];
    int FallDamage(float dh) => Mathf.RoundToInt(Mathf.Clamp(dh * fallDamagePerUnit, 4f, 30f));

    class St { public int ladder = -1, to; public bool scripted, up; public float fromY, downHeld; }
    readonly Dictionary<Transform, St> state = new Dictionary<Transform, St>();

    public int LevelOf(float y)
    {
        int lv = 0;
        for (int i = 1; i < levelY.Length; i++) if (y > 0.5f * (levelY[i - 1] + levelY[i])) lv = i;
        return lv;
    }
    float MinY(int lv) => lv == 0 ? streetMin : levelY[lv] - bandHalf;
    float MaxY(int lv) => lv == 0 ? streetMax : levelY[lv] + bandHalf;
    float EntryY(int lv, float y) => lv == 0 ? Mathf.Min(streetMax - 0.15f, Mathf.Max(streetMin + 0.2f, y)) : levelY[lv];

    static readonly List<SiteLevels> live = new List<SiteLevels>();
    void OnEnable() { live.Add(this); }
    void OnDisable() { live.Remove(this); }

    /// Onko kohta kerroksen laatalla (ei katutasolla): jalkakäytävän pykälä (Area.useSidewalk) ei koske ylempiä kerroksia.
    public static bool AboveStreet(Vector3 p)
    {
        var a = Area.Current;
        for (int i = 0; i < live.Count; i++)
        {
            var s = live[i];
            if (s != null && s.area == a && s.InZone(p.x) && p.y > s.streetMax + 0.05f) return true;
        }
        return false;
    }

    public static SiteLevels For(Area a) { foreach (var t in FindObjectsByType<SiteLevels>(FindObjectsSortMode.None)) if (t != null && t.area == a) return t; return null; }

    void LateUpdate()
    {
        if (area == null || Area.Current != area || levelY == null || levelY.Length < 2) { ReleaseAll(); return; }
        float dt = Time.deltaTime;
        foreach (var pl in Players.All) if (pl != null && (InZone(pl.transform.position.x) || Get(pl.transform).ladder >= 0)) StepHero(pl, dt);
        var prim = Players.Primary;
        if (prim != null && InZone(prim.transform.position.x) && CameraFollow.Instance != null && levelCamOffset != null)
        {
            int lv = LevelOf(prim.transform.position.y + prim.AirHeight);   // pudotessa kamera seuraa näkyvää korkeutta
            if (lv < levelCamOffset.Length) CameraFollow.Instance.OffsetY = levelCamOffset[lv];
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || !en.isActiveAndEnabled || en.IsDead) continue;
            if (en.transform.position.x < area.walkMinX - 6f || en.transform.position.x > area.walkMaxX + 6f) continue;
            if (!InZone(en.transform.position.x) && Get(en.transform).ladder < 0) continue;
            StepEnemy(en, dt);
        }
    }

    void ReleaseAll()
    {
        foreach (var kv in state) if (kv.Value.scripted && kv.Key != null) { var pc = kv.Key.GetComponent<PlayerController>(); if (pc != null) pc.Scripted = false; }
        state.Clear();
    }

    St Get(Transform t) { if (!state.TryGetValue(t, out var s)) { s = new St(); state[t] = s; } return s; }

    int NearestLadder(float x, int fromLv, int toLv, float maxDx)
    {
        int best = -1; float bd = maxDx;
        for (int i = 0; i < ladders.Length; i++)
        {
            var l = ladders[i];
            bool ok = (toLv > fromLv && l.lo == fromLv) || (toLv < fromLv && l.hi == fromLv);
            if (!ok) continue;
            float d = Mathf.Abs((toLv > fromLv ? l.x : l.TopX) - x);
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }

    void StepHero(PlayerController pl, float dt)
    {
        var st = Get(pl.transform);
        Vector3 p = pl.transform.position;
        if (st.ladder >= 0)
        {
            var l = ladders[st.ladder];
            float ty = EntryY(st.to, p.y);
            float cx0 = st.up ? l.x : l.TopX, cx1 = st.up ? l.TopX : l.x;
            p.x = Mathf.MoveTowards(p.x, Mathf.Lerp(cx0, cx1, Mathf.InverseLerp(st.fromY, ty, p.y)), 10f * dt);
            p.y = Mathf.MoveTowards(p.y, ty, climbSpeed * dt);
            pl.transform.position = p;
            pl.Scripted = true; st.scripted = true; pl.ScriptedMove = new Vector2(0f, Mathf.Sign(ty - p.y) * 0.5f);
            if (Mathf.Abs(p.y - ty) < 0.02f) { p.x = cx1; pl.transform.position = p; st.ladder = -1; pl.Scripted = false; st.scripted = false; pl.ScriptedMove = Vector2.zero; }
            return;
        }
        int lv = LevelOf(p.y);
        p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
        if (lv > 0 && !pl.Scripted && OverDropEdge(lv, p.x))   // laatan pään yli: putoaa kadulle
        {
            pl.transform.position = p; st.downHeld = 0f;
            float ny = LandY(0, p.y); pl.DropToLevel(ny, FallDamage(p.y - ny));
            return;
        }
        p.x = ClampSlabX(lv, p.x);
        if (pl.AirHeight < 0.1f && !pl.Scripted)
        {
            float my = CoopInput.Move(pl.playerIndex).y;
            if (my > 0.7f && lv + 1 < levelY.Length) TryClimb(pl, st, p, lv, lv + 1);
            else if (my < -0.7f && lv > 0 && p.y >= levelY[lv] - bandHalf) TryClimb(pl, st, p, lv, lv - 1);
            // etureunalta alas (ei tikkaita lähellä): pito -> hyppy alempaan kerrokseen
            if (st.ladder < 0 && my < -0.7f && lv > 0 && p.y <= MinY(lv) + 0.04f && edgeDrop)
            {
                st.downHeld += dt;
                if (st.downHeld >= frontDropHold)
                {
                    st.downHeld = 0f; pl.transform.position = p;
                    float ny = LandY(lv - 1, p.y); pl.DropToLevel(ny, FallDamage(p.y - ny));
                    return;
                }
            }
            else st.downHeld = 0f;
        }
        pl.transform.position = p;
    }

    void TryClimb(PlayerController pl, St st, Vector3 p, int from, int to)
    {
        int li = NearestLadder(p.x, from, to, 0.6f);
        if (li < 0) return;
        st.ladder = li; st.to = to; st.up = to > from; st.fromY = p.y;
    }

    void StepEnemy(Enemy en, float dt)
    {
        var st = Get(en.transform);
        Vector3 p = en.transform.position;
        bool free = en.IsAwake && en.BrainIdleOrChase && !en.IsAirborneNow && !en.IsHeldNow;
        if (st.ladder >= 0 && (!free && !(en.IsAwake && en.BrainIdleOrChase))) st.ladder = -1;   // lyöty tikkailla: putoaa lähimmälle kerrokselle
        if (st.ladder >= 0)
        {
            var l = ladders[st.ladder];
            float ty = EntryY(st.to, p.y);
            float cx0 = st.up ? l.x : l.TopX, cx1 = st.up ? l.TopX : l.x;
            p.x = Mathf.MoveTowards(p.x, Mathf.Lerp(cx0, cx1, Mathf.InverseLerp(st.fromY, ty, p.y)), 8f * dt);
            p.y = Mathf.MoveTowards(p.y, ty, climbSpeed * 0.85f * dt);
            en.transform.position = p;
            if (Mathf.Abs(p.y - ty) < 0.02f) { p.x = cx1; en.transform.position = p; st.ladder = -1; }
            return;
        }
        int lv = LevelOf(p.y);
        var hero = en.Hero;
        bool inSite = siteMaxX <= siteMinX || (p.x >= siteMinX && p.x <= siteMaxX);
        if (lv > 0 && !free && OverDropEdge(lv, p.x))   // lyöty/heitetty laatan pään yli: putoaa kadulle
        {
            float ny = LandY(0, p.y); en.DropToLevel(ny, FallDamage(p.y - ny));
            return;
        }
        p.x = ClampSlabX(lv, p.x);
        if (free && hero != null && inSite)
        {
            int hl = LevelOf(hero.transform.position.y);
            if (hl != lv)
            {
                int to = hl > lv ? lv + 1 : lv - 1;
                int li = NearestLadder(p.x, lv, to, 999f);
                if (li >= 0)
                {
                    float lx = (to > lv ? ladders[li].x : ladders[li].TopX), sp = Mathf.Max(1.6f, en.moveSpeedX) * 1.1f * dt;
                    p.x += Mathf.Clamp(lx - p.x, -sp, sp);
                    en.FacingRight = lx > p.x;
                    p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
                    en.transform.position = p;
                    if (Mathf.Abs(p.x - lx) < 0.15f) { st.ladder = li; st.to = to; st.up = to > lv; st.fromY = p.y; }
                    return;
                }
            }
        }
        p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
        en.transform.position = p;
    }
}
