using System.Collections.Generic;
using UnityEngine;

/// Monikerroksinen rakennustyömaa (Vegas): katutaso + kolme kerrosta laattojen päällä, kerrokset yhdistävät tikkaat.
/// Alueen y-raja kattaa kaikki kerrokset (Area.depthLimits); tämä komponentti pitää hahmot omalla kerroksellaan (kapea kaista laatalla)
/// ja hoitaa tikkaat: pelaaja menee tikkaille painamalla ylös (tai alas ylhäällä) tikkaiden kohdalla; viholliset kävelevät lähimmälle tikkaalle
/// ja kiipeävät itse, kun pelaaja on toisessa kerroksessa. Kamera siirtyy kerroksen mukaan (levelCamOffset).
public class SiteLevels : MonoBehaviour
{
    [System.Serializable]
    public struct Ladder { public float x; public int lo, hi; }

    public Area area;
    [Tooltip("Kerrosten lattiakorkeudet alhaalta ylös; 0 = katutaso (kaista streetMin..streetMax), muut = laatan päällä.")]
    public float[] levelY;
    public float streetMin, streetMax;
    public float bandHalf = 0.28f;
    public float[] levelCamOffset;
    public Ladder[] ladders;
    public float climbSpeed = 3.4f;

    class St { public int ladder = -1, to; public bool scripted; }
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

    public static SiteLevels For(Area a) { foreach (var t in FindObjectsByType<SiteLevels>(FindObjectsSortMode.None)) if (t != null && t.area == a) return t; return null; }

    void LateUpdate()
    {
        if (area == null || Area.Current != area || levelY == null || levelY.Length < 2) { ReleaseAll(); return; }
        float dt = Time.deltaTime;
        foreach (var pl in Players.All) if (pl != null) StepHero(pl, dt);
        var prim = Players.Primary;
        if (prim != null && CameraFollow.Instance != null && levelCamOffset != null)
        {
            int lv = LevelOf(prim.transform.position.y);
            if (lv < levelCamOffset.Length) CameraFollow.Instance.OffsetY = levelCamOffset[lv];
        }
        foreach (var en in Enemy.All)
        {
            if (en == null || !en.isActiveAndEnabled || en.IsDead) continue;
            if (en.transform.position.x < area.walkMinX - 6f || en.transform.position.x > area.walkMaxX + 6f) continue;
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
            float d = Mathf.Abs(l.x - x);
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
            p.x = Mathf.MoveTowards(p.x, l.x, 5f * dt);
            p.y = Mathf.MoveTowards(p.y, ty, climbSpeed * dt);
            pl.transform.position = p;
            pl.Scripted = true; st.scripted = true; pl.ScriptedMove = new Vector2(0f, Mathf.Sign(ty - p.y) * 0.5f);
            if (Mathf.Abs(p.y - ty) < 0.02f) { st.ladder = -1; pl.Scripted = false; st.scripted = false; pl.ScriptedMove = Vector2.zero; }
            return;
        }
        int lv = LevelOf(p.y);
        p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
        if (pl.AirHeight < 0.1f && !pl.Scripted)
        {
            float my = CoopInput.Move(pl.playerIndex).y;
            if (my > 0.7f && lv + 1 < levelY.Length) TryClimb(pl, st, p, lv, lv + 1);
            else if (my < -0.7f && lv > 0 && p.y >= levelY[lv] - bandHalf) TryClimb(pl, st, p, lv, lv - 1);
        }
        pl.transform.position = p;
    }

    void TryClimb(PlayerController pl, St st, Vector3 p, int from, int to)
    {
        int li = NearestLadder(p.x, from, to, 0.6f);
        if (li < 0) return;
        st.ladder = li; st.to = to;
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
            p.x = Mathf.MoveTowards(p.x, l.x, 4f * dt);
            p.y = Mathf.MoveTowards(p.y, ty, climbSpeed * 0.85f * dt);
            en.transform.position = p;
            if (Mathf.Abs(p.y - ty) < 0.02f) st.ladder = -1;
            return;
        }
        int lv = LevelOf(p.y);
        var hero = en.Hero;
        if (free && hero != null)
        {
            int hl = LevelOf(hero.transform.position.y);
            if (hl != lv)
            {
                int to = hl > lv ? lv + 1 : lv - 1;
                int li = NearestLadder(p.x, lv, to, 999f);
                if (li >= 0)
                {
                    float lx = ladders[li].x, sp = Mathf.Max(1.6f, en.moveSpeedX) * 1.1f * dt;
                    p.x += Mathf.Clamp(lx - p.x, -sp, sp);
                    en.FacingRight = lx > p.x;
                    p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
                    en.transform.position = p;
                    if (Mathf.Abs(p.x - lx) < 0.15f) { st.ladder = li; st.to = to; }
                    return;
                }
            }
        }
        p.y = Mathf.Clamp(p.y, MinY(lv), MaxY(lv));
        en.transform.position = p;
    }
}
