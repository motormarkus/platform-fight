using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Lumilautailu Alppikylän gondolin päässä (lumirinne). Kun pelaaja tulee rinteelle, PlayerController pysäytetään ja hero laskee laudalla:
/// ohjaus liikuttaa häntä rinteellä (vasen = jarru, oikea = vauhtia), hyppy ohittaa kivet, ilmassa isku/potku tekee volttin (pisteitä).
/// Puut ja kivet liukuvat rinteen suuntaisesti kohti heroa (omat esteet, kaukaiset puut jäävät koristeeksi). Osuma kaataa hetkeksi, mutta ei vie energiaa.
/// Ajo kestää runTime sekuntia: sen jälkeen pimennys ja paluu gondolilla kylään (rinteen ovi, jonka kohde on toinen alue).
/// Kuvat ja rinteen mitat asetetaan valikosta 290.
/// </summary>
public class SnowRide : MonoBehaviour
{
    public Area slope;
    public Sprite[] idleFrames;
    Sprite[] pushFrames, landFrames;   // johdetaan temppu 1 -kuvista ajon alussa
    public Sprite[] trick1Frames, trick2Frames;   // temppu 1 (isku J): 360-kierre, temppu 2 (potku K): voltti; koko hyppy videosta
    public float trick1AirStart = 0.26f, trick2AirStart = 0.40f;   // kuinka suuri osa kuvasarjasta on valmistautumista ennen ilmaan lähtöä
    public float angleDeg = 14.1f, TiltMul = 1.5f;
    public float runTime = 55f;
    [Header("Esteet")]
    public float baseSpeed = 20f, obstacleInterval = 0.9f, accelTime = 4f;   // vauhti kiihtyy alussa accelTime sekunnissa ja pysyy sitten samana
    [Header("Hyppyrit")]
    public Sprite[] rampSprites;   // hyppyrin kuva (valinnainen; ilman sitä käytetään koodilla piirrettyä paikkamerkkiä)
    public float rampChance = 0.18f, rampWidth = 1.1f, rampJumpTime = 1.5f, rampJumpHeight = 5.5f;
    public float treeHitWidth = 0.55f, rockHitWidth = 0.65f, hitDepth = 0.45f;
    public float jumpTime = 1.0f, jumpHeight = 3.0f, rockClearHeight = 0.55f;

    class Obstacle { public Transform t; public SpriteRenderer sr; public bool rock, ramp; }
    class Rider
    {
        public PlayerController pc;
        public Vector3 bodyPos, bodyScale; public bool flipX;
        public float height, jumpT = -1f, flipT = -1f, flipStart, trick, dur = 1f, peak = 3f, stun, invuln, landT = -1f;
        public int flips;
    }

    readonly List<Rider> riders = new List<Rider>();
    readonly List<Obstacle> obstacles = new List<Obstacle>();
    Sprite[] treeSprites, rockSprites;
    GameObject hiddenFlow;
    bool running;
    float elapsed, spawnClock, score, message;
    string messageText = "";
    int hits;

