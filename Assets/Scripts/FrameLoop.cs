using System.Linq;
using UnityEngine;

/// <summary>
/// Pyörittää kuvasarjaa (Resources/&lt;folder&gt;, nimijärjestyksessä) silmukkana esim. soihtuliekille tai lipulle.
/// Jos kansiossa ei ole kuvia, objekti pysyy piilossa. Kuvat skaalataan niin, että korkeus on worldHeight (maailmayksikköä).
/// </summary>
public class FrameLoop : MonoBehaviour
{
    public string folder = "Muuri/Soihtu";
    public float fps = 10f, worldHeight = 1.5f;
    public Vector2 pivot = new Vector2(0.5f, 0f);
    public int sortingOrder = -9990;
    public float phase;

    Sprite[] frames; SpriteRenderer sr;

    void Start()
    {
        var tex = Resources.LoadAll<Texture2D>(folder).OrderBy(t => t.name).ToArray();
        if (tex.Length == 0) { enabled = false; return; }
        frames = tex.Select(t => { t.filterMode = FilterMode.Point; return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, t.height / worldHeight); }).ToArray();
        sr = GetComponent<SpriteRenderer>(); if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder; sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null) return;
        sr.sprite = frames[(int)((Time.time + phase) * fps) % frames.Length];
    }
}
