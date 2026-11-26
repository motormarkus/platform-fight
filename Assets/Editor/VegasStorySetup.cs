using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valikko 436: Vegasin juonen loppuosa.
///  - Baari: Violetti kiittää häiden estämisestä (halaus, 1000 mk, lisätasku = 3. eväspaikka), Candi kertoo Shadow Innistä -> "Mene motellille".
///  - Motelli (Shadow Inn): Reginat, Lippikset ja Betty pihalla; kun kaikki kaatuvat -> "Mene huoneeseen 203" (ovi aukeaa).
///  - Huone 203: lisää Reginoita ja Lippiksiä; viimeinen pudottaa kassakaapin avaimen; kassakaappi takaseinällä (avautuu animaationa): 1500 mk + lippu illan otteluun.
///  - WTF-areenan katsomon ovi vaatii lipun; kehän voitosta Muurin lippu (RingMatch).
/// Aja ensin 379 (baari), 386 (Violetti), 403–404 (motelli ja huoneet), 431–435 (areena). Korvaa valikon 433 Reginat.
/// </summary>
public static partial class BeatEmUpSetup
{
    public const string VegasFightTicket = "lippu_wtf_ottelu", VegasFightTicketLocked = "Tarvitset lipun illan otteluun.";
    public const string VegasVioletThanks = "violetti_kiitos", VegasMotelTask = "tehtava_motelli", VegasMotelYard = "motelli_piha", VegasSafeKey = "motelli_kassakaapin_avain";
    const int VegasStoryRoom = 203;

