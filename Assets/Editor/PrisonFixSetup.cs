using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Vankilan mittakaavakorjaukset olemassa olevaan sceneen (ilman koko vankilan uudelleenrakennusta): vartijat 15 % isommiksi, suojalaatikot pienemmiksi (3,4 -> 2,5 korkeus).
/// Uudet ajot (353–363) käyttävät samoja arvoja suoraan. Valikko on turvallista ajaa uudelleen (ei kerro kahdesti).</summary>
public static partial class BeatEmUpSetup
{
    const float GuardScale = 1.11f;   // vartija: 1,15 -> 1,035 (-10 %) -> 1,11 (+7 %)
    /// Vankilassa ei ole suojalaatikoita (ei stealth-tilaa): false = valikot eivät luo niitä ja valikko 367 poistaa olemassa olevat.
    const bool PrisonCrates = false;
    const float RexScale = 1.15f, RicoScale = 1.15f, RicoKnockScale = 0.94f;   // Rico 25 % isommaksi (oli 0,92); makaavana liian pitkä -> kaatumis-/nousukuvat 6 % pienemmäksi   // Rico oli vähän isompi kuin vartija: 8 % pienemmäksi
    /// Sellikäytävien (D/E) kuvat 10 % isommiksi ja lisälattiaa alas (HallFloorExt riviä, ks. Tools/PrisonAssets/make_hall_floor_ext.py).
    const float PrisonHallZoom = 1.1f, HallFloorExt = 140f;
    /// Sellien kameran lähennys: hahmot näyttävät isommilta sellissä (kuva-alan alareuna pysyy samana, yläosa rajautuu).
    const float CellZoom = 1.3f;

