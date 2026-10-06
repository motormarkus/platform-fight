using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelastusrenkaan teline. Kun pelaaja ottaa renkaan (kiinniottonappi telineen vieressä), teline piilotetaan ja
/// heron kuvat (rengas_otto) näyttävät telineen ja renkaan; lopuksi teline näkyy tyhjänä.
/// </summary>
public class RingStand : MonoBehaviour
{
    public static readonly List<RingStand> All = new List<RingStand>();
    public SpriteRenderer body;
    public Sprite withRing, empty;
    public Sprite[] ringSprites;
    [Tooltip("Telineen keskikohdan etäisyys heron sijainnista otettaessa (yksikköä).")]
    public float heroOffset = 1.715f;
    public bool HasRing { get; private set; } = true;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void Start() { if (body != null) { body.sprite = withRing; body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f); } }

    public static RingStand Nearby(Vector3 me)
    {
        RingStand best = null; float bd = 99f;
        foreach (var s in All)
        {
            if (s == null || !s.HasRing) continue;
            Vector3 q = s.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 2.6f || dy > 0.7f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = s; }
        }
        return best;
    }

    /// Otto alkaa: teline piiloon (heron kuvissa), rengas luodaan pelaajan käteen.
    public LifeRing BeginTake(Vector3 heroPos)
    {
        HasRing = false;
        if (body != null) body.enabled = false;
        var r = LifeRing.Create(ringSprites, heroPos);
        r.TakeBy();
        return r;
    }

    public void EndTake()
    {
        if (body != null) { body.sprite = empty; body.enabled = true; }
    }
}
