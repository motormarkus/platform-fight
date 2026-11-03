using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kuun laboratorion olion ovi: robottipomo ilmestyy käytävältä, kun pelaaja etenee (bossTriggerX), eikä sitä voi kaataa (heroDamageMul), ennen kuin olio on vapautettu.
/// Olio (Koe-olento) odottaa oven takana piilossa: jytisee ja karjuu. Kun pelaaja kävelee oven kohdalle, kysytään "Vapauta olio / Jätä olio rauhaan".
/// Vapautettu olio käy kaikkien kimppuun (myös robottipomon), ja pomo muuttuu tavalliseksi vastukseksi.
/// </summary>
public class BeastCage : MonoBehaviour
{
    public Area here;
    [Tooltip("Robottipomo (aluksi pois päältä): ilmestyy, kun joku pelaaja ylittää bossTriggerX.")] public Enemy boss;
    [Tooltip("Olio (aluksi pois päältä): ilmestyy oven kohdalle vapautuksessa.")] public Enemy beast;
    public float bossTriggerX;
    [Tooltip("Vahvistus (aluksi pois päältä): ilmestyy, kun robottipomo on voitettu; peto raatelee nämä myös.")] public Enemy[] reinforcements;
    [Header("Oven lehdet (nousevat ylös, kun olio vapautetaan)")]
    [Tooltip("Oven lehtien kuva; piirtyy vain aukon sisällä (SpriteMask) ja nousee ylös aukon yläreunan taakse.")] public SpriteRenderer doorLeaf;
    public Vector2 openingCenter, openingSize;
    public float doorRiseTime = 1.6f, beastDelay = 1.3f;
    [Tooltip("Ovi olion huoneeseen (aluksi pois päältä): avautuu, kun olio on vapautettu.")] public GameObject roomDoor;
    [Tooltip("Pomon heron vahingon kerroin ennen vapautusta.")] public float lockedBossDamageMul = 0.02f;
    public float halfWidth = 1.3f, maxDistanceFromWall = 1.2f;
    [Tooltip("Jytinän kuuluvuusetäisyys (x).")] public float hearDistance = 22f;

    bool reinforced;
    bool released, bossOut, open, suppressed; int sel; float bangTimer, growlTimer, hintUntil, savedScale = 1f;
    AudioSource aud; AudioClip[] bangs, growls; bool ownSounds; float growlAt = -1f; PlayerController pc; int openedFrame;
    GUIStyle box, line;

