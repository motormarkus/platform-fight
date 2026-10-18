using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TotalFist.Poker;
using UnityEngine;
using UnityEngine.SceneManagement;

// Persistent owner: the view NEVER owns money or the running hand.
[DefaultExecutionOrder(-250)]
public sealed class PokerService : MonoBehaviour
{
    public static PokerService Instance { get; private set; }
    static int closedFrame = -10;
    public static bool BlocksCombat => (Instance != null && Instance.Visible) || Time.frameCount <= closedFrame;
    public PokerCashSession Cash { get; private set; } = new PokerCashSession();
    public bool Visible { get; private set; }
    public bool Demo { get; private set; }
    public string Error { get; private set; }
    public PokerPresentationSettings Settings { get; private set; }
    public event Action Changed;
    readonly System.Random random = new System.Random();
    readonly List<PlayerController> disabledPlayers = new List<PlayerController>();
    readonly List<GameHUD> disabledHuds = new List<GameHUD>();
    PlayerController attached;
    PokerView view;
    Task<PokerDecision> thinking;
    int thinkRevision, thinkSeat;
    string thinkSession;
    float nextBotAt, thinkReadyAt, oldTimeScale = 1, lastWalletSave;
    bool quitting, loaded, faulted, walletDirty;
    static readonly PokerStyle[] Styles = { PokerStyle.Balanced, PokerStyle.Loose, PokerStyle.Cautious, PokerStyle.Aggressive, PokerStyle.Balanced };
    string SavePath => Path.Combine(Application.persistentDataPath, Demo ? "total-fist-poker-demo.json" : "total-fist-poker.json");
    PokerCheckpointStore<PokerCashSession> Store => new PokerCheckpointStore<PokerCashSession>(SavePath, s => JsonUtility.ToJson(s), s => JsonUtility.FromJson<PokerCashSession>(s));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; closedFrame = -10; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() { Ensure(); }
    public static PokerService Ensure()
    {
        if (Instance == null) new GameObject("Poker · persistent settlement").AddComponent<PokerService>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        SceneManager.sceneUnloaded += SceneUnloaded;
        // Load lazily: the test bootstrap selects its separate demo ledger first.
    }

    void Load()
    {
        if (loaded) return; loaded = true;
        try
        {
            var restored = Store.Read();
            if (restored == null && Store.Exists) throw new InvalidDataException("Empty poker checkpoint");
            if (restored != null)
            {
                Cash = restored;
                if (Cash == null || Cash.Version != 1 || Cash.Wallet < 0 || (Cash.Joined && Cash.Table == null)) throw new InvalidDataException("Invalid poker checkpoint");
                if (Cash.Table != null && Cash.Table.Running) Cash.Table.AssertConserved();
                if (Cash.Joined) { Cash.Leave(); Commit(); }
            }
        }
        catch (Exception e) { Fail("Pokerin tallennusta ei voitu avata. Tiedosto säilytetty: " + e.Message); }
    }

    public bool Open(PokerPresentationSettings settings, bool demo = false)
    {
        if (Visible || BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen) return false;
        if (Demo != demo)
        {
            if (loaded && Cash.Joined) return false;
            if (loaded && !faulted) Commit();
            Demo = demo; loaded = false; Cash = new PokerCashSession(); attached = null; faulted = false; Error = null;
        }
        Load(); if (faulted) return false;
        AttachPlayer();
        if (Cash.Joined) { Error = "Edellisen jaon selvitys on vielä kesken."; return false; }
        Settings = settings;
        if (settings == null) { Error = "Pokerin asetukset puuttuvat."; return false; }
        if (!Demo && attached == null) { Error = "Pelaajaa ei löytynyt."; return false; }
        int wallet = Demo ? (Cash.WalletInitialized ? Cash.Wallet : 10000) : attached.money;
        if (!Cash.Join(settings.buyIn, settings.smallBlind, settings.bigBlind, wallet)) { Error = "Sisäänostoon tarvitaan " + settings.buyIn + " mk."; return false; }
        Error = null;
        if (!Commit()) return false;
        ApplyWallet(); Visible = true; oldTimeScale = Time.timeScale; Time.timeScale = 0;
        foreach (var p in Players.All) if (p != null && p.enabled) { disabledPlayers.Add(p); p.enabled = false; }
        foreach (var hud in FindObjectsByType<GameHUD>(FindObjectsSortMode.None)) if (hud.enabled) { disabledHuds.Add(hud); hud.enabled = false; }
        var go = new GameObject("Texas Hold'em · UI"); go.transform.SetParent(transform);
        view = go.AddComponent<PokerView>(); view.Build(this, settings);
        Changed?.Invoke(); return true;
    }

    void AttachPlayer()
    {
        if (Demo) return;
        var p = Players.Primary;
        if (p == null || attached == p) return;
        attached = p;
        // Existing game has no wallet save; this companion ledger restores the wallet once
        // poker has been used, including winnings settled while the original scene was gone.
        if (Cash.WalletInitialized) attached.money = Cash.Wallet;
    }
    void SyncWallet() { if (!Demo && attached != null && Cash.WalletInitialized) Cash.Wallet = attached.money; }
    void ApplyWallet() { if (!Demo && attached != null && Cash.WalletInitialized) attached.money = Cash.Wallet; }

