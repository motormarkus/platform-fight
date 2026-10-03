using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heron takimmaisen (oikean) nyrkin paikka kuvissa (yks, kuvan alareunan keskeltä, katse oikealle).
/// Pullo pidetään tässä nyrkissä: nyrkki piirretään pullon päälle (Bottle: maski).
/// Mitattu kuvista idle/kavely/juoksu; muissa kuvissa oletus.
/// </summary>
public static class HeroFist
{
    public static readonly Vector2 Default = new Vector2(0.13f, 2.28f);

    static readonly Dictionary<string, Vector2> table = new Dictionary<string, Vector2>
    {
        { "idle_0", new Vector2(0.13f, 2.26f) },
        { "idle_1", new Vector2(0.14f, 2.27f) },
        { "idle_2", new Vector2(0.13f, 2.29f) },
        { "idle_3", new Vector2(0.11f, 2.29f) },
        { "idle_4", new Vector2(0.13f, 2.31f) },
        { "idle_5", new Vector2(0.15f, 2.35f) },
        { "kavely_0", new Vector2(0.02f, 2.30f) },
        { "kavely_1", new Vector2(-0.01f, 2.29f) },
        { "kavely_2", new Vector2(-0.02f, 2.25f) },
        { "kavely_3", new Vector2(-0.02f, 2.26f) },
        { "kavely_4", new Vector2(0.07f, 2.36f) },
        { "kavely_5", new Vector2(0.10f, 2.44f) },
        { "kavely_6", new Vector2(0.13f, 2.48f) },
        { "kavely_7", new Vector2(0.21f, 2.54f) },
        { "kavely_8", new Vector2(0.21f, 2.53f) },
        { "kavely_9", new Vector2(0.19f, 2.42f) },
        { "kavely_10", new Vector2(0.16f, 2.38f) },
        { "kavely_11", new Vector2(0.07f, 2.34f) },
        { "juoksu_0", new Vector2(0.28f, 2.33f) },
        { "juoksu_1", new Vector2(0.13f, 2.25f) },
        { "juoksu_2", new Vector2(-0.21f, 2.22f) },
        { "juoksu_3", new Vector2(-0.26f, 2.26f) },
        { "juoksu_4", new Vector2(-0.28f, 2.28f) },
        { "juoksu_5", new Vector2(-0.13f, 2.19f) },
        { "juoksu_6", new Vector2(0.05f, 2.20f) },
        { "juoksu_7", new Vector2(0.37f, 2.45f) },
        { "juoksu_8", new Vector2(0.45f, 2.56f) },
        { "juoksu_9", new Vector2(0.45f, 2.59f) },
        { "juoksu_10", new Vector2(0.47f, 2.23f) },
    };

    public static Vector2 For(Sprite s)
    {
        return s != null && table.TryGetValue(s.name, out var v) ? v : Default;
    }
}
