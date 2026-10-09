using UnityEngine;

/// <summary>Valtatie: lähettää vihuprätkiä takaa, kun pelaaja ajaa alueella minX…maxX.</summary>
public class EnemyBikeSpawner : MonoBehaviour
{
    public EnemyBike template;
    public float minX, maxX;
    public Vector2 interval = new Vector2(8f, 15f);
    public int maxAlive = 2;
    [Tooltip("Skeittari roikkuu osan vihuprätkien perässä (malli, piilossa).")]
    public SkaterHitch skaterTemplate;
    [Range(0f, 1f)] public float skaterChance = 0.4f;
    [Tooltip("Määrän kerroin: tiheämmin (väli / kerroin) ja useampi kerralla; skeittareita enemmän.")]
    public float density = 5f;
    float timer = 4f;

    void Update()
    {
        var mb = Motorbike.Current;
        var cam = Camera.main;
        if (mb == null || template == null || cam == null) return;
        float x = mb.transform.position.x;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        x = pc.transform.position.x;
        if (x < minX || x > maxX - 40f) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        float dn = Mathf.Max(1f, density * 0.7f);   // 30 % vähemmän kuin density sanoo (5 -> 3,5)
        timer = Random.Range(interval.x, interval.y) / dn;
        if (FindObjectsByType<EnemyBike>(FindObjectsSortMode.None).Length >= Mathf.RoundToInt(maxAlive * Mathf.Min(dn, 3f))) return;
        float halfW = cam.orthographicSize * cam.aspect;
        var go = Instantiate(template.gameObject);
        go.SetActive(true);
        float y = Random.Range(pc.minDepthY + 0.2f, pc.maxDepthY - 0.2f);
        go.transform.position = new Vector3(cam.transform.position.x - halfW - 3f, y, 0f);
        if (skaterTemplate != null && Random.value < Mathf.Min(1f, skaterChance * Mathf.Min(dn, 2f)))
        {
            var sk = Instantiate(skaterTemplate.gameObject);
            var h = sk.GetComponent<SkaterHitch>();
            h.tower = go.GetComponent<EnemyBike>();
            sk.transform.position = go.transform.position + new Vector3(-3f, 0f, 0f);
            sk.SetActive(true);
        }
    }
}
