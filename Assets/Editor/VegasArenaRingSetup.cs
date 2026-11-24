using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Valikko 434: WTF-areenan katsomo ja kehä, alue "WTF-areenan katsomo" (x 127000). Kolme 5120x2880-kuvaa peräkkäin:
/// 1 katsomo ja käytävä (vasemmalla verhoilla oviaukko + EXIT -> aulaan), 2 kehän vasen puoli, 3 kehän oikea puoli.
/// Kuvat on zoomattu ArenaZoom-kertaisesti: ruudun alareuna = kuvan alareuna, kattoristikon yläosa rajautuu pois.
/// Yleisö kannustaa silmukkana (vegas_areena_yleiso_N.png, Tools/ArenaAssets/make_arena_crowd.py: videoista vain liikkuva yleisö
/// masterin päälle), ääni Resources/Ambienssi/wtf_areenan_katsomo.wav (AmbiencePlayer hakee alueen nimellä).
/// Kävely: lattia katsomon kaiteen / kehän esiliinan edessä. Aulan "SEATING / ARENA" -ovi avataan tänne. Aja ensin 431.
/// </summary>
public static partial class BeatEmUpSetup
{
    const float VegasArenaRingX0 = 127000f, ArenaZoom = 1.2f, ArenaCrowdFps = 12f;
    const float ArenaPanelStepPx = 4940f;   // kuvissa 180 px päällekkäisyys: kuvat kohtiin x = 0, 4940, 9880 (yhteiset alueet pikselilleen samat)
    const string ArenaRingAreaName = "WTF-areenan katsomo";
    // kehä katsomon kuvissa 2–3 (px): lattian taka- ja etureuna, esiliinan alareuna (lattian takaraja), lattian x-rajat (kuva 2 vasen, kuva 3 oikea, pylväiden sisäpuolella)
    const float ArenaMatBackRow = 1605f, ArenaMatFrontRow = 1925f, ArenaApronRow = 2290f;
    static readonly Vector2 ArenaMatPx = new Vector2(1520f, 3680f), ArenaRingZonePx = new Vector2(770f, 3990f);