    public void Deal()
    {
        if (!Visible || faulted || !Cash.Joined || Cash.Leaving || Cash.Table.Running || Cash.Table.Seats[0].Stack == 0) return;
        Cash.StartHand(random); PersistAndNotify();
    }
    public void Rebuy()
    {
        if (!Visible || faulted) return;
        if (Cash.Rebuy(Settings.buyIn)) PersistAndNotify();
    }
    public void Act(PokerAction action, int amount, int revision)
    {
        if (!Visible || faulted) return;
        if (Cash.Act(0, action, amount, revision)) PersistAndNotify();
    }
    public void Leave()
    {
        if (!Visible) return;
        if (!faulted) { Cash.Leave(); PersistAndNotify(); }
        HideView();
    }
    void HideView()
    {
        if (!Visible) return;
        Visible = false; closedFrame = Time.frameCount + 1;
        foreach (var p in disabledPlayers) if (p != null) p.enabled = true; disabledPlayers.Clear();
        foreach (var hud in disabledHuds) if (hud != null) hud.enabled = true; disabledHuds.Clear();
        Time.timeScale = oldTimeScale;
        if (view != null) { view.HostClosing = true; Destroy(view.gameObject); view = null; }
        Changed?.Invoke();
    }
    public void ViewDestroyed() { if (Visible && !quitting) Leave(); }
    void SceneUnloaded(Scene scene) { if (Visible) Leave(); else if (loaded && !faulted) Commit(); }

    void Update()
    {
        // Do not touch a gameplay ledger merely by entering the isolated test scene.
        if (!loaded && FindFirstObjectByType<PokerTestBootstrap>() == null) Load();
        if (!loaded || faulted) return;
        AttachPlayer();
        if (Cash.Joined && Cash.Leaving) SyncWallet();
        if (thinking != null && thinking.IsCompleted && Time.unscaledTime >= thinkReadyAt)
        {
            var task = thinking; thinking = null;
            if (task.IsFaulted) { Fail("Botin päätös epäonnistui: " + task.Exception.GetBaseException().Message); return; }
            if (Cash.Joined && Cash.SessionId == thinkSession && Cash.Table.Revision == thinkRevision && Cash.Table.Actor == thinkSeat)
            {
                var d = task.Result;
                if (!Cash.Act(thinkSeat, d.Action, d.RaiseTo, thinkRevision)) { Fail("Botti tarjosi laittoman toiminnon."); return; }
                PersistAndNotify(); nextBotAt = Time.unscaledTime + (Visible ? .8f : .01f);
            }
        }
        if (thinking != null || !Cash.Joined || !Cash.Table.Running || Time.unscaledTime < nextBotAt) return;
        if (Cash.Table.Actor == 0) return;
        thinkSeat = Cash.Table.Actor; thinkRevision = Cash.Table.Revision; thinkSession = Cash.SessionId;
        var obs = PokerBot.Observe(Cash.Table, thinkSeat); var style = Styles[thinkSeat]; int seed = random.Next();
        // Näkyvässä pöydässä botti "miettii" hetken, jotta vuoron kierto ja valinta ehtii nähdä.
        thinkReadyAt = Time.unscaledTime + (Visible ? 1f + (float)random.NextDouble() * 1.4f : 0f);
        thinking = Task.Run(() => PokerBot.Decide(obs, style, new System.Random(seed)));
    }

    void LateUpdate()
    {
        if (!loaded || faulted || Demo || attached == null || !Cash.WalletInitialized || Visible) return;
        if (attached.money != Cash.Wallet)
        {
            SyncWallet(); walletDirty = true;
        }
        if (walletDirty && Time.unscaledTime - lastWalletSave > .5f) { Commit(); lastWalletSave = Time.unscaledTime; }
    }

    void PersistAndNotify() { if (Commit()) { ApplyWallet(); Changed?.Invoke(); } }
    bool Commit()
    {
        if (faulted) return false;
        try
        {
            Store.Write(Cash); walletDirty = false; return true;
        }
        catch (Exception e) { Fail("Pokerin tallennus epäonnistui; rahatoiminnot pysäytetty: " + e.Message); return false; }
    }
    void Fail(string message) { Error = message; faulted = true; Debug.LogError(message); Changed?.Invoke(); }

    void OnApplicationQuit()
    {
        quitting = true;
        if (!loaded || faulted) return;
        SyncWallet();
        if (Cash.Joined)
        {
            Cash.Leave(); int guard = 1000;
            while (Cash.Joined && Cash.Table.Running && guard-- > 0)
            {
                int seat = Cash.Table.Actor;
                var d = PokerBot.Decide(PokerBot.Observe(Cash.Table, seat), Styles[seat], random, 8);
                Cash.Act(seat, d.Action, d.RaiseTo);
            }
            Cash.CompleteExit();
        }
        Commit(); HideView(); ApplyWallet();
    }
    void OnDestroy()
    {
        SceneManager.sceneUnloaded -= SceneUnloaded;
        if (Instance != this) return;
        if (!quitting && loaded && !faulted) { SyncWallet(); if (Cash.Joined) Cash.Leave(); Commit(); }
        HideView(); Instance = null;
    }
}
