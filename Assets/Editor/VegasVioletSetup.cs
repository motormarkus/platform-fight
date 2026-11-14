using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Violetti Vegasin baariin (valikko 386): NpcWalker-kopio Violettista (Muurin juhlahalli): seisoo, kävelee, puhuu, tanssii ja nojaa tiskiin baarin lattialla. Ei taistelija eikä lyötävissä. Aja valikko 379 ensin.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/386. Las Vegas: Violetti baariin (kävelee, tanssii, nojaa tiskiin)", false, 386)]
    static void AddVegasViolet()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var srcOpened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        try
        {
            var bar = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin baari");
            if (bar == null) { Info("Alue \"Vegasin baari\" puuttuu: aja valikko 379."); return; }
            // Violetti on linnakkeen juhlahallissa: Muuri.unity tai (Muurin jaon 349 jälkeen) Linnake.unity
            var srcPaths = new[] { WallScenePath, WallPartPath(WallFortScene) }.Where(File.Exists).Distinct().ToArray();
            if (srcPaths.Length == 0) { Info("Muuri-scene puuttuu: aja valikko 327."); return; }
            foreach (var sp0 in srcPaths)
            {
                var sc0 = EditorSceneManager.GetSceneByPath(sp0);
                if (!sc0.IsValid() || !sc0.isLoaded) { sc0 = EditorSceneManager.OpenScene(sp0, OpenSceneMode.Additive); srcOpened.Add(sc0); }
            }
            var tmpl = Object.FindObjectsByType<NpcStroller>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w != null && w.gameObject.scene != scene && w.name == "Violetti");
            if (tmpl == null) { Info("Violettia ei löytynyt Muurin scenestä: aja valikko 336."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Violetti").ToArray()) Undo.DestroyObjectImmediate(old);
            var go = Object.Instantiate(tmpl.gameObject);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            go.name = "Vegasin Violetti"; go.SetActive(true);
            go.transform.localScale = go.transform.localScale * 1.1f;   // Violetti oli liian pieni: +10 %
            var w2 = go.GetComponent<NpcStroller>();
            float H = 2f * CamHalf, imgW = 5120f / (1844f / H);
            w2.area = bar; w2.laneMinX = VegasBarX0 + imgW * 0.15f; w2.laneMaxX = VegasBarX0 + imgW * 2.4f;
            w2.leanMinX = VegasBarX0 + imgW * 0.80f; w2.leanMaxX = VegasBarX0 + imgW * 1.40f;   // nojaa tiskiin
            float lx = (w2.leanMinX + w2.leanMaxX) * 0.5f; w2.leanY = Mathf.Lerp(bar.MaxDepthAtX(lx, bar.maxDepthY), bar.MinDepthAtX(lx, bar.minDepthY), 0.06f);
            EditorUtility.SetDirty(w2);
            go.transform.position = new Vector3(VegasBarX0 + imgW * 0.55f, Mathf.Lerp(bar.maxDepthY, bar.minDepthY, 0.6f), 0f);
            Undo.RegisterCreatedObjectUndo(go, "Vegasin Violetti");

            // Violetti kasinolla: päätehtävän antaja (VioletQuest): pyytää apua häiden estämiseen, ainoa vastaus "Hoidan homman."
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Vegasin Violetti (kasino)").ToArray()) Undo.DestroyObjectImmediate(old);
            string casinoInfo = "";
            var casino = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin kasino");
            if (casino == null) casinoInfo = " Kasinon Violettia ei lisätty: alue \"Vegasin kasino\" puuttuu (aja 376).";
            else
            {
                var qgo = Object.Instantiate(tmpl.gameObject);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(qgo, scene);
                qgo.name = "Vegasin Violetti (kasino)"; qgo.SetActive(true);
                qgo.transform.localScale = qgo.transform.localScale * 1.1f;
                var st = qgo.GetComponent<NpcStroller>();
                var idleS = st.idleSprites; var talkS = st.talkSprites;
                var bodyR = st.body != null ? st.body : qgo.GetComponentInChildren<SpriteRenderer>(); var shadowR = st.shadow;
                Object.DestroyImmediate(st);   // ei kävele: seisoo ja puhuu
                var q = qgo.AddComponent<VioletQuest>();
                q.body = bodyR; q.shadow = shadowR; q.idleSprites = idleS; q.talkSprites = talkS; q.area = casino;
                // pakotettu kohtaaminen: hero saapuu katolta hissillä kasinon oikeaan päähän (hissi ~ walkMaxX - 0,055 kuvaa, tulopaikka 2,6 yks. vasemmalla) ja kulkee vasemmalle;
                // 9 yksikköä tulopaikasta vasemmalle hän pysähtyy ja Violetti ilmestyy eteen. Katukäynti vasemmasta päästä pysähtyy samaan rajaan.
                float elevSpawnX = casino.walkMaxX - imgW * 0.055f - 2.6f;
                q.triggerX = elevSpawnX - 9f;
                float qx = q.triggerX - 2.4f;
                float qyMax = casino.MaxDepthAtX(qx, casino.maxDepthY), qyMin = casino.MinDepthAtX(qx, casino.minDepthY);
                qgo.transform.position = new Vector3(qx, Mathf.Lerp(qyMax, qyMin, 0.45f), 0f);
                EditorUtility.SetDirty(q);
                Undo.RegisterCreatedObjectUndo(qgo, "Vegasin Violetti (kasino)");
                casinoInfo = " Kasinolla Violetti ilmestyy heron eteen (raja x " + q.triggerX.ToString("0.0") + ", ei ohitettavissa) ja antaa päätehtävän: häiden estäminen (vastaus \"Hoidan homman.\").";
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Violetti lisätty Vegasin baariin: seisoo, kävelee, puhuu, tanssii ja nojaa tiskiin; ei taistele." + casinoInfo);
        }
        finally
        {
            foreach (var so in srcOpened) if (so.IsValid()) EditorSceneManager.CloseScene(so, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
