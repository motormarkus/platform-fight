using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 430: motellin jälkeen tyhjä tienpätkä ja WTF-areenan julkisivu (3 ruutua) Vegasin kadun jatkoksi. Aja ensin 403 (motelli).
/// Kuvat: Tools/ArenaAssets/make_arena_front.py -> vegas_areena_1..3.png (5120x1844, katu sama kuin vegas_tie_patka).
/// Kadun taustakerroksille lisätään peilikopioita, jotta ne liikkuvat koko kadun matkan (eivät pysähdy).
/// Areenan ovet (sisäänkäynnin kolme lasiovea) ovat toistaiseksi vain kuvaa: sisätila ja ottelu tulevat myöhemmin.
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/430. Las Vegas: tienpätkä + WTF-areenan julkisivu motellin jälkeen", false, 430)]
    static void AddVegasArenaFront()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string[] files = { "vegas_tie_patka", "vegas_areena_1", "vegas_areena_2", "vegas_areena_3" };
        foreach (var f in files)
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja Tools/ArenaAssets/make_arena_front.py."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var site = Object.FindObjectsByType<SiteLevels>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(s => s != null && s.gameObject.scene == scene && s.gameObject.name == "Rakennustyömaa");
            var motelRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin motelli");
            var motel = motelRoot != null ? motelRoot.GetComponent<SiteLevels>() : null;
            if (street == null || site == null || motel == null) { Info("Alue \"Vegasin katu\", työmaa tai motelli puuttuu: aja valikot 378, 392, 393 ja 403."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "WTF-areena").ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, bottom = cy - CamHalf;
            foreach (var csr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))   // kadun kuvan todellinen alareuna (kuten motellissa)
                if (csr.gameObject.scene == scene && csr.gameObject.name == "Katu: hääkappeli") { bottom = csr.bounds.min.y; cy = bottom + CamHalf; break; }
            float ppuFull = 1844f / H;
            float imgW = 5120f / ppuFull, W = 1672f / (941f / H);
            float motelEnd = motel.zoneMaxX;                  // motellin oikea reuna = tienpätkän alku
            float ax0 = motelEnd + imgW, ax1 = ax0 + 3f * imgW;   // areena: tienpätkän jälkeen

            var root = new GameObject("WTF-areena");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "WTF-areena");
            var parts = new[] { (files[0], -1, "Tienpätkä 2"), (files[1], 0, "Areena 1 vasen"), (files[2], 1, "Areena 2 sisäänkäynti"), (files[3], 2, "Areena 3 oikea") };
            int k = 0;
            foreach (var (f, cx, nm) in parts)
            {
                var sp = ImportShipSprite(T + f + ".png", ppuFull, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: " + f); return; }
                var sr = new GameObject(nm + ": " + f).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000 + (++k);
                sr.transform.position = new Vector3(ax0 + imgW * (cx + 0.5f), cy, 0f);
            }

            // katu jatkuu: kävelyalue, kamera, syvyysraja (kadun taso), taustatasot
            street.walkMaxX = ax1 - 1.5f;
            street.camMaxX = Mathf.Max(street.camMinX, ax1 - W * 0.5f);
            float baseLimit = street.MaxDepthAtX(site.siteMaxX, street.maxDepthY);
            var dl = street.depthLimits.Where(v => v.x <= motelEnd + 0.01f).ToList();
            dl.Add(new Vector2(motelEnd + 2.5f, baseLimit));   // motellin parvekekorkeus päättyy
            dl.Add(new Vector2(ax1, baseLimit));
            street.depthLimits = dl.ToArray();
            street.depthLimitsExceedMax = true;
            EditorUtility.SetDirty(street);
            foreach (var pl in Object.FindObjectsByType<ParallaxLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (pl.gameObject.scene != scene) continue;
                if (!(pl.name.StartsWith("Katu: ") || pl.name == "Työmaa: korkea taivas")) continue;
                pl.maxX = street.camMaxX + 2f;
                // peilikopioita niin monta, ettei kerros pysähdy ennen kadun päätä (kerros jumittuu ruutuun, kun kuva loppuu)
                var psr = pl.GetComponent<SpriteRenderer>();
                if (psr != null && psr.sprite != null)
                {
                    float tileW = psr.sprite.bounds.size.x * Mathf.Abs(pl.transform.lossyScale.x);
                    float need = W + pl.factor * (street.camMaxX + 2f - Mathf.Min(pl.startCamX, street.camMinX)) + 2f;
                    float extraW = 0f;
                    if (pl.extraSprites != null) foreach (var es in pl.extraSprites) if (es != null) extraW += es.bounds.size.x * Mathf.Abs(pl.transform.lossyScale.x);
                    int copies = tileW > 0.01f ? Mathf.CeilToInt((need - extraW) / tileW) - 1 : 0;
                    if (copies > pl.mirrorCopies) pl.mirrorCopies = copies;
                }
                EditorUtility.SetDirty(pl);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Tienpätkä (x " + motelEnd.ToString("0") + "–" + ax0.ToString("0") + ") ja WTF-areenan julkisivu (x " + ax0.ToString("0") + "–" + ax1.ToString("0") + ") lisätty motellin jälkeen. Kävelyalue päättyy x " + street.walkMaxX.ToString("0") + ". Areenan ovet ovat vielä vain kuvaa. Kadun taustat (palmut, hotellit, taivas) jatkuvat peilikopioina kadun päähän asti.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// <summary>Areenan julkisivu: 430 + saumat (401).</summary>
    public static void RunVegasArenaBatch()
    {
        batch = true;
        try
        {
            Step("430 areenan julkisivu", AddVegasArenaFront);
            Step("401 saumat", FixVegasSeams);
        }
        finally { batch = false; }
    }
}
