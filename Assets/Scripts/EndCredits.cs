using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lopputekstit: Total Fist -logo ylimpänä, sen alla "Pääosissa / Starring" ja kuva nimen vieressä, sitten pomot, viholliset ja muut hahmot, tilastot ja kiitos.
/// Muotokuvat ovat yhdessä kuvassa Resources/Lopputekstit/muotokuvat (rakennetaan valikosta 427: Assets/Editor/CreditsPortraits.cs). Käynnistys: EndCredits.Begin() (rantabaarin ovet, Door.endCredits).
/// </summary>
public sealed class EndCredits : MonoBehaviour
{
    public struct Cast
    {
        public int group; public string fi, en, src;
        public Cast(int g, string fi, string en, string src) { group = g; this.fi = fi; this.en = en; this.src = src; }
    }
    public const int PortraitCols = 8, CellW = 192, CellH = 240;
    public static readonly string[] GroupFi = { "PÄÄOSISSA", "POMOT", "VIHOLLISET", "MUISSA ROOLEISSA" };
    public static readonly string[] GroupEn = { "STARRING", "BOSSES", "ENEMIES", "ALSO STARRING" };
    /// src = kuva (polku Assets-kansion alla; ensimmäinen kuva siitä); järjestys = kuvan paikka muotokuva-atlaksessa.
    public static readonly Cast[] Casts =
    {
        new Cast(0, "Rocco", "Rocco", "Sprites/idle.png"),
        new Cast(0, "Ruby", "Ruby", "Sprites/sankaritar_idle.png"),
        new Cast(0, "Rex", "Rex", "Sprites/Viholliset/rex_idle.png"),
        new Cast(0, "Tom", "Tom", "Sprites/Viholliset/tom_idle.png"),
        new Cast(0, "Rekkamies", "Trucker", "Sprites/Viholliset/rekkamies_calm.png"),
        new Cast(0, "Betty", "Betty", "Sprites/betty_idle.png"),
        new Cast(0, "Amatsooni", "Amazon", "Sprites/Viholliset/amatsoni_idle.png"),
        new Cast(0, "Lätkäfani", "Hockey fan", "Sprites/Viholliset/latkafani_idle.png"),

        new Cast(1, "Pierre", "Pierre", "Sprites/Viholliset/pierre_idle.png"),
        new Cast(1, "Vincent", "Vincent", "Sprites/Viholliset/rottakeisari_idle.png"),
        new Cast(1, "Metsuri", "Lumberjack", "Sprites/Viholliset/metsuri_idle.png"),
        new Cast(1, "Horhe", "Jorge", "Sprites/Viholliset/horhe_idle.png"),
        new Cast(1, "Rosvokapteeni", "Pirate chief", "Sprites/Viholliset/rosvokapteeni_idle.png"),
        new Cast(1, "Romu-ukko", "Yard chief", "Sprites/Viholliset/romuukko_idle.png"),
        new Cast(1, "Deacon", "Deacon", "Sprites/deacon_idle.png"),
        new Cast(1, "Gustav", "Gustav", "Sprites/Viholliset/gustav_idle.png"),
        new Cast(1, "Don Kurinai", "Don Kurinai", "Sprites/Viholliset/yakuzapomo_idle.png"),
        new Cast(1, "Joulupukki", "Santa", "Sprites/Joulupukki/pukki_idle.png"),
        new Cast(1, "Lätkäpomo", "Hockey boss", "Sprites/Viholliset/kapteeni_idle.png"),
        new Cast(1, "Temppelin kuningas", "Temple king", "Sprites/Viholliset/kuningas_idle.png"),
        new Cast(1, "Timppa", "Tim", "Sprites/Viholliset/timppa_idle.png"),
        new Cast(1, "Warden", "Warden", "Sprites/Viholliset/warden_idle.png"),
        new Cast(1, "Taikuri", "Magician", "Sprites/Taikuri/taikuri_kavely_00.png"),
        new Cast(1, "Nokkamies", "Foreman", "Sprites/Viholliset/nokkamies_idle.png"),
        new Cast(1, "Kaani", "Khan", "Sprites/Khan/khan_idle_00.png"),
        new Cast(1, "Robottipomo", "Roboboss", "Sprites/Viholliset/robotti_pomo_idle.png"),
        new Cast(1, "Koe-olento", "Thingy", "Sprites/Viholliset/alien_idle.png"),
        new Cast(1, "Kenraali", "General", "Sprites/Viholliset/kenraali_idle.png"),
        new Cast(1, "Lokki", "Gull", "Sprites/Viholliset/lokki_idle.png"),

        new Cast(2, "Kovis", "Thug", "Sprites/Viholliset/vihu_idle.png"),
        new Cast(2, "Punkkari", "Punker", "Sprites/Viholliset/punk_idle.png"),
        new Cast(2, "Lippis", "Capper", "Sprites/Viholliset/lippis_idle.png"),
        new Cast(2, "Skeittari", "Skater", "Sprites/Viholliset/skettari_idle.png"),
        new Cast(2, "Prätkä", "Biker", "Sprites/Viholliset/motoristi_idle.png"),
        new Cast(2, "Rokkimimmi", "Rochick", "Sprites/Viholliset/rokkari2_idle.png"),
        new Cast(2, "Portsari", "Bouncer", "Sprites/Viholliset/portsari_idle.png"),
        new Cast(2, "Puliukko", "Bigshot", "Sprites/Viholliset/puliukko_idle.png"),
        new Cast(2, "Samoalainen", "Samoan", "Sprites/Viholliset/samoa_idle.png"),
        new Cast(2, "Pensalenkkari", "Kidd", "Sprites/Viholliset/lenkkari_idle.png"),
        new Cast(2, "Merirosvo", "Pirate", "Sprites/Viholliset/merirosvo_idle.png"),
        new Cast(2, "Seilori", "Sailor", "Sprites/Viholliset/seilori_idle.png"),
        new Cast(2, "Rusty", "Rusty", "Sprites/Viholliset/rusty_idle.png"),
        new Cast(2, "Yakuza", "Yakuza", "Sprites/yakuza_idle.png"),
        new Cast(2, "Tanya", "Tanya", "Sprites/Tanya/tanya_idle_00.png"),
        new Cast(2, "Lentokapteeni", "Flight captain", "Sprites/Viholliset/lentokapteeni_idle.png"),
        new Cast(2, "Lentoperämies", "First officer", "Sprites/Viholliset/lentoperamies_idle.png"),
        new Cast(2, "Luuranko", "Skeletor", "Sprites/Viholliset/luuranko_idle.png"),
        new Cast(2, "Mainari", "Miner", "Sprites/Viholliset/mainari_idle.png"),
        new Cast(2, "Vartija", "Guard", "Sprites/Viholliset/vartija_idle.png"),
        new Cast(2, "Hector", "Hector", "Sprites/Viholliset/hector_idle.png"),
        new Cast(2, "Rico", "Rico", "Sprites/Viholliset/rico_idle.png"),
        new Cast(2, "Melvin", "Melvin", "Sprites/Viholliset/melvin_idle.png"),
        new Cast(2, "Tankkitony", "Tank Tony", "Sprites/Viholliset/tony_idle.png"),
        new Cast(2, "Don", "Don", "Sprites/Viholliset/don_idle.png"),
        new Cast(2, "Mongolisoturi", "Mongol warrior", "Sprites/Viholliset/mongoli_idle.png"),
        new Cast(2, "Kuurobotti", "Moon robot", "Sprites/Viholliset/robotti_idle.png"),
        new Cast(2, "Gestapo", "Gestapo", "Sprites/Viholliset/gestapo_idle.png"),

        new Cast(3, "Sohvi", "Sofie", "Sprites/Sohvi/sohvi_seisoo_idle.png"),
        new Cast(3, "Sini", "Siny", "Resources/Sini/counter_00.png"),
        new Cast(3, "Candy", "Candy", "Sprites/Candi/candi_0_00.png"),
        new Cast(3, "Dina", "Dina", "Resources/Dina/counter_00.png"),
        new Cast(3, "Mama", "Mama", "Sprites/Myyjat/mama_myyja_00.png"),
        new Cast(3, "Helvi", "Helga", "Sprites/Helvi/helvi_idle.png"),
        new Cast(3, "Pablo", "Pablo", "Sprites/Myyjat/pablo_idle_00.png"),
        new Cast(3, "Linsey", "Linsey", "Sprites/linsey_idle.png"),
        new Cast(3, "Violetti", "Purple", "Sprites/Viholliset/violetti_idle.png"),
        new Cast(3, "Nadine", "Nadine", "Sprites/Viholliset/nadine_idle.png"),
        new Cast(3, "Marcus", "Marcus", "Sprites/Viholliset/marcus_idle.png"),
    };

