using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Lokin nurkkapurkaus (valikko 422): kyynärpääisku + lennättävä tuplahyppypotku (lokki_kyynarpaa, lokki_tuplapotku: Tools/LokkiAssets/make_lokki_burst.py).
/// Asettaa burstMoves kaikille Lokin aivoille Aavikon ja Linnakkeen sceneissä (malli, katto, ohjaamo). Ajo uudelleen turvallinen.
/// </summary>
public static partial class BeatEmUpSetup
{
    [MenuItem("Beat em up/422. Lokki: nurkkapurkaus (kyynärpää + lennättävä tuplahyppypotku)", false, 422)]
    static void AddLokkiBurst()
    {
        if (PlayModeBlocked()) return;
        var el = LoadWalkerSprites("lokki_kyynarpaa"); var kk = LoadWalkerSprites("lokki_tuplapotku");
        if (el == null || el.Length == 0 || kk == null || kk.Length == 0) { Info("Puuttuu lokki_kyynarpaa / lokki_tuplapotku (aja Tools/LokkiAssets/make_lokki_burst.py)."); return; }
        const float F = 1f / 22f;
        var opened = new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();
        int n = 0;
        foreach (var path in new[] { "Assets/Scenes/Areas/Aavikko.unity", "Assets/Scenes/Areas/Linnake.unity" })
        {
            if (!File.Exists(path)) continue;
            var sc = EditorSceneManager.GetSceneByPath(path);
            if (!sc.IsValid() || !sc.isLoaded) { sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); opened.Add(sc); }
            int here = 0;
            foreach (var lb in Object.FindObjectsByType<LokkiBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b != null && b.gameObject.scene == sc))
            {
                lb.burstMoves = new[]
                {
                    // kyynärpää: ryntäys + isku kuvissa 4-10 (ei torjuttavissa; purkaus lennättää heron)
                    new MongolBrain.Move { name = "kyynärpää", sprites = el, frameTime = F, hitFrom = 4, hitTo = 10, reach = 2.5f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 10, knockdown = true, moveFrom = 1, moveTo = 11, moveSpeed = 5f, impactHold = 0f },
                    // lennättävä tuplahyppypotku: hyppy kuvissa 3-14, vaakapotku kuvissa 15-29 (liikkuu eteen), lasku ja kyykky sen jälkeen
                    new MongolBrain.Move { name = "tuplahyppypotku", sprites = kk, frameTime = F, hitFrom = 16, hitTo = 29, reach = 3.5f, minRange = 0f, maxRange = 99f, weight = 0f, cooldown = 0f, damage = 12, knockdown = true, moveFrom = 6, moveTo = 29, moveSpeed = 9.5f, impactHold = 0f },
                };
                // heitto: kuvajono kiihtyy (veto hidas -> heitto nopea; osumakuvaksi heittokohta) ja hero lentää kauemmas
                foreach (var cm in new[] { lb.catchHand, lb.catchLeg })
                    if (cm != null && cm.sprites != null && cm.sprites.Length > 0)
                    {
                        bool leg = cm == lb.catchLeg;
                        cm.hitFrom = leg ? lb.legThrowPos : lb.handThrowPos; cm.hitTo = 99; cm.reach = -1f; cm.damage = 0; cm.frameTime = 0.11f;
                    }
                lb.throwSpeed = 16f; lb.throwUp = 8.5f;
                EditorUtility.SetDirty(lb); here++; n++;
            }
            if (here > 0) { EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc); }
        }
        foreach (var s in opened) if (s.IsValid()) EditorSceneManager.CloseScene(s, true);
        Info("Lokin nurkkapurkaus asetettu " + n + " aivolle (kyynärpää " + el.Length + " kuvaa, tuplahyppypotku " + kk.Length + " kuvaa). Purkaus tulee, kun Lokki ajetaan nurkkaan ja siihen kertyy osumia.");
    }

    public static void RunLokkiBurstBatch()
    {
        batch = true;
        try { AddLokkiBurst(); }
        finally { batch = false; }
    }
}
