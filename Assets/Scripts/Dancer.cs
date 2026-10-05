using UnityEngine;

/// <summary>Taustahahmon silmukka-animaatio (esim. tanssija lavalla).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Dancer : MonoBehaviour
{
    public Sprite[] sprites;
    public float frameTime = 0.124f;
    [Tooltip("Mistä kuvasta silmukka alkaa (useampi tanssija eri tahdissa).")]
    public int startFrame;
    public bool flipX;
    [Tooltip("Kuvat annetussa järjestyksessä (esim. toistettu sarja), ei lajitella numeron mukaan.")]
    public bool keepOrder;

    SpriteRenderer sr;
    float clock;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!keepOrder) PlayerController.SortByFrameNumber(sprites);
        sr.flipX = flipX;
        clock = startFrame * frameTime;
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;
        clock += Time.deltaTime;
        sr.sprite = sprites[(int)(clock / frameTime) % sprites.Length];
    }
}
