using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Linnakkeen ovi kuun pinnalla siirretään sille kohdalle, jossa tie kääntyy linnaketta kohti (kuva muuri_kuupinta_vasen3, px ~1000).
/// Ajonaikainen korjaus, ettei valikkoa tarvitse ajaa uudelleen (valikko 340 rakentaa uudet scenet jo oikeaan paikkaan: SurfFortDoorPx).
/// </summary>
public static class MoonDoorFix
{
    const float WallSurfX0 = 77500f, TargetPx = 1000f, OldPx = 640f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() { SceneManager.sceneLoaded -= OnLoaded; SceneManager.sceneLoaded += OnLoaded; Apply(); }
    static void OnLoaded(Scene s, LoadSceneMode m) { Apply(); }

    static void Apply()
    {
        Door fort = null, back = null;
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d == null) continue;
            if (d.name == "Kuun pinta: linnakkeeseen") fort = d;
            else if (d.name == "Juhlahalli: ulos") back = d;
        }
        if (fort == null) return;
        const float ppu = 941f / 12f;   // 941 px = 2 * CamHalf (6) yksikköä
        float want = WallSurfX0 + TargetPx / ppu, old = WallSurfX0 + OldPx / ppu;
        if (Mathf.Abs(fort.transform.position.x - old) > 0.5f) return;   // jo siirretty (uusi build) tai eri paikassa
        var p = fort.transform.position; fort.transform.position = new Vector3(want, p.y, p.z);
        if (back != null) { var sp = back.spawnPoint; back.spawnPoint = new Vector2(want + 1.6f, sp.y); }
    }
}
