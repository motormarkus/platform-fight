using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan katto (valikko 372): kaksi kuvaa (Assets/Sprites/Taustat/vankila_katto_1.png vasen: hissi, valonheittimet, ilmastointi; vankila_katto_2.png oikea: helikopterikenttä),
/// 6 vartijaa (valikon 363 jakoon), kopteri helikopterikentällä (sama roottorisilmukka kuin Thaitownin katolla). Hissi: henkilökunnan alakerran hissin ovi ("Vankila: hissin avain",
/// jonka Warden pudottaa kaatuessaan) vie katolle ja katon hissi takaisin. Kopterin kyytiin nousu / seuraava kenttä: päätetään myöhemmin.</summary>
public static partial class BeatEmUpSetup
{
    const float PrisonRoofX0 = 96000f;

    [MenuItem("Beat em up/372. Vankila: katto (hissi, 6 vartijaa, kopteri helikopterikentällä)", false, 372)]
    static void AddPrisonRoof()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_katto_1", "vankila_katto_2", "kone_taivas", "vankila_piha_taka" })
        {
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
            AssetDatabase.ImportAsset(T + f + ".png", ImportAssetOptions.ForceUpdate);
        }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–362."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var staff = areas.FirstOrDefault(a => a.areaName == "Vankilan henkilökunta");
            if (staff == null) { Info("Henkilökunnan tiloja ei löytynyt: aja valikko 362."); return; }
            var elev = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Henkilökunta: hissi");
            if (elev == null) { Info("Hissin ovea ei löytynyt (\"Henkilökunta: hissi\"): aja valikko 362."); return; }
            foreach (var n in new[] { "Vankilan katto", "Alue: Vankilan katto" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Katto:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonRoofX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;    // kuvat 5120 x 1844
            float totalW = 2f * imgW;
            var root = new GameObject("Vankilan katto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan katto");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_katto_" + (i + 1) + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (katto " + (i + 1) + ")."); return; }
                var sr = new GameObject("Katto " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }
            var roof = ReuseArea("Alue: Vankilan katto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(roof.gameObject, scene);
            roof.areaName = "Vankilan katto"; roof.useSidewalk = false;
            roof.maxDepthY = top - 0.745f * H; roof.minDepthY = top - 0.985f * H;   // lattia alkaa kaiteen alta (~0,72 kuvan korkeudesta)
            roof.walkMinX = X0 + imgW * 0.10f; roof.walkMaxX = X0 + imgW * 1.86f;    // oikealla kulma: kaide kääntyy
            roof.camMinX = X0 + W * 0.5f; roof.camMaxX = Mathf.Max(roof.camMinX, X0 + totalW - W * 0.5f);
            roof.camSize = CamHalf; roof.camRiseY = 0f; roof.camOffsetY = 0f; roof.backScale = 1f; roof.depthLimits = new Vector2[0];
            roof.music = staff.music; roof.musicGain = staff.musicGain;

            // taivas parallaksina kuvan takana (sama kone_taivas-parallaksi kuin vankilan pihoilla; kuvien taivas on läpinäkyvä)
            {
                float ppu = 922f / H;
                var sp = ImportShipSprite(T + "kone_taivas.png", ppu, false, SpriteAlignment.Center);
                if (sp != null)
                {
                    float scale = 1.25f, left = roof.camMinX - W * 0.5f;
                    var sr = new GameObject("Katon taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + sp.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = roof.camMinX; pl.minX = roof.camMinX - 2f; pl.maxX = roof.camMaxX + 2f;
                }
                // sen edessä vankilan piha-kuva (sama kerros kuin etupihalla: vankila_piha_taka, kerroin 0,3, skaala 1)
                var sp2 = ImportShipSprite(T + "vankila_piha_taka.png", ppu, false, SpriteAlignment.Center);
                if (sp2 != null)
                {
                    float left2 = roof.camMinX - W * 0.5f;
                    var sr2 = new GameObject("Katon vankilan piha").AddComponent<SpriteRenderer>();
                    sr2.transform.SetParent(root.transform, false); sr2.sprite = sp2; sr2.sortingOrder = -10050;
                    sr2.transform.position = new Vector3(left2 + sp2.bounds.size.x * 0.5f, cy, 0f);
                    var pl2 = sr2.gameObject.AddComponent<ParallaxLayer>();
                    pl2.factor = 0.3f; pl2.startCamX = roof.camMinX; pl2.minX = roof.camMinX - 2f; pl2.maxX = roof.camMaxX + 2f;
                }
            }

            // kopteri helikopterikentällä (kuvan 2 H-merkki ~ 70 % kuvan leveydestä); sama roottorisilmukka kuin Thaitownin katolla
            {
                const string dir = "Assets/Sprites/Rekvisiitta/KatonKopteri/";
                var frames = new List<Sprite>();
                for (int i = 0; ; i++)
                {
                    string fp = $"{dir}katon_kopteri_{i:00}.png";
                    if (!File.Exists(fp)) break;
                    var fs = ImportShipSprite(fp, 720f / RoofHeliWidth, false, SpriteAlignment.BottomCenter);
                    if (fs != null) frames.Add(fs);
                }
                if (frames.Count > 0)
                {
                    float hx = X0 + imgW * 1.695f, hy = Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.55f);
                    var heli = new GameObject("Vankilan katon kopteri");
                    heli.transform.SetParent(root.transform, false);
                    heli.transform.position = new Vector3(hx, hy, 0f);
                    heli.transform.localScale = new Vector3(1.8f, 1.8f, 1f);   // isompi (oli 1,3): peittää helikopterikentän H-ympyrän
                    var hull = new GameObject("Runko").AddComponent<SpriteRenderer>();
                    hull.transform.SetParent(heli.transform, false);
                    hull.sprite = frames[0]; hull.sortingOrder = Mathf.RoundToInt(-hy * 100f);
                    var loop = hull.gameObject.AddComponent<RotorLoop>();
                    loop.frames = frames.ToArray(); loop.fps = 24f; loop.reverse = false;
                }
            }

            // ovet: henkilökunnan alakerran hissi (vaatii Wardenin pudottaman avaimen) <-> katon hissi
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            float elevX = X0 + imgW * 0.1575f, elevY = roof.maxDepthY - 0.3f;
            Vector2 downSpawn = new Vector2(elev.transform.position.x, elev.transform.position.y - 1.3f);
            var up = elev;
            Undo.RecordObject(up, "Hissi katolle");
            up.target = roof; up.prompt = "Nouse katolle"; up.requiredClue = "Vankila: hissin avain"; up.lockedClueText = "Hissi ei toimi. Wardenilla on avain.";
            up.spawnPoint = new Vector2(elevX + 2.6f, Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.45f));
            EditorUtility.SetDirty(up);
            var back = new GameObject("Katto: hissi alas").AddComponent<Door>();
            back.transform.SetParent(doorRoot.transform, false); back.transform.position = new Vector3(elevX, elevY, 0f);
            back.prompt = "Mene hissillä alas"; back.here = roof; back.target = staff; back.spawnPoint = downSpawn;
            back.halfWidth = 1.6f; back.maxDistanceFromWall = 2.4f; back.onGround = true; back.groundHalfDepth = 1.6f;

            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan katto")) names.Add("Vankilan katto");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(roof);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AddPrisonGuards();   // lisää myös katon 6 vartijaa (suunnitelmassa "Vankilan katto")
            Info("Vankilan katto rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): henkilökunnan hissi vie katolle (avain \"Vankila: hissin avain\" tulee Wardenilta), katon hissi takaisin. Katolla 6 vartijaa ja kopteri helikopterikentällä.\nLattiarajat (0,745–0,985) ja kopterin paikka/koko ovat arvauksia: säädä Alue: Vankilan katto ja Vankilan katon kopteri. Kopterin kyytiin nousu ja seuraava kenttä puuttuvat vielä. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
