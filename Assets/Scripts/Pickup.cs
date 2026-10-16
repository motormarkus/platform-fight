using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maahan pudonnut raha tai energiajuoma. Seteli (1 mk) on tavallinen, setelitukku (50 mk) harvinainen.
/// Energiajuoma (laatikoista) palauttaa staminaa ja vähän terveyttä.
/// Kuvat: Assets/Resources/Pickups/seteli.png, setelitukku.png ja energiajuoma.png (puuttuessa piirretään kolikko / tölkki).
/// Kerätään kävelemällä päälle.
/// </summary>
public class Pickup : MonoBehaviour
{
    public int value = 1;
    public bool rare;
    [Tooltip("Energiajuoma: staminaa takaisin (0 = raha).")]
    public int stamina;
    [Tooltip("Energiajuoma: myös terveyttä takaisin.")]
    public int health = 20;
    [Tooltip("Ruoka: energiaa takaisin (0 = ei ruoka). Kuva arvotaan Resources/Pickups/ruoka_*.png -tiedostoista.")]
    public int healAmount;
    [Tooltip("Ruoka: energiaa takaisin prosenttina palkin pituudesta (ohittaa healAmountin).")]
    public int healPercent;
    [Tooltip("Ruoka: staminaa takaisin.")]
    public int staminaAmount;
    [Tooltip("Ruoka: täyttää energian ja staminan kokonaan (pizza).")]
    public bool fullRestore;
    [Tooltip("Voimakuvake: pidentää energiapalkkia näin paljon (ja täyttää sen verran). Kuva Resources/Pickups/voima_*.png.")]
    public int maxBoost;
    [Tooltip("Tietty kuva (tiedoston nimi ilman .png), tyhjä = arvotaan.")]
    public string spriteName = "";
    [Tooltip("Kuinka läheltä raha kerätään (x ja syvyys).")]
    public float pickRadiusX = 0.8f, pickRadiusY = 0.45f;

    SpriteRenderer sr;
    PlayerController pc;
    float visScale = 1f;
    float t, pop;
    bool isCoin;
    static Sprite note, stack, coin, drink, canFallback;
    static AudioClip drinkSound;
    static bool loaded;
    /// Irtonaiset ruoat ja voimakuvakkeet (maassa, vihujen pudottamina tai laatikoista): pois käytöstä, ruoka ostetaan Sohvin kaupasta.
    public static bool LooseFood = false;

    // ---- ruoka ja voimakuvakkeet: kuvat Resources/Pickups/ruoka_<nimi>[_<maara>].png ja voima_<nimi>[_<maara>].png ----
    // Tiedoston nimen lopussa oleva luku on energian maara (ruoka) tai palkin pidennys (voimakuvake); ilman lukua 20.
    // Kuvan ei tarvitse olla Sprite-tuonnilla: tekstuurista tehdaan tarvittaessa sprite ajon aikana. Kuva skaalataan sopivan kokoiseksi.
    struct Item { public Sprite sprite; public int amount, pct, stamina; public bool full; public string name; }
    public bool IsFood => healAmount > 0 || healPercent > 0 || fullRestore;
    /// Tunnetut ruoat (avain = osa tiedoston nimestä): energia prosentteina palkista, stamina ja täysi lataus. Kauppa käyttää samoja.
    public static bool FoodStats(string key, out int pct, out int stamina, out bool full)
    {
        pct = 0; stamina = 0; full = false;
        if (key.Contains("hotdog")) { pct = 25; stamina = 10; return true; }
        if (key.Contains("lihis")) { pct = 35; stamina = 15; return true; }
        if (key.Contains("pizza")) { full = true; pct = 100; stamina = 9999; return true; }
        return false;
    }
    /// Ruoan kuva avaimella (hotdog, lihis, pizza).
    public static Sprite FoodSprite(string key)
    {
        LoadItems();
        foreach (var it in foods) if (it.name.Contains(key)) return it.sprite;
        return null;
    }
    // tunnetut ruoat: nimi -> (energia, stamina, täysi). Muille: energia tiedoston nimen lopun luvusta (oletus 20), stamina puolet siitä.
    static void KnownFood(string name, ref int hp, ref int st, ref bool full) { }
    static List<Item> foods, powers;

