using UnityEngine;

/// <summary>Luuranko nousee maasta: ennen laukaisua se on piilossa ja Enemy-komponentti pois päältä (ei osu, ei näy vihollislistoilla). Kun pelaaja tulee triggerDistance:n sisään,
/// nousukuvat (luuranko_maasta, 44 kuvaa) toistetaan kuvasta toiseen: käsi ojentuu maasta, hahmo kiipeää ylös ja asettuu taisteluasentoon. Sen jälkeen Enemy herää tavallisena vihollisena.</summary>
[RequireComponent(typeof(Enemy))]
public class SkeletonRiser : MonoBehaviour
{
    public Sprite[] riseSprites;
    public float frameTime = 0.05f, triggerDistance = 8f;
    [Tooltip("Varjo ilmestyy tästä kuvasta alkaen.")]
    public int shadowFrame = 24;

    Enemy e; SpriteRenderer body, shadow;
    bool rising, faceRight = true, spoke, crunch2; float t;
    AudioClip crunchClip, speakClip;

    /// Maan rouske: Resources/Sfx/Luuranko/rouske (jos annettu), muuten koodilla tehty paikkaäänite (kosteaa maata rouskuvia kohinapurskeita).
    static AudioClip synthCrunch;
    static AudioClip Crunch()
    {
        var c = Resources.Load<AudioClip>("Sfx/Luuranko/rouske");
        if (c != null) return c;
        if (synthCrunch != null) return synthCrunch;
        const int sr = 22050; int n = (int)(sr * 0.7f);
        var d = new float[n]; var rng = new System.Random(7);
        float[] starts = { 0f, 0.07f, 0.16f, 0.27f, 0.4f, 0.5f };
        foreach (float st in starts)
        {
            int s0 = (int)(st * sr), len = (int)(sr * 0.07f); float lp = 0f;
            for (int i = 0; i < len && s0 + i < n; i++)
            {
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * 0.28f;   // pehmeä, matala rouske
                d[s0 + i] += lp * Mathf.Exp(-i / (sr * 0.018f)) * 0.9f;
            }
        }
        synthCrunch = AudioClip.Create("rouske", n, 1, sr, false); synthCrunch.SetData(d, 0);
        return synthCrunch;
    }
    void Play(AudioClip c) { if (c != null && e != null && e.Audio != null) e.Audio.PlayOneShotLeveled(c, 0.75f, true); }

    void Awake()
    {
        e = GetComponent<Enemy>(); body = e.body; shadow = e.shadow;
        crunchClip = Crunch(); speakClip = Resources.Load<AudioClip>("Sfx/Luuranko/speak1");
        e.enabled = false;
        if (body != null) body.enabled = false;
        if (shadow != null) shadow.enabled = false;
    }

    void Update()
    {
        if (e == null || body == null || riseSprites == null || riseSprites.Length == 0) { Finish(); return; }
        if (!rising)
        {
            PlayerController near = null; float best = float.MaxValue;
            foreach (var p in Players.All)
            {
                if (p == null || !p.enabled || p.GameOver) continue;
                float d = Mathf.Abs(p.transform.position.x - transform.position.x);
                if (Mathf.Abs(p.transform.position.y - transform.position.y) > 4f) d += 4f;
                if (d < best) { best = d; near = p; }
            }
            if (near == null || best > triggerDistance) return;
            rising = true; t = 0f; faceRight = near.transform.position.x > transform.position.x;
            body.enabled = true; body.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
        t += Time.deltaTime;
        int i = Mathf.Min((int)(t / frameTime), riseSprites.Length - 1);
        body.sprite = riseSprites[i]; body.flipX = !faceRight;
        if (t - Time.deltaTime <= 0f) Play(crunchClip);                                          // käsi puhkeaa maasta
        if (!crunch2 && i >= shadowFrame - 2) { crunch2 = true; Play(crunchClip); }               // ylävartalo nousee
        if (!spoke && i >= riseSprites.Length * 2 / 3) { spoke = true; Play(speakClip); }          // seisomaan: speak1
        if (shadow != null && i >= shadowFrame) shadow.enabled = true;
        if (t >= riseSprites.Length * frameTime) Finish();
    }

    void Finish()
    {
        if (e != null) { e.FacingRight = faceRight; e.enabled = true; }
        if (body != null) body.enabled = true;
        if (shadow != null) shadow.enabled = true;
        Destroy(this);
    }
}
