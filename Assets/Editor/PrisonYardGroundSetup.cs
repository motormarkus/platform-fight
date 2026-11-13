using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Vankilan pihan lähiparallaksi (valikko 377): maanpinta (vankila_maa_lahi.png), joka liikkuu 0,5 kameran vauhdista ja peilautuu kerran.
/// Lisää vain tämän kerroksen olemassa olevaan Vankila-sceneen (ei rakenna pihaa uudelleen, ovet säilyvät).
/// </summary>
public static partial class BeatEmUpSetup
{
    const float FenceStartPx = 830f, FenceEndPx = 5830f;   // aidan alku (portin aidan jälkeen) ja loppu (terminaalin seinä) pihan kuvan x:ssä, px
    [MenuItem("Beat em up/377. Vankila: pihan maa, aita, musiikki, saumat, katon lattia ja kopteri, henkilökunnan ikkunat", false, 377)]
    static void AddPrisonYardGround()
    {
        if (PlayModeBlocked()) return;
        const string T = "Assets/Sprites/Taustat/";
        if (!File.Exists(T + "vankila_maa_lahi.png")) { Info("Puuttuu kuva vankila_maa_lahi.png (Assets/Sprites/Taustat)."); return; }
        if (!File.Exists(PrisonScenePath)) { Info("Vankila-scenea ei ole: aja ensin valikko 353."); return; }
        var scene = EditorSceneManager.GetSceneByPath(PrisonScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(PrisonScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vankila");
            if (area == null) { Info("Aluetta \"Vankila\" ei löytynyt: aja valikko 353."); return; }
            foreach (var o in scene.GetRootGameObjects().Where(g => g.name == "Vankilan lähiparallaksi").ToArray()) Undo.DestroyObjectImmediate(o);

            float ppu = 941f / (2f * CamHalf), W = 1672f / ppu, cy = CamY;
            var sp = ImportShipSprite(T + "vankila_maa_lahi.png", ppu, false, SpriteAlignment.Center);
            if (sp == null) { Info("Kuvan tuonti epäonnistui: vankila_maa_lahi"); return; }
            var root = new GameObject("Vankilan lähiparallaksi");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Vankila maa");
            var sr = new GameObject("Vankilan maa (lähiparallaksi)").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(root.transform, false);
            sr.sprite = sp; sr.sortingOrder = -10040;   // pihan taka-parallaksin (-10050) edessä, pelitason kuvien (-10000) takana
            const float gs = 0.5f;   // puolet pienempi: maa on vain ruudun alaosassa, keskimmäinen parallaksi (piha) jää näkyviin sen yläpuolelle
            sr.transform.localScale = new Vector3(gs, gs, 1f);
            float left = area.camMinX - W * 0.5f;
            sr.transform.position = new Vector3(left + sp.bounds.size.x * gs * 0.5f, cy - CamHalf * (1f - gs), 0f);   // alareuna ruudun alareunassa
            var pl = sr.gameObject.AddComponent<ParallaxLayer>();
            pl.factor = 0.5f; pl.mirrorCopies = 3;   // kuva on nyt puolet kapeampi: peilikopioita tarvitaan enemmän
            pl.startCamX = area.camMinX; pl.minX = area.camMinX - 2f; pl.maxX = area.camMaxX + 2f;
            // verkkoaita (loopattava kuva, kopio viidakon työmaan aidasta) tien yläreunan tasalle koko pihan matkalle: alareuna = tien yläreuna (area.maxDepthY)
            string fenceNote = "";
            float fenceH = 0.17f * 2f * CamHalf;   // korkeus 17 % ruudusta
            var fsp = ImportShipSprite(T + "vankila_aita_silmukka.png", 703f / fenceH, false, SpriteAlignment.Center);
            if (fsp == null) fenceNote = " Aitaa ei lisätty: vankila_aita_silmukka.png puuttuu.";
            else
            {
                // aita alkaa portin aidan jälkeen (kuvan x ~980 px) ja päättyy terminaalin seinään (~5830 px); yksi toistuva (tiled) pala, joten pää katkeaa suoraan
                float fx0 = PrisonX0 + FenceStartPx / ppu, fx1 = PrisonX0 + FenceEndPx / ppu;
                var fr = new GameObject("Vankilan aita (tien yläreuna)").AddComponent<SpriteRenderer>();
                fr.transform.SetParent(root.transform, false);
                fr.sprite = fsp; fr.drawMode = SpriteDrawMode.Tiled; fr.tileMode = SpriteTileMode.Continuous; fr.size = new Vector2(fx1 - fx0, fenceH);
                fr.sortingOrder = -9990;   // pihan kuvien (-10000) edessä, hahmojen takana
                fr.transform.position = new Vector3((fx0 + fx1) * 0.5f, area.maxDepthY + fenceH * 0.5f, 0f);
                fenceNote = " Verkkoaita x " + fx0.ToString("0.0") + "–" + fx1.ToString("0.0") + " (portin jälkeen terminaalin seinään), alareuna tien yläreunassa y = " + area.maxDepthY.ToString("0.00") + ".";
            }
            var vm = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Musiikki/Vankila.mp3");   // pihan taustamusiikki (Jammin' in the Groove), voimakkuus tasattu muuhun musiikkiin
            if (vm != null)
            {
                // kaikki vankilan alueet (piha, terminaali, käytävät, sellit, työpaja, ruokala, katto, valvontahuone...) saivat musiikin kopioimalla pihalta: päivitetään ne kaikki
                foreach (var ar in Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(q => q.gameObject.scene == scene))
                { ar.music = vm; ar.musicGain = PrisonMusicGain; EditorUtility.SetDirty(ar); }
            }
            // --- henkilökunnan huone: ikkunoista näkyy vankilan piha parallaksina (sky-kerroksen alaosa on tasaista sinistä, joten alakerran ikkunoissa ei näkynyt mitään) ---
            {
                float Hs = 2f * CamHalf, Ws = 1672f / (941f / Hs);
                var staff = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vankilan henkilökunta");
                var sroot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan henkilökunta");
                var pihaSp = AssetDatabase.LoadAssetAtPath<Sprite>(T + "vankila_piha_taka.png");
                if (staff != null && sroot != null && pihaSp != null && sroot.transform.Find("Henkilökunnan vankilan piha") == null)
                {
                    float left2 = staff.camMinX - Ws * 0.5f;
                    var sr2 = new GameObject("Henkilökunnan vankilan piha").AddComponent<SpriteRenderer>();
                    sr2.transform.SetParent(sroot.transform, false); sr2.sprite = pihaSp; sr2.sortingOrder = -10050;
                    sr2.transform.position = new Vector3(left2 + pihaSp.bounds.size.x * 0.5f, cy, 0f);
                    var pl2 = sr2.gameObject.AddComponent<ParallaxLayer>();
                    pl2.factor = 0.3f; pl2.startCamX = staff.camMinX; pl2.minX = staff.camMinX - 2f; pl2.maxX = staff.camMaxX + 2f;
                    fenceNote += " Henkilökunnan huoneen ikkunoihin lisätty pihaparallaksi.";
                }
            }
            // --- katto: lattiaa lisää alas (kamera seuraa pystysuunnassa kuten Vegasissa/käytävillä), kopteri isommaksi ---
            {
                float Hr = 2f * CamHalf, ppuR = 1844f / Hr;
                var roofA = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vankilan katto");
                var rroot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vankilan katto");
                if (roofA != null && rroot != null)
                {
                    float extra = 0.4f * Hr;
                    if (roofA.camRiseY < 0.01f)
                    {
                        Undo.RecordObject(roofA, "Katon lattia");
                        roofA.minDepthY -= extra; roofA.camRiseY = extra; roofA.camOffsetY = -extra;
                        EditorUtility.SetDirty(roofA);
                    }
                    for (int i = 1; i <= 2; i++)
                    {
                        var img = rroot.transform.Find("Katto " + i);
                        var old = rroot.transform.Find("Katto matto alas " + i); if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                        var msp = ImportShipSprite(T + "vankila_katto_matto_" + i + ".png", ppuR, false, SpriteAlignment.Center);
                        if (img == null || msp == null) continue;
                        var msr = new GameObject("Katto matto alas " + i).AddComponent<SpriteRenderer>();
                        msr.transform.SetParent(rroot.transform, false); msr.sprite = msp; msr.sortingOrder = -10005;
                        msr.transform.position = new Vector3(img.position.x, img.position.y - Hr * 0.5f - msp.bounds.size.y * 0.5f, 0f);
                        msr.transform.localScale = new Vector3(1.0018f, 1f, 1f);
                    }
                    var heli = rroot.transform.Find("Vankilan katon kopteri");
                    if (heli != null) { Undo.RecordObject(heli, "Kopteri"); heli.localScale = new Vector3(1.8f, 1.8f, 1f); EditorUtility.SetDirty(heli); }
                    fenceNote += " Katolle lisätty lattiaa alas (0,4 ruutua, kamera seuraa) ja kopteri suurennettu 1,8-kertaiseksi.";
                }
            }
            // --- Warden: pienempi kuva (oli liian iso), kovempi vastus (nopeampi, vähemmän taukoa, enemmän elämää); sheetteihin hento ääriviiva tehty skriptillä ---
            {
                var wardenE = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.gameObject.scene == scene && x.displayName == "Warden");
                if (wardenE != null)
                {
                    Undo.RecordObject(wardenE, "Warden");
                    wardenE.visualScale = 1.0f; wardenE.moveSpeedX = 4.0f; wardenE.moveSpeedY = 2.8f; wardenE.attackCooldown = 0.15f;
                    EditorUtility.SetDirty(wardenE);
                    fenceNote += " Warden: kuva 1,0 (oli 1,2), elämät ennallaan, nopeampi ja vähemmän taukoa iskujen välillä.";
                }
            }
            // saumat: vierekkäiset taustakuvat (järjestys -10000) leventään ~1 px:llä, ettei kuvien väliin jää ohutta rakoa (esim. valvontahuoneen keskellä)
            int widened = 0;
            foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (r.gameObject.scene == scene && r.sortingOrder == -10000 && r.sprite != null && r.sprite.bounds.size.x > 15f && Mathf.Approximately(r.transform.localScale.x, 1f) && Mathf.Approximately(r.transform.localScale.y, 1f)
                    && r.GetComponent<ParallaxLayer>() == null)
                { Undo.RecordObject(r.transform, "Sauma"); r.transform.localScale = new Vector3(1.0018f, 1f, 1f); EditorUtility.SetDirty(r.transform); widened++; }
            fenceNote += " Saumoja peitetty " + widened + " taustakuvalta (leveys +0,18 %).";
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Vankilan pihalle lisätty lähiparallaksi (maanpinta, kerroin 0,5, mittakaava 0,5 alareunassa, kolme peilikopiota, järjestys -10040)." + fenceNote + "\nJos horisontti tai aidan korkeus ei sovi, kerroksia voi siirtää tai skaalata. Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