    static void LoadItems()
    {
        if (foods != null) return;
        foods = new List<Item>(); powers = new List<Item>();
        foreach (var tex in Resources.LoadAll<Texture2D>("Pickups"))
        {
            bool food = tex.name.StartsWith("ruoka_"), power = tex.name.StartsWith("voima_");
            if (!food && !power) continue;
            var sp = Resources.Load<Sprite>("Pickups/" + tex.name);
            if (sp == null) sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
            int amount = 20;
            int us = tex.name.LastIndexOf('_');
            if (us > 0 && int.TryParse(tex.name.Substring(us + 1), out int n) && n > 0) amount = n;
            int stam = food ? amount / 2 : 0, pct = 0; bool full = false;
            if (food && FoodStats(tex.name, out int kp, out int ks, out bool kf)) { pct = kp; stam = ks; full = kf; }
            var it = new Item { sprite = sp, amount = amount, pct = pct, stamina = stam, full = full, name = tex.name };
            (food ? foods : powers).Add(it);
        }
    }

    /// Pudotettu esine pidetään alueella, jonne pelaaja pääsee (ei seinän sisään eikä ruudun reunan taakse): pelaajan x-raja on 1,3 yksikköä ruudun reunasta.
    static Vector3 ClampToReach(Vector3 pos)
    {
        var pc = Players.Primary;
        var cf = CameraFollow.Instance; var cam = Camera.main;
        if (cf != null && cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect - 1.3f - 0.5f;
            if (pos.x >= cf.minX - halfW - 3f && pos.x <= cf.maxX + halfW + 3f)   // vain pelaajan nykyisellä alueella
            {
                pos.x = Mathf.Clamp(pos.x, cf.minX - halfW, cf.maxX + halfW);
                var ar = Area.Current;
                if (ar != null && ar.walkMaxX != 0f) pos.x = Mathf.Min(pos.x, ar.walkMaxX - 0.5f);
                if (ar != null && ar.walkMinX != 0f) pos.x = Mathf.Max(pos.x, ar.walkMinX + 0.5f);
            }
        }
        if (pc != null && Mathf.Abs(pos.x - pc.transform.position.x) < 40f)
            pos.y = Mathf.Clamp(pos.y, pc.minDepthY, Mathf.Max(pc.minDepthY, Area.MaxDepthAt(pos.x, pc.maxDepthY) - 0.1f));
        return pos;
    }

    /// Ruoka maahan: big = isompi annos (yli 20 energiaa), muuten pieni. Kuva arvotaan.
    public static void SpawnFood(Vector3 pos, bool big = false, string spriteName = "")
    {
        if (!LooseFood && spriteName != "pizza") return;   // ainoa poikkeus: pomojen ja rekkamiehen pizza
        pos = ClampToReach(pos);
        LoadItems();
        var go = new GameObject("Ruoka");
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.value = 0; p.spriteName = spriteName;
        p.PickFood(big);
        p.SetSprite();
    }

    /// Voimakuvake maahan: pidentaa energiapalkkia pysyvasti.
    public static void SpawnPowerUp(Vector3 pos, string spriteName = "")
    {
        if (!LooseFood) return;
        pos = ClampToReach(pos);
        LoadItems();
        var go = new GameObject("Voimakuvake");
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.value = 0; p.spriteName = spriteName;
        p.PickPower();
        p.SetSprite();
    }

