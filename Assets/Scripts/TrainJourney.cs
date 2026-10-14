using UnityEngine;

/// <summary>
/// Junamatkan eteneminen (0–1): kasvaa, kun juna on liikkeessä. Taustavaiheet (aavikko → yöllinen kaupunki)
/// häivytetään sen mukaan: vaihe i tulee näkyviin, kun eteneminen ohittaa kohdan i-1.
/// </summary>
public static class TrainJourney
{
    public static float Progress;
    public static int[] Counts = { 1, 1 };       // vaiheiden määrä ryhmittäin: 0 = kaukainen tausta, 1 = lähitausta (JourneyPhaseLayer päivittää)
    public static float Duration = 240f;         // sekuntia liikkeessä koko matkaan

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { Progress = 0f; Counts = new[] { 1, 1 }; }

    public static void Advance(float movedSeconds)
    {
        Progress = Mathf.Clamp01(Progress + movedSeconds / Mathf.Max(1f, Duration));
    }

    /// Vaiheen läpinäkyvyys: ensimmäinen aina täysi, seuraavat häivytetään päälle edellisen yli.
    public static float Alpha(int group, int phase)
    {
        int count = Counts[Mathf.Clamp(group, 0, Counts.Length - 1)];
        if (phase <= 0 || count <= 1) return 1f;
        float p = Progress * (count - 1);
        return Mathf.Clamp01(p - (phase - 1));
    }
}
