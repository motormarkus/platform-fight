using UnityEngine;

/// <summary>
/// Vihujen iskujen veto kiihtyy: vetokuvat (ennen osumakuvaa) näytetään alussa hitaasti ja osumaa kohti yhä nopeammin.
/// Vedon kokonaisaika ei muutu, joten osuman ajoitus pysyy ennallaan.
/// </summary>
public static class AttackEase
{
    /// Vedon edistyminen ajan osuudesta u (0–1): hidas alku, nopea loppu.
    public static float Progress(float u) { u = Mathf.Clamp01(u); return 0.35f * u + 0.65f * u * u; }

    /// Vetokuvan indeksi (0..n-1), kun vetoa on kulunut t sekuntia kokonaisajasta total.
    public static int Wind(float t, float total, int n)
    {
        if (n <= 1) return 0;
        return Mathf.Min((int)(Progress(t / Mathf.Max(0.001f, total)) * n), n - 1);
    }

    /// Vetokuvan i (0..n-1) kestokerroin: 1,45 -> 0,55 (keskiarvo 1), eli vedon kokonaisaika pysyy samana.
    public static float Weight(int i, int n) => n <= 1 ? 1f : 1.45f - 0.9f * i / (n - 1);

    /// Tasaisin kuva-ajoin tehty liike, jonka osuma on kuvassa impact: veto (kuvat 0..impact-1) kiihtyy, loput tasaisesti.
    public static int Frame(float t, float frameTime, int impact)
    {
        frameTime = Mathf.Max(0.001f, frameTime);
        float wind = impact * frameTime;
        if (impact > 1 && t < wind) return Wind(t, wind, impact);
        return (int)(t / frameTime);
    }
}
