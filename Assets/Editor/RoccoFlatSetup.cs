using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Roccon kämppä: pelin aloituskadun talon ovi (x ≈ 0.35, jossa peli alkaa) vie sisään. Kuvat rocco_kampa_vasen + rocco_kampa_aloitus (2600 x 1000, vasen jatkuu aloituksen vasemmalle puolelle).
/// Ovien välissä on myöhemmin käytävä (kuvat tulossa): silloin katu-ovi vie käytävään ja käytävän pää kämppään.</summary>
public static partial class BeatEmUpSetup
{
    const float RoccoFlatX0 = 59000f, RoccoFlatFloorPx = 715f, RoccoFlatZoom = 0.78f;   // pääsceneen (Bricktown 40000–60000), sama kuvasuhde kuin Sinin kämpällä
    const float RoccoFlatDoorPx = 2804f, RoccoStreetDoorX = 0.35f, RoccoCorrX0 = 59200f, RoccoCorrFloorPx = 738f, RoccoCorrDoorPx = 277f, RoccoElevatorPx = 4586f, RoccoTvPx = 952f, RoccoLaundryX0 = 59400f, RoccoLaundryFloorPx = 715f, RoccoLaundryElevPx = 702f, RoccoLaundryDoorPx = 2945f, RoccoLobbyX0 = 59600f, RoccoLobbyFloorPx = 735f, RoccoLobbyStreetPx = 182f, RoccoLobbyGreenPx = 1314f, RoccoLobbyElevPx = 2090f;   // käytävä: oven 313 ja hissin kohdat kuvassa (px)                       // kämpän ulko-ovi (kuvan px) ja katuoven x
    static readonly Vector2[] RoccoFlatWindowsPx = { new Vector2(611f, 328f), new Vector2(2600f + 1300f, 330f), new Vector2(2600f + 2480f, 300f) };   // läpinäkyvien ikkunoiden keskikohdat koko kämpän kuvassa (px; vasen kuva ensin: makuuhuoneen ikkuna, sitten aloituskuvan kaksi)

