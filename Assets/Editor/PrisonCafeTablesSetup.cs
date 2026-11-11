using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan ruokalan pöydät (valikko 364): puisia pöytiä (Sprites/Rekvisiitta/poyta.png, ehjä kuva) kahdessa tasaisessa rivissä, kullakin kaksi makkara-annosta (Resources/Makkara/annos.png). Ei tuoppeja.
/// Pöydät ovat koristeita (eivät mene rikki). Suojalaatikot siirretään rivien väliin, pöytäsarakkeiden rakoihin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/364. Vankila: ruokalan puupöydät ja makkara-annokset (kaksi riviä)", false, 364)]
    static void AddPrisonCafeTables()
    {
        if (PlayModeBlocked()) return;
        var tbl = LoadSprites("poyta").OrderBy(sp => int.TryParse(sp.name.Substring(sp.name.LastIndexOf('_') + 1), out int k) ? k : 0).ToArray();
        const string plateP = "Assets/Resources/Makkara/annos.png";
        var plateImp = AssetImporter.GetAtPath(plateP) as TextureImporter;
        if (plateImp != null)
        {
            var st = new TextureImporterSettings(); plateImp.ReadTextureSettings(st);
            if (st.spriteAlignment != (int)SpriteAlignment.BottomCenter) { st.spriteAlignment = (int)SpriteAlignment.BottomCenter; plateImp.SetTextureSettings(st); plateImp.SaveAndReimport(); }   // lautanen seisoo pöydän pinnalla (juuri alareunassa)
        }
        var plate = AssetDatabase.LoadAssetAtPath<Sprite>(plateP);
        var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
        if (tbl.Length < 7) { Info("poyta.png: kuvia " + tbl.Length + "/7 (kohta 1)."); return; }
        if (plate == null) { Info("Puuttuu Assets/Resources/Makkara/annos.png."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–363."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var cafe = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vankilan ruokala");
            if (cafe == null) { Info("Ruokalaa ei löytynyt: aja valikko 359."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Vankilan ruokalan pöydät").ToArray()) Undo.DestroyObjectImmediate(o);
            float H = 2f * CamHalf, ppu = CafeImgH / H, img1 = CafeImgW / ppu, X0 = PrisonCafeX0;
            var root = new GameObject("Vankilan ruokalan pöydät");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Ruokalan pöydät");

            const int cols = 6; const float f0 = 0.30f, f1 = 1.78f, tableScale = 1.1f, plateScale = 1f;
            float colW = (f1 - f0) / cols;
            float[] depths = { 0.22f, 0.78f };   // kaksi riviä (0 = lattian takareuna, 1 = etureuna)
            int tables = 0;
            foreach (var d in depths)
                for (int col = 0; col < cols; col++)
                {
                    float x = X0 + (f0 + (col + 0.5f) * colW) * img1;
                    float yMax = cafe.MaxDepthAtX(x, cafe.maxDepthY), yMin = cafe.MinDepthAtX(x, cafe.minDepthY);
                    float y = Mathf.Lerp(yMax, yMin, d);
                    int order = Mathf.RoundToInt(-y * 100f);
                    // rikottava ja heitettävä pöytä (kuten klubin pöydät), annokset seuraavat pöytää
                    var t = new GameObject("Pöytä");
                    t.transform.SetParent(root.transform, false); t.transform.position = new Vector3(x, y, 0f);
                    var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(t.transform, false);
                    var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(t.transform, false);
                    sh.color = new Color(0f, 0f, 0f, 0.35f);
                    var c = t.AddComponent<Crate>();
                    c.body = vis; c.shadow = sh;
                    c.sprites = new[] { tbl[0], tbl[1], tbl[2] };
                    c.breakSprites = new[] { tbl[3], tbl[4], tbl[5], tbl[6] };
                    c.breakFrameTime = 0.1f; c.hitsToBreak = 3; c.breakable = true; c.footOffset = 0.04f;
                    c.visualScale = tableScale; c.shadowWidth = 2.6f * tableScale / 1.1f;
                    c.carryLower = 0.86f * tableScale; c.plowThrough = true; c.hitRadiusX = 1.4f * tableScale; c.debrisTime = 6f;
                    c.moneyChance = 0.3f; c.energyChance = 0f; c.foodChance = 0f; c.thermosChance = 0f; c.throwDamage = 22;
                    vis.sprite = tbl[0];
                    foreach (var side in new[] { -0.62f, 0.62f })
                    {
                        var p = new GameObject("Makkara-annos");
                        p.transform.SetParent(root.transform, false);
                        p.transform.position = t.transform.position;
                        var fp = p.AddComponent<FishPlate>();
                        fp.spriteSet = "Makkara"; fp.countMul = 0.35f; fp.scale = plateScale;
                        fp.table = c; fp.tableX = side * tableScale; fp.tableTop = 1.28f * tableScale;
                        fp.breakSounds = plateSnd;
                    }
                    tables++;
                }
            // suojalaatikot rivien väliin, pöytäsarakkeiden rakoihin
            int moved = 0;
            var cafeRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ruokala");
            if (cafeRoot != null)
            {
                var crates = cafeRoot.GetComponentsInChildren<CoverCrate>(true).OrderBy(cc => cc.transform.position.x).ToArray();
                int[] gaps = { 1, 3, 5 };   // sarakkeiden välit (1 = sarakkeiden 1 ja 2 välissä …)
                for (int i = 0; i < crates.Length && i < gaps.Length; i++)
                {
                    float x = X0 + (f0 + gaps[i] * colW) * img1;
                    float y = Mathf.Lerp(cafe.MaxDepthAtX(x, cafe.maxDepthY), cafe.MinDepthAtX(x, cafe.minDepthY), 0.5f);
                    Undo.RecordObject(crates[i].transform, "Laatikko");
                    crates[i].transform.position = new Vector3(x, y, 0f);
                    var sr = crates[i].GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = Mathf.RoundToInt(-y * 100f);
                    EditorUtility.SetDirty(crates[i]); moved++;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Ruokalaan " + tables + " puupöytää kahdessa rivissä (6 + 6, tasavälein), joka pöydällä 2 makkara-annosta, ei tuoppeja; " + moved + " suojalaatikkoa siirretty rivien väliin. Pöydät ovat rikottavia ja heitettäviä (kuten klubin pöydät), annokset lentävät ja hajoavat mukana.\nAjamatta Unityssä; jos pöydät ovat liian isoja tai pieniä, muuta tableScale-vakiota.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
