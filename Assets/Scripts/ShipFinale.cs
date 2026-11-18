using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Aluksen loppukohtaus (ohjaamo): hero taistelee Lokkia vastaan.
///  - Lokin elämät alle damageAt: VAURIO: tärinä, savu, punainen hälytysvalo, varoitustekstit ja hälytysääni.
///  - alle diveAt: SYÖKSY: ruutu kallistuu (CameraFollow.TiltDeg) ja zoomaa, painovoima heikkenee (Area.gravityScale) -> painottomuus; tärinä voimistuu.
///  - Lokki kaatuu: valkoinen välähdys, "alus syöksyy maahan", loppuruutu; Enter käynnistää pelin uudelleen.
/// Tilat nollautuvat, kun scene vaihtuu.
/// </summary>
public sealed class ShipFinale : MonoBehaviour
{
    public Area cockpit; public Enemy lokki; public EscapePod pod;
    [Tooltip("Loppuranta: kapselilla pelastautunut hero siirtyy tänne (Area) ja ilmestyy kohtaan beachSpawn.")] public Area beach; public Vector2 beachSpawn;
    bool arrived; float arrivedAt;
    public float escapeTime = 10f;
    [Range(0f, 1f)] public float damageAt = 0.75f, diveAt = 0.45f;
    [Tooltip("Viimeiset hetket: Lokin elämät alle tämän syöksyssä -> \"Collision imminent\" (hajoava kuulutus).")]
    [Range(0f, 1f)] public float finalAt = 0.15f;
    public float tiltTarget = 10f, zoomTarget = 1.3f, diveGravity = 0.14f;

