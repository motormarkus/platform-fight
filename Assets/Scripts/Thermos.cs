using UnityEngine;

/// <summary>
/// Termospullo (Böönin kahvia): putoaa harvinaisena hajonneesta laatikosta tai pöydästä. Hero kävelee päälle,
/// ryyppää (Rocco: juontianimaatio) ja saa pärinän: liikkeet nopeutuvat, vihut hidastuvat, stamina ei kulu.
/// Kuva: Resources/Termari/termospullo.png.
/// </summary>
public class Thermos : MonoBehaviour
{
    public float pickRadiusX = 0.8f, pickRadiusY = 0.45f;
    SpriteRenderer sr;
    PlayerController pc;
    float t, pop = 1f;
    static Sprite sprite;

    public static void Spawn(Vector3 pos)
    {
        var go = new GameObject("Termospullo");
        go.transform.position = pos;
        go.AddComponent<Thermos>();
    }

    void Awake()
    {
        if (sprite == null) sprite = Resources.Load<Sprite>("Termari/termospullo");
        var vis = new GameObject("Visual");
        vis.transform.SetParent(transform, false);
        sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        vis.transform.localScale = Vector3.one * 0.6f;
        t = Random.value * 10f;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (pop > 0f) pop = Mathf.Max(0f, pop - Time.deltaTime * 2.5f);
        float arc = pop > 0f ? Mathf.Sin((1f - pop) * Mathf.PI) * 0.9f : 0f;
        sr.transform.localPosition = new Vector3(0f, 0.02f + arc + Mathf.Sin(t * 3f) * 0.03f, 0f);
        // kimaltaa, ettei jää huomaamatta
        float g = 0.9f + 0.2f * Mathf.Abs(Mathf.Sin(t * 3.5f));
        sr.color = new Color(g, g, g);
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 1;
        if (pop > 0.3f) return;
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || pc.GameOver || !pc.CanDrinkThermos) return;
        Vector3 p = pc.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) <= pickRadiusX && Mathf.Abs(p.y - me.y) <= pickRadiusY && pc.AirHeight < 0.5f)
        {
            pc.DrinkThermos();
            Destroy(gameObject);
        }
    }
}
