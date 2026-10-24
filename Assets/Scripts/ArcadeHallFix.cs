using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pelihallin poistumisovi: kävelyraja viedään oven keskelle ja oven toiminta-alue laajennetaan, jotta ulos pääsee aina (ei vaadi editorivalikkoa).
/// Ajetaan automaattisesti, kun scene latautuu (halli voi olla omassa scenessään).
/// </summary>
public static class ArcadeHallFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Apply();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) { Apply(); }

    public static void Apply()
    {
        Area hall = null;
        foreach (var a in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a != null && a.areaName == "Pelihalli") { hall = a; break; }
        if (hall == null) return;
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d == null || d.name != "Pelihallista kadulle") continue;
            d.halfWidth = Mathf.Max(d.halfWidth, 2.6f);
            d.maxDistanceFromWall = Mathf.Max(d.maxDistanceFromWall, 4f);
            // vasen kävelyraja oven keskelle (ei oven oikeaan reunaan), jotta ovelle pääsee
            float doorX = d.transform.position.x;
            if (hall.walkMinX > doorX + 0.3f) hall.walkMinX = doorX + 0.3f;
        }
    }
}