    static EndCredits inst;
    public static bool Active => inst != null;

    public static void Begin()
    {
        if (inst != null) return;
        var go = new GameObject("Lopputekstit"); inst = go.AddComponent<EndCredits>();
    }

    // kind: 0 logo, 1 otsikko, 2 hahmo, 3 teksti, 4 iso teksti; col: 0 keskellä, -1 vasen sarake, 1 oikea sarake
    struct Item { public float y, h; public int kind, cast, col; public string a, b; }
    readonly List<Item> items = new List<Item>();
    Texture2D atlas, logo, px; float scroll, total, startAt, endAt = -1f; GUIStyle head, nameSt, txt, big;
    int atlasRows; Rect[] stars; float[] starPh;

    void Start()
    {
        atlas = Resources.Load<Texture2D>("Lopputekstit/muotokuvat"); logo = Resources.Load<Texture2D>("Valikko/logo");
        atlasRows = atlas != null ? Mathf.Max(1, atlas.height / CellH) : 1;
        px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
        startAt = Time.unscaledTime;
        stars = new Rect[90]; starPh = new float[90];
        for (int i = 0; i < stars.Length; i++) { stars[i] = new Rect(Random.value, Random.value, 1f + Random.value * 2f, 0f); starPh[i] = Random.value * 6.28f; }
        Build();
    }

