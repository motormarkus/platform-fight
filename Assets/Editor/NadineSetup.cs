using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Nadine (NPC, natsilinnakkeen juhlahalli): kuvat Assets/Sprites/Viholliset/nadine_*.png (Tools/NadineAssets/make_nadine_sheets.py).
/// Valikko 335 lisää hänet juhlahalliin: seisoo, kävelee, elehtii ja tanssii (NpcStroller). Ei taistelija eikä lyötävissä.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/335. Nadine: juhlahalliin (seisoo, kävelee, elehtii, tanssii)", false, 335)]
    static void AddNadine()
    {
        if (PlayModeBlocked()) return;
        var idle = LoadWalkerSprites("nadine_idle"); var walk = LoadWalkerSprites("nadine_kavely");
        var talk = LoadWalkerSprites("nadine_puhe"); var dance = LoadWalkerSprites("nadine_tanssi"); var dance2 = LoadWalkerSprites("nadine_tanssi2");
        if (idle == null || idle.Length == 0 || walk == null || walk.Length == 0 || talk == null || talk.Length == 0 || dance == null || dance.Length == 0 || dance2 == null || dance2.Length == 0)
        { Info("Puuttuu nadine_idle/kavely/puhe/tanssi/tanssi2.png (Assets/Sprites/Viholliset). Aja Tools/NadineAssets/make_nadine_sheets.py."); return; }
        if (!File.Exists(WallScenePath)) { Info("Aja ensin valikko 327 (Kiinan muuri)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(WallScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(WallScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var hall = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Muurin juhlahalli" && a.gameObject.scene == scene);
            if (hall == null) { Info("Juhlahallia ei löytynyt: aja valikko 330 (tai 327)."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Nadine").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = new GameObject("Nadine");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Nadine");
            float ppu = 941f / (2f * CamHalf);
            var vis = new GameObject("Visual").AddComponent<SpriteRenderer>(); vis.transform.SetParent(go.transform, false);
            vis.sprite = idle[0];
            var sh = new GameObject("Shadow").AddComponent<SpriteRenderer>(); sh.transform.SetParent(go.transform, false);
            sh.color = new Color(0f, 0f, 0f, 0.3f); sh.transform.localScale = new Vector3(1.2f, 0.4f, 1f);
            var st = go.AddComponent<NpcStroller>();
            st.area = hall; st.body = vis; st.shadow = sh; st.idleSprites = idle; st.walkSprites = walk; st.talkSprites = talk; st.danceSprites = dance; st.danceSprites2 = dance2;
            st.idleFrameTime = 0.06f; st.walkFrameTime = 0.042f; st.talkFrameTime = 0.125f; st.danceFrameTime = 0.19f; st.danceFrameTime2 = 0.21f;   // kuvaväli = näytteenottoväli videossa (24 kuvaa/s)
            st.talkChance = 0.3f; st.danceChance = 0.6f; st.speed = 1.6f;
            st.laneMinX = WallHallX0 + 900f / HallPpu; st.laneMaxX = WallHallX0 + 5000f / HallPpu;   // vain juhlasalin puolella (käytävä jää vartijoille)
            float x = WallHallX0 + 2600f / HallPpu, yMax = hall.MaxDepthAtX(x, hall.maxDepthY), yMin = hall.MinDepthAtX(x, hall.minDepthY);
            go.transform.position = new Vector3(x, Mathf.Lerp(yMax, yMin, 0.55f), 0f);
            EditorUtility.SetDirty(st);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Nadine lisätty juhlahalliin. Hän seisoo, kävelee hallissa, puhuu (keskusteluvideo, eteen ja takaisin peilattuna) ja tanssii (kaksi tanssia arvotaan); ei taistele eikä ole lyötävissä.\nAjamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