    static Sprite[] SliceCrowdSheet(string path, int cw, int ch, int cols, int count, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu; ti.filterMode = FilterMode.Bilinear; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
        ti.maxTextureSize = 8192; ti.textureCompression = TextureImporterCompression.CompressedHQ;   // 3 arkkia ~8000x3000: pakattuna ~75 Mt
        ti.SaveAndReimport();
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti); dp.InitSpriteEditorDataProvider();
        var oldIds = dp.GetSpriteRects().GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.First().spriteID);
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new List<SpriteRect>();
        for (int k = 0; k < count; k++)
        {
            int r = k / cols, c = k % cols;
            string n = $"{baseName}_{k:000}";
            rects.Add(new SpriteRect
            {
                name = n, rect = new Rect(c * cw, h - (r + 1) * ch, cw, ch),
                alignment = SpriteAlignment.TopLeft, pivot = new Vector2(0f, 1f),
                spriteID = oldIds.TryGetValue(n, out GUID id) ? id : GUID.Generate(),
            });
        }
        dp.SetSpriteRects(rects.ToArray());
        dp.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        dp.Apply(); ti.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    [MenuItem("Beat em up/434. Las Vegas: WTF-areenan katsomo ja kehä (yleisö kannustaa, aulan ovi auki)", false, 434)]
    static void AddVegasArenaRing()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        string metaPath = T + "vegas_areena_yleiso.txt";
        for (int i = 1; i <= 3; i++)
            foreach (var f in new[] { "vegas_areena_katsomo_" + i, "vegas_areena_yleiso_" + i })
                if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat). Aja Tools/ArenaAssets/make_arena_crowd.py."); return; }
        if (!File.Exists(metaPath)) { Info("Puuttuu " + metaPath + " (make_arena_crowd.py)."); return; }
        var meta = File.ReadAllLines(metaPath).Where(l => l.Trim().Length > 0).Select(l => l.Split(' ')).ToArray();
        if (meta.Length < 3) { Info("vegas_areena_yleiso.txt: odotettiin 3 riviä."); return; }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var lobby = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "WTF-areenan aula");
            var seatDoor = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d.gameObject.scene == scene && d.gameObject.name == "Vegas: areenan katsomon ovi");
            if (lobby == null || seatDoor == null) { Info("WTF-areenan aula tai sen katsomon ovi puuttuu: aja valikko 431."); return; }
            foreach (var n in new[] { ArenaRingAreaName, "WTF-areenan katsomon ovet" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, cy = CamY, X0 = VegasArenaRingX0;
            float W = 1672f / (941f / H);                  // ruudun leveys
            float imgH = ArenaZoom * H, ppu = 2880f / imgH, imgW = 5120f / ppu, step = ArenaPanelStepPx / ppu, totalW = 2f * step + imgW;
            float bottom = cy - CamHalf, top = bottom + imgH;   // kuvan alareuna ruudun alareunassa
            float PX(int img, float px) => X0 + img * step + px / ppu;
            float PY(float py) => top - py / ppu;

            var root = new GameObject(ArenaRingAreaName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, ArenaRingAreaName);
            var area = ReuseArea("Alue: " + ArenaRingAreaName);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, scene);
            area.areaName = ArenaRingAreaName; area.useSidewalk = false;
            area.maxDepthY = PY(2300f); area.minDepthY = bottom + 0.25f;
            area.walkMinX = PX(0, 120f); area.walkMaxX = PX(2, 5040f);
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + totalW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f;
            // takaraja: katsomon kaide (kuva 1, rivi ~1990), kehän portaat ja esiliina (kuvat 2–3, rivi ~2290), oikealla aidan edessä taas syvempi.
            // Kehän kohdalla (SiteLevels-vyöhyke) raja nousee kehän lattian takareunaan: kerrokset pitävät hahmot joko lattialla tai kehässä.
            float zoneMin = PX(1, ArenaRingZonePx.x), zoneMax = PX(2, ArenaRingZonePx.y);
            area.depthLimits = new[]
            {
                new Vector2(PX(0, 0f), PY(1990f)), new Vector2(PX(1, 600f), PY(1990f)), new Vector2(PX(1, 760f), PY(2280f)),
                new Vector2(zoneMin - 0.01f, PY(2280f)), new Vector2(zoneMin, PY(ArenaMatBackRow)), new Vector2(zoneMax, PY(ArenaMatBackRow)), new Vector2(zoneMax + 0.01f, PY(2310f)),
                new Vector2(PX(2, 4120f), PY(1990f)), new Vector2(PX(2, 5120f), PY(1990f)),
            };
            area.depthLimitsExceedMax = true;
            EditorUtility.SetDirty(area);

            // kehä: kerros 1 (lattia = kerros 0 esiliinan edessä), portaat punaisen kulman vasemmalla puolella
            var sl = root.AddComponent<SiteLevels>();
            float floorMax = PY(ArenaApronRow), matY = PY(0.5f * (ArenaMatBackRow + ArenaMatFrontRow));
            sl.area = area; sl.levelY = new[] { floorMax, matY }; sl.streetMin = area.minDepthY; sl.streetMax = floorMax;
            sl.bandHalf = 0.5f * (ArenaMatFrontRow - ArenaMatBackRow) / ppu;
            sl.levelCamOffset = new[] { 0f, 0f };
            sl.zoneMinX = zoneMin; sl.zoneMaxX = zoneMax; sl.siteMinX = zoneMin; sl.siteMaxX = zoneMax;
            sl.levelMinX = new[] { 0f, PX(1, ArenaMatPx.x) }; sl.levelMaxX = new[] { 0f, PX(2, ArenaMatPx.y) };
            sl.edgeDrop = true; sl.noDropMin = new[] { false, true }; sl.noDropMax = new[] { false, true };   // köydet sivuilla; edestä voi hypätä alas (alas pohjassa)
            sl.climbSpeed = 3.6f;
            sl.ladders = new[] { new SiteLevels.Ladder { x = PX(1, 1060f), topX = PX(1, ArenaMatPx.x + 10f), useTop = true, lo = 0, hi = 1 } };

            // kehän etuosa (kulmapylväät, pehmusteet, etu- ja sivuköydet) kehässä olevien eteen; lattialla kulkevat piirtyvät sen eteen
            const string frontPath = T + "vegas_areena_keha_etu.png";
            if (File.Exists(frontPath) && File.Exists(T + "vegas_areena_keha_etu.txt"))
            {
                var fm = File.ReadAllText(T + "vegas_areena_keha_etu.txt").Trim().Split(' ');
                float fx = float.Parse(fm[0], System.Globalization.CultureInfo.InvariantCulture), fy = float.Parse(fm[1], System.Globalization.CultureInfo.InvariantCulture);
                var fsp = ImportShipSprite(frontPath, ppu, false, SpriteAlignment.TopLeft);
                if (fsp != null)
                {
                    var fsr = new GameObject("Kehän etuosa").AddComponent<SpriteRenderer>();
                    fsr.transform.SetParent(root.transform, false); fsr.sprite = fsp;
                    fsr.transform.position = new Vector3(PX(0, fx), PY(fy), 0f);
                    fsr.sortingOrder = Mathf.RoundToInt(-PY(ArenaApronRow) * 100f);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                var sp = ImportShipSprite(T + "vegas_areena_katsomo_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan tuonti epäonnistui: vegas_areena_katsomo_" + (i + 1)); return; }
                var sr = new GameObject("Areena " + (i + 1)).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = -10000;
                sr.transform.position = new Vector3(X0 + step * i + imgW * 0.5f, bottom + imgH * 0.5f, 0f);

                // yleisö: kuvasarja masterin päällä, solun vasen yläkulma kohdassa (x, y) masterin pikseleinä
                var m = meta[i];
                int cw = int.Parse(m[0]), ch = int.Parse(m[1]), cols = int.Parse(m[2]), count = int.Parse(m[3]);
                float mx = float.Parse(m[4], System.Globalization.CultureInfo.InvariantCulture), my = float.Parse(m[5], System.Globalization.CultureInfo.InvariantCulture);
                float sc = float.Parse(m[6], System.Globalization.CultureInfo.InvariantCulture);
                var frames = SliceCrowdSheet(T + "vegas_areena_yleiso_" + (i + 1) + ".png", cw, ch, cols, count, ppu / sc);
                if (frames == null || frames.Length < 2) { Info("Yleisön kuvasarja ei leikkaudu: vegas_areena_yleiso_" + (i + 1)); return; }
                var cg = new GameObject("Yleisö " + (i + 1));
                cg.transform.SetParent(root.transform, false);
                cg.transform.position = new Vector3(PX(i, mx), PY(my), 0f);
                var csr = cg.AddComponent<SpriteRenderer>(); csr.sprite = frames[0]; csr.sortingOrder = -9990;
                var loop = cg.AddComponent<SpriteLoop>(); loop.frames = frames; loop.fps = ArenaCrowdFps;
            }

            // ovet: aulan katsomon ovi auki tänne, kuvan 1 verhojen välinen oviaukko (EXIT) takaisin aulaan
            var doorRoot = new GameObject("WTF-areenan katsomon ovet");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, scene);
            Undo.RegisterCreatedObjectUndo(doorRoot, "WTF-areenan katsomon ovet");
            float exitX = PX(0, 500f);
            Vector2 At(Area a, float x) => new Vector2(x, Mathf.Lerp(a.MaxDepthAtX(x, a.maxDepthY), a.minDepthY, 0.12f));
            var back = new GameObject("Vegas: katsomosta aulaan").AddComponent<Door>();
            back.transform.SetParent(doorRoot.transform, false); back.transform.position = new Vector3(exitX, area.MaxDepthAtX(exitX, area.maxDepthY) - 0.3f, 0f);
            back.prompt = "Mene takaisin aulaan"; back.here = area; back.target = lobby; back.spawnPoint = At(lobby, seatDoor.transform.position.x);
            back.halfWidth = 1.6f; back.maxDistanceFromWall = 2.4f; back.onGround = true; back.groundHalfDepth = 1.6f;
            Undo.RecordObject(seatDoor, "Katsomon ovi auki");
            seatDoor.target = area; seatDoor.spawnPoint = At(area, exitX + 2.2f); seatDoor.requiredClue = ""; seatDoor.prompt = "Mene katsomoon";
            EditorUtility.SetDirty(seatDoor);

            var areasPath = VegasScenePath.Replace(".unity", ".areas.txt");
            var names = File.Exists(areasPath) ? File.ReadAllLines(areasPath).ToList() : new List<string>();
            if (!names.Contains(ArenaRingAreaName)) { names.Add(ArenaRingAreaName); File.WriteAllLines(areasPath, names); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("WTF-areenan katsomo ja kehä rakennettu (x " + X0.ToString("0") + "–" + (X0 + totalW).ToString("0") + ", zoom " + ArenaZoom + "): yleisö kannustaa " + ArenaCrowdFps + " fps, ääni Resources/Ambienssi/wtf_areenan_katsomo.wav.\nAulan katsomon ovi on nyt auki; katsomon vasen oviaukko (EXIT) vie takaisin aulaan.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
