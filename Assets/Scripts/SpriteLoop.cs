using UnityEngine;

/// <summary>
/// Toistaa kuvasarjaa silmukkana (esim. videosta otettu idle-animaatio taustan päällä, kuten baarin pokerinpelaajat).
/// Jos altFrames on annettu, sarjat vaihtuvat muutaman kierroksen välein ristihäivytyksellä.
/// Ambienssiääni soi silmukkana vain, kun kuva näkyy ruudulla (sisätila on kaukana muusta kentästä).
/// Loppuanimaatio (finale) alkaa, kun kuva on näkynyt finaleAfter sekuntia: idle häivytetään siihen, ja viimeinen kuva jää.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    public Sprite[] frames;
    [Tooltip("Toinen idle-sarja (valinnainen). Samankokoiset solut ja sama tukipiste kuin frames-sarjassa.")]
    public Sprite[] altFrames;
    public float fps = 12f;
    [Tooltip("Kuinka monta kierrosta yhtä sarjaa pyöritetään ennen vaihtoa (satunnainen väliltä).")]
    public Vector2Int loopsPerSet = new Vector2Int(2, 4);
    [Tooltip("Sarjojen välisen ristihäivytyksen kesto (s).")]
    public float crossfade = 0.4f;

    [Header("Loppuanimaatio (esim. pöytä kaatuu)")]
    [Tooltip("Oma SpriteRenderer (eri solukoko ja paikka kuin idlellä). Tyhjä = ei loppua.")]
    public SpriteRenderer finaleRenderer;
    public Sprite[] finale;
    public float finaleFps = 12f;
    [Tooltip("Kuinka kauan idleä katsotaan ennen loppua (s, vain kun kuva näkyy ruudulla).")]
    public float finaleAfter = 10f;
    public AudioClip finaleSound;
    [Range(0f, 1f)] public float finaleVolume = 1f;
    float watched, ft;
    bool finaleOn;

    [Header("Ääni")]
    public AudioClip ambience;
    [Range(0f, 1f)] public float volume = 0.6f;

    SpriteRenderer sr, fadeSr;
    AudioSource src;
    Sprite[] cur, next;
    float t, tn, fadeLeft;
    int loopsLeft;

    bool HasAlt => altFrames != null && altFrames.Length > 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cur = frames;
        loopsLeft = Random.Range(loopsPerSet.x, loopsPerSet.y + 1);
        if (HasAlt)
        {
            var go = new GameObject("Häivytys");
            go.transform.SetParent(transform, false);
            fadeSr = go.AddComponent<SpriteRenderer>();
            fadeSr.sortingLayerID = sr.sortingLayerID;
            fadeSr.sortingOrder = sr.sortingOrder + 1;
            fadeSr.color = new Color(1f, 1f, 1f, 0f);
        }
        if (ambience != null)
        {
            src = gameObject.AddComponent<AudioSource>();
            src.clip = ambience;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = volume;
            src.mute = true;
        }
    }

    void Start()
    {
        if (src != null) src.Play();
        if (finaleRenderer != null) finaleRenderer.enabled = false;
    }

    bool HasFinale => finaleRenderer != null && finale != null && finale.Length > 0;
    /// Loppuanimaatio on soitettu loppuun (viimeinen kuva näkyy).
    public bool FinaleDone => finaleOn && ft * finaleFps >= finale.Length;

    /// Piilottaa idlen, loppuanimaation ja pelimerkkien äänen (esim. tausta vaihtuu tappelukuvaan).
    public void HideAll()
    {
        if (sr != null) sr.enabled = false;
        if (fadeSr != null) fadeSr.enabled = false;
        if (finaleRenderer != null) finaleRenderer.enabled = false;
        if (src != null) src.Stop();
        enabled = false;
    }

    void Update()
    {
        if (finaleOn) { UpdateFinale(); return; }
        if (HasFinale && sr.isVisible)
        {
            watched += Time.deltaTime;
            if (watched >= finaleAfter) { StartFinale(); return; }
        }
        if (cur == null || cur.Length == 0) return;
        float step = Time.deltaTime * fps;
        t += step;
        if (t >= cur.Length)
        {
            t -= cur.Length;
            if (next == null && HasAlt && --loopsLeft <= 0)
            {
                next = cur == frames ? altFrames : frames;
                tn = 0f;
                fadeLeft = crossfade;
            }
        }
        if (next != null)
        {
            tn += step;
            fadeLeft -= Time.deltaTime;
            float a = crossfade > 0f ? 1f - Mathf.Clamp01(fadeLeft / crossfade) : 1f;
            fadeSr.sprite = next[(int)tn % next.Length];
            fadeSr.color = new Color(1f, 1f, 1f, a);
            if (fadeLeft <= 0f)
            {
                cur = next;
                t = tn % cur.Length;
                next = null;
                fadeSr.color = new Color(1f, 1f, 1f, 0f);
                loopsLeft = Random.Range(loopsPerSet.x, loopsPerSet.y + 1);
            }
        }
        sr.sprite = cur[(int)t % cur.Length];

        if (src != null)
        {
            src.volume = volume;
            src.mute = !sr.isVisible;
        }
    }

    void StartFinale()
    {
        finaleOn = true; ft = 0f;
        finaleRenderer.enabled = true;
        finaleRenderer.sortingOrder = sr.sortingOrder + 2;
        finaleRenderer.sprite = finale[0];
        finaleRenderer.color = new Color(1f, 1f, 1f, 0f);
        if (finaleSound != null)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false; a.spatialBlend = 0f;
            a.PlayOneShot(finaleSound, finaleVolume);
        }
    }

    void UpdateFinale()
    {
        ft += Time.deltaTime;
        int i = Mathf.Min((int)(ft * finaleFps), finale.Length - 1);   // viimeinen kuva jää (pöytä nurin, tappeluasento)
        finaleRenderer.sprite = finale[i];
        // idle häivytetään loppuun lyhyesti; pelimerkkien ääni hiljenee
        float a = crossfade > 0f ? Mathf.Clamp01(ft / crossfade) : 1f;
        finaleRenderer.color = new Color(1f, 1f, 1f, a);
        if (a >= 1f) { sr.enabled = false; if (fadeSr != null) fadeSr.enabled = false; }
        if (src != null) src.volume = volume * (1f - Mathf.Clamp01(ft / 1.5f));
    }
}
