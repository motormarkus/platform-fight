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
        // Ruby: takimmainen (oikea) nyrkki
        { "sankaritar_idle_0", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_idle_1", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_idle_2", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_idle_3", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_idle_4", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_idle_5", new Vector2(-0.56f, 2.64f) },
        { "sankaritar_kavely_0", new Vector2(-0.48f, 2.58f) },
        { "sankaritar_kavely_1", new Vector2(-0.50f, 2.53f) },
        { "sankaritar_kavely_2", new Vector2(-0.46f, 2.58f) },
        { "sankaritar_kavely_3", new Vector2(-0.72f, 2.60f) },
        { "sankaritar_kavely_4", new Vector2(-0.66f, 2.60f) },
        { "sankaritar_kavely_5", new Vector2(-0.62f, 2.60f) },
        { "sankaritar_kavely_6", new Vector2(-0.50f, 2.65f) },
        { "sankaritar_kavely_7", new Vector2(-0.50f, 2.56f) },
        { "sankaritar_kavely_8", new Vector2(-0.46f, 2.48f) },
        { "sankaritar_kavely_9", new Vector2(-0.44f, 2.50f) },
        { "sankaritar_kavely_10", new Vector2(-0.40f, 2.56f) },
        { "sankaritar_kavely_11", new Vector2(-0.62f, 2.56f) },
        { "sankaritar_juoksu_0", new Vector2(-0.78f, 2.10f) },
        { "sankaritar_juoksu_1", new Vector2(-1.05f, 2.18f) },
        { "sankaritar_juoksu_2", new Vector2(-1.05f, 2.18f) },
        { "sankaritar_juoksu_3", new Vector2(-0.90f, 2.16f) },
        { "sankaritar_juoksu_4", new Vector2(-0.89f, 2.44f) },
        { "sankaritar_juoksu_5", new Vector2(-0.55f, 2.07f) },
        { "sankaritar_juoksu_6", new Vector2(-0.19f, 2.10f) },
    };

    public static Vector2 For(Sprite s)
    {
        return s != null && table.TryGetValue(s.name, out var v) ? v : Default;
    }
}
