using UnityEditor;

/// Hahmonvalinnan idle- ja sarjakuvat (Resources/Valikko/*_idle.png, *_sarja.png): isoja ruudukkoja (3520 px),
/// tuodaan täysikokoisina ilman pakkausta ja ilman kahden potenssiin skaalausta.
public class MenuArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.Contains("/Resources/Valikko/")) return;
        if (!(p.EndsWith("_idle.png") || p.EndsWith("_sarja.png"))) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 8192;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    }
}
