using System.Collections;
using UnityEngine;

/// <summary>
/// Valtatien tapahtumat: ajon varrella tulee väliajoin jotain muuta kuin tasainen vihuprätkien virta.
/// Rahalinja (seteleitä suoraan tai aaltona, kerätään ajamalla läpi), saarto (kolme vihuprätkää kerralla takaa)
/// ja hiekkamyrsky (näkyvyys huononee hetkeksi). Käyttää samaa aluetta kuin EnemyBikeSpawner.
/// </summary>
public class HighwayEvents : MonoBehaviour
{
    public EnemyBikeSpawner spawner;
    [Tooltip("Aika tapahtumien välillä (s).")]
    public Vector2 gap = new Vector2(16f, 28f);
    public float firstDelay = 14f;
    public int ambushCount = 3;
    public float stormTime = 9f;

    float timer, stormLeft, bannerLeft;
    string banner = "";
    int last = -1;

    void Start() { timer = firstDelay; }

    void Update()
    {
        if (bannerLeft > 0f) bannerLeft -= Time.unscaledDeltaTime;
        if (stormLeft > 0f) stormLeft -= Time.deltaTime;
        var mb = Motorbike.Current;
        if (mb == null || mb.Rider == null || spawner == null) return;
        float x = mb.Rider.transform.position.x;
        if (x < spawner.minX + 20f || x > spawner.maxX - 80f) return;
        timer -= Time.deltaTime;
        if (timer > 0f || stormLeft > 0f) return;
        timer = Random.Range(gap.x, gap.y);
        int pick = Random.Range(0, 3);
        if (pick == last) pick = (pick + 1) % 3;   // ei sama kahdesti peräkkäin
        last = pick;
        switch (pick)
        {
            case 0: StartCoroutine(MoneyLine(mb.Rider)); break;
            case 1: StartCoroutine(Ambush()); break;
            default: stormLeft = stormTime; Banner("HIEKKAMYRSKY!"); break;
        }
    }

    void Banner(string s) { banner = s; bannerLeft = 2.2f; }

    IEnumerator MoneyLine(PlayerController pc)
    {
        var cam = Camera.main; if (cam == null) yield break;
        Banner("RAHAA TIELLÄ!");
        float halfW = cam.orthographicSize * cam.aspect;
        float x0 = cam.transform.position.x + halfW + 6f;
        bool wave = Random.value < 0.5f;
        float lane = Random.Range(pc.minDepthY + 0.5f, pc.maxDepthY - 0.5f);
        const int n = 10;
        for (int i = 0; i < n; i++)
        {
            float y = wave ? Mathf.Lerp(pc.minDepthY + 0.4f, pc.maxDepthY - 0.4f, 0.5f + 0.5f * Mathf.Sin(i * 0.75f)) : lane;
            bool last = i == n - 1;
            var p = Pickup.SpawnMoney(new Vector3(x0 + i * 2.8f, y, 0f), last ? 5 : 1, last);
            if (p != null) Destroy(p.gameObject, 25f);
        }
    }

    IEnumerator Ambush()
    {
        Banner("SAARTO!");
        for (int i = 0; i < ambushCount; i++)
        {
            spawner.SpawnOne();
            yield return new WaitForSeconds(0.9f);
        }
    }

    void OnGUI()
    {
        float H = Screen.height, W = Screen.width;
        if (stormLeft > 0f)
        {
            float a = Mathf.Clamp01(Mathf.Min(stormLeft, stormTime - stormLeft) / 1.5f);
            GUI.color = new Color(0.82f, 0.68f, 0.42f, 0.42f * a);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = new Color(0.95f, 0.85f, 0.6f, 0.5f * a);
            float t = Time.time;
            for (int i = 0; i < 60; i++)
            {
                float sp = 900f + (i * 137 % 600), y = ((i * 7919) % 1000) / 1000f * H;
                float x = W - ((t * sp + i * 311f) % (W + 400f)) + 200f;
                GUI.DrawTexture(new Rect(x, y, 80f + (i * 53 % 160), 2f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
        if (bannerLeft > 0f)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.07f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            st.normal.textColor = new Color(1f, 0.9f, 0.3f, Mathf.Clamp01(bannerLeft / 0.5f));
            GUI.Label(new Rect(0, H * 0.18f, W, H * 0.12f), Loc.T(banner), st);
        }
    }
}
