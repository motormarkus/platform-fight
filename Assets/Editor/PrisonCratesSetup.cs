using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan rikottavat puulaatikot (valikko 373): kolme iskua hajottaa (tai heitetään), sisältä tippuu satunnaisesti energiajuomatölkki (45 %) ja markkoja (30 %). Ei ruokaa.
/// Laatikot jaetaan osastoille, ruokalaan, kuntopihalle, henkilökunnan tiloihin ja katolle. Kuvat: Sprites/Rekvisiitta/laatikko.png (+ laatikko_sirpaleet.png).</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/373. Vankila: rikottavat laatikot (satunnaisesti energiatölkkejä)", false, 373)]
    static void AddPrisonBreakableCrates()
    {
        if (PlayModeBlocked()) return;
        if (AssetImporter.GetAtPath(CratePath) == null) { Info("Puuttuu " + CratePath); return; }
        SetupAndSlice(CratePath);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(CratePath).OfType<Sprite>()
            .OrderBy(s => int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
        if (sprites.Length == 0) { Info("laatikko.png ei leikkaudu."); return; }
        var bti = AssetImporter.GetAtPath(CrateBurstPath) as TextureImporter;
        Sprite burst = null;
        if (bti != null)
        {
            bti.textureType = TextureImporterType.Sprite; bti.spriteImportMode = SpriteImportMode.Single; bti.spritePixelsPerUnit = 200;
            bti.alphaIsTransparency = true; bti.mipmapEnabled = false; bti.SaveAndReimport();
            burst = AssetDatabase.LoadAssetAtPath<Sprite>(CrateBurstPath);
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–363."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vankilan laatikot").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Vankilan laatikot");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan laatikot");
            int count = 0;

            Crate MakeCrate(Transform parent, Vector3 pos)
            {
                var go = new GameObject("Laatikko " + (++count));
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                var c = go.AddComponent<Crate>();
                c.sprites = sprites; c.burstSprite = burst; c.visualScale = 1.2f; c.hitRadiusX = 1.05f; c.body = vis; c.shadow = sh;
                c.moneyChance = 0.3f; c.energyChance = 0.45f; c.foodChance = 0f;   // tölkki (energiajuoma) usein, rahaa joskus, ei ruokaa
                vis.sprite = sprites[0];
                vis.transform.localPosition = new Vector3(0f, -c.footOffset, 0f);
                EditorUtility.SetDirty(c);
                return c;
            }

            // (alue, osuudet kävelyalueen leveydestä, syvyys): 2–3 laatikkoa / alue, ei ovien kohdalla
            var plan = new (string area, float[] fr, float[] depth)[]
            {
                ("Vankila", new[] { 0.35f, 0.7f }, new[] { 0.7f, 0.6f }),
                ("Vankilan D-osasto", new[] { 0.3f, 0.62f }, new[] { 0.65f, 0.35f }),
                ("Vankilan työpaja", new[] { 0.28f, 0.6f }, new[] { 0.65f, 0.4f }),
                ("Vankilan E-osasto", new[] { 0.32f, 0.66f }, new[] { 0.4f, 0.65f }),
                ("Vankilan ruokala", new[] { 0.2f, 0.9f }, new[] { 0.5f, 0.5f }),
                ("Vankilan kuntopiha", new[] { 0.12f, 0.36f, 0.64f, 0.9f }, new[] { 0.6f, 0.45f, 0.65f, 0.5f }),
                ("Vankilan henkilökunta", new[] { 0.3f, 0.7f }, new[] { 0.5f, 0.45f }),
                ("Vankilan katto", new[] { 0.3f, 0.5f, 0.62f }, new[] { 0.6f, 0.4f, 0.65f }),
            };
            var report = new List<string>();
            foreach (var (an, fr, depth) in plan)
            {
                var a = areas.FirstOrDefault(x => x.areaName == an);
                if (a == null) { report.Add(an + ": ei löytynyt"); continue; }
                var tf = Object.FindObjectsByType<TwoFloorCorridor>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t != null && t.gameObject.scene == scene && t.area == a);
                var grp = new GameObject("Laatikot: " + an); grp.transform.SetParent(root.transform, false);
                for (int i = 0; i < fr.Length; i++)
                {
                    float x = Mathf.Lerp(a.walkMinX, a.walkMaxX, fr[i]);
                    float yMax = a.MaxDepthAtX(x, a.maxDepthY), yMin = a.MinDepthAtX(x, a.minDepthY);
                    if (tf != null) { yMax = tf.lowerMax; yMin = tf.lowerMin; }   // kaksikerroksinen käytävä: laatikot alakerran lattialle (ei kerrosten väliin ilmaan)
                    float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth[i]), yMin + 0.05f, yMax - 0.05f);
                    MakeCrate(grp.transform, FreeSpot(a, scene, new Vector3(x, y, 0f)));
                }
                report.Add(an + " " + fr.Length);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " rikottavaa laatikkoa lisätty: " + string.Join(", ", report) + ".\nKolme iskua hajottaa (tai nosta ja heitä); sisältä tippuu satunnaisesti energiatölkki (45 %) ja markkoja (30 %), ei ruokaa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
