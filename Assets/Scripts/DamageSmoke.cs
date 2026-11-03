using System.Collections.Generic;
using UnityEngine;

/// <summary>Savua vaurioituneen vihollisen rinnasta (Enemy.Damaged: elämät alle rajan, esim. Kuurobotti alle 30 %). Pehmeät harmaat pöllähdykset nousevat ylös ja haalistuvat; satunnainen kipinä sekä sinivalkoisia sähkökaaria, jotka välähtelevät rungon ympärillä.</summary>
[RequireComponent(typeof(Enemy))]
public class DamageSmoke : MonoBehaviour
{
    class Puff { public GameObject go; public SpriteRenderer sr; public Vector3 v; public float age, life, s0, s1; public bool spark; }
    static Sprite softSpr;
    readonly List<Puff> puffs = new List<Puff>();
    Enemy e; float t;

    class Arc { public LineRenderer lr; public float age, life, flick; public Vector3 from, off, dir; public float len; }
    static Material arcMat;
    readonly List<Arc> arcs = new List<Arc>();
    float arcTimer;

    void Awake() { e = GetComponent<Enemy>(); }

    static Sprite Soft()
    {
        if (softSpr != null) return softSpr;
        const int N = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
        }
        tex.Apply();
        return softSpr = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 32f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool active = e != null && e.Damaged && !e.IsDead && e.body != null && e.body.sprite != null;
        if (active) { Emit(dt); EmitArcs(dt); }
        for (int i = arcs.Count - 1; i >= 0; i--)
        {
            var a = arcs[i];
            a.age += dt;
            if (a.lr == null || a.age >= a.life) { if (a.lr != null) Destroy(a.lr.gameObject); arcs.RemoveAt(i); continue; }
            a.flick -= dt;
            if (a.flick <= 0f) { a.flick = 0.035f; a.from = transform.position + a.off; Zigzag(a); }   // seuraa robottia, kun se liikkuu
            float k = a.age / a.life; float al = 1f - k * k;
            a.lr.startColor = new Color(0.6f, 0.9f, 1f, al); a.lr.endColor = new Color(1f, 1f, 1f, al * 0.6f);
        }
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var p = puffs[i];
            p.age += dt;
            if (p.go == null || p.age >= p.life) { if (p.go != null) Destroy(p.go); puffs.RemoveAt(i); continue; }
            float k = p.age / p.life;
            p.go.transform.position += p.v * dt;
            p.go.transform.localScale = Vector3.one * Mathf.Lerp(p.s0, p.s1, k);
            if (p.spark) p.sr.color = new Color(1f, Mathf.Lerp(0.8f, 0.3f, k), 0.1f, 1f - k);
            else { float g = Mathf.Lerp(0.18f, 0.55f, k); p.sr.color = new Color(g, g, g, (1f - k) * 0.65f); }
        }
    }

    void Emit(float dt)
    {
        t += dt;
        var b = e.body.bounds;
        while (t >= 0.07f)
        {
            t -= 0.07f;
            var go = new GameObject("Savu");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Soft(); sr.sortingLayerID = e.body.sortingLayerID; sr.sortingOrder = e.body.sortingOrder + 1;
            go.transform.position = new Vector3(b.center.x + Random.Range(-0.25f, 0.25f) * b.size.x * 0.4f, Mathf.Lerp(b.min.y, b.max.y, 0.62f), transform.position.z);
            bool spark = Random.value < 0.08f;
            puffs.Add(new Puff { go = go, sr = sr, spark = spark, v = new Vector3(Random.Range(-0.3f, 0.3f), spark ? Random.Range(1.5f, 2.5f) : Random.Range(0.9f, 1.5f), 0f), life = spark ? Random.Range(0.25f, 0.45f) : Random.Range(0.8f, 1.3f), s0 = spark ? 0.08f : Random.Range(0.2f, 0.3f), s1 = spark ? 0.03f : Random.Range(0.7f, 1.0f) });
        }
    }

    /// Sähkökaari: lyhyt sahalaitainen viiva rungosta ulospäin, joka välähtelee muutaman kymmenesosasekunnin.
    void EmitArcs(float dt)
    {
        arcTimer -= dt;
        if (arcTimer > 0f) return;
        arcTimer = Random.Range(0.15f, 0.55f);
        var b = e.body.bounds;
        var go = new GameObject("Sahko");
        var lr = go.AddComponent<LineRenderer>();
        if (arcMat == null) arcMat = new Material(Shader.Find("Sprites/Default"));
        lr.sharedMaterial = arcMat; lr.useWorldSpace = true; lr.numCapVertices = 2; lr.numCornerVertices = 1;
        lr.widthMultiplier = Random.Range(0.035f, 0.07f);
        lr.sortingLayerID = e.body.sortingLayerID; lr.sortingOrder = e.body.sortingOrder + 2;
        float ang = Random.Range(0f, Mathf.PI * 2f);
        var a = new Arc { lr = lr, life = Random.Range(0.12f, 0.3f), len = Random.Range(0.25f, 0.6f) * Mathf.Max(0.6f, b.size.y * 0.3f), dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) };
        a.from = new Vector3(b.center.x + Random.Range(-0.2f, 0.2f) * b.size.x, Mathf.Lerp(b.min.y, b.max.y, Random.Range(0.3f, 0.75f)), transform.position.z); a.off = a.from - transform.position;   // alkaa rungon keskeltä, lyhyt ja kiinni robotissa
        arcs.Add(a); Zigzag(a);
    }

    static void Zigzag(Arc a)
    {
        const int N = 7;
        a.lr.positionCount = N;
        var perp = new Vector3(-a.dir.y, a.dir.x, 0f);
        for (int i = 0; i < N; i++)
        {
            float f = i / (float)(N - 1);
            float jitter = i == 0 ? 0f : Random.Range(-0.12f, 0.12f) * a.len;
            a.lr.SetPosition(i, a.from + a.dir * (a.len * f) + perp * jitter);
        }
    }

    void OnDestroy()
    {
        foreach (var p in puffs) if (p.go != null) Destroy(p.go);
        foreach (var a in arcs) if (a.lr != null) Destroy(a.lr.gameObject);
    }
}
