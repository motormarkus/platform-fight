using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pienen esineen (pullo, myöhemmin muutkin) nosto lattialta ja heitto: yhteinen kuvasarja heroille ja punkkarille
/// (pullonosto.png / punk_pullo.png). Kuvat: 0 tappeluasento, 1–5 kumartuu, tarttuu ja nousee, 6–7 käsi taakse,
/// 8 ei käytössä, 9 heitto (irti), 10–11 paluu. Käden paikka kuvissa (yks, kuvan alareunan keskeltä, katse oikealle)
/// ja esineen kallistus (+ = yläpää taaksepäin).
/// </summary>
public static class ThrowPose
{
    public struct Grip { public Vector2 hand; public float rot; public Grip(float x, float y, float r) { hand = new Vector2(x, y); rot = r; } }

    public static readonly int[] PickFrames = { 1, 2, 3, 4, 5 };
    public static readonly float[] PickTimes = { 0.07f, 0.08f, 0.1f, 0.08f, 0.08f };
    public static readonly int[] ThrowFrames = { 6, 7, 9, 10, 11 };
    public static readonly float[] ThrowTimes = { 0.1f, 0.12f, 0.1f, 0.1f, 0.14f };
    /// ThrowFrames-indeksi, jonka alussa esine lähtee kädestä.
    public const int ReleaseIndex = 2;

    // ennen kuvaa 2 esine on vielä lattialla; hero pitää oikeassa (lähemmässä) kädessä
    public static readonly Dictionary<int, Grip> Hero = new Dictionary<int, Grip>
    {
        { 2, new Grip(0.92f, 0.31f, 90f) }, { 3, new Grip(0.66f, 0.31f, 90f) }, { 4, new Grip(-0.34f, 1.02f, 40f) },
        { 5, new Grip(-0.33f, 1.89f, 10f) }, { 6, new Grip(-1.17f, 2.91f, 30f) }, { 7, new Grip(-0.87f, 2.96f, 40f) },
        { 9, new Grip(1.99f, 2.35f, -60f) },
    };
    // Ruby (sankaritar_pullonosto.png, sama 12 kuvan rakenne): nostaa etukädellä, pitää ja heittää takakädellä
    public static readonly Dictionary<int, Grip> Ruby = new Dictionary<int, Grip>
    {
        { 2, new Grip(-0.36f, 0.31f, 90f) }, { 3, new Grip(-0.18f, 0.77f, 70f) }, { 4, new Grip(-0.16f, 1.49f, 40f) },
        { 5, new Grip(-0.26f, 1.90f, 10f) }, { 6, new Grip(-0.80f, 3.45f, 30f) }, { 7, new Grip(-1.08f, 3.07f, 40f) },
        { 9, new Grip(1.02f, 3.07f, -60f) },
    };
    public static readonly Dictionary<int, Grip> Punk = new Dictionary<int, Grip>
    {
        { 2, new Grip(0.77f, 0.36f, 90f) }, { 3, new Grip(0.66f, 0.31f, 90f) }, { 4, new Grip(0.71f, 1.84f, 45f) },
        { 5, new Grip(0.77f, 2.09f, 10f) }, { 6, new Grip(0.77f, 2.30f, 10f) }, { 7, new Grip(-0.71f, 3.01f, 40f) },
        { 9, new Grip(1.68f, 2.45f, -60f) },
    };
    public static readonly int[] PunkPickFrames = { 1, 2, 3, 4, 5 };
    public static readonly int[] PunkThrowFrames = { 6, 7, 9, 10, 11, 12 };
    public static readonly float[] PunkThrowTimes = { 0.12f, 0.16f, 0.1f, 0.1f, 0.12f, 0.12f };

    /// Sarjan kuva ajanhetkellä t (-1 = sarja loppui).
    public static int Index(float[] times, float t)
    {
        for (int i = 0; i < times.Length; i++) { if (t < times[i]) return i; t -= times[i]; }
        return -1;
    }
    public static float Total(float[] times) { float s = 0f; foreach (var x in times) s += x; return s; }
    public static float Start(float[] times, int index) { float s = 0f; for (int i = 0; i < index; i++) s += times[i]; return s; }
}
