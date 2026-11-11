using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Vankilan sisäpiha (valikko 353): oma scene Assets/Scenes/Areas/Vankila.unity, alue "Vankila", neljä ruutua: peilattu portti (vasen pää), kaksi pätkää tietä, terminaalin suu-aukko (oikea pää, ovi toistaiseksi lukossa).
/// Kuvat: Tools/PrisonAssets/make_prison_tiles.py (peilaus ja rajaus käyttäjän kuvista). Parallaksit: taivas (kone_taivas, hitain) ja vankilan piha-kuva (taka-parallaksi). Rekan kohtaus (TruckRide) siirtää tänne.
/// </summary>
public static partial class BeatEmUpSetup
{
    const string PrisonScenePath = "Assets/Scenes/Areas/Vankila.unity";
    const float PrisonX0 = 85000f, PrisonGroundPx = 710f, PrisonMouthPx = 1250f;

    [MenuItem("Beat em up/353. Vankila: sisäpiha (portti, tietä, terminaalin suu, parallaksit)", false, 353)]
    static void AddPrisonYard()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_sisa_1", "vankila_sisa_2", "vankila_sisa_3", "vankila_sisa_4", "vankila_piha_taka", "kone_taivas" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py ja anna Unityn tuoda kuvat."); return; }
        UnityEngine.SceneManagement.Scene scene;
        bool openedHere = false;
        if (File.Exists(PrisonScenePath))
        {
            scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
            if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            openedHere = true;
            if (!EditorSceneManager.SaveScene(scene, PrisonScenePath)) { Info("Scenen luonti epäonnistui: " + PrisonScenePath); EditorSceneManager.CloseScene(scene, true); return; }
        }
        try
        {
            foreach (var n in new[] { "Vankilan sisäpiha", "Vankilan ovet", "Alue: Vankila" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float ppu = 941f / (2f * CamHalf), H = 941f / ppu, W = 1672f / ppu;
            float bottom = CamY - CamHalf, top = bottom + H, cy = CamY, X0 = PrisonX0;
            GameObject NewRoot(string n)
            {
                var g = new GameObject(n);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, scene);
                Undo.RegisterCreatedObjectUndo(g, "Vankila");
                return g;
            }
            var root = NewRoot("Vankilan sisäpiha");
            var doorRoot = NewRoot("Vankilan ovet");
            SpriteRenderer Put(Transform parent, string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(parent, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos;
                return sr;
            }

            // ruudut: peilattu portti, kaksi pätkää tietä, terminaalin suu
            for (int i = 0; i < 4; i++)
            {
                var sp = ImportShipSprite(T + "vankila_sisa_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: vankila_sisa_" + (i + 1)); return; }
                var tile = Put(root.transform, "Vankila " + (i + 1), sp, new Vector3(X0 + W * (i + 0.5f), cy, 0f), -10000);
                tile.transform.localScale = new Vector3(1.0018f, 1f, 1f);   // ~1 px limitys: ruutujen väliin ei jää rakoa
            }

            var area = ReuseArea("Alue: Vankila");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = "Vankila"; area.useSidewalk = false;
            area.maxDepthY = top - PrisonGroundPx / ppu; area.minDepthY = bottom + 0.9f;
            area.walkMinX = X0 + 1.5f; area.walkMaxX = X0 + 4f * W - 1.2f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = X0 + 4f * W - W * 0.5f;
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
            area.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Viidakko.mp3"); area.musicGain = area.music != null ? JungleMusicGain : 1f;   // väliaikainen musiikki

            // parallaksit: taivas (hitain), sen edessä vankilan piha-kuva
            SpriteRenderer Para(string name, string file, float factor, int order, float scale)
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) return null;
                float left = area.camMinX - W * 0.5f;
                var sr = Put(root.transform, name, sp, new Vector3(left + sp.bounds.size.x * scale * 0.5f, cy, 0f), order);
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
                return sr;
            }
            Para("Vankilan taivas", "kone_taivas", 0.02f, -10100, 1.25f);
            Para("Vankilan piha", "vankila_piha_taka", 0.3f, -10050, 1f);

            // terminaalin suu: ovi toistaiseksi lukossa (seuraava osa tulee myöhemmin)
            float mouthX = X0 + 3f * W + PrisonMouthPx / ppu;
            var d = new GameObject("Vankila: terminaaliin").AddComponent<Door>();
            d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(mouthX, area.maxDepthY, 0f);
            d.prompt = "Mene terminaaliin"; d.here = area; d.target = area; d.spawnPoint = new Vector2(mouthX - 2.8f, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.4f));
            d.halfWidth = 2.2f; d.maxDistanceFromWall = 2.2f; d.requiredClue = "Vankila: terminaalin avain"; d.lockedClueText = "Terminaali aukeaa myöhemmin.";

            File.WriteAllLines(PrisonScenePath.Replace(".unity", ".areas.txt"), new[] { "Vankila" });
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var list = EditorBuildSettings.scenes.ToList();
            if (!list.Any(s => s.path == PrisonScenePath)) { list.Add(new EditorBuildSettingsScene(PrisonScenePath, true)); EditorBuildSettings.scenes = list.ToArray(); }
            Info("Vankilan sisäpiha rakennettu (scene " + PrisonScenePath + ", alue \"Vankila\", x " + X0.ToString("0") + "–" + (X0 + 4f * W).ToString("0") + ", lisätty Build Settingsiin): peilattu portti, kaksi pätkää tietä, terminaalin suu (ovi lukossa). Parallaksit: taivas ja vankilan piha.\nRekan ajo (TruckRide) vie tänne. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan terminaali (sisäpuoli) ----------------
    const float PrisonTermX0 = 86000f, TermImgW = 2560f, TermImgH = 922f;   // vankila_terminaali_sisa.png

    [MenuItem("Beat em up/354. Vankila: terminaalin sisäpuoli (ovi pihalta)", false, 354)]
    static void AddPrisonTerminal()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_terminaali_sisa", "vankila_terminaali_sisa_2" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353 (Vankilan sisäpiha)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var yard = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Vankila" && a.gameObject.scene == scene);
            if (yard == null) { Info("Pihan aluetta ei löytynyt: aja valikko 353."); return; }
            foreach (var n in new[] { "Vankilan terminaali", "Alue: Vankilan terminaali" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Terminaali:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, bottom = cy - CamHalf, X0 = PrisonTermX0;
            float ppu = TermImgH / H;             // kuva täyttää kameran korkeuden
            float img1 = TermImgW / ppu, imgW = 2f * img1, W = 1672f / (941f / H);   // kaksi kuvaa peräkkäin (vasen + jatko oikealla)
            var root = new GameObject("Vankilan terminaali");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + (i == 0 ? "vankila_terminaali_sisa" : "vankila_terminaali_sisa_2") + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (terminaali " + (i + 1) + ")."); return; }
                var sr = new GameObject("Terminaali sisältä " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + img1 * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);   // pieni limitys: kuvien väliin ei jää rakoa
            }

            var room = ReuseArea("Alue: Vankilan terminaali");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(room.gameObject, scene);
            room.areaName = "Vankilan terminaali"; room.useSidewalk = false;
            room.maxDepthY = top - TermImgH * (388f / 461f) / ppu; room.minDepthY = top - TermImgH * (458f / 461f) / ppu;   // lattia: kuvan alaosa (rivit 388–458 / 461)
            room.walkMinX = X0 + 1.5f; room.walkMaxX = X0 + imgW - 1.5f;
            room.camMinX = X0 + W * 0.5f; room.camMaxX = Mathf.Max(room.camMinX, X0 + imgW - W * 0.5f);
            room.camSize = CamHalf; room.camRiseY = 0f; room.camOffsetY = 0f; room.backScale = 1f; room.depthLimits = new Vector2[0];
            room.music = yard.music; room.musicGain = yard.musicGain;

            // ovet: pihan terminaalin suu <-> terminaalin vasen pää; vasemman pään pieni ovi ja oikea pää toistaiseksi lukossa
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.2f;
                return d;
            }
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.45f);
            float inX = X0 + 2.8f, outX = X0 + 1.8f;
            var mouth = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Vankila: terminaaliin");
            if (mouth != null)
            {
                mouth.target = room; mouth.requiredClue = ""; mouth.lockedClueText = ""; mouth.spawnPoint = new Vector2(inX + 0.6f, midY(room));
                EditorUtility.SetDirty(mouth);
                MakeDoor("Terminaali: pihalle", "Ulos pihalle", room, yard, new Vector3(outX, room.maxDepthY, 0f), new Vector2(mouth.transform.position.x - 2.8f, midY(yard)));
            }
            else Info("Pihan terminaaliovea (valikko 353) ei löytynyt; ovia ei yhdistetty.");
            var side = MakeDoor("Terminaali: sivuovi", "Mene sivuovesta", room, room, new Vector3(X0 + img1 * (270f / 1280f), room.maxDepthY, 0f), new Vector2(X0 + img1 * (270f / 1280f), midY(room)));
            side.requiredClue = "Vankila: sellien avain"; side.lockedClueText = "Ovi on lukossa.";   // vankilan käytävä tulee myöhemmin (pari ovea vie sinne)
            // oikean pään kaksoisovi (jatkokuvan oikea reuna, ~89 % kuvan leveydestä): toinen ovi vankilan käytävälle
            float bigDoorX = X0 + img1 * (1f + 0.89f);
            var big = MakeDoor("Terminaali: kaksoisovi", "Mene käytävälle", room, room, new Vector3(bigDoorX, room.maxDepthY, 0f), new Vector2(bigDoorX, midY(room)));
            big.halfWidth = 2.4f; big.requiredClue = "Vankila: käytävän avain"; big.lockedClueText = "Ovi on lukossa.";

            // suojalaatikot (iso puulaatikko, taakse piiloon; CoverCrate): muutama terminaalin lattialle
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                {
                    float cyRow = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.6f);
                    foreach (var cx in new[] { X0 + 15f, X0 + 36f, X0 + 52f })
                    {
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
                }
                else if (PrisonCrates) Info("Suojalaatikon kuvaa (Assets/Resources/Rekka/laatikko.png) ei löytynyt; laatikoita ei sijoitettu.");
            }
            File.AppendAllText(PrisonScenePath.Replace(".unity", ".areas.txt"), "");
            File.WriteAllLines(PrisonScenePath.Replace(".unity", ".areas.txt"), new[] { "Vankila", "Vankilan terminaali" });
            EditorUtility.SetDirty(room);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan terminaali rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + ", kaksi kuvaa): ovi pihan terminaalin suusta sisään, takaisin pihalle, sivuovi ja oikean pään kaksoisovi lukossa (vankilan käytävä myöhemmin). Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan käytävä: D-osasto, ensimmäinen osio ----------------
    const float PrisonCorrX0 = 87000f, CorrImgW = 2560f, CorrImgH = 922f;   // vankila_kaytava_d1.png

    [MenuItem("Beat em up/355. Vankila: D-osasto, käytävän ensimmäinen osio (kaksoisovi terminaalista, sellien ovet)", false, 355)]
    static void AddPrisonCorridorD()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_kaytava_d1", "vankila_kaytava_d1b" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353 ja 354 (pihan ja terminaalin alueet)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var term = areas.FirstOrDefault(a => a.areaName == "Vankilan terminaali");
            if (term == null) { Info("Terminaalin aluetta ei löytynyt: aja valikko 354."); return; }
            foreach (var n in new[] { "Vankilan D-osasto", "Alue: Vankilan D-osasto" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("D-osasto:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonCorrX0;
            float ppu = CorrImgH / H, img1 = CorrImgW / ppu, imgW = 2f * img1, W = 1672f / (941f / H);   // osio = kuva + sen peilikuva
            var root = new GameObject("Vankilan D-osasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + (i == 0 ? "vankila_kaytava_d1" : "vankila_kaytava_d1b") + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (D-osasto " + (i + 1) + ")."); return; }
                var sr = new GameObject("D-osasto " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + img1 * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
            }

            var hall = ReuseArea("Alue: Vankilan D-osasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hall.gameObject, scene);
            hall.areaName = "Vankilan D-osasto"; hall.useSidewalk = false;
            float lowMax = top - 0.905f * H, lowMin = top - 0.99f * H, upMax = top - 0.415f * H, upMin = top - 0.47f * H;   // alakerran lattia / yläkerran kulkusilta
            hall.maxDepthY = lowMax; hall.minDepthY = lowMin;
            hall.walkMinX = X0 + 1.5f; hall.walkMaxX = X0 + imgW - 1.5f;
            hall.camMinX = X0 + W * 0.5f; hall.camMaxX = Mathf.Max(hall.camMinX, X0 + imgW - W * 0.5f);
            hall.camSize = CamHalf; hall.camRiseY = 0f; hall.camOffsetY = 0f; hall.backScale = 1f; hall.depthLimits = new[] { new Vector2(X0, upMax), new Vector2(X0 + imgW, upMax) }; hall.depthLimitsExceedMax = true;
            hall.music = term.music; hall.musicGain = term.musicGain;
            var tf = root.AddComponent<TwoFloorCorridor>(); tf.area = hall; tf.lowerMin = lowMin; tf.lowerMax = lowMax; tf.upperMin = upMin; tf.upperMax = upMax;
            tf.stairs = new[] {
                new TwoFloorCorridor.Stair { xBottom = X0 + 0.17f * img1, xTop = X0 + 0.345f * img1, yBottom = top - 0.91f * H, yTop = top - 0.44f * H },
                new TwoFloorCorridor.Stair { xBottom = X0 + 1.83f * img1, xTop = X0 + 1.655f * img1, yBottom = top - 0.91f * H, yTop = top - 0.44f * H } };   // vinot portaat molemmissa päissä

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float half)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = half; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 0.9f;
                return d;
            }
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.5f);
            float entryX = X0 + 0.067f * img1, exitX = X0 + 1.933f * img1;   // vasemman ja (peilatun) oikean reunan kaksoisovet
            // terminaalin oikean pään kaksoisovi <-> D-osaston vasen kaksoisovi
            var big = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Terminaali: kaksoisovi");
            if (big != null)
            {
                big.target = hall; big.requiredClue = ""; big.lockedClueText = ""; big.spawnPoint = new Vector2(entryX + 1.6f, midY(hall)); EditorUtility.SetDirty(big);
                MakeDoor("D-osasto: terminaaliin", "Takaisin terminaaliin", hall, term, new Vector3(entryX, hall.maxDepthY, 0f), new Vector2(big.transform.position.x - 3.0f, midY(term)), 2.4f);
            }
            else Info("Terminaalin kaksoisovea ei löytynyt (valikko 354); D-osastoa ei yhdistetty terminaaliin.");
            // oikean pään kaksoisovi: seuraava osio myöhemmin
            var nextD = MakeDoor("D-osasto: seuraava osio", "Mene eteenpäin", hall, hall, new Vector3(exitX, hall.maxDepthY, 0f), new Vector2(exitX - 3.2f, midY(hall)), 2.4f);
            nextD.requiredClue = "Vankila: D-osaston oven avain"; nextD.lockedClueText = "Ovi on lukossa.";
            // sellien ovet (8 kpl: alkuperäisen kuvan neljä + peilikuvan neljä): lukossa, sisältö myöhemmin
            int cellNo = 0;
            foreach (var px in new[] { 0.4225f, 0.5975f, 0.7725f, 0.9475f, 1.0525f, 1.2275f, 1.4025f, 1.5775f })
            {
                cellNo++;
                var cd = MakeDoor("D-osasto: selli D" + cellNo, "Mene selliin D" + cellNo, hall, hall, new Vector3(X0 + px * img1, hall.maxDepthY, 0f), new Vector2(X0 + px * img1, midY(hall)), 1.4f);
                cd.requiredClue = "Vankila: sellin D" + cellNo + " avain"; cd.lockedClueText = "Sellin ovi on lukossa.";
                var up = MakeDoor("D-osasto: yläselli D" + (cellNo + 8), "Mene selliin D" + (cellNo + 8), hall, hall, new Vector3(X0 + px * img1, upMax, 0f), new Vector2(X0 + px * img1, upMax), 1.4f);
                up.requiredClue = "Vankila: sellin D" + (cellNo + 8) + " avain"; up.lockedClueText = "Sellin ovi on lukossa.";   // yläkerran sellit: sisältö myöhemmin
                float lx = X0 + DoorLabelPx[cellNo - 1] / 2560f * img1;   // oven keskikohta mitattuna kuvasta
                AddDoorLabel(root.transform, "D" + cellNo, lx, top - 678f / ppu, 1f / ppu);
                AddDoorLabel(root.transform, "D" + (cellNo + 8), lx, top - 223f / ppu, 1f / ppu);
            }

            // suojalaatikot käytävälle
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var fx in new[] { 0.55f, 1.0f, 1.45f })
                    {
                        float cyRow = Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.55f), cx = X0 + fx * img1;
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }

            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            foreach (var nm in new[] { "Vankila", "Vankilan terminaali", "Vankilan D-osasto" }) if (!names.Contains(nm)) names.Add(nm);
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(hall);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan D-osasto rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + "): terminaalin kaksoisovi vie vasempaan kaksoisoveen (ja takaisin), oikean pään kaksoisovi ja kahdeksan sellin ovea lukossa (D1–D8), neljä suojalaatikkoa. Seuraava osio myöhemmin. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// Sellien ovien keskikohdat taustakuvan pikseleinä (kuva 1: ovet 1–4, peilikuva 2560 + x: ovet 5–8), mitattu ovista.
    static readonly float[] DoorLabelPx = { 1085f, 1533f, 1981f, 2414f, 2705f, 3137f, 3585f, 4034f };

    /// Haalistunut sabluunanumero sellin oveen (esim. "D1"): teksti oven ikkunan alla, kahvan yläpuolella. px-mitat ovat taustakuvan pikseleitä (kuva 2560 × 922).
    /// capPx = isojen kirjainten korkeus pikseleinä. Koodilla piirretty, ei kuvaa: voi säätää vakioilla.
    static void AddDoorLabel(Transform parent, string text, float worldX, float worldY, float worldPerPx, float capPx = 21f, int order = -9990)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        float cs = capPx * worldPerPx / 4.5f;   // fontSize 64: kirjainkorkeus ≈ 4,5 × characterSize
        for (int pass = 0; pass < 2; pass++)
        {
            bool shadow = pass == 0;
            var go = new GameObject("Kyltti " + text + (shadow ? " (varjo)" : ""));
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(worldX + (shadow ? 0.012f : 0f), worldY + (shadow ? -0.012f : 0f), 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.font = font; tm.fontStyle = FontStyle.Bold; tm.fontSize = 64; tm.characterSize = cs;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = shadow ? new Color(0.05f, 0.05f, 0.05f, 0.30f) : new Color(0.90f, 0.88f, 0.78f, 0.48f);   // haalistunut maali
            var mr = go.GetComponent<MeshRenderer>(); mr.sharedMaterial = font.material; mr.sortingOrder = order + (shadow ? 0 : 1);
        }
    }

    /// Korjaa pihan terminaaliovi: valikko 353 luo oven uudelleen lukittuna ja osoittamaan pihaan itseensä (ajettuna 354:n jälkeen yhteys katoaa).
    /// Tämä yhdistää oven takaisin terminaaliin (kuten valikko 354) ilman, että koko vankilaa tarvitsee rakentaa uudelleen.
    [MenuItem("Beat em up/353b. Vankila: korjaa pihan ovi terminaaliin (ajettuna 353:n jälkeen)", false, 353)]
    static void FixPrisonYardDoor()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var yard = areas.FirstOrDefault(a => a.areaName == "Vankila"); var room = areas.FirstOrDefault(a => a.areaName == "Vankilan terminaali");
            var mouth = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Vankila: terminaaliin");
            if (yard == null || room == null || mouth == null) { Info("Puuttuu piha, terminaali tai pihan ovi: aja valikot 353 ja 354."); return; }
            float inX = PrisonTermX0 + 2.8f;
            mouth.target = room; mouth.requiredClue = ""; mouth.lockedClueText = ""; mouth.spawnPoint = new Vector2(inX + 0.6f, Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.45f));
            EditorUtility.SetDirty(mouth);
            var back = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Terminaali: pihalle");
            if (back != null) { back.target = yard; back.spawnPoint = new Vector2(mouth.transform.position.x - 2.8f, Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.45f)); EditorUtility.SetDirty(back); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Pihan ovi yhdistetty terminaaliin" + (back != null ? " ja terminaalin ovi takaisin pihalle." : " (terminaalin 'Ulos pihalle' -ovea ei löytynyt: aja 354)."));
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// Palauttaa puuttuvat terminaalin ovet (valikko 353 poistaa ne ajettaessa): terminaali -> piha, terminaali -> D-osasto ja D-osasto -> terminaali.
    /// Tarvitsee olemassa olevat alueet (353, 354, 355); ei rakenna mitään uudelleen. Ajettavissa uudelleen.
    [MenuItem("Beat em up/353c. Vankila: palauta terminaalin ja D-osaston väliset ovet", false, 353)]
    static void RestoreTerminalDoors()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var yard = areas.FirstOrDefault(a => a.areaName == "Vankila"); var room = areas.FirstOrDefault(a => a.areaName == "Vankilan terminaali"); var hall = areas.FirstOrDefault(a => a.areaName == "Vankilan D-osasto");
            if (yard == null || room == null || hall == null) { Info("Puuttuu piha, terminaali tai D-osasto: aja valikot 353, 354 ja 355."); return; }
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && (x.name == "Terminaali: pihalle" || x.name == "Terminaali: kaksoisovi" || x.name == "D-osasto: terminaaliin")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);
            Door Make(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float half, bool ground)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = half; d.maxDistanceFromWall = ground ? 2.4f : 2.2f;
                if (ground) { d.onGround = true; d.groundHalfDepth = 0.9f; }
                EditorUtility.SetDirty(d); return d;
            }
            float H = 2f * CamHalf;
            float ppuT = TermImgH / H, img1T = TermImgW / ppuT, ppuD = CorrImgH / H, img1D = CorrImgW / ppuD;
            float midRoom = Mathf.Lerp(room.maxDepthY, room.minDepthY, 0.45f), midYard = Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.45f), midHall = Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.5f);
            var mouth = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Vankila: terminaaliin");
            float mouthX = mouth != null ? mouth.transform.position.x : yard.walkMaxX - 4f;
            float bigX = PrisonTermX0 + img1T * (1f + 0.89f), entryX = PrisonCorrX0 + 0.067f * img1D;
            Make("Terminaali: pihalle", "Ulos pihalle", room, yard, new Vector3(PrisonTermX0 + 1.8f, room.maxDepthY, 0f), new Vector2(mouthX - 2.8f, midYard), 1.6f, false);
            var big = Make("Terminaali: kaksoisovi", "Mene käytävälle", room, hall, new Vector3(bigX, room.maxDepthY, 0f), new Vector2(entryX + 1.6f, midHall), 2.4f, false);
            Make("D-osasto: terminaaliin", "Takaisin terminaaliin", hall, room, new Vector3(entryX, hall.maxDepthY, 0f), new Vector2(bigX - 3.0f, midRoom), 2.4f, true);
            if (mouth != null) { mouth.target = room; mouth.requiredClue = ""; mouth.lockedClueText = ""; mouth.spawnPoint = new Vector2(PrisonTermX0 + 3.4f, midRoom); EditorUtility.SetDirty(mouth); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Terminaalin ovet palautettu: piha <-> terminaali (ovi pihalle) ja terminaali <-> D-osasto (kaksoisovi). Tästä pääsee D-osastolle, työpajan kautta E-osastolle ja yläkerran selliin E13 (Rex).");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan sellit (D1–D8): sama sellikuva, parittomat ovi oikealla, parilliset peilikuvana (ovi vasemmalla) ----------------
    const float PrisonCellX0 = 88000f, PrisonCellStep = 40f, CellImgW = 2560f, CellImgH = 922f;
    /// Sellit, joihin pääsee sisälle (ei lukossa). Muut ovat lukossa. Muuta lista, kun tiedät, mihin selleihin pääsee, ja aja valikko 356 uudelleen.
    static readonly int[] PrisonOpenCells = { 1, 2 };

    [MenuItem("Beat em up/356. Vankila: sellit D1–D8 (sellikuva, peilikuva; ovet D-osastolta)", false, 356)]
    static void AddPrisonCells()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_selli", "vankila_selli_b" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–355."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var hall = areas.FirstOrDefault(a => a.areaName == "Vankilan D-osasto");
            if (hall == null) { Info("D-osastoa ei löytynyt: aja valikko 355."); return; }
            foreach (var g in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Vankilan selli D") || g.name.StartsWith("Alue: Vankilan selli D")).ToArray()) Undo.DestroyObjectImmediate(g);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Selli D")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf;
            float ppu = CellImgH / H, imgW = CellImgW / ppu, W = 1672f / (941f / H);
            var sprR = ImportShipSprite(T + "vankila_selli.png", ppu, false, SpriteAlignment.Center);
            var sprL = ImportShipSprite(T + "vankila_selli_b.png", ppu, false, SpriteAlignment.Center);
            if (sprR == null || sprL == null) { Info("Sellikuvien tuonti epäonnistui."); return; }
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.4f; d.maxDistanceFromWall = 2.4f;
                return d;
            }
            var names = new System.Collections.Generic.List<string>(File.Exists(PrisonScenePath.Replace(".unity", ".areas.txt")) ? File.ReadAllLines(PrisonScenePath.Replace(".unity", ".areas.txt")) : new string[0]);
            int built = 0;
            for (int n = 1; n <= 8; n++)
            {
                bool doorRight = n % 2 == 1;   // parittomat: kuva sellaisenaan (ovi oikealla), parilliset peilikuva (ovi vasemmalla)
                float X0 = PrisonCellX0 + (n - 1) * PrisonCellStep;
                string rootName = "Vankilan selli D" + n, areaName = "Vankilan selli D" + n;
                var root = new GameObject(rootName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                Undo.RegisterCreatedObjectUndo(root, "Vankila");
                var sr = new GameObject("Selli D" + n).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = doorRight ? sprR : sprL; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + imgW * 0.5f, cy, 0f);

                var cell = ReuseArea("Alue: " + areaName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cell.gameObject, scene);
                cell.areaName = areaName; cell.useSidewalk = false;
                cell.maxDepthY = top - 0.76f * H; cell.minDepthY = top - 0.985f * H;   // lattia: kuvan alaosa
                cell.walkMinX = X0 + 1.5f; cell.walkMaxX = X0 + imgW - 1.5f;
                cell.camMinX = X0 + W * 0.5f / CellZoom; cell.camMaxX = Mathf.Max(cell.camMinX, X0 + imgW - W * 0.5f / CellZoom);   // lähikuva: hahmot isommiksi sellissä
                cell.camSize = CamHalf / CellZoom; cell.camRiseY = 0f; cell.camOffsetY = CamHalf / CellZoom - CamHalf;   // kuvan alareuna pysyy paikallaan (lattia) cell.backScale = 1f; cell.depthLimits = new Vector2[0];
                cell.music = hall.music; cell.musicGain = hall.musicGain;
                if (!names.Contains(areaName)) names.Add(areaName);

                // ovi sellin sisällä (oikea tai vasen reuna) takaisin käytävälle; käytävän oven kohta ja lukitus
                var corrDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "D-osasto: selli D" + n);
                float inDoorX = X0 + (doorRight ? 0.925f : 0.075f) * imgW;
                float midCell = Mathf.Lerp(cell.maxDepthY, cell.minDepthY, 0.5f);
                if (corrDoor != null)
                {
                    corrDoor.target = cell; corrDoor.spawnPoint = new Vector2(inDoorX + (doorRight ? -3.2f : 3.2f), midCell);
                    bool open = PrisonOpenCells.Contains(n);
                    corrDoor.requiredClue = open ? "" : "Vankila: sellin D" + n + " avain"; corrDoor.lockedClueText = "Sellin ovi on lukossa.";
                    EditorUtility.SetDirty(corrDoor);
                    var back = MakeDoor("Selli D" + n + ": ulos", "Ulos käytävälle", cell, hall, new Vector3(inDoorX, cell.maxDepthY, 0f), new Vector2(corrDoor.transform.position.x, Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.5f)));
                }
                EditorUtility.SetDirty(cell);
                built++;
            }
            File.WriteAllLines(PrisonScenePath.Replace(".unity", ".areas.txt"), names);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan sellit D1–D8 rakennettu (" + built + " kpl, x " + PrisonCellX0.ToString("0") + "…): parittomat ovi oikealla, parilliset peilikuvana. Auki: " + string.Join(", ", PrisonOpenCells.Select(c => "D" + c)) + " (muut lukossa; lista PrisonOpenCells tiedostossa PrisonYardSetup.cs). Sellit ovat tyhjiä. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan työpaja: kaksi kuvaa, kaksoisovi vasemmalla (D-osastolta) ja oikealla ----------------
    const float PrisonShopX0 = 89000f, ShopImgW = 2560f, ShopImgH = 922f;

    [MenuItem("Beat em up/357. Vankila: työpaja (kaksi kuvaa, ovi D-osastolta, suojalaatikot)", false, 357)]
    static void AddPrisonWorkshop()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_tyopaja_1", "vankila_tyopaja_2" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–355."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var hall = areas.FirstOrDefault(a => a.areaName == "Vankilan D-osasto");
            if (hall == null) { Info("D-osastoa ei löytynyt: aja valikko 355."); return; }
            foreach (var n in new[] { "Vankilan työpaja", "Alue: Vankilan työpaja" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Työpaja:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonShopX0;
            float ppu = ShopImgH / H, img1 = ShopImgW / ppu, imgW = 2f * img1, W = 1672f / (941f / H);
            var root = new GameObject("Vankilan työpaja");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_tyopaja_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (työpaja " + (i + 1) + ")."); return; }
                var sr = new GameObject("Työpaja " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + img1 * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
            }
            var shop = ReuseArea("Alue: Vankilan työpaja");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shop.gameObject, scene);
            shop.areaName = "Vankilan työpaja"; shop.useSidewalk = false;
            shop.maxDepthY = top - 0.70f * H; shop.minDepthY = top - 0.985f * H;
            shop.walkMinX = X0 + 1.5f; shop.walkMaxX = X0 + imgW - 1.5f;
            shop.camMinX = X0 + W * 0.5f; shop.camMaxX = Mathf.Max(shop.camMinX, X0 + imgW - W * 0.5f);
            shop.camSize = CamHalf; shop.camRiseY = 0f; shop.camOffsetY = 0f; shop.backScale = 1f; shop.depthLimits = new Vector2[0];
            shop.music = hall.music; shop.musicGain = hall.musicGain;

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 2.4f;
                return d;
            }
            float midShop = Mathf.Lerp(shop.maxDepthY, shop.minDepthY, 0.5f), midHall = Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.5f);
            float inX = X0 + 0.09f * img1, outX = X0 + img1 * 1.905f;   // vasen kaksoisovi (kuva 1) ja oikea kaksoisovi (kuva 2)
            var next = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "D-osasto: seuraava osio");
            if (next != null)
            {
                next.target = shop; next.requiredClue = ""; next.lockedClueText = ""; next.prompt = "Mene työpajaan"; next.spawnPoint = new Vector2(inX + 3.2f, midShop); EditorUtility.SetDirty(next);
                MakeDoor("Työpaja: takaisin D-osastolle", "Takaisin D-osastolle", shop, hall, new Vector3(inX, shop.maxDepthY, 0f), new Vector2(next.transform.position.x - 3.2f, midHall));
            }
            else Info("D-osaston oikean pään ovea ei löytynyt (valikko 355); työpajaa ei yhdistetty.");
            var exit = MakeDoor("Työpaja: oikea kaksoisovi", "Mene eteenpäin", shop, shop, new Vector3(outX, shop.maxDepthY, 0f), new Vector2(outX - 3.2f, midShop));
            exit.requiredClue = "Vankila: työpajan oven avain"; exit.lockedClueText = "Ovi on lukossa.";   // seuraava osa myöhemmin

            // suojalaatikot työpajaan
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var fx in new[] { 0.42f, 1.18f, 1.62f })
                    {
                        float cyRow = Mathf.Lerp(shop.maxDepthY, shop.minDepthY, 0.55f), cx = X0 + fx * img1;
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan työpaja")) names.Add("Vankilan työpaja");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(shop);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan työpaja rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + ", kaksi kuvaa): D-osaston oikean pään kaksoisovi vie työpajan vasempaan kaksoisoveen (ja takaisin), oikea kaksoisovi lukossa, kolme suojalaatikkoa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- E-osasto: toinen selliosasto (sama käytäväkuva kuin D-osastolla), työpajan jälkeen ----------------
    const float PrisonEX0 = 90000f;

    [MenuItem("Beat em up/358. Vankila: E-osasto, toinen sellikäytävä (ovi työpajasta, sellit lukossa)", false, 358)]
    static void AddPrisonCorridorE()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_kaytava_e1", "vankila_kaytava_e1b" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–357."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var hallD = areas.FirstOrDefault(a => a.areaName == "Vankilan D-osasto");
            var shop = areas.FirstOrDefault(a => a.areaName == "Vankilan työpaja");
            if (hallD == null || shop == null) { Info("D-osastoa tai työpajaa ei löytynyt: aja valikot 355 ja 357."); return; }
            foreach (var n in new[] { "Vankilan E-osasto", "Alue: Vankilan E-osasto" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("E-osasto:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonEX0;
            float ppu = CorrImgH / H, img1 = CorrImgW / ppu, imgW = 2f * img1, W = 1672f / (941f / H);
            var root = new GameObject("Vankilan E-osasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + (i == 0 ? "vankila_kaytava_e1" : "vankila_kaytava_e1b") + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (E-osasto " + (i + 1) + ")."); return; }
                var sr = new GameObject("E-osasto " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + img1 * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
            }
            var hall = ReuseArea("Alue: Vankilan E-osasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hall.gameObject, scene);
            hall.areaName = "Vankilan E-osasto"; hall.useSidewalk = false;
            float lowMax = top - 0.905f * H, lowMin = top - 0.99f * H, upMax = top - 0.415f * H, upMin = top - 0.47f * H;   // alakerran lattia / yläkerran kulkusilta
            hall.maxDepthY = lowMax; hall.minDepthY = lowMin;
            hall.walkMinX = X0 + 1.5f; hall.walkMaxX = X0 + imgW - 1.5f;
            hall.camMinX = X0 + W * 0.5f; hall.camMaxX = Mathf.Max(hall.camMinX, X0 + imgW - W * 0.5f);
            hall.camSize = CamHalf; hall.camRiseY = 0f; hall.camOffsetY = 0f; hall.backScale = 1f; hall.depthLimits = new[] { new Vector2(X0, upMax), new Vector2(X0 + imgW, upMax) }; hall.depthLimitsExceedMax = true;
            hall.music = hallD.music; hall.musicGain = hallD.musicGain;
            var tf = root.AddComponent<TwoFloorCorridor>(); tf.area = hall; tf.lowerMin = lowMin; tf.lowerMax = lowMax; tf.upperMin = upMin; tf.upperMax = upMax;
            tf.stairs = new[] {
                new TwoFloorCorridor.Stair { xBottom = X0 + 0.17f * img1, xTop = X0 + 0.345f * img1, yBottom = top - 0.91f * H, yTop = top - 0.44f * H },
                new TwoFloorCorridor.Stair { xBottom = X0 + 1.83f * img1, xTop = X0 + 1.655f * img1, yBottom = top - 0.91f * H, yTop = top - 0.44f * H } };   // vinot portaat molemmissa päissä

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float half)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = half; d.maxDistanceFromWall = 2.4f; d.onGround = true; d.groundHalfDepth = 0.9f;
                return d;
            }
            float midY(Area a) => Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.5f);
            float entryX = X0 + 0.067f * img1, exitX = X0 + 1.933f * img1;
            var shopOut = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Työpaja: oikea kaksoisovi");
            if (shopOut != null)
            {
                shopOut.target = hall; shopOut.requiredClue = ""; shopOut.lockedClueText = ""; shopOut.prompt = "Mene E-osastolle"; shopOut.spawnPoint = new Vector2(entryX + 1.6f, midY(hall)); EditorUtility.SetDirty(shopOut);
                MakeDoor("E-osasto: takaisin työpajaan", "Takaisin työpajaan", hall, shop, new Vector3(entryX, hall.maxDepthY, 0f), new Vector2(shopOut.transform.position.x - 3.2f, midY(shop)), 2.4f);
            }
            else Info("Työpajan oikeaa kaksoisovea ei löytynyt (valikko 357); E-osastoa ei yhdistetty.");
            var nextE = MakeDoor("E-osasto: seuraava osio", "Mene eteenpäin", hall, hall, new Vector3(exitX, hall.maxDepthY, 0f), new Vector2(exitX - 3.2f, midY(hall)), 2.4f);
            nextE.requiredClue = "Vankila: E-osaston oven avain"; nextE.lockedClueText = "Ovi on lukossa.";   // avataan valikossa 359
            int cellNo = 0;
            foreach (var px in new[] { 0.4225f, 0.5975f, 0.7725f, 0.9475f, 1.0525f, 1.2275f, 1.4025f, 1.5775f })
            {
                cellNo++;
                var cd = MakeDoor("E-osasto: selli E" + cellNo, "Mene selliin E" + cellNo, hall, hall, new Vector3(X0 + px * img1, hall.maxDepthY, 0f), new Vector2(X0 + px * img1, midY(hall)), 1.4f);
                cd.requiredClue = "Vankila: sellin E" + cellNo + " avain"; cd.lockedClueText = "Sellin ovi on lukossa.";
                var up = MakeDoor("E-osasto: yläselli E" + (cellNo + 8), "Mene selliin E" + (cellNo + 8), hall, hall, new Vector3(X0 + px * img1, upMax, 0f), new Vector2(X0 + px * img1, upMax), 1.4f);
                up.requiredClue = "Vankila: sellin E" + (cellNo + 8) + " avain"; up.lockedClueText = "Sellin ovi on lukossa.";   // yläkerran sellit: sisältö myöhemmin
                float lx = X0 + DoorLabelPx[cellNo - 1] / 2560f * img1;   // oven keskikohta mitattuna kuvasta
                AddDoorLabel(root.transform, "E" + cellNo, lx, top - 678f / ppu, 1f / ppu);
                AddDoorLabel(root.transform, "E" + (cellNo + 8), lx, top - 223f / ppu, 1f / ppu);
            }
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var fx in new[] { 0.55f, 1.0f, 1.45f })
                    {
                        float cyRow = Mathf.Lerp(hall.maxDepthY, hall.minDepthY, 0.55f), cx = X0 + fx * img1;
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan E-osasto")) names.Add("Vankilan E-osasto");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(hall);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan E-osasto rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + "): työpajan oikea kaksoisovi vie vasempaan kaksoisoveen (ja takaisin), kahdeksan sellin ovea (E1–E8) ja oikean pään ovi lukossa, neljä suojalaatikkoa. Aja sitten valikko 359 (ruokala). Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan ruokala: kaksi kuvaa, kaksoisovi vasemmalla (E-osastolta) ja oikealla ----------------
    const float PrisonCafeX0 = 91000f, CafeImgW = 2560f, CafeImgH = 922f;

    [MenuItem("Beat em up/359. Vankila: ruokala (kaksi kuvaa, ovi E-osastolta, suojalaatikot)", false, 359)]
    static void AddPrisonCafeteria()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_ruokala_1", "vankila_ruokala_2" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–358."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var hallE = areas.FirstOrDefault(a => a.areaName == "Vankilan E-osasto");
            if (hallE == null) { Info("E-osastoa ei löytynyt: aja valikko 358."); return; }
            foreach (var n in new[] { "Vankilan ruokala", "Alue: Vankilan ruokala" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Ruokala:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonCafeX0;
            float ppu = CafeImgH / H, img1 = CafeImgW / ppu, imgW = 2f * img1, W = 1672f / (941f / H);
            var root = new GameObject("Vankilan ruokala");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_ruokala_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (ruokala " + (i + 1) + ")."); return; }
                var sr = new GameObject("Ruokala " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + img1 * (i + 0.5f), cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
            }
            var cafe = ReuseArea("Alue: Vankilan ruokala");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cafe.gameObject, scene);
            cafe.areaName = "Vankilan ruokala"; cafe.useSidewalk = false;
            cafe.maxDepthY = top - 0.64f * H; cafe.minDepthY = top - 0.985f * H;   // lattia alkaa tiskin alareunasta (~57 %), pehmeä marginaali
            cafe.walkMinX = X0 + 1.5f; cafe.walkMaxX = X0 + imgW - 1.5f;
            cafe.camMinX = X0 + W * 0.5f; cafe.camMaxX = Mathf.Max(cafe.camMinX, X0 + imgW - W * 0.5f);
            cafe.camSize = CamHalf; cafe.camRiseY = 0f; cafe.camOffsetY = 0f; cafe.backScale = 1f; cafe.depthLimits = new Vector2[0];
            cafe.music = hallE.music; cafe.musicGain = hallE.musicGain;

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 2.4f;
                return d;
            }
            float midCafe = Mathf.Lerp(cafe.maxDepthY, cafe.minDepthY, 0.5f), midE = Mathf.Lerp(hallE.maxDepthY, hallE.minDepthY, 0.5f);
            float inX = X0 + 0.09f * img1, outX = X0 + img1 * 1.90f;
            var next = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "E-osasto: seuraava osio");
            if (next != null)
            {
                next.target = cafe; next.requiredClue = ""; next.lockedClueText = ""; next.prompt = "Mene ruokalaan"; next.spawnPoint = new Vector2(inX + 3.2f, midCafe); EditorUtility.SetDirty(next);
                MakeDoor("Ruokala: takaisin E-osastolle", "Takaisin E-osastolle", cafe, hallE, new Vector3(inX, cafe.maxDepthY, 0f), new Vector2(next.transform.position.x - 3.2f, midE));
            }
            else Info("E-osaston oikean pään ovea ei löytynyt (valikko 358); ruokalaa ei yhdistetty.");
            var exit = MakeDoor("Ruokala: oikea kaksoisovi", "Mene eteenpäin", cafe, cafe, new Vector3(outX, cafe.maxDepthY, 0f), new Vector2(outX - 3.2f, midCafe));
            exit.requiredClue = "Vankila: ruokalan oven avain"; exit.lockedClueText = "Ovi on lukossa.";   // seuraava osa myöhemmin

            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var fx in new[] { 0.45f, 1.05f, 1.6f })
                    {
                        float cyRow = Mathf.Lerp(cafe.maxDepthY, cafe.minDepthY, 0.6f), cx = X0 + fx * img1;
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan ruokala")) names.Add("Vankilan ruokala");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(cafe);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan ruokala rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + "): E-osaston oikean pään kaksoisovi vie ruokalan vasempaan kaksoisoveen (ja takaisin), oikea kaksoisovi lukossa, kolme suojalaatikkoa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan kuntopiha: kolme kuvaa, kaksoisovi vasemmalla (ruokalasta) ja oikealla, taivasparallaksi ----------------
    const float PrisonYardX0 = 92000f;

    [MenuItem("Beat em up/360. Vankila: kuntopiha (kolme kuvaa, ovi ruokalasta, taivas, suojalaatikot)", false, 360)]
    static void AddPrisonExerciseYard()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_piha_1", "vankila_piha_2", "vankila_piha_3", "kone_taivas" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–359."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var cafe = areas.FirstOrDefault(a => a.areaName == "Vankilan ruokala");
            if (cafe == null) { Info("Ruokalaa ei löytynyt: aja valikko 359."); return; }
            foreach (var n in new[] { "Vankilan kuntopiha", "Alue: Vankilan kuntopiha" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Kuntopiha:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonYardX0;
            float ppu = 922f / H, W = 1672f / (941f / H);
            float[] tw = { 2560f / ppu, 2406f / ppu, 2406f / ppu };   // kuvien leveydet (kuva 2 ja 3 on leikattu päällekkäisyyden verran)
            float totalW = tw[0] + tw[1] + tw[2];
            var root = new GameObject("Vankilan kuntopiha");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            float tx = X0;
            for (int i = 0; i < 3; i++)
            {
                var sp = ImportShipSprite(T + "vankila_piha_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (kuntopiha " + (i + 1) + ")."); return; }
                var sr = new GameObject("Kuntopiha " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(tx + tw[i] * 0.5f, cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
                tx += tw[i];
            }
            var yard = ReuseArea("Alue: Vankilan kuntopiha");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(yard.gameObject, scene);
            yard.areaName = "Vankilan kuntopiha"; yard.useSidewalk = false;
            yard.maxDepthY = top - 0.72f * H; yard.minDepthY = top - 0.985f * H;   // lattia kiviaidan edestä alareunaan
            yard.walkMinX = X0 + 1.5f; yard.walkMaxX = X0 + totalW - 1.5f;
            yard.camMinX = X0 + W * 0.5f; yard.camMaxX = Mathf.Max(yard.camMinX, X0 + totalW - W * 0.5f);
            yard.camSize = CamHalf; yard.camRiseY = 0f; yard.camOffsetY = 0f; yard.backScale = 1f; yard.depthLimits = new Vector2[0];
            yard.music = cafe.music; yard.musicGain = cafe.musicGain;

            // taivas parallaksina kuvan takana (kuvien taivas on läpinäkyvä)
            {
                var sp = ImportShipSprite(T + "kone_taivas.png", ppu, false, SpriteAlignment.Center);
                if (sp != null)
                {
                    float scale = 1.25f, left = yard.camMinX - W * 0.5f;
                    var sr = new GameObject("Kuntopihan taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + sp.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = yard.camMinX; pl.minX = yard.camMinX - 2f; pl.maxX = yard.camMaxX + 2f;
                }
            }

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 2.4f;
                d.onGround = true; d.groundHalfDepth = 1.6f;   // ovet seinässä lattian takareunan takana
                return d;
            }
            float midYard = Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.5f), midCafe = Mathf.Lerp(cafe.maxDepthY, cafe.minDepthY, 0.5f);
            float inX = X0 + 218f / ppu;                          // vasen kaksoisovi (kuva 1)
            float outX = X0 + tw[0] + tw[1] + 2166f / ppu;       // oikea kaksoisovi (kuva 3)
            float doorY = yard.maxDepthY - 0.4f;
            var next = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Ruokala: oikea kaksoisovi");
            if (next != null)
            {
                next.target = yard; next.requiredClue = ""; next.lockedClueText = ""; next.prompt = "Mene kuntopihalle"; next.spawnPoint = new Vector2(inX + 3.4f, midYard); EditorUtility.SetDirty(next);
                MakeDoor("Kuntopiha: takaisin ruokalaan", "Takaisin ruokalaan", yard, cafe, new Vector3(inX, doorY, 0f), new Vector2(next.transform.position.x - 3.2f, midCafe));
            }
            else Info("Ruokalan oikeaa kaksoisovea ei löytynyt (valikko 359); kuntopihaa ei yhdistetty.");
            var exit = MakeDoor("Kuntopiha: oikea kaksoisovi", "Mene eteenpäin", yard, yard, new Vector3(outX, doorY, 0f), new Vector2(outX - 3.4f, midYard));
            exit.requiredClue = "Vankila: pihan oven avain"; exit.lockedClueText = "Ovi on lukossa.";   // seuraava osa myöhemmin

            // suojalaatikot (laitteiden välissä)
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var fx in new[] { 0.20f, 0.52f, 0.78f })
                    {
                        float cyRow = Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.55f), cx = X0 + fx * totalW;
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan kuntopiha")) names.Add("Vankilan kuntopiha");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(yard);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan kuntopiha rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + ", kolme kuvaa + taivasparallaksi): ruokalan oikea kaksoisovi vie pihan vasempaan oveen (ja takaisin), pihan oikea kaksoisovi lukossa, kolme suojalaatikkoa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan henkilökunnan tilat: kaksikerroksinen (portaat, keittiö, pukuhuone), yläkerrassa Wardenin puuovet ----------------
    const float PrisonStaffX0 = 93000f;

    [MenuItem("Beat em up/362. Vankila: henkilökunnan tilat (kaksi kuvaa, kaksi kerrosta, portaat, Wardenin ovi)", false, 362)]
    static void AddPrisonStaffArea()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_henkilokunta_1", "vankila_henkilokunta_2", "kone_taivas" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–361."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var yard = areas.FirstOrDefault(a => a.areaName == "Vankilan kirjasto");
            if (yard == null) { Info("Kirjastoa ei löytynyt: aja valikko 361."); return; }
            foreach (var n in new[] { "Vankilan henkilökunta", "Alue: Vankilan henkilökunta" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Henkilökunta:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonStaffX0;
            float ppu = 922f / H, W = 1672f / (941f / H);
            float[] tw = { 2560f / ppu, 2522f / ppu };
            float totalW = tw[0] + tw[1];
            float wx(float px) => X0 + px * 1.28f / ppu;   // px = kuvakoordinaatti (2000 px:n mittakaavassa, kuvat peräkkäin; oikean kuvan päällekkäisyys leikattu)
            var root = new GameObject("Vankilan henkilökunta");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            float tx = X0;
            for (int i = 0; i < 2; i++)
            {
                var sp = ImportShipSprite(T + "vankila_henkilokunta_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui (henkilökunta " + (i + 1) + ")."); return; }
                var sr = new GameObject("Henkilökunta " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(tx + tw[i] * 0.5f, cy, 0f);
                sr.transform.localScale = new Vector3(1.0012f, 1f, 1f);
                tx += tw[i];
            }
            var hall = ReuseArea("Alue: Vankilan henkilökunta");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hall.gameObject, scene);
            hall.areaName = "Vankilan henkilökunta"; hall.useSidewalk = false;
            float lowMax = top - 0.905f * H, lowMin = top - 0.99f * H, upMax = top - 0.415f * H, upMin = top - 0.47f * H;
            hall.maxDepthY = lowMax; hall.minDepthY = lowMin;
            hall.depthLimits = new[] { new Vector2(X0, upMax), new Vector2(X0 + totalW, upMax) }; hall.depthLimitsExceedMax = true;
            hall.walkMinX = X0 + 1.5f; hall.walkMaxX = X0 + totalW - 1.5f;
            hall.camMinX = X0 + W * 0.5f; hall.camMaxX = Mathf.Max(hall.camMinX, X0 + totalW - W * 0.5f);
            hall.camSize = CamHalf; hall.camRiseY = 0f; hall.camOffsetY = 0f; hall.backScale = 1f;
            hall.music = yard.music; hall.musicGain = yard.musicGain;
            var tf = root.AddComponent<TwoFloorCorridor>(); tf.area = hall; tf.lowerMin = lowMin; tf.lowerMax = lowMax; tf.upperMin = upMin; tf.upperMax = upMax;
            tf.stairs = new[] {
                new TwoFloorCorridor.Stair { xBottom = wx(340f), xTop = wx(690f), yBottom = top - 0.91f * H, yTop = top - 0.44f * H },     // vasemmat portaat (nousu oikealle)
                new TwoFloorCorridor.Stair { xBottom = wx(3560f), xTop = wx(3270f), yBottom = top - 0.91f * H, yTop = top - 0.44f * H } };   // oikeat portaat (nousu vasemmalle)

            // ikkunat ovat läpinäkyviä: taivasparallaksi näkyy niistä
            {
                var sp = ImportShipSprite(T + "kone_taivas.png", ppu, false, SpriteAlignment.Center);
                if (sp != null)
                {
                    float scale = 1.25f, left = hall.camMinX - W * 0.5f;
                    var sr = new GameObject("Henkilökunnan taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + sp.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = hall.camMinX; pl.minX = hall.camMinX - 2f; pl.maxX = hall.camMaxX + 2f;
                }
            }

            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float half)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = half; d.maxDistanceFromWall = 2.4f;
                d.onGround = true; d.groundHalfDepth = 0.9f;
                return d;
            }
            float midHall = Mathf.Lerp(lowMax, lowMin, 0.5f), midYard = Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.5f);
            float entryX = wx(135f);
            var next = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Kirjasto: oikea ovi");
            if (next != null)
            {
                next.target = hall; next.requiredClue = ""; next.lockedClueText = ""; next.prompt = "Mene henkilökunnan tiloihin"; next.spawnPoint = new Vector2(entryX + 1.6f, midHall); EditorUtility.SetDirty(next);
                MakeDoor("Henkilökunta: takaisin kirjastoon", "Takaisin kirjastoon", hall, yard, new Vector3(entryX, lowMax, 0f), new Vector2(next.transform.position.x - 3.4f, midYard), 2.4f);
            }
            else Info("Kirjaston oikeaa ovea ei löytynyt (valikko 361); henkilökunnan tiloja ei yhdistetty.");

            // lukitut ovet (sisältö myöhemmin): vessat ja hissi alakerrassa, harmaa ovi ja Wardenin puuovet yläkerrassa
            void Locked(string name, string prompt, float px, float y, float half, string clue, string text)
            {
                var d = MakeDoor(name, prompt, hall, hall, new Vector3(wx(px), y, 0f), new Vector2(wx(px), midHall), half);
                d.requiredClue = clue; d.lockedClueText = text;
            }
            Locked("Henkilökunta: vessa 1", "Mene vessaan", 795f, lowMax, 1.3f, "Vankila: vessan avain", "Ovi on lukossa.");
            Locked("Henkilökunta: vessa 2", "Mene vessaan", 957f, lowMax, 1.3f, "Vankila: vessan avain", "Ovi on lukossa.");
            Locked("Henkilökunta: hissi", "Mene hissiin", 3715f, lowMax, 135f * 1.28f / ppu, "Vankila: hissin avain", "Hissi ei toimi.");
            Locked("Henkilökunta: yläkerran ovi", "Mene sisään", 775f, upMax, 1.4f, "Vankila: vartijahuoneen avain", "Ovi on lukossa.");
            Locked("Henkilökunta: Wardenin huone", "Mene Wardenin huoneeseen", 3098f, upMax, 2.4f, "Vankila: Wardenin huoneen avain", "Wardenin ovi on lukossa.");

            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var px in new[] { 1450f, 2000f, 2550f })
                    {
                        float cyRow = Mathf.Lerp(lowMax, lowMin, 0.55f), cx = wx(px);
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan henkilökunta")) names.Add("Vankilan henkilökunta");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(hall);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan henkilökunnan tilat rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + ", kaksi kerrosta, portaat molemmissa päissä): kirjaston oikea ovi vie vasempaan oveen (ja takaisin). Lukossa: vessat, hissi, yläkerran harmaa ovi ja Wardenin puuovet (huone tulee myöhemmin). Kolme suojalaatikkoa. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    // ---------------- Vankilan kirjasto + kanttiini (Sohvi myyjänä): kuntopihan ja henkilökunnan tilojen välissä ----------------
    const float PrisonLibX0 = 94000f, LibSohviScale = 1.1f;   // Sohvin koko arvattu; säädä tarvittaessa

    [MenuItem("Beat em up/361. Vankila: kirjasto ja kanttiini (Sohvi myyjänä, ovi kuntopihalta)", false, 361)]
    static void AddPrisonLibrary()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "vankila_kirjasto", "kone_taivas" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja python Tools/PrisonAssets/make_prison_tiles.py."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikot 353–360."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        UnityEngine.SceneManagement.Scene alpine = default; bool openedAlpine = false;
        try
        {
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var yard = areas.FirstOrDefault(a => a.areaName == "Vankilan kuntopiha");
            if (yard == null) { Info("Kuntopihaa ei löytynyt: aja valikko 360."); return; }
            foreach (var n in new[] { "Vankilan kirjasto", "Alue: Vankilan kirjasto" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);
            foreach (var dd in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x != null && x.gameObject.scene == scene && x.name.StartsWith("Kirjasto:")).ToArray())
                Undo.DestroyObjectImmediate(dd.gameObject);

            float H = 2f * CamHalf, cy = CamY, top = cy + CamHalf, X0 = PrisonLibX0;
            float ppu = 922f / H, W = 1672f / (941f / H), imgW = 2560f / ppu;
            float wx(float px) => X0 + px * 1.28f / ppu;   // px = kuvakoordinaatti (2000 px:n mittakaavassa)
            var root = new GameObject("Vankilan kirjasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila");
            var sp = ImportShipSprite(T + "vankila_kirjasto.png", ppu, false, SpriteAlignment.Center);
            if (sp == null) { Info("Kuvan tuonti epäonnistui (kirjasto)."); return; }
            var bg = new GameObject("Kirjasto").AddComponent<SpriteRenderer>();
            bg.transform.SetParent(root.transform, false); bg.sprite = sp; bg.sortingOrder = -10000; bg.transform.position = new Vector3(X0 + imgW * 0.5f, cy, 0f);

            var lib = ReuseArea("Alue: Vankilan kirjasto");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lib.gameObject, scene);
            lib.areaName = "Vankilan kirjasto"; lib.useSidewalk = false;
            lib.maxDepthY = top - 0.675f * H; lib.minDepthY = top - 0.985f * H;
            lib.walkMinX = X0 + 1.5f; lib.walkMaxX = X0 + imgW - 1.5f;
            lib.camMinX = X0 + W * 0.5f; lib.camMaxX = Mathf.Max(lib.camMinX, X0 + imgW - W * 0.5f);
            lib.camSize = CamHalf; lib.camRiseY = 0f; lib.camOffsetY = 0f; lib.backScale = 1f; lib.depthLimits = new Vector2[0];
            lib.music = yard.music; lib.musicGain = yard.musicGain;

            // ikkunat läpinäkyviä: taivasparallaksi
            {
                var ssp = ImportShipSprite(T + "kone_taivas.png", ppu, false, SpriteAlignment.Center);
                if (ssp != null)
                {
                    float scale = 1.25f, left = lib.camMinX - W * 0.5f;
                    var sr = new GameObject("Kirjaston taivas").AddComponent<SpriteRenderer>();
                    sr.transform.SetParent(root.transform, false); sr.sprite = ssp; sr.sortingOrder = -10100;
                    sr.transform.position = new Vector3(left + ssp.bounds.size.x * scale * 0.5f, cy, 0f);
                    sr.transform.localScale = new Vector3(scale, scale, 1f);
                    var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                    pl.factor = 0.02f; pl.startCamX = lib.camMinX; pl.minX = lib.camMinX - 2f; pl.maxX = lib.camMaxX + 2f;
                }
            }

            // ovet
            var doorRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan ovet");
            if (doorRoot == null) { doorRoot = new GameObject("Vankilan ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene); }
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.2f; d.maxDistanceFromWall = 2.4f;
                d.onGround = true; d.groundHalfDepth = 1.6f;
                return d;
            }
            float midLib = Mathf.Lerp(lib.maxDepthY, lib.minDepthY, 0.5f), midYard = Mathf.Lerp(yard.maxDepthY, yard.minDepthY, 0.5f);
            float inX = wx(105f), outX = wx(1900f), doorY = lib.maxDepthY - 0.3f;
            var next = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x != null && x.gameObject.scene == scene && x.name == "Kuntopiha: oikea kaksoisovi");
            if (next != null)
            {
                next.target = lib; next.requiredClue = ""; next.lockedClueText = ""; next.prompt = "Mene kirjastoon"; next.spawnPoint = new Vector2(inX + 3.2f, midLib); EditorUtility.SetDirty(next);
                MakeDoor("Kirjasto: takaisin kuntopihalle", "Takaisin kuntopihalle", lib, yard, new Vector3(inX, doorY, 0f), new Vector2(next.transform.position.x - 3.4f, midYard));
            }
            else Info("Kuntopihan oikeaa kaksoisovea ei löytynyt (valikko 360); kirjastoa ei yhdistetty.");
            var exit = MakeDoor("Kirjasto: oikea ovi", "Mene eteenpäin", lib, lib, new Vector3(outX, doorY, 0f), new Vector2(outX - 3.2f, midLib));
            exit.requiredClue = "Vankila: kirjaston oven avain"; exit.lockedClueText = "Ovi on lukossa.";   // valikko 362 liittää henkilökunnan tilat tähän

            // Sohvi myyjäksi kanttiinin tiskin taakse (mallit Alppikylän afterski-baarista)
            string sohviInfo = "";
            if (File.Exists(AlpineScenePathNow))
            {
                alpine = EditorSceneManager.GetSceneByPath(AlpineScenePathNow);
                if (!alpine.IsValid() || !alpine.isLoaded) { alpine = EditorSceneManager.OpenScene(AlpineScenePathNow, OpenSceneMode.Additive); openedAlpine = true; }
                var sohviT = Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alpine && c.name == "Sohvi (afterski-baari)");
                var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == alpine && c.name == "Afterski-baarin tiski (Sohvi)");
                if (sohviT != null && counterT != null)
                {
                    float hx = wx(1475f), hy = top - 0.495f * H;   // tiskin pinta ~ rivi 357/722
                    var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (vankilan kanttiini)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, scene); sg.transform.SetParent(root.transform, false);
                    sg.transform.position = new Vector3(hx, hy, 0f); sg.transform.localScale = Vector3.one * LibSohviScale;
                    var rS = sg.GetComponentInChildren<SpriteRenderer>(); if (rS != null) rS.sortingOrder = Mathf.RoundToInt(-hy * 100f);
                    var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Vankilan kanttiinin tiski (Sohvi)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, scene); shopGo.transform.SetParent(root.transform, false);
                    shopGo.transform.position = new Vector3(hx, lib.maxDepthY, 0f);
                    var sc = shopGo.GetComponent<ShopCounter>();
                    sc.here = lib; sc.title = "VANKILAN KANTTIINI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville";
                    sc.halfWidth = 4.2f; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
                    sc.items = new[]
                    {
                        new ShopCounter.Item { name = "Voileipä", price = 6, heal = 40, stamina = 10, comment = "Vankilan parasta, tuoretta." },
                        new ShopCounter.Item { name = "Keksejä", price = 3, heal = 15, stamina = 20, comment = "Purkista." },
                        new ShopCounter.Item { name = "Sipsipussi", price = 3, heal = 20, stamina = 10, comment = "Suolaista." },
                        new ShopCounter.Item { name = "Virvoitusjuoma", price = 4, heal = 10, stamina = 40, comment = "Kylmää jääkaapista." },
                        new ShopCounter.Item { name = "Kahvi", price = 3, heal = 0, stamina = 60, comment = "Vahvaa ja mustaa." },
                        new ShopCounter.Item { name = "Kalakeitto", price = 15, heal = 999, stamina = 999, comment = "Päivän keitto. Täyttää kaiken." },
                    };
                    sohviInfo = "Sohvi kanttiinin tiskillä (kauppa: 6 omaa tuotetta, Kalakeitto täyttää kaiken)";
                }
                else sohviInfo = "Sohvin malleja ei löytynyt Alppikylän scenestä (aja valikko 279) – kanttiini ilman myyjää";
            }
            else sohviInfo = "Alppikylän scene puuttuu – Sohvia ei lisätty";

            // suojalaatikot
            {
                const string crateAsset = "Assets/Resources/Rekka/laatikko.png";
                var cs = PrisonCrates && File.Exists(crateAsset) ? ImportShipSprite(crateAsset, 624f / 2.5f, false, SpriteAlignment.BottomCenter) : null;
                if (cs != null)
                    foreach (var px in new[] { 560f, 1050f })
                    {
                        float cyRow = Mathf.Lerp(lib.maxDepthY, lib.minDepthY, 0.55f), cx = wx(px);
                        var cg = new GameObject("Suojalaatikko");
                        cg.transform.SetParent(root.transform, false); cg.transform.position = new Vector3(cx, cyRow, 0f);
                        var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = cs; csr.sortingOrder = Mathf.RoundToInt(-cyRow * 100f);
                        var cc = cg.AddComponent<CoverCrate>(); cc.height = 2.5f; cc.coverHalfWidth = 0.5f * cs.bounds.size.x - 0.25f;
                    }
            }
            var listPath = PrisonScenePath.Replace(".unity", ".areas.txt");
            var names = new System.Collections.Generic.List<string>(File.Exists(listPath) ? File.ReadAllLines(listPath) : new string[0]);
            if (!names.Contains("Vankilan kirjasto")) names.Add("Vankilan kirjasto");
            File.WriteAllLines(listPath, names);
            EditorUtility.SetDirty(lib);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan kirjasto ja kanttiini rakennettu (x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + "): kuntopihan oikea kaksoisovi vie vasempaan oveen (ja takaisin), oikea ovi lukossa kunnes valikko 362 liittää henkilökunnan tilat. " + sohviInfo + ". Kaksi suojalaatikkoa. Ajamatta Unityssä.");
        }
        finally
        {
            if (openedAlpine && alpine.IsValid()) EditorSceneManager.CloseScene(alpine, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