    enum Phase { Wait, Fight, Damage, Dive, Escape, Pod, Crash, End }
    Phase phase = Phase.Wait; float phaseAt, nextShake, nextSmoke, nextBeep, warnT; float baseGravity = 1f; bool gravSaved;
    float tilt, zoom = 1f, flash, fadeBlack, escapeLeft; int msgIdx; bool podEnd;
    bool Diving => phase == Phase.Dive || phase == Phase.Escape || phase == Phase.Pod;
    static bool InShip() { var a = Area.Current; return a != null && a.areaName != null && a.areaName.StartsWith("Aluksen"); }
    void OpenReturnDoor()
    {   // ohjaamon paluuovi helpoksi löytää: koko syvyys ja leveä alue (kapseli on hytissä, aikaa 10 s)
        foreach (var d in Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d != null && d.name == "Alus: ohjaamosta hyttiin") { d.halfWidth = 6f; d.maxDistanceFromWall = 50f; }
    }
    void LokkiDown() { OpenReturnDoor(); if (pod != null) { Go(Phase.Escape); escapeLeft = escapeTime; pod.Arm(); Interrupt("evacuate_hajoava"); } else { Go(Phase.Crash); Interrupt("evacuate_hajoava"); } }
    AudioClip beep; GUIStyle big, small; Texture2D px, vignette; Sprite smokeSp;
    static readonly string[] Msgs = { "VAROITUS: RUNGON VAURIO", "PAINEENMENETYS", "MOOTTORI 2 SAMMUNUT", "SYÖKSY ALKANUT", "ITSETUHO", "TÖRMÄYS LÄHESTYY" };
    string[] msgLoc;
    // kuulutukset (oma ääni, Tools/ShipVoice/process_voice.py): Resources/Sfx/Alus/<nimi>; ruudulla sama teksti englanniksi
    static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
    {
        { "earth_orbit", "EARTH ORBIT ACHIEVED" }, { "radar_offline", "ALERT: RADAR OFFLINE" }, { "main_power", "ALERT: MAIN POWER INSUFFICIENT" },
        { "hull_breach_airbpressure", "WARNING: HULL BREACH DETECTED - LOSING AIR PRESSURE" }, { "orbit_decaying", "WARNING: ORBIT DECAYING" },
        { "losing_altitude", "WARNING: LOSING ALTITUDE" }, { "collision_imminent_hajoava", "WARNING: COLLISION IMMINENT" }, { "evacuate_hajoava", "EVACUATE" },
    };
    readonly Queue<string> voiceQ = new Queue<string>(); AudioSource voice; float nextVoiceAt, lineUntil; string curLine; bool finalSaid;
    bool altToggle;
    void Say(params string[] names) { foreach (var n in names) voiceQ.Enqueue(n); }
    void Interrupt(params string[] names) { voiceQ.Clear(); if (voice != null) voice.Stop(); nextVoiceAt = 0f; Say(names); }
    void VoiceUpdate()
    {
        if (voice == null) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0f; }
        if (voiceQ.Count == 0 && !voice.isPlaying && Time.time >= nextVoiceAt)
        {   // syöksyssä ja pakovaiheessa kuulutukset toistuvat, ja loppua kohti tiheämmin
            if (phase == Phase.Escape) Say("evacuate_hajoava");
            else if (phase == Phase.Dive) Say(finalSaid ? "collision_imminent_hajoava" : (altToggle = !altToggle) ? "losing_altitude" : "orbit_decaying");
        }
        if (voice.isPlaying || Time.time < nextVoiceAt || voiceQ.Count == 0) return;
        string n = voiceQ.Dequeue(); var clip = Resources.Load<AudioClip>("Sfx/Alus/" + n);
        curLine = Lines.TryGetValue(n, out var tx) ? tx : null;
        if (clip == null) { lineUntil = Time.time + 2.2f; nextVoiceAt = Time.time + 2.4f; return; }
        voice.clip = clip; voice.volume = GameSettings.SfxVolume; voice.Play();
        float gap = phase >= Phase.Escape ? 0.15f : finalSaid ? 0.3f : phase == Phase.Dive ? 0.6f : 0.9f;   // loppua kohti kuulutukset tihenevät
        lineUntil = Time.time + clip.length + 0.6f; nextVoiceAt = Time.time + clip.length + gap;
    }

    void OnDisable() { StopPodVideo(); Restore(); }
    void OnDestroy() { Restore(); }
    void Restore()
    {
        CameraFollow.TiltDeg = 0f; CameraFollow.ZoomMul = 1f; StarDrift.Boost = 1f;
        if (cockpit != null && gravSaved) { cockpit.gravityScale = baseGravity; gravSaved = false; }
    }

    void Go(Phase p) { phase = p; phaseAt = Time.time; }

    /// Kapseli laskeutuu: hero ilmestyy rannalle, alusefektit pois, valkoinen välähdys häipyy.
    void ArriveBeach()
    {
        arrived = true; arrivedAt = Time.time; flash = 0f; beachBlack = 1f;
        Restore(); StarDrift.Boost = 1f; voiceQ.Clear(); if (voice != null) voice.Stop();
        var pc = pod != null ? pod.Release() : null;
        foreach (var p in Players.All)
        {
            if (p == null) continue;
            beach.Apply(p); p.TeleportTo(new Vector3(beachSpawn.x + (p == pc ? 0f : 1.2f), beachSpawn.y, 0f));
        }
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(beachSpawn.x);
        CameraFollow.Shake(0.3f, 0.5f);
        if (pod != null) pod.Disarm();
    }

    void Update()
    {
        if (cockpit == null) return;
        if (lokki == null && phase >= Phase.Fight && phase < Phase.Escape) { LokkiDown(); return; }   // Lokki hävisi ennen kuin kaatuminen huomattiin
        if (lokki == null && phase < Phase.Escape) return;   // Lokki voi hävitä kaaduttuaan: pakovaiheen ajastin ja loppu jatkuvat silti
        float dt = Time.deltaTime;
        if (arrived) { flash = 0f; beachBlack = Mathf.Max(0f, 1f - (Time.time - arrivedAt) / 1.6f); return; }   // rannalla: valkoinen häivytys pois, ei enää alusefektejä
        bool inCockpit = Area.Current == cockpit;
        switch (phase)
        {
            case Phase.Wait: if (inCockpit && lokki.isActiveAndEnabled) { Go(Phase.Fight); nextVoiceAt = Time.time + 1.5f; Say("earth_orbit"); } break;
            case Phase.Fight:
                if (lokki.IsDead) { LokkiDown(); break; }
                if (lokki.HealthFraction <= damageAt) { Go(Phase.Damage); StartAlarm(); Say("radar_offline", "main_power", "hull_breach_airbpressure"); }
                break;
            case Phase.Damage:
                if (lokki.IsDead) { LokkiDown(); break; }
                if (lokki.HealthFraction <= diveAt) { Go(Phase.Dive); CameraFollow.Shake(0.5f, 1.5f); SaveGravity(); Interrupt("orbit_decaying", "losing_altitude"); }
                break;
            case Phase.Dive:
                if (lokki.IsDead) { LokkiDown(); break; }
                if (!finalSaid && lokki.HealthFraction <= finalAt) { finalSaid = true; Interrupt("collision_imminent_hajoava"); }   // viimeiset hetket: kuulutus hajoaa
                break;
            case Phase.Escape:
                escapeLeft -= dt;
                if (pod != null && pod.Entered) { podEnd = true; Go(Phase.Pod); StartPodVideo(); break; }
                if (escapeLeft <= 0f) { if (pod != null) pod.Disarm(); Go(Phase.Crash); }
                break;
            case Phase.Pod:
                if (vp != null)
                {   // välianimaatiota ei voi ohittaa: video loppuun, sitten häivytys mustaan ja rantakohtaus
                    if ((vidDone || Time.time - phaseAt > 20f) && vidFadeAt < 0f) vidFadeAt = Time.time;
                    if (vidFadeAt >= 0f && Time.time - vidFadeAt > 0.9f) { StopPodVideo(); if (beach != null && pod != null) ArriveBeach(); else Go(Phase.End); }
                }
                else if (Time.time - phaseAt > 2.3f) { if (beach != null && pod != null) ArriveBeach(); else Go(Phase.End); }
                break;
        }
        if (phase >= Phase.Fight && (inCockpit || phase >= Phase.Escape)) VoiceUpdate();
        StarDrift.Boost = phase >= Phase.Pod ? 8f : Diving ? 4f : phase == Phase.Damage ? 1.6f : 1f;   // tähdet vilistävät nopeammin syöksyssä
        if (phase >= Phase.Damage && phase <= Phase.Escape && InShip()) DamageFx(dt);
        // kallistus ja zoom pehmeästi; painovoima
        float tt = Diving ? tiltTarget + Mathf.Sin(Time.time * 0.8f) * 1.2f : (phase >= Phase.Crash ? tiltTarget + 6f : 0f), zt = Diving || phase >= Phase.Crash ? zoomTarget : 1f;
        tilt = Mathf.MoveTowards(tilt, tt, 6f * dt); zoom = Mathf.MoveTowards(zoom, zt, 0.25f * dt);
        bool shipFx = inCockpit || (phase >= Phase.Escape && InShip());
        CameraFollow.TiltDeg = shipFx ? tilt : 0f; CameraFollow.ZoomMul = shipFx ? zoom : 1f;
        if (gravSaved) cockpit.gravityScale = Diving ? Mathf.MoveTowards(cockpit.gravityScale, diveGravity, 0.5f * dt) : (phase >= Phase.Crash ? diveGravity : baseGravity);
        if (phase == Phase.Pod)
        {
            float t = Time.time - phaseAt;
            if (vp != null) flash = 0f;
            else if (t < 1.4f) { if (Time.time >= nextShake) { nextShake = Time.time + 0.12f; CameraFollow.Shake(0.45f, 0.25f); } }
            if (vp == null) flash = t > 1.4f ? Mathf.Clamp01((t - 1.4f) / 0.5f) : 0f;
        }
        if (phase == Phase.Crash)
        {
            float t = Time.time - phaseAt;
            if (t < 1.2f) { if (Time.time >= nextShake) { nextShake = Time.time + 0.15f; CameraFollow.Shake(0.7f, 0.3f); } }
            flash = t > 1.2f ? Mathf.Clamp01((t - 1.2f) / 0.5f) : 0f;
            if (t > 2.6f) { Go(Phase.End); }
        }
        if (phase == Phase.End)
        {
            fadeBlack = Mathf.Clamp01((Time.time - phaseAt) / 1.2f); flash = Mathf.Max(0f, flash - dt * 0.8f);
            if (Time.time - phaseAt > 2f && EnterPressed()) { Time.timeScale = 1f; GameMenu.SkipTitleOnce = true; Restore(); SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
        }
    }

    static bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current; return k != null && (k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    void SaveGravity() { if (!gravSaved) { baseGravity = cockpit.gravityScale; gravSaved = true; } cockpit.camJumpLift = Mathf.Max(cockpit.camJumpLift, 0.6f); }

    void StartAlarm()
    {
        msgIdx = 0; warnT = Time.time; nextBeep = 0f; nextShake = 0f; nextSmoke = 0f;
        CameraFollow.Shake(0.4f, 1.2f);
        SaveGravity(); if (phase == Phase.Damage) { /* painovoima vasta syöksyssä */ cockpit.gravityScale = baseGravity; }
    }

    void DamageFx(float dt)
    {
        bool dive = phase >= Phase.Dive; float kShake = dive ? 1f : 0.45f;
        if (Time.time >= nextShake) { nextShake = Time.time + (dive ? 0.28f : 0.7f); CameraFollow.Shake(0.07f + 0.14f * kShake, 0.45f); }
        if (Time.time >= nextSmoke) { nextSmoke = Time.time + (dive ? 0.07f : 0.16f); SpawnSmoke(); }
        if (Time.time >= nextBeep) { nextBeep = Time.time + 1.4f; PlayBeep(); }
        if (Time.time - warnT > 2.2f) { warnT = Time.time; msgIdx = (msgIdx + 1) % (dive ? Msgs.Length : 3); }
    }

    // savu: pehmeitä harmaita läiskiä, jotka nousevat ruudun reunoilta
    void SpawnSmoke()
    {
        var cam = Camera.main; if (cam == null) return;
        if (smokeSp == null)
        {
            const int N = 96; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float r = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f); t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r))); }
            t.Apply(); smokeSp = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 48f);
        }
        float hw = cam.orthographicSize * cam.aspect, hh = cam.orthographicSize; Vector3 c = cam.transform.position;
        bool side = Random.value < 0.5f;
        Vector3 p = side ? new Vector3(c.x + (Random.value < 0.5f ? -1f : 1f) * hw * Random.Range(0.55f, 1.05f), c.y + Random.Range(-hh, hh), 0f) : new Vector3(c.x + Random.Range(-hw, hw), c.y + hh * Random.Range(0.6f, 1.05f), 0f);
        var go = new GameObject("Savu"); go.transform.position = p;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = smokeSp; sr.sortingOrder = 2500;
        float dark = Random.Range(0.12f, 0.35f); sr.color = new Color(dark, dark, dark, 0f);
        var sm = go.AddComponent<SmokePuff>(); sm.life = Random.Range(1.6f, 2.8f); sm.vel = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.4f, 1.6f), 0f); sm.size0 = Random.Range(1.5f, 2.5f); sm.size1 = Random.Range(4f, 7f); sm.alpha = Random.Range(0.3f, 0.55f); sm.sr = sr;
    }

    void PlayBeep()
    {
        if (beep == null)
        {
            const int sr = 22050; int n = (int)(sr * 0.6f); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)sr; float f = t < 0.3f ? 880f : 620f; d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * 0.35f * (1f - Mathf.Clamp01(Mathf.Abs((t % 0.3f) - 0.15f) / 0.15f) * 0.6f); }
            beep = AudioClip.Create("hälytys", n, 1, sr, false); beep.SetData(d, 0);
        }
        var cam = Camera.main; AudioSource.PlayClipAtPoint(beep, cam != null ? cam.transform.position : Vector3.zero, (voice != null && voice.isPlaying ? 0.25f : 0.6f) * GameSettings.SfxVolume);
    }

    // välianimaatio: hero kapseliin -> kapseli irtoaa palavasta aluksesta (StreamingAssets/Finaali/kapseli_pako.mp4)
    VideoPlayer vp; RenderTexture vidRt; bool vidDone, vidShown; float vidFadeAt = -1f, beachBlack;
    void StartPodVideo()
    {
        voiceQ.Clear(); if (voice != null) voice.Stop();
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Finaali/kapseli_pako.mp4");
        if (!System.IO.File.Exists(path)) return;
        vidDone = false; vidShown = false; vidFadeAt = -1f;
        vidRt = new RenderTexture(1280, 720, 0);
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false; vp.isLooping = false; vp.source = VideoSource.Url; vp.url = path;
        vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = vidRt;
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.errorReceived += (v, m) => { Debug.LogWarning("Kapselivideo: " + m); vidDone = true; };
        vp.loopPointReached += v => vidDone = true;
        vp.prepareCompleted += v => { v.SetDirectAudioVolume(0, GameSettings.SfxVolume); v.Play(); vidShown = true; };
        vp.Prepare();
    }
    void StopPodVideo()
    {
        if (vp != null) { vp.Stop(); Destroy(vp); vp = null; }
        if (vidRt != null) { vidRt.Release(); Destroy(vidRt); vidRt = null; }
        vidShown = false;
    }

    void OnGUI()
    {
        if (phase == Phase.Pod && vp != null)
        {
            GUI.depth = -1000;
            if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
            GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), px); GUI.color = Color.white;
            if (vidShown && vidRt != null)
            {
                float W = Screen.width, H = Screen.height, w = Mathf.Min(W, H * 16f / 9f), h = w * 9f / 16f;
                GUI.DrawTexture(new Rect((W - w) * 0.5f, (H - h) * 0.5f, w, h), vidRt, ScaleMode.StretchToFill, false);
            }
            if (vidFadeAt >= 0f) { GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01((Time.time - vidFadeAt) / 0.9f)); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), px); GUI.color = Color.white; }
            return;
        }
        if (arrived && beachBlack > 0.001f)
        {   // rantakohtaus avautuu mustasta
            if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
            GUI.depth = -1000; GUI.color = new Color(0f, 0f, 0f, beachBlack); GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), px); GUI.color = Color.white;
        }
        bool lineOn = curLine != null && Time.time < lineUntil && Area.Current == cockpit;
        if (phase < Phase.Damage)
        {
            if (!lineOn || phase < Phase.Fight) return;
            if (big == null) { big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; small = new GUIStyle(big); }
            big.normal.textColor = new Color(0.6f, 0.9f, 1f); big.fontSize = Mathf.RoundToInt(Screen.height * 0.045f);
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, Screen.height * 0.08f), curLine, big);
            return;
        }
        float W = Screen.width, H = Screen.height;
        if (px == null)
        {
            px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
            const int N = 128; vignette = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float r = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f); vignette.SetPixel(x, y, new Color(1f, 0f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.2f, r)))); }
            vignette.Apply();
            if (big == null) { big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; small = new GUIStyle(big); }
        }
        bool active = phase <= Phase.Escape && (Area.Current == cockpit || (phase == Phase.Escape && InShip()));
        if (phase < Phase.End) big.normal.textColor = new Color(1f, 0.25f, 0.2f);
        if (active)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (Diving ? 7f : 4f));
            GUI.color = new Color(1f, 1f, 1f, (Diving ? 0.55f : 0.35f) * (0.4f + 0.6f * pulse)); GUI.DrawTexture(new Rect(0, 0, W, H), vignette); GUI.color = Color.white;
            big.fontSize = Mathf.RoundToInt(H * 0.05f);
            float a = Mathf.Clamp01(1f - (Time.time - warnT) / 2.2f) * 0.5f + 0.5f; bool blink = Mathf.Sin(Time.time * 9f) > -0.2f;
            if (blink) { GUI.color = new Color(1f, 1f, 1f, a); GUI.Label(new Rect(0, H * 0.08f, W, H * 0.08f), lineOn ? curLine : Loc.T(Msgs[msgIdx % Msgs.Length]), big); GUI.color = Color.white; }
        }
        if (phase == Phase.Escape)
        {
            big.fontSize = Mathf.RoundToInt(H * 0.11f); big.normal.textColor = escapeLeft < 4f && Mathf.Sin(Time.time * 14f) > 0f ? Color.white : new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, H * 0.17f, W, H * 0.14f), Mathf.Max(0, Mathf.CeilToInt(escapeLeft)).ToString(), big);
            big.fontSize = Mathf.RoundToInt(H * 0.035f); big.normal.textColor = Color.white;
            GUI.Label(new Rect(0, H * 0.30f, W, H * 0.06f), Loc.T("PELASTUSKAPSELI HYTISSÄ!"), big);
        }
        if (flash > 0.001f) { GUI.color = new Color(1f, 1f, 1f, flash); GUI.DrawTexture(new Rect(0, 0, W, H), px); GUI.color = Color.white; }
        if (phase == Phase.End)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeBlack); GUI.DrawTexture(new Rect(0, 0, W, H), px); GUI.color = Color.white;
            if (fadeBlack > 0.6f)
            {
                big.normal.textColor = Color.white; big.fontSize = Mathf.RoundToInt(H * 0.07f);
                GUI.Label(new Rect(0, H * 0.35f, W, H * 0.12f), Loc.T(podEnd ? "KAPSELI IRTOAA ALUKSESTA..." : "ALUS SYÖKSYY MAAHAN..."), big);
                big.fontSize = Mathf.RoundToInt(H * 0.11f); GUI.Label(new Rect(0, H * 0.46f, W, H * 0.16f), Loc.T("LOPPU"), big);
                if (Time.time - phaseAt > 2f) { small.normal.textColor = new Color(1f, 1f, 1f, 0.7f); small.fontSize = Mathf.RoundToInt(H * 0.03f); GUI.Label(new Rect(0, H * 0.68f, W, H * 0.06f), Loc.T("Paina Enter aloittaaksesi alusta"), small); }
            }
        }
    }
}

/// <summary>Savupuuska: kasvaa, nousee ja häipyy.</summary>
public sealed class SmokePuff : MonoBehaviour
{
    public float life = 2f, size0 = 1.5f, size1 = 5f, alpha = 0.4f; public Vector3 vel; public SpriteRenderer sr; float age;
    void Update()
    {
        age += Time.deltaTime; if (age >= life) { Destroy(gameObject); return; }
        float u = age / life; transform.position += vel * Time.deltaTime; transform.localScale = Vector3.one * Mathf.Lerp(size0, size1, u);
        var c = sr.color; c.a = alpha * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u)); sr.color = c;
    }
}
