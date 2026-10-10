using UnityEditor;
using UnityEngine;

/// Termospullo, juontikuvat (Resources/Termari) ja Roccon hyppy (Resources/Hyppy): sprite, 100 px/yks, kiinnityspiste alhaalla keskellä.
public class TermariImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.Contains("/Resources/Termari/") && !p.Contains("/Resources/Hyppy/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        st.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(st);
    }
}
