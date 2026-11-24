using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rantabaarin sisätilat (valikko 424): kolme 5000 x 1884 -kuvaa (output/imagegen/total-fist-rantabaari-sisatilat-3x5k): baaritiski, esiintymislava, terassi.
/// Alue "Rantabaari" Linnake-scenessä. Sohvi tiskin takana (kauppa: kokteilit, tiskin etuosa leikattu kuvasta Sohvin eteen), mariachi-bändi lavalla (kopio El Loipparin bändistä),
/// musiikki sama kuin El Loipparissa (Uccopulco). Terassin avoimen sivun takana yötaivas ja meri. Ovet: ulkobaarin portaat (vasen) ja terassin portaat (oikea) <-> sisätilat.
/// Vaatii valikon 423 (loppuranta). Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    const string FortBarAreaName = "Rantabaari";
    const float FortBarX0 = 81000f, FortBarImgPx = 5000f, FortBarImgPy = 1884f, FortBarFloorFar = 1235f, FortBarFloorNear = 1780f;
    // ulkobaarin portaat (ulkokuvissa, 5 palan ranta: px vasemmasta reunasta) ja vastaavat paikat sisällä (px sisätilojen vasemmasta reunasta)
    const float FortBarOutLeftStairsPx = 900f, FortBarOutRightStairsPx = 7250f, FortBarInLeftPx = 450f, FortBarInRightPx = 14550f;
    // tiski (kuvan 1 rivit/sarakkeet), Sohvin paikka ja lavan lattiarivi
    const float FortBarCounterX0 = 520f, FortBarCounterX1 = 4770f, FortBarCounterTop = 955f, FortBarCounterBottom = 1220f, FortBarSohviPx = 2600f, FortBarStageFloorRow = 1115f, FortBarStagePx = 7500f;

    [MenuItem("Beat em up/424. Linnake: rantabaarin sisätilat (tiski + Sohvi, lava + mariachi-bändi, terassi, ovet)", false, 424)]
    static void AddFortressBeachBar()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        for (int i = 1; i <= 3; i++)
        {
            string d = T + "rantabaari_sisa_" + i + ".png";
            if (!File.Exists(d)) { Info("Puuttuu " + d + " (kopioi kuvat kansiosta output/imagegen/total-fist-rantabaari-sisatilat-3x5k-uusi-kuvitus)."); return; }
            AssetDatabase.ImportAsset(d, ImportAssetOptions.ForceUpdate);
        }
        // tiskin etuosa: leikataan kuvasta 1 (peittää Sohvin jalat; Sohvi tiskin takana)
        string stripPath = T + "rantabaari_tiski_etu.png";
        {
            var src = new Texture2D(2, 2); src.LoadImage(File.ReadAllBytes(T + "rantabaari_sisa_1.png"));
            int cx0 = (int)FortBarCounterX0, cw = (int)(FortBarCounterX1 - FortBarCounterX0), cy0 = (int)(FortBarImgPy - FortBarCounterBottom), ch = (int)(FortBarCounterBottom - FortBarCounterTop);   // Texture2D: y alhaalta
            var strip = new Texture2D(cw, ch, TextureFormat.RGBA32, false); strip.SetPixels(src.GetPixels(cx0, cy0, cw, ch)); strip.Apply();
            File.WriteAllBytes(stripPath, strip.EncodeToPNG()); Object.DestroyImmediate(src); Object.DestroyImmediate(strip);
            AssetDatabase.ImportAsset(stripPath, ImportAssetOptions.ForceUpdate);
        }
        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        UnityEngine.SceneManagement.Scene Open(string path)
        {
            var s = EditorSceneManager.GetSceneByPath(path);
            if (!s.IsValid() || !s.isLoaded) { s = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(s); }
            return s;
        }
        try
        {
            var sc = Open(scenePath);
            var beach = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == sc && a.areaName == FortBeachAreaName);
            if (beach == null) { Info("Loppuranta puuttuu: aja valikko 423."); return; }
            foreach (var n in new[] { "Rantabaari", "Alue: Rantabaari", "Rantabaarin ovet" })
                foreach (var o in sc.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, ppu = FortBarImgPy / H, W = 1672f / (941f / H), cy = CamY;
            float imgW = FortBarImgPx / ppu, X0 = FortBarX0, widthU = 3f * imgW;
            var root = new GameObject("Rantabaari"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Rantabaari");
            SpriteRenderer Put(string name, Sprite sp, Vector3 pos, int order)
            {
                var sr = new GameObject(name).AddComponent<SpriteRenderer>();
                sr.transform.SetParent(root.transform, false); sr.sprite = sp; sr.sortingOrder = order; sr.transform.position = pos; return sr;
            }
            for (int i = 0; i < 3; i++)
            {
                var sp = ImportShipSprite(T + "rantabaari_sisa_" + (i + 1) + ".png", ppu, false, SpriteAlignment.Center);
                if (sp == null) { Info("Kuvan rantabaari_sisa_" + (i + 1) + " tuonti epäonnistui."); return; }
                Put("Sisätila " + (i + 1), sp, new Vector3(X0 + imgW * (i + 0.5f), cy, 0f), -10000);
            }

            var area = ReuseArea("Alue: Rantabaari");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(area.gameObject, sc);
            area.areaName = FortBarAreaName; area.useSidewalk = false;
            area.maxDepthY = cy + (FortBarImgPy * 0.5f - FortBarFloorFar) / ppu; area.minDepthY = cy + (FortBarImgPy * 0.5f - FortBarFloorNear) / ppu;
            area.walkMinX = X0 + 1.5f; area.walkMaxX = X0 + widthU - 1.5f;
            area.camMinX = X0 + W * 0.5f; area.camMaxX = Mathf.Max(area.camMinX, X0 + widthU - W * 0.5f);
            area.camSize = CamHalf; area.camRiseY = 0f; area.camOffsetY = 0f; area.backScale = 1f; area.depthLimits = new Vector2[0]; area.gravityScale = 1f;
            var mus = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Uccopulco.mp3");   // El Loipparin musiikki (ei S-Club)
            area.music = mus; area.musicGain = 1f;

            // terassin avoimen sivun takana yötaivas ja meri (parallaksit, näkyvät vain tässä alueessa)
            var seaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/RantaMeri.mat");
            void Para(string name, string file, float factor, int order, int copies, Material mat)
            {
                var sp = ImportShipSprite(T + file, FortBeachImgPy / H, false, SpriteAlignment.Center); if (sp == null) return;
                var sr = Put(name, sp, new Vector3(area.camMaxX + (W * 0.5f - sp.bounds.size.x * 0.5f), cy, 0f), order);
                if (mat != null) sr.sharedMaterial = mat;
                var pr = sr.gameObject.AddComponent<ParallaxRight>();
                pr.factor = factor; pr.endCamX = area.camMaxX; pr.mirrorCopies = copies; pr.limitRange = true; pr.rangeMin = area.camMinX - 2f; pr.rangeMax = area.camMaxX + 2f;
            }
            Para("Taivas", "ranta_1.png", 0.02f, -10400, 6, null); Para("Ulappa ja saaret", "ranta_3.png", 0.18f, -10300, 6, seaMat); Para("Lähimeri", "ranta_4.png", 0.45f, -10200, 6, seaMat);

            // tiskin etuosa (peittää Sohvin jalat) ja Sohvi + kauppa tiskin taakse (mallit Vegasin kanttiinista / kasinon baarista)
            string sohviInfo = "Sohvia ei lisätty (aja valikko 376: Vegasin Sohvi ja tiski ovat mallit)";
            var vegasPath = "Assets/Scenes/Areas/Vegas.unity"; var uccoPath = "Assets/Scenes/Areas/UccoLaiva.unity";
            var stripSp = ImportShipSprite(stripPath, ppu, false, SpriteAlignment.Center);
            float counterCx = X0 + (FortBarCounterX0 + FortBarCounterX1) * 0.5f / ppu, counterCy = cy + (FortBarImgPy * 0.5f - (FortBarCounterTop + FortBarCounterBottom) * 0.5f) / ppu;
            if (stripSp != null) Put("Tiskin etu", stripSp, new Vector3(counterCx, counterCy, 0f), -9980);   // Sohvin (-9990) edessä, hahmojen takana
            if (File.Exists(vegasPath))
            {
                var vs = Open(vegasPath);
                var sohviT = Object.FindObjectsByType<Dancer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == vs && c.name == "Sohvi (kasinon baari)");
                var counterT = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == vs && c.name == "Kanttiinin tiski (Sohvi)")
                               ?? Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(c => c != null && c.gameObject.scene == vs);
                if (sohviT != null && counterT != null)
                {
                    float hx = X0 + FortBarSohviPx / ppu, hy = cy + (FortBarImgPy * 0.5f - (FortBarCounterBottom - 27f)) / ppu;   // jalat juuri tiskin etuosan alareunan yläpuolella: tiski peittää jalat
                    var sg = Object.Instantiate(sohviT.gameObject); sg.name = "Sohvi (rantabaari)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sg, sc); sg.transform.SetParent(root.transform, false);
                    sg.transform.position = new Vector3(hx, hy, 0f); sg.transform.localScale = Vector3.one * 1.35f;
                    var rS = sg.GetComponentInChildren<SpriteRenderer>(); if (rS != null) rS.sortingOrder = -9990;
                    var shopGo = Object.Instantiate(counterT.gameObject); shopGo.name = "Rantabaarin tiski (Sohvi)";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shopGo, sc); shopGo.transform.SetParent(root.transform, false);
                    shopGo.transform.position = new Vector3(hx, area.maxDepthY, 0f);
                    var shop = shopGo.GetComponent<ShopCounter>();
                    shop.here = area; shop.title = "RANTABAARI"; shop.npcName = "Sohvi"; shop.prompt = "Puhu Sohville"; shop.halfWidth = 6f; shop.maxDistanceFromWall = 50f; shop.ownItemsOnly = false;
                    shop.items = new[]
                    {
                        new ShopCounter.Item { name = "Kookoskokteili", price = 6, heal = 35, stamina = 20, comment = "Kuutamoranta ja kylmä kookos: lomalle sopiva." },
                        new ShopCounter.Item { name = "Kylmä olut", price = 4, heal = 15, stamina = 35, comment = "Aaltojen ääntä ja vaahtoa." },
                        new ShopCounter.Item { name = "Grillimakkara", price = 8, heal = 60, stamina = 0, comment = "Rantabaarin grillistä, vielä kuumana." },
                        new ShopCounter.Item { name = "Mango-smoothie", price = 5, heal = 40, stamina = 40, comment = "Raikas ja kirkkaan keltainen." },
                    };
                    EditorUtility.SetDirty(shop);
                    sohviInfo = "Sohvi tiskillä (x " + hx.ToString("0.0") + ", kauppa: 4 tuotetta)";
                }
            }
            // mariachi-bändi lavalle: kopio El Loipparin bändistä (UccoLaiva)
            string bandInfo = "bändiä ei lisätty (aja valikko 52: El Loipparin lava)";
            if (File.Exists(uccoPath))
            {
                var us = Open(uccoPath);
                var bandT = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(r => r != null && r.gameObject.scene == us && r.gameObject.name == "Loipparin bändi");
                if (bandT != null)
                {
                    var bg = Object.Instantiate(bandT.gameObject); bg.name = "Rantabaarin bändi";
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bg, sc); bg.transform.SetParent(root.transform, true);
                    var bsr = bg.GetComponent<SpriteRenderer>();
                    // uusi bändianimaatio (bandi_ranta.png): alkaa liikkeestä, toistuu edestakaisin (ping-pong), keskitempo ~89 BPM
                    string nbp = FindTexture("bandi_ranta");
                    if (nbp != null)
                    {
                        SetupAndSlice(nbp);
                        var ns = LoadSprites("bandi_ranta").OrderBy(q => int.TryParse(q.name.Substring(q.name.LastIndexOf('_') + 1), out int n) ? n : 0).ToArray();
                        if (ns.Length > 2)
                        {
                            var pp = new System.Collections.Generic.List<Sprite>(ns);
                            for (int k = ns.Length - 2; k >= 1; k--) pp.Add(ns[k]);
                            var dn = bg.GetComponent<Dancer>();
                            if (dn != null) { dn.sprites = pp.ToArray(); dn.keepOrder = true; dn.frameTime = 0.112f; dn.startFrame = 0; dn.flipX = false; }
                            bsr.sprite = ns[0];
                        }
                    }
                    float h0 = Mathf.Max(0.01f, bsr.sprite.bounds.size.y); bg.transform.localScale = Vector3.one * (4.2f / h0);   // bändin korkeus ~4,2 yksikköä
                    float feetY = cy + (FortBarImgPy * 0.5f - FortBarStageFloorRow) / ppu;
                    bg.transform.position = new Vector3(X0 + FortBarStagePx / ppu, feetY, 0f);
                    bg.transform.position += new Vector3(0f, feetY - bsr.bounds.min.y, 0f);
                    bsr.sortingOrder = -9985; bsr.color = new Color(1f, 0.95f, 0.88f);
                    bandInfo = "mariachi-bändi lavalla (x " + bg.transform.position.x.ToString("0.0") + ")";
                }
            }

            // ovet: ulkobaarin portaat (vasen) ja terassin portaat (oikea) <-> sisätilat
            var doorRoot = new GameObject("Rantabaarin ovet"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(doorRoot, sc); Undo.RegisterCreatedObjectUndo(doorRoot, "Rantabaarin ovet");
            float beachImgW = FortBeachImgPx / (FortBeachImgPy / H), beachX0 = FortBeachX0 - FortBeachLeft.Length * beachImgW;
            float OutX(float px) => beachX0 + px / (FortBeachImgPy / H);
            float OutMidY() => Mathf.Lerp(beach.maxDepthY, beach.minDepthY, 0.45f);
            float InMidY() => Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.45f);
            Door Door2(string name, string prompt, Area here, Area target, float x, Vector2 spawn)
            {
                var d = new GameObject(name).AddComponent<Door>();
                d.transform.SetParent(doorRoot.transform, false); d.transform.position = new Vector3(x, here.maxDepthY, 0f);
                d.prompt = prompt; d.here = here; d.target = target; d.spawnPoint = spawn; d.halfWidth = 2.4f; d.maxDistanceFromWall = 50f;
                return d;
            }
            Door2("Ranta: baarin sisäänkäynti", "Mene baariin", beach, area, OutX(FortBarOutLeftStairsPx), new Vector2(X0 + FortBarInLeftPx / ppu + 2.5f, InMidY()));
            Door2("Ranta: terassin sisäänkäynti", "Mene baariin", beach, area, OutX(FortBarOutRightStairsPx), new Vector2(X0 + FortBarInRightPx / ppu - 2.5f, InMidY()));
            var exitL = Door2("Baari: ulos vasemmalle", "Poistu baarista", area, beach, X0 + FortBarInLeftPx / ppu, new Vector2(OutX(FortBarOutLeftStairsPx) + 2.8f, OutMidY()));
            var exitR = Door2("Baari: ulos terassille", "Poistu baarista", area, beach, X0 + FortBarInRightPx / ppu, new Vector2(OutX(FortBarOutRightStairsPx) - 2.8f, OutMidY()));
            foreach (var ex in new[] { exitL, exitR }) { ex.endCredits = true; ex.confirmText = "Poistutko baarista? Peli päättyy ja lopputekstit alkavat."; }   // baarista poistuminen = pelin loppu (lopputekstit)

            EditorUtility.SetDirty(area);
            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Rantabaarin sisätilat rakennettu (x " + X0.ToString("0") + "–" + (X0 + widthU).ToString("0") + "): tiski, lava ja terassi. " + sohviInfo + "; " + bandInfo + ". Musiikki: Uccopulco (El Loippari). Ovet: ulkobaarin portaat (x " + OutX(FortBarOutLeftStairsPx).ToString("0.0") + ") ja terassin portaat (x " + OutX(FortBarOutRightStairsPx).ToString("0.0") + ") <-> sisätilat.");
        }
        finally { foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true); }
    }

    /// <summary>Loppuranta (423) + rantabaarin sisätilat (424) + aloitus katolta (420).</summary>
    public static void RunBeachBarBatch()
    {
        batch = true;
        try { Step("423 loppuranta", AddFortressBeach); Step("424 rantabaari", AddFortressBeachBar); Step("420 aloitus", StartAtFortressRoof); }
        finally { batch = false; }
    }
}
