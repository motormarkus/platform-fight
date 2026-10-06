using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelastusrenkaan teline. Kun pelaaja ottaa renkaan (kiinniottonappi telineen vieressä), teline piilotetaan ja
/// heron kuvat (rengas_otto) näyttävät telineen ja renkaan; lopuksi teline näkyy tyhjänä.
/// Lyönnit ja lentävät vihut hajottavat telineen (rengas_teline_hajoaa); rengas putoaa silloin lattialle.
/// </summary>
public class RingStand : MonoBehaviour
{
    public static readonly List<RingStand> All = new List<RingStand>();
    public SpriteRenderer body;
    public Sprite withRing, empty;
    public Sprite[] ringSprites;
    [Tooltip("rengas_teline_hajoaa: 0 ehjä (tyhjä), 1–5 hajoaa, viimeinen jää lattialle.")]
    public Sprite[] breakSprites;
    public AudioClip[] breakSounds;
    public float breakFrameTime = 0.08f;
    [Tooltip("Montako iskua teline kestää (kaatava isku hajottaa heti).")]
    public int hits = 2;
    [Tooltip("Telineen keskikohdan etäisyys heron sijainnista otettaessa (yksikköä).")]
    public float heroOffset = 1.715f;
    public bool HasRing { get; private set; } = true;
    public bool Broken { get; private set; }
    bool taking;
    float breakTime = -1f;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }
    void Start() { if (body != null) { body.sprite = withRing; body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f); } }

    void Update()
    {
        if (breakTime < 0f || body == null || breakSprites == null || breakSprites.Length == 0) return;
        breakTime += Time.deltaTime;
        body.sprite = breakSprites[Mathf.Min((int)(breakTime / breakFrameTime), breakSprites.Length - 1)];
        // romu lattialla piirretään ohikulkijoiden taakse
        if (breakTime > breakSprites.Length * breakFrameTime) { body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) - 30; enabled = false; }
    }

    public static RingStand Nearby(Vector3 me)
    {
        RingStand best = null; float bd = 99f;
        foreach (var s in All)
        {
            if (s == null || !s.HasRing || s.Broken) continue;
            Vector3 q = s.transform.position;
            float dx = Mathf.Abs(q.x - me.x), dy = Mathf.Abs(q.y - me.y);
            if (dx > 2.6f || dy > 0.7f) continue;
            if (dx + dy < bd) { bd = dx + dy; best = s; }
        }
        return best;
    }

    /// Otto alkaa: teline piiloon (heron kuvissa), rengas luodaan pelaajan käteen.
    public LifeRing BeginTake(Vector3 heroPos, bool heroFacesRight = true)
    {
        HasRing = false; taking = true;
        // heron kuvissa teline on piirretty heron eteen: vasemmalle katsoessa kuvat peilataan, joten teline peilataan myös
        if (body != null) { body.enabled = false; body.flipX = !heroFacesRight; }
        var r = LifeRing.Create(ringSprites, heroPos);
        r.TakeBy();
        return r;
    }

    public void EndTake()
    {
        taking = false;
        if (body != null && !Broken) { body.sprite = empty; body.enabled = true; }
    }

    public bool CanBeHit => !Broken && !taking && breakSprites != null && breakSprites.Length > 0;

    /// Isku telineeseen: kestää pari iskua, kaatava isku hajottaa heti.
    public bool Hit(bool knockdown)
    {
        if (!CanBeHit) return false;
        hits -= knockdown ? hits : 1;
        if (hits <= 0) Smash();
        else StartCoroutine(Wobble());
        return true;
    }

    System.Collections.IEnumerator Wobble()
    {
        if (body == null) yield break;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 60f) * 4f * (1f - t / 0.25f));
            yield return null;
        }
        body.transform.localRotation = Quaternion.identity;
    }

    public void Smash()
    {
        if (!CanBeHit) return;
        Broken = true;
        StopAllCoroutines();
        if (body != null) { body.transform.localRotation = Quaternion.identity; body.enabled = true; }
        if (HasRing)
        {
            HasRing = false;
            LifeRing.Create(ringSprites, transform.position).DropFrom(transform.position + new Vector3(0f, -0.05f, 0f), 2.4f);
        }
        breakTime = 0f;
        if (breakSounds != null && breakSounds.Length > 0) HitFx.PlayClip(breakSounds[Random.Range(0, breakSounds.Length)], Random.Range(0.85f, 1f));
        if (CameraFollow.Instance != null) CameraFollow.Shake(0.08f, 0.1f);
    }

    /// Lentävä vihu tai rengas osuu: hajoaa, jos on lähellä.
    public static void SmashNear(Vector3 at, float rx = 1.2f, float ry = 0.5f)
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            var s = All[i];
            if (s == null || !s.CanBeHit) continue;
            Vector3 q = s.transform.position;
            if (Mathf.Abs(q.x - at.x) < rx && Mathf.Abs(q.y - at.y) < ry) s.Smash();
        }
    }
}
