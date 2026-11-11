using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Työmaan bussipysäkin rekka (Assets/Resources/Rekka/rekka.png + rengas.png). E rekan perällä: pelaajat nousevat konttiin, kamera pysyy paikallaan ja rekka ajaa ruudusta ulos
/// renkaat pyörien ja pakokaasua perästä. Perillä (alue "Vankila") siirrytään ovella; jos aluetta ei vielä ole, palataan pysäkille ja rekka palaa paikalleen.
/// Moottorin äänet (valinnaiset): Assets/Resources/Rekka/tyhjakaynti (silmukka, rekka paikallaan) ja Rekka/moottori (silmukka, ajossa; äänenkorkeus nousee nopeuden mukana).
/// Luodaan ajonaikaisesti (WorkSiteExtend), joten valikkoa ei tarvitse ajaa.
/// </summary>
public class TruckRide : MonoBehaviour
{
    public const string AreaName = "Työmaa";
    public const string DestinationArea = "Vankila";   // kohdealueen nimi, kun vankilakenttä on tehty
    public const string DestinationScene = "Vankila";   // scene (valikko 353)
    public static Vector2 DestinationSpawn = new Vector2(85004f, -2.1f);   // sisäpihan vasen pää (x 85000 + 4, y tien keskellä; valikko 353)

    // kuvan mitat ja renkaiden paikat (rekka.png 2172 x 724, y ylhäältä)
    const float ImgW = 2172f, ImgH = 724f, GroundPx = 597f, WheelCenterPx = 523f, WheelDiaPx = 148f;
    static readonly float[] WheelX = { 200f, 375f, 548f, 1332f, 1507f, 2022f };
    const float TruckScale = 0.6f;     // rekan koko suhteessa kuvaan (ppu 78,4 -> pituus ~16,6 yksikköä)
    const float TopSpeed = 17f, Accel = 5.5f;

    Transform body;
    readonly List<Transform> wheels = new List<Transform>();
    float u, k, wheelRadius, speed, leftX0, floorY;
    int order;
    bool busy, away;
    Vector3 startPos;
    Transform exhaust;
    AudioSource idleSrc, driveSrc;
    float msgUntil; string msg;
    float smokeT;
    readonly List<Puff> puffs = new List<Puff>();
    static Sprite puffSprite;

    class Puff { public Transform tr; public SpriteRenderer sr; public Vector2 vel; public float age, life, size; }

    /// Luo rekan työmaan alueelle (scene sc); leftX = rekan perän x, floorY = renkaiden pohjan y, ppu = taustakuvien ppu.
    public static void Create(Scene sc, float leftX, float floorY, float ppu, Area work = null)
    {
        foreach (var g in sc.GetRootGameObjects()) if (g.name == "Rekka") return;
        var tex = Resources.Load<Texture2D>("Rekka/rekka");
        var wtex = Resources.Load<Texture2D>("Rekka/rengas");
        if (tex == null || wtex == null) { Debug.LogWarning("TruckRide: Resources/Rekka/rekka.png tai rengas.png puuttuu."); return; }
        var root = new GameObject("Rekka");
        SceneManager.MoveGameObjectToScene(root, sc);
        var t = root.AddComponent<TruckRide>();
        t.workArea = work;
        t.Build(root.transform, tex, wtex, leftX, floorY, ppu);
    }

