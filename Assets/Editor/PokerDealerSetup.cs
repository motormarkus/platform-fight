using System.IO;
using UnityEditor;
using UnityEngine;

public static class PokerDealerSetup
{
    const string Source = "Assets/ArtSources/Poker/dealer-button.png";
    const string Target = "Assets/Resources/Poker/DealerButton.asset";

    [MenuItem("Beat em up/195. Holdem: ota jakajan nappi käyttöön")]
    static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Jakajan nappi", "Paina ensin Stop.", "OK");
            return;
        }
        Configure();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Jakajan nappi", "Jakajan nappi liitetty pokeripöytien asetuksiin. Avaa pokerinäkymä uudelleen nähdäksesi sen.", "OK");
    }

    public static void Configure()
    {
        AssetDatabase.ImportAsset(Source, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(Source) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Jakajan napin kuva puuttuu", Source);
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
        // Measured opaque bounds in the original 1717 x 916 PNG, with padding.
        // Crop only the Sprite rectangle; preserve the original image bytes.
        var fresh = Sprite.Create(texture, new Rect(396, 204, 922, 458),
            new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
        fresh.name = "Jakajan nappi";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Target);
        if (sprite == null) { AssetDatabase.CreateAsset(fresh, Target); sprite = fresh; }
        else { EditorUtility.CopySerialized(fresh, sprite); Object.DestroyImmediate(fresh); EditorUtility.SetDirty(sprite); }
        foreach (var guid in AssetDatabase.FindAssets("t:PokerPresentationSettings", new[] { "Assets" }))
        {
            var settings = AssetDatabase.LoadAssetAtPath<PokerPresentationSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (settings == null) continue;
            Undo.RecordObject(settings, "Jakajan nappi");
            settings.dealerMarker = sprite;
            EditorUtility.SetDirty(settings);
        }
    }
}
