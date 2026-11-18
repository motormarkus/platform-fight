using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Aluksen loppukohtaus (ohjaamo): hero taistelee Lokkia vastaan.
///  - Lokin elämät alle damageAt: VAURIO: tärinä, savu, punainen hälytysvalo, varoitustekstit ja hälytysääni.
///  - alle diveAt: SYÖKSY: ruutu kallistuu (CameraFollow.TiltDeg) ja zoomaa, painovoima heikkenee (Area.gravityScale) -> painottomuus; tärinä voimistuu.
///  - Lokki kaatuu: valkoinen välähdys, "alus syöksyy maahan", loppuruutu; Enter käynnistää pelin uudelleen.
/// Tilat nollautuvat, kun scene vaihtuu.
/// </summary>
public sealed class ShipFinale : MonoBehaviour
{
    public Area cockpit; public Enemy lokki;
    [Range(0f, 1f)] public float damageAt = 0.75f, diveAt = 0.45f;
    public float tiltTarget = 10f, zoomTarget = 1.3f, diveGravity = 0.14f;

    enum Phase { Wait, Fight, Damage, Dive, Crash, End }
    Phase phase = Phase.Wait; float phaseAt, nextShake, nextSmoke, nextBeep, warnT; float baseGravity = 1f; bool gravSaved;
    float tilt, zoom = 1f, flash, fadeBlack; int msgIdx;
    AudioClip beep; GUIStyle big, small; Texture2D px, vignette; Sprite smokeSp;
    static readonly string[] Msgs = { "VAROITUS: RUNGON VAURIO", "PAINEENMENETYS", "MOOTTORI 2 SAMMUNUT", "SYÖKSY ALKANUT", "ITSETUHO", "TÖRMÄYS LÄHESTYY" };
    string[] msgLoc;

    void OnDisable() { Restore(); }
    void OnDestroy() { Restore(); }
    void Restore()
    {
        CameraFollow.TiltDeg = 0f; CameraFollow.ZoomMul = 1f; StarDrift.Boost = 1f;
        if (cockpit != null && gravSaved) { cockpit.gravityScale = baseGravity; gravSaved = false; }
    }

    void Go(Phase p) { phase = p; phaseAt = Time.time; }

    void Update()
    {
        if (cockpit == null || lokki == null) return;
        float dt = Time.deltaTime;
        bool inCockpit = Area.Current == cockpit;
        switch (phase)
        {
            case Phase.Wait: if (inCockpit && lokki.isActiveAndEnabled) Go(Phase.Fight); break;
            case Phase.Fight:
                if (lokki.IsDead) { Go(Phase.Crash); break; }
                if (lokki.HealthFraction <= damageAt) { Go(Phase.Damage); StartAlarm(); }
                break;
            case Phase.Damage:
                if (lokki.IsDead) { Go(Phase.Crash); break; }
                if (lokki.HealthFraction <= diveAt) { Go(Phase.Dive); CameraFollow.Shake(0.5f, 1.5f); SaveGravity(); }
                break;
            case Phase.Dive:
                if (lokki.IsDead) Go(Phase.Crash);
                break;
        }
        StarDrift.Boost = phase >= Phase.Crash ? 8f : phase == Phase.Dive ? 4f : phase == Phase.Damage ? 1.6f : 1f;   // tähdet vilistävät nopeammin syöksyssä
        if (phase >= Phase.Damage && phase <= Phase.Dive && inCockpit) DamageFx(dt);
        // kallistus ja zoom pehmeästi; painovoima
        float tt = phase == Phase.Dive ? tiltTarget + Mathf.Sin(Time.time * 0.8f) * 1.2f : (phase == Phase.Crash ? tiltTarget + 6f : 0f), zt = phase == Phase.Dive || phase == Phase.Crash ? zoomTarget : 1f;
        tilt = Mathf.MoveTowards(tilt, tt, 6f * dt); zoom = Mathf.MoveTowards(zoom, zt, 0.25f * dt);
        CameraFollow.TiltDeg = inCockpit ? tilt : 0f; CameraFollow.ZoomMul = inCockpit ? zoom : 1f;
        if (gravSaved) cockpit.gravityScale = phase == Phase.Dive ? Mathf.MoveTowards(cockpit.gravityScale, diveGravity, 0.5f * dt) : (phase >= Phase.Crash ? diveGravity : baseGravity);
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
        bool dive = phase == Phase.Dive; float kShake = dive ? 1f : 0.45f;
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
        var cam = Camera.main; AudioSource.PlayClipAtPoint(beep, cam != null ? cam.transform.position : Vector3.zero, 0.6f * GameSettings.SfxVolume);
    }

    void OnGUI()
    {
        if (phase < Phase.Damage) return;
        float W = Screen.width, H = Screen.height;
        if (px == null)
        {
            px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
            const int N = 128; vignette = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float r = Vector2.Distance(new Vector2(x, y), new Vector2(N * 0.5f, N * 0.5f)) / (N * 0.5f); vignette.SetPixel(x, y, new Color(1f, 0f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.2f, r)))); }
            vignette.Apply();
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold }; big.normal.textColor = new Color(1f, 0.25f, 0.2f);
            small = new GUIStyle(big);
        }
        bool active = phase <= Phase.Dive && Area.Current == cockpit;
        if (active)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (phase == Phase.Dive ? 7f : 4f));
            GUI.color = new Color(1f, 1f, 1f, (phase == Phase.Dive ? 0.55f : 0.35f) * (0.4f + 0.6f * pulse)); GUI.DrawTexture(new Rect(0, 0, W, H), vignette); GUI.color = Color.white;
            big.fontSize = Mathf.RoundToInt(H * 0.05f);
            float a = Mathf.Clamp01(1f - (Time.time - warnT) / 2.2f) * 0.5f + 0.5f; bool blink = Mathf.Sin(Time.time * 9f) > -0.2f;
            if (blink) { GUI.color = new Color(1f, 1f, 1f, a); GUI.Label(new Rect(0, H * 0.08f, W, H * 0.08f), Loc.T(Msgs[msgIdx % Msgs.Length]), big); GUI.color = Color.white; }
        }
        if (flash > 0.001f) { GUI.color = new Color(1f, 1f, 1f, flash); GUI.DrawTexture(new Rect(0, 0, W, H), px); GUI.color = Color.white; }
        if (phase == Phase.End)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeBlack); GUI.DrawTexture(new Rect(0, 0, W, H), px); GUI.color = Color.white;
            if (fadeBlack > 0.6f)
            {
                big.normal.textColor = Color.white; big.fontSize = Mathf.RoundToInt(H * 0.07f);
                GUI.Label(new Rect(0, H * 0.35f, W, H * 0.12f), Loc.T("ALUS SYÖKSYY MAAHAN..."), big);
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
