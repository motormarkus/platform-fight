using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heron etummaisen nyrkin paikka kuvissa (yks, kuvan alareunan keskeltä, katse oikealle).
/// Pullo piirretään tämän nyrkin taakse. Mitattu kuvista idle/kavely/juoksu; muissa kuvissa oletus.
/// </summary>
public static class HeroFist
{
    public static readonly Vector2 Default = new Vector2(0.76f, 2.25f);

    static readonly Dictionary<string, Vector2> table = new Dictionary<string, Vector2>
    {
        { "idle_0", new Vector2(0.76f, 2.22f) },
        { "idle_1", new Vector2(0.77f, 2.22f) },
        { "idle_2", new Vector2(0.75f, 2.23f) },
        { "idle_3", new Vector2(0.75f, 2.24f) },
        { "idle_4", new Vector2(0.76f, 2.26f) },
        { "idle_5", new Vector2(0.76f, 2.27f) },
        { "kavely_0", new Vector2(0.80f, 2.50f) },
        { "kavely_1", new Vector2(0.79f, 2.53f) },
        { "kavely_2", new Vector2(0.81f, 2.52f) },
        { "kavely_3", new Vector2(0.79f, 2.51f) },
        { "kavely_4", new Vector2(0.77f, 2.45f) },
        { "kavely_5", new Vector2(0.77f, 2.44f) },
        { "kavely_6", new Vector2(0.77f, 2.42f) },
        { "kavely_7", new Vector2(0.76f, 2.32f) },
        { "kavely_8", new Vector2(0.74f, 2.27f) },
        { "kavely_9", new Vector2(0.78f, 2.30f) },
        { "kavely_10", new Vector2(0.79f, 2.35f) },
        { "kavely_11", new Vector2(0.79f, 2.46f) },
        { "juoksu_0", new Vector2(0.71f, 2.19f) },
        { "juoksu_1", new Vector2(0.83f, 2.24f) },
        { "juoksu_2", new Vector2(0.89f, 2.46f) },
        { "juoksu_3", new Vector2(0.87f, 2.54f) },
        { "juoksu_4", new Vector2(0.85f, 2.54f) },
        { "juoksu_5", new Vector2(0.82f, 2.33f) },
        { "juoksu_6", new Vector2(0.76f, 2.20f) },
        { "juoksu_7", new Vector2(0.51f, 2.48f) },
        { "juoksu_8", new Vector2(0.46f, 2.66f) },
        { "juoksu_9", new Vector2(0.46f, 2.74f) },
        { "juoksu_10", new Vector2(0.54f, 2.47f) },
    };

    public static Vector2 For(Sprite s)
    {
        return s != null && table.TryGetValue(s.name, out var v) ? v : Default;
    }
}
