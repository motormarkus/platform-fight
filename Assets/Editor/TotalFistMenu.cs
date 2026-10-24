using UnityEditor;

/// <summary>Lyhyt oma valikko uusimmille työkaluille (Beat em up -valikko on niin pitkä, että kohdat voivat jäädä piiloon).</summary>
public static class TotalFistMenu
{
    [MenuItem("Total Fist/Luo kämppä pelihallin yläkertaan (261)")]
    static void FlatMenu() { BeatEmUpSetup.CreateFlat(); }
}
