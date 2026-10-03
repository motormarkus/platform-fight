using UnityEngine;

/// <summary>
/// Läiskä lattiaan (hajonnut pullo). Kuvat: Assets/Resources/Tahrat/*.png (yksittäisiä kuvia).
/// Jos kuvia ei ole, ei tehdä mitään. Läiskät jäävät lattialle (enintään maxStains, vanhimmat häviävät).
/// </summary>
public class BarStain : MonoBehaviour
{
    static Sprite[] sprites;
    static bool loaded;
    static readonly System.Collections.Generic.Queue<GameObject> live = new System.Collections.Generic.Queue<GameObject>();
    const int MaxStains = 40;

    public static void SpawnAt(Vector3 pos)
    {
        if (!loaded) { sprites = Resources.LoadAll<Sprite>("Tahrat"); loaded = true; }
        if (sprites == null || sprites.Length == 0) return;
        var go = new GameObject("Läiskä");
        go.transform.position = pos + new Vector3(Random.Range(-0.2f, 0.2f), -0.02f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[Random.Range(0, sprites.Length)];
        sr.flipX = Random.value < 0.5f;
        sr.sortingOrder = -8000;                 // lattian päällä, kaikkien hahmojen ja esineiden alla
        sr.color = new Color(1f, 1f, 1f, 0.9f);
        live.Enqueue(go);
        while (live.Count > MaxStains) { var o = live.Dequeue(); if (o != null) Destroy(o); }
    }
}
