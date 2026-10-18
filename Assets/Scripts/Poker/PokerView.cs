using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TotalFist.Poker;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Approved reference coordinates: 1280x720. Text stays live UI, never baked into artwork.
public sealed class PokerView : MonoBehaviour
{
    public bool HostClosing;
    PokerService host;
    PokerPresentationSettings art;
    RectTransform root;
    Font font;
    Text[] names = new Text[5], stacks = new Text[5], bets = new Text[5];
    GameObject[] highlights = new GameObject[5];
    Text pot, status, callText, raiseText, amountText, walletText, message, phase;
    RectTransform dealer;
    RawImage[] turnGlows = new RawImage[5]; Texture2D glowTexture;
    GameObject[] bubbles = new GameObject[5]; Text[] bubbleTexts = new Text[5]; string[] lastActions = new string[5]; float[] bubbleUntil = new float[5];
    GameObject[] blindBadges = new GameObject[5]; Text[] blindTexts = new Text[5];
    Button fold, call, raise, allIn, deal, rebuy, minus, plus, leave;
    Slider slider;
    int raiseTo, shownRevision = -1, selection;
    bool rendering;
    float repeatAt, inputAfter;
    readonly List<Button> controls = new List<Button>();
    readonly Dictionary<Button, GameObject> controlFrames = new Dictionary<Button, GameObject>();
    readonly List<Behaviour> oldEventSystems = new List<Behaviour>();
    readonly List<GameObject> cards = new List<GameObject>();
    readonly Dictionary<string, int> lastCards = new Dictionary<string, int>();
    readonly int[] lastBets = new int[5];
    int lastHand = -1, boardHand = -1, revealedBoard;
    bool runoutActive; Coroutine runoutRoutine;
    EventSystem eventSystem;
    AudioSource audioSource;
    PokerCardWorldLayer worldCards;
    // Pelipaikat on järjestetty pöydän ympäri myötäpäivään: Sinä, Puliukko, Yakuza, Prätkäjätkä, Rokkimimmi.
    static readonly Vector2[] SeatPos = { new Vector2(560, 548), new Vector2(25, 385), new Vector2(395, 262), new Vector2(790, 264), new Vector2(1110, 380) };
    // Hehkun keskipiste ja koko (x, y, leveys, korkeus) hahmon kohdalla.
    static readonly Vector4[] GlowCenter = { Vector4.zero, new Vector4(150, 235, 380, 340), new Vector4(450, 150, 420, 320), new Vector4(850, 155, 420, 320), new Vector4(1150, 255, 380, 340) };
    // Jakajan nappi paneelin yläpuolelle, poissa korttien ja merkkipinojen päältä.
    static readonly Vector2[] DealerPos = { new Vector2(745, 578), new Vector2(125, 369), new Vector2(495, 246), new Vector2(890, 248), new Vector2(1210, 364) };
    static readonly Vector2[] HolePos = { new Vector2(560, 448), new Vector2(168, 367), new Vector2(278, 242), new Vector2(923, 242), new Vector2(978, 367) };

