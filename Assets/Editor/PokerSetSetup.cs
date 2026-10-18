using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class PokerSetSetup
{
    const string Source = "Assets/ArtSources/Poker/ImportedSet";
    const string Output = "Assets/Resources/Poker/ImportedSet";
    static readonly int[] Values = { 1, 2, 5, 10, 20, 25, 50 };
    static readonly string[] Suits = { "clubs", "diamonds", "hearts", "spades" };

    [MenuItem("Beat em up/194. Holdem: ota PokerSetin kortit ja 3D-merkit käyttöön")]
    static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorUtility.DisplayDialog("PokerSet", "Paina ensin Stop.", "OK"); return; }
        string[] files = Enumerable.Range(0, 52).Select(FacePath)
            .Concat(new[] { Source + "/Textures/Back1.png", Source + "/Models/CardFace.obj", Source + "/Models/CardBack.obj", Source + "/Models/CardEdges.obj", Source + "/Models/PokerSet.mtl" })
            .Concat(Values.Select(n => Source + "/Models/Chip-" + n + ".obj")).ToArray();
        string[] missing = files.Where(f => !File.Exists(f)).ToArray();
        var shader = Shader.Find("Total Fist/Poker Asset Model");
        if (missing.Length > 0 || shader == null)
        {
            EditorUtility.DisplayDialog("PokerSet", "Odotetaan PokerSetin importointia. Puuttuvat:\n" + string.Join("\n", missing) + (shader == null ? "\nPoker Asset Model -shader" : ""), "OK"); return;
        }
        Directory.CreateDirectory(Output + "/Materials"); Directory.CreateDirectory(Output + "/Prefabs"); AssetDatabase.Refresh();
        var faceSprites = new Sprite[52]; var faceMaterials = new Material[52];
        for (int id = 0; id < 52; id++)
        {
            string path = FacePath(id); faceSprites[id] = Sprite(path);
            faceMaterials[id] = Material(Output + "/Materials/Card-" + id + ".mat", shader, AssetDatabase.LoadAssetAtPath<Texture2D>(path), Color.white);
            OrientCardTexture(faceMaterials[id]);
        }
        var backSprite = Sprite(Source + "/Textures/Back1.png");
        var backMaterial = Material(Output + "/Materials/Card-Back.mat", shader, AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "/Textures/Back1.png"), Color.white);
        OrientCardTexture(backMaterial);
        var edgeMaterial = Material(Output + "/Materials/Card-Edges.mat", shader, null, new Color(.84f, .82f, .75f));
        var palette = Palette(shader);
        var preview = EditorSceneManager.NewPreviewScene();
        GameObject cardPrefab;
        var chipPrefabs = new GameObject[Values.Length];
        try
        {
            var root = new GameObject("PokerSet Card"); SceneManager.MoveGameObjectToScene(root, preview);
            var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false);
            var face = Model(Source + "/Models/CardFace.obj", visual, new[] { faceMaterials[0] });
            var back = Model(Source + "/Models/CardBack.obj", visual, new[] { backMaterial });
            Model(Source + "/Models/CardEdges.obj", visual, new[] { edgeMaterial });
            var adapter = root.AddComponent<PokerMeshCard>(); adapter.face = face; adapter.back = back; adapter.visualRoot = visual;
            adapter.faces = faceMaterials; adapter.backMaterial = backMaterial;
            cardPrefab = PrefabUtility.SaveAsPrefabAsset(root, Output + "/Prefabs/Card.prefab");
            for (int n = 0; n < Values.Length; n++)
            {
                string path = Source + "/Models/Chip-" + Values[n] + ".obj";
                var materials = File.ReadLines(path).Where(l => l.StartsWith("usemtl ")).Select(l => l.Substring(7).Trim()).Distinct().Select(name => palette[name]).ToArray();
                var chip = new GameObject("PokerSet Chip " + Values[n]); SceneManager.MoveGameObjectToScene(chip, preview);
                Model(path, chip.transform, materials, palette);
                chipPrefabs[n] = PrefabUtility.SaveAsPrefabAsset(chip, Output + "/Prefabs/Chip-" + Values[n] + ".prefab");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        if (cardPrefab == null || chipPrefabs.Any(p => p == null)) throw new InvalidOperationException("PokerSet prefab creation failed");
        string rtPath = Output + "/PokerAssets.renderTexture";
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (texture == null)
        {
            texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { name = "PokerAssets", antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
            AssetDatabase.CreateAsset(texture, rtPath);
        }
        int configured = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:PokerPresentationSettings", new[] { "Assets" }))
        {
            var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (settings == null) continue;
            Undo.RecordObject(settings, "PokerSetin kortit ja merkit");
            settings.cardFaces = faceSprites; settings.cardBack = backSprite;
            settings.cardPrefab = cardPrefab.GetComponent<PokerMeshCard>(); settings.cardRenderTexture = texture;
            settings.cardTableEuler = new Vector3(30, 0, 0); settings.cardWorldScale = 1;
            settings.chipPrefabs = chipPrefabs; settings.chipValues = (int[])Values.Clone();
            EditorUtility.SetDirty(settings); configured++;
        }
        PokerDealerSetup.Configure();
        PokerSetup.RepairArtwork();
        AssetDatabase.SaveAssets(); Selection.activeObject = cardPrefab;
        EditorUtility.DisplayDialog("PokerSet", "52 korttia, sininen selkä ja 3D-merkit 1/2/5/10/20/25/50 mk otettu käyttöön (" + configured + " pokeriasetusta).\n\nAvaa PokerTest.unity ja testaa itse. Tarkista korttien suunta, mittakaava, perspektiivi ja merkkien värit. Pelisääntöjä, sisäänostoa tai taustoja ei muutettu.\n\nJos luot baaripöydät myöhemmin valikolla 193, ne perivät nämä assetit oletusasetuksista.", "OK");
    }

    static string FacePath(int id)
    {
        int rank = id % 13 + 2;
        string name = rank < 11 ? rank.ToString(CultureInfo.InvariantCulture) : new[] { "jack", "queen", "king", "ace" }[rank - 11];
        return Source + "/Textures/Faces/" + name + "_of_" + Suits[id / 13] + ".png";
    }
    static void OrientCardTexture(Material material)
    {
        // OBJ import reverses the visible horizontal UV direction in this model.
        material.SetTextureScale("_MainTex", new Vector2(-1, 1));
        material.SetTextureOffset("_MainTex", new Vector2(1, 0));
        EditorUtility.SetDirty(material);
    }
    static Sprite Sprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Bilinear;
        var st = new TextureImporterSettings(); importer.ReadTextureSettings(st); st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteAlignment = (int)SpriteAlignment.Center; importer.SetTextureSettings(st); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Material Material(string path, Shader shader, Texture texture, Color color)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
        mat.name = Path.GetFileNameWithoutExtension(path); mat.shader = shader; mat.SetTexture("_MainTex", texture); mat.SetColor("_Color", color); EditorUtility.SetDirty(mat); return mat;
    }
    static Dictionary<string, Material> Palette(Shader shader)
    {
        var result = new Dictionary<string, Material>(); string name = "";
        foreach (string line in File.ReadLines(Source + "/Models/PokerSet.mtl"))
        {
            if (line.StartsWith("newmtl ")) name = line.Substring(7).Trim();
            else if (line.StartsWith("Kd "))
            {
                var rgb = line.Substring(3).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                result[name] = Material(Output + "/Materials/Chip-" + name + ".mat", shader, null, new Color(rgb[0], rgb[1], rgb[2]));
            }
        }
        return result;
    }
    static Renderer Model(string path, Transform parent, Material[] materials, Dictionary<string, Material> palette = null)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) throw new InvalidOperationException("Model import not complete: " + path);
        var instance = Object.Instantiate(model, parent); instance.name = Path.GetFileNameWithoutExtension(path);
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length != 1) throw new InvalidOperationException("Expected one mesh renderer: " + path);
        // Prefer imported material names, falling back to the explicitly exported OBJ group order.
        if (palette != null)
        {
            var imported = renderers[0].sharedMaterials;
            if (imported.Length == materials.Length && imported.All(m => m != null && palette.ContainsKey(m.name.Replace(" (Instance)", ""))))
                materials = imported.Select(m => palette[m.name.Replace(" (Instance)", "")]).ToArray();
        }
        renderers[0].sharedMaterials = materials;
        if (instance.GetComponentInChildren<MeshFilter>().sharedMesh.subMeshCount != materials.Length) throw new InvalidOperationException("Submesh/material mismatch: " + path);
        return renderers[0];
    }
}
