using UnityEngine;

/// <summary>Skettarin irronnut rullalauta: jatkaa matkaa, hidastuu ja jää maahan. Häipyy, kun omistaja on poissa.</summary>
public class LooseBoard : MonoBehaviour
{
    SpriteRenderer sr, shadow;
    float vx, t, wobble;
    public Enemy owner;

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
        transform.position = p;
        // irtoaa pienellä pompulla ja keikahduksella
        if (wobble > 0f) wobble -= dt;
        float hop = wobble > 0f ? Mathf.Sin((0.35f - wobble) / 0.35f * Mathf.PI) * 0.25f : 0f;
        sr.transform.localPosition = new Vector3(0f, hop, 0f);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, wobble > 0f ? Mathf.Sin(wobble * 40f) * 8f : 0f);
        int order = Mathf.RoundToInt(-p.y * 100f);
        sr.sortingOrder = order; shadow.sortingOrder = order - 1;
        // omistaja kuollut tai poissa: häipyy hetken päästä
        if (owner == null || owner.IsDead)
        {
            fade += dt;
            float a = Mathf.Clamp01(1f - (fade - 2f));
            sr.color = new Color(1f, 1f, 1f, a); shadow.color = new Color(0f, 0f, 0f, 0.35f * a);
            if (fade > 3f) Destroy(gameObject);
        }
    }
    float fade;
}
