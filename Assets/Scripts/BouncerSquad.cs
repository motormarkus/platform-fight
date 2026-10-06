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
    [Tooltip("Joka toinen portsari tulee klubin oikeasta reunasta (muut ovelta): molemmista suunnista.")]
    public bool bothSides = true;
    [Tooltip("Tappelu alkaa myös, kun pelaaja kävelee tämän x:n ohi (esim. laivan hyttiovi). Ei käytössä, jos < -9000.")]
    public float triggerX = -99999f;
    [Tooltip("Aallot: montako tulee kerralla (esim. 3, 3, 2). Seuraava aalto, kun edellisistä on pystyssä enää waveNextAt. Tyhjä = kaikki peräkkäin.")]
    public int[] waves;
    public int waveNextAt = 1;
    [Tooltip("Tulevat ruudun vasemmasta reunasta (esim. laivan keulan puolelta), eivät omista paikoistaan.")]
    public bool fromLeftEdge;
    [Tooltip("Tulevat taustakuvan oikeasta reunasta (esim. salin perältä).")]
    public bool fromRightEdge;
    int waveIndex, waveLeft = -1;
    [Tooltip("Ovi, josta tullaan: avautuu ennen ensimmäistä ja sulkeutuu, kun aalto on tullut ulos.")]
    public AnimatedDoor door;
    public float doorCloseDelay = 1.6f;
    float lastSpawnTime = -99f;

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
            bool fight = pc.health < heroHealthAtEnter || (triggerX > -9000f && pc.transform.position.x >= triggerX);
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
            if (door != null) door.Open();
            // alueella tepastelevat (esim. puliukko kannella) liittyvät tappeluun
            foreach (var e in Enemy.All)
                if (e != null && e.joinsFightWhenSquadComes && e.transform.position.x >= area.camMinX - 15f && e.transform.position.x <= area.camMaxX + 15f) e.WakeUp();
        }
        if (door != null && door.IsOpen && Time.time > lastSpawnTime + doorCloseDelay && (next >= bouncers.Length || waveLeft == 0)) door.Close();
        if (next >= bouncers.Length) { if (door == null || !door.IsOpen) enabled = false; return; }
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        if (waves != null && waves.Length > 0)
        {
            if (waveLeft < 0) waveLeft = waves[Mathf.Min(waveIndex, waves.Length - 1)];
            if (waveLeft == 0)
            {
                // seuraava aalto vasta, kun edellisistä on pystyssä enää muutama
                int up = 0;
                for (int i = 0; i < next; i++) if (bouncers[i] != null && !bouncers[i].IsDead) up++;
                if (up > waveNextAt) return;
                waveIndex++;
                waveLeft = waves[Mathf.Min(waveIndex, waves.Length - 1)];
                timer = firstDelay;
                if (door != null) door.Open();
                return;
            }
            waveLeft--;
        }
        var bnc = bouncers[next++];
        timer = spawnInterval;
        if (bnc == null) return;
        if (fromRightEdge || (bothSides && next % 2 == 0))
        {
            // oikea reuna: kävelee sisään ruudun ulkopuolelta (taustakuvan oikeasta päästä)
            var cam = Camera.main;
            float halfW = cam != null ? cam.orthographicSize * cam.aspect : 9f;
            Vector3 q = bnc.transform.position;
            bnc.transform.position = new Vector3(area.camMaxX + halfW - 0.8f, q.y, 0f);
        }
        else if (fromLeftEdge)
        {
            // ruudun vasemman reunan takaa (keulan puolelta)
            var cam = Camera.main;
            float halfW = cam != null ? cam.orthographicSize * cam.aspect : 9f;
            float cx = cam != null ? cam.transform.position.x : area.camMinX;
            Vector3 q = bnc.transform.position;
            bnc.transform.position = new Vector3(cx - halfW - 1f, q.y, 0f);
        }
        bnc.gameObject.SetActive(true);
        lastSpawnTime = Time.time;
        bnc.WakeUp();
    }
}
