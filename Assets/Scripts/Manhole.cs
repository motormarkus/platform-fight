using UnityEngine;

/// <summary>
/// Viemärin kansi kadulla: kiinni kansi on maassa, auki kansi nousee, liukuu syrjään ja kaivo näkyy.
/// Door avaa kannen (Door.manhole) ennen laskeutumista ja kun hero nousee viemäristä (Door.emergeManhole).
/// Kansi sulkeutuu itsestään, kun hero kävelee pois tai alue vaihtuu.
/// </summary>
public class Manhole : MonoBehaviour
{
    public SpriteRenderer lid, hole;
    [Tooltip("Katu, jolla kansi on.")]
    public Area area;
    [Tooltip("Kansi sulkeutuu, kun hero on näin kaukana (x).")]
    public float closeDistance = 3.4f;
    /// Kannen ja kaivon koko kadulla (1 = kuvan oma koko, 2,8 yksikköä). Ei tallennu sceneen: säädetään tästä. Kädessä pidetty kansi on heron kuvissa noin 1,7 yksikköä leveä.
    public float sizeScale => 0.62f;
    /// Kaivo on kannen kokoinen: kuvien leveyssuhde 1248/1436 ja korkeussuhde 519/641 tasataan kannen jalanjälkeen (hiukan reunojen yli, jotta kansi peittää sen kokonaan).
    static readonly Vector2 HoleStretch = new Vector2(1.05f, 1.05f * 1.075f);
    public float openTime = 0.45f, closeTime = 0.22f;
    [Tooltip("Kannen äänet (nosto ja sulkeutuminen): arvotaan, ei samaa kahdesti peräkkäin.")]
    public AudioClip[] openSounds;
    static int lastSound = -1;
    [Tooltip("Pohjapiirto: kansi ja kaivo piirretään tämän järjestysnumeron kohdalla (taustojen päällä, hahmojen alla).")]
    public int groundOrder = -9000;

    float k;            // 0 = kiinni, 1 = auki
    bool wantOpen;
    Vector3 lidHome;
    PlayerController pc;

    public bool IsOpen => wantOpen;
    /// Hero on nostanut kannen pois (kantaa sitä tai se on heitetty): kaivo pysyy auki, eikä kansi palaa itsestään.
    public bool lidAway;

    /// Hero nostaa kannen kaivon päältä: kaivo auki heti, kansi piiloon (se näkyy heron kuvissa).
    public void LiftByHero()
    {
        lidAway = true; wantOpen = true; k = 1f;
        if (openSounds != null && openSounds.Length > 0) { var c = openSounds[Random.Range(0, openSounds.Length)]; if (c != null) HitFx.PlayClip(c, 0.8f); }
        Apply();
    }

    void Awake()
    {
        if (lid != null) lidHome = lid.transform.localPosition;
        Apply();
    }

    public void SetOpen(bool open, bool instant = false)
    {
        if (open == wantOpen && !instant) return;
        wantOpen = open;
        if (!instant && openSounds != null && openSounds.Length > 0)
        {
            // sama ääni sekä nostossa että sulkeutuessa, ei samaa kahdesti peräkkäin
            int i = Random.Range(0, openSounds.Length);
            if (openSounds.Length > 1 && i == lastSound) i = (i + 1 + Random.Range(0, openSounds.Length - 1)) % openSounds.Length;
            lastSound = i;
            if (openSounds[i] != null) HitFx.PlayClip(openSounds[i], 0.8f);
        }
        if (instant) { k = open ? 1f : 0f; Apply(); }
    }

    void Update()
    {
        if (wantOpen && !lidAway)
        {
            if (pc == null) pc = FindFirstObjectByType<PlayerController>();
            bool away = Area.Current != area
                || (pc != null && (Mathf.Abs(pc.transform.position.x - transform.position.x) > closeDistance
                                   || Mathf.Abs(pc.transform.position.y - transform.position.y) > 2.4f));
            if (away) SetOpen(false, Area.Current != area);   // alueen vaihtuessa heti (pimennyksessä)
        }
        float target = wantOpen ? 1f : 0f;
        if (!Mathf.Approximately(k, target))
        {
            k = Mathf.MoveTowards(k, target, Time.deltaTime / Mathf.Max(0.05f, wantOpen ? openTime : closeTime));
            Apply();
        }
    }

    void Apply()
    {
        if (hole != null) { hole.enabled = k > 0.12f; hole.sortingOrder = groundOrder; hole.transform.localScale = new Vector3(HoleStretch.x, HoleStretch.y, 1f) * sizeScale; }
        if (lid == null) return;
        lid.enabled = k < 0.98f && !lidAway;
        float up = Mathf.Sin(Mathf.Clamp01(k * 2f) * Mathf.PI * 0.5f);              // nousu ensin
        float aside = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 2f - 1f));           // sitten syrjään
        lid.transform.localPosition = lidHome + new Vector3(aside * 1.8f, up * 0.9f - aside * 0.5f, 0f) * sizeScale;
        lid.transform.localScale = Vector3.one * sizeScale * (1f + 0.12f * up);
        lid.color = new Color(1f, 1f, 1f, 1f - aside);
        // liikkeessä kansi piirretään hahmojen päälle, levossa maahan
        lid.sortingOrder = k > 0.001f ? 3000 : groundOrder + 1;
    }
}
