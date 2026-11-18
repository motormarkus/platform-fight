using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 410: showbaariin (406) ruletti-pöytä lavan vasemmalle puolelle (oma kuva, este + E avaa ruletin) ja Sohvi tiskin taakse myymään samoja tuotteita kuin kasinon baarissa.
/// Sohvi ja tiski kopioidaan kasinon baarista (valikko 376: "Sohvi (kasinon baari)"). Tiskin etuosa piirretään Sohvin päälle (kuvan oma tiski-kaistale), jotta Sohvi seisoo tiskin takana.
/// Vaatii 376 ja 406. Ajo uudelleen turvallinen.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/410. Las Vegas: showbaarin ruletti-pöytä ja Sohvi", false, 410)]
    static void AddShowbarExtras()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/", P = "Assets/Sprites/Props/";
        foreach (var f in new[] { T + "vegas_showbaari_tiski_etu.png", P + "ruletti_poyta.png" })
            if (!File.Exists(f)) { Info("Puuttuu " + f); return; }
        AssetDatabase.ImportAsset(T + "vegas_showbaari_tiski_etu.png", ImportAssetOptions.ForceUpdate); AssetDatabase.ImportAsset(P + "ruletti_poyta.png", ImportAssetOptions.ForceUpdate);
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var show = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin showbaari");
            if (show == null) { Info("Alue \"Vegasin showbaari\" puuttuu: aja valikko 406."); return; }
            foreach (var n in new[] { "Ruletti pöytä (showbaari)", "Sohvi (showbaari)", "Showbaarin tiski (Sohvi)", "Showbaarin tiskin etuosa" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;
            string info = "";

            // ---- ruletti-pöytä lavan vasemmalle (kuvan 1 oikea puoli, seinäpaneelien edessä) ----
            {
                var sp = ImportShipSprite(P + "ruletti_poyta.png", 1641f / 5.6f, false, SpriteAlignment.BottomCenter);   // pöydän leveys 5,6 yksikköä
                if (sp == null) { Info("Pöytäkuvan tuonti epäonnistui."); return; }
                float x = VegasShowbarX0 + imgW * 0.60f, y = Mathf.Lerp(show.maxDepthY, show.minDepthY, 0.55f);
                var go = new GameObject("Ruletti pöytä (showbaari)");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene); Undo.RegisterCreatedObjectUndo(go, "Ruletti");
                go.transform.position = new Vector3(x, y, 0f);
                var body = new GameObject("Kuva").AddComponent<SpriteRenderer>(); body.transform.SetParent(go.transform, false); body.sprite = sp;
                body.transform.localPosition = new Vector3(0f, -0.2f, 0f);   // jalat hieman etureunan alapuolelle
                var ob = go.AddComponent<Obstacle>(); ob.body = body; ob.halfWidth = 2.6f; ob.depth = 0.9f;
                var entry = go.AddComponent<RouletteTableEntry>(); entry.here = show; entry.rangeX = 4.2f; entry.rangeY = 2.6f;
                info += "Ruletti-pöytä x " + x.ToString("0.0") + ". ";
            }

            // ---- Sohvi + tiski ----
            {
                var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.gameObject.scene == scene).ToArray();
                var sohviSrc = all.FirstOrDefault(t => t.name == "Sohvi (kasinon baari)");
                var counterSrc = all.FirstOrDefault(t => t.name == "Kasinon baarin tiski (Sohvi)");
                if (sohviSrc == null || counterSrc == null) info += "Sohvia ei lisätty: aja valikko 376 (kasinon baarin Sohvi on mallina).";
                else
                {
                    float hx = VegasShowbarX0 + imgW * 2.52f, hy = top - 0.66f * H;
                    var sg = Object.Instantiate(sohviSrc.gameObject); sg.name = "Sohvi (showbaari)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene);
                    var scl = sohviSrc.lossyScale; sg.transform.SetParent(null, true); sg.transform.localScale = scl;
                    sg.transform.position = new Vector3(hx, hy, 0f);
                    foreach (var r in sg.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = -9995;   // taustakuvan (-10000) edessä, tiskin etuosan (-9990) takana
                    var shopGo = Object.Instantiate(counterSrc.gameObject); shopGo.name = "Showbaarin tiski (Sohvi)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(null, true);
                    shopGo.transform.position = new Vector3(hx, show.maxDepthY, 0f);
                    var sc = shopGo.GetComponent<ShopCounter>();
                    if (sc != null) { sc.here = show; sc.title = "SHOWBAARI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville"; }
                    // tiskin etuosa Sohvin päälle: sama kuvan 3 sprite täsmälleen samassa paikassa, näkyvissä vain maskin (tiskin kaistale) sisällä -> ei kohdistusvirhettä
                    Transform bg3 = all.FirstOrDefault(t => t.name == "Kuva 3" && t.parent != null && t.parent.name == "Vegasin showbaari");
                    var bgSr = bg3 != null ? bg3.GetComponent<SpriteRenderer>() : null;
                    const string MP = "Assets/Sprites/Props/maski_valkoinen.png";
                    if (!File.Exists(MP)) { var wt = new Texture2D(8, 8, TextureFormat.RGBA32, false); var px = new Color32[64]; for (int i = 0; i < 64; i++) px[i] = new Color32(255, 255, 255, 255); wt.SetPixels32(px); File.WriteAllBytes(MP, wt.EncodeToPNG()); Object.DestroyImmediate(wt); AssetDatabase.ImportAsset(MP, ImportAssetOptions.ForceUpdate); }
                    var maskSp = ImportShipSprite(MP, 8f, false, SpriteAlignment.Center);
                    if (bgSr == null || maskSp == null) info += "(tiskin etuosaa ei tehty: Kuva 3 tai maski puuttuu) ";
                    else
                    {
                        var ov = new GameObject("Showbaarin tiskin etuosa").AddComponent<SpriteRenderer>();
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ov.gameObject, scene);
                        ov.sprite = bgSr.sprite; ov.sortingOrder = -9990; ov.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                        ov.transform.position = bgSr.transform.position; ov.transform.localScale = bgSr.transform.lossyScale;
                        var mk = new GameObject("Maski").AddComponent<SpriteMask>(); mk.transform.SetParent(ov.transform, false); mk.sprite = maskSp;
                        float k = bgSr.transform.lossyScale.x;
                        // kaistale kuvan pikseleinä: x 0-4300, y 930-1320 (kuva 5120 x 1844, keskipiste 2560, 922)
                        mk.transform.localPosition = new Vector3((2150f - 2560f) / ppuImg, (922f - 1125f) / ppuImg, 0f);
                        mk.transform.localScale = new Vector3(4300f / ppuImg, 390f / ppuImg, 1f);
                    }
                    info += "Sohvi x " + hx.ToString("0.0") + " tiskin takana.";
                }
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Showbaari: " + info);
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
