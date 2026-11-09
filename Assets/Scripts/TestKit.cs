using UnityEngine;

/// <summary>
/// TILAPÄINEN testipaketti (vain editorissa): pelin alussa taskuun tulee kaksi pizzaa ja Bööni.
/// Poista tämä tiedosto (ja sen .meta), kun ei enää tarvita. Valikosta Total Fist / Testipaketti voi kytkeä pois.
/// </summary>
#if UNITY_EDITOR
public class TestKit : MonoBehaviour
{
    const string Pref = "TF_TestKit", MinePref = "TF_TestKitMine";

    [UnityEditor.MenuItem("Total Fist/Testipaketti: aloita Kuuasemalta päälle–pois")]
    static void ToggleMine() { UnityEditor.EditorPrefs.SetBool(MinePref, !UnityEditor.EditorPrefs.GetBool(MinePref, true)); Debug.Log("[TestKit] aloitus Kuuasemalta " + (UnityEditor.EditorPrefs.GetBool(MinePref, true) ? "PÄÄLLÄ" : "POIS")); }

    [UnityEditor.MenuItem("Total Fist/Testipaketti taskuun (2 pizzaa + Bööni) päälle\u2013pois")]
    static void Toggle() { UnityEditor.EditorPrefs.SetBool(Pref, !UnityEditor.EditorPrefs.GetBool(Pref, true)); Debug.Log("[TestKit] " + (UnityEditor.EditorPrefs.GetBool(Pref, true) ? "PÄÄLLÄ" : "POIS")); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!UnityEditor.EditorPrefs.GetBool(Pref, true) || FindFirstObjectByType<TestKit>() != null) return;
        var go = new GameObject("TestKit"); DontDestroyOnLoad(go); go.AddComponent<TestKit>();
    }

    float until;
    void Start() { until = Time.unscaledTime + 20f; }

    void Update()
    {
        if (Time.unscaledTime > until) { Destroy(gameObject); return; }
        foreach (var p in Players.All)
        {
            if (p == null || !p.enabled) continue;
            p.pocketFood = "pizza"; p.pocketFood2 = "pizza"; p.booniPocket = 1;
            Debug.Log("[TestKit] taskuun: 2 pizzaa + Bööni");
            if (UnityEditor.EditorPrefs.GetBool(MinePref, true))
            {
                var gs = new GameObject("TestKit aloitus: Kuuasema").AddComponent<GameStart>();   // peli alkaa Kuuasemalta
                gs.areaName = "Muurin kuuasema"; gs.sceneName = "Kuu"; gs.position = new Vector2(77469.4f, -2.7f);   // kuuasema (Muuri jaettu: Kuu-scene, valikko 349), hissin oven luota; Amatsoni seuraa mukana
                Debug.Log("[TestKit] aloitus: Kuuasema");
            }
            Destroy(gameObject); return;
        }
    }
}
#endif
