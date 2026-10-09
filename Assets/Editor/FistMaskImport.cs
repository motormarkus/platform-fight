using UnityEditor;
using UnityEngine;

/// Roccon nyrkin maskit (Resources/Nyrkki/*.png): sprite, 100 px/yks, keskipiste nyrkin kohdalla (pullon päälle piirrettävä nyrkki).
public class FistMaskImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.Contains("/Resources/Nyrkki/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.spritePivot = new Vector2(0.5f, 0.5f);
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.Center;
        st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
    }
}
