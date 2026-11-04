using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Violetti (NPC, natsilinnakkeen juhlahalli): kuvat Assets/Sprites/Viholliset/violetti_*.png (Tools/ViolettiAssets/make_violetti_sheets.py).
/// Valikko 336 lisää hänet juhlahalliin: seisoo, kävelee, puhuu, tanssii ja kävelee välillä tiskille nojaamaan (kääntyy selin ja takaisin; NpcStroller). Ei taistelija eikä lyötävissä.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/336. Violetti: juhlahalliin (kävelee, puhuu, tanssii, nojaa tiskiin)", false, 336)]
    static void AddViolettiHall()
    {
        if (PlayModeBlocked()) return;
        var idle = LoadWalkerSprites("violetti_idle"); var walk = LoadWalkerSprites("violetti_kavely"); var talk = LoadWalkerSprites("violetti_puhe");
        var dance = LoadWalkerSprites("violetti_tanssi"); var turn = LoadWalkerSprites("violetti_kaanto"); var back = LoadWalkerSprites("violetti_selin");
        bool ok = new[] { idle, walk, talk, dance, turn, back }.All(a => a != null && a.Length > 0);
        if (!ok) { Info("Puuttuu violetti_idle/kavely/puhe/tanssi/kaanto/selin.png (Assets/Sprites/Viholliset). Aja Tools/ViolettiAssets/make_violetti_sheets.py."); return; }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin juhlahalli" && a.gameObject.scene == scene);
            if (hall == null) { Info("Juhlahallia ei löytynyt: aja valikko 330 (tai 327)."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Violetti").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Violetti");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Violetti");
            float ppu = 941f / (2f * CamHalf);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            vis.sprite = idle[0];
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.3f); sh.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            var st = go.AddComponent<NpcStroller>();
            st.area = hall; st.body = vis; st.shadow = sh; st.idleSprites = idle; st.walkSprites = walk; st.talkSprites = talk; st.danceSprites = dance;
            st.idleFrameTime = 0.045f; st.walkFrameTime = 0.033f; st.talkFrameTime = 0.083f; st.danceFrameTime = 0.21f;   // kuvaväli = näytteenottoväli videossa (kävely/idle 30 kuvaa/s, puhe/tanssi 24)
            st.talkChance = 0.25f; st.danceChance = 0.4f; st.speed = 1.6f;
            st.leanTurnSprites = turn; st.leanIdleSprites = back; st.leanTurnFrameTime = 0.067f; st.leanIdleFrameTime = 0.05f; st.leanChance = 0.5f; st.leanSeconds = new Vector2(6f, 12f);
            st.laneMinX = WallHallX0 + 900f / HallPpu; st.laneMaxX = WallHallX0 + 5000f / HallPpu;   // vain juhlasalin puolella (käytävä jää vartijoille)
            st.leanMinX = WallHallX0 + 1000f / HallPpu; st.leanMaxX = WallHallX0 + 1900f / HallPpu;   // tiski: kuvien 1 ja 2 yli (px ~760–2090)
            float lx = (st.leanMinX + st.leanMaxX) * 0.5f;
            st.leanY = Mathf.Lerp(hall.MaxDepthAtX(lx, hall.maxDepthY), hall.MinDepthAtX(lx, hall.minDepthY), 0.06f);
            float x = WallHallX0 + 3200f / HallPpu, yMax = hall.MaxDepthAtX(x, hall.maxDepthY), yMin = hall.MinDepthAtX(x, hall.minDepthY);
            go.transform.position = new Vector3(x, Mathf.Lerp(yMax, yMin, 0.5f), 0f);
            EditorUtility.SetDirty(st);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Violetti lisätty juhlahalliin. Hän seisoo, kävelee, puhuu, tanssii ja kävelee tiskille: kääntyy selin, nojaa 6–12 s ja kääntyy takaisin; ei taistele eikä ole lyötävissä.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
