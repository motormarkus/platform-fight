using UnityEngine;

/// <summary>
/// Pyörittää helikopterin roottoria: toistaa kuvasarjaa silmukkana. reverse = takaperin, jolloin
/// kopterin kaksi roottoria (samaan suuntaan pyörivistä kuvista) pyörivät vastakkain.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RotorLoop : MonoBehaviour
{
    public Sprite[] frames;
    [Tooltip("Kuvaa sekunnissa: pienempi = hitaampi pyöriminen.")]
    public float fps = 12f;
    [Tooltip("Soita kuvat takaperin (vastakkainen pyörimissuunta).")]
    public bool reverse;
    SpriteRenderer sr;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime * fps;
        int n = frames.Length, i = (int)t % n;
        sr.sprite = frames[reverse ? n - 1 - i : i];
    }
}
