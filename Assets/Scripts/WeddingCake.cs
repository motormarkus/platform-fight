using UnityEngine;

/// <summary>
/// Iso hääkakku pöydällä (Vegasin hääkappeli): seisoo pöydän päällä, heilahtaa kun pöytää lyödään ja luhistuu (kuvat 1–5, sulhas- ja morsiusnukke lentävät) kun pöytä on saanut tarpeeksi iskuja
/// (disturbToCollapse) tai hajonnut. Viimeinen kuva jää sirpaleina pöydän päälle. Kuvat: Assets/Sprites/Morsian/haakakku.png (6 kuvaa, 0 ehjä).
/// </summary>
public class WeddingCake : MonoBehaviour
{
    public Crate table;
    public Sprite[] sprites;
    public float tableTop = 1.62f;
    public int disturbToCollapse = 2;
    public float collapseFrameTime = 0.11f;
    public AudioClip[] sounds;
    [Range(0f, 1f)] public float soundVolume = 0.9f;
    [Tooltip("Luhistuessa kerma läiskähtää lattialle (CreamPuddle): liukas, hahmot liukastelevat.")]
    public bool spillCream = true;
    [Tooltip("Läikkiä lattialle (1 pöydän eteen + loput sivuille).")]
    public int creamPuddles = 3;
    bool spilled;

    SpriteRenderer body;
    int seen;
    bool collapsing;
    float t, wobble;

    void Awake()
    {
        var v = new GameObject("Visual"); v.transform.SetParent(transform, false);
        body = v.AddComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(sprites);
        if (sprites != null && sprites.Length > 0) body.sprite = sprites[0];
    }

    void Start() { if (table != null) seen = table.Disturb; }

    void Update()
    {
        if (body == null || sprites == null || sprites.Length < 2) return;
        if (collapsing && !spilled && (t >= collapseFrameTime * 2f || table == null || !table.isActiveAndEnabled)) SpillCream();
        if (table == null || !table.isActiveAndEnabled) { body.enabled = false; return; }   // pöytä poistui: kakku (ja sirpaleet) pois
        Vector3 tp = table.transform.position;
        transform.position = new Vector3(tp.x, tp.y + tableTop, 0f);
        body.sortingOrder = table.SortOrder + 2;
        if (!collapsing)
        {
            if (table.Disturb != seen) { seen = table.Disturb; wobble = 0.35f; }
            if (table.IsBroken || table.Disturb >= disturbToCollapse) { Collapse(); return; }
            if (wobble > 0f) { wobble -= Time.deltaTime; body.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(wobble * 40f) * 2.5f * Mathf.Clamp01(wobble / 0.35f)); }
            else body.transform.localRotation = Quaternion.identity;
            return;
        }
        t += Time.deltaTime;
        int f = 1 + (int)(t / collapseFrameTime);
        body.sprite = sprites[Mathf.Min(f, sprites.Length - 1)];
        body.transform.localRotation = Quaternion.identity;
    }

    /// Kerma lattialle: pisarat lentävät kakusta, läikät pöydän eteen ja sivuille (ei päällekkäin).
    void SpillCream()
    {
        spilled = true;
        if (!spillCream) return;
        Vector3 tp = table != null ? table.transform.position : transform.position - Vector3.up * tableTop;
        CreamPuddle.Splash(transform.position + Vector3.up * 0.4f, 10, 4f);
        CreamPuddle.Spawn(new Vector3(tp.x + Random.Range(-0.3f, 0.3f), tp.y - 0.6f, 0f), -1, Random.value < 0.5f);
        float side = Random.value < 0.5f ? -1f : 1f;
        for (int i = 1; i < creamPuddles; i++, side = -side)
            CreamPuddle.Spawn(new Vector3(tp.x + side * Random.Range(2.6f, 3.4f) * (1f + (i - 1) / 2), tp.y - Random.Range(0.2f, 0.9f), 0f), -1, Random.value < 0.5f);
    }

    void Collapse()
    {
        collapsing = true; t = 0f;
        if (sounds != null && sounds.Length > 0) AudioSource.PlayClipAtPoint(sounds[Random.Range(0, sounds.Length)], transform.position, soundVolume);
        else if (table != null && table.breakSounds != null && table.breakSounds.Length > 0) AudioSource.PlayClipAtPoint(table.breakSounds[Random.Range(0, table.breakSounds.Length)], transform.position, soundVolume);
    }
}
