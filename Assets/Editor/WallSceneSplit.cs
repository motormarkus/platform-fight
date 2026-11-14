using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Muurin scenen jako kolmeen: Muuri (kiitotie, terminaali, kanikonttori, kuuhissi, muuri, torni), Kuu (kuuasema, kuun pinta, laboratorio, käytävät, olion huone)
/// ja Linnake (juhlasali). Tekstuurit latautuvat scene kerrallaan, ja Area.FreeOtherScenes purkaa edellisen kentän alueen vaihtuessa.
/// Valikko 349 jakaa, 350 yhdistää takaisin (tarvitaan ennen Muurin rakennusvalikoita 327/330, jotka rakentavat yhteen sceneen). Koordinaatit eivät muutu.
/// Työnkulku: 350 (jos jaettu) -> 330 / muut rakennusvalikot -> 349.
/// </summary>
public static partial class BeatEmUpSetup
{
    const string WallMainScene = "Muuri", WallMoonScene = "Kuu", WallFortScene = "Linnake";
    static string WallPartPath(string level) => "Assets/Scenes/Areas/" + level + ".unity";

    /// <summary>Alueen sijainnin (x) mukainen scene: kuu 76870–78000 (käytävät, kuuasema, pinta, laboratorio, olion huone), linnake 78990–79290 (sali ja pomohuoneet), muu Muuri.</summary>
    static string WallLevelOfX(float x) => x >= 76870f && x < 78000f ? WallMoonScene : x >= 78990f && x < 79290f ? WallFortScene : WallMainScene;