    /// Vihun kuolema: tavallisesta vihusta pieni todennakoisyys ruokaan, pomosta aina voimakuvake ja iso annos.
    public static void DropFromEnemy(Vector3 pos, bool boss, string enemyName = "")
    {
        if (boss)
        {
            SpawnPowerUp(pos + new Vector3(-0.5f, -0.05f, 0f));
            SpawnFood(pos + new Vector3(0.6f, -0.05f, 0f), true, "pizza");   // pomo pudottaa aina pizzan (täysi lataus) ja voimakuvakkeen
            return;
        }
        if (enemyName == "Rekkamies") { SpawnFood(pos + new Vector3(0.4f, -0.05f, 0f), true, "pizza"); return; }   // rekkamies pudottaa aina pizzan
        float r = Random.value;
        if (r < 0.16f) SpawnFood(pos + new Vector3(0f, -0.05f, 0f), r < 0.04f);   // 4 % iso, 12 % pieni
    }

    void PickFood(bool big)
    {
        LoadItems();
        if (foods.Count == 0) { healAmount = big ? 25 : 15; staminaAmount = big ? 15 : 10; return; }
        if (!string.IsNullOrEmpty(spriteName)) { foreach (var it in foods) if (it.name == "ruoka_" + spriteName || it.name == spriteName) { Apply(it); return; } }
        // satunnaisesti arvottavista pizza (täysi lataus) on pois: sen pudottavat vain rekkamies ja pomot
        var pool = new List<Item>();
        foreach (var it in foods) if (!it.full && (big ? Mathf.Max(it.pct, it.amount) >= 30 : Mathf.Max(it.pct, it.amount) < 30)) pool.Add(it);
        if (pool.Count == 0) foreach (var it in foods) if (!it.full) pool.Add(it);
        if (pool.Count == 0) pool = foods;
        Apply(pool[Random.Range(0, pool.Count)]);
    }

    void Apply(Item it) { spriteName = it.name; healAmount = it.pct > 0 ? 0 : it.amount; healPercent = it.pct; staminaAmount = it.stamina; fullRestore = it.full; }

    void PickPower()
    {
        LoadItems();
        if (powers.Count == 0) { maxBoost = 25; return; }
        foreach (var it in powers) if (!string.IsNullOrEmpty(spriteName) && (it.name == spriteName || it.name == "voima_" + spriteName)) { maxBoost = it.amount; return; }
        var pick = powers[Random.Range(0, powers.Count)];
        spriteName = pick.name; maxBoost = pick.amount;
    }

    static Sprite ItemSprite(string name, bool power)
    {
        LoadItems();
        foreach (var it in power ? powers : foods) if (it.name == name) return it.sprite;
        return null;
    }

    public static void SpawnMoney(Vector3 pos, int value, bool rare)
    {
        if (value <= 0) return;
        pos = ClampToReach(pos);
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
        pos = ClampToReach(pos);
        var go = new GameObject("Energiajuoma");
        go.transform.position = pos;
        var p = go.AddComponent<Pickup>();
        p.stamina = stamina;
        p.value = 0;
        p.SetSprite();
    }

