using UnityEngine;

/// <summary>
/// Maahan pudonnut raha tai energiajuoma. Seteli (1 mk) on tavallinen, setelitukku (50 mk) harvinainen.
/// Energiajuoma (laatikoista) palauttaa staminaa.
/// Kuvat: Assets/Resources/Pickups/seteli.png, setelitukku.png ja energiajuoma.png (puuttuessa piirretään kolikko / tölkki).
/// Kerätään kävelemällä päälle.
/// </summary>
public class Pickup : MonoBehaviour
{
    public int value = 1;
    public bool rare;
    [Tooltip("Energiajuoma: staminaa takaisin (0 = raha).")]
    public int stamina;
    [Tooltip("Kuinka läheltä raha kerätään (x ja syvyys).")]
    public float pickRadiusX = 0.8f, pickRadiusY = 0.45f;

    SpriteRenderer sr;
    PlayerController pc;
    float t, pop;
    bool isCoin;
    static Sprite note, stack, coin, drink, canFallback;
    static bool loaded;

    public static void SpawnMoney(Vector3 pos, int value, bool rare)
    {
        if (value <= 0) return;
        var go = new GameObject((rare ? "Setelitukku " : "Seteli ") + value + " mk");
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.value = value;
        p.rare = rare;
        p.SetSprite();
    }

    /// Energiajuoma maahan (esim. hajonneesta laatikosta).
    public static void SpawnEnergy(Vector3 pos, int stamina)
    {
        if (stamina <= 0) return;
        var go = new GameObject("Energiajuoma");
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.stamina = stamina;
        p.value = 0;
        p.SetSprite();
    }

    void Awake()
    {
        var vis = new GameObject("Visual");
        vis.transform.SetParent(transform, false);
        sr = vis.AddComponent<SpriteRenderer>();
        pop = 1f;
        t = Random.value * 10f;
        SetSprite();
    }

    void SetSprite()
    {
        if (!loaded)
        {
            note = Resources.Load<Sprite>("Pickups/seteli");
            stack = Resources.Load<Sprite>("Pickups/setelitukku");
            drink = Resources.Load<Sprite>("Pickups/energiajuoma");
            loaded = true;
        }
        if (stamina > 0) { isCoin = false; sr.sprite = drink != null ? drink : CanSprite(); return; }
        Sprite s = rare ? stack : note;
        isCoin = s == null;
        sr.sprite = isCoin ? CoinSprite() : s;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (pop > 0f) pop = Mathf.Max(0f, pop - Time.deltaTime * 2.5f);

        // putoaa pienellä kaarella, sitten lepää maassa ja keinuu kevyesti
        float arc = pop > 0f ? Mathf.Sin((1f - pop) * Mathf.PI) * 0.9f : 0f;
        float bob = Mathf.Sin(t * 3f) * 0.03f;
        sr.transform.localPosition = new Vector3(0f, 0.04f + arc + bob, 0f);
        if (isCoin)
            sr.transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 3f)) * 0.8f + 0.2f, 1f, 1f);
        else if (stamina <= 0)
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * (rare ? 3f : 7f));   // seteli lepattaa
        // harvinainen tukku kimaltaa
        if (rare)
        {
            float g = 0.85f + 0.3f * Mathf.Abs(Mathf.Sin(t * 4f));
            sr.color = new Color(g, g, g * 0.9f);
        }
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 1;

        if (pop > 0.3f) return;
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        if (pc == null || pc.GameOver) return;
        Vector3 p = pc.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) <= pickRadiusX && Mathf.Abs(p.y - me.y) <= pickRadiusY && pc.AirHeight < 0.5f)
        {
            if (stamina > 0)
            {
                int got = pc.AddStamina(stamina);
                HitFx.PlayPickup(false);
                GameHUD.Popup("+" + got + " STAMINA", me + Vector3.up * 1.2f, new Color(0.35f, 0.7f, 1f));
                Destroy(gameObject);
                return;
            }
            pc.AddMoney(value);
            HitFx.PlayPickup(rare);
            GameHUD.Popup("+" + value + " mk", me + Vector3.up * 1.2f,
                rare ? new Color(0.5f, 1f, 0.4f) : new Color(1f, 0.85f, 0.2f));
            Destroy(gameObject);
        }
    }

    /// Varakuva: sininen energiajuomatölkki piirrettynä koodilla (kunnes energiajuoma.png on tehty).
    static Sprite CanSprite()
    {
        if (canFallback != null) return canFallback;
        const int W = 22, H = 40;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool edge = x == 0 || x == W - 1 || y == 0 || y == H - 1;
                float shade = 0.75f + 0.45f * Mathf.Sin(x / (float)(W - 1) * Mathf.PI);
                Color col = new Color(0.1f, 0.35f, 0.95f) * shade;
                if (y >= H - 4 || y < 3) col = new Color(0.8f, 0.82f, 0.86f) * shade;      // metallinen kansi ja pohja
                else if (y > 14 && y < 24) col = new Color(0.95f, 0.95f, 1f) * shade;       // valkoinen raita
                if (edge) col = new Color(0.05f, 0.05f, 0.1f);
                col.a = 1f;
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        canFallback = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f);
        return canFallback;
    }

    /// Varakuva: kolikko piirrettynä koodilla.
    static Sprite CoinSprite()
    {
        if (coin != null) return coin;
        const int S = 40;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var c = new Vector2(S / 2f - 0.5f, S / 2f - 0.5f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (S / 2f);
                Color col;
                if (d > 1f) col = Color.clear;
                else if (d > 0.86f) col = new Color(0.55f, 0.36f, 0.05f);
                else if (d > 0.72f) col = new Color(0.95f, 0.75f, 0.2f);
                else
                {
                    float shade = 0.85f + 0.25f * ((x - y) / (float)S);
                    col = new Color(0.98f * shade, 0.8f * shade, 0.25f * shade);
                }
                tex.SetPixel(x, y, col);
            }
        tex.Apply();
        coin = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0f), 100f);
        return coin;
    }
}
