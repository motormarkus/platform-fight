using UnityEngine;

/// <summary>Pölypilvi maahan iskeytyessä: muutama laajeneva ja haihtuva läiskä kumpaankin suuntaan.</summary>
public class DustPuff : MonoBehaviour
{
    const float Life = 0.55f;
    SpriteRenderer[] puffs;
    Vector2[] vel;
    float t, size;

    public static void Spawn(Vector3 pos, int sortingOrder, float size = 1f)
    {
        var go = new GameObject("Pöly");
        go.transform.position = pos;
        var d = go.AddComponent<DustPuff>();
        d.size = size;
        d.Init(sortingOrder);
    }

    void Init(int order)
    {
        const int n = 7;
        puffs = new SpriteRenderer[n];
        vel = new Vector2[n];
        var spr = PlayerController.CreateShadowSprite();   // pehmeä soikio
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject("Läiskä");
            g.transform.SetParent(transform, false);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.sortingOrder = order;
            float side = i % 2 == 0 ? 1f : -1f;
            vel[i] = new Vector2(side * Random.Range(1.2f, 3.2f), Random.Range(0.3f, 1.4f)) * size;
            g.transform.localPosition = new Vector3(side * Random.Range(0f, 0.5f), Random.Range(0f, 0.2f), 0f);
            puffs[i] = sr;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        float k = t / Life;
        for (int i = 0; i < puffs.Length; i++)
        {
            var tr = puffs[i].transform;
            tr.localPosition += (Vector3)(vel[i] * dt);
            vel[i] *= 1f - 4f * dt;   // hidastuu nopeasti
            float s = Mathf.Lerp(0.5f, 1.4f, k) * size;
            tr.localScale = new Vector3(s, s * 0.6f, 1f);
            puffs[i].color = new Color(0.72f, 0.66f, 0.58f, (1f - k) * 0.75f);
        }
        if (t >= Life) Destroy(gameObject);
    }
}
