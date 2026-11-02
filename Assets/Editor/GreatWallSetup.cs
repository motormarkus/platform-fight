using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Kiinan muuri (oma scene Assets/Scenes/Areas/Muuri.unity, x 76200…77150): kiitotie (loppuu terminaalitorniin), terminaalin sisätila ja muurin alku. Kuvat Assets/Sprites/Taustat/muuri_*.png.
/// Terminaalissa Sohvi tiskin takana (kauppa), nuudelipöydät, pullot ja väki; ovet: kiitotie ↔ terminaali ↔ muuri. Portaat ylös -kuvat tulevat myöhemmin.</summary>
public static partial class BeatEmUpSetup
{
    const float WallRunX0 = 76200f, WallTermX0 = 76400f, WallX0 = 77000f;   // vapaa väli 76100–77990 (Kopteri 76060 asti, Aavikko alkaa 77998)
    const float WallPawnX0 = 76600f, WallElevX0 = 76800f, WallMoonX0 = 77300f, WallSurfX0 = 77450f, WallLabX0 = 77800f;
    const float SurfBasePx = 2385f;   // kuun pinnan vasemmassa päässä oleva tukikohdan ulkokuva (muuri_kuuasema_ulko, leveys px); loput kuun pinnasta alkavat tämän jälkeen
    const string WallScenePath = "Assets/Scenes/Areas/Muuri.unity";
    static readonly string[] WallAreaNames = { "Muurin kiitotie", "Muurin terminaali", "Muurin kanikonttori", "Muuri", "Muurin kuuhissi", "Muurin kuuasema", "Muurin kuun pinta", "Muurin laboratorio" };

