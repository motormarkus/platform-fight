using UnityEngine;

/// <summary>
/// Viidakon hallin yllätys: kun kaksi rosvoa on kaatunut, kaksi amatsonia ilmestyy hallin vasemmasta päästä ja hakkaa hallin vihollisia (liittolaisina).
/// Kun kaikki muut viholliset on voitettu, amatsonit kääntyvät heroa vastaan. Amatsonit ovat aluksi pois käytöstä (Enemy.ally asetetaan vasta ilmestyessä).
/// </summary>
public class AmazonAmbush : MonoBehaviour
{
    public Enemy[] bandits;      // laukaisu: kaksi näistä kaatuu
    public Enemy[] foes;         // kaikki hallin muut viholliset (rosvot, samoalaiset, kapteeni): kun kaikki kaatuneet, amatsonit kääntyvät
    public Enemy[] amazons;
    public Vector3[] entryPoints;
    public int triggerCount = 2;
    enum Phase { Waiting, Allies, Hostile }
    Phase phase;

    static bool Down(Enemy e) => e == null || e.IsDead || !e.gameObject.activeInHierarchy;

    void Update()
    {
        if (phase == Phase.Waiting)
        {
            int dead = 0; foreach (var b in bandits) if (Down(b)) dead++;
            if (dead < triggerCount) return;
            if (AmazonBrain.HasAlly) { phase = Phase.Hostile; enabled = false; return; }   // heron rinnalla on jo lahjottu amatsoni: hallin amatsonia ei tule
            phase = Phase.Allies;
            for (int i = 0; i < amazons.Length; i++)
            {
                var a = amazons[i]; if (a == null) continue;
                a.transform.position = entryPoints[Mathf.Min(i, entryPoints.Length - 1)];
                a.ally = true; a.fightsEveryone = false; a.huntsBrawlers = false; a.brawlsWithOthers = false;
                a.wakeDistance = 60f; a.IsAwake = true;
                a.gameObject.SetActive(true);
            }
        }
        else if (phase == Phase.Allies)
        {
            foreach (var f in foes) if (!Down(f)) return;
            phase = Phase.Hostile;   // viimeinenkin vihu kaatui: amatsonit käyvät heron kimppuun
            foreach (var a in amazons) if (a != null && !Down(a)) { a.ally = false; a.fightsEveryone = false; a.IsAwake = true; a.wakeDistance = 60f; }
            enabled = false;
        }
    }
}
