using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Aluksen vihut ja loppukohtaus (valikko 419): kuurobotit cargoon, Gustav hyttiin ja Lokki ohjaamoon (finale: kaatavat iskut menevät perille, kombot samassa järjestyksessä, ei pakoa)
/// + ShipFinale ohjaamoon (vaurio, savu, tärinä, varoitukset, syöksy: kallistus + painottomuus, loppu). Mallit kopioidaan Kuu-, Thaitown- ja Aavikko-scenestä. Vaatii valikon 418. Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/419. Linnake: aluksen vihut (kuurobotit, Gustav, Lokki) ja syöksykohtaus", false, 419)]
    static void AddFortressShipEnemies()
    {
        if (PlayModeBlocked()) return;
        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        UnityEngine.SceneManagement.Scene Open(string path)
        {
            var s = EditorSceneManager.GetSceneByPath(path);
            if (!s.IsValid() || !s.isLoaded) { s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(s); }
            return s;
        }
        try
        {
            var sc = Open(scenePath);
            var all = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == sc).ToArray();
            var cargo = all.FirstOrDefault(a => a.areaName == "Aluksen cargo"); var hytti = all.FirstOrDefault(a => a.areaName == "Aluksen hytti"); var cockpit = all.FirstOrDefault(a => a.areaName == "Aluksen ohjaamo");
            if (cargo == null || hytti == null || cockpit == null) { Info("Aluksen alueet puuttuvat: aja valikko 418."); return; }

            // mallit muista sceneistä
            GameObject FindTemplate(string path, string displayName)
            {
                if (!File.Exists(path)) return null;
                var s = Open(path);
                return Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(e => e != null && e.gameObject.scene == s && e.displayName == displayName && !e.ally).Select(e => e.gameObject).FirstOrDefault();
            }
            var kuuT = FindTemplate("Assets/Scenes/Areas/Kuu.unity", "Kuurobotti");
            var gusT = FindTemplate("Assets/Scenes/Areas/Thaitown.unity", "Gustav");
            var lokT = FindTemplate("Assets/Scenes/Areas/Aavikko.unity", "Lokki");
            string miss = (kuuT == null ? "Kuurobotti " : "") + (gusT == null ? "Gustav " : "") + (lokT == null ? "Lokki " : "");
            if (miss.Length > 0) { Info("Mallit puuttuvat: " + miss + "(valikot 329 / Gustav / 397)."); return; }

            foreach (var n in new[] { "Aluksen vihut", "Aluksen loppukohtaus" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            var root = new GameObject("Aluksen vihut"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Aluksen vihut");

            GameObject Place(GameObject tmpl, string name, Area area, float x, float depth)
            {
                var c = Object.Instantiate(tmpl);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(c, sc);
                c.transform.SetParent(root.transform, true); c.name = name;
                float y = Mathf.Lerp(area.maxDepthY, area.minDepthY, depth);
                c.transform.position = new Vector3(x, y, 0f); if (name.StartsWith("Lokki")) c.transform.localScale *= 1.1f; c.SetActive(true);   // Lokki 10 % isommaksi
                return c;
            }
            // cargo: kuusi kuurobottia pitkin käytävää
            string info = "";
            float cw = cargo.walkMaxX - cargo.walkMinX;
            float[] fr = { 0.22f, 0.36f, 0.5f, 0.62f, 0.76f, 0.88f }, dp = { 0.3f, 0.7f, 0.45f, 0.75f, 0.3f, 0.6f };
            for (int i = 0; i < fr.Length; i++)
            {
                var g = Place(kuuT, "Kuurobotti (alus " + (i + 1) + ")", cargo, cargo.walkMinX + cw * fr[i], dp[i]);
                var e = g.GetComponent<Enemy>(); if (e != null) { e.wakeDistance = 13f; EditorUtility.SetDirty(e); }
            }
            info += "kuurobotit 6, ";
            // hytti: Gustav (oleskelutilan ja kanttiinin jälkeen, lennonvalvonnan alussa)
            var gus = Place(gusT, "Gustav (alus)", hytti, hytti.walkMinX + (hytti.walkMaxX - hytti.walkMinX) * 0.68f, 0.5f);
            var ge = gus.GetComponent<Enemy>(); if (ge != null) { ge.wakeDistance = 14f; EditorUtility.SetDirty(ge); }
            info += "Gustav, ";
            // ohjaamo: Lokki (finale)
            var lok = Place(lokT, "Lokki (alus)", cockpit, cockpit.walkMaxX - 9f, 0.5f);
            var le = lok.GetComponent<Enemy>(); var lb = lok.GetComponent<LokkiBrain>();
            if (lb != null) { lb.finale = true; lb.reactive = true; lb.invulnerable = false; lb.landKnockdown = true; lb.finaleKnockLand = 0.8f; lb.finaleKnockLandMid = 0.85f; EditorUtility.SetDirty(lb); }
            if (le != null) { le.wakeDistance = 99f; le.isBoss = true; EditorUtility.SetDirty(le); }
            info += lb != null ? "Lokki (finale), " : "Lokki (LokkiBrain puuttui!), ";

            // Lokki katolla: taistelu ennen alusta (finale-tila). Kun hän häviää, hän juoksee alukseen ja luukku aktivoituu (RoofLokki)
            var roofArea = all.FirstOrDefault(a => a.areaName == FortRoofAreaName);
            if (roofArea != null)
            {
                float rppu = 1844f / (2f * CamHalf), rW = 5f * (5120f / rppu), sW = (5120f * 1.3f) / rppu, hatch = FortRoofX0 + rW - 200f / rppu - sW + 820f * 1.3f / rppu;
                var rl = Place(lokT, "Lokki (katto)", roofArea, hatch - 4.5f, 0.5f);
                var rb = rl.GetComponent<LokkiBrain>(); var re = rl.GetComponent<Enemy>();
                if (rb != null) { rb.finale = true; rb.reactive = true; rb.invulnerable = false; rb.landKnockdown = true; rb.finaleKnockLand = 0.8f; rb.finaleKnockLandMid = 0.85f; EditorUtility.SetDirty(rb); }
                if (re != null) { re.wakeDistance = 13f; re.isBoss = true; EditorUtility.SetDirty(re); }
                var hatchDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d != null && d.gameObject.scene == sc && d.name == "Alus: katolta cargoon");
                var rctl = new GameObject("Katon Lokki-ohjain"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rctl, sc); rctl.transform.SetParent(root.transform, true);
                var rr = rctl.AddComponent<RoofLokki>(); rr.lokki = re; rr.brain = rb; rr.hatchDoor = hatchDoor != null ? hatchDoor.gameObject : null; rr.hatchX = hatch;
                if (hatchDoor != null) { hatchDoor.gameObject.SetActive(false); EditorUtility.SetDirty(hatchDoor.gameObject); }
                EditorUtility.SetDirty(rr);
                info += "Lokki katolla (taistelu, luukku auki vasta voitettuaan), ";
            }

            // hytin kanttiini: tankkauspiste tiskille (kuvan 4 tiski ~ 25 % kuvan leveydestä)
            var vegasPath = "Assets/Scenes/Areas/Vegas.unity";
            string fuelInfo = "tankkauspistettä ei lisätty (Vegasin tiskimalli puuttuu)";
            if (File.Exists(vegasPath))
            {
                var vs = Open(vegasPath);
                var tmplShop = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == vs && c.name == "Kanttiinin tiski (Sohvi)")
                               ?? Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == vs);
                if (tmplShop != null)
                {
                    float sppu = 1844f / (2f * CamHalf), simgW = 5000f / sppu, hytX0 = hytti.walkMinX - 1.5f;
                    var fg = Object.Instantiate(tmplShop.gameObject); fg.name = "Aluksen tankkauspiste";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fg, sc); fg.transform.SetParent(root.transform, false);
                    fg.transform.position = new Vector3(hytX0 + simgW * 1.25f, hytti.maxDepthY, 0f);
                    var fs = fg.GetComponent<ShopCounter>();
                    fs.here = hytti; fs.title = "TANKKAUSPISTE"; fs.npcName = "Automaatti"; fs.prompt = "Tankkaa"; fs.halfWidth = 6f; fs.maxDistanceFromWall = 50f; fs.ownItemsOnly = false;
                    fs.items = new[]
                    {
                        new ShopCounter.Item { name = "Energiajuoma", price = 4, heal = 20, stamina = 60, comment = "Kylmää ja kuplivaa, tankkaa kestoa." },
                        new ShopCounter.Item { name = "Proteiinipatukka", price = 8, heal = 60, stamina = 0, comment = "Tiivis annos avaruusmatkalle." },
                        new ShopCounter.Item { name = "Täystankkaus", price = 20, heal = 100, stamina = 100, comment = "Kuuma ateria ja kahvi: kunto kuntoon." },
                    };
                    EditorUtility.SetDirty(fs);
                    fuelInfo = "tankkauspiste hytin kanttiinin tiskillä (x " + fg.transform.position.x.ToString("0.0") + ")";
                }
            }

            // loppukohtaus
            var ctl = new GameObject("Aluksen loppukohtaus"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ctl, sc); Undo.RegisterCreatedObjectUndo(ctl, "Aluksen loppukohtaus");
            var fin = ctl.AddComponent<ShipFinale>(); fin.cockpit = cockpit; fin.lokki = le;
            EditorUtility.SetDirty(fin);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Aluksen vihut lisätty: " + info + fuelInfo + ", " + "ShipFinale ohjaamoon (vaurio 75 %, syöksy 45 % Lokin elämistä). Mallit kopioitu Kuusta, Thaitownista ja Aavikolta.");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }
}
