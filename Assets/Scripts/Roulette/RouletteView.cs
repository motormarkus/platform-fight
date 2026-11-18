using System;
using System.Collections.Generic;
using System.Linq;
using TotalFist.Roulette;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Ruletti (kokopöytä, eurooppalainen). Avataan pöydän luota (RouletteTableEntry). Raha: pelaajan oma money (panos veloitetaan heti, voitto maksetaan kierroksen jälkeen; poistuttaessa
/// asetetut panokset palautetaan). Ohjaus: hiiri tai oikea tatti (osoitin), A/klikkaus panos, X/oikea klikkaus peru, Y/Space pyöräytä, olkanapit/ristiohjain/1-4 pelimerkki, B/Esc poistu.
/// Kuvat: Resources/Roulette (Tools/RouletteAssets/make_roulette_assets.py). Piirto 1280x720-viitekoordinaatistossa, kaikki aika unscaled (peli on pysäytetty).</summary>
public sealed class RouletteView : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    static int closedFrame = -10;
    public static bool BlocksCombat => IsOpen || Time.frameCount <= closedFrame;
    static readonly List<int> History = new List<int>();
    static List<KeyValuePair<BetSpec, int>> lastBets = new List<KeyValuePair<BetSpec, int>>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { IsOpen = false; closedFrame = -10; History.Clear(); lastBets.Clear(); }

    // asettelu 1280x720-viitteessä: pyörä vasemmalla, pöytä oikealla
    const float PanelScale = 1f / 3f, WheelSize = 520f, WheelTex = 1536f, DiscR = 539f, TrackR = 556f, LandR = 455f;   // pyörä: neliökuva 1536 px, numerolevyn ulkosäde 539 px
    const float PanelX = 560f, WheelX = 17f;
    float PanelY => (720f - RouletteLayout.PanelH * PanelScale) * .5f - 6f;
    float WheelY => (720f - WheelSize) * .5f;
    float discRot;   // numerolevyn kierto radiaaneina, myötäpäivään

    enum Phase { Betting, Spinning, Result }
    PlayerController player;
    RectTransform root, chipLayer, hlLayer;
    Font font;
    RouletteTable table = new RouletteTable();
    RectTransform wheelRect, disc; Image ball;
    Text balanceText, stakeText, messageText, hintText;
    Image cursorImg, chipSelect, winMark;
    readonly Image[] cellHl = new Image[37];
    readonly List<GameObject> chipObjs = new List<GameObject>();
    readonly List<Image> historyBoxes = new List<Image>(); readonly List<Text> historyTexts = new List<Text>();
    Texture2D discTex, glowTex, notchTex;
    RectTransform ballShadow; Image glow, badge; Text badgeText; const float BallSize = 27f;
    readonly List<KeyValuePair<Transform, float>> pops = new List<KeyValuePair<Transform, float>>();
    readonly List<PlayerController> disabledPlayers = new List<PlayerController>(); readonly List<GameHUD> disabledHuds = new List<GameHUD>();
    float oldTimeScale = 1f, inputAfter;
    int chipIndex = 1, result = -1;
    Phase phase = Phase.Betting;
    float t, ringRot0, ringTotal, thetaRel0, thetaTotal; float spinDur = 6.4f, resultAt;
    AudioSource sfx, sfxLate, loopBall, loopWheel; int bounceIdx;
    static readonly float[] BounceAt = { .22f, .48f, .74f }, BounceVol = { .75f, .55f, .4f };
    float Vol => GameSettings.SfxVolume;
    void Play(AudioClip c, float vol = 1f, float pitch = 1f) { if (c == null || sfx == null) return; sfx.pitch = pitch; sfx.PlayOneShot(c, vol * Vol); }
    void PlayLate(AudioClip c, float delay, float vol = 1f) { if (c == null || sfxLate == null) return; sfxLate.clip = c; sfxLate.volume = vol * Vol; sfxLate.PlayScheduled(AudioSettings.dspTime + delay); }
    AudioSource NewSource(bool loop, AudioClip clip)
    {
        var a = gameObject.AddComponent<AudioSource>(); a.playOnAwake = false; a.ignoreListenerPause = true; a.loop = loop; a.clip = clip; a.spatialBlend = 0f; return a;
    }
    string lastPlacedKey;
    string message = ""; Color messageColor = Color.white;
    Vector2 cursor = new Vector2(850, 330); bool useMouse = true;
    RouletteLayout.Hit hover; int hoverKey = -1; string hoverSpecKey;
    int lastWin;
    readonly System.Random rng = new System.Random(Environment.TickCount);
    static readonly Color Gold = new Color(1f, .79f, .38f);

    public static bool Open(PlayerController p)
    {
        if (p == null || IsOpen || BlocksCombat || PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen) return false;
        var go = new GameObject("Ruletti · UI");
        var v = go.AddComponent<RouletteView>(); v.Build(p);
        return true;
    }

    void Build(PlayerController p)
    {
        player = p; IsOpen = true;
        RouletteSfx.Build(); sfx = NewSource(false, null); sfxLate = NewSource(false, null); loopBall = NewSource(true, RouletteSfx.Roll); loopWheel = NewSource(true, RouletteSfx.Whirr);
        font = Resources.Load<Font>("Valikko/Bangers"); if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        var matte = new GameObject("Tausta", typeof(RectTransform), typeof(Image)); matte.transform.SetParent(transform, false);
        var mr = matte.GetComponent<RectTransform>(); mr.anchorMin = Vector2.zero; mr.anchorMax = Vector2.one; mr.offsetMin = mr.offsetMax = Vector2.zero;
        matte.GetComponent<Image>().color = new Color(.02f, .012f, .012f, 1f);
        var glowBg = Img(transform, "Hehku", 0, 0, 1700, 1100, MakeGlow(128), new Color(.42f, .14f, .08f, .55f));
        glowBg.rectTransform.anchorMin = glowBg.rectTransform.anchorMax = new Vector2(.5f, .5f); glowBg.rectTransform.pivot = new Vector2(.5f, .5f); glowBg.rectTransform.anchoredPosition = Vector2.zero;
        root = new GameObject("1280x720", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(transform, false);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f); root.sizeDelta = new Vector2(1280, 720);

        var frameTex = Resources.Load<Texture2D>("Roulette/wheel_frame"); var discTex2 = Resources.Load<Texture2D>("Roulette/wheel_disc"); var panelTex = Resources.Load<Texture2D>("Roulette/panel");
        if (frameTex == null || discTex2 == null || panelTex == null) Debug.LogError("Ruletin kuvat puuttuvat (Assets/Resources/Roulette). Aja Tools/RouletteAssets/make_roulette_assets.py.");
        // pyörä ylhäältä: kiinteä runko + pyörivä numerolevy + pallo
        var wShadow = Img(root, "Pyörän varjo", WheelX - 34, WheelY - 18, WheelSize + 68, WheelSize + 68, MakeGlow(128), new Color(0, 0, 0, .75f));
        wheelRect = Rt(root, "Pyörä", WheelX, WheelY, WheelSize, WheelSize);
        wheelRect.gameObject.AddComponent<RawImage>().texture = frameTex;
        var discGo = new GameObject("Numerolevy", typeof(RectTransform)); discGo.transform.SetParent(wheelRect, false);
        disc = discGo.GetComponent<RectTransform>(); disc.anchorMin = Vector2.zero; disc.anchorMax = Vector2.one; disc.offsetMin = disc.offsetMax = Vector2.zero; disc.pivot = new Vector2(.5f, .5f);
        var dr = discGo.AddComponent<RawImage>(); dr.texture = discTex2; dr.raycastTarget = false;
        discTex = MakeDisc(64); glowTex = MakeGlow(128); notchTex = MakeNotches(96);
        // pallo: Flipper Flapperin kromikuula (Resources/Flipper/pallo.png, kuula rajattu kuvasta), pehmeä varjo alla
        var shadow = Img(wheelRect, "Pallon varjo", 0, 0, 26, 26, glowTex, new Color(0, 0, 0, .6f)); CenterAnchor(shadow.rectTransform); ballShadow = shadow.rectTransform; shadow.gameObject.SetActive(false);
        var chrome = Resources.Load<Texture2D>("Flipper/pallo");
        if (chrome != null) { chrome.filterMode = FilterMode.Bilinear; ball = Img(wheelRect, "Pallo", 0, 0, BallSize, BallSize, null, Color.white); ball.sprite = Sprite.Create(chrome, new Rect(30, 38, 172, 173), new Vector2(.5f, .5f), 100f); }
        else ball = Img(wheelRect, "Pallo", 0, 0, BallSize, BallSize, discTex, new Color(.9f, .92f, .96f));
        CenterAnchor(ball.rectTransform); ball.gameObject.SetActive(false);
        glow = Img(wheelRect, "Voittohehku", 0, 0, 78, 78, glowTex, new Color(1f, .8f, .3f, .0f)); CenterAnchor(glow.rectTransform); glow.gameObject.SetActive(false);
        // voittonumeron merkki navan päällä
        badge = Img(wheelRect, "Voittonumero", 0, 0, 116, 116, discTex, new Color(.1f, .1f, .12f)); CenterAnchor(badge.rectTransform);
        var ring2 = Img(badge.transform, "Kehys", 0, 0, 128, 128, MakeRing(96), Gold); CenterAnchor(ring2.rectTransform); ring2.rectTransform.anchoredPosition = Vector2.zero;
        badgeText = Txt(badge.transform, new Rect(0, 0, 116, 116), "", 62, Color.white, TextAnchor.MiddleCenter); badge.gameObject.SetActive(false);
        // pöytä
        var panel = Rt(root, "Pöytä", PanelX, PanelY, RouletteLayout.PanelW * PanelScale, RouletteLayout.PanelH * PanelScale);
        panel.gameObject.AddComponent<RawImage>().texture = panelTex;
        hlLayer = Rt(root, "Korostukset", 0, 0, 1280, 720); chipLayer = Rt(root, "Pelimerkit", 0, 0, 1280, 720);
        for (int n = 0; n < 37; n++)
        {
            float u, v, w, h; RouletteLayout.CellRect(n, out u, out v, out w, out h);
            var a = PR(u, v); cellHl[n] = Img(hlLayer, "Korostus " + n, a.x, a.y, w * PanelScale, h * PanelScale, null, new Color(1f, .85f, .3f, .3f)); cellHl[n].gameObject.SetActive(false);
        }
        winMark = Img(hlLayer, "Voittaja", 0, 0, 10, 10, null, new Color(1f, .8f, .2f, .5f)); winMark.gameObject.SetActive(false);
        chipSelect = Img(root, "Valittu pelimerkki", 0, 0, 10, 10, MakeRing(64), Gold); chipSelect.rectTransform.pivot = new Vector2(.5f, .5f);
        balanceText = Txt(root, PanelRect(1250, 40, 790, 110), "", 28, Gold, TextAnchor.MiddleRight);
        stakeText = Txt(root, PanelRect(740, 1000, 300, 130), "", 20, Color.white, TextAnchor.MiddleCenter);
        messageText = Txt(root, PanelRect(201, 798, 1680, 84), "", 24, Color.white, TextAnchor.MiddleCenter);
        hintText = Txt(root, PanelRect(120, 1150, 1870, 60), "Hiiri tai oikea tatti: osoitin   ·   A / klikkaus: panos   ·   X / oikea klikkaus: peru   ·   Y / Välilyönti: pyöräytä   ·   olkanapit: pelimerkki   ·   B / Esc: poistu", 13, new Color(.8f, .76f, .65f), TextAnchor.MiddleCenter);
        for (int i = 0; i < 12; i++)
        {
            var box = Img(root, "Historia", 0, 0, 30, 24, null, Color.black);
            var tx = Txt(box.transform, new Rect(0, 0, 30, 24), "", 15, Color.white, TextAnchor.MiddleCenter);
            historyBoxes.Add(box); historyTexts.Add(tx); box.gameObject.SetActive(false);
        }
        cursorImg = Img(root, "Osoitin", 0, 0, 30, 30, MakeRing(64), Color.white); cursorImg.rectTransform.pivot = new Vector2(.5f, .5f); cursorImg.raycastTarget = false;
        foreach (var pl in Players.All) if (pl != null && pl.enabled) { disabledPlayers.Add(pl); pl.enabled = false; }
        foreach (var hud in FindObjectsByType<GameHUD>(FindObjectsSortMode.None)) if (hud.enabled) { disabledHuds.Add(hud); hud.enabled = false; }
        oldTimeScale = Time.timeScale; Time.timeScale = 0f;
        inputAfter = Time.unscaledTime + .25f;
        SetDiscRot(0f); RefreshAll();
        SetMessage("Aseta panokset ja pyöräytä.", Color.white);
    }

    // ---------------- apurit ----------------
    Vector2 PR(float u, float v) => new Vector2(PanelX + u * PanelScale, PanelY + v * PanelScale);
    Rect PanelRect(float u, float v, float w, float h) { var a = PR(u, v); return new Rect(a.x, a.y, w * PanelScale, h * PanelScale); }
    static RectTransform Rt(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
    }
    static Image Img(Transform parent, string name, float x, float y, float w, float h, Texture2D tex, Color color)
    {
        var r = Rt(parent, name, x, y, w, h); var im = r.gameObject.AddComponent<Image>(); im.color = color; im.raycastTarget = false;
        if (tex != null) im.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
        return im;
    }
    Text Txt(Transform parent, Rect rc, string s, int size, Color color, TextAnchor anchor)
    {
        var r = Rt(parent, "Teksti", rc.x, rc.y, rc.width, rc.height); var t = r.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.color = color; t.alignment = anchor; t.text = s; t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = r.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0, 0, 0, .85f); o.effectDistance = new Vector2(1.2f, -1.2f);
        return t;
    }
    static Texture2D MakeDisc(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f - n / 2f) / (n / 2f), dy = (y + .5f - n / 2f) / (n / 2f), d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - d) * n / 4f); float l = Mathf.Lerp(1f, .62f, Mathf.Clamp01(d)) * (1f - .25f * Mathf.Clamp01(dy + .2f));   // pieni varjostus
                if (d > .72f && d < .8f) l *= .78f;
                tex.SetPixel(x, y, new Color(l, l, l, a));
            }
        tex.Apply(); return tex;
    }
    static void CenterAnchor(RectTransform r) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); }
    static Texture2D MakeGlow(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f - n / 2f) / (n / 2f), dy = (y + .5f - n / 2f) / (n / 2f), d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f); tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply(); return tex;
    }
    // pelimerkin reunan lovet ja sisärengas (valkoinen, läpinäkyvä), väritetty kiekon päälle
    static Texture2D MakeNotches(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f - n / 2f) / (n / 2f), dy = (y + .5f - n / 2f) / (n / 2f), d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * 8f / Mathf.PI, f = Mathf.Abs(ang - Mathf.Round(ang));
                float notch = (d > .72f && d < .98f && f < .22f) ? 1f : 0f;
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - .6f) * n / 5f) * .9f;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(Mathf.Max(notch, inner) * Mathf.Clamp01((1f - d) * n / 5f))));
            }
        tex.Apply(); return tex;
    }
    static Texture2D MakeRing(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f - n / 2f) / (n / 2f), dy = (y + .5f - n / 2f) / (n / 2f), d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(Mathf.Min((d - .72f), (.96f - d)) * n / 5f);
                float dot = Mathf.Clamp01((.13f - d) * n / 5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Max(a, dot)));
            }
        tex.Apply(); return tex;
    }
    void SetMessage(string s, Color c) { message = s; messageColor = c; if (messageText != null) { messageText.text = s; messageText.color = c; } }

    // ---------------- tila ----------------
    int Wallet => player != null ? player.money : 0;
    void RefreshAll()
    {
        balanceText.text = "Rahat: " + Wallet + " mk";
        stakeText.text = "Panokset yhteensä\n" + table.TotalStake + " mk\n<size=14>pelimerkki " + RouletteLayout.ChipValues[chipIndex] + " mk</size>";
        var cp = PR(RouletteLayout.ChipX[chipIndex], RouletteLayout.ChipY); float d = RouletteLayout.ChipR * 2f * PanelScale + 6f;
        chipSelect.rectTransform.anchoredPosition = new Vector2(cp.x, -cp.y); chipSelect.rectTransform.sizeDelta = new Vector2(d, d);
        RebuildChips();
        for (int i = 0; i < historyBoxes.Count; i++)
        {
            bool has = i < History.Count; historyBoxes[i].gameObject.SetActive(has);
            if (!has) continue;
            int n = History[History.Count - 1 - i];
            historyBoxes[i].color = n == 0 ? new Color(.12f, .5f, .32f) : RouletteTable.IsRed(n) ? new Color(.6f, .13f, .1f) : new Color(.1f, .1f, .12f);
            historyTexts[i].text = n.ToString();
            var a = PR(201 + 140f * i, 905); historyBoxes[i].rectTransform.anchoredPosition = new Vector2(a.x, -a.y);
        }
    }
    void RebuildChips()
    {
        foreach (var o in chipObjs) if (o != null) Destroy(o);
        chipObjs.Clear();
        foreach (var e in table.Bets)
        {
            int amt = e.Amount; var p = PR(e.Spec.PosU, e.Spec.PosV); float d = 26f;
            Color c = amt < 5 ? new Color(.93f, .9f, .8f) : amt < 25 ? new Color(.78f, .16f, .13f) : amt < 100 ? new Color(.12f, .5f, .32f) : new Color(.16f, .16f, .2f);
            var sh = Img(chipLayer, "Panos " + e.Spec.Key, p.x - d / 2 - 2, p.y - d / 2 + 2, d + 4, d + 4, glowTex, new Color(0, 0, 0, .6f));   // varjo = panoksen juuriobjekti
            var chip = Img(sh.transform, "Pelimerkki", 0, 0, d, d, discTex, c); CenterAnchor(chip.rectTransform); chip.rectTransform.anchoredPosition = new Vector2(-1.5f, 1.5f);
            var rim = Img(chip.transform, "Reuna", 0, 0, d, d, notchTex, new Color(1f, 1f, 1f, amt < 5 ? .55f : .8f)); rim.rectTransform.anchorMin = Vector2.zero; rim.rectTransform.anchorMax = Vector2.one; rim.rectTransform.offsetMin = rim.rectTransform.offsetMax = Vector2.zero;
            var tx = Txt(chip.transform, new Rect(-8, 0, d + 16, d), amt.ToString(), amt >= 1000 ? 9 : 12, amt < 5 ? Color.black : Color.white, TextAnchor.MiddleCenter);
            var ol = tx.GetComponent<Outline>(); if (amt < 5) ol.effectColor = new Color(1, 1, 1, .6f);
            if (e.Spec.Key == lastPlacedKey) { pops.Add(new KeyValuePair<Transform, float>(sh.transform, Time.unscaledTime)); lastPlacedKey = null; }
            chipObjs.Add(sh.gameObject);
        }
    }
    void ShowHover(BetSpec spec)
    {
        string key = spec != null ? spec.Key : null;
        if (key == hoverSpecKey) return;
        hoverSpecKey = key;
        for (int n = 0; n < 37; n++) cellHl[n].gameObject.SetActive(false);
        if (spec != null) foreach (int n in spec.Numbers) cellHl[n].gameObject.SetActive(true);
    }

    // ---------------- toiminnot ----------------
    void PlaceAt(BetSpec spec)
    {
        if (phase != Phase.Betting || spec == null) return;
        int chip = RouletteLayout.ChipValues[chipIndex];
        if (Wallet < chip) { SetMessage("Ei tarpeeksi rahaa tälle pelimerkille.", new Color(1f, .5f, .4f)); return; }
        int add = table.Place(spec, chip); lastPlacedKey = spec.Key;
        if (add <= 0) { SetMessage("Tähän ruutuun mahtuu enintään " + RouletteTable.MaxPerSpot + " mk.", new Color(1f, .5f, .4f)); return; }
        player.money -= add; Play(RouletteSfx.Chip, .9f, 0.95f + .06f * chipIndex + UnityEngine.Random.Range(-.03f, .03f));
        SetMessage(spec.Label + ": " + table.AmountOn(spec.Key) + " mk  (voitto " + (spec.Pays + 1) + "x panos)", Color.white);
        RefreshAll();
    }
    void Undo() { if (phase != Phase.Betting) return; int back = table.Undo(); if (back > 0) { player.money += back; Play(RouletteSfx.Chip, .6f, .78f); SetMessage("Viimeisin panos peruttu.", Color.white); RefreshAll(); } }
    void ClearAll() { if (phase != Phase.Betting) return; int back = table.Clear(); if (back > 0) { player.money += back; Play(RouletteSfx.Chip, .8f, .7f); PlayLate(RouletteSfx.Chip, .07f, .5f); SetMessage("Panokset tyhjennetty.", Color.white); RefreshAll(); } }
    void Rebet()
    {
        if (phase != Phase.Betting || lastBets.Count == 0 || table.TotalStake > 0) return;
        int need = lastBets.Sum(b => b.Value);
        if (Wallet < need) { SetMessage("Edellisiin panoksiin tarvitaan " + need + " mk.", new Color(1f, .5f, .4f)); return; }
        foreach (var b in lastBets) { int add = table.Place(b.Key, b.Value); player.money -= add; }
        SetMessage("Edelliset panokset asetettu.", Color.white); RefreshAll();
    }
    void Spin()
    {
        if (phase != Phase.Betting) return;
        if (table.TotalStake <= 0) { SetMessage(lastBets.Count > 0 ? "Aseta panos (tai L3 / R: edelliset panokset)." : "Aseta ensin panos pöydälle.", new Color(1f, .8f, .4f)); return; }
        lastBets = table.Bets.Select(e => new KeyValuePair<BetSpec, int>(e.Spec, e.Amount)).ToList();
        result = rng.Next(37);
        phase = Phase.Spinning; t = 0; bounceIdx = 0; Play(RouletteSfx.Whoosh, .8f); loopBall.volume = 0f; loopWheel.volume = 0f; loopBall.Play(); loopWheel.Play(); ball.gameObject.SetActive(true); ballShadow.gameObject.SetActive(true); glow.gameObject.SetActive(false); badge.gameObject.SetActive(false);
        ringRot0 = discRot;
        ringTotal = (2.2f + (float)rng.NextDouble() * .6f) * Mathf.PI * 2f;                 // rengas pyörähtää myötäpäivään
        float alpha = RouletteTable.PocketIndex(result) * Mathf.PI * 2f / 37f;
        thetaRel0 = (float)rng.NextDouble() * Mathf.PI * 2f;
        float diff = Mathf.Repeat(thetaRel0 - alpha, Mathf.PI * 2f);                         // pallo kiertää vastapäivään renkaaseen nähden ja päätyy voittokoloon
        thetaTotal = diff + Mathf.PI * 2f * (6f + (float)rng.Next(0, 2));
        SetMessage("Kierros käynnissä...", Gold); RefreshAll();
    }
    static float EaseOutCubic(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }
    static float EaseOutQuad(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }

    void Finish()
    {
        loopBall.Stop(); loopWheel.Stop(); Play(RouletteSfx.Clunk, .9f);
        var res = table.Resolve(result); int total = res.Sum(r => r.Won), stake = res.Sum(r => r.Stake);
        if (total > 0) PlayLate(total >= stake * 8 || total >= 500 ? RouletteSfx.WinBig : RouletteSfx.WinSmall, .4f, .85f); else PlayLate(RouletteSfx.Lose, .45f, .8f);
        player.money += total; History.Add(result); if (History.Count > 12) History.RemoveAt(0);
        string col = RouletteTable.ColorName(result);
        if (total > 0) SetMessage("Numero " + result + " (" + col + "). Voitit " + (total - stake >= 0 ? "+" : "") + (total - stake) + " mk, palautus " + total + " mk!", new Color(.6f, 1f, .6f));
        else SetMessage("Numero " + result + " (" + col + "). Ei voittoa, " + stake + " mk meni.", new Color(1f, .6f, .55f));
        float u, v, w, h; RouletteLayout.CellRect(result, out u, out v, out w, out h); var a = PR(u, v);
        winMark.rectTransform.anchoredPosition = new Vector2(a.x, -a.y); winMark.rectTransform.sizeDelta = new Vector2(w * PanelScale, h * PanelScale); winMark.gameObject.SetActive(true);
        phase = Phase.Result; resultAt = Time.unscaledTime; lastWin = total;
        badge.color = result == 0 ? new Color(.1f, .5f, .32f) : RouletteTable.IsRed(result) ? new Color(.62f, .12f, .1f) : new Color(.08f, .08f, .1f);
        badgeText.text = result.ToString(); badge.gameObject.SetActive(true); badge.transform.localScale = Vector3.zero; pops.Add(new KeyValuePair<Transform, float>(badge.transform, Time.unscaledTime));
        glow.gameObject.SetActive(true); glow.rectTransform.anchoredPosition = ball.rectTransform.anchoredPosition;
        RefreshAll();
    }

    void Leave()
    {
        if (phase == Phase.Spinning) return;
        int back = table.Clear(); if (back > 0 && player != null) player.money += back;
        Close();
    }
    void Close()
    {
        if (!IsOpen) return;
        IsOpen = false; closedFrame = Time.frameCount + 1;
        foreach (var p in disabledPlayers) if (p != null) p.enabled = true; disabledPlayers.Clear();
        foreach (var hud in disabledHuds) if (hud != null) hud.enabled = true; disabledHuds.Clear();
        Time.timeScale = oldTimeScale;
        Destroy(gameObject);
    }
    void OnDestroy()
    {
        if (IsOpen) { int back = table.Clear(); if (back > 0 && player != null) player.money += back; IsOpen = false; closedFrame = Time.frameCount + 1; Time.timeScale = oldTimeScale; foreach (var p in disabledPlayers) if (p != null) p.enabled = true; foreach (var hud in disabledHuds) if (hud != null) hud.enabled = true; }
    }

    // ---------------- päivitys ----------------
    Vector2 ScreenToRef(Vector2 screen)
    {
        Vector2 lp; RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out lp);
        return new Vector2(lp.x + 640f, 360f - lp.y);
    }
    void Update()
    {
        if (!IsOpen) return;
        // sovitus: koko 1280x720 mahtuu ruutuun suhteessa
        var frame = transform as RectTransform; root.localScale = Vector3.one * Mathf.Min(frame.rect.width / 1280f, frame.rect.height / 720f);
        float dt = Time.unscaledDeltaTime;
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = CoopInput.PadFor(0);
        if (Time.unscaledTime < inputAfter) return;

        // osoitin
        if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > .01f) useMouse = true;
        Vector2 stick = pad != null ? pad.rightStick.ReadValue() : Vector2.zero;
        if (stick.sqrMagnitude > .09f) useMouse = false;
        if (useMouse && mouse != null) cursor = ScreenToRef(mouse.position.ReadValue());
        else if (stick.sqrMagnitude > .04f) { cursor += new Vector2(stick.x, -stick.y) * (stick.magnitude > .9f ? 620f : 380f) * dt; cursor = new Vector2(Mathf.Clamp(cursor.x, 0, 1280), Mathf.Clamp(cursor.y, 0, 720)); }
        cursorImg.gameObject.SetActive(!useMouse);
        cursorImg.rectTransform.anchoredPosition = new Vector2(cursor.x, -cursor.y);

        // kohde osoittimen alla
        float u = (cursor.x - PanelX) / PanelScale, v = (cursor.y - PanelY) / PanelScale;
        hover = RouletteLayout.Probe(u, v);
        ShowHover(phase == Phase.Betting && hover.Zone == RouletteLayout.Zone.Bet ? hover.Spec : null);

        bool leave = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
        if (leave) { Leave(); return; }
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            var pr = pops[i]; if (pr.Key == null) { pops.RemoveAt(i); continue; }
            float k = (Time.unscaledTime - pr.Value) / .16f;
            if (k >= 1f) { pr.Key.localScale = Vector3.one; pops.RemoveAt(i); } else pr.Key.localScale = Vector3.one * (1f + .45f * Mathf.Sin(k * Mathf.PI) * (1f - k * .3f));
        }
        if (phase == Phase.Betting) HandleBettingInput(kb, mouse, pad);
        if (phase == Phase.Spinning) Animate(dt);
        else if (phase == Phase.Result && Time.unscaledTime - resultAt > .3f)
        {
            float pulse = .35f + .25f * Mathf.Sin((Time.unscaledTime - resultAt) * 6f);
            winMark.color = new Color(1f, .8f, .2f, pulse); glow.color = new Color(1f, .8f, .3f, pulse + .25f);
            bool any = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (pad != null && (pad.buttonNorth.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame)) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (any || Time.unscaledTime - resultAt > 6f) { winMark.gameObject.SetActive(false); glow.gameObject.SetActive(false); badge.gameObject.SetActive(false); phase = Phase.Betting; if (Wallet <= 0 && table.TotalStake == 0) SetMessage("Rahat loppuivat. Poistu pöydästä (B / Esc).", new Color(1f, .6f, .5f)); else SetMessage("Aseta panokset ja pyöräytä.", Color.white); inputAfter = Time.unscaledTime + .15f; }
        }
    }

    void HandleBettingInput(Keyboard kb, Mouse mouse, Gamepad pad)
    {
        // pelimerkin valinta
        int dir = 0;
        if (pad != null) { if (pad.rightShoulder.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame) dir++; if (pad.leftShoulder.wasPressedThisFrame || pad.dpad.left.wasPressedThisFrame) dir--; }
        if (mouse != null) { float sc = mouse.scroll.ReadValue().y; if (sc > 0) dir++; else if (sc < 0) dir--; }
        int direct = -1;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame) direct = 0; else if (kb.digit2Key.wasPressedThisFrame) direct = 1; else if (kb.digit3Key.wasPressedThisFrame) direct = 2; else if (kb.digit4Key.wasPressedThisFrame) direct = 3;
            if (kb.rightArrowKey.wasPressedThisFrame) dir++;
            if (kb.leftArrowKey.wasPressedThisFrame) dir--;
        }
        if (direct >= 0) { chipIndex = direct; RefreshAll(); }
        else if (dir != 0) { chipIndex = Mathf.Clamp(chipIndex + dir, 0, 3); RefreshAll(); }

        bool click = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
        if (click)
        {
            switch (hover.Zone)
            {
                case RouletteLayout.Zone.Bet: PlaceAt(hover.Spec); break;
                case RouletteLayout.Zone.Chip: chipIndex = hover.Chip; Play(RouletteSfx.Chip, .45f, 1.35f); RefreshAll(); break;
                case RouletteLayout.Zone.Clear: ClearAll(); break;
                case RouletteLayout.Zone.Spin: Spin(); break;
            }
        }
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame) || (kb != null && kb.backspaceKey.wasPressedThisFrame)) Undo();
        if ((kb != null && kb.deleteKey.wasPressedThisFrame)) ClearAll();
        if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame)) Spin();
        if ((kb != null && kb.rKey.wasPressedThisFrame) || (pad != null && pad.leftStickButton.wasPressedThisFrame)) Rebet();
    }

    void SetDiscRot(float r) { discRot = r; if (disc != null) disc.localRotation = Quaternion.Euler(0f, 0f, -r * Mathf.Rad2Deg); }
    void Animate(float dt)
    {
        t += dt; float u = Mathf.Clamp01(t / spinDur);
        float rot = ringRot0 + ringTotal * EaseOutQuad(u);
        SetDiscRot(rot);
        float theta = thetaRel0 - thetaTotal * EaseOutCubic(u);
        float r;
        if (u < .62f) r = TrackR;
        else
        {
            float k = Mathf.Clamp01((u - .62f) / .38f), e = k * k * (3f - 2f * k);
            r = Mathf.Lerp(TrackR, LandR, e) + 14f * Mathf.Sin(k * Mathf.PI * 4f) * (1f - k) * (1f - k);
        }
        float world = theta + rot, k2 = WheelSize / WheelTex;   // kulma myötäpäivään ylhäältä
        var bpos = new Vector2(Mathf.Sin(world), Mathf.Cos(world)) * (r * k2);
        ball.rectTransform.anchoredPosition = bpos; ballShadow.anchoredPosition = bpos + new Vector2(2.2f, -3.2f);
        ball.rectTransform.localScale = Vector3.one * (r > LandR + 30f ? 1.18f : 1f);   // reunalla pallo on korkeammalla
        // äänet: pyörän hurina ja pallon vieriminen hidastuvat; kolon reunoilla kilahtaa
        loopWheel.volume = .22f * Vol * (1f - u); loopWheel.pitch = .65f + .9f * (1f - u);
        float kb = (u - .62f) / .38f;
        loopBall.volume = Vol * (u < .62f ? .55f : Mathf.Lerp(.55f, 0f, Mathf.Clamp01(kb * 1.3f))); loopBall.pitch = Mathf.Lerp(1.35f, .7f, u);
        while (bounceIdx < BounceAt.Length && kb >= BounceAt[bounceIdx]) { Play(RouletteSfx.Tink, BounceVol[bounceIdx], UnityEngine.Random.Range(.88f, 1.12f)); bounceIdx++; }
        if (t >= spinDur) { SetDiscRot(Mathf.Repeat(rot, Mathf.PI * 2f)); Finish(); }
    }
}
