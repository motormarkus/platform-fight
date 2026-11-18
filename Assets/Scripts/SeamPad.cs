using UnityEngine;

/// <summary>
/// Merkki taustakuvalle, jota on levennetty hieman (valikko 401), jotta vierekkäisten kuvien väliin ei jää rakoa suurilla x-koordinaateilla
/// (float-tarkkuus x ≈ 100 000: ~0,008 yksikköä ≈ 1 px). baseScaleX = alkuperäinen skaala: valikon voi ajaa uudelleen samalla tuloksella.
/// </summary>
public class SeamPad : MonoBehaviour
{
    public float baseScaleX = 1f;
}
