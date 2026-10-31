using UnityEditor;

/// Suudelmakuvat (Resources/Suukko): ei skaalausta lähimpään kahden potenssiin (muuten kuvat venyvät), ei mipmappeja.
public class SuukkoImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Suukko/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureType = TextureImporterType.Default;
        ti.maxTextureSize = 1024;
        ti.isReadable = false;
    }
}
