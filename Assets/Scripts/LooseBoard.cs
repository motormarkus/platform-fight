using System.Collections.Generic;
using UnityEngine;

/// <summary>Skettarin irronnut rullalauta: jatkaa matkaa, hidastuu ja jää maahan. Häipyy, kun omistaja on poissa.</summary>
public class LooseBoard : MonoBehaviour
{
    SpriteRenderer sr, shadow;
    float vx, t, wobble;
    public Enemy owner;
    /// Pelaajan pudottama lauta: ei häviä omistajan mukana.
    public bool persistent;
    float muteUntil;
    static readonly List<LooseBoard> All = new List<LooseBoard>();
    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// Estää nousun laudalle hetkeksi (juuri pudotettu lauta).
    public void Mute(float seconds) { muteUntil = Time.time + seconds; }

    /// Seisova lauta pelaajan jalkojen alla (pelaaja astuu päälle): null, jos ei ole.
    public static LooseBoard FindRideable(Vector3 pos)
    {
        foreach (var b in All)
        {
            if (b == null || b.fade > 0f || Time.time < b.muteUntil || b.t < 0.6f || Mathf.Abs(b.vx) > 0.6f) continue;
            if (Mathf.Abs(b.transform.position.x - pos.x) < 1.5f && Mathf.Abs(b.transform.position.y - pos.y) < 0.8f) return b;
        }
        return null;
    }

    /// Pelaaja nousi laudalle: lauta häviää (kuvat sisältävät laudan).
    public void Take() { Destroy(gameObject); }

    public static LooseBoard Spawn(Sprite sprite, Vector3 pos, float vx, Enemy owner)
    {
        var go = new GameObject("Rullalauta");
        go.transform.position = pos;
        var b = go.AddComponent<LooseBoard>();
        b.vx = vx; b.owner = owner;
        var v = new GameObject("Visual"); v.transform.SetParent(go.transform, false);
        b.sr = v.AddComponent<SpriteRenderer>(); b.sr.sprite = sprite;
        b.sr.flipX = vx < 0f;
        var s = new GameObject("Shadow"); s.transform.SetParent(go.transform, false);
        b.shadow = s.AddComponent<SpriteRenderer>(); b.shadow.sprite = PlayerController.CreateShadowSprite();
        b.shadow.color = new Color(0f, 0f, 0f, 0.35f);
        b.shadow.transform.localScale = new Vector3(1.4f, 0.3f, 1f);
        b.wobble = 0.35f;
        return b;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        Vector3 p = transform.position;
        p.x += vx * dt;
        vx = Mathf.MoveTowards(vx, 0f, 3.5f * dt);   // rullaa ja hidastuu
        // ei seinien läpi eikä ruudun ohi: sama alue kuin hahmoilla, törmätessä vauhti loppuu
        float before = p.x;
        var cf = CameraFollow.Instance; var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - 0.6f;
            p.x = Mathf.Clamp(p.x, cf.minX - halfW, cf.maxX + halfW);
            if (Area.Current != null && Area.Current.walkMaxX != 0f) p.x = Mathf.Min(p.x, Area.Current.walkMaxX);
            if (Area.Current != null && Area.Current.walkMinX != 0f) p.x = Mathf.Max(p.x, Area.Current.walkMinX);
        }
        if (!Mathf.Approximately(p.x, before)) vx = 0f;
        transform.position = p;
        // irtoaa pienellä pompulla ja keikahduksella
        if (wobble > 0f) wobble -= dt;
        float hop = wobble > 0f ? Mathf.Sin((0.35f - wobble) / 0.35f * Mathf.PI) * 0.25f : 0f;
        sr.transform.localPosition = new Vector3(0f, hop, 0f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, wobble > 0f ? Mathf.Sin(wobble * 40f) * 8f : 0f);
        int order = Mathf.RoundToInt(-p.y * 100f);
        sr.sortingOrder = order; shadow.sortingOrder = order - 1;
        if (persistent && t > 150f && Mathf.Abs(vx) < 0.1f) Destroy(gameObject);   // vanha, käyttämätön lauta siivotaan pois
        // omistaja kuollut tai poissa: häipyy hetken päästä
        if (!persistent && (owner == null || owner.IsDead))
        {
            fade += dt;
            float a = Mathf.Clamp01(1f - (fade - 2f));
            sr.color = new Color(1f, 1f, 1f, a); shadow.color = new Color(0f, 0f, 0f, 0.35f * a);
            if (fade > 3f) Destroy(gameObject);
        }
    }
    float fade;
}