    Texture2D tableTex, panelTex; Material tableMat, panelMat;
    public void Build(PokerService service, PokerPresentationSettings settings)
    {
        host = service; art = settings;
        // Resolve shared artwork at runtime too: existing scene settings may
        // retain missing references after an editor import or domain reload.
        if (art.tableAndOpponents == null) art.tableAndOpponents = Resources.Load<Texture2D>("Poker/Artwork/TableOpponents");
        if (art.panelAtlas == null) art.panelAtlas = Resources.Load<Texture2D>("Poker/Artwork/Panels");
        if (art.whiteKeyMaterial == null) art.whiteKeyMaterial = Resources.Load<Material>("Poker/WhiteKey");
        // Esikäsitellyt RGBA-versiot (Tools/PokerAssets/key_poker_artwork.py): tausta poistettu kuvasta, ei shaderilla.
        tableTex = Resources.Load<Texture2D>("Poker/Artwork/TableOpponentsKeyed"); tableMat = null;
        if (tableTex == null) { tableTex = art.tableAndOpponents; tableMat = art.whiteKeyMaterial; }
        panelTex = Resources.Load<Texture2D>("Poker/Artwork/PanelsKeyed"); panelMat = null;
        if (panelTex == null) { panelTex = art.panelAtlas; panelMat = art.whiteKeyMaterial; }
        if (tableTex == null || panelTex == null)
            Debug.LogError("Pokerin yhteinen kuvitus puuttuu Resources/Poker/Artwork-kansiosta. Tarkista kuvien importointi.");
        font = art.textFont != null ? art.textFont : Resources.Load<Font>("Valikko/Bangers");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        audioSource = gameObject.AddComponent<AudioSource>(); audioSource.ignoreListenerPause = true; audioSource.playOnAwake = false;
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000; canvas.pixelPerfect = true;   // teksti pikselirajoihin, ei sumeutta
        var scale = gameObject.AddComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();
        // Fit reference without stretching on ultrawide/4:3; outside remains a dark matte.
        var matte = Panel(transform, new Rect(0, 0, 1280, 720), new Color(.025f, .019f, .015f));
        matte.rectTransform.anchorMin = Vector2.zero; matte.rectTransform.anchorMax = Vector2.one; matte.rectTransform.offsetMin = matte.rectTransform.offsetMax = Vector2.zero;
        root = new GameObject("1280x720 reference frame", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(transform, false);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, .5f); root.sizeDelta = new Vector2(1280, 720);
        var fitter = root.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 1280f / 720f;
        // The root's children use the reference space, scaled with the root if needed.
        var reference = new GameObject("Reference content", typeof(RectTransform)).GetComponent<RectTransform>(); reference.SetParent(root, false);
        reference.anchorMin = reference.anchorMax = new Vector2(.5f, .5f); reference.sizeDelta = new Vector2(1280, 720);
        root = reference;
        // Valmis kuva: baari, pöytä ja pelaajat samassa tarkassa (3840x2160) kuvassa. Vanha polku on varalla.
        var scene = Resources.Load<Texture2D>("Poker/Artwork/TableScene");
        if (scene != null) Raw(root, new Rect(0, 0, 1280, 720), scene, null);
        else
        {
            Raw(root, new Rect(0, 0, 1280, 720), art.barBackground, null);
            Panel(root, new Rect(0, 0, 1280, 720), new Color(0, 0, 0, .22f));
            if (tableTex != null) Raw(root, new Rect(0, 0, 1280, 720), tableTex, tableMat);
            else Panel(root, new Rect(35, 240, 1210, 355), new Color(.015f, .22f, .19f));
        }
        // Vuorossa olevan hahmon kultainen hehku (pehmeä säteittäinen kuva, luodaan koodilla).
        glowTexture = MakeGlowTexture();
        for (int i = 1; i <= 4; i++)
        {
            var c = GlowCenter[i]; var g = Raw(root, new Rect(c.x - c.z / 2, c.y - c.w / 2, c.z, c.w), glowTexture, null);
            g.color = new Color(1f, .78f, .25f, .0f); g.gameObject.name = "Vuorohehku"; turnGlows[i] = g; g.gameObject.SetActive(false);
        }
        if (art.originalLogo != null) Raw(root, new Rect(25, 10, 290, 95), art.originalLogo, null);
        else Label(root, new Rect(25, 10, 290, 65), "TOTAL FIST", 48, Gold);
        Label(root, new Rect(35, 98, 285, 25), "TEXAS HOLD'EM", 21, Gold);
        Box(root, new Rect(1100, 15, 165, 37));
        Label(root, new Rect(1102, 16, 161, 35), "BLINDIT " + settings.smallBlind + " / " + settings.bigBlind, 20, Color.white);
        leave = Button(new Rect(1070, 62, 195, 38), "Poistu pöydästä", 0, host.Leave);
        walletText = Label(root, new Rect(1000, 105, 265, 30), "", 19, Gold);
        for (int i = 0; i < 5; i++)
        {
            var pos = SeatPos[i]; var rect = new Rect(pos.x, pos.y, i == 4 ? 145 : 140, 54);
            Box(root, rect); highlights[i] = Frame(root, new Rect(rect.x - 5, rect.y - 5, rect.width + 10, rect.height + 10));
            names[i] = Label(root, new Rect(rect.x, rect.y + 3, rect.width, 20), "", 19, Color.white);
            stacks[i] = Label(root, new Rect(rect.x, rect.y + 22, rect.width, 29), "", 27, Gold);
            bets[i] = Label(root, new Rect(rect.x - 20, rect.y + 55, rect.width + 40, 24), "", 17, Color.white);
            var badge = Panel(root, new Rect(rect.x - 12, rect.y - 14, 40, 24), new Color(.95f, .85f, .45f)); badge.name = "Blindimerkki";
            blindTexts[i] = Label(badge.transform, new Rect(0, 0, 40, 24), "", 19, Color.black); blindBadges[i] = badge.gameObject;
            if (i > 0)
            {
                // Puhekupla: näyttää kuka miettii ja mitä botti teki.
                var bubble = Panel(root, new Rect(rect.x + rect.width / 2 - 85, rect.y - 66, 170, 30), new Color(.04f, .03f, .02f, .93f)); bubble.name = "Puhekupla";
                bubbleTexts[i] = Label(bubble.transform, new Rect(0, 0, 170, 30), "", 21, Gold); bubbles[i] = bubble.gameObject; bubble.gameObject.SetActive(false);
            }
        }
        Box(root, new Rect(560, 296, 165, 44));
        pot = Label(root, new Rect(564, 299, 157, 37), "", 27, Gold);
        phase = Label(root, new Rect(460, 271, 360, 24), "", 18, Color.white);
        dealer = new GameObject("Jakajan merkki", typeof(RectTransform)).GetComponent<RectTransform>(); dealer.SetParent(root, false);
        SetRect(dealer, new Rect(735, 275, 80, 40));
        if (art.dealerMarker != null) { var im = dealer.gameObject.AddComponent<Image>(); im.sprite = art.dealerMarker; im.preserveAspect = true; im.raycastTarget = false; }
        else { Panel(dealer, new Rect(0, 0, 32, 32), new Color(.95f, .89f, .72f)); Label(dealer, new Rect(0, 0, 32, 32), "D", 25, Color.black); }
        message = Label(root, new Rect(110, 554, 440, 50), "", 22, Color.white);
        Panel(root, new Rect(0, 606, 1280, 114), new Color(.025f, .025f, .025f, .98f));
        status = Label(root, new Rect(18, 620, 265, 60), "", 30, Gold);
        fold = Button(new Rect(305, 617, 220, 62), "Luovuta", 1, () => Submit(PokerAction.Fold));
        call = Button(new Rect(545, 617, 225, 62), "", 0, () => Submit(host.Cash.Table.Legal(0).Check ? PokerAction.Check : PokerAction.Call)); callText = call.GetComponentInChildren<Text>();
        raise = Button(new Rect(790, 617, 245, 62), "", 2, () => Submit(PokerAction.RaiseTo)); raiseText = raise.GetComponentInChildren<Text>();
        allIn = Button(new Rect(1050, 617, 210, 45), "All-in", 1, () => Submit(PokerAction.AllIn));
        deal = Button(new Rect(305, 617, 290, 62), "Jaa seuraava jako", 0, host.Deal);
        rebuy = Button(new Rect(615, 617, 320, 62), "Lisäosto " + settings.buyIn + " mk", 2, host.Rebuy);
        minus = Button(new Rect(1035, 680, 40, 32), "−", 0, () => Adjust(-settings.bigBlind));
        plus = Button(new Rect(1215, 680, 40, 32), "+", 0, () => Adjust(settings.bigBlind));
        amountText = Label(root, new Rect(1078, 676, 134, 39), "", 24, Gold);
        Label(root, new Rect(790, 683, 235, 29), "Korotus yhteensä (mk)", 17, Color.white);
        slider = MakeSlider(new Rect(548, 692, 220, 16));
        slider.onValueChanged.AddListener(v => { if (!rendering) { raiseTo = Mathf.RoundToInt(v); UpdateRaiseText(); } });
        Label(root, new Rect(20, 692, 510, 22), "← → valitse   ↑ ↓ summa   Enter / A vahvista   Esc / B poistu", 15, new Color(.8f, .76f, .65f));
        if (settings.cardPrefab != null) { worldCards = gameObject.AddComponent<PokerCardWorldLayer>(); worldCards.Build(settings, root); }
        foreach (var es in FindObjectsByType<EventSystem>(FindObjectsSortMode.None)) if (es.enabled) { oldEventSystems.Add(es); es.enabled = false; }
        var events = new GameObject("Pokerin hiiri", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform);
        eventSystem = events.GetComponent<EventSystem>();
        var module = events.GetComponent<InputSystemUIInputModule>(); module.AssignDefaultActions(); module.move = null; module.submit = null; module.cancel = null;
        inputAfter = Time.unscaledTime + .2f;
        host.Changed += Render; Render();
    }

