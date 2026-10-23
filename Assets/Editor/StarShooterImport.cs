using UnityEditor;
using UnityEngine;

/// <summary>Tähtipyssyn kuvat (Resources/TahtiPyssy): terävä suodatus, ei mipmappeja, ei pakkausta (kuvat pieniä).</summary>
public class StarShooterImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string ap = assetPath.Replace('\\', '/');
        bool sini = ap.StartsWith("Assets/Resources/Sini/") || ap.StartsWith("Assets/Resources/Dina/");
        if (!sini && !ap.StartsWith("Assets/Resources/TahtiPyssy/") && !ap.StartsWith("Assets/Resources/Pelihalli/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.isReadable = false;
        ti.maxTextureSize = 4096;
        bool bigBg = assetPath.EndsWith("stars.png") || assetPath.EndsWith("stars2.png") || assetPath.EndsWith("title.png");
        if (sini) { ti.textureCompression = TextureImporterCompression.Compressed; return; }
        ti.textureCompression = bigBg ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
    }
}
