using System.Collections.Generic;
using UnityEngine;

/// Kaksikerroksinen sellikäytävä (vankilan D- ja E-osasto): alakerran lattia, yläkerran kulkusilta ja vinot portaat välissä.
/// Alueen y-raja kattaa molemmat kerrokset (Area.depthLimits), tämä komponentti pitää hahmot omalla kerroksellaan ja
/// nostaa heidät portaissa pitkin portaiden viivaa (y seuraa x:ää). Pelaaja menee portaisiin kävelemällä alapäästä ylös tai yläpäästä alas.
/// Viholliset (vartijat) ja liittolainen (Rex) käyttävät portaita itse: kun hero on toisessa kerroksessa, he kävelevät lähimmälle portaalle,
/// nousevat/laskevat sen ja jatkavat jahtia.
public class TwoFloorCorridor : MonoBehaviour
{
    [System.Serializable]
    public struct Stair { public float xBottom, xTop, yBottom, yTop; }

    public Area area;
    public float lowerMin, lowerMax, upperMin, upperMax;
    public Stair[] stairs;
    [Tooltip("Kameran pystysiirros ala- ja yläkerrassa (Area.camOffsetY:n lisäksi ei; nämä korvaavat sen): molemmat 0 = ei käytössä.")]
    public float lowerOffsetY, upperOffsetY;

    class St { public int stair = -1; public float[] prev; public float lastX = float.NaN; public bool tumble; }
    readonly Dictionary<Transform, St> state = new Dictionary<Transform, St>();

    /// Onko y yläkerrassa (kulkusillalla).
    public bool IsUpper(float y) => y > 0.5f * (lowerMax + upperMin);
    /// Alueen kaksikerroksinen käytävä, jos alueella on sellainen.
    public static TwoFloorCorridor For(Area a) { foreach (var t in FindObjectsByType<TwoFloorCorridor>(FindObjectsSortMode.None)) if (t != null && t.area == a) return t; return null; }

    static float T(Stair s, float x) => (x - s.xBottom) / (s.xTop - s.xBottom);

    void LateUpdate()
    {
        if (area == null || Area.Current != area || stairs == null) { state.Clear(); return; }
        float split = 0.5f * (lowerMax + upperMin);
        foreach (var pl in Players.All)
            if (pl != null) Step(pl.transform, null, split);
        var prim = Players.Primary;
        if (prim != null && CameraFollow.Instance != null && (lowerOffsetY != 0f || upperOffsetY != 0f)) CameraFollow.Instance.OffsetY = IsUpper(prim.transform.position.y) ? upperOffsetY : lowerOffsetY;   // kamera ylös yläkerrassa, alas alakerrassa
        // viholliset ja liittolaiset: samat portaat, mutta kulkevat itse, kun hero on toisessa kerroksessa
        float cx = 0.5f * (area.walkMinX + area.walkMaxX), half = 0.5f * (area.walkMaxX - area.walkMinX) + 4f;
        foreach (var en in Enemy.All)
        {
            if (en == null || !en.isActiveAndEnabled || en.IsDead) continue;
            if (Mathf.Abs(en.transform.position.x - cx) > half) continue;   // muulla alueella
            Step(en.transform, en, split);
        }
    }

