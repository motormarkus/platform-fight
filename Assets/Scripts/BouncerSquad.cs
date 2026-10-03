using UnityEngine;

/// <summary>
/// S-Clubin portsarit: odottavat piilossa, ja kun klubissa alkaa ensimmäinen tappelu (vihu tai pelaaja saa osuman),
/// ne tulevat ovesta yksi kerrallaan ja käyvät lähimmän kimppuun, myös punkkareiden.
/// </summary>
public class BouncerSquad : MonoBehaviour
{
    public Area area;
    [Tooltip("Portsarit (piilossa, kunnes tappelu alkaa).")]
    public Enemy[] bouncers;
    [Tooltip("Aika portsarien välillä ovesta (s).")]
    public float spawnInterval = 0.8f;
    [Tooltip("Viive ensimmäisestä iskusta ensimmäiseen portsariin (s).")]
    public float firstDelay = 1.2f;

    PlayerController pc;
    bool triggered;
    int heroHealthAtEnter = -1, next;
    float timer;

    void Start()
    {
        foreach (var b in bouncers) if (b != null) b.gameObject.SetActive(false);
    }

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (!triggered)
        {
            if (area == null || Area.Current != area) { heroHealthAtEnter = -1; return; }
            if (pc == null) return;
            if (heroHealthAtEnter < 0) heroHealthAtEnter = pc.health;
            bool fight = pc.health < heroHealthAtEnter;
            if (!fight)
                foreach (var e in Enemy.All)
                {
                    if (e == null || e.fightsEveryone) continue;
                    float x = e.transform.position.x;
                    if (x < area.camMinX - 5f || x > area.camMaxX + 5f) continue;
                    if (e.Health < e.maxHealth) { fight = true; break; }
                }
            if (!fight) return;
            triggered = true;
            timer = firstDelay;
        }
        if (next >= bouncers.Length) { enabled = false; return; }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        var bnc = bouncers[next++];
        timer = spawnInterval;
        if (bnc == null) return;
        bnc.gameObject.SetActive(true);
        bnc.WakeUp();
    }
}
