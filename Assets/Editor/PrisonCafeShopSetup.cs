using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 414: vankilan ruokalaan Sohvi tiskin taakse myymään (tankkauspiste: samat tuotteet kuin kirjaston kanttiinissa, myös Rexin elämät täyttyvät).
/// Sohvi ja tiski kopioidaan kirjaston kanttiinin Sohvista ("Sohvi (vankilan kanttiini)" ja "Vankilan kanttiinin tiski (Sohvi)", valikko 359/vankilan kirjasto). Tiskin etuosa piirretään Sohvin päälle:
/// sama taustakuva maskilla (kaistale), jolloin jalat jäävät tiskin taakse. Ajo uudelleen turvallinen.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/414. Vankila: ruokalaan Sohvi ja kauppa (tankkauspiste)", false, 414)]
    static void AddPrisonCafeShop()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Vankila-scene puuttuu."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var cafe = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vankilan ruokala");
            if (cafe == null) { Info("Alue \"Vankilan ruokala\" puuttuu: aja ruokalan valikko."); return; }
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene).ToArray();
            var sohviSrc = all.FirstOrDefault(t => t.name == "Sohvi (vankilan kanttiini)"); var counterSrc = all.FirstOrDefault(t => t.name == "Vankilan kanttiinin tiski (Sohvi)");
            if (sohviSrc == null || counterSrc == null) { Info("Kirjaston kanttiinin Sohvi ja tiski puuttuvat (mallit): aja vankilan kirjaston valikko ensin."); return; }
            foreach (var n in new[] { "Sohvi (vankilan ruokala)", "Vankilan ruokalan tiski (Sohvi)", "Ruokalan tiskin etuosa 1", "Ruokalan tiskin etuosa 2" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, X0 = PrisonCafeX0, ppu = CafeImgH / H;
            float hx = X0 + 2411f / ppu, hy = cy + (CafeImgH * 0.5f - 465f) / ppu;   // jalat 465 px (tiskin etuosan alareuna ~523 px): tiski peittää jalat
            var sg = Object.Instantiate(sohviSrc.gameObject); sg.name = "Sohvi (vankilan ruokala)";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); var scl = sohviSrc.lossyScale; sg.transform.SetParent(null, true); sg.transform.localScale = scl;
            sg.transform.position = new Vector3(hx, hy, 0f);
            foreach (var r in sg.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = -9995;
            var shopGo = Object.Instantiate(counterSrc.gameObject); shopGo.name = "Vankilan ruokalan tiski (Sohvi)";
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(null, true);
            shopGo.transform.position = new Vector3(hx, cafe.maxDepthY, 0f);
            var sc = shopGo.GetComponent<ShopCounter>();
            if (sc != null) { sc.here = cafe; sc.title = "VANKILAN RUOKALA"; sc.refillAllies = true; sc.halfWidth = Mathf.Max(sc.halfWidth, 4.5f); }

            // tiskin etuosa: sama taustakuva, näkyvissä vain tiskin kaistalla (y 340-530 px; kuva 1: x 1215-2560, kuva 2: x 0-1640)
            const string MP = "Assets/Sprites/Props/maski_valkoinen.png";
            if (!File.Exists(MP)) { var wt = new Texture2D(8, 8, TextureFormat.RGBA32, false); var px = new Color32[64]; for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255); wt.SetPixels32(px); File.WriteAllBytes(MP, wt.EncodeToPNG()); Object.DestroyImmediate(wt); AssetDatabase.ImportAsset(MP, ImportAssetOptions.ForceUpdate); }
            var maskSp = ImportShipSprite(MP, 8f, false, SpriteAlignment.Center);
            string info = "";
            for (int i = 0; i < 2; i++)
            {
                var bg = all.FirstOrDefault(t => t.name == "Ruokala " + (i + 1) && t.parent != null && t.parent.name == "Vankilan ruokala");
                var bgSr = bg != null ? bg.GetComponent<SpriteRenderer>() : null;
                if (bgSr == null || maskSp == null) { info += "(etuosa " + (i + 1) + " puuttuu) "; continue; }
                var ov = new GameObject("Ruokalan tiskin etuosa " + (i + 1)).AddComponent<SpriteRenderer>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ov.gameObject, scene);
                ov.sprite = bgSr.sprite; ov.sortingOrder = -9990; ov.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                ov.transform.position = bgSr.transform.position; ov.transform.localScale = bgSr.transform.lossyScale;
                float x0 = i == 0 ? 1215f : 0f, x1 = i == 0 ? CafeImgW : 1640f, cxPx = (x0 + x1) * 0.5f, wPx = x1 - x0, y0 = 340f, y1 = 530f;
                var mk = new GameObject("Maski").AddComponent<SpriteMask>(); mk.transform.SetParent(ov.transform, false); mk.sprite = maskSp;
                mk.transform.localPosition = new Vector3((cxPx - CafeImgW * 0.5f) / ppu, (CafeImgH * 0.5f - (y0 + y1) * 0.5f) / ppu, 0f);
                mk.transform.localScale = new Vector3(wPx / ppu, (y1 - y0) / ppu, 1f);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Ruokalan Sohvi tiskin takana (x " + hx.ToString("0.0") + "), kauppa VANKILAN RUOKALA (täyttää myös Rexin). " + info);
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
