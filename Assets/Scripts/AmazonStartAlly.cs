using System.Collections;
using UnityEngine;

/// <summary>Testialoitus: kun peli alkaa, heron rinnalle ilmestyy lahjottu amatsoni (kuin otsapanta olisi annettu). Lisätään "Aloituskohta"-olioon valikosta 318.</summary>
public class AmazonStartAlly : MonoBehaviour
{
    [Tooltip("Pois päältä (oletus): amatsoni ei liity heron mukaan pelin alussa. Kytke päälle, kun haluat testata hänen kanssaan.")]
    public bool joinAtStart = false;

    IEnumerator Start()
    {
        if (!joinAtStart) yield break;
        yield return null; yield return null;   // odotetaan, että hero ja alue on asetettu
        if (AmazonBrain.HasAlly) yield break;
        PlayerController hero = null;
        foreach (var p in Players.All) if (p != null && !p.GameOver) { hero = p; break; }
        if (hero == null) yield break;
        AmazonBrain tmpl = null;
        foreach (var b in FindObjectsByType<AmazonBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (b != null && b.GetComponent<Enemy>() != null && !b.GetComponent<Enemy>().ally) { tmpl = b; break; }
        if (tmpl == null) { Debug.LogWarning("Aloitusapuri: amatsonimallia ei löytynyt (aja valikko 306 / 311)."); yield break; }
        var go = Instantiate(tmpl.gameObject);
        go.name = "Amatsoni (aloitusapuri)";
        go.transform.SetParent(null, true);
        go.transform.position = hero.transform.position + new Vector3(-2f, 0f, 0f);
        go.SetActive(true);
        yield return null;
        var brain = go.GetComponent<AmazonBrain>();
        if (brain != null) brain.JoinAsAlly(hero);
    }
}
