using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 393: Vegasin työmaakonttori kadun jatkona työmaan oikealla puolella (alue "Vegasin katu"). Aja valikko 392 ensin (siitä saadaan työmaan oikea reuna).
/// Kaksikerroksinen konttori 10240x3688 neljänä 5120x1844-palana (katu vasen/oikea, yläosa vasen/oikea); ikkunat ja pariovien lasit läpinäkyviä (ikkunoista näkyy kadun tausta).
/// Ulkoportaat oikealla: katu -> parveke -> katto (SiteLevels: toinen komponentti, oma x-alue; portaat diagonaalisia: Ladder.useTop). Sisätilan parallaksi lisätään myöhemmin.
/// Huom: valikon 392 uudelleenajo palauttaa kadun rajat; aja sitten 393 uudelleen.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/393. Las Vegas: työmaakonttori kadun jatkoksi työmaan jälkeen (2 kerrosta, ulkoportaat katolle)", false, 393)]
    static void AddVegasOffice()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string[] files = { "vegas_konttori_etu", "vegas_konttori_katu2", "vegas_konttori_yla1", "vegas_konttori_yla2", "vegas_kanttiini_1", "vegas_kanttiini_2", "vegas_ylakerta_pax_1", "vegas_ylakerta_pax_2" };
        foreach (var f in files)
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var site = Object.FindObjectsByType<SiteLevels>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s != null && s.gameObject.scene == scene && s.gameObject.name == "Rakennustyömaa");
            if (street == null || site == null) { Info("Alue \"Vegasin katu\" tai työmaa puuttuu: aja valikot 378 ja 392."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Työmaakonttori").ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, bottom = cy - CamHalf;
            foreach (var csr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))   // kadun kuvan todellinen alareuna
                if (csr.gameObject.scene == scene && csr.gameObject.name == "Katu: hääkappeli") { bottom = csr.bounds.min.y; cy = bottom + CamHalf; break; }
            float ppuFull = 1844f / H;
            float imgW = 5120f / ppuFull, W = 1672f / (941f / H);
            float siteEnd = site.siteMaxX;
            float x0 = siteEnd, x1 = x0 + 2f * imgW;
            float XW(float px) => x0 + px / ppuFull;
            float YW(float py) => bottom + (3688f - py) / ppuFull;   // 3688 = konttorin kokonaiskorkeus (yläosa + katu)

            site.zoneMinX = site.siteMinX; site.zoneMaxX = site.siteMaxX; EditorUtility.SetDirty(site);   // työmaan komponentti toimii vain työmaalla

            var root = new GameObject("Työmaakonttori");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Työmaakonttori");
            // (tiedosto, x-puoli 0/1, rivi 0 = yläosa / 1 = katu)
            var parts = new[] { (files[0], 0, 1), (files[1], 1, 1), (files[2], 0, 0), (files[3], 1, 0) };
            int k = 0;
            foreach (var (f, cx, row) in parts)
            {
                var sp = ImportShipSprite(T + f + ".png", ppuFull, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + f); return; }
                var sr = new GameObject("Konttori " + (++k) + ": " + f).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000 + k;
                sr.transform.position = new Vector3(x0 + imgW * (cx + 0.5f), cy + (row == 0 ? H : 0f), 0f);
            }

            // sisätilat ikkunoiden takana (parallaksi: liikkuvat hieman kameran suuntaan); alakerta = kanttiini, yläkerta = Nokkamiehen toimisto.
            // Kuvat on rajattu rakennuksen leveyteen: pariovet, portaat ja rappukäytävä jäävät julkisivun seinän taakse, ei paradoksia.
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 2; i++)
                {
                    string f = files[4 + row * 2 + i];
                    var isp = ImportShipSprite(T + f + ".png", ppuFull, false, SpriteAlignment.Center);
                    if (isp == null) { Info("Kuvan tuonti epäonnistui: " + f); return; }
                    var isr = new GameObject((row == 0 ? "Konttori: kanttiini " : "Konttori: yläkerran toimisto ") + (i + 1)).AddComponent<SpriteRenderer>();
                    isr.transform.SetParent(root.transform, false); isr.sprite = isp; isr.sortingOrder = -10020;   // palmujen (-10040) edessä, julkisivun (-10000) takana
                    isr.transform.position = new Vector3(x0 + imgW * (i + 0.5f), row == 0 ? cy : YW(2029f), 0f);   // yläkerran toimiston lattiaviiva (953 px) = kerrosten välilaatan päälle (~2060 px kokonaiskuvassa)
                    isr.gameObject.AddComponent<ParallaxInterior>();
                }

            // kerrokset: katu, parveke (portaiden välitaso), katto
            float streetMax = site.streetMax, streetMin = site.streetMin;
            float[] levelY = { streetMax, YW(1995f + 25f), YW(920f + 25f) };
            float topY = levelY[2] + 0.4f;
            float Cam(float feetY) => feetY + 0.19f * H - cy;
            var sl = root.AddComponent<SiteLevels>();
            sl.area = street; sl.levelY = levelY; sl.streetMin = streetMin; sl.streetMax = streetMax; sl.bandHalf = 0.28f;
            sl.levelCamOffset = new[] { 0f, Cam(levelY[1]), Cam(levelY[2]) };
            sl.zoneMinX = x0; sl.zoneMaxX = x1; sl.siteMinX = x0; sl.siteMaxX = x1;
            sl.levelMinX = new[] { 0f, XW(8600f), XW(1250f) }; sl.levelMaxX = new[] { 0f, XW(9150f), XW(7600f) };
            sl.climbSpeed = 4.5f;
            sl.ladders = new[]
            {
                new SiteLevels.Ladder { x = XW(7470f), topX = XW(8620f), useTop = true, lo = 0, hi = 1 },   // 1. portaikko: katu -> parveke
                new SiteLevels.Ladder { x = XW(8620f), topX = XW(7560f), useTop = true, lo = 1, hi = 2 },   // 2. portaikko: parveke -> katto
            };

            // katu jatkuu: kävelyalue, kamera, syvyysrajat, taustatasot
            street.walkMaxX = x1 - 1.5f;
            street.camMaxX = Mathf.Max(street.camMinX, x1 - W * 0.5f);
            var dl = street.depthLimits.Where(v => v.x <= siteEnd + 0.01f).ToList();
            dl.Add(new Vector2(siteEnd + 2.5f, topY)); dl.Add(new Vector2(x1, topY));
            street.depthLimits = dl.ToArray();
            street.depthLimitsExceedMax = true;
            EditorUtility.SetDirty(street);
            foreach (var pl in Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pl.gameObject.scene != scene) continue;
                if (pl.name.StartsWith("Katu: ") || pl.name == "Työmaa: korkea taivas") { pl.maxX = street.camMaxX + 2f; EditorUtility.SetDirty(pl); }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Työmaakonttori lisätty kadun jatkoksi (x " + x0.ToString("0") + "–" + x1.ToString("0") + "): kaksikerroksinen julkisivu läpinäkyvine ikkunoineen, ulkoportaat katu -> parveke -> katto (ylös/alas-napilla portaiden kohdalla).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
