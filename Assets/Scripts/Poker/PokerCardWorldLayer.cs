using System.Collections;
using System.Collections.Generic;
using TotalFist.Poker;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

// Isolated perspective pass. Positions use approved reference pixels, not model-dependent units.
public sealed class PokerCardWorldLayer : MonoBehaviour
{
    PokerPresentationSettings settings;
    Camera camera3D;
    Transform world;
    readonly Dictionary<string, PokerCardPresenter> cards = new Dictionary<string, PokerCardPresenter>();
    readonly Dictionary<string, int> shown = new Dictionary<string, int>();
    readonly Dictionary<string, int> animations = new Dictionary<string, int>();
    readonly List<GameObject> chips = new List<GameObject>();
    readonly int[] lastBets = new int[5];
    int lastHand = -1, animationId;
    static readonly Vector2[] Holes = { new Vector2(598, 496), new Vector2(190, 395), new Vector2(300, 270), new Vector2(945, 270), new Vector2(1000, 395) };
    static readonly Vector2[] Stacks = { new Vector2(775, 535), new Vector2(195, 452), new Vector2(420, 338), new Vector2(860, 340), new Vector2(1180, 448) };
    static readonly Vector2[] Bets = { new Vector2(770, 482), new Vector2(300, 420), new Vector2(505, 342), new Vector2(775, 344), new Vector2(1040, 430) };
    RenderTexture runtimeTexture;
    AudioSource audioOut; AudioClip dealClip, chipClip;
    // Äänet: asetuksen klippi, muuten Resources/Poker/Sfx.
    void Play(AudioClip clip, float volume = 1f) { if (clip != null && audioOut != null) audioOut.PlayOneShot(clip, GameSettings.SfxVolume * volume); }
    void OnDestroy() { if (runtimeTexture != null) { runtimeTexture.Release(); Destroy(runtimeTexture); } }
    public bool Active => camera3D != null;
    public void Build(PokerPresentationSettings art, RectTransform ui)
    {
        settings = art;
        audioOut = gameObject.AddComponent<AudioSource>(); audioOut.playOnAwake = false; audioOut.ignoreListenerPause = true;
        dealClip = art.dealSound != null ? art.dealSound : Resources.Load<AudioClip>("Poker/Sfx/CardDeal");
        chipClip = art.chipSound != null ? art.chipSound : Resources.Load<AudioClip>("Poker/Sfx/ChipClack");
        if (art.cardRenderTexture == null || art.cardPrefab == null) return;
        world = new GameObject("3D card stage").transform; world.SetParent(transform); world.position = new Vector3(0, 0, 10000);
        var cameraGo = new GameObject("Poker card perspective camera"); cameraGo.transform.SetParent(world, false); cameraGo.transform.localPosition = new Vector3(0, 0, -12);
        camera3D = cameraGo.AddComponent<Camera>(); camera3D.clearFlags = CameraClearFlags.SolidColor; camera3D.backgroundColor = Color.clear;
        camera3D.cullingMask = 1 << 30; camera3D.nearClipPlane = .1f; camera3D.farClipPlane = 30; camera3D.fieldOfView = 45;
        camera3D.allowHDR = false; camera3D.allowMSAA = true;
        // Oma suurempi ja reunoiltaan pehmennetty kuva (asset on 1920x1080 ilman MSAA:ta, mikä näytti pikselöityneeltä).
        runtimeTexture = new RenderTexture(2560, 1440, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, filterMode = FilterMode.Bilinear, name = "Poker cards (runtime)" };
        runtimeTexture.Create(); camera3D.targetTexture = runtimeTexture;
        var cameraData = camera3D.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing = false; cameraData.renderShadows = false;
        var raw = new GameObject("3D cards and chips", typeof(RectTransform), typeof(RawImage)); raw.transform.SetParent(ui, false);
        var rt = raw.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var image = raw.GetComponent<RawImage>(); image.texture = runtimeTexture; image.raycastTarget = false;
    }
    public void Show(PokerTable table, int boardCount = -1, bool highlight = false)
    {
        if (boardCount < 0 || boardCount > table.Board.Count) boardCount = table.Board.Count;
        if (!Active) return;
        if (lastHand != table.HandNumber)
        {
            lastHand = table.HandNumber;
            foreach (var p in cards.Values) if (p != null) Destroy(p.gameObject);
            cards.Clear(); shown.Clear(); animations.Clear(); System.Array.Clear(lastBets, 0, lastBets.Length);
        }
        // Loppunäytös: voittokäsien viisi korttia nostetaan esiin, muut himmennetään.
        HashSet<int> win = null;
        if (highlight && table.Showdown && !table.Running && table.Awards.Count > 0)
        {
            win = new HashSet<int>();
            foreach (var award in table.Awards) foreach (int w in award.Winners)
                foreach (int c in PokerCards.BestFive(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(table.Seats[w].Hole, table.Board)))) win.Add(c);
        }
        var wanted = new HashSet<string>();
        for (int i = 0; i < boardCount; i++)
        {
            string slot = "board" + i; wanted.Add(slot);
            PutCard(slot, table.Board[i], true, new Vector2(489 + i * 78, 394), 70, 0, win == null ? 0 : win.Contains(table.Board[i]) ? 1 : -1, (i < 3 ? i : 0) * .12f);
        }
        for (int i = 0; i < table.Seats.Length; i++)
            if (table.Seats[i].InHand && !table.Seats[i].Folded)
                for (int k = 0; k < 2; k++)
                {
                    string slot = "hole" + i + ":" + k; wanted.Add(slot);
                    PutCard(slot, table.Seats[i].Hole[k], i == 0 || table.Showdown,
                        Holes[i] + new Vector2(k * (i == 0 ? 63 : 45), 0), i == 0 ? 60 : 43, k == 0 ? -6 : 6,
                        win == null ? 0 : win.Contains(table.Seats[i].Hole[k]) ? 1 : -1,
                        (k * 5 + (i - table.Dealer - 1 + 10) % 5) * .09f);
                }
        var remove = new List<string>();
        foreach (var pair in cards) if (!wanted.Contains(pair.Key)) { if (pair.Value != null) Destroy(pair.Value.gameObject); remove.Add(pair.Key); }
        foreach (string slot in remove) { cards.Remove(slot); shown.Remove(slot); animations.Remove(slot); }
        foreach (var go in chips) if (go != null) Destroy(go); chips.Clear();
        for (int i = 0; i < table.Seats.Length; i++)
        {
            ChipPile(table.Seats[i].Stack, Stacks[i]); ChipPile(table.Seats[i].Bet, Bets[i]);
            if (table.Running && table.Seats[i].Bet > lastBets[i]) AnimateChip(i, table.Seats[i].Bet - lastBets[i]);
            lastBets[i] = table.Seats[i].Bet;
        }
        int collected = table.Pot;
        foreach (var s in table.Seats) collected -= s.Bet;
        ChipPile(collected, new Vector2(520, 312));
    }

    Vector3 Position(Vector2 pixel)
    {
        var ray = camera3D.ViewportPointToRay(new Vector3(pixel.x / 1280f, 1 - pixel.y / 720f));
        var plane = new Plane(Vector3.back, world.position);
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : world.position;
    }
    float ScaleForWidth(float pixels)
    {
        float width = 24 * Mathf.Tan(camera3D.fieldOfView * Mathf.Deg2Rad * .5f) * (1280f / 720f);
        return width * pixels / 1280f;
    }
    static void Layer(GameObject go) { foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30; }
    static void Pose(Transform t, Vector3 p, Quaternion r, float scale) { t.position = p; t.rotation = r; t.localScale = Vector3.one * scale; }

    void PutCard(string slot, int card, bool faceUp, Vector2 pixel, float width, float turn, int emphasis = 0, float delay = 0)
    {
        bool fresh = !cards.TryGetValue(slot, out var presenter) || presenter == null;
        if (fresh) { presenter = Instantiate(settings.cardPrefab, world); cards[slot] = presenter; Layer(presenter.gameObject); }
        Vector3 target = Position(emphasis > 0 ? pixel + new Vector2(0, -14) : pixel) + (emphasis > 0 ? new Vector3(0, 0, -1.2f) : Vector3.zero); Quaternion rotation = Quaternion.Euler(settings.cardTableEuler + new Vector3(0, 0, turn));
        float scale = ScaleForWidth(width) * settings.cardWorldScale * (emphasis > 0 ? 1.2f : 1f);
        int visible = faceUp ? card : -1;
        bool flip = shown.TryGetValue(slot, out int previous) && previous < 0 && faceUp;
        presenter.SetCard(faceUp ? card : -1, faceUp); presenter.SetDim(emphasis < 0 && faceUp); shown[slot] = visible;
        if (fresh || flip)
        {
            int revision = ++animationId; animations[slot] = revision;
            StartCoroutine(CardMotion(slot, revision, presenter.transform, fresh, flip, target, rotation, scale, fresh ? delay : 0));
        }
        else if (!animations.ContainsKey(slot)) Pose(presenter.transform, target, rotation, scale);
    }
    IEnumerator CardMotion(string slot, int revision, Transform card, bool deal, bool flip, Vector3 target, Quaternion rotation, float scale, float delay)
    {
        Vector3 from = deal ? Position(new Vector2(640, 290)) : card.position; float elapsed = 0;
        if (delay > 0 && card != null) { card.localScale = Vector3.zero; card.position = from; yield return new WaitForSecondsRealtime(delay); }
        if (deal || flip) Play(dealClip, deal ? .55f : .4f);
        while (card != null && elapsed < .22f && animations.TryGetValue(slot, out int active) && active == revision)
        {
            float t = elapsed / .22f;
            Pose(card, Vector3.Lerp(from, target, 1 - Mathf.Pow(1 - t, 3)), rotation * Quaternion.Euler(0, flip ? 180 * (1 - t) : 0, 0), scale);
            elapsed += Time.unscaledDeltaTime; yield return null;
        }
        if (card != null && animations.TryGetValue(slot, out int last) && last == revision) { Pose(card, target, rotation, scale); animations.Remove(slot); }
    }
    int ChipIndex(int amount)
    {
        for (int i = Mathf.Min(settings.chipValues.Length, settings.chipPrefabs.Length) - 1; i >= 0; i--)
            if (settings.chipValues[i] > 0 && settings.chipValues[i] <= amount && settings.chipPrefabs[i] != null) return i;
        return -1;
    }
    void ChipPile(int amount, Vector2 center)
    {
        // Cap the visual pile; the full authoritative amount always remains in UI text.
        for (int n = 0; n < 18 && amount > 0; n++)
        {
            int index = ChipIndex(amount); if (index < 0) break;
            amount -= settings.chipValues[index];
            var go = Instantiate(settings.chipPrefabs[index], world); Layer(go); chips.Add(go);
            go.transform.position = Position(center + new Vector2((n % 3) * 18, -(n / 3) * 4)) + new Vector3(0, 0, -(n / 3) * .04f - (n % 3) * .02f);
            go.transform.rotation = Quaternion.Euler(42, 0, -8); go.transform.localScale = Vector3.one * ScaleForWidth(30);
        }
    }
    void AnimateChip(int seat, int amount)
    {
        int index = ChipIndex(amount); if (index < 0) return;
        var go = Instantiate(settings.chipPrefabs[index], world); Layer(go);
        go.transform.rotation = Quaternion.Euler(42, 0, -8); go.transform.localScale = Vector3.one * ScaleForWidth(30);
        Play(chipClip, .6f);
        StartCoroutine(ChipMotion(go, Position(Stacks[seat]), Position(Bets[seat])));
    }
    IEnumerator ChipMotion(GameObject go, Vector3 from, Vector3 to)
    {
        float elapsed = 0;
        while (go != null && elapsed < .3f) { go.transform.position = Vector3.Lerp(from, to, elapsed / .3f); elapsed += Time.unscaledDeltaTime; yield return null; }
        if (go != null) Destroy(go);
    }
}
