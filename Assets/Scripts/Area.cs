using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pelialue (esim. katu tai S-Club sisältä): kävelyalueen syvyysrajat, jalkakäytävä ja kameran rajat.
/// Ovi asettaa nämä pelaajalle ja kameralle, kun alueelle siirrytään.
/// </summary>
public class Area : MonoBehaviour
{
    public string areaName = "Alue";
    [Tooltip("Alueen oma musiikki (tyhjä = kentän pääkappale).")]
    public AudioClip music;
    [Tooltip("Alueen musiikin vahvistus (1 = kappaleen oma taso, 1,334 = +2,5 dB; AudioSource ei voi ylittää 1:tä, joten vaikutus riippuu musiikin perustasosta).")]
    public float musicGain = 1f;
    /// Viemärin musiikin vahvistus (+3,9 dB): Viemari.mp3 on n. 3,5 dB hiljaisempi kuin muu musiikki (RMS -19,7 vs n. -16 dBFS).
    public const float SewerDefaultGain = 1.57f;
    [Tooltip("Alueen taustahäly silmukkana (tyhjä = Resources/Ambienssi/<alueen nimi>, esim. laivan_kansi.wav).")]
    public AudioClip ambience;
    [Range(0f, 2f)] public float ambienceVolume = 1f;
    /// Alue, jolla pelaaja on (viimeksi asetettu).
    public static Area Current;

    // Aluerekisteri: alue ilmoittautuu nimellään, kun se latautuu. Ovi voi etsiä kohteensa nimellä, vaikka alue on omassa sceneensä.
    static readonly System.Collections.Generic.Dictionary<string, Area> registry = new System.Collections.Generic.Dictionary<string, Area>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() { registry.Clear(); }

    /// Ladattu alue nimen mukaan (null, jos sitä ei ole ladattuna).
    public static Area Find(string name)
    {
        return !string.IsNullOrEmpty(name) && registry.TryGetValue(name, out var a) && a != null ? a : null;
    }

    /// Aluevaihdon jälkeen puretaan kaikki muut ladatut kenttäscenet (paitsi kohdealueen ja pelaajan scene) ja vapautetaan niiden tekstuurit.
    /// Kutsutaan mustan ruudun aikana: Unity ei vapauta purettujen scenejen kuvia itsestään (UnloadUnusedAssets), ja vapautus voi pysäyttää pelin hetkeksi.
    public static System.Collections.IEnumerator FreeOtherScenes(Area dest, PlayerController pc)
    {
        if (dest == null || pc == null) yield break;
        Scene keepA = dest.gameObject.scene, keepB = pc.gameObject.scene;
        var ops = new System.Collections.Generic.List<AsyncOperation>();
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded || s == keepA || s == keepB) continue;
            var op = SceneManager.UnloadSceneAsync(s);
            if (op != null) ops.Add(op);
        }
        if (ops.Count == 0) yield break;
        foreach (var op in ops) while (!op.isDone) yield return null;
        yield return Resources.UnloadUnusedAssets();
    }

    void Awake() { if (!string.IsNullOrEmpty(areaName)) registry[areaName] = this; }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(areaName) && registry.TryGetValue(areaName, out var a) && a == this) registry.Remove(areaName);
    }

    /// Onko piste pelaajan nykyisellä alueella (alueet ovat eri kohdissa x-akselia). Muualla olevien vihollisten äänet mykistetään.
    public static bool IsHere(Vector3 p, float margin = 8f)
    {
        var cur = Effective;
        if (cur == null) return true;
        return Spans(cur, out float lo, out float hi) ? p.x >= lo - margin && p.x <= hi + margin : true;
    }

    /// Alueen x-väli: kävelyrajat, tai (jos niitä ei ole, esim. katu) kameran rajat reunoineen. False = ei rajausta.
    static bool Spans(Area a, out float lo, out float hi)
    {
        lo = a.walkMinX; hi = a.walkMaxX;
        if (hi > lo) return true;
        if (a.camMaxX > a.camMinX) { lo = a.camMinX - 20f; hi = a.camMaxX + 20f; return true; }
        return false;
    }

    static Area effCache; static int effFrame = -1;
    /// Nykyinen alue. Pelin alussa (ennen ensimmäistä ovea) Current on tyhjä: silloin alue päätellään heron paikasta (muuten kaikki alueet kuuluisivat kadulle).
    public static Area Effective
    {
        get
        {
            if (Current != null) return Current;
            if (effFrame == Time.frameCount) return effCache;
            effFrame = Time.frameCount; effCache = null;
            var pl = Players.Primary;
            if (pl == null) return null;
            float x = pl.transform.position.x;
            foreach (var a in registry.Values)
                if (a != null && Spans(a, out float lo, out float hi) && x >= lo && x <= hi) { effCache = a; break; }
            return effCache;
        }
    }

    [Header("Liukas pinta (jää)")]
    [Tooltip("Hero ja vihut liukuvat: vauhti kasvaa ja hiipuu hitaasti.")]
    public bool slippery;
    [Tooltip("Kiihtyvyys jäällä (yksikköä/s²). Pienempi = liukkaampi.")]
    public float iceAccel = 9f;
    [Tooltip("Hidastuvuus, kun ei liikuta (yksikköä/s²).")]
    public float iceGlide = 3.5f;
    /// Käytössä oleva kiihtyvyys ja hidastuvuus: jää on liukkaampi kuin vanhaan scene-arvoon on tallennettu (min).
    public float AccelNow => Mathf.Min(iceAccel, 5f);
    public float GlideNow => Mathf.Min(iceGlide, 1.4f);

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
    [Tooltip("Takarajan käyrä (depthLimits) saa ylittää alueen maxDepthY:n (esim. dinerin STAFF-oven koloon pääsee muuta takarajaa ylemmäs).")]
    public bool depthLimitsExceedMax;

    float Cap(float fallback, float limit) => depthLimitsExceedMax ? limit : Mathf.Min(fallback, limit);

    public float MaxDepthAtX(float x, float fallback)
    {
        if (depthLimits == null || depthLimits.Length == 0) return fallback;
        var d = depthLimits;
        if (x <= d[0].x) return Cap(fallback, d[0].y);
        for (int i = 1; i < d.Length; i++)
            if (x <= d[i].x) return Cap(fallback, Mathf.Lerp(d[i - 1].y, d[i].y, (x - d[i - 1].x) / Mathf.Max(0.001f, d[i].x - d[i - 1].x)));
        return Cap(fallback, d[d.Length - 1].y);
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
        float gain = musicGain;
        if (Mathf.Approximately(gain, 1f) && areaName != null && areaName.StartsWith("Viemäri")) gain = SewerDefaultGain;   // viemärin musiikki on hiljaisempaa kuin muu: oletusvahvistus ilman valikon ajoa
        MusicPlayer.SetAreaMusic(music, music != null ? gain : 1f);
        if (pc != null)
        {
            pc.minDepthY = minDepthY;
            pc.maxDepthY = maxDepthY;
            pc.useSidewalk = useSidewalk;
            pc.sidewalkHeight = sidewalkHeight;
            pc.curbDepthY = curbDepthY;
        }
        foreach (var o in Players.All)
        {
            if (o == null || o == pc) continue;
            o.minDepthY = minDepthY; o.maxDepthY = maxDepthY; o.useSidewalk = useSidewalk; o.sidewalkHeight = sidewalkHeight; o.curbDepthY = curbDepthY;
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
