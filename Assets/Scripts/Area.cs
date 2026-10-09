using UnityEngine;

/// <summary>
/// Pelialue (esim. katu tai S-Club sisältä): kävelyalueen syvyysrajat, jalkakäytävä ja kameran rajat.
/// Ovi asettaa nämä pelaajalle ja kameralle, kun alueelle siirrytään.
/// </summary>
public class Area : MonoBehaviour
{
    public string areaName = "Alue";
    [Tooltip("Alueen oma musiikki (tyhjä = kentän pääkappale).")]
    public AudioClip music;
    [Tooltip("Alueen taustahäly silmukkana (tyhjä = Resources/Ambienssi/<alueen nimi>, esim. laivan_kansi.wav).")]
    public AudioClip ambience;
    [Range(0f, 2f)] public float ambienceVolume = 1f;
    /// Alue, jolla pelaaja on (viimeksi asetettu).
    public static Area Current;

    [Header("Kävelyalue (y = syvyys)")]
    public float minDepthY = -4.3f;
    public float maxDepthY = -0.8f;

    [Header("Jalkakäytävä")]
    public bool useSidewalk = true;
    public float sidewalkHeight = 0.42f;
    public float curbDepthY = -1.2f;

    [Tooltip("Takaraja kohdittain (x maailmassa, suurin syvyys y), esim. terassin kaide. Tyhjä = maxDepthY kaikkialla. Välissä lineaarisesti.")]
    public Vector2[] depthLimits;

    [Tooltip("Perspektiivi: hahmojen koko takaseinällä (maxDepthY) suhteessa eteen (minDepthY). 1 = ei perspektiiviä (esim. syvä pokerihuone 0.85).")]
    [Range(0.5f, 1f)] public float backScale = 1f;

    /// Hahmon kuvan kerroin syvyydessä y nykyisellä alueella (takana pienempi).
    public static float DepthScale(float y)
    {
        var a = Current;
        if (a == null || a.backScale >= 0.999f || a.maxDepthY <= a.minDepthY) return 1f;
        return Mathf.Lerp(1f, a.backScale, Mathf.InverseLerp(a.minDepthY, a.maxDepthY, y));
    }

    /// Suurin sallittu syvyys kohdassa x (nykyisellä alueella).
    public static float MaxDepthAt(float x, float fallback)
    {
        var a = Current;
        return a == null ? fallback : a.MaxDepthAtX(x, fallback);
    }

    /// Suurin sallittu syvyys kohdassa x tällä alueella.
    public float MaxDepthAtX(float x, float fallback)
    {
        if (depthLimits == null || depthLimits.Length == 0) return fallback;
        var d = depthLimits;
        if (x <= d[0].x) return Mathf.Min(fallback, d[0].y);
        for (int i = 1; i < d.Length; i++)
            if (x <= d[i].x) return Mathf.Min(fallback, Mathf.Lerp(d[i - 1].y, d[i].y, (x - d[i - 1].x) / Mathf.Max(0.001f, d[i].x - d[i - 1].x)));
        return Mathf.Min(fallback, d[d.Length - 1].y);
    }

    [Header("Kameran rajat (x)")]
    public float camMinX = -100f;
    public float camMaxX = 100f;
    [Tooltip("Pelaaja ei kävele tätä oikeammalle (esim. laiturin reuna). 0 = ei rajaa.")]
    public float walkMaxX = 0f;
    [Tooltip("Pelaaja ei kävele tätä vasemmalle (esim. laivan keula). 0 = ei rajaa.")]
    public float walkMinX = 0f;
    [Tooltip("Kameran koko tällä alueella (orthographic size). 0 = oletus (kadun koko).")]
    public float camSize = 0f;
    [Tooltip("Kuinka paljon kamera nousee, kun pelaaja kävelee seinän viereen (yksikköä). 0 = korkeus pysyy.")]
    public float camRiseY = 0f;
    [Tooltip("Kameran korkeuden siirto tällä alueella (yksikköä, pelaaja edessä). Esim. pokerihuone: kamera laskee lattialle.")]
    public float camOffsetY = 0f;

    public void Apply(PlayerController pc)
    {
        Current = this;
        MusicPlayer.SetAreaMusic(music);
        if (pc != null)
        {
            pc.minDepthY = minDepthY;
            pc.maxDepthY = maxDepthY;
            pc.useSidewalk = useSidewalk;
            pc.sidewalkHeight = sidewalkHeight;
            pc.curbDepthY = curbDepthY;
        }
        var cam = CameraFollow.Instance;
        if (cam != null)
        {
            cam.minX = camMinX;
            cam.maxX = camMaxX;
            cam.SetSize(camSize);   // aluevaihto tapahtuu pimennyksessä: koko vaihtuu heti
            cam.SetVertical(camRiseY, minDepthY, maxDepthY, camOffsetY);
        }
    }
}
