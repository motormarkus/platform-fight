using UnityEngine;

/// <summary>Aaltotaistelu alueella (esim. kuuhissi): kun alue on käytössä, ensimmäinen aalto aktivoidaan; seuraava tulee, kun edellinen on kaatunut. Aallon viholliset ovat aluksi piilossa (ei-aktiivisia).</summary>
public class EnemyWaves : MonoBehaviour
{
    [System.Serializable] public class Wave { public Enemy[] enemies; public float delay = 1.2f; }
    public Area area;
    public Wave[] waves;
    int next; float timer; bool started;

    void Update()
    {
        if (area == null || waves == null || Area.Current != area) return;
        if (!started) { started = true; next = 0; timer = 0.8f; }
        if (next >= waves.Length) return;
        if (next > 0 && !AllDead(waves[next - 1])) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        foreach (var en in waves[next].enemies)
            if (en != null) { en.gameObject.SetActive(true); en.wakeDistance = 99f; }
        next++;
        if (next < waves.Length) timer = waves[next].delay;
    }

    static bool AllDead(Wave w)
    {
        foreach (var en in w.enemies) if (en != null && en.gameObject.activeSelf && !en.IsDead) return false;
        return true;
    }
}