    void Update()
    {
        if (running || slope == null || Area.Current != slope || Players.Count == 0) return;
        if (idleFrames == null || idleFrames.Length == 0 || trick1Frames == null || trick1Frames.Length == 0 || trick2Frames == null || trick2Frames.Length == 0) return;
        running = true;
        ScreenFader.Host.StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(0.9f);   // oven saapumispimennys ehtii hälvetä
        if (Area.Current != slope) { running = false; yield break; }

        // puut ja kivet: käytetään rinteen koristevirran kuvia, koriste piilotetaan ajon ajaksi
        treeSprites = rockSprites = null;
        foreach (var fl in GetComponentsInChildren<SlopeFlow>(true))
        {
            if (!fl.name.Contains("Puut ja kivet")) continue;
            var list = new List<Sprite>(); var rocks = new List<Sprite>();
            foreach (var s in fl.sprites) { if (s == null) continue; if (s.name.StartsWith("lumi_kivi")) rocks.Add(s); else list.Add(s); }
            treeSprites = list.ToArray(); rockSprites = rocks.ToArray();
            hiddenFlow = fl.gameObject; hiddenFlow.SetActive(false);
            break;
        }
        if (treeSprites == null || treeSprites.Length == 0) { if (hiddenFlow != null) hiddenFlow.SetActive(true); running = false; yield break; }

        // tavallisen hypyn ponnistus ja alastulo: temppu 1 -kuvasarjan alku (kyykky, ilmaan) ja loppu (alastulo)
        int n1 = trick1Frames.Length;
        pushFrames = trick1Frames.Skip(0).Take(Mathf.Max(1, Mathf.RoundToInt(n1 * trick1AirStart))).ToArray();
        landFrames = trick1Frames.Skip(Mathf.RoundToInt(n1 * 0.78f)).ToArray();

        riders.Clear();
        foreach (var p in Players.All)
        {
            if (p == null || p.body == null) continue;
            var r = new Rider { pc = p, bodyScale = p.body.transform.localScale, flipX = p.body.flipX };
            p.enabled = false;
            p.body.flipX = false;
            p.body.enabled = true;
            if (p.shadow != null) p.shadow.enabled = true;
            r.bodyScale = new Vector3(Mathf.Abs(r.bodyScale.x), r.bodyScale.y, r.bodyScale.z);
            p.body.transform.localScale = r.bodyScale;
            riders.Add(r);
        }
        elapsed = 0f; spawnClock = 0.5f; score = 0f; hits = 0; message = 0f;
        Say("Laske alas! Hyppynappi = hyppy, isku (J) tai potku (K) ilmassa = voltti");
        foreach (var o in obstacles) if (o != null && o.t != null) Destroy(o.t.gameObject);
        obstacles.Clear();

        while (elapsed < runTime && Area.Current == slope)
        {
            Tick(Time.deltaTime);
            yield return null;
        }

        Say(Area.Current == slope ? "Perillä! Pisteet " + Mathf.RoundToInt(score) : "");
        for (float t = 0f; t < 1.2f && Area.Current == slope; t += Time.deltaTime) yield return null;
        foreach (var o in obstacles) if (o != null && o.t != null) Destroy(o.t.gameObject);
        obstacles.Clear();

        // paluu gondolilla: rinteen ovi, jonka kohde on toinen alue
        Door back = null;
        foreach (var d in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d.here != slope || d.target == slope || (d.target == null && string.IsNullOrEmpty(d.targetName))) continue;
            if (back == null || (d.prompt != null && d.prompt.Contains("Gondoli"))) back = d;
        }
        var lead = Players.Primary;
        if (back != null && lead != null && Area.Current == slope)
        {
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = t / 0.4f; yield return null; }
            ScreenFader.Alpha = 1f;
            Release();
            Area dest = back.target != null ? back.target : Area.Find(back.targetName);
            if (dest == null && !string.IsNullOrEmpty(back.targetScene))
            {
                var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(back.targetScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                if (load != null) while (!load.isDone) yield return null;
                dest = Area.Find(back.targetName);
            }
            if (dest != null) dest.Apply(lead);
            Vector3 pos = new Vector3(back.spawnPoint.x, back.spawnPoint.y, 0f);
            lead.TeleportTo(pos);
            int n = 0;
            foreach (var p in Players.All) { if (p == null || p == lead) continue; n++; p.TeleportTo(pos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f)); }
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);
            yield return new WaitForSecondsRealtime(0.15f);
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / 0.4f; yield return null; }
            ScreenFader.Alpha = 0f;
        }
        else Release();   // ei paluuovea: ajo päättyy ja heron voi ohjata (uusi ajo alkaa, kun alue vaihtuu)
        running = false;
        if (Area.Current == slope) { yield return new WaitForSecondsRealtime(0.5f); }
    }

    void Release()
    {
        foreach (var r in riders)
        {
            if (r.pc == null) continue;
            r.pc.body.flipX = r.flipX;
            r.pc.body.color = Color.white;
            r.pc.enabled = true;
        }
        riders.Clear();
        if (hiddenFlow != null) hiddenFlow.SetActive(true);
    }

    void OnDisable() { if (running) { Release(); running = false; } }

    void Say(string s) { messageText = s; message = 3f; }

    void Tick(float dt)
    {
        elapsed += dt;
        float a = angleDeg * Mathf.Deg2Rad, ca = Mathf.Cos(a), sa = Mathf.Sin(a);
        float cx = (slope.walkMinX + slope.walkMaxX) * 0.5f;
        float halfWalk = (slope.walkMaxX - slope.walkMinX) * 0.5f;
        if (message > 0f) message -= dt;

        // ---- hero(t) ----
        foreach (var r in riders)
        {
            var pc = r.pc; if (pc == null) continue;
            Vector3 pos = pc.transform.position;
            Vector2 mv = pc.GameOver ? Vector2.zero : CoopInput.Move(pc.playerIndex);
            bool canAct = r.stun <= 0f;
            if (canAct)
            {
                pos.x = Mathf.Clamp(pos.x + mv.x * 6.5f * dt, cx - halfWalk, cx + halfWalk);
                pos.y = Mathf.Clamp(pos.y + mv.y * 6f * dt, slope.minDepthY, slope.maxDepthY);
                if (r.jumpT < 0f && CoopInput.Jump(pc.playerIndex)) { r.jumpT = 0f; r.landT = -1f; r.dur = jumpTime; r.peak = jumpHeight; }
                if (r.jumpT >= 0f && r.flipT < 0f && (CoopInput.Punch(pc.playerIndex) || CoopInput.Kick(pc.playerIndex)) && (CoopInput.Punch(pc.playerIndex) ? trick1Frames : trick2Frames).Length > 0 && r.jumpT < r.dur * 0.7f) { r.flipT = 0f; r.flipStart = r.jumpT; r.trick = CoopInput.Punch(pc.playerIndex) ? 1 : 2; }
            }
            pc.transform.position = pos;

            if (r.jumpT >= 0f)
            {
                r.jumpT += dt;
                float k = r.jumpT / r.dur;
                r.height = 4f * r.peak * k * (1f - k);
                if (r.flipT >= 0f) r.flipT += dt;
                if (r.jumpT >= r.dur)
                {
                    if (r.flipT >= 0f) { int bonus = r.peak > jumpHeight + 0.1f ? 300 : 150; r.flips++; score += bonus; Say("Voltti! +" + bonus); }
                    r.jumpT = -1f; r.flipT = -1f; r.height = 0f; r.landT = 0f;
                }
            }
            if (r.landT >= 0f) { r.landT += dt; if (r.landT > 0.3f) r.landT = -1f; }
            if (r.stun > 0f) r.stun -= dt;
            if (r.invuln > 0f) r.invuln -= dt;

            // ---- kuva ----
            Sprite sp; bool newArt = false;
            if (r.stun > 0f && landFrames.Length > 0) sp = landFrames[Mathf.Min(landFrames.Length - 1, (int)((0.9f - Mathf.Clamp(r.stun, 0f, 0.9f)) * 10f))];
            else if (r.flipT >= 0f)
            {
                var tf = r.trick == 1 ? trick1Frames : trick2Frames; float st = r.trick == 1 ? trick1AirStart : trick2AirStart;
                float u = Mathf.Clamp01((r.jumpT - r.flipStart) / Mathf.Max(0.2f, r.dur * 0.95f - r.flipStart));
                sp = tf[Mathf.Min(tf.Length - 1, (int)((st + (1f - st) * u) * tf.Length))]; newArt = true;
            }
            else if (r.jumpT >= 0f && pushFrames.Length > 0) sp = pushFrames[Mathf.Min(pushFrames.Length - 1, (int)(Mathf.Clamp01(r.jumpT / (r.dur * 0.3f)) * pushFrames.Length))];
            else if (r.landT >= 0f && landFrames.Length > 0) sp = landFrames[Mathf.Min(landFrames.Length - 1, (int)(r.landT / 0.3f * landFrames.Length))];
            else { sp = idleFrames[(int)(Time.time * 24f) % idleFrames.Length]; newArt = true; }
            var body = pc.body;
            body.sprite = sp;
            float ppu = sp.pixelsPerUnit;
            body.transform.localPosition = new Vector3((sp.pivot.x - sp.rect.width * 0.5f) / ppu * r.bodyScale.x, r.height + (sp.pivot.y - 32f) / ppu * r.bodyScale.y, 0f);
            body.transform.localRotation = Quaternion.Euler(0f, 0f, r.flipT >= 0f ? 0f : -angleDeg * TiltMul);   // nojaa rinteen suuntaan
            body.sortingOrder = Mathf.RoundToInt(-pos.y * 100f);
            bool blink = r.invuln > 0f && Mathf.FloorToInt(r.invuln * 12f) % 2 == 1;
            body.color = blink ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
            if (pc.shadow != null)
            {
                pc.shadow.transform.localPosition = Vector3.zero;
                float s = Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(r.height / 2.5f));
                pc.shadow.transform.localScale = new Vector3(1.5f * s, 0.45f * s, 1f);
            }
        }

        // ---- esteet ----
        float speed = baseSpeed * Mathf.SmoothStep(0.2f, 1f, Mathf.Clamp01(elapsed / accelTime));
        score += speed * dt;
        spawnClock -= dt;
        if (spawnClock <= 0f)
        {
            spawnClock = obstacleInterval * Random.Range(0.6f, 1.3f);
            SpawnObstacle(cx, halfWalk, ca, sa);
        }
        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            var o = obstacles[i];
            o.t.position += new Vector3(-ca, sa, 0f) * speed * dt;
            Vector3 op = o.t.position;
            o.sr.sortingOrder = Mathf.RoundToInt(-op.y * 100f);
            if (op.x < cx - halfWalk - 6f) { Destroy(o.t.gameObject); obstacles.RemoveAt(i); continue; }
            foreach (var r in riders)
            {
                if (r.pc == null || r.stun > 0f) continue;
                Vector3 pp = r.pc.transform.position;
                if (o.ramp)
                {
                    if (r.jumpT < 0f && r.height < 0.3f && Mathf.Abs(pp.x - op.x) < rampWidth && Mathf.Abs(pp.y - op.y) < hitDepth)
                    { r.jumpT = 0f; r.landT = -1f; r.flipT = -1f; r.dur = rampJumpTime; r.peak = rampJumpHeight; Say("Hyppyri! Voltti = isku (J) tai potku (K)"); }
                    continue;
                }
                if (r.invuln > 0f) continue;
                float w = o.rock ? rockHitWidth : treeHitWidth;
                if (Mathf.Abs(pp.x - op.x) > w || Mathf.Abs(pp.y - op.y) > hitDepth) continue;
                if (o.rock && r.height > rockClearHeight) continue;   // kivi ohitetaan hypyllä
                if (!o.rock && r.height > 1.8f) continue;
                r.stun = 0.9f; r.invuln = 2.2f; r.jumpT = r.flipT = -1f; r.height = 0f; hits++;
                score = Mathf.Max(0f, score - 80f);
                HitFx.PlayWhoosh();
                CameraFollow.Shake(0.08f, 0.25f);
                Say("Kaatuminen! -80");
            }
        }
    }

    void SpawnObstacle(float cx, float halfWalk, float ca, float sa)
    {
        bool ramp = Random.value < rampChance;
        bool rock = !ramp && rockSprites.Length > 0 && Random.value < 0.3f;
        var set = ramp ? (rampSprites != null && rampSprites.Length > 0 ? rampSprites : new[] { PlaceholderRamp() }) : rock ? rockSprites : treeSprites;
        float xs = cx + halfWalk + 12f;
        float yTarget = Random.Range(slope.minDepthY, slope.maxDepthY);
        float ys = yTarget - (xs - cx) * (sa / ca);   // esteet nousevat liikkuessaan ylävasemmalle: lähtökorkeus alempana, jotta ne saapuvat sankarin korkeudelle
        var go = new GameObject(ramp ? "Hyppyri" : rock ? "Kivi" : "Puu");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = set[Random.Range(0, set.Length)];
        float sc = Random.Range(0.9f, 1.15f);
        go.transform.localScale = new Vector3(sc * (!ramp && Random.value < 0.5f ? -1f : 1f), sc, 1f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, -angleDeg);
        go.transform.position = new Vector3(xs, ys, 0f);
        obstacles.Add(new Obstacle { t = go.transform, sr = sr, rock = rock, ramp = ramp });
    }

    static Sprite placeholder;
    /// Tilapäinen hyppyri ilman kuvaa: valkoinen kiila (korvataan rampSprites-kuvalla, kun oikea kuva on tehty).
    static Sprite PlaceholderRamp()
    {
        if (placeholder != null) return placeholder;
        const int W = 160, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                float top = H * Mathf.Pow(x / (float)(W - 1), 1.6f);
                Color c = y > top ? Color.clear : Color.Lerp(new Color(0.72f, 0.82f, 0.95f, 1f), Color.white, y / Mathf.Max(1f, top));
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        placeholder = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 64f);
        return placeholder;
    }

    void OnGUI()
    {
        if (!running || riders.Count == 0) return;
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.035f), fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        st.normal.textColor = Color.white;
        float left = Mathf.Max(0f, runTime - elapsed);
        GUI.Label(new Rect(0, Screen.height * 0.12f, Screen.width, Screen.height * 0.06f), "Pisteet " + Mathf.RoundToInt(score) + "   Aikaa " + Mathf.CeilToInt(left) + " s", st);
        if (message > 0f) GUI.Label(new Rect(0, Screen.height * 0.19f, Screen.width, Screen.height * 0.06f), messageText, st);
    }
}