    [MenuItem("Beat em up/436. Las Vegas: juoni (Violetin kiitos, Candi, motellin jengi, huone 203, kassakaappi, ottelulippu)", false, 436)]
    static void AddVegasStory()
    {
        if (PlayModeBlocked()) return;
        var regina = LoadReginaSheets(out string missing);
        if (regina == null) { Info("Puuttuu tai ei leikkaudu: " + missing + " (Assets/Sprites/Viholliset)."); return; }
        var candiSheets = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "violetti_candi_puhe", "violetti_candi_kavely" })
        {
            var sp = LoadWalkerSprites(n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu " + n + ".png (Assets/Sprites/Viholliset)."); return; }
            candiSheets[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        if (!File.Exists(ThaitownScenePath)) { Info("Thaitown-scene puuttuu (Lippiksen malli): aja valikko 201."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var thai = EditorSceneManager.GetSceneByPath(ThaitownScenePath);
        bool thaiOpened = false;
        if (!thai.IsValid() || !thai.isLoaded) { thai = EditorSceneManager.OpenScene(ThaitownScenePath, OpenSceneMode.Additive); thaiOpened = true; }
        try
        {
            Area FindArea(string n) => Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == n);
            Door FindDoor(string n) => Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(d => d != null && d.gameObject.scene == scene && d.gameObject.name == n);
            var bar = FindArea("Vegasin baari");
            var street = FindArea("Vegasin katu");
            var room = FindArea("Motellihuone " + VegasStoryRoom);
            var motelRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin motelli");
            var motel = motelRoot != null ? motelRoot.GetComponent<SiteLevels>() : null;
            var violet = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin Violetti");
            if (bar == null || violet == null) { Info("Baari tai baarin Violetti puuttuu: aja valikot 379 ja 386."); return; }
            if (street == null || motel == null || room == null) { Info("Motelli tai huone " + VegasStoryRoom + " puuttuu: aja valikot 403 ja 404."); return; }
            var lippisTmpl = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e != null && e.gameObject.scene == thai && e.displayName == "Lippis");
            if (lippisTmpl == null) { Info("Lippistä ei löytynyt Thaitownista (malli)."); return; }
            var report = new List<string>();

            float H = 2f * CamHalf, imgW = 5120f / (1844f / H);

            // ---------------- baari: Violetin kiitos ----------------
            foreach (var old in violet.GetComponents<NpcTalk>()) Object.DestroyImmediate(old);
            var vt = violet.AddComponent<NpcTalk>();
            vt.area = bar; vt.speaker = "Violetti"; vt.prompt = "Puhu Violetille";
            vt.requiredClue = ChapelAftermath.NextId; vt.doneClue = VegasVioletThanks; vt.doneTitle = "Violetin kiitos";
            vt.doneText = "Violetti kiitti häiden estämisestä halaamalla ja antoi 1000 mk sekä uuden taskun.";
            vt.pages = new[]
            {
                "{hero}! Sinä teit sen! Siskoni on turvassa, ja Donin suunnitelma meni myttyyn.",
                "En tiedä, miten voisin ikinä kiittää sinua tarpeeksi…",
                "Ota nämä: 1000 markkaa ja tämä tasku. Siihen mahtuu yksi eväs lisää. Kiitos, {hero}!",
            };
            vt.hug = true; vt.rewardMoney = 1000;
            vt.extraClue = PlayerController.ExtraPocketId; vt.extraTitle = "Violetin tasku"; vt.extraText = "Violetin lahja: taskuun mahtuu nyt kolme evästä ja Bööni.";
            vt.popupText = "UUSI TASKU: 3 EVÄSPAIKKAA";
            vt.repeatLine = "Kiitos vielä kerran, {hero}. Candi tuolla halusi jutella kanssasi.";
            EditorUtility.SetDirty(vt);

            // ---------------- baari: Candi ----------------
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Candi").ToArray()) Undo.DestroyObjectImmediate(old);
            var cgo = new GameObject("Vegasin Candi");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cgo, scene);
            Undo.RegisterCreatedObjectUndo(cgo, "Vegasin Candi");
            float cx = VegasBarX0 + imgW * 1.95f, cy = Mathf.Lerp(bar.MaxDepthAtX(cx, bar.maxDepthY), bar.MinDepthAtX(cx, bar.minDepthY), 0.5f);
            cgo.transform.position = new Vector3(cx, cy, 0f); cgo.transform.localScale = violet.transform.localScale;   // sama koko kuin Violetti
            var cvis = new GameObject("Visual").AddComponent<SpriteRenderer>(); cvis.transform.SetParent(cgo.transform, false); cvis.sprite = candiSheets["violetti_candi_puhe"][0];
            var csh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); csh.transform.SetParent(cgo.transform, false);
            csh.color = new Color(0f, 0f, 0f, 0.3f); csh.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            var cst = cgo.AddComponent<NpcStroller>();
            cst.area = bar; cst.body = cvis; cst.shadow = csh;
            cst.idleSprites = candiSheets["violetti_candi_puhe"]; cst.walkSprites = candiSheets["violetti_candi_kavely"]; cst.talkSprites = candiSheets["violetti_candi_puhe"];
            cst.idleFrameTime = 0.125f; cst.walkFrameTime = 0.1f; cst.talkFrameTime = 0.125f; cst.talkChance = 0.3f; cst.speed = 1.3f;
            cst.laneMinX = VegasBarX0 + imgW * 1.6f; cst.laneMaxX = VegasBarX0 + imgW * 2.3f;
            cst.idleSeconds = new Vector2(3f, 9f); cst.walkDistance = new Vector2(1.5f, 4f);
            var ct = cgo.AddComponent<NpcTalk>();
            ct.area = bar; ct.speaker = "Candi"; ct.prompt = "Puhu Candille";
            ct.requiredClue = ChapelAftermath.NextId; ct.doneClue = VegasMotelTask; ct.doneTitle = "Mene motellille";
            ct.doneText = "Candi: Shadow Inn -motelli on jengin hallinnassa, ja sieltä hallitaan Vegasin rikollisuutta. Motelli on kadulla työmaan ja konttorin takana. Candin pitäisi päästä siskonsa luo.";
            ct.pages = new[]
            {
                "Hei {hero}, Shadow Inn on jengin hallinnassa, eikä siitä pääse ohi. Voisitko auttaa? Minun pitäisi päästä siskoni luo…",
                "Siellä on jengin päämaja. Sieltä hallitaan koko Vegasin rikollisuutta. Motelli on kadulla, työmaan ja konttorin takana.",
            };
            ct.bannerTitle = "Mene motellille"; ct.bannerInfo = "Shadow Inn on kadulla työmaan ja konttorin takana";
            ct.repeatLine = "Ole varovainen Shadow Innissä, {hero}. Siellä on paljon jengiläisiä.";
            EditorUtility.SetDirty(ct);

            // ---------------- motellin piha ----------------
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Reginat" || g.name == "Motellin jengi" || g.name == "Huoneen " + VegasStoryRoom + " jengi").ToArray()) Undo.DestroyObjectImmediate(old);
            var yardRoot = new GameObject("Motellin jengi");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(yardRoot, scene);
            Undo.RegisterCreatedObjectUndo(yardRoot, "Motellin jengi");
            int nR = 0, nL = 0;
            Enemy Lippis(Transform parent, Vector3 pos)
            {
                var go = Object.Instantiate(lippisTmpl.gameObject);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetParent(parent, false); go.SetActive(true);
                go.name = "Lippis (motelli " + (++nL) + ")";
                var e = go.GetComponent<Enemy>();
                e.wanderMinX = 0f; e.wanderMaxX = 0f; e.idleFacing = 0; e.duelFoe = null;
                go.transform.position = pos;
                EditorUtility.SetDirty(e);
                return e;
            }
            Vector3 StreetSpot(float fr, float depth)
            {
                float x = Mathf.Lerp(motel.zoneMinX, motel.zoneMaxX, fr);
                float yMax = street.MaxDepthAtX(x, street.maxDepthY), yMin = street.MinDepthAtX(x, street.minDepthY);
                float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f);
                return FreeSpot(street, scene, new Vector3(x, y, 0f));
            }
            var yard = new List<Enemy>
            {
                BuildRegina(regina, yardRoot.transform, "Regina (motelli " + (++nR) + ")", StreetSpot(0.22f, 0.70f)),
                Lippis(yardRoot.transform, StreetSpot(0.34f, 0.85f)),
                BuildRegina(regina, yardRoot.transform, "Regina (motelli " + (++nR) + ")", StreetSpot(0.48f, 0.78f)),
                Lippis(yardRoot.transform, StreetSpot(0.60f, 0.65f)),
            };
            var bettyReport = new List<string>();
            var betty = MakeBetty(yardRoot.transform, bettyReport);
            if (betty == null) report.AddRange(bettyReport);
            if (betty != null)
            {
                betty.name = "Betty (motelli)";
                betty.transform.position = StreetSpot(0.78f, 0.75f); betty.idleFacing = -1;
                EditorUtility.SetDirty(betty); yard.Add(betty);
            }
            var yw = yardRoot.AddComponent<ClearWatch>();
            yw.enemies = yard.ToArray();
            yw.clueId = VegasMotelYard; yw.clueTitle = "Shadow Innin piha";
            yw.clueText = "Motellin piha on tyhjennetty. Jengin päämaja on yläkerran huoneessa " + VegasStoryRoom + " (parveke, portaat ulkona).";
            yw.bannerTitle = "Piha tyhjä"; yw.bannerInfo = "Mene huoneeseen " + VegasStoryRoom + " (yläkerta)";
            EditorUtility.SetDirty(yw);

            var door = FindDoor("Motelli: ovi " + VegasStoryRoom + " sisään");
            if (door != null) { door.requiredClue = VegasMotelYard; door.lockedClueText = "Ovi on lukossa. Pihalla on vielä jengiläisiä."; EditorUtility.SetDirty(door); }
            else report.Add("Huoneen " + VegasStoryRoom + " ovea ei löytynyt (aja 404).");

            // ---------------- huone 203 ----------------
            var roomRoot = new GameObject("Huoneen " + VegasStoryRoom + " jengi");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(roomRoot, scene);
            Undo.RegisterCreatedObjectUndo(roomRoot, "Huoneen jengi");
            float rppu = 1844f / H, rx0 = room.walkMinX - imgW * 0.05f;   // huoneen kuvan vasen reuna (404: walkMinX = X0 + 5 % kuvasta)
            Vector3 RoomSpot(float px, float depth)
            {
                float x = rx0 + px / rppu;
                float y = Mathf.Lerp(room.MaxDepthAtX(x, room.maxDepthY), room.MinDepthAtX(x, room.minDepthY), depth);
                return FreeSpot(room, scene, new Vector3(x, y, 0f));
            }
            var inside = new List<Enemy>
            {
                BuildRegina(regina, roomRoot.transform, "Regina (huone " + VegasStoryRoom + " 1)", RoomSpot(4300f, 0.35f)),
                Lippis(roomRoot.transform, RoomSpot(5300f, 0.70f)),
                BuildRegina(regina, roomRoot.transform, "Regina (huone " + VegasStoryRoom + " 2)", RoomSpot(6300f, 0.55f)),
                Lippis(roomRoot.transform, RoomSpot(7400f, 0.30f)),
                BuildRegina(regina, roomRoot.transform, "Regina (huone " + VegasStoryRoom + " 3)", RoomSpot(8300f, 0.75f)),
            };
            foreach (var e in inside) { e.wakeDistance = 30f; e.idleFacing = -1; EditorUtility.SetDirty(e); }
            var rw = roomRoot.AddComponent<ClearWatch>();
            rw.enemies = inside.ToArray();
            rw.clueId = VegasSafeKey; rw.clueTitle = "Kassakaapin avain"; rw.notify = true;
            rw.clueText = "Viimeisen jengiläisen taskusta putosi kassakaapin avain. Kassakaappi on huoneen " + VegasStoryRoom + " takaseinällä.";
            rw.dropPopup = "KASSAKAAPIN AVAIN";
            rw.bannerTitle = "Kassakaapin avain"; rw.bannerInfo = "Avaa kassakaappi";
            EditorUtility.SetDirty(rw);

            // kassakaappi (Assets/Sprites/Props/kassakaappi.png, 6 kuvaa: suljettu -> auki) takaseinän vieressä vaatekaapin ja keittiön välissä (kuvassa px ~4700)
            var safe = new GameObject("Kassakaappi (huone " + VegasStoryRoom + ")").AddComponent<ClueSpot>();
            safe.transform.SetParent(roomRoot.transform, false);
            float sx = rx0 + 4700f / rppu;
            float sy = room.MaxDepthAtX(sx, room.maxDepthY) - 0.12f;
            safe.transform.position = new Vector3(sx, sy, 0f);
            var safeFrames = ImportSafeFrames(rppu);
            if (safeFrames != null)
            {
                var ssr = new GameObject("Kuva").AddComponent<SpriteRenderer>();
                ssr.transform.SetParent(safe.transform, false); ssr.sprite = safeFrames[0];
                ssr.sortingOrder = Mathf.RoundToInt(-sy * 100f);
                safe.anim = ssr; safe.openFrames = safeFrames; safe.openFrameTime = 0.09f;
            }
            else report.Add("Kassakaapin kuva puuttuu (Assets/Sprites/Props/kassakaappi.png): aja Tools/MotelAssets/make_safe_sheet.py.");
            safe.halfDepth = 2.2f;
            safe.prompt = "Avaa kassakaappi"; safe.halfWidth = 1.6f;
            safe.requiredClue = VegasSafeKey; safe.lockedText = "Kassakaappi on lukossa. Avain on jollakin jengiläisellä.";
            safe.clueId = VegasFightTicket; safe.clueTitle = "Lippu illan otteluun";
            safe.clueText = "Kassakaapista löytyi 1500 mk ja lippu illan otteluun WTF-areenalle. Areena on kadulla; lippu avaa katsomon oven.";
            safe.money = 1500; safe.emptyText = "Kassakaappi on tyhjä.";
            EditorUtility.SetDirty(safe);

            // ---------------- areena: katsomon ovi vaatii lipun ----------------
            var seat = FindDoor("Vegas: areenan katsomon ovi");
            if (seat != null && seat.target != null) { seat.requiredClue = VegasFightTicket; seat.lockedClueText = VegasFightTicketLocked; EditorUtility.SetDirty(seat); }
            else report.Add("Areenan katsomon ovea ei löytynyt tai katsomoa ei ole rakennettu (aja 431 ja 434).");
            var match = Object.FindObjectsByType<RingMatch>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(m => m.gameObject.scene == scene);
            if (match != null) { match.dropWallTicket = true; EditorUtility.SetDirty(match); } else report.Add("Kehäottelua ei ole (aja 435): Muurin lippua ei pudoteta.");

            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vegasin juoni lisätty:\n" +
                 "• Baari: Violetti kiittää (halaus, 1000 mk, 3. eväspaikka) ja Candi kertoo Shadow Innistä -> \"Mene motellille\" (kumpikin häiden jälkeen).\n" +
                 "• Motellin piha: 2 Reginaa, 2 Lippistä" + (betty != null ? " ja Betty" : "") + "; kun kaikki kaatuvat, huoneen " + VegasStoryRoom + " ovi aukeaa.\n" +
                 "• Huone " + VegasStoryRoom + ": 3 Reginaa ja 2 Lippistä; viimeinen pudottaa kassakaapin avaimen. Kassakaappi (avautuu animaationa): 1500 mk + lippu illan otteluun.\n" +
                 "• Areenan katsomon ovi vaatii lipun; kehän voitosta Muurin lippu.\n" +
                 (report.Count > 0 ? "\nHuom: " + string.Join("\n", report) : ""));
        }
        finally
        {
            if (thaiOpened && thai.IsValid()) EditorSceneManager.CloseScene(thai, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
    /// Kassakaapin 6 kuvaa (464x424 vaakanauhana): pivot alhaalla keskellä, koko ~360 huoneen kuvan pikseliä korkea (n. ovenkahvan korkeus).
    static Sprite[] ImportSafeFrames(float roomPpu)
    {
        const string path = "Assets/Sprites/Props/kassakaappi.png";
        const int cw = 464, ch = 424, n = 6;
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Multiple; ti.alphaIsTransparency = true; ti.mipmapEnabled = false;
        ti.spritePixelsPerUnit = 410f * roomPpu / 360f;   // rungon korkeus ~410 px -> 360 huoneen kuvan px
        ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.maxTextureSize = 4096;
#pragma warning disable 618
        var metas = new SpriteMetaData[n];
        for (int i = 0; i < n; i++) metas[i] = new SpriteMetaData { name = "kassakaappi_" + i, rect = new Rect(i * cw, 0, cw, ch), alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(0.5f, 8f / ch) };
        ti.spritesheet = metas;
#pragma warning restore 618
        ti.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sp => int.Parse(sp.name.Substring(sp.name.LastIndexOf('_') + 1))).ToArray();
        return sprites.Length == n ? sprites : null;
    }
}
