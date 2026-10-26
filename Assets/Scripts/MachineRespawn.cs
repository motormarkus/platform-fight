using UnityEngine;

/// <summary>
/// Pelikoneiden uudelleensyntyminen: kun tappelussa rikkoutunut kone on tuhoutunut ja alueen tappelu on ohi (kaikki hereillä olevat pieksetty),
/// kone ilmestyy takaisin alkuperäiselle paikalleen (kopio piilotetusta mallista). Terminaalissa vain pelattavat koneet.
/// </summary>
public class MachineRespawn : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject template;   // piilotettu (ei-aktiivinen) kopio
        public Vector3 position;
        [System.NonSerialized] public GameObject live;
        [System.NonSerialized] public float goneAt = -1f;
    }

    public Area area;
    public Entry[] entries;
    public float delay = 2.5f;

    void Start()
    {
        // ensimmäiset elävät koneet ovat juuren lapsia nimellä, joka vastaa mallin nimeä (ilman "(malli)")
        foreach (var e in entries)
        {
            if (e == null || e.template == null) continue;
            string n = e.template.name.Replace(" (malli)", "");
            foreach (Transform t in transform)
                if (t.gameObject != e.template && t.name == n && (t.position - e.position).sqrMagnitude < 0.01f) { e.live = t.gameObject; break; }
            if (e.live == null) e.goneAt = Time.time;
        }
    }

    bool FightOn()
    {
        foreach (var b in Object.FindObjectsByType<SalsaBrawl>(FindObjectsSortMode.None))
            if (b != null && b.area == area && b.Ongoing) return true;
        return false;
    }

    void Update()
    {
        if (entries == null) return;
        bool fight = FightOn();
        foreach (var e in entries)
        {
            if (e == null || e.template == null) continue;
            if (e.live != null) { e.goneAt = -1f; continue; }
            if (e.goneAt < 0f) e.goneAt = Time.time;
            if (fight || Time.time - e.goneAt < delay) continue;
            var go = Instantiate(e.template, transform);
            go.name = e.template.name.Replace(" (malli)", "");
            go.transform.position = e.position;
            go.SetActive(true);
            e.live = go;
            e.goneAt = -1f;
        }
    }
}
