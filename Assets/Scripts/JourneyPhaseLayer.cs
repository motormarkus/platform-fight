using UnityEngine;

/// <summary>Junamatkan taustavaihe: häivyttää kerroksen näkyviin matkan edetessä (TrainJourney).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class JourneyPhaseLayer : MonoBehaviour
{
    public int phase;
    [Tooltip("0 = kaukainen tausta, 1 = lähitausta (omat vaiheet, sama matka)")]
    public int group;
    [Tooltip("Junan katto: vaihe seuraa katon taivasnauhan kohtaa (JourneyStrip.RoofProgress), ei koko matkan etenemistä.")]
    public bool useRoofProgress;
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        int g = Mathf.Clamp(group, 0, TrainJourney.Counts.Length - 1);
        TrainJourney.Counts[g] = Mathf.Max(TrainJourney.Counts[g], phase + 1);
    }

    void LateUpdate()
    {
        if (sr == null) return;
        var c = sr.color; c.a = useRoofProgress ? RoofAlpha() : TrainJourney.Alpha(group, phase); sr.color = c;
    }
    /// Katolla ei häivytetä: näkyvissä on vain se vaihe, jonka kohdalla taivasnauha on (kovat vaihdot, ei päällekkäisiä kuvia).
    float RoofAlpha()
    {
        int count = Mathf.Max(1, TrainJourney.Counts[Mathf.Clamp(group, 0, TrainJourney.Counts.Length - 1)]);
        int idx = Mathf.Clamp(Mathf.FloorToInt(JourneyStrip.RoofProgress * count), 0, count - 1);
        return phase == idx ? 1f : 0f;
    }
}
