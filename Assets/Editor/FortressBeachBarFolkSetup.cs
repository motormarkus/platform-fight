using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rantabaarin väki (valikko 425): Rex, toinen sankari (vain yksinpelissä), Helvi, Maria, Tom, Rekkamies, Puliukko, Rokkimimmi, Prätkäjätkä, Lippis, Betty, Violetti ja Candi.
/// Kukaan ei taistele: seisovat, kävelevät lyhyitä matkoja ja juttelevat (NpcStroller); Maria, Helvi ja Rokkimimmi tanssivat lavan edessä (Dancer).
/// Candi juttelee, kävelee tiskille, kääntyy selin ja nojaa siihen (NpcStrollerin nojaus). Vaatii valikon 424. Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/425. Linnake: rantabaarin väki (Rex, Candi, Violetti, Tom, tanssijat ym.)", false, 425)]
    static void AddFortressBeachBarFolk()
    {
        if (PlayModeBlocked()) return;
        string scenePath = "Assets/Scenes/Areas/" + (AreaSceneName("Muurin juhlahalli") ?? "Linnake") + ".unity";
        var sc = EditorSceneManager.GetSceneByPath(scenePath); bool openedHere = false;
        if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == sc && a.areaName == FortBarAreaName);
            if (area == null) { Info("Rantabaaria ei löydy: aja valikko 424."); return; }
            foreach (var o in sc.GetRootGameObjects().Where(g => g.name == "Rantabaarin väki").ToArray()) Undo.DestroyObjectImmediate(o);
            var root = new GameObject("Rantabaarin väki"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, sc); Undo.RegisterCreatedObjectUndo(root, "Rantabaarin väki");

            float H = 2f * CamHalf, ppu = FortBarImgPy / H;
            float PX(float px) => FortBarX0 + px / ppu;
            float Y(float depth) => Mathf.Lerp(area.maxDepthY, area.minDepthY, depth);
            var missing = new List<string>();
            Sprite[] Walk(string n) { var s = LoadWalkerSprites(n); if (s == null || s.Length == 0) { missing.Add(n); return null; } return s; }

            // NpcStroller-hahmo: seisoo, kävelee lanella px lane0..lane1, ajoittain juttelee
            NpcStroller Stroller(string name, Sprite[] idle, Sprite[] walk, Sprite[] talk, float px, float depth, float lane0, float lane1, float scale = 1f, float idleFt = 0.1f, float walkFt = 0.06f)
            {
                if (idle == null) return null;
                var go = new GameObject(name); go.transform.SetParent(root.transform, false); go.transform.position = new Vector3(PX(px), Y(depth), 0f); go.transform.localScale = Vector3.one * scale;
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false); vis.sprite = idle[0];
                var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
                sh.color = new Color(0f, 0f, 0f, 0.3f); sh.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
                var st = go.AddComponent<NpcStroller>();
                st.area = area; st.body = vis; st.shadow = sh; st.idleSprites = idle; st.walkSprites = walk ?? idle; st.talkSprites = talk;
                st.idleFrameTime = idleFt; st.walkFrameTime = walkFt; st.talkChance = talk != null ? 0.3f : 0f; st.speed = 1.3f;
                st.laneMinX = PX(lane0); st.laneMaxX = PX(lane1);
                st.idleSeconds = new Vector2(3f, 9f); st.walkDistance = new Vector2(1.5f, 4f);   // seisoskelee pitkään, kävelee lyhyitä matkoja
                return st;
            }
            void Dance(string name, List<Sprite> seq, float px, float depth, float ft, int start)
            {
                if (seq == null || seq.Count == 0) return;
                float y = Y(depth);
                var go = new GameObject(name); go.transform.SetParent(root.transform, false); go.transform.position = new Vector3(PX(px), y, 0f); go.transform.localScale = Vector3.one * SalsaGirlScale;
                var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = seq[0]; sr.sortingOrder = Mathf.RoundToInt(-y * 100f);
                var d = go.AddComponent<Dancer>(); d.sprites = seq.ToArray(); d.frameTime = ft; d.keepOrder = true; d.startFrame = start;
            }
            List<Sprite> Aero(string path, int take = 0) { var s = SliceAerobicsSheet(path); if (s == null) { missing.Add(path); return null; } return take > 0 ? s.Take(take).ToList() : s.ToList(); }

            // ---- tiskin ääressä (px 520–4770) ----
            Stroller("Puliukko", Walk("puliukko_idle"), Walk("puliukko_kavely"), null, 1000f, 0.12f, 800f, 1300f);
            Stroller("Rekkamies", Walk("rekkamies_calm"), Walk("rekkamies_kavely"), null, 1900f, 0.14f, 1650f, 2200f);
            var rex = Stroller("Rex", Walk("rex_idle"), Walk("rex_kavely"), null, 3200f, 0.14f, 3000f, 3500f, 1.15f);
            if (rex != null)
            {
                // Rex antaisi lentolipun (Clues.GlobePass) tässäkin, jos se puuttuu (normaalisti lippu tulee Wardenin jälkeen); paikka seuraa Rexiä
                var spot = new GameObject("Rexin lentolippu").AddComponent<ClueSpot>(); spot.transform.SetParent(rex.transform, false); spot.transform.localPosition = Vector3.zero;
                spot.prompt = "Puhu Rexille"; spot.halfWidth = 2.2f; spot.halfDepth = 1.8f;
                spot.clueId = Clues.GlobePass; spot.clueTitle = "Rexin lentolippu";
                spot.clueText = "Rex antoi sinulle lentolipun: \"Maailma on nyt auki. Minne vain.\" Maailmanpallo (F8 / kartta-nappi) aukeaa, ja voit matkustaa vapaasti.";
                spot.emptyText = "Rex: Mukavaa, kun pääsit tänne asti.";
            }
            Stroller("Tom", Walk("tom_idle"), Walk("tom_kavely"), null, 3900f, 0.12f, 3700f, 4100f);

            // ---- Candi: juttelee, kävelee tiskille ja nojaa siihen ----
            var cTalk = Walk("violetti_candi_puhe"); var cWalk = Walk("violetti_candi_kavely"); var cTurn = Walk("violetti_candi_kaanto"); var cBack = Walk("violetti_candi_selin");
            var candi = Stroller("Candi", cTalk, cWalk, cTalk, 5100f, 0.55f, 4600f, 5900f, 1f, 0.125f, 0.1f);
            if (candi != null)
            {
                candi.talkFrameTime = 0.125f; candi.talkChance = 0.6f;
                candi.leanTurnSprites = cTurn; candi.leanIdleSprites = cBack; candi.leanTurnFrameTime = 0.1f; candi.leanIdleFrameTime = 0.1f; candi.leanChance = 0.5f; candi.leanSeconds = new Vector2(8f, 16f);
                candi.leanMinX = PX(4350f); candi.leanMaxX = PX(4650f); candi.leanY = Mathf.Lerp(area.maxDepthY, area.minDepthY, 0.06f);
                candi.laneMinX = PX(4300f); candi.laneMaxX = PX(5900f);
            }

            // ---- tanssilattia lavan edessä ----
            Dance("Maria", Aero("Assets/Sprites/Maria/maria_tanssi_1.png", 80), 6400f, 0.72f, 0.1f, 0);
            Dance("Helvi", Aero("Assets/Sprites/Helvi/helvi_tanssi_1.png"), 7100f, 0.85f, 0.1f, 8);
            Dance("Rokkimimmi", Aero("Assets/Sprites/Rokkimimmi/rokkimimmi_tanssi_1.png", 100), 8700f, 0.75f, 0.1f, 20);

            // ---- terassi ----
            Stroller("Violetti", Walk("violetti_idle"), Walk("violetti_kavely"), Walk("violetti_puhe"), 11300f, 0.5f, 10800f, 11900f, 1f, 0.045f, 0.033f);
            Stroller("Lippis", Walk("lippis_idle"), Walk("lippis_kavely"), null, 10300f, 0.45f, 10000f, 10700f);
            Stroller("Betty", Walk("betty_idle"), Walk("betty_kavely"), null, 12100f, 0.55f, 11800f, 12400f, 1.065f, 0.15f);
            Stroller("Prätkäjätkä", Walk("motoristi_idle"), Walk("motoristi_kavely"), null, 13300f, 0.4f, 13000f, 13700f, 1f, 0.12f);

            // ---- toinen sankari (vain yksinpelissä) ----
            var rocco = Walk("idle"); var ruby = Walk("sankaritar_idle");
            if (rocco != null && ruby != null)
            {
                var go = new GameObject("Toinen sankari (baari)"); go.transform.SetParent(root.transform, false); float hy = Y(0.65f); go.transform.position = new Vector3(PX(12800f), hy, 0f);
                var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false); vis.sprite = rocco[0]; vis.sortingOrder = Mathf.RoundToInt(-hy * 100f);
                var bh = go.AddComponent<BarHero>(); bh.roccoIdle = rocco; bh.rubyIdle = ruby; bh.body = vis;
            }

            EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc);
            Info("Rantabaarin väki lisätty: " + root.transform.childCount + " hahmoa. Puuttui: " + (missing.Count == 0 ? "ei mitään" : string.Join(", ", missing)) + ".\nAjamatta Unityssä.");
        }
        finally { if (openedHere && sc.IsValid()) EditorSceneManager.CloseScene(sc, true); }
    }

    public static void RunBeachBarFolkBatch()
    {
        batch = true;
        try { Step("423 loppuranta", AddFortressBeach); Step("424 rantabaari", AddFortressBeachBar); Step("425 väki", AddFortressBeachBarFolk); Step("420 aloitus", StartAtFortressRoof); }
        finally { batch = false; }
    }
}
