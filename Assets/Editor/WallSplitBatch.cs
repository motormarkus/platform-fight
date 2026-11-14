using UnityEditor;

/// <summary>Komentoriviajo Muurin jaolle (valikko 349): Unity.exe -batchmode -executeMethod BeatEmUpSetup.RunWallSplitBatch. Ilmoitukset menevät lokiin.</summary>
public static partial class BeatEmUpSetup
{
    public static void RunWallSplitBatch()
    {
        batch = true;
        try { SplitWallScene(); }
        finally { batch = false; }
    }
}
