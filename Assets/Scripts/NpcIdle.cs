using UnityEngine;

/// <summary>
/// Paikallaan oleva NPC, jonka kuvat ovat erillisiä asentoja (ei sujuvaa animaatiota).
/// Perusasento vaihtuu rauhallisesti "kotikuvien" välillä, ja välillä tehdään ele (vilkutus, kello…),
/// jota pidetään hetki ennen paluuta. Kuvat liukuvat toisiinsa ristihäivytyksellä, ja koko ajan on kevyt hengitys.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NpcIdle : MonoBehaviour
{
    public Sprite[] sprites;
    [Tooltip("Perusasennon kuvat (indeksit sprites-taulukossa), joiden välillä vaihdellaan rauhallisesti.")]
    public int[] homeFrames = { 6, 7, 8, 11 };
    [Tooltip("Eleiden kuvat (indeksit), joista arvotaan välillä yksi.")]
    public int[] gestureFrames = { 0, 1, 2, 3, 4, 5, 9, 14, 15, 16, 17 };
    [Tooltip("Kuinka kauan kotikuvaa pidetään (s), satunnainen väliltä.")]
    public Vector2 homeHold = new Vector2(1.4f, 2.6f);
    [Tooltip("Kuinka kauan elettä pidetään (s).")]
    public Vector2 gestureHold = new Vector2(1.2f, 2.0f);
    [Tooltip("Todennäköisyys, että seuraava vaihto on ele (muuten toinen kotikuva).")]
    [Range(0f, 1f)] public float gestureChance = 0.4f;
    [Tooltip("Ristihäivytyksen kesto (s).")]
    public float fadeTime = 0.22f;
    [Tooltip("Hengityksen voimakkuus (pystyvenytys) ja tahti.")]
    public float breathAmount = 0.012f, breathSpeed = 1.6f;
    public bool flipX;

    SpriteRenderer sr, fade;
    float holdLeft, fadeLeft, breathPhase;
    bool inGesture;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(sprites);
        sr.flipX = flipX;
        baseScale = transform.localScale;
        breathPhase = Random.value * 10f;

        // vanha kuva häivytetään tämän päältä pois, uusi on alla täytenä (ei läpikuultavuutta)
        var go = new GameObject("Häivytys");
        go.transform.SetParent(transform, false);
        fade = go.AddComponent<SpriteRenderer>();
        fade.flipX = flipX;
        fade.enabled = false;

        if (sprites != null && sprites.Length > 0) sr.sprite = sprites[Pick(homeFrames)];
        holdLeft = Random.Range(homeHold.x, homeHold.y);
    }

    int Pick(int[] list)
    {
        if (list == null || list.Length == 0) return 0;
        return Mathf.Clamp(list[Random.Range(0, list.Length)], 0, sprites.Length - 1);
    }

    void Show(int index)
    {
        if (sprites[index] == sr.sprite) return;
        fade.sprite = sr.sprite;
        fade.sortingLayerID = sr.sortingLayerID;
        fade.sortingOrder = sr.sortingOrder + 1;
        fade.enabled = true;
        fadeLeft = fadeTime;
        sr.sprite = sprites[index];
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;
        float dt = Time.deltaTime;

        // hengitys: kevyt pystyvenytys jalkojen ympäri (pivot alareunassa)
        breathPhase += dt * breathSpeed;
        float b = 1f + Mathf.Sin(breathPhase * Mathf.PI * 2f * 0.5f) * breathAmount;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * b, baseScale.z);

        if (fadeLeft > 0f)
        {
            fadeLeft -= dt;
            fade.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fadeLeft / fadeTime));
            if (fadeLeft <= 0f) fade.enabled = false;
        }

        holdLeft -= dt;
        if (holdLeft > 0f) return;
        if (inGesture || Random.value >= gestureChance)
        {
            // takaisin perusasentoon (tai toiseen kotikuvaan)
            inGesture = false;
            int i = Pick(homeFrames);
            for (int t = 0; t < 3 && sprites[i] == sr.sprite; t++) i = Pick(homeFrames);
            Show(i);
            holdLeft = Random.Range(homeHold.x, homeHold.y);
        }
        else
        {
            inGesture = true;
            Show(Pick(gestureFrames));
            holdLeft = Random.Range(gestureHold.x, gestureHold.y);
        }
    }
}
