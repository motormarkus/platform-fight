using UnityEngine;

/// <summary>
/// Junan vauhti (0–1): kasvaa ja laskee tasaisesti. Juna liikkuu, kun hero on junan sisällä tai katolla
/// (alueet "Junan vaunu", "Junan veturi", "Junan katto"); muualla seisoo.
/// </summary>
public static class TrainMotion
{
    static float cur;
    static int frame = -1;
    /// Nostetaan/lasketaan koodista (esim. juna pysähtyy asemalle); null = päättele alueesta.
    public static float? Override;
    const float Accel = 0.25f;   // kiihtyvyys: 4 s seisovasta täyteen vauhtiin

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { cur = 0f; frame = -1; Override = null; }

    public static bool InTrainArea
    {
        get
        {
            var a = Area.Current;
            return a != null && (a.areaName.StartsWith("Junan vaunu") || a.areaName == "Junan veturi" || a.areaName == "Junan takaparveke" || a.areaName == "Junan katto");
        }
    }

    public static float Current(float dt)
    {
        if (frame != Time.frameCount)
        {
            frame = Time.frameCount;
            float target = Override ?? (InTrainArea ? 1f : 0f);
            cur = Mathf.MoveTowards(cur, target, Accel * dt);
            if (cur > 0f) TrainJourney.Advance(cur * dt);
        }
        return cur;
    }
}
