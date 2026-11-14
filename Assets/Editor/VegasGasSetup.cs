using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 391: kaasupullot Vegasin kadulle (testipaikat; lopullinen paikka rakennustyömaa). Kuva Assets/Sprites/Rekvisiitta/kaasupullo.png. Toiminta GasCylinder.cs.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/391. Las Vegas: kaasupullot kadulle (lyö: sihisee, lähtee rakettina, räjähtää)", false, 391)]
    static void AddVegasGas()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Vegas-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            if (street == null) { Info("Alue \"Vegasin katu\" puuttuu: aja valikko 378."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin kaasupullot").ToArray()) Undo.DestroyObjectImmediate(old);
            const float capH = 2.2f;
            var spr = ImportShipSprite("Assets/Sprites/Rekvisiitta/kaasupullo.png", 768f / capH, false, SpriteAlignment.BottomCenter);
            if (spr == null) { Info("Kaasupullon kuva puuttuu (Assets/Sprites/Rekvisiitta/kaasupullo.png)."); return; }
            var root = new GameObject("Vegasin kaasupullot");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin kaasupullot");
            float H = 2f * CamHalf, imgW = 5120f / (1844f / H);
            int n = 0;
            // (x kuvaleveyksinä kadun alusta, syvyys 0 = ala … 1 = takaseinä)
            foreach (var (u, f) in new[] { (2.20f, 0.45f), (2.24f, 0.62f), (2.95f, 0.5f), (3.08f, 0.4f), (3.25f, 0.65f) })
            {
                float x = VegasStreetX0 + imgW * u;
                float y = Mathf.Lerp(street.MaxDepthAtX(x, street.maxDepthY), street.MinDepthAtX(x, street.minDepthY), 1f - f);
                var go = new GameObject("Kaasupullo " + (++n));
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(x, y, 0f);
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                sh.color = new Color(0f, 0f, 0f, 0.35f);
                var c = go.AddComponent<Crate>();
                c.body = vis; c.shadow = sh; c.sprites = new[] { spr };
                c.breakable = false; c.anchored = true; c.visualScale = 1f; c.footOffset = 0f; c.shadowWidth = 1.2f; c.hitRadiusX = 0.8f;
                c.moneyChance = 0f; c.energyChance = 0f; c.foodChance = 0f; c.thermosChance = 0f;
                vis.sprite = spr;
                var g = go.AddComponent<GasCylinder>(); g.capHeight = capH;
                EditorUtility.SetDirty(c); EditorUtility.SetDirty(g);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(n + " kaasupulloa lisätty Vegasin kadulle (kasinon ja kappelin väliin). Lyö pulloa: kilahdus, kaatuu, sihisee, lähtee kiihtyen iskun suuntaan ja räjähtää. Toinen pullo tai gallona sytyttää viereiset.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