    [MenuItem("Beat em up/341. Roccon kämppä + 3. kerroksen käytävä (pelin aloitusovi kadulla)", false, 341)]
    public static void CreateRoccoFlat()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/", cityPath = "Assets/Sprites/Taustat/kampa_yo_kaupunki.png";
        string[] files = { "rocco_kampa_vasen", "rocco_kampa_aloitus" };
        foreach (var f in files) if (!System.IO.File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Katu");
        if (street == null) { Info("Katu-aluetta ei löytynyt: avaa SampleScene (Bricktown) ensin."); return; }
        var scene = street.gameObject.scene;
        foreach (var n in new[] { "Roccon kämppä", "Alue: Roccon kämppä", "Katu: Roccon kämppään", "Roccon kämpältä kadulle", "Roccon käytävä", "Alue: Roccon käytävä", "Käytävä: hissillä kadulle", "Käytävä: 313", "Roccon TV", "Roccon pyykkitupa", "Alue: Roccon pyykkitupa", "Käytävä: hissillä kellariin", "Pyykkitupa: hissi ylös", "Hissin valinta", "Roccon talon aula", "Alue: Roccon talon aula", "Aula: ulos kadulle", "Aula: hissi ylös", "Aula: kellarin ovi", "Pyykkitupa: ovi ylös", "Hissi: aula", "Hissi: 3. kerros", "Hissi: kellari", "Pyykkituvan jengi" })
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

        float ppu = 1000f / (2f * CamHalf) / ArcadeScale / RoccoFlatZoom;
        float camS = CamHalf * RoccoFlatZoom;
        float imgW = 2600f / ppu, hU = 1000f / ppu, halfW = camS * 16f / 9f;
        float top = CamY - camS + hU;
        float X(float px) => RoccoFlatX0 + px / ppu;
        float Y(float row) => top - row / ppu;
        Sprite Import(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { AssetDatabase.ImportAsset(path); ti = AssetImporter.GetAtPath(path) as TextureImporter; }
            if (ti == null) return null;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu; ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 4096;
            ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.SaveAndReimport();   // ikkunalasit ovat läpinäkyviä
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        var root = new GameObject("Roccon kämppä");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Roccon kämppä");
        var area = ReuseArea("Alue: Roccon kämppä");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
        area.areaName = "Roccon kämppä";
        float totalW = 2f * imgW;
        for (int i = 0; i < files.Length; i++)
        {
            var spr = Import(T + files[i] + ".png");
            if (spr == null) continue;
            var bg = new GameObject(files[i]); bg.transform.SetParent(root.transform, false);
            var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -10000;
            bg.transform.position = new Vector3(RoccoFlatX0 + imgW * (i + 0.5f), top - hU * 0.5f, 0f);
        }

        // tyhjät taulut aloituskuvan seinällä: omat kuvat rocco_kampa_taulu_1.png (vasen) ja _2.png (oikea) venytetään taulun sisäosaan (px: x, rivi, leveys, korkeus)
        var frames = new[] { new Rect(739f, 131f, 168f, 264f), new Rect(954f, 132f, 161f, 262f) };
        for (int i = 0; i < frames.Length; i++)
        {
            string fp = T + "rocco_kampa_taulu_" + (i + 1) + ".png";
            if (!System.IO.File.Exists(fp)) continue;
            var fs = Import(fp);
            if (fs == null) continue;
            var fr = frames[i];
            var go = new GameObject("Taulu " + (i + 1)); go.transform.SetParent(root.transform, false);
            var fsr = go.AddComponent<SpriteRenderer>(); fsr.sprite = fs; fsr.sortingOrder = -9990;
            go.transform.position = new Vector3(X(2600f + fr.x + fr.width * 0.5f), Y(fr.y + fr.height * 0.5f), 0f);
            go.transform.localScale = new Vector3(fr.width / fs.rect.width, fr.height / fs.rect.height, 1f);
        }

        // yökaupunki läpinäkyvien ikkunoiden takana (aloituskuvan ikkunat); liikkuu hitaasti kameraa vasten
        var cityTi = AssetImporter.GetAtPath(cityPath) as TextureImporter;
        if (cityTi != null)
        {
            cityTi.textureType = TextureImporterType.Sprite; cityTi.spriteImportMode = SpriteImportMode.Single;
            cityTi.spritePixelsPerUnit = ppu; cityTi.filterMode = FilterMode.Bilinear;
            cityTi.textureCompression = TextureImporterCompression.Uncompressed; cityTi.mipmapEnabled = false; cityTi.SaveAndReimport();
            var citySpr = AssetDatabase.LoadAssetAtPath<Sprite>(cityPath);
            for (int i = 0; i < RoccoFlatWindowsPx.Length; i++)
            {
                var winCenter = new Vector2(X(RoccoFlatWindowsPx[i].x), Y(RoccoFlatWindowsPx[i].y));
                var cg = new GameObject("Yökaupunki " + (i + 1)); cg.transform.SetParent(root.transform, false);
                var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = citySpr; csr.sortingOrder = -10200;
                cg.transform.position = new Vector3(winCenter.x, winCenter.y, 0f);
                var fx = cg.AddComponent<InteriorWindowParallax>();
                fx.area = area; fx.anchor = winCenter; fx.cameraReference = new Vector2(winCenter.x, CamY); fx.factor = 0.9f;
            }
        }

        area.useSidewalk = false;
        area.maxDepthY = Y(RoccoFlatFloorPx);
        area.minDepthY = CamY - camS + 0.9f;
        area.camSize = camS; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f;
        area.camMinX = RoccoFlatX0 + halfW;
        area.camMaxX = Mathf.Max(area.camMinX, RoccoFlatX0 + totalW - halfW);
        area.walkMinX = X(110f); area.walkMaxX = X(5200f - 110f);
        area.depthLimits = new Vector2[0];
        EditorUtility.SetDirty(area);

        // ovet: katu -> kämppä (aloituspaikan ovi) ja kämpän ulko-ovi -> katu
        // ---- 3. kerroksen käytävä: 313 (Roccon ovi) vasemmalla, hissi oikeassa päässä (vie kadulle); ovet 312, 311 ja 310 ovat vain kuvaa ----
        string[] cFiles = { "rocco_kaytava_vasen", "rocco_kaytava_oikea" };
        foreach (var f in cFiles) if (!System.IO.File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        var cRoot = new GameObject("Roccon käytävä");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cRoot, scene);
        Undo.RegisterCreatedObjectUndo(cRoot, "Roccon käytävä");
        var corr = ReuseArea("Alue: Roccon käytävä");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(corr.gameObject, scene);
        corr.areaName = "Roccon käytävä";
        for (int i = 0; i < cFiles.Length; i++)
        {
            var spr = Import(T + cFiles[i] + ".png");
            if (spr == null) continue;
            var bg = new GameObject(cFiles[i]); bg.transform.SetParent(cRoot.transform, false);
            var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -10000;
            bg.transform.position = new Vector3(RoccoCorrX0 + imgW * (i + 0.5f), top - hU * 0.5f, 0f);
        }
        float CX(float px) => RoccoCorrX0 + px / ppu;
        corr.useSidewalk = false;
        corr.maxDepthY = Y(RoccoCorrFloorPx);
        corr.minDepthY = CamY - camS + 0.9f;
        corr.camSize = camS; corr.camRiseY = 0f; corr.camOffsetY = 0f; corr.backScale = 1f;
        corr.camMinX = RoccoCorrX0 + halfW;
        corr.camMaxX = Mathf.Max(corr.camMinX, RoccoCorrX0 + totalW - halfW);
        corr.walkMinX = CX(110f); corr.walkMaxX = CX(5200f - 150f);
        corr.depthLimits = new Vector2[0];
        EditorUtility.SetDirty(corr);

        // ovet: katu <-> hissi, käytävä (313) <-> kämppä
        Door NewDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn, float half, float dist)
        {
            var d = new GameObject(name).AddComponent<Door>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(d.gameObject, scene);
            Undo.RegisterCreatedObjectUndo(d.gameObject, "Ovi");
            d.transform.position = pos; d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = half; d.maxDistanceFromWall = dist;
            EditorUtility.SetDirty(d);
            return d;
        }
        Vector2 CorrSpawn(float px) => new Vector2(CX(px), Mathf.Lerp(corr.maxDepthY, corr.minDepthY, 0.4f));
        // ---- rakennuksen aula (katuovi vasemmalla, hissi oikealla, vihreä ovi keskellä = kellarin ovi) ----
        if (!System.IO.File.Exists(T + "rocco_aula.png")) { Info("Puuttuu kuva rocco_aula.png (Assets/Sprites/Taustat)."); return; }
        var aRoot = new GameObject("Roccon talon aula");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(aRoot, scene);
        Undo.RegisterCreatedObjectUndo(aRoot, "Roccon talon aula");
        var lobby = ReuseArea("Alue: Roccon talon aula");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lobby.gameObject, scene);
        lobby.areaName = "Roccon talon aula";
        var lobbySpr = Import(T + "rocco_aula.png");
        if (lobbySpr != null)
        {
            var bg = new GameObject("rocco_aula"); bg.transform.SetParent(aRoot.transform, false);
            var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = lobbySpr; sr.sortingOrder = -10000;
            bg.transform.position = new Vector3(RoccoLobbyX0 + imgW * 0.5f, top - hU * 0.5f, 0f);
        }
        float AX(float px) => RoccoLobbyX0 + px / ppu;
        lobby.useSidewalk = false;
        lobby.maxDepthY = Y(RoccoLobbyFloorPx);
        lobby.minDepthY = CamY - camS + 0.9f;
        lobby.camSize = camS; lobby.camRiseY = 0f; lobby.camOffsetY = 0f; lobby.backScale = 1f;
        lobby.camMinX = RoccoLobbyX0 + halfW;
        lobby.camMaxX = Mathf.Max(lobby.camMinX, RoccoLobbyX0 + imgW - halfW);
        lobby.walkMinX = AX(110f); lobby.walkMaxX = AX(2600f - 110f);
        lobby.depthLimits = new Vector2[0];
        EditorUtility.SetDirty(lobby);
        Vector2 LobbySpawn(float px) => new Vector2(AX(px), Mathf.Lerp(lobby.maxDepthY, lobby.minDepthY, 0.4f));
        Vector2 StreetSpawn = new Vector2(RoccoStreetDoorX + 0.8f, Mathf.Lerp(street.maxDepthY, street.minDepthY, 0.35f));
        NewDoor("Katu: Roccon kämppään", "Mene sisään", street, lobby, new Vector3(RoccoStreetDoorX, -1.053077f, 0f), LobbySpawn(RoccoLobbyStreetPx + 190f), 1.2f, 1.2f);   // talon ovi: aulaan
        NewDoor("Aula: ulos kadulle", "Ulos kadulle", lobby, street, new Vector3(AX(RoccoLobbyStreetPx), lobby.maxDepthY, 0f), StreetSpawn, 1.6f, 3f);
        // ---- kellarin pyykkitupa: hissin (B) aula + pyykkitupa yhtenä alueena (aulan vihreä ovi vie tänne, kun LaundryQuest.Unlocked) ----
        foreach (var f in new[] { "pyykkitupa_hissi", "pyykkitupa_sali" }) if (!System.IO.File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        var lRoot = new GameObject("Roccon pyykkitupa");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lRoot, scene);
        Undo.RegisterCreatedObjectUndo(lRoot, "Roccon pyykkitupa");
        var laundry = ReuseArea("Alue: Roccon pyykkitupa");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(laundry.gameObject, scene);
        laundry.areaName = "Roccon pyykkitupa";
        var lFiles = new[] { "pyykkitupa_hissi", "pyykkitupa_sali" };
        for (int i = 0; i < lFiles.Length; i++)
        {
            var spr = Import(T + lFiles[i] + ".png");
            if (spr == null) continue;
            var bg = new GameObject(lFiles[i]); bg.transform.SetParent(lRoot.transform, false);
            var sr = bg.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = -10000;
            bg.transform.position = new Vector3(RoccoLaundryX0 + imgW * (i + 0.5f), top - hU * 0.5f, 0f);
        }
        float LX(float px) => RoccoLaundryX0 + px / ppu;
        laundry.useSidewalk = false;
        laundry.maxDepthY = Y(RoccoLaundryFloorPx);
        laundry.minDepthY = CamY - camS + 0.9f;
        laundry.camSize = camS; laundry.camRiseY = 0f; laundry.camOffsetY = 0f; laundry.backScale = 1f;
        laundry.camMinX = RoccoLaundryX0 + halfW;
        laundry.camMaxX = Mathf.Max(laundry.camMinX, RoccoLaundryX0 + totalW - halfW);
        laundry.walkMinX = LX(110f); laundry.walkMaxX = LX(5200f - 110f);
        laundry.depthLimits = new Vector2[0];
        EditorUtility.SetDirty(laundry);
        // pyykkituvan jengi: 6 Kovista, 4 Punkkaria, 4 Lippistä (kopioitu kadun vihuista; sali on kuvan oikea puolisko)
        {
            var gang = new GameObject("Pyykkituvan jengi");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gang, scene);
            Undo.RegisterCreatedObjectUndo(gang, "Pyykkituvan jengi");
            var allEn = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // Betty on Thaitown-scenessä: avataan se hetkeksi, jos mallia ei ole ladattuna
            var thaiScene = EditorSceneManager.GetSceneByPath(ThaitownScenePath);
            bool openedThai = false;
            if (!allEn.Any(en => en != null && en.displayName == "Betty" && en.gameObject.name == "Betty") && System.IO.File.Exists(ThaitownScenePath))
            {
                if (!thaiScene.IsValid() || !thaiScene.isLoaded) { thaiScene = EditorSceneManager.OpenScene(ThaitownScenePath, OpenSceneMode.Additive); openedThai = true; }
                allEn = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }
            Enemy Tmpl(string who) => allEn.FirstOrDefault(en => en != null && en.displayName == who && !en.gameObject.name.StartsWith("Pyykkitupa") && !en.gameObject.name.StartsWith("Baarin") && (who == "Betty" ? en.gameObject.name == "Betty" : en.gameObject.scene == scene));
            var order = new[] { "Kovis", "Lippis", "Kovis", "Punkkari", "Kovis", "Lippis", "Betty", "Punkkari", "Kovis", "Lippis", "Kovis", "Punkkari", "Lippis", "Kovis", "Punkkari" };
            var missing = new List<string>(); int made = 0; var gangList = new List<Enemy>();
            for (int i = 0; i < order.Length; i++)
            {
                var tm = Tmpl(order[i]);
                if (tm == null) { if (!missing.Contains(order[i])) missing.Add(order[i]); continue; }
                var e = Object.Instantiate(tm.gameObject, gang.transform).GetComponent<Enemy>();
                e.gameObject.name = "Pyykkitupa: " + order[i] + " " + (i + 1);
                float px = Mathf.Lerp(3000f, 4900f, i / (float)(order.Length - 1));
                float depth = (i * 0.37f) % 1f;
                e.transform.position = new Vector3(LX(px), Mathf.Lerp(laundry.maxDepthY - 0.15f, laundry.minDepthY + 0.2f, 0.15f + 0.7f * depth), 0f);
                e.wakeDistance = 16f; e.joinsFightWhenSquadComes = true;   // koko jengi heräämään yhtä aikaa
                e.wanderMinX = e.wanderMaxX = 0f;   // Bettyn (ja muiden) vanha vaellusalue on toisessa kentässä: ilman tätä vihu kävelee pois huoneesta
                e.gameObject.SetActive(true);
                gangList.Add(e);
                made++;
            }
            // Dinan äiti (Mama, Salsafestin ruokakoju): ilmestyy, kun jengi on kaatunut; kiittää ja antaa onnenamuletin
            var mamaFrames = LoadSellerFrames("mama_myyja");
            if (mamaFrames.Length > 1)
            {
                var mama = new GameObject("Pyykkitupa: Dinan äiti");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mama, scene);
                Undo.RegisterCreatedObjectUndo(mama, "Dinan äiti");
                mama.transform.SetParent(gang.transform, false);
                float my = Mathf.Lerp(laundry.maxDepthY, laundry.minDepthY, 0.45f);
                mama.transform.position = new Vector3(LX(RoccoLaundryDoorPx + 300f), my, 0f);
                mama.transform.localScale = Vector3.one * 1.05f;
                var msr = mama.AddComponent<SpriteRenderer>(); msr.sprite = mamaFrames[0]; msr.sortingOrder = Mathf.RoundToInt(-my * 100f);
                var lm = mama.AddComponent<LaundryMama>();
                lm.area = laundry; lm.gang = gangList.ToArray(); lm.body = msr; lm.frames = mamaFrames;
                EditorUtility.SetDirty(lm);
            }
            else Debug.LogWarning("Dinan äiti: kuvia mama_myyja ei löytynyt (Assets/Sprites/Myyjat).");
            if (openedThai && thaiScene.IsValid()) EditorSceneManager.CloseScene(thaiScene, true);
            if (missing.Count > 0) Debug.LogWarning("Pyykkituvan jengi: mallivihua ei löytynyt: " + string.Join(", ", missing));
            Debug.Log("Pyykkituvan jengi: " + made + " vihua.");
        }
        // hissit: eteen mentäessä E avaa kerrosvalinnan (1 aula / 3 asunnot / Kellari; oman kerroksen valinta = paikallaan)
        var floorAreas = new[] { lobby, corr, laundry };
        var floorSpawns = new[] { LobbySpawn(RoccoLobbyElevPx), CorrSpawn(RoccoElevatorPx + 20f), new Vector2(LX(RoccoLaundryElevPx), Mathf.Lerp(laundry.maxDepthY, laundry.minDepthY, 0.4f)) };
        ElevatorPanel NewElevator(string name, Area from, float x, int cur)
        {
            var go = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Hissi");
            go.transform.position = new Vector3(x, from.maxDepthY, 0f);
            var ep = go.AddComponent<ElevatorPanel>();
            ep.here = from; ep.current = cur; ep.floors = new Door[3];
            for (int f = 0; f < 3; f++)
            {
                if (f == cur) continue;
                var d = NewDoor(name + " > kerros " + (f + 1), "Hissi", from, floorAreas[f], go.transform.position, floorSpawns[f], 1.6f, 3f);
                d.manualOnly = true; d.fadeTime = 0.35f;
                d.transform.SetParent(go.transform, true);
                ep.floors[f] = d;
            }
            EditorUtility.SetDirty(ep);
            return ep;
        }
        NewElevator("Hissi: aula", lobby, AX(RoccoLobbyElevPx), 0);
        NewElevator("Hissi: 3. kerros", corr, CX(RoccoElevatorPx), 1);
        NewElevator("Hissi: kellari", laundry, LX(RoccoLaundryElevPx), 2);
        var cellarDoor = NewDoor("Aula: kellarin ovi", "Alas kellariin", lobby, laundry, new Vector3(AX(RoccoLobbyGreenPx), lobby.maxDepthY, 0f), new Vector2(LX(RoccoLaundryDoorPx + 130f), Mathf.Lerp(laundry.maxDepthY, laundry.minDepthY, 0.4f)), 1.4f, 3f);
        NewDoor("Pyykkitupa: ovi ylös", "Ylös aulaan", laundry, lobby, new Vector3(LX(RoccoLaundryDoorPx), laundry.maxDepthY, 0f), LobbySpawn(RoccoLobbyGreenPx + 130f), 1.4f, 3f);
        cellarDoor.gameObject.SetActive(false);   // auki vasta, kun pyykkitupa-tehtävä on käynnissä (LaundryQuest.Unlocked)
        var sw = new GameObject("Hissin valinta").AddComponent<ElevatorSwitch>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sw.gameObject, scene);
        Undo.RegisterCreatedObjectUndo(sw.gameObject, "Hissin valinta");
        sw.whenOpen = new[] { cellarDoor.gameObject };
        NewDoor("Käytävä: 313", "Mene kämppään", corr, area, new Vector3(CX(RoccoCorrDoorPx), corr.maxDepthY, 0f), new Vector2(X(RoccoFlatDoorPx + 150f), Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.4f)), 1.6f, 3f);
        NewDoor("Roccon kämpältä kadulle", "Ulos käytävään", area, corr, new Vector3(X(RoccoFlatDoorPx), area.maxDepthY, 0f), CorrSpawn(RoccoCorrDoorPx + 160f), 2f, 3f);
        // televisio (vasen kuva, px ~952): telkkarin luo mentäessä TV-lähetys (video) alkaa pyöriä ruudulla selostuksen ajan
        var tvGo = new GameObject("Roccon TV");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tvGo, scene);
        Undo.RegisterCreatedObjectUndo(tvGo, "Roccon TV");
        var rtv = tvGo.AddComponent<RoccoTv>(); rtv.area = area; rtv.tvX = X(RoccoTvPx); rtv.range = 2.2f;
        tvGo.transform.position = new Vector3(rtv.tvX, area.maxDepthY, 0f);
        EditorSceneManager.MarkSceneDirty(scene);
        Info("Roccon kämppä ja käytävä luotu (x " + RoccoFlatX0.ToString("0") + " ja " + RoccoCorrX0.ToString("0") + "). Katuovi x " + RoccoStreetDoorX.ToString("0.00") + " vie käytävään hissin eteen, hissi vie takaisin kadulle, ovi 313 (vasemmalla) vie kämppään ja kämpän ovi takaisin 313:n eteen.\n\nTallenna scene (Ctrl+S).");
    }

    [MenuItem("Beat em up/342. Pyykkitupa auki / kiinni (testi, pelin aikana: aulan vihreä ovi vie kellariin)", false, 342)]
    static void ToggleLaundryTest()
    {
        if (!EditorApplication.isPlaying) { Info("Aja pelin aikana. Aulan vihreä ovi vie kellarin pyykkituvalle, kun tämä on päällä (sivutehtävä avaa sen myöhemmin)."); return; }
        LaundryQuest.Unlocked = !LaundryQuest.Unlocked;
        Info("Pyykkitupa " + (LaundryQuest.Unlocked ? "AUKI: aulan vihreä ovi vie kellariin." : "KIINNI: kellarin ovi on pois käytöstä."));
    }
}
