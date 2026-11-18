using UnityEngine;

/// <summary>
/// Linnakkeen katon Lokki: taistelu ennen alusta. Kun Lokin elämä putoaa rajan alle, hän on hävinnyt: tulee haavoittumattomaksi, juoksee alukseen (luukulle) ja katoaa.
/// Vasta sitten aluksen luukun ovi aktivoituu (muuten ympyrä/hyökkäysnappi veisi heti alukseen). Elämä ei putoa nollaan: Lokki ei kuole katolla.
/// </summary>
public sealed class RoofLokki : MonoBehaviour
{
    public Enemy lokki; public LokkiBrain brain; public GameObject hatchDoor; public float hatchX;
    [Range(0.05f, 0.9f)] public float fleeAt = 0.3f;
    bool fled, opened;

    void Update()
    {
        if (opened) return;
        if (lokki == null || lokki.IsDead) { Open(); return; }
        if (!fled)
        {
            if (lokki.Health <= lokki.maxHealth * fleeAt)
            {
                fled = true;
                if (brain != null) { brain.invulnerable = true; brain.reactive = true; brain.Flee(hatchX >= lokki.transform.position.x ? 1f : -1f); }
            }
        }
        else if (Mathf.Abs(lokki.transform.position.x - hatchX) < 1.2f || (hatchX > lokki.transform.position.x ? lokki.transform.position.x > hatchX : lokki.transform.position.x < hatchX))
        {
            lokki.gameObject.SetActive(false); Open();
        }
    }

    void Open()
    {
        opened = true;
        if (hatchDoor != null) hatchDoor.SetActive(true);
        GameHUD.Popup("LUUKKU AUKI", (Players.Primary != null ? Players.Primary.transform.position : transform.position) + new Vector3(0f, 3f, 0f), new Color(0.8f, 1f, 0.8f));
    }
}
