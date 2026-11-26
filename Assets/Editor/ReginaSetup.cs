using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Regina (valikko 433): Vegasin motellin ensimmäinen jengiläinen, kaksi kappaletta motellin edustalle kadulle.
/// Kuvat Assets/Sprites/Viholliset/regina_*.png (Tools/ReginaAssets/make_regina_sheets.py: 3 videota + osuma- ja suojauskuvasarjat).
/// Iskut: J jabi, S takasuora, K askelsuora (kaataa); alt: 0 etupotku, 1 sivupotku (kaataa), 2 syöksy (rynnistää eteenpäin, kaataa).
/// Osuma ja suojaus kuvasarjoista, kaatuminen/heitetty/nousu videosta (nousun selin oleva osuus peilattu). Aja ensin 403 (motelli).</summary>
public static partial class BeatEmUpSetup
{
    const float ReginaScale = 1.15f;


    internal static readonly string[] ReginaSheetNames = { "idle", "kavely", "juoksu", "jabi", "takasuora", "askelsuora", "etupotku", "sivupotku", "syoksy", "osuma", "suojaus", "kaatuminen", "nousu" };

    /// Reginan kuvat (null + puuttuvan nimi, jos jokin puuttuu).
    internal static Dictionary<string, Sprite[]> LoadReginaSheets(out string missing)
    {
        missing = null;
        var d = new Dictionary<string, Sprite[]>();
        foreach (var n in ReginaSheetNames)
        {
            var sp = LoadWalkerSprites("regina_" + n);
            if (sp == null || sp.Length == 0) { missing = "regina_" + n + ".png"; return null; }
            d[n] = sp;
        }
        return d;
    }

