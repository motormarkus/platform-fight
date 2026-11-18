using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valikko 415: Vegasin showroomin esitys. Hero astuu showroomiin -> valot pimenevät, taikuri esittää molemmat temput lavalla ja sanoo repliikin, sitten taistelu taikuria vastaan,
/// ja vasta voiton jälkeen Violetti kertoo häistä (päätehtävä). Yleisö: Violetti, Elaine, Shanice, Pablo, Maria (ei vihuja). Kasinon pakotettu Violetti-kohtaaminen poistetaan.
/// Elaine/Shanice kopioidaan Aavikkobaarista, Maria ja Pablo UccoLaivasta, Violetti Muurin/Linnakkeen juhlahallista. Vaatii 376, 406 ja 409. Ajo uudelleen turvallinen.</summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/415. Las Vegas: showroomin esitys (pimennys, taikurin temput, taistelu, Violetti kertoo häistä) ja yleisö", false, 415)]
    static void AddShowroomShow()
    {
        if (PlayModeBlocked()) return;
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        var srcOpened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        try
        {
            var show = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.gameObject.scene == scene && a.areaName == "Vegasin showbaari");
            if (show == null) { Info("Alue \"Vegasin showbaari\" puuttuu: aja valikko 406."); return; }
            var mag = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e != null && e.gameObject.scene == scene && e.name == "Taikuri (Vegasin showbaari)");
            if (mag == null) { Info("Showbaarin taikuri puuttuu: aja valikko 409."); return; }

            string[] srcPaths = { DesertScenePath, UccoLaivaScenePath, WallScenePath, WallPartPath(WallFortScene) };
            foreach (var sp0 in srcPaths.Where(File.Exists).Distinct())
            {
                var sc0 = EditorSceneManager.GetSceneByPath(sp0);
                if (!sc0.IsValid() || !sc0.isLoaded) { sc0 = EditorSceneManager.OpenScene(sp0, OpenSceneMode.Additive); srcOpened.Add(sc0); }
            }
            bool Ours(GameObject g) => g.scene == scene;

            // siivous: vanhat yleisöt, esitys ja kasinon pakotettu Violetti
            foreach (var n in new[] { "Showroomin yleisö", "Showroom-esitys", "Vegasin Violetti (kasino)", "Vegasin Violetti (showroom)" })
                foreach (var o in scene.GetRootGameObjects().Where(g => g.name == n).ToArray()) Undo.DestroyObjectImmediate(o);

            float H = 2f * CamHalf, imgW = 5120f / (1844f / H);
            float X(float f) => VegasShowbarX0 + imgW * f;
            float Y(float d) => Mathf.Lerp(show.maxDepthY, show.minDepthY, d);
            var crowd = new GameObject("Showroomin yleisö"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(crowd, scene); Undo.RegisterCreatedObjectUndo(crowd, "Showroomin yleisö");
            string info = "";

            GameObject Place(GameObject src, string name, float xf, float depth)
            {
                var c = Object.Instantiate(src);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(c, scene);
                c.transform.SetParent(crowd.transform, true); c.name = name; c.SetActive(true);
                float y = Y(depth);
                c.transform.position = new Vector3(X(xf), y, 0f);
                int order = Mathf.RoundToInt(-y * 100f);
                foreach (var r in c.GetComponentsInChildren<SpriteRenderer>(true)) r.sortingOrder = order - (r.name.ToLowerInvariant().Contains("shadow") ? 1 : 0);
                return c;
            }
            GameObject FindSrc<T>(string n) where T : Component =>
                Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t != null && t.gameObject.scene != scene && t.name == n).Select(t => t.gameObject).FirstOrDefault();

            // Elaine ja Shanice (BarElaine -> seisova Dancer-silmukka)
            foreach (var (n, xf, d) in new[] { ("Elaine", 1.38f, 0.74f), ("Shanice", 1.62f, 0.82f) })
            {
                var src = FindSrc<BarElaine>(n);
                if (src == null) { info += n + " puuttuu (Aavikkobaari: valikot 112/114). "; continue; }
                var c = Place(src, n + " (yleisö)", xf, d);
                var be = c.GetComponent<BarElaine>();
                var body = be.body; var idle = be.idle; float ft = be.idleFrameTime;
                Object.DestroyImmediate(be);
                var dn = body.gameObject.AddComponent<Dancer>(); dn.sprites = idle; dn.frameTime = ft; dn.keepOrder = true; dn.startFrame = Random.Range(0, 8);
                info += n + " ";
            }
            // Maria (Dancer) ja Pablo (SpriteLoop)
            {
                var src = FindSrc<Dancer>("Salsafestin Maria");
                if (src == null) info += "Maria puuttuu (UccoLaiva: valikko 233). ";
                else { Place(src, "Maria (yleisö)", 1.98f, 0.76f); info += "Maria "; }
                var ps = FindSrc<SpriteLoop>("Pablo (myyja)");
                if (ps == null) info += "Pablo puuttuu (UccoLaiva). ";
                else
                {
                    var c = Place(ps, "Pablo (yleisö)", 1.80f, 0.70f);
                    foreach (var rp in c.GetComponentsInChildren<RubyPabloDance>(true)) Object.DestroyImmediate(rp);
                    var sl = c.GetComponent<SpriteLoop>(); if (sl != null) { sl.ambience = null; sl.finaleRenderer = null; sl.finale = null; }
                    info += "Pablo ";
                }
            }
            // Violetti: yleisössä, puhuu vasta taikurin voiton jälkeen
            VioletQuest vq = null;
            {
                var tmpl = Object.FindObjectsByType<NpcStroller>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(w => w != null && w.gameObject.scene != scene && w.name == "Violetti");
                if (tmpl == null) info += "Violettia ei löytynyt Muurin/Linnakkeen scenestä (valikko 336). ";
                else
                {
                    var qgo = Place(tmpl.gameObject, "Vegasin Violetti (showroom)", 1.18f, 0.78f);
                    qgo.transform.localScale = qgo.transform.localScale * 1.1f;
                    var st = qgo.GetComponent<NpcStroller>();
                    var idleS = st.idleSprites; var talkS = st.talkSprites;
                    var bodyR = st.body != null ? st.body : qgo.GetComponentInChildren<SpriteRenderer>(); var shadowR = st.shadow;
                    Object.DestroyImmediate(st);
                    vq = qgo.AddComponent<VioletQuest>();
                    vq.body = bodyR; vq.shadow = shadowR; vq.idleSprites = idleS; vq.talkSprites = talkS; vq.area = show;
                    vq.startVisible = true; vq.locked = true; vq.triggerX = 0f;
                    EditorUtility.SetDirty(vq);
                    info += "Violetti ";
                }
            }
            // esityksen ohjaus
            var ctl = new GameObject("Showroom-esitys"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ctl, scene); Undo.RegisterCreatedObjectUndo(ctl, "Showroom-esitys");
            var ss = ctl.AddComponent<ShowroomShow>(); ss.show = show; ss.magician = mag; ss.violet = vq; ss.useSubtitle = true;
            EditorUtility.SetDirty(ss);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info("Showroomin esitys valmis. Yleisö: " + info + ". Kasinon pakotettu Violetti poistettu: häistä kertoo nyt vain showroomin Violetti taikurin voiton jälkeen. Taikurin repliikin ääni: Resources/Sfx/Taikuri/repliikki (valinnainen), teksti ShowroomShow.line.");
        }
        finally
        {
            foreach (var so in srcOpened) if (so.IsValid()) EditorSceneManager.CloseScene(so, true);
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
