using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Las Vegasin Strip, kasinon ulkopuoli (valikko 378): panttilainaamo, baari ja kasinon julkisivu (vasemmalta oikealle) (kuvat vegas_katu_baari, vegas_katu_kasino).
/// Parallaksit samat kuin katolla (taivas, hotellit, palmut). Kasinon sisätilan vasen ovipari vie tänne ja kasinon ulko-ovi takaisin sisään.
/// Oikea jatko tulee myöhemmin (kuva päättyy kasinon kulmapilariin). Aja valikko 376 ensin.</summary>
public static partial class BeatEmUpSetup
{
    const float VegasStreetX0 = 104000f;

    [MenuItem("Beat em up/378. Las Vegas: Strip (kasinon ulkopuoli, baari) ja kasinon vasen ovi", false, 378)]
    static void AddVegasStreet()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vegas_katu_pantti", "vegas_katu_baari", "vegas_katu_kasino", "vegas_taivas", "vegas_hotellit", "vegas_palmut" })
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var casino = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin kasino");
            if (casino == null) { Info("Alue \"Vegasin kasino\" puuttuu: aja valikko 376."); return; }
            foreach (var n in new[] { "Vegasin katu", "Vegasin katu ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasStreetX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            var root = new GameObject("Vegasin katu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vegasin katu");

            var area = ReuseArea("Alue: Vegasin katu");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Vegasin katu"; area.useSidewalk = false;
            float casX = X0 + 2f * imgW;                                             // kasinopalan vasen reuna (vasemmalla panttilainaamo ja baari)
            area.maxDepthY = top - 0.675f * H; area.minDepthY = top - 0.985f * H;   // jalkakäytävä + katu: seinän alareuna ~66 %
            area.walkMinX = X0 + imgW * 0.03f; area.walkMaxX = casX + imgW * 0.60f;  // kasinon kulmapilarin jälkeen kuva on musta (jatko myöhemmin)
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, casX + imgW * 0.645f - W * 0.5f);   // kamera ei näytä mustaa reunaa
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];

            // etuala: baari (vasen) ja kasino (oikea)
            foreach (var (name, file, px) in new[] { ("Katu: panttilainaamo", "vegas_katu_pantti", X0 + imgW * 0.5f), ("Katu: baari", "vegas_katu_baari", X0 + imgW * 1.5f), ("Katu: kasino", "vegas_katu_kasino", casX + imgW * 0.5f) })
            {
                var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(px, cy, 0f);
            }

            // taustatasot (samat kuin katolla); baarikuvan aukosta näkyy Vegas
            var layers = new[] { ("Taivas", "vegas_taivas", -10100, 0.02f, 0), ("Hotellit", "vegas_hotellit", -10050, 0.25f, 1), ("Palmut", "vegas_palmut", -10040, 0.5f, 1) };
            float left = area.camMinX - W * 0.5f;
            foreach (var (name, file, order, factor, mirrors) in layers)
            {
                var sp = ImportShipSprite(T + file + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + file); return; }
                var sr = new GameObject("Katu: " + name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order;
                sr.transform.position = new Vector3(left + sp.bounds.size.x * 0.5f, cy + (file == "vegas_taivas" ? 0f : 0.325f * H), 0f);   // hotellit ja palmut ylös: jalat jalkakäytävän takana (katu on kattoa korkeammalla)
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.mirrorCopies = mirrors;
                pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
            }

            // ovet: kasinon sisätilan vasen ovipari <-> kasinon ulko-ovi (kuvan x ~25 %)
            var doorRoot = new GameObject("Vegasin katu ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "Vegasin katu ovet");
            {
                float inX = VegasCasinoX0 + imgW * 0.152f, outX = casX + imgW * 0.25f;
                float inY = Mathf.Lerp(casino.maxDepthY, casino.minDepthY, 0.12f), outY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.12f);
                var toStreet = new GameObject("Vegas: kasinon vasen ovi ulos").AddComponent<Door>();
                toStreet.transform.SetParent(doorRoot.transform, false); toStreet.transform.position = new Vector3(inX, casino.maxDepthY - 0.3f, 0f);
                toStreet.prompt = "Mene ulos kadulle"; toStreet.here = casino; toStreet.target = area; toStreet.spawnPoint = new Vector2(outX, outY);
                toStreet.halfWidth = 1.8f; toStreet.maxDistanceFromWall = 2.4f; toStreet.onGround = true; toStreet.groundHalfDepth = 1.6f;
                var toCasino = new GameObject("Vegas: kasinon ulko-ovi sisään").AddComponent<Door>();
                toCasino.transform.SetParent(doorRoot.transform, false); toCasino.transform.position = new Vector3(outX, area.maxDepthY - 0.3f, 0f);
                toCasino.prompt = "Mene kasinolle"; toCasino.here = area; toCasino.target = casino; toCasino.spawnPoint = new Vector2(inX, inY);
                toCasino.halfWidth = 1.8f; toCasino.maxDistanceFromWall = 2.4f; toCasino.onGround = true; toCasino.groundHalfDepth = 1.6f;
            }
            EditorUtility.SetDirty(area);

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new System.Collections.Generic.List<string>();
            if (!names.Contains("Vegasin katu")) names.Add("Vegasin katu");
            File.WriteAllLines(areasPath, names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin katu rakennettu (x " + X0.ToString("0") + "–" + (casX + imgW).ToString("0") + "): panttilainaamo, baari ja kasino, parallaksit taivas/hotellit/palmut. Kasinon sisätilan vasen ovi vie kadulle ja kasinon ulko-ovi takaisin. Oikea jatko puuttuu vielä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
