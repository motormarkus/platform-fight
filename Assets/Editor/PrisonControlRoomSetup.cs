using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan valvontahuone / vartijahuone (valikko 375): kaksi kuvaa (Assets/Sprites/Taustat/vankila_valvonta_1.png vasen: avainkaappi, työpöydät; vankila_valvonta_2.png oikea: valvontanäytöt, sohva).
/// Läpikuljettava: terminaalin oikean pään kaksoisovi -> huone -> oikea ovi D-osastolle (terminaalin sivuovi ei muutu). Sisällä 3 vartijaa.
/// Avainkaapista (E) saa sellin E13 avaimen ("Vankila: sellin E13 avain"): sen jälkeen Rexin voi vapauttaa. E13:n ovi lukitaan tässä.</summary>
public static partial class BeatEmUpSetup
{
    const float PrisonControlX0 = 97000f;

    [MenuItem("Beat em up/375. Vankila: valvontahuone (avainkaappi, sellin E13 avain, 3 vartijaa)", false, 375)]
    static void AddPrisonControlRoom()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_valvonta_1", "vankila_valvonta_2", "kone_taivas", "vankila_piha_taka" })
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
            var staff = areas.FirstOrDefault(a => a.areaName == "Vankilan henkilökunta");   // vain musiikkia varten
            var term = areas.FirstOrDefault(a => a.areaName == "Vankilan terminaali");
            var dHall = areas.FirstOrDefault(a => a.areaName == "Vankilan D-osasto");
            if (term == null) { Info("Terminaalia ei löytynyt: aja valikko 354."); return; }
            if (dHall == null) { Info("D-osastoa ei löytynyt: aja valikko 355."); return; }
            var allDoors = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene).ToArray();
            var bigT = allDoors.FirstOrDefault(x => x.name == "Terminaali: kaksoisovi");
            if (bigT == null) { Info("Terminaalin kaksoisovea ei löytynyt (\"Terminaali: kaksoisovi\"): aja valikko 354."); return; }
            foreach (var n in new[] { "Vankilan valvontahuone", "Alue: Vankilan valvontahuone", "Vankilan vihjepaikat" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in allDoors.Where(x => x.name.StartsWith("Valvonta:")).ToArray()) Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonControlX0;
            float W = 1672f / (941f / H);
            float ppuImg = 1844f / H, imgW = 5120f / ppuImg;    // kuvat 5120 x 1844
            float totalW = 2f * imgW;
            var root = new GameObject("Vankilan valvontahuone");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankilan valvontahuone");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_valvonta_" + (i + 1) + ".png", ppuImg, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (valvonta " + (i + 1) + ")."); return; }
                var sr = new GameObject("Valvontahuone " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.localScale = new Vector3(1.0018f, 1f, 1f);   // ~1 px limitys: kuvien väliin ei jää rakoa
                sr.transform.position = new Vector3(X0 + imgW * (i + 0.5f), cy, 0f);
            }
            var room = ReuseArea("Alue: Vankilan valvontahuone");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            room.areaName = "Vankilan valvontahuone"; room.useSidewalk = false;
            room.maxDepthY = top - 0.745f * H; room.minDepthY = top - 0.985f * H;   // lattia alkaa kalusteiden alta (~0,72 kuvan korkeudesta)
            room.walkMinX = X0 + 1.5f; room.walkMaxX = X0 + totalW - 1.5f;
            room.camMinX = X0 + W * 0.5f; room.camMaxX = Mathf.Max(room.camMinX, X0 + totalW - W * 0.5f);
            room.camSize = CamHalf; room.camRiseY = 0f; room.camOffsetY = 0f; room.backScale = 1f; room.depthLimits = new Vector2[0];
            var mus = staff != null ? staff : term; room.music = mus.music; room.musicGain = mus.musicGain;

            // oikean kuvan ikkunan raot ovat läpinäkyvät: taivas + vankilan piha parallaksina kuvan takana
            {
                float pp = 922f / H;
                var psk = ImportShipSprite(T + "kone_taivas.png", pp, false, SpriteAlignment.Center);
                if (psk != null)
                {
                    float scale = 1.25f, left = room.camMinX - W * 0.5f;
                    var sr = new GameObject("Valvontahuoneen taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = psk; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + psk.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = room.camMinX; pl.minX = room.camMinX - 2f; pl.maxX = room.camMaxX + 2f;
                }
                var ppy = ImportShipSprite(T + "vankila_piha_taka.png", pp, false, SpriteAlignment.Center);
                if (ppy != null)
                {
                    float left2 = room.camMinX - W * 0.5f;
                    var sr2 = new GameObject("Valvontahuoneen vankilan piha").AddComponent<SpriteRenderer>();
                    sr2.transform.SetParent(root.transform, false); sr2.sprite = ppy; sr2.sortingOrder = -10050;
                    sr2.transform.position = new Vector3(left2 + ppy.bounds.size.x * 0.5f, cy, 0f);
                    var pl2 = sr2.gameObject.AddComponent<ParallaxLayer>();
                    pl2.factor = 0.3f; pl2.startCamX = room.camMinX; pl2.minX = room.camMinX - 2f; pl2.maxX = room.camMaxX + 2f;
                }
            }

            // ovet: henkilökunnan yläkerran ovi <-> huoneen vasen kaksoisovi; oikea kaksoisovi lukossa
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            float inX = X0 + imgW * 0.066f, outX = X0 + imgW * 1.93f;
            float midRoom = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.45f);
            Undo.RecordObject(bigT, "Terminaalin kaksoisovi");
            bigT.target = room; bigT.prompt = "Mene valvontahuoneeseen"; bigT.requiredClue = ""; bigT.lockedClueText = "";   // terminaalin oikea pääty vie nyt valvontahuoneeseen (ennen D-osastolle)
            bigT.spawnPoint = new Vector2(inX + 3.2f, midRoom);
            EditorUtility.SetDirty(bigT);
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 2.4f;
                d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            MakeDoor("Valvonta: takaisin henkilökuntaan", "Takaisin terminaaliin", room, term, new Vector3(inX, room.maxDepthY - 0.3f, 0f), new Vector2(bigT.transform.position.x - 3.2f, Mathf.Lerp(term.maxDepthY, term.minDepthY, 0.45f)));
            // oikea ovi vie D-osaston vasempaan kaksoisoveen (huone on läpikuljettava: terminaali -> valvontahuone -> D-osasto); terminaalin oma kaksoisovi lukitaan
            var dBack = allDoors.FirstOrDefault(x => x.name == "D-osasto: terminaaliin");
            if (dBack == null) Info("D-osaston vasenta ovea (\"D-osasto: terminaaliin\") ei löytynyt: aja valikko 355; oikea ovi ei johda D-osastolle.");
            else
            {
                float dMid = Mathf.Lerp(dHall.maxDepthY, dHall.minDepthY, 0.5f);
                var outDoor = MakeDoor("Valvonta: oikea ovi", "Mene D-osastolle", room, dHall, new Vector3(outX, room.maxDepthY - 0.3f, 0f), new Vector2(dBack.transform.position.x + 3.0f, dMid));
                Undo.RecordObject(dBack, "D-osaston vasen ovi");
                dBack.target = room; dBack.prompt = "Takaisin valvontahuoneeseen"; dBack.spawnPoint = new Vector2(outX - 3.2f, midRoom);
                EditorUtility.SetDirty(dBack);
            }

            // avainkaappi (kuvan 1 seinäkaappi ~ 20,6 % kuvan leveydestä): sellin E13 avain
            var spots = new GameObject("Vankilan vihjepaikat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(spots, scene);
            Undo.RegisterCreatedObjectUndo(spots, "Vankilan vihjepaikat");
            {
                var g = new GameObject("Avainkaappi");
                g.transform.SetParent(spots.transform, false);
                g.transform.position = new Vector3(X0 + imgW * 0.206f, Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.2f), 0f);
                var cs = g.AddComponent<ClueSpot>();
                cs.prompt = "Tutki avainkaappia"; cs.clueId = "Vankila: sellin E13 avain"; cs.clueTitle = "Sellin E13 avain";
                cs.clueText = "Valvontahuoneen avainkaapista löytyi sellin E13 avain. Rex on E-osaston yläkerran sellissä E13.";
                cs.emptyText = "Muut avaimet eivät sovi tähän tehtävään."; cs.halfWidth = 2.4f; cs.halfDepth = 2.5f;
                cs.requiredClue = "Vankila: avainkaapin avain"; cs.lockedText = "Avainkaappi on lukossa. Valvontahuoneen viimeisellä vartijalla on avain.";
                var lgk = spots.AddComponent<LastGuardKey>();   // viimeinen vartija pudottaa avaimen kaapin lukkoon
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan valvontahuone")) names.Add("Vankilan valvontahuone");
            File.WriteAllLines(listPath, names);

            // E13:n ovi lukkoon (avain valvontahuoneen kaapista)
            var e13 = allDoors.FirstOrDefault(x => x.name == "E-osasto: yläselli E13");
            if (e13 != null) { Undo.RecordObject(e13, "E13 lukko"); e13.requiredClue = "Vankila: sellin E13 avain"; e13.lockedClueText = "Sellin ovi on lukossa. Avain on valvontahuoneen kaapissa."; EditorUtility.SetDirty(e13); }

            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AddPrisonGuards();   // lisää myös valvontahuoneen 3 vartijaa (suunnitelmassa "Vankilan valvontahuone")
            Info("Valvontahuone rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + "): terminaalin oikean pään kaksoisovi vie huoneeseen; avainkaapista (E) saa sellin E13 avaimen" + (e13 != null ? " (E13 lukittu)" : " (E13:n ovea ei löytynyt: aja 358)") + ". Sisällä 3 vartijaa; oikea ovi vie D-osastolle.\nKirjaston laatikon paikka ja huoneen lattiarajat ovat arvauksia. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