    [MenuItem("Beat em up/327. Kiinan muuri: oma scene (kiitotie, terminaali, yläkerta, muuri, kuuhissi)", false, 327)]
    static void AddGreatWall()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var f in new[] { "muuri_kiitotie_torni", "muuri_terminaali_pano", "muuri_terminaali_tiski", "muuri_alku_torni", "muuri_main", "muuri_main_peili", "muuri_front", "muuri_front_peili", "muuri_far_x2", "muuri_mid_x2", "muuri_yla_pano", "muuri_yla_tiski", "muuri_rotko", "muuri_lankku", "muuri_hissi_paaty", "muuri_hissi_sisa", "muuri_taivas_pysty", "muuri_kuu_pinta", "muuri_kuuasema_pano", "muuri_kuuasema_tiski", "muuri_avaruus", "muuri_kuupinta_a", "muuri_kuupinta_b", "muuri_kuupinta_far", "muuri_kuupinta_tahdet", "muuri_kuuasema_ulko", "muuri_laboratorio_pano" })
            if (!File.Exists(T + f + ".png")) { Info("Puuttuu kuva " + f + ".png (Assets/Sprites/Taustat)."); return; }
        var opened = new List<UnityEngine.SceneManagement.Scene>();
        UnityEngine.SceneManagement.Scene Open(string path)
        {
            if (!File.Exists(path)) return default;
            var sc = EditorSceneManager.GetSceneByPath(path);
            if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(sc); }
            return sc;
        }
        try
        {
            UnityEngine.SceneManagement.Scene jungle;   // = Muuri-scene (nimi säilytetty muun koodin vuoksi)
            if (File.Exists(WallScenePath))
            {
                jungle = EditorSceneManager.GetSceneByPath(WallScenePath);
                if (!jungle.IsValid() || !jungle.isLoaded) { jungle = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); opened.Add(jungle); }
            }
            else
            {
                jungle = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); opened.Add(jungle);
                if (!EditorSceneManager.SaveScene(jungle, WallScenePath)) { Info("Scenen luonti epäonnistui: " + WallScenePath); return; }
            }
            var jungleTpl = Open(JungleScenePath);   // vain mallien lähteenä (esim. viholliset)
            var ucco = Open(UccoLaivaScenePath); var alp = Open(AlpineScenePathNow); var thai = Open(ThaitownScenePath);
            var sample = Open("Assets/Scenes/SampleScene.unity"); var desert = Open("Assets/Scenes/Areas/Aavikko.unity");
            var report = new List<string>();
            foreach (var n in new[] { "Muurin kiitotie", "Muurin terminaali", "Muuri", "Muurin ovet", "Alue: Muurin kiitotie", "Alue: Muurin terminaali", "Alue: Muuri", "Muurin kuuhissi", "Alue: Muurin kuuhissi", "Muurin laboratorio", "Alue: Muurin laboratorio", "Muurin kuuasema", "Alue: Muurin kuuasema", "Muurin kuun pinta", "Alue: Muurin kuun pinta", "Muurin kuuaseman sisus", "Muurin kanikonttori", "Alue: Muurin kanikonttori", "Muurin terminaalin sisus", "Muurin terminaalin väki", "Muurin terminaalin tappelu" })
                foreach (var o in jungle.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float ppu = 941f / (2f * CamHalf), H = 941f / ppu, W = 1672f / ppu;
            float bottom = CamY - CamHalf, top = bottom + H, cy = CamY;
            GameObject NewRoot(string n)
            {
                var g = new GameObject(n);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(g, jungle);
                Undo.RegisterCreatedObjectUndo(g, "Kiinan muuri");
                return g;
            }
            SpriteRenderer Put(Transform parent, string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(parent, false);
                sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos;
                return sr;
            }
            Area MakeArea(string goName, string areaName, float x0, float wallPx, float widthPx)
            {
                var a = ReuseArea(goName);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a.gameObject, jungle);
                a.areaName = areaName; a.useSidewalk = false;
                a.maxDepthY = top - wallPx / ppu; a.minDepthY = bottom + 0.9f;
                a.walkMinX = x0 + 1.5f; a.walkMaxX = x0 + widthPx / ppu - 1.2f;
                a.camMinX = x0 + W * 0.5f; a.camMaxX = Mathf.Max(a.camMinX, x0 + widthPx / ppu - W * 0.5f);
                a.camSize = CamHalf; a.camRiseY = 0f; a.camOffsetY = 0f; a.backScale = 1f; a.depthLimits = new Vector2[0];
                a.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Viidakko.mp3"); a.musicGain = a.music != null ? JungleMusicGain : 1f;
                return a;
            }
            // yksi kuva (leveys px) alueen vasemmasta reunasta alkaen
            SpriteRenderer Image(Transform parent, string name, string file, float x0, float leftPx, int order)
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Debug.LogWarning("Kiinan muuri: kuvan tuonti epäonnistui: " + file); return null; }
                return Put(parent, name, sp, new Vector3(x0 + leftPx / ppu + sp.bounds.size.x * 0.5f, cy, 0f), order);
            }
            void Para(Transform parent, Area area, string name, string file, float factor, int order, float dy = 0f)
            {
                var sp = ImportShipSprite(T + file + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) return;
                float left = area.camMinX - W * 0.5f;
                var sr = Put(parent, name, sp, new Vector3(left + sp.bounds.size.x * 0.5f, cy + dy, 0f), order);
                var pl = sr.gameObject.AddComponent<ParallaxLayer>();
                pl.factor = factor; pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
            }
            var doorRoot = NewRoot("Muurin ovet");
            Door MakeDoor(string name, string prompt, Area here, Area target, Vector3 pos, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = pos;
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 1.6f; d.maxDistanceFromWall = 2.2f;
                return d;
            }

            // ---- kiitotie: yksi ruutu, loppuu terminaalitorniin (ovi tornin juuressa) ----
            const float RunWallPx = 750f, RunDoorPx = 1360f, RunWalkMaxPx = 1420f;
            var runRoot = NewRoot("Muurin kiitotie");
            Image(runRoot.transform, "Kiitotie", "muuri_kiitotie_torni", WallRunX0, 0f, -10000);
            var run = MakeArea("Alue: Muurin kiitotie", "Muurin kiitotie", WallRunX0, RunWallPx, 1672f);
            run.walkMaxX = WallRunX0 + RunWalkMaxPx / ppu; run.camMinX = run.camMaxX = WallRunX0 + W * 0.5f;
            Para(runRoot.transform, run, "Taivas", "muuri_far_x2", 0.03f, -10150);
            Para(runRoot.transform, run, "Kukkulat", "muuri_mid_x2", 0.06f, -10100);

            // ---- terminaali: yksi leveä kuva (kuvat 1–3 yhdistettynä), tiski, Sohvi ----
            const float TermPx = 4796f, TermWallPx = 700f, TermLeftDoorPx = 235f, TermRightDoorPx = 4590f;
            const float CounterLeftPx = 1365f, CounterTopPx = 560f, CounterRightPx = 2386f;
            var termRoot = NewRoot("Muurin terminaali");
            Image(termRoot.transform, "Terminaali", "muuri_terminaali_pano", WallTermX0, 0f, -10000);
            var term = MakeArea("Alue: Muurin terminaali", "Muurin terminaali", WallTermX0, TermWallPx, TermPx);
            term.walkMinX = WallTermX0 + 330f / ppu;   // vasemman oven pilari
            term.walkMaxX = WallTermX0 + 4700f / ppu;
            Para(termRoot.transform, term, "Ikkunoiden taivas", "muuri_far_x2", 0.03f, -10150);

            // ---- muuri: torni + kaksi ruutua (toinen peilikuva); etumuuri liikkuu nopeammin ----
            const float WallWallPx = 775f, WallDoorPx = 300f;
            var wallRoot = NewRoot("Muuri");
            string[] wallTiles = { "muuri_alku_torni", "muuri_main", "muuri_rotko", "muuri_main", "muuri_main_peili", "muuri_main", "muuri_hissi_paaty" };   // kuvan 3 rotko: muurin korkeus sovitettu naapureihin; sen yli hypätään rullalaudalla
            for (int i = 0; i < wallTiles.Length; i++) Image(wallRoot.transform, "Muuri " + (i + 1), wallTiles[i], WallX0, i * 1672f, -10000);
            var wall = MakeArea("Alue: Muuri", "Muuri", WallX0, WallWallPx, wallTiles.Length * 1672f);
            wall.walkMinX = WallX0 + 240f / ppu; wall.minDepthY = bottom + 60f / ppu;   // etumuuri laskettu 100 px: muurin kävelykaista on nyt ~105 px (775–880)
            Para(wallRoot.transform, wall, "Taivas ja vuoret", "muuri_far_x2", 0.04f, -10150);
            Para(wallRoot.transform, wall, "Kukkulat", "muuri_mid_x2", 0.10f, -10100);
            {   // rotko (kuva 3): maassa putoaa, menettää puolet energiasta ja palaa rotkon eteen; laudan hypyllä (ilmassa) yli
                var gap = new GameObject("Rotko").AddComponent<ChasmGap>(); gap.transform.SetParent(wallRoot.transform, false);
                gap.area = wall; gap.x0 = WallX0 + (2f * 1672f + 570f) / ppu; gap.x1 = WallX0 + (2f * 1672f + 1120f) / ppu; gap.respawnX = gap.x0 - 2.5f; gap.damageFraction = 0.5f;
                // kaksi lankkua rotkon yli (kävely vain lankuilla); lankun syvyys = sen kuvapikselirivi
                var plank = ImportShipSprite(T + "muuri_lankku.png", ppu, false, SpriteAlignment.Center);
                if (plank != null)
                {
                    float spanPx = 700f, midPx = (570f + 1120f) * 0.5f, sc = spanPx / plank.rect.width;
                    var ys = new List<float>();
                    foreach (float rowPx in new[] { 790f, 840f })
                    {
                        float py = top - rowPx / ppu;
                        var pr = Put(wallRoot.transform, "Lankku", plank, new Vector3(WallX0 + (2f * 1672f + midPx) / ppu, py, 0f), Mathf.RoundToInt(-py * 100f) - 50);
                        pr.transform.localScale = new Vector3(sc, sc, 1f);
                        ys.Add(py);
                    }
                    gap.laneY = ys.ToArray(); gap.laneHalf = 0.8f; gap.bridged = true;   // silta kattaa koko kävelykaistan (näkymätön kantava taso, lankut ovat vain kuva)
                }
            }
            {
                const float f = 1.3f;
                string[] frontTiles = { "muuri_front", "muuri_front", "muuri_front_peili", "muuri_front" };
                int[] frontAt = { 1, 3, 4, 5 };
                for (int i = 0; i < frontTiles.Length; i++)
                {
                    var fsp = ImportShipSprite(T + frontTiles[i] + ".png", ppu, false, SpriteAlignment.BottomCenter);
                    if (fsp == null) continue;
                    float tx = WallX0 + frontAt[i] * W + W * 0.5f;
                    var fr = Put(wallRoot.transform, "Etumuuri " + (i + 1), fsp, new Vector3(tx, bottom - 100f / ppu, 0f), 3000);
                    fr.transform.localScale = new Vector3(f, 1f, 1f);   // ruudulla vierekkäiset laatat pysyvät yhtenäisinä, kun ne liikkuvat kertoimella f
                    var fp = fr.gameObject.AddComponent<FrontParallax>(); fp.factor = f; fp.anchorCamX = tx; fp.visibleRange = W * 1.0f;
                }
            }

            // ---- kuuhissin sisätila (kuva skaalattu korkeuteen 941; ikkunat läpinäkyviä: taakse tulee pystyparallaksi myöhemmin) ----
            const float ElevWallPx = 715f, ElevDoorPx = 1380f;
            var elevRoot = NewRoot("Muurin kuuhissi");
            var elevImg = Image(elevRoot.transform, "Hissin sisä", "muuri_hissi_sisa", WallElevX0, 0f, -10000);
            float elevPx = elevImg != null ? elevImg.sprite.rect.width : 2320f;
            var elev = MakeArea("Alue: Muurin kuuhissi", "Muurin kuuhissi", WallElevX0, ElevWallPx, elevPx);
            elev.walkMinX = WallElevX0 + 100f / ppu;
            {   // takaraja viistoon: oven kohdalla lattia ulottuu syvemmälle (rivi 795), ikkunaseinän kohdalla rivi 715
                float ex = WallElevX0; int er = 715;
                elev.depthLimits = new[] { new Vector2(ex + 100f / ppu, top - 795f / ppu), new Vector2(ex + 270f / ppu, top - 765f / ppu), new Vector2(ex + 460f / ppu, top - er / ppu), new Vector2(ex + 2319f / ppu, top - er / ppu) };
            }
            {   // ikkunoiden taakse pystyparallaksi: vuorimaisema -> taivas -> avaruus (nousu 70 s)
                var skySp = ImportShipSprite(T + "muuri_taivas_pysty.png", ppu, false, SpriteAlignment.Center);
                var ride = elevRoot.AddComponent<VerticalSkyRide>(); ride.area = elev; ride.sky = skySp; ride.duration = 70f;
                ride.moon = ImportShipSprite(T + "muuri_kuu_pinta.png", ppu, false, SpriteAlignment.Center);
            }

            // ---- kuuasema (hissistä tullaan oikeaan päätyyn; vasen ovi vie myöhemmin kuun pinnalle). Kuva: kolme kuvaa yhdistettynä, korkeus 941 ----
            const float MoonWallPx = 735f, MoonLeftDoorPx = 262f, MoonElevDoorPx = 5775f;
            var moonRoot = NewRoot("Muurin kuuasema");
            var moonImg = Image(moonRoot.transform, "Kuuasema", "muuri_kuuasema_pano", WallMoonX0, 0f, -10000);
            float moonPx = moonImg != null ? moonImg.sprite.rect.width : 5970f;
            var moon = MakeArea("Alue: Muurin kuuasema", "Muurin kuuasema", WallMoonX0, MoonWallPx, moonPx);
            Para(moonRoot.transform, moon, "Ikkunoiden avaruus", "muuri_avaruus", 0.02f, -10150);

            // ---- ovet ----
            MakeDoor("Muurin kiitotie: terminaaliin", "Mene terminaaliin", run, term, new Vector3(WallRunX0 + RunDoorPx / ppu, run.maxDepthY, 0f), new Vector2(WallTermX0 + (TermLeftDoorPx + 130f) / ppu, Mathf.Lerp(term.maxDepthY, term.minDepthY, 0.3f)));
            MakeDoor("Terminaali: kiitotielle", "Ulos kiitotielle", term, run, new Vector3(WallTermX0 + TermLeftDoorPx / ppu, term.maxDepthY, 0f), new Vector2(WallRunX0 + (RunDoorPx - 130f) / ppu, Mathf.Lerp(run.maxDepthY, run.minDepthY, 0.3f)));
            MakeDoor("Terminaali: muurille", "Ulos muurille", term, wall, new Vector3(WallTermX0 + TermRightDoorPx / ppu, term.maxDepthY, 0f), new Vector2(WallX0 + (WallDoorPx + 120f) / ppu, Mathf.Lerp(wall.maxDepthY, wall.minDepthY, 0.4f)));
            MakeDoor("Muuri: terminaaliin", "Mene terminaaliin", wall, term, new Vector3(WallX0 + WallDoorPx / ppu, wall.maxDepthY, 0f), new Vector2(WallTermX0 + (TermRightDoorPx - 120f) / ppu, Mathf.Lerp(term.maxDepthY, term.minDepthY, 0.3f)));
            float elevDoorX = WallX0 + (6f * 1672f + ElevDoorPx) / ppu;
            MakeDoor("Muuri: kuuhissiin", "Mene kuuhissiin", wall, elev, new Vector3(elevDoorX, wall.maxDepthY, 0f), new Vector2(WallElevX0 + 1800f / ppu, Mathf.Lerp(elev.maxDepthY, elev.minDepthY, 0.4f)));
            MakeDoor("Kuuhissi: kuuasemalle", "Ulos kuuasemalle", elev, moon, new Vector3(WallElevX0 + 165f / ppu, top - 795f / ppu, 0f), new Vector2(WallMoonX0 + (MoonElevDoorPx - 330f) / ppu, Mathf.Lerp(moon.maxDepthY, moon.minDepthY, 0.4f)));   // lukossa taistelun ajan (valikko 328)
            MakeDoor("Kuuasema: hissiin", "Mene hissiin", moon, elev, new Vector3(WallMoonX0 + MoonElevDoorPx / ppu, moon.maxDepthY, 0f), new Vector2(WallElevX0 + 600f / ppu, Mathf.Lerp(elev.maxDepthY, elev.minDepthY, 0.4f)));

            // ---- kuun pinta: kuljetaan oikealta vasemmalle; asema-ovi oikeassa päässä (kuvat 1+2 yhdistettynä, korkeus 941) ----
            const float SurfWallPx = 660f, SurfDoorPx = 8663f + SurfBasePx, SurfDoorBasePx = 700f, SurfWalkMaxPx = 8810f + SurfBasePx, BaseDoorPx = 1155f;   // BaseDoorPx: tukikohdan ovi ulkokuvassa
            var surfRoot = NewRoot("Muurin kuun pinta");
            Image(surfRoot.transform, "Tukikohta (ulko)", "muuri_kuuasema_ulko", WallSurfX0, 0f, -10000);
            var surfImgA = Image(surfRoot.transform, "Kuun pinta 1", "muuri_kuupinta_a", WallSurfX0, SurfBasePx, -10000);   // kuva jaettu kahtia (enimmäisleveys 8192)
            float surfHalfPx = surfImgA != null ? surfImgA.sprite.rect.width : 4558f;
            var surfImgB = Image(surfRoot.transform, "Kuun pinta 2", "muuri_kuupinta_b", WallSurfX0, SurfBasePx + surfHalfPx, -10000);
            float surfPx = SurfBasePx + surfHalfPx + (surfImgB != null ? surfImgB.sprite.rect.width : 4559f);
            var surf = MakeArea("Alue: Muurin kuun pinta", "Muurin kuun pinta", WallSurfX0, SurfWallPx, surfPx);
            surf.walkMaxX = WallSurfX0 + SurfWalkMaxPx / ppu; surf.gravityScale = 0.4f; surf.camJumpLift = 0.6f;   // kuun heikko painovoima
            Para(surfRoot.transform, surf, "Tähdet", "muuri_kuupinta_tahdet", 0.02f, -10150, H * 0.5f);   // kuva on kaksinkertaisen korkea (peilattu ylös), koska kamera nousee hypyssä
            Para(surfRoot.transform, surf, "Kaukaiset kraatterit", "muuri_kuupinta_far", 0.07f, -10100);   // 0.07: kuva (2389 px) riittää pidentyneelle pinnalle
            MakeDoor("Kuuasema: kuun pinnalle", "Ulos kuun pinnalle", moon, surf, new Vector3(WallMoonX0 + MoonLeftDoorPx / ppu, moon.maxDepthY, 0f), new Vector2(WallSurfX0 + (SurfDoorPx - 170f) / ppu, Mathf.Lerp(surf.maxDepthY, surf.minDepthY, 0.35f)));
            MakeDoor("Kuun pinta: asemalle", "Mene asemalle", surf, moon, new Vector3(WallSurfX0 + SurfDoorPx / ppu, surf.maxDepthY, 0f), new Vector2(WallMoonX0 + (MoonLeftDoorPx + 130f) / ppu, Mathf.Lerp(moon.maxDepthY, moon.minDepthY, 0.3f)));

            // ---- laboratorio (tukikohdan sisällä): kaksi ensimmäistä ruutua, kuvat yhdistettynä (korkeus 941); ikkunat läpinäkyviä: taakse avaruus ----
            const float LabWallPx = 700f, LabDoorPx = 235f;
            var labRoot = NewRoot("Muurin laboratorio");
            var labImg = Image(labRoot.transform, "Laboratorio", "muuri_laboratorio_pano", WallLabX0, 0f, -10000);
            float labPx = labImg != null ? labImg.sprite.rect.width : 4408f;
            var lab = MakeArea("Alue: Muurin laboratorio", "Muurin laboratorio", WallLabX0, LabWallPx, labPx);
            Para(labRoot.transform, lab, "Ikkunoiden avaruus", "muuri_avaruus", 0.02f, -10150);
            MakeDoor("Kuun pinta: tukikohtaan", "Mene tukikohtaan", surf, lab, new Vector3(WallSurfX0 + BaseDoorPx / ppu, surf.maxDepthY, 0f), new Vector2(WallLabX0 + (LabDoorPx + 130f) / ppu, Mathf.Lerp(lab.maxDepthY, lab.minDepthY, 0.3f)));
            MakeDoor("Laboratorio: ulos", "Ulos kuun pinnalle", lab, surf, new Vector3(WallLabX0 + LabDoorPx / ppu, lab.maxDepthY, 0f), new Vector2(WallSurfX0 + BaseDoorPx / ppu, Mathf.Lerp(surf.maxDepthY, surf.minDepthY, 0.3f)));

            // ---- yläkerta: Kanikonttori (kuvat 1 ja 2 yhdistettynä), Helvi tiskin takana ----
            const float PawnPx = 3147f, PawnWallPx = 725f, PawnDoorPx = 115f, PawnCounterLeftPx = 1440f, PawnCounterTopPx = 505f, PawnCounterBottomPx = 715f, StairsPx = 2765f;
            var pawnRoot = NewRoot("Muurin kanikonttori");
            Image(pawnRoot.transform, "Kanikonttori", "muuri_yla_pano", WallPawnX0, 0f, -10000);
            var pawn = MakeArea("Alue: Muurin kanikonttori", "Muurin kanikonttori", WallPawnX0, PawnWallPx, PawnPx);
            Para(pawnRoot.transform, pawn, "Ikkunan taivas", "muuri_far_x2", 0.03f, -10150);
            MakeDoor("Terminaali: portaat ylös", "Portaat ylös", term, pawn, new Vector3(WallTermX0 + StairsPx / ppu, term.maxDepthY, 0f), new Vector2(WallPawnX0 + (PawnDoorPx + 130f) / ppu, Mathf.Lerp(pawn.maxDepthY, pawn.minDepthY, 0.3f)));
            MakeDoor("Kanikonttori: portaat alas", "Portaat alas", pawn, term, new Vector3(WallPawnX0 + PawnDoorPx / ppu, pawn.maxDepthY, 0f), new Vector2(WallTermX0 + StairsPx / ppu, Mathf.Lerp(term.maxDepthY, term.minDepthY, 0.3f)));
            string pawnInfo = "";
            {
                Sprite[] Sheet(string n, int count)
                {
                    var sp = SliceAerobicsSheet(HelviDir + n + ".png");
                    if (sp == null || sp.Length < count) return null;
                    return sp.Take(count).ToArray();
                }
                var idle = Sheet("helvi_idle", 24); var hair = Sheet("helvi_hiukset", 35);
                var cSpr = ImportShipSprite(T + "muuri_yla_tiski.png", ppu, false, SpriteAlignment.Center);
                if (idle == null || hair == null || cSpr == null) pawnInfo = "Helvi tai tiskikuva puuttuu (Helvin kuvat: " + HelviDir + ")";
                else
                {
                    var seq = new List<Sprite>();
                    for (int i = 0; i < 3; i++) seq.AddRange(idle);
                    seq.AddRange(hair);
                    float phy = top - (PawnCounterTopPx + 190f) / ppu, phx = WallPawnX0 + 1830f / ppu, frontY = top - PawnCounterBottomPx / ppu;
                    var cgo = new GameObject("Kanikonttorin tiski (etu)").AddComponent<SpriteRenderer>();
                    cgo.transform.SetParent(pawnRoot.transform, false); cgo.sprite = cSpr;
                    cgo.transform.position = new Vector3(WallPawnX0 + (PawnCounterLeftPx + cSpr.rect.width * 0.5f) / ppu, top - (PawnCounterTopPx + cSpr.rect.height * 0.5f) / ppu, 0f);
                    cgo.sortingOrder = Mathf.RoundToInt(-frontY * 100f);
                    var h = new GameObject("Helvi (muurin kanikonttori)");
                    h.transform.SetParent(pawnRoot.transform, false); h.transform.position = new Vector3(phx, phy, 0f); h.transform.localScale = Vector3.one * AlpPawnHelviScale;
                    var hsr = h.AddComponent<SpriteRenderer>(); hsr.sprite = seq[0]; hsr.sortingOrder = Mathf.RoundToInt(-phy * 100f);
                    var dancer = h.AddComponent<Dancer>(); dancer.sprites = seq.ToArray(); dancer.frameTime = 0.1f; dancer.keepOrder = true;
                    var shopGo = new GameObject("Kanikonttorin tiski (Helvi)");
                    shopGo.transform.SetParent(pawnRoot.transform, false); shopGo.transform.position = new Vector3(phx, pawn.maxDepthY, 0f);
                    var psc = shopGo.AddComponent<ShopCounter>();
                    psc.here = pawn; psc.title = "KANIKONTTORI"; psc.npcName = "Helvi"; psc.prompt = "Puhu Helville";
                    psc.halfWidth = 3.4f; psc.maxDistanceFromWall = 1.4f; psc.ownItemsOnly = true;
                    psc.items = new[]
                    {
                        new ShopCounter.Item { name = "Rullalauta", price = 80, give = "esine", comment = "Hyvässä kunnossa. Ota taukovalikosta; muurilla rotkon yli." },
                        new ShopCounter.Item { name = "Kaulakoru", price = 250, give = "esine", comment = "Kultainen ketju." },
                        new ShopCounter.Item { name = "Korvakorut", price = 150, give = "esine", comment = "Turkoosit, pari." },
                        new ShopCounter.Item { name = "Kitara", price = 400, give = "esine", comment = "Käytetty akustinen." },
                        new ShopCounter.Item { name = "Rannekello", price = 180, give = "esine", comment = "Kultainen, käy tarkasti." },
                        new ShopCounter.Item { name = "Kiikarit", price = 70, give = "esine", comment = "Näkee kauas." },
                        new ShopCounter.Item { name = "Radio", price = 60, give = "esine", comment = "Vanha, mutta soi." },
                    };
                    pawnInfo = "Kanikonttori (x " + WallPawnX0.ToString("0") + "): Helvi tiskin takana";
                }
            }

            // ---- terminaalin sisus: tiskin etuosa + Sohvi ja kauppa, nuudelipöydät, pullot ----
            var root = NewRoot("Muurin terminaalin sisus");
            float PxX(float px) => WallTermX0 + px / ppu;
            float Y(float depth) => Mathf.Lerp(term.maxDepthY - 0.2f, term.minDepthY + 0.4f, depth);
            float hx = PxX((CounterLeftPx + CounterRightPx) * 0.5f), hy = top - (CounterTopPx + 92f) / ppu;
            var csp = ImportShipSprite(T + "muuri_terminaali_tiski.png", ppu, false, SpriteAlignment.Center);
            if (csp != null)
            {
                var cs = new GameObject("Tiskin etu").AddComponent<SpriteRenderer>(); cs.transform.SetParent(root.transform, false); cs.sprite = csp; cs.sortingOrder = Mathf.RoundToInt(-hy * 100f) + 5;
                cs.transform.position = new Vector3(WallTermX0 + (CounterLeftPx + csp.rect.width * 0.5f) / ppu, top - (CounterTopPx + csp.rect.height * 0.5f) / ppu, 0f);
            }
            var sohviT = Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => alp.IsValid() && c.gameObject.scene == alp && c.name == "Sohvi (afterski-baari)");
            var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => alp.IsValid() && c.gameObject.scene == alp && c.name == "Afterski-baarin tiski (Sohvi)");
            if (sohviT != null && counterT != null)
            {
                var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (muurin terminaali)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, jungle); sg.transform.SetParent(root.transform, false);
                sg.transform.position = new Vector3(hx, hy, 0f); sg.transform.localScale = Vector3.one * AlpTermSohviScale;
                var rS = sg.GetComponentInChildren<SpriteRenderer>(); if (rS != null) rS.sortingOrder = Mathf.RoundToInt(-hy * 100f);
                var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Terminaalin tiski (Sohvi)";
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, jungle); shopGo.transform.SetParent(root.transform, false);
                shopGo.transform.position = new Vector3(hx, term.maxDepthY, 0f);
                var sc = shopGo.GetComponent<ShopCounter>();
                sc.here = term; sc.title = "TERMINAALI"; sc.npcName = "Sohvi"; sc.prompt = "Puhu Sohville"; sc.halfWidth = 3.6f; sc.maxDistanceFromWall = 1.5f; sc.ownItemsOnly = false;
                sc.items = new[]
                {
                    new ShopCounter.Item { name = "Nuudelit", price = 12, heal = 60, stamina = 0, comment = "Kuumia, chilillä." },
                    new ShopCounter.Item { name = "Voileipä", price = 8, heal = 40, stamina = 0, comment = "Tuore, tiskiltä." },
                    new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                    new ShopCounter.Item { name = "Olut", price = 8, heal = 30, stamina = 999, comment = "Kylmää." },
                };
            }
            else report.Add("Sohvin malleja ei löytynyt (aja valikko 279)");

            // pöydät: nuudeliannos + pulloja; tuolit
            var allCrates = Object.FindObjectsByType<Crate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allFish = Object.FindObjectsByType<FishPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allBottles = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fishT = alp.IsValid() ? allFish.FirstOrDefault(fp => fp != null && fp.gameObject.scene == alp && fp.spriteSet == "Kala" && fp.table != null) : null;
            var noodleT = allFish.FirstOrDefault(fp => fp != null && fp.spriteSet == "Nuudeli" && fp.table != null);
            var plateT = noodleT != null ? noodleT : fishT;   // jos nuudelimallia ei löydy, kopioidaan kalaannos ja vaihdetaan kuvat
            var tableT = plateT != null ? plateT.table : (ucco.IsValid() ? allCrates.FirstOrDefault(c => c != null && c.gameObject.scene == ucco && c.name == "P" + (char)0xF6 + "yt" + (char)0xE4 && c.transform.parent != null && c.transform.parent.name.StartsWith("El Loipparin")) : null);
            var beerT = plateT != null ? allBottles.FirstOrDefault(b => b != null && b.table == plateT.table) : null;
            var chairT = ucco.IsValid() ? Object.FindObjectsByType<Chair>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c.gameObject.scene == ucco) : null;
            var tables = new[] { new Vector2(830f, 0.85f), new Vector2(1500f, 0.92f), new Vector2(3550f, 0.88f), new Vector2(4150f, 0.90f), new Vector2(2750f, 0.95f) };
            int nt = 0, nb = 0, nf = 0;
            if (tableT != null)
            {
                var props = new GameObject("Terminaalin pöydät ja tuolit"); props.transform.SetParent(root.transform, false);
                foreach (var v in tables)
                {
                    float x = PxX(v.x), y = Y(v.y); nt++;
                    var tg = Object.Instantiate(tableT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tg, jungle); tg.transform.SetParent(props.transform, false);
                    tg.name = "Terminaalipöytä " + nt; tg.transform.position = new Vector3(x, y, 0f); tg.SetActive(true);
                    var tc = tg.GetComponent<Crate>(); tc.stackedOn = null;
                    if (plateT != null)
                    {
                        var fg = Object.Instantiate(plateT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fg, jungle); fg.transform.SetParent(props.transform, false);
                        fg.name = "Nuudeliannos " + nt; fg.transform.position = tg.transform.position; fg.SetActive(true); nf++;
                        var fp = fg.GetComponent<FishPlate>(); fp.table = tc; fp.tableX = (nt % 2 == 0 ? 0.25f : -0.25f) * 1.5f; fp.spriteSet = "Nuudeli"; if (noodleT == null) fp.scale *= 0.8f;
                        if (beerT != null)
                            foreach (float bx in new[] { -0.9f, 0.9f, nt % 2 == 0 ? -0.3f : 0.3f })
                            {
                                var bg = Object.Instantiate(beerT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bg, jungle); bg.transform.SetParent(props.transform, false);
                                bg.name = "Pullo " + (++nb); bg.transform.position = tg.transform.position; bg.SetActive(true);
                                var bb = bg.GetComponent<Bottle>(); bb.table = tc; bb.tableX = bx * 1.5f;
                            }
                    }
                    if (chairT != null)
                    {
                        var cg = Object.Instantiate(chairT.gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg, jungle); cg.transform.SetParent(props.transform, false);
                        cg.name = "Terminaalituoli " + nt; cg.transform.position = new Vector3(x + (nt % 2 == 0 ? 1.7f : -1.7f), y - 0.05f, 0f);
                        var vis = cg.GetComponentInChildren<SpriteRenderer>(); if (vis != null) vis.flipX = nt % 2 == 0;
                    }
                }
                if (plateT == null) report.Add("annoksia ei lisätty (aja valikko 303)");
                else if (noodleT == null) report.Add("nuudelimallia ei löytynyt: annoksina kopioitu kala vaihdetuilla kuvilla (Resources/Nuudeli)");
            }
            else report.Add("pöytämallia ei löytynyt (aja valikko 303 tai 50)");

            // ---- wok-koju (Thaitownin thaikioski): hummeri- ja nuudeliannokset ja pannu, kuten stadionin ravintolassa ----
            var plateSnd = LoadClips("Assets/Audio/sfx", "posliini");
            var collapse = Resources.LoadAll<Sprite>("ThaiKiosk").Where(a => a.name.StartsWith("romahdus_")).OrderBy(a => int.Parse(a.name.Substring(a.name.LastIndexOf('_') + 1))).ToArray();
            var intact = Resources.Load<Sprite>("ThaiKiosk/koju_0");
            var panFrames = Resources.LoadAll<Sprite>("Pan").Where(a => a.name.StartsWith("pannu_kuvakulmat_")).OrderBy(a => int.Parse(a.name.Substring(a.name.LastIndexOf('_') + 1))).ToArray();
            if (collapse.Length == 6 && intact != null && panFrames.Length == 8)
            {
                float kx = PxX(3170f), kback = term.MaxDepthAtX(kx, term.maxDepthY);
                var p = new Vector3(kx, Mathf.Lerp(kback - 0.2f, term.minDepthY + 0.3f, 0.2f), 0f);
                var kgo = new GameObject("Wok-koju"); kgo.transform.SetParent(root.transform, false); kgo.transform.position = p;
                var kvis = new GameObject("Visual"); kvis.transform.SetParent(kgo.transform, false);
                var ksr = kvis.AddComponent<SpriteRenderer>(); ksr.sprite = intact;
                var kc = kgo.AddComponent<Crate>(); kc.body = ksr; kc.anchored = true; kc.impactRadiusX = 2.5f;
                kc.sprites = new[] { intact, collapse[1], collapse[2] }; kc.breakSprites = collapse; kc.hitsToBreak = 5; kc.breakFrameTime = 0.085f; kc.debrisTime = 25f;
                kc.visualScale = 1.1f; kc.footOffset = 0f; kc.breakSounds = LoadClips("Assets/Audio/sfx", "puu");
                kc.moneyChance = kc.energyChance = kc.foodChance = kc.thermosChance = 0f;
                const float ktop = 1.65f;
                for (int i = 0; i < 3; i++)
                {
                    var foodGo = new GameObject(i == 0 ? "Hummeriannos" : "Nuudeliannos"); foodGo.transform.SetParent(root.transform, false); foodGo.transform.position = p;
                    var food = foodGo.AddComponent<FishPlate>(); food.spriteSet = i == 0 ? "Hummeri" : "Nuudeli"; food.table = kc; food.tableX = i == 0 ? -0.8f : i == 1 ? 0.1f : 1.05f;
                    food.tableTop = ktop; food.scale = i == 0 ? 0.6f : 0.55f; food.breakSounds = plateSnd;
                }
                var panGo = new GameObject("Kojun pannu"); panGo.transform.SetParent(root.transform, false); panGo.transform.position = p;
                var panVisual = new GameObject("Visual"); panVisual.transform.SetParent(panGo.transform, false);
                var pan = panGo.AddComponent<Pan>(); pan.body = panVisual.AddComponent<SpriteRenderer>(); pan.spinSprites = panFrames;
                pan.table = kc; pan.tableX = 1.9f; pan.tableTop = ktop; pan.hitDurability = 10; pan.throwDurability = 3;
            }
            else report.Add("wok-kojun kuvat puuttuvat (aja valikot 188 ja 189 Thaitownissa)");

            // ---- kuuasema: Sohvi tiskin taakse (sama kauppa kuin terminaalissa) ja kaksi wok-kojua ----
            var moonProps = NewRoot("Muurin kuuaseman sisus");
            float MoonX(float px) => WallMoonX0 + px / ppu;
            {
                const float MoonCounterLeftPx = 2148f, MoonCounterTopPx = 596f, MoonSohviPx = 3060f;
                float mhx = MoonX(MoonSohviPx), mhy = top - (MoonCounterTopPx + 92f) / ppu;
                var mcsp = ImportShipSprite(T + "muuri_kuuasema_tiski.png", ppu, false, SpriteAlignment.Center);
                if (mcsp != null)
                {
                    var mcs = new GameObject("Tiskin etu").AddComponent<SpriteRenderer>(); mcs.transform.SetParent(moonProps.transform, false); mcs.sprite = mcsp; mcs.sortingOrder = Mathf.RoundToInt(-mhy * 100f) + 5;
                    mcs.transform.position = new Vector3(MoonX(MoonCounterLeftPx + mcsp.rect.width * 0.5f), top - (MoonCounterTopPx + mcsp.rect.height * 0.5f) / ppu, 0f);
                }
                if (sohviT != null && counterT != null)
                {
                    var msg = Object.Instantiate(sohviT.gameObject); msg.name = "Sohvi (kuuasema)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(msg, jungle); msg.transform.SetParent(moonProps.transform, false);
                    msg.transform.position = new Vector3(mhx, mhy, 0f); msg.transform.localScale = Vector3.one * AlpTermSohviScale;
                    var mrS = msg.GetComponentInChildren<SpriteRenderer>(); if (mrS != null) mrS.sortingOrder = Mathf.RoundToInt(-mhy * 100f);
                    var mshop = Object.Instantiate(counterT.gameObject); mshop.name = "Kuuaseman tiski (Sohvi)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(mshop, jungle); mshop.transform.SetParent(moonProps.transform, false);
                    mshop.transform.position = new Vector3(mhx, moon.maxDepthY, 0f);
                    var msc = mshop.GetComponent<ShopCounter>();
                    msc.here = moon; msc.title = "KUUASEMA"; msc.npcName = "Sohvi"; msc.prompt = "Puhu Sohville"; msc.halfWidth = 4.5f; msc.maxDistanceFromWall = 1.5f; msc.ownItemsOnly = false;
                    msc.items = new[]
                    {
                        new ShopCounter.Item { name = "Nuudelit", price = 12, heal = 60, stamina = 0, comment = "Kuumia, chilillä." },
                        new ShopCounter.Item { name = "Voileipä", price = 8, heal = 40, stamina = 0, comment = "Tuore, tiskiltä." },
                        new ShopCounter.Item { name = "Kahvi", price = 4, heal = 0, stamina = 60, comment = "Kuumaa ja vahvaa." },
                        new ShopCounter.Item { name = "Olut", price = 8, heal = 30, stamina = 999, comment = "Kylmää." },
                    };
                }
                else report.Add("kuuaseman Sohvia ei lisätty (mallit puuttuvat)");
            }
            if (collapse.Length == 6 && intact != null && panFrames.Length == 8)
            {
                int wokNo = 0;
                foreach (var (wpx, wdepth) in new[] { (800f, 0.25f), (4650f, 0.3f) })
                {
                    wokNo++;
                    float wx = MoonX(wpx), wback = moon.MaxDepthAtX(wx, moon.maxDepthY);
                    var wp = new Vector3(wx, Mathf.Lerp(wback - 0.2f, moon.minDepthY + 0.3f, wdepth), 0f);
                    var wgo = new GameObject("Wok-koju " + wokNo); wgo.transform.SetParent(moonProps.transform, false); wgo.transform.position = wp;
                    var wvis = new GameObject("Visual"); wvis.transform.SetParent(wgo.transform, false);
                    var wsr = wvis.AddComponent<SpriteRenderer>(); wsr.sprite = intact;
                    var wkc = wgo.AddComponent<Crate>(); wkc.body = wsr; wkc.anchored = true; wkc.impactRadiusX = 2.5f;
                    wkc.sprites = new[] { intact, collapse[1], collapse[2] }; wkc.breakSprites = collapse; wkc.hitsToBreak = 5; wkc.breakFrameTime = 0.085f; wkc.debrisTime = 25f;
                    wkc.visualScale = 1.1f; wkc.footOffset = 0f; wkc.breakSounds = LoadClips("Assets/Audio/sfx", "puu");
                    wkc.moneyChance = wkc.energyChance = wkc.foodChance = wkc.thermosChance = 0f;
                    for (int fi = 0; fi < 3; fi++)
                    {
                        var wfood = new GameObject(fi == 0 ? "Hummeriannos" : "Nuudeliannos"); wfood.transform.SetParent(moonProps.transform, false); wfood.transform.position = wp;
                        var wfp = wfood.AddComponent<FishPlate>(); wfp.spriteSet = fi == 0 ? "Hummeri" : "Nuudeli"; wfp.table = wkc; wfp.tableX = fi == 0 ? -0.8f : fi == 1 ? 0.1f : 1.05f;
                        wfp.tableTop = 1.65f; wfp.scale = fi == 0 ? 0.6f : 0.55f; wfp.breakSounds = plateSnd;
                    }
                    var wpanGo = new GameObject("Kojun pannu"); wpanGo.transform.SetParent(moonProps.transform, false); wpanGo.transform.position = wp;
                    var wpanVis = new GameObject("Visual"); wpanVis.transform.SetParent(wpanGo.transform, false);
                    var wpan = wpanGo.AddComponent<Pan>(); wpan.body = wpanVis.AddComponent<SpriteRenderer>(); wpan.spinSprites = panFrames;
                    wpan.table = wkc; wpan.tableX = 1.9f; wpan.tableTop = 1.65f; wpan.hitDurability = 10; wpan.throwDurability = 3;
                }
            }

            // ---- väki: rauhassa, tappelu alkaa 1 min kuluttua tai kun hero aloittaa ----
            Enemy Template(string display)
            {
                foreach (var sc in new[] { alp, thai, sample, desert, ucco, jungleTpl })
                {
                    if (!sc.IsValid()) continue;
                    var all = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(en => en != null && en.gameObject.scene == sc && en.displayName == display).ToArray();
                    var pick = all.FirstOrDefault(en => !en.gameObject.name.Contains("(")) ?? all.FirstOrDefault();
                    if (pick != null) return pick;
                }
                return null;
            }
            var crowdDef = new (string type, float px, float depth)[] {
                ("Lentokapteeni", 650f, 0.60f), ("Lentokapteeni", 3300f, 0.70f),
                ("Lentoperämies", 1000f, 0.75f), ("Lentoperämies", 3950f, 0.65f),
                ("Puliukko", 1250f, 0.55f), ("Puliukko", 4300f, 0.80f),
                ("Punkkari", 2100f, 0.90f), ("Punkkari", 3050f, 0.60f),
                ("Jakuza", 2650f, 0.70f), ("Jakuza", 3700f, 0.92f),
                ("Rokkimimmi", 800f, 0.95f), ("Rokkimimmi", 4000f, 0.55f),
                ("Prätkäjätkä", 1850f, 0.95f), ("Prätkäjätkä", 3450f, 0.50f),
                ("Lätkäfani", 2400f, 0.85f),
                ("Betty", 1500f, 0.70f) };
            var crowd = NewRoot("Muurin terminaalin väki");
            var made = new List<Enemy>(); var counts = new Dictionary<string, int>(); var tmpls = new Dictionary<string, Enemy>();
            foreach (var (type, px, depth) in crowdDef)
            {
                if (!tmpls.ContainsKey(type)) tmpls[type] = Template(type);
                if (tmpls[type] == null) { if (!counts.ContainsKey(type + " (puuttuu)")) { counts[type + " (puuttuu)"] = 1; report.Add(type + ": mallia ei löytynyt"); } continue; }
                counts[type] = counts.TryGetValue(type, out int c0) ? c0 + 1 : 1;
                var go = Object.Instantiate(tmpls[type].gameObject); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, jungle); go.transform.SetParent(crowd.transform, false);
                go.name = type + " (terminaali " + counts[type] + ")"; go.SetActive(true);
                var e = go.GetComponent<Enemy>(); if (e == null) { Object.DestroyImmediate(go); continue; }
                float x = PxX(px);
                e.wakeDistance = -1f; e.wakeOnFightRadius = 0f; e.joinsFightWhenSquadComes = false; e.fightsEveryone = true; e.ally = false; e.brawlsWithOthers = false;
                if (e.calmIdleSprites == null || e.calmIdleSprites.Length == 0) e.calmIdleSprites = e.idleSprites;
                if (e.calmWalkSprites == null || e.calmWalkSprites.Length == 0) { e.calmWalkSprites = e.walkSprites; e.calmWalkFrameTime = e.walkFrameTime; }
                e.wanderMinX = x - 520f / ppu; e.wanderMaxX = x + 520f / ppu;
                e.transform.position = new Vector3(x, Mathf.Lerp(term.maxDepthY, term.minDepthY, depth), 0f);
                made.Add(e);
            }
            var bgo = NewRoot("Muurin terminaalin tappelu");
            var brawl = bgo.AddComponent<SalsaBrawl>();
            brawl.area = term; brawl.instigator = made.FirstOrDefault(m => m != null && m.displayName == "Puliukko") ?? made.FirstOrDefault();
            brawl.calmSeconds = 60f; brawl.crowd = made.ToArray(); brawl.sailors = null; brawl.rogues = null;
            EditorUtility.SetDirty(brawl); EditorUtility.SetDirty(term); EditorUtility.SetDirty(pawn); EditorUtility.SetDirty(run); EditorUtility.SetDirty(wall);

            // alueet scenen alueluetteloon (areas.txt) ja Viidakko Build Settingsiin on jo valikon 305 jäljiltä
            File.WriteAllLines(WallScenePath.Replace(".unity", ".areas.txt"), WallAreaNames);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(bs => bs.path == WallScenePath)) { buildScenes.Add(new EditorBuildSettingsScene(WallScenePath, true)); EditorBuildSettings.scenes = buildScenes.ToArray(); }
            EditorSceneManager.MarkSceneDirty(jungle); EditorSceneManager.SaveScene(jungle);
            Info("Kiinan muuri rakennettu omaan sceneensä (" + WallScenePath + ", lisätty Build Settingsiin): Muurin kiitotie (x " + WallRunX0.ToString("0") + "), Muurin terminaali (x " + WallTermX0.ToString("0") + ", " + nt + " nuudelipöytää, " + nf + " annosta, " + nb + " pulloa), Muuri (x " + WallX0.ToString("0") + ", torni, muuri, rotko, muuri, peilikuva; rotkoon putoaa, laudalla hyppäämällä yli).\n" +
                 "Väki " + made.Count + " (" + string.Join(", ", counts.Select(kv => kv.Value + " " + kv.Key)) + "). Ovet: kiitotie ↔ terminaali ↔ muuri. " + string.Join("; ", report) + " " + pawnInfo + ".\nAjamatta Unityssä. Maailmanpallo (F8): Kiinan muuri vaatii Timpan lipun.");
        }
        finally { foreach (var sc in opened) if (sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }

    [MenuItem("Beat em up/331. Aloita peli kuuasemalta (testaus)", false, 331)]
    static void StartAtMoonStation() { WithLevelScenes(StartAtMoonStationImpl); }

    static void StartAtMoonStationImpl()
    {
        if (PlayModeBlocked()) return;
        const string areaName = "Muurin kuuasema";
        var area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == areaName);
        string sceneName = null; UnityEngine.SceneManagement.Scene opened = default;
        if (area == null)
        {
            sceneName = AreaSceneName(areaName);
            if (sceneName != null)
            {
                opened = EditorSceneManager.OpenScene("Assets/Scenes/Areas/" + sceneName + ".unity", OpenSceneMode.Additive);
                area = Object.FindObjectsByType<Area>(FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == areaName);
            }
        }
        if (area == null) { Info("Aluetta \"" + areaName + "\" ei ole: aja valikko 330 (Kiinan muuri kerralla)."); return; }
        var old = GameObject.Find("Aloituskohta");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var go = new GameObject("Aloituskohta");
        var start = go.AddComponent<GameStart>();
        float ppu = 941f / (2f * CamHalf);
        float sx = WallMoonX0 + (5775f - 330f) / ppu;   // hissin ovi oikeassa päässä
        start.position = new Vector2(sx, Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.4f));
        if (sceneName == null) start.area = area;
        else { start.areaName = areaName; start.sceneName = sceneName; EditorSceneManager.CloseScene(opened, true); }
        Undo.RegisterCreatedObjectUndo(go, "Aloituskohta");
        EditorSceneManager.MarkSceneDirty(go.scene);
        Info("Peli alkaa nyt kuuasemalta hissin ovelta (x ≈ " + sx.ToString("0.0") + "). Vasen ovi vie kuun pinnalle. Aja 330 ensin, jos muuria ei ole rakennettu. Muissa kentissä tätä ei tarvitse poistaa: aloituskohta korvautuu seuraavalla aloitusvalikolla (esim. 283).");
    }
}