    // asettelu 1080 px korkeassa viitekehyksessä (skaalataan ruutuun)
    void Build()
    {
        items.Clear(); float y = 1100f;
        items.Add(new Item { y = y, h = 330f, kind = 0 }); y += 390f;
        for (int g = 0; g < GroupFi.Length; g++)
        {
            items.Add(new Item { y = y, h = 110f, kind = 1, a = GroupFi[g], b = GroupEn[g] }); y += 130f;
            if (g == 0)
            {
                for (int i = 0; i < Casts.Length; i++) if (Casts[i].group == 0) { items.Add(new Item { y = y, h = 200f, kind = 2, cast = i, col = 0 }); y += 220f; }
            }
            else
            {
                int side = 0;
                for (int i = 0; i < Casts.Length; i++)
                {
                    if (Casts[i].group != g) continue;
                    items.Add(new Item { y = y, h = 168f, kind = 2, cast = i, col = side == 0 ? -1 : 1 });
                    if (side == 1) y += 190f;
                    side = 1 - side;
                }
                if (side == 1) y += 190f;
            }
            y += 90f;
        }
        items.Add(new Item { y = y, h = 110f, kind = 1, a = "TILASTOT", b = "STATISTICS" }); y += 140f;
        var pc = Players.Primary; int money = pc != null ? pc.money : 0;
        AddText(ref y, "Peliaika: " + GameStats.TimeText(), "Play time: " + GameStats.TimeText());
        AddText(ref y, "Kaadetut viholliset: " + GameStats.Kills, "Enemies defeated: " + GameStats.Kills);
        AddText(ref y, "Voitetut pomot: " + GameStats.Bosses.Count, "Bosses defeated: " + GameStats.Bosses.Count);
        AddText(ref y, "Rahaa mukana: " + money + " mk", "Money carried: " + money + " mk");
        y += 120f; items.Add(new Item { y = y, h = 130f, kind = 4, a = "Kiitos kun pelasit!", b = "Thank you for playing!" }); y += 130f;
        total = y + 500f;
    }
    void AddText(ref float y, string fi, string en) { items.Add(new Item { y = y, h = 70f, kind = 3, a = fi, b = en }); y += 85f; }