    internal static Enemy BuildRegina(Dictionary<string, Sprite[]> s, Transform parent, string name, Vector3 pos)
    {
        const float F = 1f / 24f, F2 = 2f / 24f * 0.75f;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var visual = new GameObject("Visual").AddComponent<SpriteRenderer>(); visual.transform.SetParent(go.transform, false);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>(); shadow.transform.SetParent(go.transform, false);
        var e = go.AddComponent<Enemy>();
        e.body = visual; e.shadow = shadow; visual.sprite = s["idle"][0];
        e.displayName = "Regina";
        e.visualScale = ReginaScale; e.attackArtScale = 1f; e.knockArtScale = 1f; e.walkArtScale = 1f;
        e.idleSprites = s["idle"]; e.idleFrameTime = F;
        e.walkSprites = s["kavely"]; e.walkFrameTime = F;
        e.runSprites = s["juoksu"]; e.runFrameTime = F; e.runSpeedMultiplier = 1.9f; e.runDistance = 6f;
        e.maxHealth = 160; e.moveSpeedX = 3.2f; e.moveSpeedY = 2.4f; e.depthTolerance = 0.4f; e.attackCooldown = 0.6f; e.wakeDistance = 12f;
        e.comboChance = 0.6f; e.comboWindupScale = 0.6f; e.comboRecoverFraction = 0.6f; e.attackRange = 2.3f;
        // J = jabi (osuma kuvassa 5), S = takasuora (osuma 3), K = askelsuora (osuma 13, kaataa)
        e.punchSprites = s["jabi"]; e.punchImpactFrame = 4; e.windupTime = 4 * F; e.punchRecoverTime = (s["jabi"].Length - 4) * F; e.punchActiveTime = 0.12f; e.punchDamage = 9;
        e.punch2Sprites = s["takasuora"]; e.punch2ImpactFrame = 2; e.punch2Damage = 12; e.punch2Knockdown = false; e.punch2Chance = 0.4f;
        e.punch3Sprites = s["askelsuora"]; e.punch3ImpactFrame = 12; e.punch3Damage = 16; e.punch3Knockdown = true; e.punch3Chance = 0.2f; e.punch3IsPunch = true;
        e.punch3WindupTime = 12 * F; e.punch3RecoverTime = (s["askelsuora"].Length - 12) * F; e.punch3Reach = 2.8f;
        // alt: 0 etupotku (osuma 9), 1 sivupotku (osuma 6, kaataa), 2 syöksy (osuma 9, rynnistää eteenpäin, kaataa)
        e.altAttackSprites = s["etupotku"]; e.altImpactFrame = 8; e.altAnimationFrameTime = F2; e.altDamage = 13; e.altReach = 3.2f; e.altChance = 0.35f;
        e.altVariants = new[]
        {
            new Enemy.AltVariant { name = "Etupotku", sprites = s["etupotku"], impactFrame = 8, frameTime = F2, damage = 13, reach = 3.3f, knockdown = false, weight = 1f, lungeSpeed = 0f },
            new Enemy.AltVariant { name = "Sivupotku", sprites = s["sivupotku"], impactFrame = 5, frameTime = F2, damage = 17, reach = 3.5f, knockdown = true, knockSpeed = 7f, knockUp = 3.5f, weight = 0.9f, lungeSpeed = 0f },
            new Enemy.AltVariant { name = "Syöksy", sprites = s["syoksy"], impactFrame = 8, frameTime = F2, damage = 15, reach = 3.0f, knockdown = true, knockSpeed = 8f, knockUp = 3f, weight = 0.5f, lungeSpeed = 9f, lungeTime = 0.3f },
        };
        // kombot: J jabi, S takasuora, K askelsuora, 0 etupotku, 1 sivupotku, 2 syöksy
        e.combos = new[] { "JS", "JSK", "J0", "JS1", "S0", "2", "J1" };
        e.hurtSprites = s["osuma"]; e.hurtTime = 0.3f;
        // kaatuminen 23 kuvaa: 0-6 horjahdus taaksepäin, 7-16 ilmassa (jalat ylhäällä), 17-22 alastulo ja makuu
        e.knockdownSprites = s["kaatuminen"]; e.airFrameTime = 0.07f; e.airLoopFrames = new[] { 9, 10, 11, 12, 13, 14, 13, 12, 11, 10 };
        e.thrownStartFrames = new[] { 0, 1, 2, 3, 4, 5, 6 }; e.thrownLoopFrames = new[] { 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }; e.thrownLandFrames = Enumerable.Range(17, s["kaatuminen"].Length - 17).ToArray(); e.thrownFrameTime = 0.06f;
        e.getUpSprites = s["nousu"]; e.getUpTime = 1.6f;
        e.blockSprites = s["suojaus"]; e.blockChance = 0.25f;
        // omat äänet (Assets/Audio/regina, tasattu Tools/AudioLevels/level_regina.py): huudot, potkut, kipu, kaatuminen, nousu, häviö, puheet
        const string A = "Assets/Audio/regina";
        AudioClip R(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>(A + "/" + n + ".wav");
        e.attackSounds = LoadClips(A, "regina_hit"); e.attackVolume = 1f; e.attackSoundChance = 0.6f;
        e.altSwingSounds = LoadClips(A, "regina_kick"); e.altSwingVolume = 1f;
        e.hurtSounds = LoadClips(A, "regina_pain"); e.hurtVolume = 1f;
        e.fallVoice = R("regina_fall1"); e.fallVoiceAlt = R("regina_fall2"); e.fallVoiceVolume = 1f;
        e.getUpVoices = LoadClips(A, "regina_nousu"); e.getUpVoiceChance = 0.7f; e.getUpVoiceGap = 3f;
        e.deathVoice = R("regina_loses"); e.deathVoiceVolume = 1f;
        e.calmTauntSounds = new[] { R("regina_welcome_speak") }.Where(c => c != null).ToArray();
        e.tauntSounds = new[] { R("regina_speak1"), R("regina_speak2"), R("regina_speak3") }.Where(c => c != null).ToArray();   // idlessä ja tappelussa
        e.fightTauntSounds = new[] { R("regina_taistelu_speak") }.Where(c => c != null).ToArray();   // vain tappelussa
        e.talksWhenCalm = true; e.tauntOnWakeChance = 0.5f; e.tauntPause = new Vector2(6f, 12f); e.oneSpeakerPerKind = true;
        if (e.hurtSounds.Length == 0) { e.hurtSounds = LoadClips("Assets/Audio/big thug", "gasp"); e.hurtVolume = 0.95f; }   // varalla, jos äänet puuttuvat
        go.transform.position = pos;
        EditorUtility.SetDirty(e);
        return e;
    }

    [MenuItem("Beat em up/433. Las Vegas: Regina, motellin jengiläinen (2 motellin eteen)", false, 433)]
    static void AddVegasRegina()
    {
        if (PlayModeBlocked()) return;
        var s = new Dictionary<string, Sprite[]>();
        foreach (var n in new[] { "idle", "kavely", "juoksu", "jabi", "takasuora", "askelsuora", "etupotku", "sivupotku", "syoksy", "osuma", "suojaus", "kaatuminen", "nousu" })
        {
            var sp = LoadWalkerSprites("regina_" + n);
            if (sp == null || sp.Length == 0) { Info("Puuttuu tai ei leikkaudu: regina_" + n + ".png (Assets/Sprites/Viholliset). Aja Tools/ReginaAssets/make_regina_sheets.py."); return; }
            s[n] = sp;
        }
        if (!File.Exists(VegasScenePath)) { Info("Aja ensin valikko 376 (Vegas)."); return; }
        var scene = EditorSceneManager.GetSceneByPath(VegasScenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(VegasScenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var street = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a != null && a.gameObject.scene == scene && a.areaName == "Vegasin katu");
            var motelRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Vegasin motelli");
            var motel = motelRoot != null ? motelRoot.GetComponent<SiteLevels>() : null;
            if (street == null || motel == null) { Info("\"Vegasin katu\" tai motelli puuttuu: aja valikot 378 ja 403."); return; }
            foreach (var old in scene.GetRootGameObjects().Where(g => g.name == "Reginat").ToArray()) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Reginat");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Reginat");
            const float F = 1f / 24f, F2 = 2f / 24f * 0.75f;   // videot 24 fps; potkut ja syöksy joka 2. kuva -> hieman videota nopeammin
            var gasp = LoadClips("Assets/Audio/big thug", "gasp");
            int count = 0;

            Enemy MakeRegina(Vector3 pos) => BuildRegina(s, root.transform, "Regina " + (++count), pos);

            // motellin edustalle kadulle (katutaso, lähempänä kameraa kuin parvekkeen raja)
            float x0 = motel.zoneMinX, x1 = motel.zoneMaxX;
            foreach (var (fr, depth) in new[] { (0.35f, 0.7f), (0.68f, 0.82f) })
            {
                float x = Mathf.Lerp(x0, x1, fr);
                float yMax = street.MaxDepthAtX(x, street.maxDepthY), yMin = street.MinDepthAtX(x, street.minDepthY);
                float y = Mathf.Clamp(Mathf.Lerp(yMax, yMin, depth), yMin + 0.05f, yMax - 0.05f);
                MakeRegina(FreeSpot(street, scene, new Vector3(x, y, 0f)));
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Info(count + " Reginaa (160 elämää) lisätty Vegasin motellin edustalle (x " + x0.ToString("0") + "–" + x1.ToString("0") + ").\nIskut: jabi, takasuora, askelsuora (kaataa); potkut: etupotku, sivupotku (kaataa); syöksy (kaataa). Kombot JS, JSK, J0, JS1, S0, 2, J1. Suojaus 25 %.\nÄänet väliaikaiset (big thug gasp). Ajamatta Unityssä.");
        }
        finally { if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }
    }
}