    void Step(Transform pc, Enemy en, float split)
    {
        if (!state.TryGetValue(pc, out var st)) { st = new St(); state[pc] = st; }
        Vector3 p = pc.position;
        if (st.prev == null || st.prev.Length != stairs.Length)
        {
            st.prev = new float[stairs.Length];
            for (int i = 0; i < stairs.Length; i++) st.prev[i] = float.NaN;
        }
        // vihollisen oma kulku portaalle: hero on toisessa kerroksessa -> kävellään portaalle (vihollisen oma liike tälle ruudulle korvataan)
        if (en != null)
        {
            var hero = en.Hero;
            bool drives = en.IsAwake && en.BrainIdleOrChase && !en.IsAirborneNow && !en.IsHeldNow;
            if (drives && hero != null && !float.IsNaN(st.lastX))
            {
                float hy = hero.transform.position.y;
                bool heroStable = hy <= lowerMax + 0.05f || hy >= upperMin - 0.05f;
                bool heroUp = IsUpper(hy);
                int goStair = -1; bool goUp = heroUp;
                if (st.stair >= 0)
                {
                    goStair = st.stair; goUp = heroStable ? heroUp : (T(stairs[st.stair], p.x) > 0.5f);   // portaissa: kohti hero-kerrosta
                }
                else if (heroStable && IsUpper(p.y) != heroUp && (p.y <= lowerMax + 0.05f || p.y >= upperMin - 0.05f))
                {
                    float best = float.MaxValue;   // lähin portaikko: oman kerroksen pää (ja heron läheisyys toisessa päässä)
                    for (int i = 0; i < stairs.Length; i++)
                    {
                        float ex = heroUp ? stairs[i].xBottom : stairs[i].xTop;
                        float d = Mathf.Abs(p.x - ex) + Mathf.Abs(hero.transform.position.x - (heroUp ? stairs[i].xTop : stairs[i].xBottom)) * 0.3f;
                        if (d < best) { best = d; goStair = i; }
                    }
                }
                if (goStair >= 0)
                {
                    var s1 = stairs[goStair];
                    float speed = Mathf.Max(1.5f, en.moveSpeedX) * 1.15f * Time.deltaTime;
                    float tx = st.stair >= 0 ? (goUp ? s1.xTop : s1.xBottom) : (goUp ? s1.xBottom : s1.xTop);   // portaassa: toiseen päähän; kerroksella: portaan omaan päähän
                    p.x = st.lastX + Mathf.Clamp(tx - st.lastX, -speed, speed);
                    en.FacingRight = tx > p.x;
                    if (st.stair < 0 && Mathf.Abs(p.x - tx) < 0.12f)
                    {
                        st.stair = goStair;   // perillä portaan juurella: nousu/lasku alkaa
                        p.x = tx + 0.05f * Mathf.Sign(s1.xTop - s1.xBottom) * (goUp ? 1f : -1f);
                    }
                }
            }
        }

        // portailla kaatunut/lentävä vihu pyörii portaat alas (ilmalentokuvat) eikä leiju paikallaan eikä kävele takaisin ylös
        if (en != null && st.stair >= 0 && !en.IsDead)
        {
            if (!st.tumble && (en.IsAirborneNow || en.IsDownNow) && !en.IsHeldNow && !en.ThrownByPlayerNow) { st.tumble = true; en.BeginStairTumble(); }
            if (st.tumble)
            {
                var ts = stairs[st.stair];
                float dirDown = Mathf.Sign(ts.xBottom - ts.xTop);
                float baseX = float.IsNaN(st.lastX) ? p.x : st.lastX;
                p.x = baseX + dirDown * 5.4f * Time.deltaTime;
                en.FacingRight = dirDown > 0f;
            }
        }
        if (st.stair >= 0)
        {
            var s = stairs[st.stair];
            float t = T(s, p.x);
            if (t >= 1f) { st.stair = -1; p.y = Mathf.Clamp(s.yTop, upperMin, upperMax); if (st.tumble) { st.tumble = false; if (en != null) en.EndStairTumble(0f); } }
            else if (t <= 0f) { st.stair = -1; p.y = Mathf.Clamp(s.yBottom, lowerMin, lowerMax); if (st.tumble) { st.tumble = false; if (en != null) en.EndStairTumble(Mathf.Sign(s.xBottom - s.xTop)); } }
            else p.y = Mathf.Lerp(s.yBottom, s.yTop, t);
            for (int i = 0; i < stairs.Length; i++) st.prev[i] = T(stairs[i], p.x);
            pc.position = p; st.lastX = p.x;
            return;
        }
        bool upper = p.y > split;
        p.y = upper ? Mathf.Clamp(p.y, upperMin, upperMax) : Mathf.Clamp(p.y, lowerMin, lowerMax);
        if (en == null)   // vain pelaaja menee portaisiin kävelemällä; viholliset ja liittolaiset edellä olevalla kulullaan
            for (int i = 0; i < stairs.Length; i++)
            {
                var s = stairs[i];
                float t = T(s, p.x), pt = st.prev[i];
                if (float.IsNaN(pt)) continue;
                if (!upper && pt < 0.04f && t >= 0.04f && t < 0.6f && p.y >= s.yBottom - 0.5f)
                { st.stair = i; p.y = Mathf.Lerp(s.yBottom, s.yTop, t); break; }   // alapäästä ylös
                if (upper && pt >= 1f && t < 1f && t > 0.5f)
                { st.stair = i; p.y = Mathf.Lerp(s.yBottom, s.yTop, t); break; }   // yläpäästä alas
            }
        else if (!en.IsDead && upper && (en.IsAirborneNow || en.IsDownNow) && !en.IsHeldNow && !en.ThrownByPlayerNow)
            for (int i = 0; i < stairs.Length; i++)
            {
                // yläkerrassa lyöty/kaatunut vihu lentää portaiden yläpään yli -> putoaa portaisiin ja pyörii alas (tumble-koodi ylempänä)
                var s = stairs[i];
                float t = T(s, p.x), pt = st.prev[i];
                if (float.IsNaN(pt)) continue;
                if (pt >= 1f && t < 1f && t > 0.5f) { st.stair = i; p.y = Mathf.Lerp(s.yBottom, s.yTop, t); break; }
            }
        for (int i = 0; i < stairs.Length; i++) st.prev[i] = T(stairs[i], p.x);
        pc.position = p; st.lastX = p.x;
    }
}
