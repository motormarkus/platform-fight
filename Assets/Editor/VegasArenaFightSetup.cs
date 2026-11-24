using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 435: WTF-areenan kehään Marcus ja Deacon (RingMatch). Kun hero tulee katsomoon, he ottelevat keskenään (sparraus, ei kuolemaa);
/// voittaja haastaa heron, kehän portaat aukeavat ja ottelu alkaa, kun hero nousee kehään. Aja ensin 434 (katsomo, kehän taso ja etuosa).
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/435. Las Vegas: Marcus vs Deacon WTF-areenan kehässä (voittaja haastaa heron)", false, 435)]
    static void AddVegasArenaFight()
    {
        if (PlayModeBlocked()) return;
        var sheets = new Dictionary<string, Sprite[]>();
        foreach (var n in MarcusSheetNames.Concat(DeaconSheetNames))
        {
            var sp = LoadWalkerSprites(n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: " + n + ".png (Assets/Sprites/Viholliset)."); return; }
            sheets[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == ArenaRingAreaName);
            var arenaRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == ArenaRingAreaName);
            var levels = arenaRoot != null ? arenaRoot.GetComponent<SiteLevels>() : null;
            if (area == null || levels == null || levels.levelY == null || levels.levelY.Length < 2) { Info("Katsomo tai kehän taso puuttuu: aja valikko 434."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "WTF-areenan ottelu").ToArray()) Undo.DestroyObjectImmediate(o);

            var root = new GameObject("WTF-areenan ottelu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "WTF-areenan ottelu");

            var (mgo, me, mb) = BuildMarcus(sheets, scene);
            mgo.name = "Marcus (areena)"; mgo.transform.SetParent(root.transform, false);
            mb.ringArea = null; mb.manageHeroSparring = false; mb.hold = true;
            me.sparBelt = false; me.stayOnLevel = true;

            var (dgo, de, db) = BuildDeacon(sheets);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dgo, scene);
            dgo.name = "Deacon (areena)"; dgo.transform.SetParent(root.transform, false);
            db.hold = true; de.sparring = true; de.sparBelt = false; de.stayOnLevel = true;

            // kehän keskikohta ja aloituskulmat (kehän lattia x-rajojen puolivälissä)
            float matMin = levels.levelMinX[1], matMax = levels.levelMaxX[1], mid = 0.5f * (matMin + matMax), y = levels.levelY[1];
            var m = root.AddComponent<RingMatch>();
            m.area = area; m.levels = levels; m.fighterA = me; m.fighterB = de;
            m.cornerA = new Vector2(mid - 4.5f, y); m.cornerB = new Vector2(mid + 4.5f, y);
            float wakeX = levels.ladders.Length > 0 ? levels.ladders[0].x + 1.5f : area.walkMinX + 5f;
            m.heroWakeSpot = new Vector2(wakeX, Mathf.Lerp(levels.streetMax, levels.streetMin, 0.35f));
            mgo.transform.position = new Vector3(m.cornerA.x, m.cornerA.y, 0f); me.FacingRight = true;
            dgo.transform.position = new Vector3(m.cornerB.x, m.cornerB.y, 0f); de.FacingRight = false;
            EditorUtility.SetDirty(me); EditorUtility.SetDirty(de); EditorUtility.SetDirty(m);

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Marcus ja Deacon lisätty WTF-areenan kehään. Kun tulet katsomoon, he ottelevat keskenään (" + m.exhibitionHealth + " elämää, ei kuolemaa); " +
                 "kehän portaat (punaisen kulman vasemmalla puolella) ovat auki: jos nouset kehään kesken näytöksen, kaikki tappelevat kaikkia vastaan. " +
                 "Muuten voittaja haastaa sinut, ja ottelu alkaa, kun nouset kehään " +
                 "(voittajalla " + m.challengeHealth + " elämää; hävitessä heräät kehän edestä ja voit yrittää uudelleen). Voitosta vihje \"" + m.championClue + "\".\n" +
                 "Jos ajat valikon 434 uudelleen, aja 435 sen jälkeen.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