    [MenuItem("Beat em up/367. Vankila: vartijat 15 % isommiksi, poista laatikot, Rex 15 % isommaksi, sellien kamera lähemmäs, taivas ikkunoihin (nykyiseen sceneen)", false, 367)]
    static void FixPrisonScales()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            int guards = 0, crates = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && e.displayName == "Vartija" && Mathf.Abs(e.visualScale - GuardScale) > 0.01f)
                {
                    Undo.RecordObject(e, "Vartijan koko"); e.visualScale = GuardScale; EditorUtility.SetDirty(e); guards++;
                }
            foreach (var c in Object.FindObjectsByType<CoverCrate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c != null && c.gameObject.scene == scene && !PrisonCrates) { Undo.DestroyObjectImmediate(c.gameObject); crates++; }   // poistetaan suojalaatikot
            int thrown = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && e.displayName == "Vartija" && (e.thrownLoopFrames == null || e.thrownLoopFrames.Length == 0 || Mathf.Abs(e.thrownFrameTime - 0.11f) > 0.001f))
                { Undo.RecordObject(e, "Vartija heitettynä"); e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 4, 5, 6 }; e.thrownLandFrames = new[] { 8, 9, 10, 11 }; e.thrownFrameTime = 0.11f; EditorUtility.SetDirty(e); thrown++; }
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (e == null || e.gameObject.scene != scene || (e.thrownLoopFrames != null && e.thrownLoopFrames.Length > 0)) continue;
                if (e.displayName == "Hector") { Undo.RecordObject(e, "Hector heitettynä"); e.thrownStartFrames = new[] { 0 }; e.thrownLoopFrames = new[] { 0, 1 }; e.thrownLandFrames = new[] { 1, 2, 3 }; e.thrownFrameTime = 0.11f; EditorUtility.SetDirty(e); thrown++; }
                else if (e.displayName == "Rico") { Undo.RecordObject(e, "Rico heitettynä"); e.thrownStartFrames = new[] { 0, 1, 2 }; e.thrownLoopFrames = new[] { 3, 4, 5 }; e.thrownLandFrames = new[] { 6, 7, 8, 9 }; e.thrownFrameTime = 0.11f; EditorUtility.SetDirty(e); thrown++; }
            }
            int getUps = 0;   // Hectorin ja Ricon nousukuvat puuttuivat setupeista (rivi oli kommentin sisällä): lisätään jo tehtyihin
            Sprite[] hectorUp = null, ricoUp = null;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (e == null || e.gameObject.scene != scene || (e.getUpSprites != null && e.getUpSprites.Length > 0)) continue;
                if (e.displayName == "Hector") { hectorUp = hectorUp ?? LoadWalkerSprites("hector_nousu"); if (hectorUp != null && hectorUp.Length > 0) { Undo.RecordObject(e, "Hector nousu"); e.getUpSprites = hectorUp; e.getUpTime = 0.95f; EditorUtility.SetDirty(e); getUps++; } }
                else if (e.displayName == "Rico") { ricoUp = ricoUp ?? LoadWalkerSprites("rico_nousu"); if (ricoUp != null && ricoUp.Length > 0) { Undo.RecordObject(e, "Rico nousu"); e.getUpSprites = ricoUp; e.getUpTime = 1.0f; EditorUtility.SetDirty(e); getUps++; } }
            }
            int punchFlags = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && (e.displayName == "Rico" || e.displayName == "Rex") && !e.punch3IsPunch) { Undo.RecordObject(e, "Kolmas isku lyönti"); e.punch3IsPunch = true; EditorUtility.SetDirty(e); punchFlags++; }
            int bigs = 0;   // vartijat, Ricot ja Warden: kuperkeikkaheitto
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && !e.bigBody && (e.displayName == "Vartija" || e.displayName == "Rico" || e.displayName == "Warden")) { Undo.RecordObject(e, "Iso vihu"); e.bigBody = true; EditorUtility.SetDirty(e); bigs++; }
            int plates = 0, rexHp = 0;   // ruokalan makkara-annokset pienemmiksi, Rexille enemmän elämää
            foreach (var fp in Object.FindObjectsByType<FishPlate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (fp != null && fp.gameObject.scene == scene && fp.spriteSet == "Makkara" && Mathf.Abs(fp.scale - 0.7f) > 0.01f) { Undo.RecordObject(fp, "Annos pienemmäksi"); fp.scale = 0.7f; EditorUtility.SetDirty(fp); plates++; }
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && e.displayName == "Rex" && e.maxHealth < 520) { Undo.RecordObject(e, "Rexin elämät"); e.maxHealth = 520; EditorUtility.SetDirty(e); rexHp++; }
            int ricos = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.gameObject.scene == scene && e.displayName == "Rico" && (Mathf.Abs(e.visualScale - RicoScale) > 0.01f || Mathf.Abs(e.knockArtScale - RicoKnockScale) > 0.01f)) { Undo.RecordObject(e, "Ricon koko"); e.visualScale = RicoScale; e.knockArtScale = RicoKnockScale; EditorUtility.SetDirty(e); ricos++; }
            int rexes = 0;
            foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (e != null && e.displayName == "Rex" && e.visualScale < RexScale - 0.01f) { Undo.RecordObject(e, "Rex isommaksi"); e.visualScale = RexScale; EditorUtility.SetDirty(e); rexes++; }
            int cells = 0;
            foreach (var ar in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ar != null && ar.gameObject.scene == scene && ar.areaName != null && ar.areaName.StartsWith("Vankilan selli") && ar.camSize > CamHalf / CellZoom + 0.01f)
                {
                    float X0 = ar.walkMinX - 1.5f, imgW = ar.walkMaxX - ar.walkMinX + 3f, W = 1672f / (941f / (2f * CamHalf)) / CellZoom;
                    Undo.RecordObject(ar, "Sellin kamera");
                    ar.camSize = CamHalf / CellZoom; ar.camOffsetY = CamHalf / CellZoom - CamHalf; ar.camMinX = X0 + W * 0.5f; ar.camMaxX = Mathf.Max(ar.camMinX, X0 + imgW - W * 0.5f);
                    EditorUtility.SetDirty(ar); cells++;
                }
            // taivas (parallaksi) ikkunoiden taakse: ikkunalasit ovat kuvissa läpinäkyviä, mutta takana ei ollut mitään (pihalla on oma taivas)
            int skies = 0;
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Vankilan taivas: ")).ToArray()) Undo.DestroyObjectImmediate(old);
            var skySprite = ImportShipSprite("Assets/Sprites/Taustat/kone_taivas.png", 922f / (2f * CamHalf), false, SpriteAlignment.Center);
            if (skySprite != null)
                foreach (var ar in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (ar == null || ar.gameObject.scene != scene || ar.areaName == null || ar.areaName == "Vankila" || ar.areaName.Contains("D-osasto") || ar.areaName.Contains("E-osasto")) continue;   // pihalla on oma taivas, käytävillä ei ole ikkunoita
                    var go = new GameObject("Vankilan taivas: " + ar.areaName);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    Undo.RegisterCreatedObjectUndo(go, "Taivas");
                    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = skySprite; sr.sortingOrder = -10100;
                    float W = 1672f / (941f / (2f * CamHalf)), left = ar.camMinX - W * 0.5f;
                    go.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
                    go.transform.position = new Vector3(left + skySprite.bounds.size.x * 1.25f * 0.5f, CamY, 0f);
                    var pl = go.AddComponent<ParallaxLayer>(); pl.factor = 0.02f; pl.startCamX = ar.camMinX; pl.minX = ar.camMinX - 2f; pl.maxX = ar.camMaxX + 2f;
                    skies++;
                }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(plates + " annosta pienemmäksi, " + rexHp + " Rexille 520 elämää. " + bigs + " vihulle kuperkeikkaheitto (vartijat, Ricot, Warden). " + getUps + " Hectorille/Ricolle nousukuvat. " + punchFlags + " Ricon/Rexin koukku napattavaksi. " + thrown + " vartijalle heitettynä-kuvajärjestys. " + skies + " alueelle taivas ikkunoiden taakse (parallaksi). " + cells + " sellin kamera lähemmäs (hahmot isommiksi, zoom " + CellZoom + "). Vartijoita suurennettu " + guards + " (kerroin " + GuardScale + "), suojalaatikoita poistettu " + crates + ", Ricoja pienennetty " + ricos + ", Rexejä isonnettu " + rexes + " (kerroin " + RexScale + ").\nVartijoita voi kutsua pillillä lisää: ne tulevat vartijan kopioina samalla koolla.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }

    /// Sellikäytävien (D/E) kuvat 10 % isommiksi ja lisälattiaa alas nykyiseen sceneen (ilman 355/358:n uudelleenajoa, joka rikkoisi ovien yhteydet).
    /// Vaihtaa kuvat *_iso-versioihin, skaalaa alueen mitat, ovet, vihut ja kyltit saman muunnoksen mukaan (x ja y 1,1-kertaisiksi yläreunan ja X0:n suhteen)
    /// sekä asettaa kameran pystysiirrot (alakerta alhaalla, yläkerta ylhäällä). Turvallista ajaa uudelleen (ohittaa jo korjatun).
    [MenuItem("Beat em up/369. Vankila: sellikäytävät 10 % isommiksi ja lisälattiaa alas (nykyiseen sceneen)", false, 369)]
    static void FixHallScales()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(PrisonScenePath)) { Info("Aja ensin valikko 353."); return; }
        const string T = "Assets/Sprites/Taustat/";
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            float H = 2f * CamHalf, top = CamY + CamHalf, s = PrisonHallZoom;
            float ppu = CorrImgH / H / PrisonHallZoom, imgH = CorrImgH + HallFloorExt, Hh = CorrImgH / ppu, img1 = CorrImgW / ppu, imgW = 2f * img1;
            var areas = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(a => a != null && a.gameObject.scene == scene).ToArray();
            var doors = Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(d => d != null && d.gameObject.scene == scene).ToArray();
            var enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(e => e != null && e.gameObject.scene == scene).ToArray();
            var labels = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t != null && t.gameObject.scene == scene && t.name.StartsWith("Kyltti")).ToArray();
            var report = new System.Collections.Generic.List<string>();
            foreach (var spec in new[] { ("Vankilan D-osasto", "D-osasto", "vankila_kaytava_d1_iso", "vankila_kaytava_d1b_iso"), ("Vankilan E-osasto", "E-osasto", "vankila_kaytava_e1_iso", "vankila_kaytava_e1b_iso") })
            {
                var hall = areas.FirstOrDefault(a => a.areaName == spec.Item1);
                if (hall == null) { report.Add(spec.Item1 + ": ei löytynyt"); continue; }
                if (hall.camOffsetY < -0.01f) { report.Add(spec.Item1 + ": jo korjattu"); continue; }
                float X0 = hall.walkMinX - 1.5f, oldW = hall.walkMaxX - hall.walkMinX + 3f;
                float MapX(float x) => X0 + (x - X0) * s;
                float MapY(float y) => top - (top - y) * s;
                bool InHall(float x) => x >= X0 - 1f && x <= X0 + oldW + 1f;
                // kuvat
                var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == spec.Item1);
                if (root == null) { report.Add(spec.Item1 + ": juuri puuttuu"); continue; }
                for (int i = 0; i < 2; i++)
                {
                    var sp = ImportShipSprite(T + (i == 0 ? spec.Item3 : spec.Item4) + ".png", ppu, false, SpriteAlignment.Center);
                    var tr = root.transform.Find(spec.Item2 + " " + (i + 1));
                    if (sp == null || tr == null) { report.Add(spec.Item1 + ": kuva " + (i + 1) + " puuttuu"); continue; }
                    var sr = tr.GetComponent<SpriteRenderer>(); Undo.RecordObject(sr, "Sellikäytävä"); Undo.RecordObject(tr, "Sellikäytävä");
                    sr.sprite = sp; tr.position = new Vector3(X0 + img1 * (i + 0.5f), top - imgH / (2f * ppu), 0f); EditorUtility.SetDirty(sr); EditorUtility.SetDirty(tr);
                }
                // ovet: sijainti (jos hallissa), saapumispiste (jos kohde on halli), leveys
                foreach (var d in doors)
                {
                    Undo.RecordObject(d, "Sellikäytävä"); Undo.RecordObject(d.transform, "Sellikäytävä");
                    if (InHall(d.transform.position.x)) { var p = d.transform.position; d.transform.position = new Vector3(MapX(p.x), MapY(p.y), p.z); d.halfWidth *= s; }
                    if (d.target == hall) d.spawnPoint = new Vector2(MapX(d.spawnPoint.x), MapY(d.spawnPoint.y));
                    EditorUtility.SetDirty(d); EditorUtility.SetDirty(d.transform);
                }
                foreach (var e in enemies)
                    if (InHall(e.transform.position.x)) { Undo.RecordObject(e.transform, "Sellikäytävä"); var p = e.transform.position; e.transform.position = new Vector3(MapX(p.x), MapY(p.y), p.z); EditorUtility.SetDirty(e.transform); }
                foreach (var t in labels)
                    if (InHall(t.transform.position.x)) { Undo.RecordObject(t, "Sellikäytävä"); Undo.RecordObject(t.transform, "Sellikäytävä"); var p = t.transform.position; t.transform.position = new Vector3(MapX(p.x), MapY(p.y), p.z); t.characterSize *= s; EditorUtility.SetDirty(t); EditorUtility.SetDirty(t.transform); }
                // alue
                float lowMax = top - 0.905f * Hh, lowMin = top - (imgH - 9.2f) / ppu, upMax = top - 0.415f * Hh, upMin = top - 0.47f * Hh, lowerOff = -(imgH / ppu - H);
                float W = 1672f / (941f / H);
                Undo.RecordObject(hall, "Sellikäytävä");
                hall.maxDepthY = lowMax; hall.minDepthY = lowMin;
                hall.walkMinX = X0 + 1.5f; hall.walkMaxX = X0 + imgW - 1.5f;
                hall.camMinX = X0 + W * 0.5f; hall.camMaxX = Mathf.Max(hall.camMinX, X0 + imgW - W * 0.5f);
                hall.camOffsetY = lowerOff; hall.camRiseY = 0f;
                hall.depthLimits = new[] { new Vector2(X0, upMax), new Vector2(X0 + imgW, upMax) };
                EditorUtility.SetDirty(hall);
                var tf = root.GetComponent<TwoFloorCorridor>();
                if (tf != null)
                {
                    Undo.RecordObject(tf, "Sellikäytävä");
                    tf.lowerMin = lowMin; tf.lowerMax = lowMax; tf.upperMin = upMin; tf.upperMax = upMax; tf.lowerOffsetY = lowerOff; tf.upperOffsetY = 0f;
                    tf.stairs = new[] {
                        new TwoFloorCorridor.Stair { xBottom = X0 + 0.17f * img1, xTop = X0 + 0.345f * img1, yBottom = top - 0.91f * Hh, yTop = top - 0.44f * Hh },
                        new TwoFloorCorridor.Stair { xBottom = X0 + 1.83f * img1, xTop = X0 + 1.655f * img1, yBottom = top - 0.91f * Hh, yTop = top - 0.44f * Hh } };
                    EditorUtility.SetDirty(tf);
                }
                report.Add(spec.Item1 + ": ok");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Sellikäytävät: " + string.Join(", ", report) + ".\nKuvat 10 % isommat, lattiaa " + HallFloorExt + " px lisää alas; kamera on alakerrassa alempana ja yläkerrassa ylempänä.\nVihut, ovet ja kyltit siirtyivät kuvan mukana. Jos jokin vihollinen on huonossa paikassa, aja 363/366/368 uudelleen.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
