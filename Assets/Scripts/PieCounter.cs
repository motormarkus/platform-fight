using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dinerin tiskille ilmestyy kermapiirakoita poimittavaksi. Kun pelaaja on alueella, tyhjille paikoille ilmestyy
/// uusi piirakka tietyin väliajoin; poimittu, heitetty tai hajonnut piirakka vapauttaa paikan.
/// Malli (template) on piilotettu piirakka (Bottle), jonka kopioita tiskille tehdään.
/// </summary>
public class PieCounter : MonoBehaviour
{
    public Area area;
    [Tooltip("Piilotettu malli: Bottle (piirakka), restHeight = tiskin pinnan korkeus.")]
    public Bottle template;
    [Tooltip("Piirakoiden paikat tiskillä: x maailmassa.")]
    public float[] xs;
    [Tooltip("Syvyys (y), jossa piirakat ovat (tiskin edessä).")]
    public float y;
    [Tooltip("Uusi piirakka ilmestyy näin usein (sekuntia), ja alueelle tullessa on heti tämän verran.")]
    public float interval = 6f;
    public int initial = 4;

    readonly List<Bottle> made = new List<Bottle>();
    readonly Dictionary<Bottle, float> born = new Dictionary<Bottle, float>();
    float timer;
    bool seeded;

    /// Piirakkasota alkoi: uusi täytö heti.
    public void Reseed() { seeded = false; }

    void Update()
    {
        if (template == null || area == null || xs == null || xs.Length == 0) return;
        if (Area.Current != area) { seeded = false; return; }
        if (!PieWar.Active)
        {
            // ei piirakkasotaa: tiskillä ja lattialla ei ole piirakoita (ilmassa olevat ja kädessä pidetyt jätetään)
            ClearResting(made); ClearResting(floorMade);
            seeded = false; timer = 0f; floorTimer = 0f;
            return;
        }
        made.RemoveAll(b => b == null || (born.TryGetValue(b, out float t0) && Time.time - t0 > 0.5f && !b.CanPickUp));   // poimittu, heitetty tai hajonnut: paikka vapautuu
        if (!seeded)
        {
            seeded = true; timer = 0f;
            int n = PieWar.Active ? xs.Length : initial;
            for (int i = 0; i < n; i++) SpawnOne();
            return;
        }
        if (PieWar.Active)
        {
            floorTimer += Time.deltaTime;
            if (floorTimer >= 1f) { floorTimer = 0f; SpawnFloor(); }   // sodassa lattialle uusi piirakka heron lähelle joka sekunti
        }
        timer += Time.deltaTime;
        if (timer < (PieWar.Active ? Mathf.Min(interval, 1.5f) : interval)) return;   // piirakkasodassa nopeammin
        timer = 0f;
        SpawnOne();
    }

    static void ClearResting(List<Bottle> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var b = list[i];
            if (b == null) { list.RemoveAt(i); continue; }
            if (!b.CanPickUp) continue;
            Destroy(b.gameObject);
            list.RemoveAt(i);
        }
    }

    readonly List<Bottle> floorMade = new List<Bottle>();
    float floorTimer;
    PlayerController hero;

    bool SpawnFloor()
    {
        floorMade.RemoveAll(b => b == null || (born.TryGetValue(b, out float t0) && Time.time - t0 > 0.5f && !b.CanPickUp));
        if (floorMade.Count >= 10) return false;
        if (hero == null) hero = FindFirstObjectByType<PlayerController>();
        float cx = hero != null ? hero.transform.position.x : (area.walkMinX + area.walkMaxX) * 0.5f;
        float cy = hero != null ? hero.transform.position.y : area.minDepthY + 1.5f;
        float px = Mathf.Clamp(cx + Random.Range(-5f, 5f), area.walkMinX + 1.5f, area.walkMaxX - 1.5f);
        float top = Mathf.Min(area.MaxDepthAtX(px, area.maxDepthY) - 1.0f, area.maxDepthY - 1.0f);
        float py = Mathf.Clamp(cy + Random.Range(-0.8f, 0.8f), area.minDepthY + 0.5f, Mathf.Max(area.minDepthY + 0.6f, top));
        var go = Instantiate(template.gameObject, new Vector3(px, py, 0f), Quaternion.identity, transform);
        go.name = "Lattian piirakka";
        go.SetActive(true);
        var nb = go.GetComponent<Bottle>();
        nb.restHeight = 0f; nb.restOnRenderer = null;   // lattialla, ei tiskin pinnalla
        floorMade.Add(nb); born[nb] = Time.time;
        return true;
    }

    void SpawnOne()
    {
        var free = new List<float>();
        foreach (float x in xs)
        {
            bool taken = false;
            foreach (var b in made) if (b != null && Mathf.Abs(b.transform.position.x - x) < 0.6f) { taken = true; break; }
            if (!taken) free.Add(x);
        }
        if (free.Count == 0) return;
        float px = free[Random.Range(0, free.Count)];
        var go = Instantiate(template.gameObject, new Vector3(px, y, 0f), Quaternion.identity, transform);
        go.name = "Tiskin piirakka";
        go.SetActive(true);
        var nb = go.GetComponent<Bottle>();
        made.Add(nb); born[nb] = Time.time;
    }
}
