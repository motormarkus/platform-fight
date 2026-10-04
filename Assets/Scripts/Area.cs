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

    /// Suurin sallittu syvyys kohdassa x (nykyisellä alueella).
    public static float MaxDepthAt(float x, float fallback)
    {
        var a = Current;
        if (a == null || a.depthLimits == null || a.depthLimits.Length == 0) return fallback;
        var d = a.depthLimits;
        if (x <= d[0].x) return Mathf.Min(fallback, d[0].y);
        for (int i = 1; i < d.Length; i++)
            if (x <= d[i].x) return Mathf.Min(fallback, Mathf.Lerp(d[i - 1].y, d[i].y, (x - d[i - 1].x) / Mathf.Max(0.001f, d[i].x - d[i - 1].x)));
        return Mathf.Min(fallback, d[d.Length - 1].y);
    }

    [Header("Kameran rajat (x)")]
    public float camMinX = -100f;
    public float camMaxX = 100f;

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
        }
    }
}
