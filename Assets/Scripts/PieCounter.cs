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

    void Update()
    {
        if (template == null || area == null || xs == null || xs.Length == 0) return;
        if (Area.Current != area) { seeded = false; return; }
        made.RemoveAll(b => b == null || (born.TryGetValue(b, out float t0) && Time.time - t0 > 0.5f && !b.CanPickUp));   // poimittu, heitetty tai hajonnut: paikka vapautuu
        if (!seeded)
        {
            seeded = true; timer = 0f;
            for (int i = 0; i < initial; i++) SpawnOne();
            return;
        }
        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;
        SpawnOne();
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