    void Build(Transform root, Texture2D tex, Texture2D wtex, float leftX, float fy, float ppu)
    {
        u = 1f / ppu; k = TruckScale; floorY = fy; leftX0 = leftX;
        order = Mathf.RoundToInt(-fy * 100f) - 5;
        root.position = new Vector3(leftX, fy, 0f);
        root.localScale = new Vector3(k, k, 1f);
        startPos = root.position;
        var bodyGo = new GameObject("Kori");
        bodyGo.transform.SetParent(root, false);
        var bsr = bodyGo.AddComponent<SpriteRenderer>();
        bsr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, (ImgH - GroundPx) / ImgH), ppu);
        bsr.sortingOrder = order;
        body = bodyGo.transform;
        // renkaat: halkaisija WheelDiaPx kuvapikseliä
        float wppu = wtex.width / (WheelDiaPx * u);
        var wsprite = Sprite.Create(wtex, new Rect(0, 0, wtex.width, wtex.height), new Vector2(0.5f, 0.5f), wppu);
        for (int i = 0; i < WheelX.Length; i++)
        {
            var w = new GameObject("Rengas " + (i + 1));
            w.transform.SetParent(root, false);
            w.transform.localPosition = new Vector3(WheelX[i] * u, (GroundPx - WheelCenterPx) * u, 0f);
            var sr = w.AddComponent<SpriteRenderer>(); sr.sprite = wsprite; sr.sortingOrder = order + 1;
            wheels.Add(w.transform);
        }
        wheelRadius = WheelDiaPx * 0.5f * u * k;
        var ex = new GameObject("Pakoputki"); ex.transform.SetParent(root, false);
        ex.transform.localPosition = new Vector3(40f * u, (GroundPx - 560f) * u, 0f);   // perän alareuna
        exhaust = ex.transform;
        // äänet (valinnaiset)
        var idle = Resources.Load<AudioClip>("Rekka/tyhjakaynti");
        var drive = Resources.Load<AudioClip>("Rekka/moottori");
        idleSrc = MakeSource(root, idle); driveSrc = MakeSource(root, drive);
    }

    AudioSource MakeSource(Transform parent, AudioClip c)
    {
        if (c == null) return null;
        var a = parent.gameObject.AddComponent<AudioSource>();
        a.clip = c; a.loop = true; a.playOnAwake = false; a.spatialBlend = 0f; a.volume = 0f;
        return a;
    }

    float DoorX => leftX0 + 150f * u * k;      // oven kohta perässä
    float InsideX => leftX0 + 420f * u * k;    // pelaajat seisovat konttiin noustuaan

    bool NearOk(PlayerController q)
    {
        if (q == null || q.Riding || q.AirHeight > 0.05f) return false;
        Vector3 p = q.transform.position;
        return Mathf.Abs(p.x - DoorX) <= 2.2f && p.y >= floorY - 1.8f && p.y <= floorY + 1.2f;
    }

    PlayerController pc;
    bool near;

    void Update()
    {
        float dt = Time.deltaTime;
        UpdatePuffs(dt);
        // paikallaan: tyhjäkäynti ja hiljainen pakokaasu, kun rekka on kameran lähellä
        var cam = Camera.main;
        bool inView = cam != null && Mathf.Abs(cam.transform.position.x - (leftX0 + 8f)) < 26f;
        if (!busy && !away)
        {
            SetSound(idleSrc, inView ? 0.35f : 0f, 1f, dt);
            SetSound(driveSrc, 0f, 1f, dt);
            if (inView) { smokeT -= dt; if (smokeT <= 0f) { smokeT = Random.Range(0.35f, 0.7f); Emit(0.5f); } }
        }
        if (busy || PokerService.BlocksCombat) { near = false; return; }
        pc = Players.Nearest(new Vector3(DoorX, floorY, 0f));
        if (pc == null) { near = false; return; }
        near = NearOk(pc);
        if (!near) return;
        bool pressed = CoopInput.Pick(pc.playerIndex);
        if (Players.Count > 1) foreach (var q in Players.All) if (q != null && !q.GameOver && CoopInput.Pick(q.playerIndex)) { pc = q; pressed = true; break; }
        if (!pressed) return;
        foreach (var q in Players.All) if (q != null && !q.GameOver && !NearOk(q)) { msg = "Molempien pelaajien pitää olla rekan luona."; msgUntil = Time.unscaledTime + 2.5f; return; }
        ScreenFader.Host.StartCoroutine(Ride());
    }

    static void SetSound(AudioSource a, float target, float pitch, float dt)
    {
        if (a == null) return;
        a.pitch = pitch;
        a.volume = Mathf.MoveTowards(a.volume, target * GameSettings.SfxVolume, 2f * dt);
        if (a.volume > 0.001f && !a.isPlaying) a.Play();
        else if (a.volume <= 0.001f && a.isPlaying) a.Pause();
    }

    // ---- pakokaasu ----
    void Emit(float strength)
    {
        if (puffSprite == null) puffSprite = PlayerController.CreateShadowSprite();
        var g = new GameObject("Pakokaasu");
        g.transform.position = exhaust.position;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = puffSprite; sr.sortingOrder = order + 2;
        var p = new Puff { tr = g.transform, sr = sr, age = 0f, life = Random.Range(0.8f, 1.4f), size = Random.Range(0.5f, 0.9f) * (0.6f + strength * 0.7f) };
        p.vel = new Vector2(-Random.Range(1.2f, 2.8f) * (0.6f + strength), Random.Range(0.5f, 1.3f));
        puffs.Add(p);
    }

    void UpdatePuffs(float dt)
    {
        for (int i = puffs.Count - 1; i >= 0; i--)
        {
            var p = puffs[i];
            if (p.tr == null) { puffs.RemoveAt(i); continue; }
            p.age += dt;
            float kk = p.age / p.life;
            if (kk >= 1f) { Destroy(p.tr.gameObject); puffs.RemoveAt(i); continue; }
            p.tr.position += (Vector3)(p.vel * dt);
            p.vel.x *= 1f - 1.2f * dt;
            p.vel.y += 0.4f * dt;
            float s = p.size * Mathf.Lerp(0.7f, 2.2f, kk);
            p.tr.localScale = new Vector3(s, s * 0.75f, 1f);
            float shade = Mathf.Lerp(0.18f, 0.5f, kk);
            p.sr.color = new Color(shade, shade, shade * 1.02f, (1f - kk) * 0.7f);
        }
    }

    // ---- ajo ----
    IEnumerator Ride()
    {
        busy = true; near = false;
        var all = new List<PlayerController>();
        foreach (var q in Players.All) if (q != null && !q.GameOver) all.Add(q);
        foreach (var q in all) q.enabled = false;
        var hidden = new List<SpriteRenderer>();
        // pelaajat kävelevät oven kautta konttiin ja piiloutuvat
        Vector3[] from = new Vector3[all.Count];
        for (int i = 0; i < all.Count; i++) from[i] = all[i].transform.position;
        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            float kk = Mathf.SmoothStep(0f, 1f, t / 0.7f);
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) continue;
                Vector3 to = new Vector3(InsideX + 0.6f * i, floorY - 0.2f, 0f);
                all[i].transform.position = Vector3.Lerp(from[i], to, kk);
                all[i].Face(true);
            }
            yield return null;
        }
        foreach (var q in all) foreach (var sr in q.GetComponentsInChildren<SpriteRenderer>(true)) if (sr.enabled) { sr.enabled = false; hidden.Add(sr); }

        // kamera jää paikalleen
        var follow = CameraFollow.Instance; Transform camTarget = follow != null ? follow.target : null;
        if (follow != null) follow.target = null;
        var cam = Camera.main;
        float camRight = cam != null ? cam.transform.position.x + cam.orthographicSize * cam.aspect : leftX0 + 12f;
        float camLeft = cam != null ? cam.transform.position.x - cam.orthographicSize * cam.aspect : leftX0 - 12f;
        rideCamSize = cam != null ? cam.orthographicSize : 6f;
        float rideCamY = cam != null ? cam.transform.position.y : 1.8f;
        away = true;
        speed = 0f;
        float bodyW = ImgW * u * k;
        float smokeAcc = 0f;
        for (float t = 0f; ; t += Time.deltaTime)
        {
            speed = Mathf.Min(TopSpeed, speed + Accel * Time.deltaTime * (1f + speed * 0.05f));
            transform.position += new Vector3(speed * Time.deltaTime, 0f, 0f);
            foreach (var w in wheels) w.Rotate(0f, 0f, -speed * Time.deltaTime / wheelRadius * Mathf.Rad2Deg);
            // pakokaasua enemmän kiihdytettäessä
            smokeAcc += Time.deltaTime * Mathf.Lerp(14f, 6f, speed / TopSpeed);
            while (smokeAcc >= 1f) { smokeAcc -= 1f; Emit(1f - 0.4f * speed / TopSpeed); }
            SetSound(driveSrc, 0.9f, Mathf.Lerp(0.8f, 1.5f, speed / TopSpeed), Time.deltaTime);
            SetSound(idleSrc, 0f, 1f, Time.deltaTime);
            if (transform.position.x > camRight + 1f) break;   // koko rekka ruudun ulkopuolella
            if (t > 12f) break;
            yield return null;
        }
        // moottorin ääni vaimenee ja ruutu pimenee; sitten kontin sisus ja portin kohtaus
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) { float kk = t / 0.5f; SetSound(driveSrc, 0.9f * (1f - kk), 1.5f, Time.deltaTime); ScreenFader.Alpha = kk; yield return null; }
        ScreenFader.Alpha = 1f;
        yield return Interior(all, follow, camTarget, hidden, rideCamY, rideCamSize);   // minuutti kontissa
        yield return Cutscene();   // päättyy mustaan ruutuun

        bool hasPrison = Area.Find(DestinationArea) != null || Application.CanStreamedLevelBeLoaded(DestinationScene);
        if (hasPrison)
        {
            ResetTruck(follow, camTarget, hidden);
            var d = new GameObject("Rekka: Vankilaan").AddComponent<Door>();
            d.transform.SetParent(transform, false);
            d.manualOnly = true; d.targetName = DestinationArea; d.targetScene = DestinationScene; d.spawnPoint = DestinationSpawn; d.fadeTime = 0.5f; d.alreadyBlack = true;
            d.GoNow(pc);
            busy = false;
            yield break;
        }
        // vankilaa ei vielä ole: paluu pysäkille
        ScreenFader.Alpha = 1f;
        ResetTruck(follow, camTarget, hidden);
        if (workArea != null) workArea.Apply(all.Count > 0 ? all[0] : null);
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null) all[i].TeleportTo(new Vector3(DoorX - 1.2f - 1.0f * i, floorY - 0.6f, 0f));
        if (follow != null) follow.SnapTo(DoorX);
        yield return new WaitForSecondsRealtime(0.6f);
        msg = "Matka vankilalle ei ole vielä valmis. Palasit pysäkille."; msgUntil = Time.unscaledTime + 4f;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime) { ScreenFader.Alpha = 1f - Mathf.Clamp01(t / 0.4f); yield return null; }
        ScreenFader.Alpha = 0f;
        busy = false;
    }

    // ---- ajo kontissa: pelaajat liikkuvat kontin sisällä noin minuutin (oma alue, pelin kamera zoomattuna lähemmäs) ----
    public const float RideSeconds = 60f;
    const float InteriorZoom = 4.5f;   // kameran koko kontissa (oletus 6): hahmot näyttävät isommilta
    Area workArea;
    float rideCamSize = -1f;
    /// Kaikki pelaajat olivat laatikossa piilossa, kun rekka saapui (vartijoiden tarkastus / tappelu hoidetaan vankila-aluella myöhemmin).
    public static bool RideHidden;
    string hint;

    IEnumerator Interior(List<PlayerController> all, CameraFollow follow, Transform camTarget, List<SpriteRenderer> hidden, float cy, float size0)
    {
        var tex = Resources.Load<Texture2D>("Rekka/kontti_sisalta");
        if (tex == null) { yield return new WaitForSecondsRealtime(3f); yield break; }
        var root = new GameObject("Rekan kontti");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        float ppu = tex.height / (2f * size0);
        var sr = new GameObject("Kontin sisus").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(root.transform, false);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        sr.sortingOrder = -10000; sr.transform.position = new Vector3(StageX, cy, 0f);
        float halfW = tex.width / ppu * 0.5f, top = cy + size0;
        var cam = Camera.main;
        float halfView = InteriorZoom * (cam != null ? cam.aspect : 16f / 9f);
        var a = new GameObject("Alue: Rekan kontti").AddComponent<Area>();
        a.transform.SetParent(root.transform, false);
        a.areaName = "Rekan kontti"; a.useSidewalk = false;
        a.maxDepthY = top - 610f / ppu; a.minDepthY = top - 722f / ppu;   // lattia: kuvan rivit ~610–722
        a.walkMinX = StageX - halfW + 2.6f; a.walkMaxX = StageX + halfW - 1.6f;
        a.camMinX = StageX - halfW + halfView; a.camMaxX = Mathf.Max(a.camMinX, StageX + halfW - halfView);
        a.camSize = InteriorZoom; a.camRiseY = 0f; a.camOffsetY = -(size0 - InteriorZoom); a.backScale = 1f; a.depthLimits = new Vector2[0];
        a.music = null;

        // pelaajat konttiin oven viereen; ohjaus takaisin
        if (follow != null) follow.target = camTarget;
        float midY = Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.5f);
        a.Apply(all.Count > 0 ? all[0] : null);
        for (int i = 0; i < all.Count; i++) if (all[i] != null) { all[i].TeleportTo(new Vector3(StageX - halfW + 4.4f + 0.9f * i, midY + 0.4f * i, 0f)); all[i].Face(true); }
        if (follow != null) follow.SnapTo(a.camMinX);
        foreach (var s in hidden) if (s != null) s.enabled = true;
        hidden.Clear();
        foreach (var q in all) if (q != null) q.enabled = true;
        // iso puulaatikko suojana: sen taakse (kauemmas kameraan nähden) mennyttä pelaajaa ei näy; laatikko piirtyy hänen edelleen
        float crateX = StageX - halfW + 13f, crateY = Mathf.Lerp(a.maxDepthY, a.minDepthY, 0.6f);
        var crate = CoverCrate.Create(root.transform, new Vector3(crateX, crateY, 0f));
        bool hasCrate = crate != null;
        RideHidden = false;
        System.Func<PlayerController, bool> behindCrate = q => q != null && CoverCrate.Hides(q.transform.position);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) { ScreenFader.Alpha = 1f - t / 0.6f; yield return null; }
        ScreenFader.Alpha = 0f;

        float nextShake = 0f;
        for (float t = 0f; t < RideSeconds; t += Time.deltaTime)
        {
            // suojassa: kaikki pelaajat laatikon takana?
            hint = null;
            if (hasCrate)
            {
                int behind = 0;
                foreach (var q in all) if (behindCrate(q)) behind++;
                if (behind > 0 && behind == all.Count) hint = "Piilossa laatikon takana";
                else if (t < 8f) hint = "Mene laatikon taakse piiloon (vartijat tarkastavat kontin)";
            }
            if (t >= nextShake) { CameraFollow.Shake(Random.Range(0.03f, 0.07f), 0.35f); nextShake = t + Random.Range(0.5f, 1.4f); }   // tien töyssyt
            SetSound(driveSrc, 0.7f, 0.95f + 0.08f * Mathf.Sin(t * 0.8f), Time.deltaTime);
            yield return null;
        }

        // perillä: pimennys, pelaajat pois ohjauksesta ja piiloon
        hint = null;
        RideHidden = hasCrate && all.Count > 0 && all.TrueForAll(q => behindCrate(q));
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) { ScreenFader.Alpha = t / 0.6f; SetSound(driveSrc, 0.7f * (1f - t / 0.6f), 1f, Time.deltaTime); yield return null; }
        ScreenFader.Alpha = 1f;
        foreach (var q in all) if (q != null) { q.enabled = false; foreach (var s in q.GetComponentsInChildren<SpriteRenderer>(true)) if (s.enabled) { s.enabled = false; hidden.Add(s); } }
        if (follow != null) follow.target = null;
        Destroy(root);
    }

    // ---- kohtaus: kontin sisus ja saapuminen vankilan portille (oma kamera ja lava kaukana pelialueesta, pelin kamera ei liiku) ----
    const float StageX = -3000f;
    const float GatePx = 1672f, GateH = 941f, GateGroundRow = 760f, RoadCropPx = 640f;

    IEnumerator Cutscene()
    {
        var main = Camera.main;
        if (main == null) yield break;
        var camGo = new GameObject("Rekkakamera");
        var c = camGo.AddComponent<Camera>();
        c.CopyFrom(main);
        c.depth = main.depth + 5f; c.orthographicSize = rideCamSize > 0f ? rideCamSize : main.orthographicSize; c.clearFlags = CameraClearFlags.SolidColor; c.orthographic = true;
        float size0 = rideCamSize > 0f ? rideCamSize : main.orthographicSize, ppuG = GateH / (2f * size0);
        var stage = new GameObject("Rekkakohtaus");
        SceneManager.MoveGameObjectToScene(stage, gameObject.scene);
        float camY = 0f, size = size0;
        c.orthographicSize = size; c.transform.position = new Vector3(StageX, camY, -10f);

        c.backgroundColor = new Color(0.07f, 0.08f, 0.11f);

        // 2) portti: kuva keskellä, vasemmalla suikale tietä (kuvan vasen tyhjä osa peilattuina paloina), rekka saapuu vasemmalta ja pysähtyy
        var gtex = Resources.Load<Texture2D>("Vankila/portti");
        if (gtex == null) { Debug.LogWarning("TruckRide: Resources/Vankila/portti.png puuttuu."); Destroy(camGo); Destroy(stage); yield break; }
        c.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        float imgHalfW = GatePx * 0.5f / ppuG;
        if (c.aspect > imgHalfW / size0) { size = imgHalfW / c.aspect; camY = -size0 + size; c.orthographicSize = size; }   // leveä ruutu: zoomataan, ettei kuvan reunan yli näy
        c.transform.position = new Vector3(StageX, camY, -10f);
        int gorder = order - 10;
        var gateSr = new GameObject("Portti").AddComponent<SpriteRenderer>();
        gateSr.transform.SetParent(stage.transform, false);
        gateSr.sprite = Sprite.Create(gtex, new Rect(0, 0, gtex.width, gtex.height), new Vector2(0.5f, 0.5f), ppuG);
        gateSr.sortingOrder = gorder; gateSr.transform.position = new Vector3(StageX, 0f, 0f);
        var piece = Sprite.Create(gtex, new Rect(0, 0, RoadCropPx, gtex.height), new Vector2(0f, 0.5f), ppuG);
        float pw = RoadCropPx / ppuG, gateLeft = StageX - imgHalfW;
        float camLeft = StageX - size * c.aspect;
        for (int i = 0; gateLeft - i * pw > camLeft - 1f; i++)
        {
            var pr = new GameObject("Tiepala").AddComponent<SpriteRenderer>();
            pr.transform.SetParent(stage.transform, false);
            pr.sprite = piece; pr.sortingOrder = gorder;
            bool flip = i % 2 == 0; pr.flipX = flip;
            pr.transform.position = new Vector3(flip ? gateLeft - i * pw : gateLeft - (i + 1) * pw, 0f, 0f);
        }
        float floor = (GateH * 0.5f - GateGroundRow) / ppuG;
        float frontLocal = 2150f * u * k;
        float stopFront = StageX - 0.3f, startFront = camLeft - 2f, D = stopFront - startFront, T = 3.8f;
        float startRear = startFront - frontLocal;
        transform.position = new Vector3(startRear, floor, 0f);
        foreach (var w in wheels) w.localRotation = Quaternion.identity;
        float smokeAcc = 0f;
        for (float t = 0f; t < T; t += Time.deltaTime)
        {
            float rem = 1f - t / T;
            float spd = 2f * D / T * rem;
            transform.position = new Vector3(startRear + D * (1f - rem * rem), floor, 0f);
            foreach (var w in wheels) w.Rotate(0f, 0f, -spd * Time.deltaTime / wheelRadius * Mathf.Rad2Deg);
            smokeAcc += Time.deltaTime * Mathf.Lerp(5f, 12f, rem);
            while (smokeAcc >= 1f) { smokeAcc -= 1f; Emit(0.5f + rem * 0.5f); }
            ScreenFader.Alpha = Mathf.Clamp01(1f - t / 0.6f);
            SetSound(driveSrc, 0.9f * Mathf.Lerp(0.6f, 1f, rem), Mathf.Lerp(0.8f, 1.35f, rem), Time.deltaTime);
            yield return null;
        }
        ScreenFader.Alpha = 0f;
        transform.position = new Vector3(startRear + D, floor, 0f);
        for (int i = 0; i < 6; i++) Emit(1f);   // jarrutuksen pölähdys
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            SetSound(driveSrc, 0f, 0.8f, Time.deltaTime);
            SetSound(idleSrc, 0.5f, 1f, Time.deltaTime);
            if (Random.value < Time.deltaTime * 3f) Emit(0.5f);
            yield return null;
        }
        // loppu: pimennys
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) { ScreenFader.Alpha = Mathf.Clamp01(t / 0.6f); SetSound(idleSrc, 0.5f * (1f - t / 0.6f), 1f, Time.deltaTime); yield return null; }
        ScreenFader.Alpha = 1f;
        SetSound(idleSrc, 0f, 1f, 1f);
        if (driveSrc != null) driveSrc.Stop();
        if (idleSrc != null) idleSrc.Pause();
        Destroy(camGo); Destroy(stage);
    }

    void ResetTruck(CameraFollow follow, Transform camTarget, List<SpriteRenderer> hidden)
    {
        transform.position = startPos; speed = 0f; away = false;
        foreach (var w in wheels) w.localRotation = Quaternion.identity;
        foreach (var sr in hidden) if (sr != null) sr.enabled = true;
        hidden.Clear();
        foreach (var q in Players.All) if (q != null) q.enabled = true;
        if (follow != null) follow.target = camTarget;
    }

    static GUIStyle style;
    void OnGUI()
    {
        if (!busy && Time.unscaledTime >= msgUntil && !near && string.IsNullOrEmpty(hint)) return;
        string text = Time.unscaledTime < msgUntil ? Loc.T(msg) : (!string.IsNullOrEmpty(hint) ? Loc.T(hint) : (near && !busy ? "[E]  " + Loc.T("Nouse rekan konttiin") : null));
        if (string.IsNullOrEmpty(text)) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            style.normal.textColor = Color.white;
        }
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        float textW = style.CalcSize(new GUIContent(text)).x + Screen.height * 0.06f;
        if (textW > w) w = Mathf.Min(textW, Screen.width * 0.9f, Screen.height * 0.9f);
        h = Mathf.Max(h, style.CalcHeight(new GUIContent(text), w - Screen.height * 0.02f) + Screen.height * 0.02f);
        GUI.depth = -50;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
