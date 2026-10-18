using System.Collections;
using UnityEngine;

// Isolated test uses its own persisted wallet (10,000 mk initially), never the combat wallet.
[DefaultExecutionOrder(-500)]
public sealed class PokerTestBootstrap : MonoBehaviour
{
    public PokerPresentationSettings settings;
    string error;
    IEnumerator Start()
    {
        yield return null;
        var service = PokerService.Ensure();
        if (!service.Open(settings, true)) error = service.Error ?? "Pokeripöytää ei voitu avata.";
    }
    void OnGUI()
    {
        if (PokerService.Instance != null && PokerService.Instance.Visible) return;
        GUI.Label(new Rect(30, 30, 900, 60), error ?? "Pokerin testiscene. Testiraha on erillään pelirahasta.");
        if (GUI.Button(new Rect(30, 100, 300, 60), "Avaa testipöytä uudelleen"))
        {
            var service = PokerService.Ensure();
            if (!service.Open(settings, true)) error = service.Error;
        }
    }
}
