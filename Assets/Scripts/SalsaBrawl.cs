using UnityEngine;

/// <summary>
/// Salsafestin rauhalliset vihut (Tom, Puliukko, Samoalainen ym.): tappelu alkaa vain, kun hero lyö jotakuta heistä tai kun alueella on oltu
/// calmSeconds (2 min) ja Puliukko vittuilee Tomille (sanoo repliikkejään). Sen jälkeen kaikki ovat kaikkia vastaan.
/// </summary>
public class SalsaBrawl : MonoBehaviour
{
    public Area area;
    public Enemy[] crowd;
    [Tooltip("Vittuilija (Puliukko): sanoo repliikkejään, kun rauha on kestänyt calmSeconds.")]
    public Enemy instigator;
    public float calmSeconds = 120f;

    float calm, tauntAt = -1f;
    bool started, secondTaunt;

    void Update()
    {
        if (started) return;
        // hero (tai kukaan) lyö jotakuta joukosta: tappelu alkaa heti
        if (Enemy.LastHit != null && Time.time - Enemy.LastHitTime < 1f)
            foreach (var e in crowd) if (e != null && e == Enemy.LastHit) { Begin(); return; }
        if (area == null || Area.Current != area || PokerService.BlocksCombat) return;
        calm += Time.deltaTime;
        if (calm >= calmSeconds && tauntAt < 0f) { tauntAt = Time.time; if (instigator != null) instigator.Speak(); }
        if (tauntAt >= 0f && !secondTaunt && Time.time - tauntAt > 3.5f) { secondTaunt = true; if (instigator != null) instigator.Speak(); }
        if (tauntAt >= 0f && Time.time - tauntAt > 6f) Begin();
    }

    void Begin()
    {
        started = true;
        foreach (var e in crowd)
        {
            if (e == null || e.IsDead) continue;
            e.fightsEveryone = true;
            e.WakeUp();
        }
    }
}
