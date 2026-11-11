using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Vankilan vihujen sijoitus: siirtää annetun paikan pois laatikoista, pöydistä ja jo sijoitetuista vihuista (ettei mikään ole pelissä päällekkäin).
/// Käytössä vartijoissa (363) ja vangeissa (366). Ajojärjestys: 364 (pöydät, laatikot) ennen 363:a ja 366:a.</summary>
public static partial class BeatEmUpSetup
{
    /// <summary>Palauttaa lähimmän vapaan paikan: vihut vähintään minDx/minDy päässä toisistaan, laatikot ja pöydät crateDx/crateDy päässä.
    /// Hakee ensin saman x:n eri syvyyksiltä, sitten x-suunnassa sivulle (± 2,4 askelin) alueen kävelyrajojen sisällä.</summary>
    static Vector3 FreeSpot(Area a, UnityEngine.SceneManagement.Scene scene, Vector3 p, float minDx = 2.6f, float minDy = 0.55f, float crateDx = 2.6f, float crateDy = 0.8f)
    {
        var obstacles = new List<(Vector2 pos, bool solid)>();
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (e != null && e.gameObject.scene == scene) obstacles.Add((e.transform.position, false));
        foreach (var c in Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.gameObject.scene == scene) obstacles.Add((c.transform.position, true));
        foreach (var c in Object.FindObjectsByType<CoverCrate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.gameObject.scene == scene) obstacles.Add((c.transform.position, true));
        bool Free(Vector2 q)
        {
            foreach (var (pos, solid) in obstacles)
            {
                float dx = solid ? crateDx : minDx, dy = solid ? crateDy : minDy;
                if (Mathf.Abs(pos.x - q.x) < dx && Mathf.Abs(pos.y - q.y) < dy) return false;
            }
            return true;
        }
        float lo = a.walkMinX + 1.5f, hi = a.walkMaxX - 1.5f;
        // kandidaatit: sivusuunnassa 0, ±2,4, ±4,8 … ; kullakin x:llä syvyydet alkuperäinen, ylempi, alempi
        for (int step = 0; step < 14; step++)
            foreach (int sgn in step == 0 ? new[] { 0 } : new[] { 1, -1 })
            {
                float x = Mathf.Clamp(p.x + sgn * step * 2.4f, lo, hi);
                float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                foreach (float dy in new[] { 0f, 0.6f, -0.6f, 1.1f, -1.1f })
                {
                    float y = Mathf.Clamp(p.y + dy, yMin + 0.05f, yMax - 0.05f);
                    if (Free(new Vector2(x, y))) return new Vector3(x, y, 0f);
                }
            }
        return p;   // ei vapaata paikkaa: jätetään alkuperäiseen
    }
}
