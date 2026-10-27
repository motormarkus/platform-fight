using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PokerSetup
{
    const string SettingsPath = "Assets/Resources/Poker/DefaultPoker.asset";
    const string ScenePath = "Assets/Scenes/PokerTest.unity";
    static bool Blocked()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode) return false;
        EditorUtility.DisplayDialog("Texas Hold'em", "Paina ensin Stop.", "OK"); return true;
    }

    public static void RepairArtwork()
    {
        Texture2D LoadArtwork(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.IO.FileNotFoundException("Pokerin kuva puuttuu", path);
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) throw new System.InvalidOperationException("Pokerin kuva ei lataudu: " + path);
            return texture;
        }
        var table = LoadArtwork("Assets/Resources/Poker/Artwork/TableOpponents.jpg");
        var panels = LoadArtwork("Assets/Resources/Poker/Artwork/Panels.jpg");
        foreach (var guid in AssetDatabase.FindAssets("t:PokerPresentationSettings", new[] { "Assets" }))
        {
            var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (settings == null) continue;
            if (settings.tableAndOpponents == null) settings.tableAndOpponents = table;
            if (settings.panelAtlas == null) settings.panelAtlas = panels;
            EditorUtility.SetDirty(settings);
        }
    }

    [MenuItem("Beat em up/191. Holdem: valmistele testiscene ja asetukset", false, 191)]
    public static void Prepare()
    {
        if (Blocked()) return;
        Directory.CreateDirectory("Assets/Resources/Poker"); AssetDatabase.Refresh();
        var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(SettingsPath);
        if (settings == null) { settings = ScriptableObject.CreateInstance<PokerPresentationSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
        Texture2D Texture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 2048))
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        if (settings.tableAndOpponents == null) settings.tableAndOpponents = Texture("Assets/ArtSources/Poker/table-opponents-source.jpg");
        if (settings.panelAtlas == null) settings.panelAtlas = Texture("Assets/ArtSources/Poker/ui-panels-source.jpg");
        if (settings.originalLogo == null) settings.originalLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Valikko/logo.png");
        if (settings.barBackground == null) settings.barBackground = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Taustat/baari_pokeri.png");
        if (settings.headingFont == null) settings.headingFont = Resources.Load<Font>("Valikko/LuckiestGuy");
        if (settings.textFont == null) settings.textFont = Resources.Load<Font>("Valikko/Bangers");
        if (settings.whiteKeyMaterial == null)
        {
            const string materialPath = "Assets/Resources/Poker/WhiteKey.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null) { mat = new Material(Shader.Find("Total Fist/Poker White Key UI")); AssetDatabase.CreateAsset(mat, materialPath); }
            settings.whiteKeyMaterial = mat;
        }
        RepairArtwork();
        EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        if (!File.Exists(ScenePath))
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var root = new GameObject("Total Fist · Poker Test"); SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<PokerTestBootstrap>().settings = settings;
            var camera = new GameObject("Camera", typeof(Camera), typeof(AudioListener)); SceneManager.MoveGameObjectToScene(camera, scene);
            camera.transform.position = new Vector3(0, 0, -10); camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = Color.black;
            EditorSceneManager.SaveScene(scene, ScenePath); EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous);
        }
        Selection.activeObject = settings;
        EditorUtility.DisplayDialog("Texas Hold'em", "Avaa Assets/Scenes/PokerTest.unity ja käynnistä Play itse. Testissä on 10 000 mk erillistä testirahaa.\n\nAsetukset: " + SettingsPath + "\n\nLiittyminen omaan erilliseen pöytään: valitse pöytä ja aja valikko 192. Kentän scene ei muuttunut.", "OK");
    }

    [MenuItem("Beat em up/192. Holdem: liitä liittyminen valittuun omaan pöytään", false, 192)]
    static void InstallTable()
    {
        if (Blocked()) return;
        var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(SettingsPath);
        var table = Selection.activeGameObject;
        if (settings == null) { EditorUtility.DisplayDialog("Texas Hold'em", "Valmistele asetukset ensin valikolla 191.", "OK"); return; }
        if (table == null || !table.scene.IsValid() || EditorUtility.IsPersistent(table))
        {
            EditorUtility.DisplayDialog("Texas Hold'em", "Valitse Hierarchysta oma erillinen pelipöytä, johon haluat liittymistoiminnon. Vanhaan pokerihuoneeseen ei lisätä mitään automaattisesti.", "OK"); return;
        }
        var entry = table.GetComponent<PokerTableEntry>();
        if (entry == null) entry = Undo.AddComponent<PokerTableEntry>(table);
        Undo.RecordObject(entry, "Oman pokeripöydän liittyminen");
        if (entry.settings == null) entry.settings = settings;
        if (entry.here == null) entry.here = table.GetComponentInParent<Area>();
        EditorUtility.SetDirty(entry); EditorSceneManager.MarkSceneDirty(table.scene);
        EditorUtility.DisplayDialog("Texas Hold'em", "Liittymistoiminto lisätty valittuun omaan pöytään. Aseta Inspectorissa Here-kenttään pöydän alue sekä sopivat Range X/Y -etäisyydet. E / toimintanappi avaa pokerin pöydän lähellä.\n\nTallenna Ctrl+S.", "OK");
    }
}
