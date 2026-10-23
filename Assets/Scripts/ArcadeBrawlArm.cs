using UnityEngine;

/// <summary>
/// Pelihallin koneet pysyvät ehjinä ja paikallaan, kunnes hallin tappelu (SalsaBrawl) alkaa. Sen jälkeen niitä voi lyödä rikki ja nostaa
/// (hero, Rekkamies, Kovis ja Tom heittävät niitä).
/// </summary>
public class ArcadeBrawlArm : MonoBehaviour
{
    public SalsaBrawl brawl;
    public Crate[] machines;
    bool armed;

    void Update()
    {
        if (armed || brawl == null || !brawl.Started) return;
        armed = true;
        foreach (var c in machines) if (c != null) { c.immune = false; c.anchored = false; }
    }
}
