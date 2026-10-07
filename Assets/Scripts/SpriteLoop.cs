using UnityEngine;

/// <summary>
/// Toistaa kuvasarjaa silmukkana (esim. videosta otettu idle-animaatio taustan päällä, kuten baarin pokerinpelaajat).
/// Jos altFrames on annettu, sarjat vaihtuvat muutaman kierroksen välein ristihäivytyksellä.
/// Ambienssiääni soi silmukkana vain, kun kuva näkyy ruudulla (sisätila on kaukana muusta kentästä).
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
    }

    void Update()
    {
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
}