    void Awake()
    {
        if (!LooseFood && (IsFood || maxBoost > 0)) { Destroy(gameObject); return; }   // sceneen aiemmin sijoitetut ruoat ja voimakuvakkeet
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
            drinkSound = Resources.Load<AudioClip>("Pickups/energiajuoma_aani");
            loaded = true;
        }
        if (maxBoost > 0 || IsFood)
        {
            isCoin = false;
            if (maxBoost > 0 && string.IsNullOrEmpty(spriteName)) PickPower();
            if (IsFood && string.IsNullOrEmpty(spriteName)) PickFood(healPercent >= 30 || healAmount >= 25);
            var sp = ItemSprite(spriteName, maxBoost > 0);
            sr.sprite = sp != null ? sp : (maxBoost > 0 ? CoinSprite() : CanSprite());
            // sopiva koko: ruoka n. 0,75 yksikkoa korkea, voimakuvake n. 1,0
            float h = sr.sprite != null ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) : 1f;
            visScale = h > 0.01f ? (maxBoost > 0 ? 1.0f : (fullRestore ? 1.15f : 0.9f)) / h : 1f;
            sr.transform.localScale = Vector3.one * visScale;
            return;
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
        else if (stamina <= 0 && !IsFood && maxBoost <= 0)
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f) * (rare ? 3f : 7f));   // seteli lepattaa
        // voimakuvake sykkii
        if (maxBoost > 0) sr.transform.localScale = Vector3.one * visScale * (1f + 0.07f * Mathf.Sin(t * 5f));
        // harvinainen tukku kimaltaa
        if (rare)
        {
            float g = 0.85f + 0.3f * Mathf.Abs(Mathf.Sin(t * 4f));
            sr.color = new Color(g, g, g * 0.9f);
        }
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 1;

        if (pop > 0.3f) return;
        pc = Players.Nearest(transform.position);
        if (pc == null || pc.GameOver) return;
        Vector3 p = pc.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) <= pickRadiusX && Mathf.Abs(p.y - me.y) <= pickRadiusY && pc.AirHeight < 0.5f)
        {
            if (maxBoost > 0)
            {
                pc.AddMaxHealth(maxBoost);
                if (drinkSound != null) HitFx.PlayClip(drinkSound, 1f); else HitFx.PlayPickup(true);
                GameHUD.Popup("+" + maxBoost + " ENERGIAPALKKI", me + Vector3.up * 1.4f, new Color(1f, 0.85f, 0.2f));
                Destroy(gameObject);
                return;
            }
            if (IsFood)
            {
                if (pc.health >= pc.maxHealth && pc.stamina >= pc.maxStamina - 0.5f) return;   // täysillä ruokaa ei kerätä (jää odottamaan)
                // tunnettu ruoka (hodari, lihis, pizza): hero syö sen omalla animaatiollaan, jos on vapaana
                string foodKey = spriteName.Contains("hotdog") ? "hotdog" : spriteName.Contains("lihis") ? "lihis" : spriteName.Contains("pizza") ? "pizza" : "";
                if (foodKey != "" && pc.EatFood(foodKey, me + Vector3.up * 1.3f)) { Destroy(gameObject); return; }
                int gotHp = pc.Heal(fullRestore ? pc.maxHealth : healPercent > 0 ? Mathf.RoundToInt(pc.maxHealth * healPercent / 100f) : healAmount);
                int gotSt = pc.AddStamina(staminaAmount);
                if (drinkSound != null) HitFx.PlayClip(drinkSound, fullRestore ? 1f : 0.7f); else HitFx.PlayPickup(fullRestore);
                if (fullRestore) GameHUD.Popup("TÄYSI LATAUS!", me + Vector3.up * 1.4f, new Color(1f, 0.95f, 0.4f));
                else
                {
                    if (gotHp > 0) GameHUD.Popup("+" + gotHp + " ENERGIA", me + Vector3.up * 1.2f, new Color(0.4f, 1f, 0.45f));
                    if (gotSt > 0) GameHUD.Popup("+" + gotSt + " STAMINA", me + Vector3.up * 1.7f, new Color(0.35f, 0.7f, 1f));
                }
                Destroy(gameObject);
                return;
            }
            if (stamina > 0)
            {
                int got = pc.AddStamina(stamina);
                int hp = health > 0 ? pc.Heal(health) : 0;
                if (drinkSound != null) HitFx.PlayClip(drinkSound, 0.9f); else HitFx.PlayPickup(false);
                GameHUD.Popup("+" + got + " STAMINA", me + Vector3.up * 1.2f, new Color(0.35f, 0.7f, 1f));
                if (hp > 0) GameHUD.Popup("+" + hp + " HP", me + Vector3.up * 1.7f, new Color(0.4f, 1f, 0.45f));
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