    void Awake()
    {
        BeastReleased = false; BeastDefeated = false;
        aud = gameObject.AddComponent<AudioSource>(); aud.playOnAwake = false; aud.spatialBlend = 0f;
        // oma kaikuinen hakkaus ja karjunta (Resources/Sfx/AlienOvi: paukku*, karju*); varalla tynnyri- ja alien-äänet
        var bl = new System.Collections.Generic.List<AudioClip>(); var l = new System.Collections.Generic.List<AudioClip>();
        foreach (var c in Resources.LoadAll<AudioClip>("Sfx/AlienOvi")) { if (c.name.StartsWith("paukku")) bl.Add(c); else if (c.name.StartsWith("karju")) l.Add(c); }
        ownSounds = bl.Count > 0 && l.Count > 0;
        if (!ownSounds)
        {
            bl.Clear(); l.Clear(); bl.AddRange(Resources.LoadAll<AudioClip>("Sfx/Barrel"));
            foreach (var c in Resources.LoadAll<AudioClip>("Sfx/Alien")) if (c != null && (c.name.StartsWith("attack") || c.name.StartsWith("idle") || c.name == "karjaisu")) l.Add(c);
        }
        bangs = bl.ToArray(); growls = l.ToArray();
        bangTimer = Random.Range(0.8f, 2f); growlTimer = Random.Range(2f, 4f);
        if (beast != null) beast.gameObject.SetActive(false);
        if (boss != null) { boss.gameObject.SetActive(false); boss.heroDamageMul = lockedBossDamageMul; }
        if (doorLeaf != null)
        {
            var tex = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; tex.SetPixels(px); tex.Apply();
            var m = new GameObject("Oviaukon maski").AddComponent<SpriteMask>();
            m.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);   // 1 x 1 yksikköä
            m.transform.SetParent(doorLeaf.transform.parent, false);
            m.transform.position = new Vector3(openingCenter.x, openingCenter.y, 0f); m.transform.localScale = new Vector3(openingSize.x, openingSize.y, 1f);
            doorLeaf.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            doorRestY = doorLeaf.transform.position.y;
        }
    }

    /// Olio on vapautettu (ikkunan varjo lakkaa vilahtelemasta).
    const float BeastDb = 0.891f;   // olion äänet -1 dB
    public static bool BeastReleased;
    /// Peto on voitettu (juoksi pois ruudulta); AlienBrain asettaa.
    public static bool BeastDefeated;
    Area lastArea; float followAt = -1f;
    float doorRestY;

    System.Collections.IEnumerator OpenDoor()
    {
        float t = 0f; bool spawned = false;
        while (t < Mathf.Max(doorRiseTime, beastDelay))
        {
            t += Time.deltaTime;
            if (doorLeaf != null)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / doorRiseTime));
                var dp = doorLeaf.transform.position; dp.y = doorRestY + k * (openingSize.y + 0.3f); doorLeaf.transform.position = dp;
            }
            if (!spawned && t >= beastDelay) { spawned = true; SpawnBeast(); }
            yield return null;
        }
        if (!spawned) SpawnBeast();
    }

    void OnDisable() { if (open) CloseMenu(); }

    void Update()
    {
        if (open) { UpdateMenu(); return; }
        if (PokerService.BlocksCombat) return;
        var p = Players.Nearest(transform.position);
        if (p == null) return;
        if (!bossOut && boss != null && p.transform.position.x >= bossTriggerX) SpawnBoss();
        if (bossOut && !reinforced && boss != null && boss.IsDead) CallReinforcements(p);
        if (released) { FollowPlayer(); return; }
        Rumble(p);
        pc = p;
        bool near = Near(p);
        if (!near) { suppressed = false; return; }
        if (suppressed) return;
        open = true; sel = 0; openedFrame = Time.frameCount;
        savedScale = Time.timeScale; Time.timeScale = 0f;
    }

    /// Vapautettu peto seuraa pelaajaa muihin huoneisiin: alueen vaihtuessa se ilmestyy hetken päästä pelaajan taakse.
    void FollowPlayer()
    {
        if (beast == null || BeastDefeated || !beast.gameObject.activeSelf || beast.IsDead) { lastArea = Area.Current; followAt = -1f; return; }
        var cur = Area.Current;
        if (cur != lastArea) { lastArea = cur; followAt = Time.time + 2f; }
        if (followAt < 0f || Time.time < followAt) return;
        followAt = -1f;
        var p = Players.Nearest(beast.transform.position); if (p == null || cur == null) return;
        if (Mathf.Abs(beast.transform.position.x - p.transform.position.x) < 14f) return;   // on jo lähellä (sama alue)
        float x = p.transform.position.x + (p.transform.position.x > cur.camMinX + 8f ? -7f : 7f);
        x = Mathf.Clamp(x, cur.walkMinX + 1f, cur.walkMaxX - 1f);
        float y = Mathf.Clamp(p.transform.position.y, cur.MinDepthAtX(x, cur.minDepthY) + 0.05f, cur.MaxDepthAtX(x, cur.maxDepthY) - 0.05f);
        beast.transform.position = new Vector3(x, y, 0f);
        beast.gameObject.SetActive(true); beast.WakeUp();
        CameraFollow.Shake(0.2f, 0.4f);
    }

    /// Kuurobotit tulevat pelaajan molemmilta puolilta ruudun reunoilta.
    void CallReinforcements(PlayerController p)
    {
        reinforced = true;
        var a = Area.Current; if (reinforcements == null) return;
        for (int i = 0; i < reinforcements.Length; i++)
        {
            var r = reinforcements[i]; if (r == null) continue;
            float x = p.transform.position.x + (i % 2 == 0 ? -1f : 1f) * (8f + 2.5f * (i / 2));
            float y = p.transform.position.y;
            if (a != null)
            {
                x = Mathf.Clamp(x, a.walkMinX + 1f, a.walkMaxX - 1f);
                y = Mathf.Clamp(y + (i - 1.5f) * 0.35f, a.MinDepthAtX(x, a.minDepthY) + 0.05f, a.MaxDepthAtX(x, a.maxDepthY) - 0.05f);
            }
            r.transform.position = new Vector3(x, y, 0f);
            r.gameObject.SetActive(true); r.WakeUp();
        }
        CameraFollow.Shake(0.15f, 0.3f);
    }

    void SpawnBoss()
    {
        bossOut = true;
        boss.gameObject.SetActive(true); boss.WakeUp();   // ei vihjetekstiä: ikkunassa vilahtava olio kertoo, että ovi kannattaa avata
    }

    /// Oven takaa kuuluva jytinä ja karjunta, niin kauan kuin olio on vielä sisällä.
    void Rumble(PlayerController p)
    {
        float dx = Mathf.Abs(p.transform.position.x - transform.position.x);
        if (dx > hearDistance) return;
        float k = Mathf.Clamp01(1f - dx / hearDistance);
        bangTimer -= Time.deltaTime; growlTimer -= Time.deltaTime;
        if (bangTimer <= 0f)
        {
            bangTimer = Random.Range(1.4f, 3.4f);
            CameraFollow.Shake(0.05f + 0.1f * k, 0.22f);
            if (bangs != null && bangs.Length > 0) { aud.pitch = ownSounds ? Random.Range(0.95f, 1.05f) : Random.Range(0.45f, 0.65f); aud.PlayOneShot(bangs[Random.Range(0, bangs.Length)], (0.35f + 0.55f * k) * BeastDb); }
            if (Random.value < 0.4f) growlAt = Time.time + Random.Range(0.45f, 0.9f);   // karjaisu paukkeen perään
        }
        if (growlAt >= 0f && Time.time >= growlAt)
        {
            growlAt = -1f; growlTimer = Random.Range(4f, 8f);
            if (growls != null && growls.Length > 0) { aud.pitch = ownSounds ? Random.Range(0.92f, 1.05f) : Random.Range(0.75f, 0.9f); aud.PlayOneShot(growls[Random.Range(0, growls.Length)], (0.35f + 0.55f * k) * BeastDb); }
        }
        if (growlTimer <= 0f)
        {
            growlTimer = Random.Range(6f, 11f);
            growlAt = Time.time + 0.05f;   // harvinainen yksinäinen karjunta
        }
    }

    bool Near(PlayerController q)
    {
        Vector3 p = q.transform.position;
        return !q.Riding && !q.HoldingLid && Mathf.Abs(p.x - transform.position.x) <= halfWidth
            && (here == null || p.y >= here.MaxDepthAtX(p.x, here.maxDepthY) - maxDistanceFromWall) && q.AirHeight <= 0.05f;
    }

    void UpdateMenu()
    {
        if (Time.frameCount == openedFrame) return;
        if (Dir() != 0) sel = 1 - sel;
        if (Cancel()) { suppressed = true; CloseMenu(); return; }
        if (!(Confirm() || (pc != null && CoopInput.Pick(pc.playerIndex)))) return;
        CloseMenu();
        if (sel == 0) Release(); else suppressed = true;
    }

    void CloseMenu() { open = false; Time.timeScale = savedScale <= 0f ? 1f : savedScale; }

    void Release()
    {
        released = true; hintUntil = 0f; BeastReleased = true;
        if (roomDoor != null) roomDoor.SetActive(true);
        if (boss != null) boss.heroDamageMul = 1f;
        CameraFollow.Shake(0.45f, 0.8f);
        if (bangs != null && bangs.Length > 0) { aud.pitch = ownSounds ? 1f : 0.5f; aud.PlayOneShot(bangs[Random.Range(0, bangs.Length)], BeastDb); if (ownSounds && growls.Length > 0) aud.PlayOneShot(growls[Random.Range(0, growls.Length)], 0.9f * BeastDb); }
        StartCoroutine(OpenDoor());
    }

    void SpawnBeast()
    {
        if (beast == null) return;
        Vector3 hp = pc != null ? pc.transform.position : transform.position;
        float y = here != null ? Mathf.Clamp(hp.y, here.MinDepthAtX(transform.position.x, here.minDepthY) + 0.05f, here.MaxDepthAtX(transform.position.x, here.maxDepthY) - 0.05f) : hp.y;
        beast.transform.position = new Vector3(transform.position.x + (hp.x <= transform.position.x ? 1.6f : -1.6f), y, 0f);
        beast.gameObject.SetActive(true); beast.WakeUp();
        CameraFollow.Shake(0.3f, 0.5f);
    }

    static int Dir()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; var g = Gamepad.current;
        if ((k != null && (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame)) || (g != null && (g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame))) return 1;
        return 0;
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0;
#endif
    }

    static bool Confirm()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E);
