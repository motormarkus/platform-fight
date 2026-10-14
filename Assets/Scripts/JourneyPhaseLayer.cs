using UnityEngine;

/// <summary>Junamatkan taustavaihe: häivyttää kerroksen näkyviin matkan edetessä (TrainJourney).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class JourneyPhaseLayer : MonoBehaviour
{
    public int phase;
    [Tooltip("0 = kaukainen tausta, 1 = lähitausta (omat vaiheet, sama matka)")]
    public int group;
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
        var c = sr.color; c.a = TrainJourney.Alpha(group, phase); sr.color = c;
    }
}
