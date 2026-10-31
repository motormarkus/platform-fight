using UnityEditor;

/// <summary>Resources/Kartta-kansion kuvat (maailmankartta) tuodaan täydessä koossa: Unityn oletus 2048 pienentäisi 5400x2700-kartan.</summary>
public class MapTextureImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Kartta/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.maxTextureSize = 8192;
        ti.mipmapEnabled = true;
        ti.wrapModeU = UnityEngine.TextureWrapMode.Repeat;
        ti.wrapModeV = UnityEngine.TextureWrapMode.Clamp;
        ti.filterMode = UnityEngine.FilterMode.Trilinear;
        ti.anisoLevel = 4;
        ti.isReadable = false;
    }
}
