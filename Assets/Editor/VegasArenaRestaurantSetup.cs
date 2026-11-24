using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 432: WTF-areenan ravintola: Sohvi baaritiskin taakse (kauppa) ja pöytäryhmät Deacon's Denistä (Aavikkobaari: pöytä + tuolit + pullot/annokset).
/// Tiskin etuosa (vegas_areena_tiski.png, leikattu ravintolan kuvasta 2830..4725 x 878..1280 px) piirretään Sohvin eteen, joten jalat jäävät tiskin taakse.
/// Aja ensin 431 (aula) ja 376 (kasinon baarin Sohvi ja tiski ovat mallit). Aavikon scene avataan lukemista varten (ei muuteta).
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/432. Las Vegas: WTF-areenan ravintola: Sohvi tiskin taakse ja pöydät Deacon's Denistä", false, 432)]
    static void AddVegasArenaRestaurant()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/", stripFile = "vegas_areena_tiski";
        if (!File.Exists(T + stripFile + ".png")) { Info("Puuttuu " + stripFile + ".png."); return; }
        AssetDatabase.ImportAsset(T + stripFile + ".png", ImportAssetOptions.ForceUpdate);
        if (!File.Exists(VegasScenePath) || !File.Exists(DesertScenePath)) { Info("Vegasin tai Aavikon scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var dscene = EditorSceneManager.GetSceneByPath(DesertScenePath);
        bool dOpened = false;
        if (!dscene.IsValid() || !dscene.isLoaded) { dscene = EditorSceneManager.OpenScene(DesertScenePath, OpenSceneMode.Additive); dOpened = true; }
        try
        {
            var lobby = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "WTF-areenan aula");
            if (lobby == null) { Info("Alue \"WTF-areenan aula\" puuttuu: aja valikko 431."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "WTF-areenan ravintola").ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = VegasArenaLobbyX0;
            float ppu = 1884f / H, imgW = 5000f / ppu;
            float PX(int img, float px) => X0 + img * imgW + px / ppu;
            float PY(float py) => top - py / ppu;

            var root = new GameObject("WTF-areenan ravintola");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "WTF-areenan ravintola");
            var report = new List<string>();

            // ---- tiskin etuosa Sohvin eteen ----
            var stripSp = ImportShipSprite(T + stripFile + ".png", ppu, false, SpriteAlignment.Center);
            var strip = new GameObject("Baaritiskin etuosa").AddComponent<SpriteRenderer>();
            strip.transform.SetParent(root.transform, false); strip.sprite = stripSp; strip.sortingOrder = -9980;   // Sohvin (-9990) edessä, hahmojen takana
            strip.transform.position = new Vector3(PX(1, (2830f + 4725f) * 0.5f), PY((878f + 1280f) * 0.5f), 0f);

            // ---- Sohvi + kauppa (mallit kasinon baarista) ----
            var sohviT = Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Sohvi (kasinon baari)");
            var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == scene && c.name == "Kasinon baarin tiski (Sohvi)");
            if (sohviT != null && counterT != null)
            {
                float hx = PX(1, 3800f), hy = PY(1120f);   // hanan ja kahvinkeittimen välissä; jalat tiskin etuosan takana
                var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (WTF-areena)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); sg.transform.SetParent(root.transform, false);
                sg.transform.position = new Vector3(hx, hy, 0f); sg.transform.localScale = Vector3.one * 1.35f;
                foreach (var r in sg.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = -9990;
                var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "WTF-areenan baaritiski (Sohvi)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(root.transform, false);
                shopGo.transform.position = new Vector3(hx, lobby.maxDepthY, 0f);
                var sc = shopGo.GetComponent<ShopCounter>();
                sc.here = lobby; sc.title = "WTF ARENA BAR"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville";
                sc.halfWidth = 4.2f; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
                sc.items = new[]
                {
                    new ShopCounter.Item { name = "Pihvileipä", price = 9, heal = 60, stamina = 0, comment = "Paksu pihvi ja paahdettu leipä. Ottelijoiden lempiruokaa." },
                    new ShopCounter.Item { name = "Kanansiivet", price = 6, heal = 35, stamina = 10, comment = "Tulisia, kuten areenan tunnelma." },
                    new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                    new ShopCounter.Item { name = "Limsa", price = 3, heal = 15, stamina = 25, comment = "Kylmää ja kuplivaa." },
                };
                EditorUtility.SetDirty(sc);
                report.Add("Sohvi tiskin takana (x " + hx.ToString("0.0") + ")");
            }
            else report.Add("Sohvia EI lisätty: mallit puuttuvat (aja valikko 376: kasinon baarin Sohvi ja tiski)");

            // ---- pöytäryhmät Deacon's Denistä (Aavikkobaari) ----
            var den = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == dscene && a.areaName == "Aavikkobaari");
            Crate tmpl = null; List<GameObject> group = null; int bestScore = -1;
            if (den != null)
            {
                var crates = Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Where(c => c != null && c.gameObject.scene == dscene && c.name == "Pöytä" && c.stackedOn == null
                                && c.transform.position.x >= den.walkMinX && c.transform.position.x <= den.walkMaxX).ToList();
                var bottles = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b != null && b.gameObject.scene == dscene).ToList();
                var plates = Object.FindObjectsByType<FishPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(f => f != null && f.gameObject.scene == dscene).ToList();
                var chairs = Object.FindObjectsByType<Chair>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(ch => ch != null && ch.gameObject.scene == dscene).ToList();
                foreach (var c in crates)
                {
                    var p = c.transform.position;
                    var g = new List<GameObject> { c.gameObject };
                    g.AddRange(bottles.Where(b => b.table == c).Select(b => b.gameObject));
                    g.AddRange(plates.Where(f => f.table == c).Select(f => f.gameObject));
                    g.AddRange(chairs.Where(ch => Mathf.Abs(ch.transform.position.x - p.x) < 2.6f && Mathf.Abs(ch.transform.position.y - p.y) < 1.0f).Select(ch => ch.gameObject));
                    int score = g.Count;
                    if (score > bestScore) { bestScore = score; tmpl = c; group = g; }
                }
            }
            if (tmpl == null) report.Add("pöytiä EI lisätty: Deacon's Denin (Aavikkobaari) pöytää ei löytynyt");
            else
            {
                var tRoot = new GameObject("Pöydät (Deacon's Denin mallit)");
                tRoot.transform.SetParent(root.transform, false);
                Vector3 origin = tmpl.transform.position;
                var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
                float tableScale = Mathf.Max(0.1f, tmpl.visualScale);
                // annoksen korkeus pöydällä: Aavikon aamiaisannoksesta, jos sellainen on pöydällä; muuten oletus
                var refPlate = Object.FindObjectsByType<FishPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(f => f != null && f.gameObject.scene == dscene && f.spriteSet == "Aamiainen" && f.table != null && f.table.name == "Pöytä");
                float plateTop = refPlate != null ? refPlate.tableTop : 1.28f * tmpl.visualScale;
                group.RemoveAll(g => g.GetComponent<FishPlate>() != null);   // vanhat annokset pois mallista: tilalle aamiaisannokset
                // takarivi (ei baaritiskin edessä, jotta Sohville pääsee) ja eturivi
                var spots = new (int img, float px, float depth)[]
                {
                    (1, 500f, 0.30f), (1, 2050f, 0.30f), (2, 600f, 0.30f), (2, 2250f, 0.30f),
                    (1, 1250f, 0.75f), (1, 3800f, 0.75f), (2, 1400f, 0.75f), (2, 3000f, 0.75f),
                };
                int n = 0;
                foreach (var (img, px, depth) in spots)
                {
                    float x = PX(img, px);
                    float y = Mathf.Lerp(lobby.MaxDepthAtX(x, lobby.maxDepthY), lobby.minDepthY, depth);
                    Vector3 off = new Vector3(x, y, 0f) - origin;
                    var map = new Dictionary<GameObject, GameObject>();
                    foreach (var src in group)
                    {
                        var cp = Object.Instantiate(src); cp.name = src.name;
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cp, scene);
                        cp.transform.SetParent(tRoot.transform, true);
                        cp.transform.position = src.transform.position + off;
                        cp.transform.localScale = src.transform.lossyScale;
                        map[src] = cp;
                    }
                    var newTable = map[tmpl.gameObject].GetComponent<Crate>();
                    foreach (var cp in map.Values)
                    {
                        var b = cp.GetComponent<Bottle>(); if (b != null) b.table = newTable;
                        var f = cp.GetComponent<FishPlate>(); if (f != null) f.table = newTable;
                        foreach (var r in cp.GetComponentsInChildren<SpriteRenderer>(true))   // syvyysjärjestys uudella y:llä (kuten pelissä: -y * 100)
                            r.sortingOrder += Mathf.RoundToInt(-off.y * 100f);
                    }
                    // pekoni-leipä-muna-annokset (Resources/Aamiainen, kuten Aavikon dinerissä): kaksi per pöytä
                    foreach (var side in new[] { -0.55f, 0.55f })
                    {
                        var p = new GameObject("Aamiaisannos");
                        p.transform.SetParent(tRoot.transform, false); p.transform.position = newTable.transform.position;
                        var fp = p.AddComponent<FishPlate>();
                        fp.spriteSet = "Aamiainen"; fp.countMul = 0.35f; fp.scale = 1f;
                        fp.table = newTable; fp.tableX = side * tableScale; fp.tableTop = plateTop;
                        fp.breakSounds = plateSnd;
                    }
                    n++;
                }
                report.Add(n + " pöytäryhmää (malli: " + group.Count + " esinettä/ryhmä: pöytä, tuolit, pullot), joka pöydällä 2 pekoni-leipä-muna-annosta");
            }

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("WTF-areenan ravintola: " + string.Join(", ", report) + ".");
        }
        finally
        {
            if (dOpened && dscene.IsValid()) EditorSceneManager.CloseScene(dscene, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void RunVegasArenaRestaurantBatch()
    {
        batch = true;
        try { Step("432 areenan ravintola", AddVegasArenaRestaurant); }
        finally { batch = false; }
    }
}
