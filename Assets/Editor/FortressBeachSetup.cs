using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Loppuranta (valikko 423): kuutamoranta, johon pelastuskapseli laskeutuu aluksen tuhon jälkeen. Kuusi 5000 x 1884 -kerrosta
/// (output/imagegen/total-fist-loppuranta-kuutamo-parallaksit-5k): taivas, kuu, ulappa ja saaret, lähimeri ja vaahto (parallaksit),
/// ranta ja kapseli (maailmassa kiinni, hero kävelee hiekalla) sekä etualan palmut (heron edessä). Alue "Loppuranta" Linnake-scenessä.
/// Kytkee ShipFinalen: kapselilla pelastautunut hero siirtyy tänne. Vaatii valikon 419 (ShipFinale). Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    const string FortBeachAreaName = "Loppuranta";
    // rannan jatkopalat vasemmalle (oikealta vasemmalle): tiedosto Assets/Sprites/Taustat/. Oikea pala (ranta_5 + kapseli) pysyy paikallaan, joten laskeutumispaikka ei siirry.
    static readonly string[] FortBeachLeft = { "ranta_5b.png", "ranta_5b_peili.png", "ranta_baari_2.png", "ranta_baari_1.png" };
    // 5b = hiekkajatko; 5b_peili = sama PEILATTUNA (sen vasen reuna = terassikuvan oikea reuna täsmälleen, oikea reuna = 5b:n vasen reuna täsmälleen); baari_2 = terassi ja ranta; baari_1 = rantabaari (uloskuva)
    const float FortBeachPlanePx = 8380f, FortBeachPlaneWaterRow = 1310f;   // vesitaso: px (5 palan ranta) ja veden pinnan rivi
    const float FortBeachX0 = 80600f, FortBeachImgPx = 5000f, FortBeachImgPy = 1884f, FortBeachFloorFar = 1530f, FortBeachFloorNear = 1860f, FortBeachWalkMaxPx = 4650f, FortBeachSpawnPx = 3900f;

    [MenuItem("Beat em up/423. Linnake: loppuranta (kuutamoranta, 6 parallaksikerrosta, kapselin laskeutumispaikka)", false, 423)]
    static void AddFortressBeach()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        foreach (var lf in FortBeachLeft) { if (!File.Exists(T + lf)) { Info("Puuttuu " + T + lf); return; } AssetDatabase.ImportAsset(T + lf, ImportAssetOptions.ForceUpdate); }
        for (int i = 1; i <= 6; i++)
        {
            string d = T + "ranta_" + i + ".png";
            if (!File.Exists(d)) { Info("Puuttuu " + d + " (kopioi kuvat kansiosta output/imagegen/total-fist-loppuranta-kuutamo-parallaksit-5k)."); return; }
            AssetDatabase.ImportAsset(d, ImportAssetOptions.ForceUpdate);
        }
        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        var sc = EditorSceneManager.GetSceneByPath(scenePath);
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); opened.Add(sc); }
        try
        {
            foreach (var n in new[] { "Loppuranta", "Alue: Loppuranta" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, ppu = FortBeachImgPy / H, W = 1672f / (941f / H), cy = CamY;
            float imgW = FortBeachImgPx / ppu; int pieces = 1 + FortBeachLeft.Length;
            float X0 = FortBeachX0 - FortBeachLeft.Length * imgW;   // alueen vasen reuna; oikea reuna (X0 + pieces*imgW) pysyy samana
            var root = new GameObject("Loppuranta"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Loppuranta");
            SpriteRenderer Put(string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos; return sr;
            }
            var spr = new Sprite[7];
            for (int i = 1; i <= 6; i++) { spr[i] = ImportShipSprite(T + "ranta_" + i + ".png", ppu, false, SpriteAlignment.Center); if (spr[i] == null) { Info("Kuvan ranta_" + i + " tuonti epäonnistui."); return; } }

            var area = ReuseArea("Alue: Loppuranta");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, sc);
            area.areaName = FortBeachAreaName; area.useSidewalk = false;
            area.maxDepthY = cy + (FortBeachImgPy * 0.5f - FortBeachFloorFar) / ppu; area.minDepthY = cy + (FortBeachImgPy * 0.5f - FortBeachFloorNear) / ppu;
            area.walkMinX = X0 + 1.5f; area.walkMaxX = X0 + (pieces - 1) * imgW + FortBeachWalkMaxPx / ppu;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + pieces * imgW - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0];
            area.music = null; area.musicGain = 1f; area.gravityScale = 1f;

            // parallaksikerrokset (taakse -> eteen), oikeaan reunaan ankkuroituina ja peilikopioina vasemmalle (alue jatkuu vasemmalle):
            // taivas 0,02, kuu 0,04 (ei peilausta), ulappa ja saaret 0,18, lähimeri 0,45. Oikeassa päässä (kapseli) sommittelu on täsmälleen kuten kuvissa.
            // meren animaatio: BeachSea-shader (aallot ja vaahtopäät liikkuvat koodilla) kerroksille 3 ja 4 (materiaali Assets/Shaders/RantaMeri.mat)
            var seaShader = Shader.Find("PlatformFight/BeachSea"); Material seaMat = null;
            if (seaShader != null)
            {
                const string matPath = "Assets/Shaders/RantaMeri.mat";
                seaMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (seaMat == null) { seaMat = new Material(seaShader); AssetDatabase.CreateAsset(seaMat, matPath); }
                seaMat.shader = seaShader; EditorUtility.SetDirty(seaMat); AssetDatabase.SaveAssets();
            }
            void Para(string name, Sprite sp, float factor, int order, int copies, Material mat = null)
            {
                var sr = Put(name, sp, new Vector3(area.camMaxX + (W * 0.5f - sp.bounds.size.x * 0.5f), cy, 0f), order);
                if (mat != null) sr.sharedMaterial = mat;
                var pr = sr.gameObject.AddComponent<ParallaxRight>();
                pr.factor = factor; pr.endCamX = area.camMaxX; pr.mirrorCopies = copies; pr.limitRange = true; pr.rangeMin = area.camMinX - 2f; pr.rangeMax = area.camMaxX + 2f;
            }
            Para("Taivas", spr[1], 0.02f, -10400, 6); Para("Kuu", spr[2], 0.04f, -10390, 0); Para("Ulappa ja saaret", spr[3], 0.18f, -10300, 6, seaMat); Para("Lähimeri ja vaahto", spr[4], 0.45f, -10200, 6, seaMat);
            // ranta ja kapseli: kiinni maailmassa; etualan palmut heron edessä
            Put("Ranta ja kapseli", spr[5], new Vector3(X0 + imgW * (pieces - 0.5f), cy, 0f), -10000);
            Put("Etualan palmut", spr[6], new Vector3(X0 + imgW * (pieces - 0.5f), cy, 0f), 6000);
            for (int k = 0; k < FortBeachLeft.Length; k++)   // jatkopalat vasemmalle: oikeanpuoleisin ensin (sauma täsmää edelliseen)
            {
                var sp = ImportShipSprite(T + FortBeachLeft[k], ppu, false, SpriteAlignment.Center);
                if (sp != null) Put("Ranta jatko " + (k + 1), sp, new Vector3(X0 + imgW * (pieces - 1.5f - k), cy, 0f), -10000);
            }

            // rantakasvillisuus (sprite: Assets/Sprites/Props/ranta_kasvi_1 = heliconia, ranta_kasvi_2 = hibiskus, ranta_palmu_2 = kaksoispalmu): vedenrajan tuntumaan, hero kulkee niiden edessä.
            // Sijainnit: maailman px (0 = alueen vasen reuna; jatkopala 0-5000, ranta 5000-10000), sprite, leveys/korkeus yksikköinä, peilaus. Sauman kallio-palmuryhmän (4400-5700) ympäristö jätetty vapaaksi.
            var vegHeli = ImportShipSprite("Assets/Sprites/Props/ranta_kasvi_1.png", 1942f / 5.0f, false, SpriteAlignment.BottomCenter);
            var vegHib = ImportShipSprite("Assets/Sprites/Props/ranta_kasvi_2.png", 1774f / 3.6f, false, SpriteAlignment.BottomCenter);
            var vegPalm = ImportShipSprite("Assets/Sprites/Props/ranta_palmu_2.png", 1536f / 6.0f, false, SpriteAlignment.BottomCenter);
            int veg = 0;
            if (vegHeli != null && vegHib != null && vegPalm != null)
            {
                float vy = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.04f);
                void Veg(string nm, Sprite sp, float px, float scale, bool flip)
                {
                    float vx = X0 + (px + (pieces - 2) * FortBeachImgPx) / ppu;   // px mitattu 2 palan rannalle (jatkopala + ranta); lisäpalat vasemmalla siirtävät
                    var vs = Put(nm, sp, new Vector3(vx, vy, 0f), Mathf.RoundToInt(-vy * 100f) - 4);
                    vs.transform.localScale = new Vector3(scale, scale, 1f); vs.flipX = flip; veg++;
                }
                Veg("Kaksoispalmu 1", vegPalm, 1000f, 1.0f, false);
                Veg("Hibiskus 1", vegHib, 300f, 0.9f, false);
                Veg("Heliconia 1", vegHeli, 2000f, 1.0f, false);
                Veg("Hibiskus 2", vegHib, 2900f, 1.0f, true);
                Veg("Heliconia 2", vegHeli, 3700f, 0.85f, true);
                Veg("Hibiskus 3", vegHib, 6300f, 0.9f, true);
                Veg("Heliconia 3", vegHeli, 7000f, 1.0f, true);
                Veg("Hibiskus 4", vegHib, 7600f, 1.1f, false);
                Veg("Kaksoispalmu 2", vegPalm, 8100f, 0.9f, true);
                // peilatulla hiekkapalalla (px 5b:n vasemmasta reunasta vasemmalle, negatiivinen)
                Veg("Hibiskus 5", vegHib, -1500f, 0.95f, false);
                Veg("Heliconia 4", vegHeli, -2800f, 0.9f, true);
                Veg("Kaksoispalmu 3", vegPalm, -3300f, 0.95f, false);
                // peililiitoksen kallio (px -5000): pusikko vasemmalle (terassin puolelle), palmu ja pusikko oikealle
                Veg("Hibiskus 6", vegHib, -5350f, 1.0f, false);
                Veg("Kaksoispalmu 4", vegPalm, -4550f, 0.95f, true);
                Veg("Heliconia 5", vegHeli, -4150f, 0.85f, false);
            }

            // vesitaso kellumaan veteen vähän ennen baaria (baarin oikealla puolella): FloatingBoat keinuttaa koneen koodilla. Sijainti 5-palaisen rannan px:nä (0 = vasen reuna)
            var planeSp = ImportShipSprite("Assets/Sprites/Props/vesitaso.png", 1536f / 2.6f, false, SpriteAlignment.BottomCenter);   // 2,6 yksikköä leveä (pienennetty 60 %, kauempana merellä)
            string planeInfo = "vesitaso puuttuu";
            if (planeSp != null)
            {
                float planeX = X0 + (FortBeachPlanePx + (pieces - 5) * FortBeachImgPx) / ppu, planeWater = cy + (FortBeachImgPy * 0.5f - FortBeachPlaneWaterRow) / ppu;
                var pg = new GameObject("Vesitaso"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pg, sc); pg.transform.SetParent(root.transform, false);
                pg.transform.position = new Vector3(planeX, planeWater, 0f);
                var psr = pg.AddComponent<SpriteRenderer>(); psr.sprite = planeSp; psr.sortingOrder = -10100; psr.color = new Color(0.68f, 0.75f, 0.92f, 1f);   // hämärrytetty: tumma, sinertävä (yövalo, kauempana merellä)
                var fb = pg.AddComponent<FloatingBoat>(); fb.body = psr; fb.waterY = planeWater; fb.width = 2.3f; fb.bobAmp = 0.05f; fb.swayAmp = 0.03f; fb.rollDeg = 1.8f;
                planeInfo = "vesitaso kellumassa x " + planeX.ToString("0.0");
            }

            // rantaan nousevat ja vetäytyvät vaahtoaallot (ShoreWash, generoitu koodilla): hiekan yläreunan kohdalle
            var wash = new GameObject("Rantaaallot"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wash, sc); wash.transform.SetParent(root.transform, false);
            var sw = wash.AddComponent<ShoreWash>();
            sw.x0 = X0; sw.x1 = X0 + pieces * imgW; sw.waterY = cy + (FortBeachImgPy * 0.5f - 1490f) / ppu; sw.advance = 0.9f; sw.height = 0.7f; sw.sortingOrder = -9980;

            Vector2 spawn = new Vector2(X0 + (pieces - 1) * imgW + FortBeachSpawnPx / ppu, cy + (FortBeachImgPy * 0.5f - 1700f) / ppu);
            var fin = Object.FindObjectsByType<ShipFinale>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(f => f != null && f.gameObject.scene == sc);
            string info = "ShipFinalea ei löytynyt (aja valikko 419 ja sitten tämä)";
            if (fin != null) { fin.beach = area; fin.beachSpawn = spawn; EditorUtility.SetDirty(fin); info = "ShipFinale kytketty: kapselilla pelastautunut hero siirtyy tänne"; }
            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Loppuranta rakennettu (" + pieces + " palaa, kasvillisuutta " + veg + " kpl, " + planeInfo + ", x " + X0.ToString("0") + "–" + (X0 + imgW).ToString("0") + ", Linnake-scene): 6 kerrosta (taivas, kuu, ulappa, lähimeri parallakseina; ranta ja kapseli; etualan palmut). Hiekka: " + area.walkMinX.ToString("0") + "–" + area.walkMaxX.ToString("0") + ", laskeutumiskohta x " + spawn.x.ToString("0.0") + ". " + info + ".");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }

    /// <summary>Aluksen vihut ja kapseli (419) + loppuranta (423) + aloitus katolta (420).</summary>
    public static void RunBeachBatch()
    {
        batch = true;
        try { Step("419 vihut ja kapseli", AddFortressShipEnemies); Step("423 loppuranta", AddFortressBeach); Step("420 aloitus", StartAtFortressRoof); }
        finally { batch = false; }
    }
}