    [MenuItem("Beat em up/349. Muurin scenen jako: Muuri, Kuu ja Linnake omiin sceneihinsä", false, 349)]
    static void SplitWallScene()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(WallPartPath(WallMainScene))) { Info("Muuri.unity puuttuu: aja valikko 330."); return; }
        if (File.Exists(WallPartPath(WallMoonScene)) || File.Exists(WallPartPath(WallFortScene))) { Info("Muuri on jo jaettu (Kuu.unity tai Linnake.unity löytyy). Jos rakennat Muurin uudelleen, aja ensin 350 (yhdistä), sitten 330 ja lopuksi tämä."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var main = EditorSceneManager.OpenScene(WallPartPath(WallMainScene), OpenSceneMode.Single);
        // 0) yhteiset säiliöjuuret (esim. "Muurin ovet": kaikki ovet yhdessä) jaetaan scenekohtaisiksi samannimisiksi juuriksi, jotta kunkin scenen ovet menevät mukana
        foreach (var r0 in main.GetRootGameObjects().ToArray())
        {
            if (r0 == null || r0.GetComponents<Component>().Length != 1 || r0.transform.childCount < 2) continue;
            var byLevel = new Dictionary<string, List<Transform>>();
            foreach (Transform ch in r0.transform)
            {
                var cx = ch.GetComponentsInChildren<Transform>(true).Select(t => t.position.x).Where(x => Mathf.Abs(x) > 1f).OrderBy(x => x).ToList();
                string lv0 = cx.Count == 0 ? WallMainScene : WallLevelOfX(cx[cx.Count / 2]);
                if (!byLevel.TryGetValue(lv0, out var l0)) byLevel[lv0] = l0 = new List<Transform>();
                l0.Add(ch);
            }
            if (byLevel.Count < 2) continue;
            foreach (var kv in byLevel)
            {
                if (kv.Key == WallMainScene) continue;   // Muurin osa jää alkuperäiseen juureen
                var nr = new GameObject(r0.name); nr.transform.position = Vector3.zero;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(nr, main);
                foreach (var ch in kv.Value) ch.SetParent(nr.transform, true);
            }
        }
        var roots = main.GetRootGameObjects();

        // 1) juuriolioiden scene: alue kameran vasemman reunan mukaan, muut keskimmäisen osan x:n mukaan; paikattomat jäävät Muuriin
        var level = new Dictionary<GameObject, string>();
        foreach (var r in roots)
        {
            var area = r.GetComponent<Area>();
            if (area != null) { level[r] = WallLevelOfX(area.camMinX); continue; }
            var xs = r.GetComponentsInChildren<Transform>(true).Select(t => t.position.x).Where(x => Mathf.Abs(x) > 1f).OrderBy(x => x).ToList();
            level[r] = xs.Count == 0 ? WallMainScene : WallLevelOfX(xs[xs.Count / 2]);
        }
        // paikattomat juuret (esim. "Juhlahallin tappelu": SalsaBrawl ilman omaa x:ää) seuraavat viittaustensa kohteita: enemmistön scene
        var unplaced = roots.Where(r => r.GetComponent<Area>() == null && !r.GetComponentsInChildren<Transform>(true).Any(t => Mathf.Abs(t.position.x) > 1f)).ToList();
        for (int pass = 0; pass < 2; pass++)
            foreach (var r in unplaced)
            {
                var votes = new Dictionary<string, int>();
                foreach (var c in r.GetComponentsInChildren<Component>(true))
                {
                    if (c == null) continue;
                    var p0 = new SerializedObject(c).GetIterator();
                    while (p0.NextVisible(true))
                    {
                        if (p0.propertyType != SerializedPropertyType.ObjectReference || p0.objectReferenceValue == null) continue;
                        var o0 = p0.objectReferenceValue; var g0 = o0 is Component oc0 ? oc0.gameObject : o0 as GameObject;
                        if (g0 == null || !g0.scene.IsValid() || g0.scene != main) continue;
                        var rt0 = g0.transform.root.gameObject;
                        if (rt0 == r || !level.TryGetValue(rt0, out var lv1)) continue;
                        votes[lv1] = votes.TryGetValue(lv1, out int n1) ? n1 + 1 : 1;
                    }
                }
                if (votes.Count > 0) level[r] = votes.OrderByDescending(kv => kv.Value).First().Key;
            }
        string LevelOf(GameObject go) { var rt = go.transform.root.gameObject; return level.TryGetValue(rt, out var l) ? l : WallMainScene; }

        // 2) viittaukset sceneltä toiselle: oven kohde, kyydin kohde ja aloituskohta korvataan nimellä; kaikki muu keskeyttää (mitään ei muuteta)
        var bad = new List<string>(); var doorFix = new List<Door>(); var rideFix = new List<RideExit>(); var startFix = new List<GameStart>();
        foreach (var r in roots)
            foreach (var c in r.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                string mine = level[r];
                var p = new SerializedObject(c).GetIterator();
                while (p.NextVisible(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    var o = p.objectReferenceValue; var tgo = o is Component oc ? oc.gameObject : o as GameObject;
                    if (tgo == null || !tgo.scene.IsValid() || tgo.scene != main) continue;
                    if (LevelOf(tgo) == mine) continue;
                    if (c is Door && p.name == "target") { doorFix.Add((Door)c); continue; }
                    if (c is RideExit && p.name == "target") { rideFix.Add((RideExit)c); continue; }
                    if (c is GameStart && p.name == "area") { startFix.Add((GameStart)c); continue; }
                    bad.Add(r.name + " / " + c.gameObject.name + "." + c.GetType().Name + "." + p.name + " -> " + tgo.name);
                }
            }
        if (bad.Count > 0)
        {
            Info("Jako peruttu: nämä viittaukset ylittäisivät scenen rajan:\n" + string.Join("\n", bad.Distinct().Take(25)) + (bad.Count > 25 ? "\n... yhteensä " + bad.Count : "") + "\n\nMitään ei tallennettu (scene ladattiin uudelleen levyltä).");
            EditorSceneManager.OpenScene(WallPartPath(WallMainScene), OpenSceneMode.Single);   // hylkää säiliöjuurten väliaikainen jako
            return;
        }
        foreach (var d in doorFix.Distinct())
        {
            var ta = d.target; d.targetName = ta.areaName; d.targetScene = LevelOf(ta.gameObject); d.target = null; EditorUtility.SetDirty(d);
        }
        foreach (var rx in rideFix.Distinct())
        {
            var ta = rx.target; rx.targetName = ta.areaName; rx.targetScene = LevelOf(ta.gameObject); rx.target = null; EditorUtility.SetDirty(rx);
        }
        foreach (var s in startFix.Distinct())
        {
            var ta = s.area; s.areaName = ta.areaName; s.sceneName = LevelOf(ta.gameObject); s.area = null; EditorUtility.SetDirty(s);
        }

        // 3) siirto: Kuu ja Linnake omiin sceneihinsä, loput jäävät Muuriin
        var areaNamesOf = new Dictionary<string, List<string>>();
        foreach (var lv in new[] { WallMainScene, WallMoonScene, WallFortScene }) areaNamesOf[lv] = new List<string>();
        foreach (var r in roots) foreach (var a in r.GetComponentsInChildren<Area>(true)) if (!areaNamesOf[level[r]].Contains(a.areaName)) areaNamesOf[level[r]].Add(a.areaName);
        var counts = new Dictionary<string, int>();
        foreach (var lv in new[] { WallMoonScene, WallFortScene })
        {
            var move = roots.Where(r => level[r] == lv).ToList();
            counts[lv] = move.Count;
            if (move.Count == 0) continue;
            var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            foreach (var g in move) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, sc);
            EditorSceneManager.SaveScene(sc, WallPartPath(lv));
            EditorSceneManager.CloseScene(sc, true);
            File.WriteAllLines("Assets/Scenes/Areas/" + lv + ".areas.txt", areaNamesOf[lv]);
        }
        File.WriteAllLines("Assets/Scenes/Areas/" + WallMainScene + ".areas.txt", areaNamesOf[WallMainScene]);
        EditorSceneManager.MarkSceneDirty(main); EditorSceneManager.SaveScene(main);
        AssetDatabase.Refresh();
        var list = EditorBuildSettings.scenes.ToList();
        foreach (var lv in new[] { WallMoonScene, WallFortScene }) if (!list.Any(s => s.path == WallPartPath(lv))) list.Add(new EditorBuildSettingsScene(WallPartPath(lv), true));
        EditorBuildSettings.scenes = list.ToArray();

        Info("Muuri jaettu kolmeen sceneen.\n" +
             "• Muuri: " + string.Join(", ", areaNamesOf[WallMainScene]) + "\n" +
             "• Kuu (" + counts[WallMoonScene] + " juuriolioa): " + string.Join(", ", areaNamesOf[WallMoonScene]) + "\n" +
             "• Linnake (" + counts[WallFortScene] + " juuriolioa): " + string.Join(", ", areaNamesOf[WallFortScene]) + "\n" +
             "Ovia korjattu: " + (doorFix.Distinct().Count() + rideFix.Distinct().Count()) + ", aloituskohtia: " + startFix.Distinct().Count() + ". Scenet lisätty Build Settingsiin.\n" +
             "Rakennusvalikot (327/330 ym.) vaativat yhden scenen: aja 350 ennen niitä ja 349 niiden jälkeen.");
    }

    [MenuItem("Beat em up/350. Muurin scenen yhdistäminen takaisin yhdeksi (ennen 330:tä)", false, 350)]
    static void MergeWallScene()
    {
        if (PlayModeBlocked()) return;
        bool hasMoon = File.Exists(WallPartPath(WallMoonScene)), hasFort = File.Exists(WallPartPath(WallFortScene));
        if (!hasMoon && !hasFort) { Info("Muuri ei ole jaettu (Kuu.unity ja Linnake.unity puuttuvat)."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var main = EditorSceneManager.OpenScene(WallPartPath(WallMainScene), OpenSceneMode.Single);
        var names = new List<string>(File.Exists("Assets/Scenes/Areas/Muuri.areas.txt") ? File.ReadAllLines("Assets/Scenes/Areas/Muuri.areas.txt") : new string[0]);
        int moved = 0;
        foreach (var lv in new[] { WallMoonScene, WallFortScene })
        {
            string path = WallPartPath(lv);
            if (!File.Exists(path)) continue;
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            foreach (var g in sc.GetRootGameObjects()) { UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, main); moved++; }
            EditorSceneManager.CloseScene(sc, true);
            string txt = "Assets/Scenes/Areas/" + lv + ".areas.txt";
            if (File.Exists(txt)) { names.AddRange(File.ReadAllLines(txt)); File.Delete(txt); if (File.Exists(txt + ".meta")) File.Delete(txt + ".meta"); }
            AssetDatabase.DeleteAsset(path);
        }
        File.WriteAllLines("Assets/Scenes/Areas/Muuri.areas.txt", names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct());
        EditorSceneManager.MarkSceneDirty(main); EditorSceneManager.SaveScene(main);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(s => s.path != WallPartPath(WallMoonScene) && s.path != WallPartPath(WallFortScene)).ToArray();
        AssetDatabase.Refresh();
        Info("Muuri yhdistetty: " + moved + " juuriolioa siirretty takaisin Muuri.unityyn, Kuu.unity ja Linnake.unity poistettu. Voit ajaa 330. Aja 349 sen jälkeen.");
    }
}
