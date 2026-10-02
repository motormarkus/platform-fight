using UnityEngine;

/// <summary>
/// Paikallaan oleva NPC: useampi idle-sarja (esim. 3 × 6 kuvaa). Soittaa yhden sarjan kuvat järjestyksessä,
/// pitää välillä taukoa ja vaihtaa sitten satunnaisesti toiseen sarjaan.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NpcIdle : MonoBehaviour
{
    public Sprite[] sprites;
    [Tooltip("Kuvia per sarja (sprites jaetaan peräkkäisiin sarjoihin).")]
    public int framesPerSet = 6;
    public float frameTime = 0.35f;
    [Tooltip("Tauko sarjan lopussa ennen seuraavaa (s), satunnainen väliltä.")]
    public Vector2 pauseRange = new Vector2(0.6f, 1.8f);
    public bool flipX;

    SpriteRenderer sr;
    int set, frame;
    float clock, pause;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        PlayerController.SortByFrameNumber(sprites);
        sr.flipX = flipX;
        set = 0;
        if (sprites != null && sprites.Length > 0) sr.sprite = sprites[0];
    }

    int Sets => sprites == null ? 0 : Mathf.Max(1, sprites.Length / Mathf.Max(1, framesPerSet));

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;
        if (pause > 0f) { pause -= Time.deltaTime; return; }
        clock += Time.deltaTime;
        if (clock < frameTime) return;
        clock -= frameTime;
        frame++;
        if (frame >= framesPerSet)
        {
            frame = 0;
            pause = Random.Range(pauseRange.x, pauseRange.y);   // jää hetkeksi viimeiseen asentoon
            int next = Random.Range(0, Sets);
            if (Sets > 1 && next == set) next = (next + 1) % Sets;
            set = next;
            return;
        }
        int i = Mathf.Min(set * framesPerSet + frame, sprites.Length - 1);
        sr.sprite = sprites[i];
    }
}
