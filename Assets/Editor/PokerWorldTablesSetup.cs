using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

public static class PokerWorldTablesSetup
{
    const string Folder = "Assets/Resources/Poker";
    const string VideoPath = "Assets/ArtSources/Poker/poker-table-idle.mov";
    const string FramePath = "Assets/ArtSources/Poker/poker-table-idle-first-frame.png";

    [MenuItem("Beat em up/193. Holdem: omat videopöydät neljään paikkaan")]
    static void PlaceTables()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("Pokeripöydät", "Paina ensin Stop.", "OK"); return; }
        PokerSetup.RepairArtwork();
        var defaults = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(Folder + "/DefaultPoker.asset");
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        var importer = AssetImporter.GetAtPath(FramePath) as TextureImporter;
        var shader = Shader.Find("Total Fist/Poker Green Key World");
        if (defaults == null || clip == null || importer == null || shader == null) { EditorUtility.DisplayDialog("Pokeripöydät", "Videon, ensimmäisen ruudun tai pokeriasetusten importointi puuttuu. Odota Unityn importointi loppuun (asetukset tarvittaessa valikolla 191).", "OK"); return; }
        var areas = Object.FindObjectsByType<Area>(FindObjectsSortMode.None);
        var room = areas.FirstOrDefault(a => a.areaName == "Baari");
        var loip = areas.FirstOrDefault(a => a.areaName == "El Loippari");
        var cabin = areas.FirstOrDefault(a => a.areaName == "Laivan hytti");
        var den = areas.FirstOrDefault(a => a.areaName == "Aavikkobaari");
        var roomBg = GameObject.Find("Baarin sisä")?.GetComponent<SpriteRenderer>();
        var loipBg = GameObject.Find("El Loippari")?.GetComponent<SpriteRenderer>();
        var cabinBg = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(s => s.sprite != null && AssetDatabase.GetAssetPath(s.sprite) == "Assets/Sprites/Taustat/laiva_hytti.png");
        var denBg = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).FirstOrDefault(s => s.sprite != null && AssetDatabase.GetAssetPath(s.sprite) == "Assets/Sprites/Taustat/aavikkobaari_sisa.png");
        if (room == null || loip == null || cabin == null || den == null || roomBg == null || loipBg == null || cabinBg == null || denBg == null)
        { EditorUtility.DisplayDialog("Pokeripöydät", "Tarvitaan Samperi'sin pokerihuone, El Loippari, laivan hytti ja Deacon's Den taustoineen. Mitään pöytää ei muutettu.", "OK"); return; }

        // Observed video: full 1280x720 frames, feet/table legs end at row 676.
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 1280f / 6.8f; importer.mipmapEnabled = false; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Bilinear;
        var st = new TextureImporterSettings(); importer.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect; st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = new Vector2(.5f, (720f - 676f) / 720f); importer.SetTextureSettings(st); importer.SaveAndReimport();
        AssetDatabase.ImportAsset(FramePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(FramePath).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            // Some imports expose the texture but no Sprite sub-asset. Make a
            // persistent Sprite explicitly instead of asking the user to wait.
            var frame = AssetDatabase.LoadAssetAtPath<Texture2D>(FramePath);
            if (frame == null) throw new System.InvalidOperationException("Pöydän kuvaa ei voitu ladata: " + FramePath);
            string spritePath = Folder + "/TableFirstFrame.asset";
            var fresh = Sprite.Create(frame, new Rect(0, 0, frame.width, frame.height),
                new Vector2(.5f, 44f / 720f), frame.width / 6.8f, 0, SpriteMeshType.FullRect);
            fresh.name = "Poker table first frame";
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) { AssetDatabase.CreateAsset(fresh, spritePath); sprite = fresh; }
            else { EditorUtility.CopySerialized(fresh, sprite); Object.DestroyImmediate(fresh); EditorUtility.SetDirty(sprite); }
        }
        string materialPath = Folder + "/GreenKeyWorld.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }

        void Put(string id, Area area, SpriteRenderer bg, float x, float y, float width)
        {
            string name = "Oma pokeripöytä · " + id;
            var go = GameObject.Find(name);
            if (go == null) { go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Oma pokeripöytä"); }
            Undo.RecordObject(go.transform, "Pokeripöydän sijainti"); go.transform.position = new Vector3(x, y, 0);
            var entry = EnsureComponent<PokerTableEntry>(go);
            string path = Folder + "/Table-" + id + ".asset";
            var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(path);
            if (settings == null) { settings = Object.Instantiate(defaults); AssetDatabase.CreateAsset(settings, path); }
            // Bar-specific poker backgrounds will be supplied later. Keep the shared placeholder
            // from defaults (or a background already assigned manually to this settings asset).
            EditorUtility.SetDirty(settings);
            Undo.RecordObject(entry, "Pöydän liittyminen"); entry.settings = settings; entry.here = area; entry.rangeX = width * .5f + .7f; entry.rangeY = .9f;
            var visual = go.transform.Find("Visual");
            if (visual == null) { var child = new GameObject("Visual"); Undo.RegisterCreatedObjectUndo(child, "Videopöytä"); child.transform.SetParent(go.transform, false); visual = child.transform; }
            Undo.RecordObject(visual, "Pöydän mittakaava"); visual.localPosition = Vector3.zero; visual.localScale = Vector3.one * (width / 6.8f);
            var body = EnsureComponent<SpriteRenderer>(visual.gameObject);
            Undo.RecordObject(body, "Pöydän kuva"); body.sprite = sprite; body.sharedMaterial = material; body.sortingOrder = Mathf.RoundToInt(-(y + .55f) * 100);
            var video = EnsureComponent<VideoPlayer>(visual.gameObject);
            Undo.RecordObject(video, "Pöydän video"); video.clip = clip; video.playOnAwake = false; video.isLooping = true; video.audioOutputMode = VideoAudioOutputMode.None;
            var loop = EnsureComponent<PokerTableVideo>(visual.gameObject);
            Undo.RecordObject(loop, "Videon alue"); loop.area = area; loop.clip = clip;
            var obstacle = EnsureComponent<Obstacle>(go);
            Undo.RecordObject(obstacle, "Pöydän este"); obstacle.body = body; obstacle.halfWidth = width * .46f; obstacle.depth = 1.1f; obstacle.maxHealth = 0;
            EditorUtility.SetDirty(entry); EditorUtility.SetDirty(body); EditorUtility.SetDirty(video); EditorUtility.SetDirty(loop); EditorUtility.SetDirty(obstacle);
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        Put("Samperis", room, roomBg, roomBg.bounds.max.x - 4.2f, room.minDepthY + 1.1f, 6.8f);
        Put("ElLoippari", loip, loipBg, loipBg.bounds.max.x - 4.2f, loip.minDepthY + 1.1f, 6.8f);
        Put("LaivanHytti", cabin, cabinBg, cabinBg.bounds.center.x, cabin.minDepthY + .8f, 6.3f);
        Put("DeaconsDen", den, denBg, denBg.bounds.min.x + 4.6f, den.minDepthY + 1.1f, 6.8f);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Pokeripöydät", "Neljä omaa liittymispöytää asetettu: Samperi's alas oikealle, El Loipparin terassi oikealle, laivan hytti ja Deacon's Den alas vasemmalle.\n\nVideo toistuu vain pöydän alueella. E / toimintanappi avaa pokerin.\n\nTallenna Ctrl+S ja tarkista sijoitus sekä mittakaava pelissä.", "OK");
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        // Unity's missing-component wrappers compare equal to null, but ?? sees
        // a managed object and would skip adding the actual component.
        var component = go.GetComponent<T>();
        if (component == null) component = Undo.AddComponent<T>(go);
        if (component == null) throw new System.InvalidOperationException("Komponentin luonti epäonnistui: " + go.name + " / " + typeof(T).Name);
        return component;
    }

    // ---------------- Pokeripöytä Thaitownin hotellin aulaan vasemmalle ----------------
    [MenuItem("Beat em up/224. Pokeripöytä: Thaitownin hotellin aulaan vasemmalle")]
    static void PlaceThaitownTable()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("Pokeripöytä", "Paina ensin Stop.", "OK"); return; }
        const string scenePath = "Assets/Scenes/Areas/Thaitown.unity";
        const string id = "ThaitownAula";
        var defaults = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(Folder + "/DefaultPoker.asset");
        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoPath);
        var sprite = AssetDatabase.LoadAllAssetsAtPath(FramePath).OfType<Sprite>().FirstOrDefault() ?? AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/TableFirstFrame.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/GreenKeyWorld.mat");
        if (defaults == null || clip == null || sprite == null || material == null)
        { EditorUtility.DisplayDialog("Pokeripöytä", "Pokeriasetukset, video, pöydän kuva tai materiaali puuttuu. Aja ensin valikko 193 (se luo ne).", "OK"); return; }
        var scene = EditorSceneManager.GetSceneByPath(scenePath);
        bool openedHere = false;
        if (!scene.IsValid() || !scene.isLoaded) { scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive); openedHere = true; }
        try
        {
            var area = Object.FindObjectsByType<Area>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(a => a.areaName == "Thaitown hotelli aula 1" && a.gameObject.scene == scene);
            if (area == null) { EditorUtility.DisplayDialog("Pokeripöytä", "Aluetta 'Thaitown hotelli aula 1' ei löydy Thaitownin scenestä.", "OK"); return; }
            float width = 6.8f;
            float x = area.walkMinX + width * .5f + 2.1f, y = area.minDepthY + 1.1f;   // aulan vasen laita, alhaalla
            string name = "Oma pokeripöytä · " + id;
            var go = Object.FindObjectsByType<PokerTableEntry>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == name && t.gameObject.scene == scene)?.gameObject;
            if (go == null)
            {
                go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Oma pokeripöytä");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            }
            Undo.RecordObject(go.transform, "Pokeripöydän sijainti"); go.transform.position = new Vector3(x, y, 0);
            var entry = EnsureComponent<PokerTableEntry>(go);
            string path = Folder + "/Table-" + id + ".asset";
            var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(path);
            if (settings == null) { settings = Object.Instantiate(defaults); AssetDatabase.CreateAsset(settings, path); }
            EditorUtility.SetDirty(settings);
            Undo.RecordObject(entry, "Pöydän liittyminen"); entry.settings = settings; entry.here = area; entry.rangeX = width * .5f + .7f; entry.rangeY = .9f;
            var visual = go.transform.Find("Visual");
            if (visual == null) { var child = new GameObject("Visual"); Undo.RegisterCreatedObjectUndo(child, "Videopöytä"); child.transform.SetParent(go.transform, false); visual = child.transform; }
            Undo.RecordObject(visual, "Pöydän mittakaava"); visual.localPosition = Vector3.zero; visual.localScale = Vector3.one * (width / 6.8f);
            var body = EnsureComponent<SpriteRenderer>(visual.gameObject);
            Undo.RecordObject(body, "Pöydän kuva"); body.sprite = sprite; body.sharedMaterial = material; body.sortingOrder = Mathf.RoundToInt(-(y + .55f) * 100);
            var video = EnsureComponent<VideoPlayer>(visual.gameObject);
            Undo.RecordObject(video, "Pöydän video"); video.clip = clip; video.playOnAwake = false; video.isLooping = true; video.audioOutputMode = VideoAudioOutputMode.None;
            var loop = EnsureComponent<PokerTableVideo>(visual.gameObject);
            Undo.RecordObject(loop, "Videon alue"); loop.area = area; loop.clip = clip;
            var obstacle = EnsureComponent<Obstacle>(go);
            Undo.RecordObject(obstacle, "Pöydän este"); obstacle.body = body; obstacle.halfWidth = width * .46f; obstacle.depth = 1.1f; obstacle.maxHealth = 0;
            EditorUtility.SetDirty(entry); EditorUtility.SetDirty(body); EditorUtility.SetDirty(video); EditorUtility.SetDirty(loop); EditorUtility.SetDirty(obstacle);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Pokeripöytä", "Pokeripöytä lisätty hotellin aulan vasempaan laitaan (x " + x.ToString("0.0") + ", y " + y.ToString("0.00") + ") ja Thaitownin scene tallennettu.\n\nE / toimintanappi avaa pokerin. Jos paikka ei ole oikea, siirrä oliota 'Oma pokeripöytä · ThaitownAula' Unityssä.", "OK");
        }
        finally { if (openedHere) EditorSceneManager.CloseScene(scene, true); }
    }
}