    void Update()
    {
        float speed = 85f * (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space) ? 3f : 1f);
        if (Time.unscaledTime - startAt > 1.2f) scroll += speed * Time.unscaledDeltaTime;
        if (scroll > total && endAt < 0f) endAt = Time.unscaledTime;
        if (endAt >= 0f && Time.unscaledTime - endAt > 2.5f && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
        {
            Time.timeScale = 1f; inst = null; SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    void OnDestroy() { if (inst == this) inst = null; }

    void OnGUI()
    {
        GUI.depth = -2000;
        float H = Screen.height, W = Screen.width, u = H / 1080f;
        float fade = Mathf.Clamp01((Time.unscaledTime - startAt) / 2f);
        GUI.color = new Color(0.01f, 0.02f, 0.06f, 0.86f * fade); GUI.DrawTexture(new Rect(0, 0, W, H), px);
        for (int i = 0; i < stars.Length; i++)
        {
            float a = (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.5f + starPh[i]))) * fade;
            GUI.color = new Color(1f, 1f, 1f, a); GUI.DrawTexture(new Rect(stars[i].x * W, stars[i].y * H, stars[i].width * u, stars[i].width * u), px);
        }
        GUI.color = Color.white;
        if (head == null)
        {
            head = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            nameSt = new GUIStyle(head) { alignment = TextAnchor.MiddleLeft };
            txt = new GUIStyle(head); big = new GUIStyle(head);
        }
        bool en = Loc.Current == Loc.Lang.English;
        head.fontSize = Mathf.RoundToInt(64 * u); head.normal.textColor = new Color(1f, 0.85f, 0.25f);
        nameSt.normal.textColor = Color.white;
        txt.fontSize = Mathf.RoundToInt(46 * u); txt.normal.textColor = Color.white;
        big.fontSize = Mathf.RoundToInt(84 * u); big.normal.textColor = new Color(1f, 0.85f, 0.25f);
        foreach (var it in items)
        {
            float top = (it.y - scroll) * u;
            if (top > H || top + it.h * u < -10f) continue;
            var r = new Rect(0, top, W, it.h * u);
            switch (it.kind)
            {
                case 0:
                    if (logo != null) { float lh = it.h * u, lw = lh * logo.width / logo.height; GUI.DrawTexture(new Rect((W - lw) * 0.5f, top, lw, lh), logo, ScaleMode.ScaleToFit); }
                    else GUI.Label(r, "TOTAL FIST", big);
                    break;
                case 1: GUI.Label(r, en ? it.b : it.a, head); break;
                case 3: GUI.Label(r, en ? it.b : it.a, txt); break;
                case 4: GUI.Label(r, en ? it.b : it.a, big); break;
                case 2: DrawCast(it, top, u, W, en); break;
            }
        }
        if (endAt >= 0f && Time.unscaledTime - endAt > 2.5f)
        {
            txt.fontSize = Mathf.RoundToInt(34 * u); GUI.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            GUI.Label(new Rect(0, H * 0.9f, W, 60f * u), en ? "[Enter] Back to the title" : "[Enter] Takaisin alkuun", txt); GUI.color = Color.white;
        }
    }

    void DrawCast(Item it, float top, float u, float W, bool en)
    {
        var c = Casts[it.cast];
        float ph = it.h * u, pw = ph * CellW / CellH;
        float x = it.col == 0 ? W * 0.5f - 330f * u : (it.col < 0 ? W * 0.5f - 700f * u : W * 0.5f + 40f * u);   // pääosat keskellä, muut kahdessa sarakkeessa
        if (atlas != null)
        {
            int col = it.cast % PortraitCols, row = it.cast / PortraitCols;
            var uv = new Rect(col / (float)PortraitCols, 1f - (row + 1) / (float)atlasRows, 1f / PortraitCols, 1f / atlasRows);
            GUI.DrawTextureWithTexCoords(new Rect(x, top, pw, ph), atlas, uv, true);
        }
        nameSt.fontSize = Mathf.RoundToInt((it.col == 0 ? 54f : 38f) * u);
        GUI.Label(new Rect(x + pw + 22f * u, top, 520f * u, ph), en ? c.en : c.fi, nameSt);
    }
}
