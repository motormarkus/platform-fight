using UnityEngine;

/// <summary>
/// Dinerin pöydät uusiutuvat: kun pöytä on rikki (tai heitetty kauas paikaltaan), samaan paikkaan ilmestyy hetken päästä uusi pöytä
/// annoksineen, piirakkoineen, olutpulloineen ja tuoleineen. Malli (template) on piilotettu ryhmä, jonka kopioita tehdään.
/// </summary>
public class DinerTableRespawn : MonoBehaviour
{
    public Area area;
    [Tooltip("Piilotettu mallipöytä (pöytä, annokset, piirakka, pullot, tuolit yhden ryhmän alla).")]
    public GameObject template;
    [Tooltip("Pöytien paikat (pöydän jalat maassa).")]
    public Vector3[] spots;
    [Tooltip("Uusi pöytä ilmestyy näin monen sekunnin kuluttua.")]
    public float delay = 6f;
    [Tooltip("Pöytä, joka on heitetty tätä kauemmas paikaltaan, korvataan uudella.")]
    public float movedDistance = 3f;
    public int maxTables = 10;
    [Tooltip("Valinnainen oma malli paikkakohtaisesti (esim. pöytä ilman piirakkaa). Tyhjä = yhteinen malli.")]
    public GameObject[] spotTemplates;
    public Vector3 templateOrigin;   // pöydän paikka mallissa

    Crate[] tables;
    float[] lostAt;
    bool initialized;

    void Init()
    {
        initialized = true;
        tables = new Crate[spots.Length];
        lostAt = new float[spots.Length];
        for (int i = 0; i < spots.Length; i++)
        {
            lostAt[i] = -1f;
            foreach (var c in Crate.All)
                if (c != null && c.gameObject.name == "Pöytä" && Vector2.Distance(c.transform.position, spots[i]) < 0.8f) { tables[i] = c; break; }
        }
    }

    void Update()
    {
        if (template == null || spots == null || spots.Length == 0 || (area != null && Area.Current != area)) return;
        if (!initialized) Init();
        int alive = 0;
        foreach (var c in Crate.All) if (c != null && c.gameObject.name == "Pöytä") alive++;
        for (int i = 0; i < spots.Length; i++)
        {
            var c = tables[i];
            bool lost = c == null || c.IsBroken || (!c.Airborne && Vector2.Distance(c.transform.position, spots[i]) > movedDistance);
            if (!lost) { lostAt[i] = -1f; continue; }
            if (lostAt[i] < 0f) { lostAt[i] = Time.time; continue; }
            if (Time.time - lostAt[i] < delay || alive >= maxTables) continue;
            Respawn(i);
            alive++;
        }
    }

    /// Piirakkasodan alkaessa: kaikki rikki menneet tai pois heitetyt pöydät ilmestyvät heti ehjinä paikoilleen.
    public void RespawnMissingNow()
    {
        if (template == null || spots == null) return;
        if (!initialized) Init();
        for (int i = 0; i < spots.Length; i++)
        {
            var c = tables[i];
            bool lost = c == null || c.IsBroken || (!c.Airborne && Vector2.Distance(c.transform.position, spots[i]) > movedDistance);
            if (!lost) continue;
            if (c != null && c.IsBroken) Destroy(c.gameObject);   // rikkinäinen jäänne pois
            Respawn(i);
        }
    }

    void Respawn(int i)
    {
        var tpl = spotTemplates != null && i < spotTemplates.Length && spotTemplates[i] != null ? spotTemplates[i] : template;
        var g = Instantiate(tpl, spots[i] - templateOrigin, Quaternion.identity, transform);
        g.name = "Uusi pöytäryhmä";
        g.SetActive(true);
        tables[i] = null;
        foreach (var c in g.GetComponentsInChildren<Crate>(true))
            if (c.gameObject.name == "Pöytä") { tables[i] = c; break; }
        lostAt[i] = -1f;
    }
}