#endif
    }

    static bool Cancel()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q);
#endif
    }

    void OnGUI()
    {
        bool hint = !released && bossOut && Time.unscaledTime < hintUntil && !open;
        if (!open && !hint) return;
        float s = Screen.height / 1080f;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
            line = new GUIStyle(box) { alignment = TextAnchor.MiddleLeft };
        }
        if (hint)
        {
            float w0 = Screen.height * 1.0f, h0 = Screen.height * 0.07f;
            GUI.Box(new Rect((Screen.width - w0) * 0.5f, Screen.height * 0.12f, w0, h0), Loc.T("Robotti on liian vahva! Vapauta olio ovesta."), box);
            return;
        }
        float w = 620f * s, rowH = 62f * s, h = rowH * 3 + 30f * s;
        float x = (Screen.width - w) * 0.5f, y = (Screen.height - h) * 0.5f;
        GUI.Box(new Rect(x, y, w, h), "", box);
        GUI.Label(new Rect(x, y + 10f * s, w, rowH), Loc.T("Vapautetaanko olio?"), box);
        string[] names = { "Vapauta olio", "Jätä olio rauhaan" };
        for (int i = 0; i < names.Length; i++)
            GUI.Label(new Rect(x + 40f * s, y + 10f * s + rowH * (i + 1), w - 60f * s, rowH), (i == sel ? "▶  " : "    ") + Loc.T(names[i]), line);
        GUI.Label(new Rect(x, y + h + 6f * s, w, 40f * s), Loc.T("Ylös / alas valitse   E / Enter hyväksy   Esc takaisin"), box);
    }
}