    static Color Gold => new Color(1, .79f, .38f);
    static void SetRect(RectTransform t, Rect r)
    {
        t.anchorMin = t.anchorMax = new Vector2(0, 1); t.pivot = new Vector2(0, 1);
        t.anchoredPosition = new Vector2(r.x, -r.y); t.sizeDelta = r.size;
    }
    static GameObject Obj(Transform parent, string name, Rect r)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); SetRect(go.GetComponent<RectTransform>(), r); return go;
    }
    static Image Panel(Transform parent, Rect r, Color color)
    {
        var image = Obj(parent, "Panel", r).AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    RawImage Raw(Transform parent, Rect r, Texture texture, Material material, Rect? pixels = null)
    {
        var image = Obj(parent, "Artwork", r).AddComponent<RawImage>(); image.texture = texture; image.material = material; image.raycastTarget = false;
        // Atlas on suunniteltu 1280x426-kokoon; skaalataan tekstuurin todelliseen kokoon.
        if (pixels.HasValue && texture != null) { var p = pixels.Value; float kx = texture.width / 1280f, ky = texture.height / 426f; p = new Rect(p.x * kx, p.y * ky, p.width * kx, p.height * ky); image.uvRect = new Rect(p.x / texture.width, 1 - (p.y + p.height) / texture.height, p.width / texture.width, p.height / texture.height); }
        if (texture == null) image.color = new Color(.09f, .07f, .055f); return image;
    }
    Text Label(Transform parent, Rect r, string value, int size, Color color)
    {
        var text = Obj(parent, "Text", r).AddComponent<Text>(); text.font = font; text.fontSize = size; text.color = color; text.text = value;
        text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }
    void Box(Transform parent, Rect r)
    {
        // Selkeä tumma lohko ilman kultareunaa (kultakehys on varattu napeille ja vuoron korostukselle).
        Panel(parent, r, new Color(0, 0, 0, .92f));
        Panel(parent, new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), new Color(.055f, .05f, .045f, .9f));
    }
    GameObject Frame(Transform parent, Rect r)
    {
        var go = Obj(parent, "Vuorokorostus", r);
        Panel(go.transform, new Rect(0, 0, r.width, 3), Gold); Panel(go.transform, new Rect(0, r.height - 3, r.width, 3), Gold);
        Panel(go.transform, new Rect(0, 0, 3, r.height), Gold); Panel(go.transform, new Rect(r.width - 3, 0, 3, r.height), Gold);
        return go;
    }
    Button Button(Rect rect, string text, int style, UnityEngine.Events.UnityAction action)
    {
        var go = Obj(root, text, rect); var hit = go.AddComponent<Image>(); hit.color = new Color(.08f, .07f, .06f);
        var button = go.AddComponent<Button>(); button.targetGraphic = hit;
        var colors = button.colors; colors.highlightedColor = new Color(1, .78f, .35f); colors.selectedColor = colors.highlightedColor; colors.disabledColor = new Color(.25f, .25f, .25f); button.colors = colors;
        // Tasainen, terävä nappi ilman kultareunaa: väri kertoo tyylin (punainen = luovuta/all-in, sininen = korotus/lisäosto).
        Color fill = style == 1 ? new Color(.46f, .08f, .07f) : style == 2 ? new Color(.07f, .21f, .46f) : new Color(.13f, .12f, .11f);
        Panel(go.transform, new Rect(0, 0, rect.width, rect.height), new Color(fill.r * .5f + .1f, fill.g * .5f + .1f, fill.b * .5f + .1f, 1));
        Panel(go.transform, new Rect(2, 2, rect.width - 4, rect.height - 4), fill);
        var label = Label(go.transform, new Rect(5, 0, rect.width - 10, rect.height), text, rect.height < 40 ? 20 : 29, Color.white);
        if (art.headingFont != null) label.font = art.headingFont;
        // Keep focus above opaque artwork: tinting the underlying hit image is invisible.
        var focus = Frame(go.transform, new Rect(-5, -5, rect.width + 10, rect.height + 10));
        focus.name = "Ohjainvalinta";
        focus.SetActive(false); controlFrames.Add(button, focus);
        button.onClick.AddListener(action); controls.Add(button); return button;
    }
    Slider MakeSlider(Rect rect)
    {
        var go = Obj(root, "Korotus yhteensä", rect); var s = go.AddComponent<Slider>(); s.wholeNumbers = true;
        Panel(go.transform, new Rect(0, 4, rect.width, 8), new Color(.3f, .23f, .13f));
        var handle = Panel(go.transform, new Rect(0, -3, 18, 24), Gold); handle.raycastTarget = true;
        s.handleRect = handle.rectTransform; s.targetGraphic = handle; s.direction = Slider.Direction.LeftToRight;
        var hit = go.AddComponent<Image>(); hit.color = new Color(0, 0, 0, .01f); return s;
    }

    void Submit(PokerAction action) { host.Act(action, raiseTo, shownRevision); }
    void Adjust(int amount)
    {
        var legal = host.Cash.Table.Legal(0); if (!legal.Raise) return;
        raiseTo = Mathf.Clamp(raiseTo + amount, legal.MinimumRaiseTo, legal.MaximumRaiseTo); slider.SetValueWithoutNotify(raiseTo); UpdateRaiseText();
    }
    void UpdateRaiseText()
    {
        amountText.text = raiseTo.ToString("N0");
        raiseText.text = (host.Cash.Table.CurrentBet == 0 ? "Panosta " : "Korota → ") + raiseTo.ToString("N0");
    }
    void Render()
    {
        if (host == null || !host.Visible) return;
        var t = host.Cash.Table; var legal = t.Legal(0); bool between = !t.Running;
        bool newRevision = shownRevision != t.Revision; shownRevision = t.Revision;
        if (newRevision) StartRunoutIfNeeded(t);
        if (runoutActive) between = false;
        for (int i = 1; i < 5; i++)
        {
            string la = t.Seats[i].LastAction ?? "";
            if (la != lastActions[i]) { lastActions[i] = la; if (la.Length > 0 && !la.Contains("blindi")) bubbleUntil[i] = Time.unscaledTime + 2.4f; }
        }
        rendering = true;
        for (int i = 0; i < 5; i++)
        {
            var seat = t.Seats[i]; names[i].text = seat.Name; stacks[i].text = seat.Stack.ToString("N0") + " mk";
            bets[i].text = seat.Folded && seat.InHand ? "Luovutti" : seat.InHand && seat.Stack == 0 ? "ALL-IN · " + seat.Bet : seat.Bet > 0 ? "Panos " + seat.Bet + " mk" : "";
            highlights[i].SetActive(t.Running && t.Actor == i);
        }
        pot.text = "POTTI  " + t.Pot.ToString("N0"); phase.text = between ? "Jaon ulkopuolella" : t.Street.ToString().ToUpperInvariant();
        walletText.text = (host.Demo ? "TESTIRAHA  " : "Lompakko  ") + host.Cash.Wallet.ToString("N0") + " mk";
        status.text = host.Error != null ? "Virhe" : between ? "Seuraava jako" : t.Actor < 0 ? "" : t.Actor == 0 ? "SINUN VUOROSI" : t.Seats[t.Actor].Name + " miettii…";
        if (runoutActive) { status.text = "All-in!"; phase.text = "KORTIT AVAUTUVAT"; }
        message.text = host.Error ?? (runoutActive ? "Jännitys tiivistyy…" : t.Running && t.Actor > 0 ? t.Seats[t.Actor].Name + " miettii…" : t.Message);
        var dp = DealerPos[Math.Max(0, t.Dealer)]; dealer.anchoredPosition = new Vector2(dp.x - dealer.sizeDelta.x / 2, -(dp.y - dealer.sizeDelta.y / 2));
        dealer.gameObject.SetActive(t.Dealer >= 0);
        for (int i = 1; i <= 4; i++) if (turnGlows[i] != null) turnGlows[i].gameObject.SetActive(t.Running && t.Actor == i);
        for (int i = 0; i < 5; i++)
        {
            bool sb = t.Dealer >= 0 && i == t.SmallBlindSeat, bb = t.Dealer >= 0 && i == t.BigBlindSeat;
            blindBadges[i].SetActive(sb || bb); blindTexts[i].text = bb ? "BB" : "SB";
        }
        fold.gameObject.SetActive(legal.Fold); call.gameObject.SetActive(legal.Check || legal.Call);
        raise.gameObject.SetActive(legal.Raise); allIn.gameObject.SetActive(legal.AllIn);
        callText.text = legal.Check ? "Sökö" : "Maksa " + legal.CallAmount.ToString("N0") + " mk";
        allIn.GetComponentInChildren<Text>().text = "All-in → " + legal.MaximumRaiseTo.ToString("N0");
        deal.gameObject.SetActive(between); deal.interactable = t.Seats[0].Stack > 0;
        rebuy.gameObject.SetActive(between); rebuy.interactable = host.Cash.Wallet >= art.buyIn;
        slider.gameObject.SetActive(legal.Raise); minus.gameObject.SetActive(legal.Raise); plus.gameObject.SetActive(legal.Raise); amountText.gameObject.SetActive(legal.Raise);
        if (legal.Raise) { slider.minValue = legal.MinimumRaiseTo; slider.maxValue = legal.MaximumRaiseTo; raiseTo = Mathf.Clamp(raiseTo, legal.MinimumRaiseTo, legal.MaximumRaiseTo); slider.SetValueWithoutNotify(raiseTo); UpdateRaiseText(); }
        rendering = false;
        if (newRevision) RenderCards();
        if (eventSystem != null && (eventSystem.currentSelectedGameObject == null || !eventSystem.currentSelectedGameObject.activeInHierarchy)) SelectFirst();
    }

    // All-in-jaossa loput yhteiset kortit paljastetaan yksi katu kerrallaan jännityksen vuoksi; sääntömoottori ratkaisee jaon heti.
    int BoardShown(PokerTable t) => runoutActive ? revealedBoard : t.Board.Count;
    void StartRunoutIfNeeded(PokerTable t)
    {
        if (boardHand != t.HandNumber) { boardHand = t.HandNumber; revealedBoard = 0; runoutActive = false; if (runoutRoutine != null) StopCoroutine(runoutRoutine); runoutRoutine = null; }
        if (runoutActive) return;
        int target = t.Board.Count, gap = target - revealedBoard;
        bool dramatic = gap > 1 && !(revealedBoard == 0 && target == 3) || (!t.Running && gap > 0);
        if (!dramatic) { revealedBoard = target; return; }
        runoutActive = true; runoutRoutine = StartCoroutine(Runout(t, target));
    }
    IEnumerator Runout(PokerTable t, int target)
    {
        yield return new WaitForSecondsRealtime(1.6f);
        while (revealedBoard < target)
        {
            revealedBoard = revealedBoard == 0 ? 3 : revealedBoard + 1;
            if (host == null || !host.Visible) yield break;
            RenderCards();
            yield return new WaitForSecondsRealtime(revealedBoard < target ? 2.4f : 1.2f);
        }
        runoutActive = false; runoutRoutine = null;
        if (host != null && host.Visible) { shownRevision = -1; Render(); }
    }

    void RenderCards()
    {
        foreach (var g in cards) if (g != null) Destroy(g); cards.Clear();
        var t = host.Cash.Table;
        if (lastHand != t.HandNumber) { lastHand = t.HandNumber; lastCards.Clear(); Array.Clear(lastBets, 0, lastBets.Length); }
        for (int i = 0; i < 5; i++)
            if (t.Seats[i].InHand && !t.Seats[i].Folded)
                    for (int k = 0; k < 2; k++) MakeCard("hole" + i + ":" + k, t.Seats[i].Hole[k], i == 0 || t.Showdown, new Rect(HolePos[i].x + k * (i == 0 ? 72 : 45), HolePos[i].y, i == 0 ? 68 : 43, i == 0 ? 84 : 56), i == 0 ? 8 : 0);
        for (int i = 0; i < 5; i++)
        {
            Rect r = new Rect(454 + i * 78, 356, 70, 87);
            if (i < BoardShown(t)) MakeCard("board" + i, t.Board[i], true, r, 0);
            else { var g = Obj(root, "Yhteisen kortin paikka", r); var im = g.AddComponent<Image>(); im.color = new Color(.12f, .4f, .32f, .15f); im.raycastTarget = false; cards.Add(g); }
        }
        if (worldCards != null) worldCards.Show(t, BoardShown(t), !runoutActive);
        for (int i = 0; i < 5; i++)
        {
            if (t.Running && t.Seats[i].Bet > lastBets[i]) { if (worldCards == null || !worldCards.Active) AnimateChip(i); if (art.chipSound != null) audioSource.PlayOneShot(art.chipSound, GameSettings.SfxVolume); }
            lastBets[i] = t.Seats[i].Bet;
        }
    }
    void MakeCard(string slot, int id, bool faceUp, Rect r, float angle)
    {
        if (worldCards != null && art.cardRenderTexture != null) return;
        var go = Obj(root, "Kortti " + (faceUp ? PokerCards.Text(id) : "selkä"), r); cards.Add(go);
        Sprite sp = faceUp && id < art.cardFaces.Length ? art.cardFaces[id] : art.cardBack;
        if (sp != null) { var im = go.AddComponent<Image>(); im.sprite = sp; im.preserveAspect = true; im.raycastTarget = false; }
        else
        {
            // Temporary live text, NOT newly authored card graphics. Replace with supplied assets.
            var im = go.AddComponent<Image>(); im.color = faceUp ? new Color(.96f, .91f, .78f) : new Color(.24f, .035f, .04f); im.raycastTarget = false;
            var label = Label(go.transform, new Rect(2, 2, r.width - 4, r.height - 4), faceUp ? PokerCards.Text(id) : "TF", faceUp ? 33 : 22,
                faceUp && (PokerCards.Suit(id) == 1 || PokerCards.Suit(id) == 2) ? new Color(.65f, .02f, .03f) : faceUp ? Color.black : Gold);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        go.transform.localRotation = Quaternion.Euler(0, 0, angle);
        int visibleId = faceUp ? id : -1;
        if (!lastCards.TryGetValue(slot, out int previous) || previous != visibleId)
        {
            StartCoroutine(CardAnimation(go.GetComponent<RectTransform>(), r, faceUp));
            if (art.dealSound != null) audioSource.PlayOneShot(art.dealSound, GameSettings.SfxVolume * .4f);
        }
        lastCards[slot] = visibleId;
    }
    IEnumerator CardAnimation(RectTransform card, Rect target, bool flip)
    {
        float elapsed = 0;
        while (card != null && elapsed < .24f)
        {
            float t = elapsed / .24f;
            card.anchoredPosition = Vector2.Lerp(new Vector2(640, -290), new Vector2(target.x, -target.y), 1 - Mathf.Pow(1 - t, 3));
            card.localScale = new Vector3(flip ? Mathf.Max(.08f, Mathf.Abs(Mathf.Cos(t * Mathf.PI))) : 1, 1, 1);
            elapsed += Time.unscaledDeltaTime; yield return null;
        }
        if (card != null) { card.anchoredPosition = new Vector2(target.x, -target.y); card.localScale = Vector3.one; }
    }
    void AnimateChip(int seat)
    {
        var pos = SeatPos[seat]; var go = Obj(root, "Pelimerkkien siirto", new Rect(pos.x + 60, pos.y + 30, 22, 22)); cards.Add(go);
        if (art.chip != null) { var im = go.AddComponent<Image>(); im.sprite = art.chip; im.preserveAspect = true; im.raycastTarget = false; }
        else Label(go.transform, new Rect(0, 0, 25, 25), "●", 24, Gold);
        StartCoroutine(ChipAnimation(go.GetComponent<RectTransform>()));
    }
    IEnumerator ChipAnimation(RectTransform chip)
    {
        Vector2 from = chip.anchoredPosition; float elapsed = 0;
        while (chip != null && elapsed < .35f) { chip.anchoredPosition = Vector2.Lerp(from, new Vector2(625, -325), elapsed / .35f); elapsed += Time.unscaledDeltaTime; yield return null; }
        if (chip != null) Destroy(chip.gameObject);
    }

    List<Button> ActiveControls() => controls.Where(b => b != null && b.gameObject.activeInHierarchy && b.interactable).ToList();
    static Texture2D MakeGlowTexture()
    {
        const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Pow(Mathf.Clamp01(1 - d), 1.7f);
            px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
        }
        tex.SetPixels32(px); tex.Apply(false, true); return tex;
    }
    void UpdateBubbles()
    {
        if (host == null || !host.Visible || host.Cash.Table == null) return;
        var t = host.Cash.Table;
        for (int i = 1; i < 5; i++)
        {
            if (bubbles[i] == null) continue;
            string text = t.Running && t.Actor == i ? "Miettii…" : Time.unscaledTime < bubbleUntil[i] ? lastActions[i] : null;
            bubbles[i].SetActive(!string.IsNullOrEmpty(text)); if (text != null) bubbleTexts[i].text = text;
        }
    }
    void LateUpdate()
    {
        UpdateBubbles();
        float pulse = .42f + .13f * Mathf.Sin(Time.unscaledTime * 4f);
        for (int i = 1; i <= 4; i++) if (turnGlows[i] != null && turnGlows[i].gameObject.activeSelf) turnGlows[i].color = new Color(1f, .78f, .25f, pulse);
        if (eventSystem == null) return;
        var selected = eventSystem.currentSelectedGameObject;
        var selectedButton = selected != null ? selected.GetComponent<Button>() : null;
        if (selectedButton == null || !selectedButton.interactable || !selectedButton.gameObject.activeInHierarchy)
        {
            SelectFirst();
            selected = eventSystem.currentSelectedGameObject;
        }
        foreach (var pair in controlFrames)
            pair.Value.SetActive(pair.Key != null && pair.Key.interactable && pair.Key.gameObject.activeInHierarchy && pair.Key.gameObject == selected);
    }
    void SelectFirst()
    {
        var list = ActiveControls(); if (list.Count == 0) return;
        // Oletus: Sökö/Maksa omalla vuorolla, muuten Jaa seuraava jako. Poistu pöydästä ei koskaan ole oletusvalinta.
        Button action = list.Contains(call) ? call : list.Contains(deal) ? deal : list.FirstOrDefault(b => b != leave);
        if (action == null) { if (eventSystem != null) eventSystem.SetSelectedGameObject(null); return; }
        selection = list.IndexOf(action); action.Select();
    }
    void Update()
    {
        if (host == null || !host.Visible || Time.unscaledTime < inputAfter) return;
        // Actual fit scale, independent of display aspect ratio.
        var frame = root.parent as RectTransform;
        root.localScale = Vector3.one * Mathf.Min(frame.rect.width / 1280f, frame.rect.height / 720f);
        var kb = Keyboard.current; var pad = CoopInput.PadFor(0);
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) { host.Leave(); return; }
        float x = 0, y = 0;
        if (kb != null) { x += (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0); y += (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0); }
        if (pad != null) { x += pad.dpad.x.ReadValue() + pad.leftStick.x.ReadValue(); y += pad.dpad.y.ReadValue() + pad.leftStick.y.ReadValue(); }
        if (Mathf.Abs(x) < .5f && Mathf.Abs(y) < .5f) repeatAt = 0;
        if (Time.unscaledTime >= repeatAt && (Mathf.Abs(x) >= .5f || Mathf.Abs(y) >= .5f))
        {
            repeatAt = Time.unscaledTime + .2f;
            if (Mathf.Abs(y) > Mathf.Abs(x)) Adjust(y > 0 ? art.bigBlind : -art.bigBlind);
            else
            {
                var list = ActiveControls(); if (list.Count > 0) { int current = list.FindIndex(b => b.gameObject == eventSystem.currentSelectedGameObject); selection = (Mathf.Max(0, current) + (x > 0 ? 1 : list.Count - 1)) % list.Count; list[selection].Select(); }
            }
        }
        if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
        {
            var selected = eventSystem.currentSelectedGameObject;
            var b = selected != null ? selected.GetComponent<Button>() : null;
            if (b != null && b.interactable && b.gameObject.activeInHierarchy) { inputAfter = Time.unscaledTime + .15f; b.onClick.Invoke(); }
        }
    }
    void OnDestroy()
    {
        if (host != null) { host.Changed -= Render; if (!HostClosing) host.ViewDestroyed(); }
        foreach (var b in oldEventSystems) if (b != null) b.enabled = true;
    }
}
