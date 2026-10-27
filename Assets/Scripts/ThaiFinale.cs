using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Thaitownin finaali (käyttäjän suunnitelma):
/// 1) Kattohuoneistokäytävässä 2 Jakuzaa ja Tanya ennen sviittiä (sviitin ovi lukossa, kunnes ne on voitettu).
/// 2) Gustav sviitissä (GustavBrain, 560 elämää): kun energia loppuu, hän juoksee pois (Vanish) ja peli ohjaa heroa: sviitistä käytävälle, käytävän päähän, katolle.
/// 3) Katolla Gustav juoksee kopteriin ("Thaitown katon kopteri", valikko 262), kopteri nousee ilmaan, eikä hero ehdi perään.
/// 4) Sen jälkeen alas hissillä (jatko: hotellin ulko-ovi oikealle -> lentokenttä -> pienkone viidakkoon, ei vielä toteutettu).
/// Toimii itsestään ajon aikana (ei vaadi scenen komponenttia); tila nollautuu uudessa pelissä (Single-scenen lataus).
/// </summary>
public class ThaiFinale : MonoBehaviour
{
    public static bool WaveSpawned, GustavFled, RoofDone;
    static GustavBrain gustav;
    static ThaiFinale inst;
    bool roofRunning;
    GameObject heliObj; AudioSource heliSrc; float heliFindAt, heliBoost;
    float doorSearchAt;
    readonly Dictionary<string, Door> doors = new Dictionary<string, Door>();
    readonly List<Enemy> wave = new List<Enemy>();
    readonly List<Enemy> roofWave = new List<Enemy>();
    readonly List<Enemy> roofYakuza = new List<Enemy>();   // katon Jakuzat: viimeinen kaatuva pudottaa klubin kutsun

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { WaveSpawned = GustavFled = RoofDone = false; gustav = null; inst = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (inst != null) return;
        var go = new GameObject("ThaiFinale");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<ThaiFinale>();
        SceneManager.sceneLoaded += (s, mode) =>
        {
            if (mode != LoadSceneMode.Single || inst == null) return;
            WaveSpawned = GustavFled = RoofDone = false; gustav = null;   // uusi peli
            inst.StopAllCoroutines(); inst.roofRunning = false; inst.wave.Clear(); inst.roofWave.Clear(); inst.roofYakuza.Clear(); inst.doors.Clear();
        };
    }

    /// GustavBrain kutsuu, kun Gustav poistuu näkyvistä taistelun päätteeksi.
    public static void OnGustavVanished(GustavBrain g)
    {
        if (GustavFled || Area.Current == null || Area.Current.areaName != "Thaitown sviitti") return;
        GustavFled = true; gustav = g;
    }

    /// Katolla: Gustav on päässyt kopteriin, kopteri nousee.
    public static void OnGustavBoarded()
    {
        if (inst != null) inst.StartCoroutine(inst.Takeoff());
    }

    Door FindDoor(string doorName)
    {
        if (doors.TryGetValue(doorName, out var d) && d != null) return d;
        foreach (var x in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (x != null && x.name == doorName) { doors[doorName] = x; return x; }
        return null;
    }

    void Update()
    {
        var cur = Area.Current;
        if (cur == null || Players.Count == 0) return;
        // 1) käytävän vartijat ennen sviittiä
        if (!WaveSpawned && !GustavFled && cur.areaName == "Thaitown kattohuoneisto") SpawnWave(cur);
        UpdateHeliSound(cur);
        CheckInviteDrop();
        // 3) katon pako
        if (GustavFled && !RoofDone && !roofRunning && cur.areaName == "Thaitown katto") StartCoroutine(RoofEscape(cur));
    }

    /// Kopterin ääni (videon roottorisilmukka, Resources/Sfx/kopteri_katto): kuuluu katolla etäisyyden mukaan; noustessa kovenee ja nousee sävelkorkeus.
    void UpdateHeliSound(Area cur)
    {
        if (heliObj == null && Time.time >= heliFindAt) { heliObj = GameObject.Find("Thaitown katon kopteri"); heliFindAt = Time.time + 1f; }
        if (heliObj == null || !heliObj.activeInHierarchy) { if (heliSrc != null) { heliSrc.volume = 0f; heliSrc.Pause(); } return; }
        if (heliSrc == null)
        {
            heliSrc = heliObj.AddComponent<AudioSource>();
            heliSrc.clip = Resources.Load<AudioClip>("Sfx/kopteri_katto");
            heliSrc.loop = true; heliSrc.playOnAwake = false; heliSrc.spatialBlend = 0f; heliSrc.volume = 0f;   // alkaa hiljaa (oletus 1 kuului muissa alueissa)
        }
        float v = 0f;
        var p = Players.Primary;
        if (p != null && cur.areaName == "Thaitown katto")
            v = Mathf.Clamp01(1f - Vector2.Distance(heliObj.transform.position, p.transform.position) / 30f) * (0.45f + 0.4f * heliBoost);
        heliSrc.volume = Mathf.MoveTowards(heliSrc.volume, v, Time.deltaTime * 2f);
        if (heliSrc.clip != null) { if (heliSrc.volume > 0.001f && !heliSrc.isPlaying) heliSrc.Play(); else if (heliSrc.volume <= 0.001f && heliSrc.isPlaying) heliSrc.Pause(); }
        heliSrc.pitch = 1f + 0.3f * heliBoost;
    }

    void SpawnWave(Area corridor)
    {
        WaveSpawned = true;
        var cam = Camera.main;
        float half = cam != null ? cam.orthographicSize * cam.aspect : 9f;
        float cx = cam != null ? cam.transform.position.x : corridor.walkMinX;
        string[] types = { "Jakuza", "Jakuza", "Tanya" };
        for (int i = 0; i < types.Length; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;   // reunoilta
            float y = Mathf.Lerp(corridor.maxDepthY, corridor.minDepthY, i == 2 ? 0.5f : (i == 0 ? 0.3f : 0.7f));
            var e = Enemy.SpawnHelper(types[i], new Vector3(cx + side * (half - 0.8f), y, 0f), types[i] + " (käytävän vartija " + (i + 1) + ")");
            if (e != null) wave.Add(e);
        }
        var door = FindDoor("Thaitown: Käytävältä sviittiin");
        if (door != null && wave.Count > 0) { door.guards = wave.ToArray(); door.guardsLockedText = "Ovi on lukossa. Voita ensin vartijat."; }
    }

    // ---------------- katto ----------------

    IEnumerator RoofEscape(Area roof)
    {
        roofRunning = true;
        var heli = GameObject.Find("Thaitown katon kopteri");
        var back = FindDoor("Thaitown: Katolta käytävälle");
        SpawnRoofWave(roof, heli);   // 2 Jakuzaa ja 2 Tanyaa pitävät heron kiireisenä, joten Gustav ehtii kopteriin
        yield return new WaitForSeconds(1.2f);   // hetki katsoa ympärille
        if (gustav == null) { RoofDone = true; roofRunning = false; yield break; }
        float startX = back != null ? back.transform.position.x : (roof.walkMinX + roof.walkMaxX) * 0.5f;
        float targetX = heli != null ? heli.transform.position.x + HeliBoardOffset(heli) : roof.walkMaxX - 3f;
        float y = Mathf.Lerp(roof.maxDepthY, roof.minDepthY, 0.45f);
        gustav.RunTo(new Vector3(startX + (targetX > startX ? 1.2f : -1.2f), y, 0f), targetX);
        float t = 0f;
        while (!RoofDone && t < 30f) { t += Time.deltaTime; yield return null; }   // Takeoff() päättää
        roofRunning = false;
    }

    void SpawnRoofWave(Area roof, GameObject heli)
    {
        roofWave.Clear(); roofYakuza.Clear();
        var cam = Camera.main;
        float half = cam != null ? cam.orthographicSize * cam.aspect : 9f;
        float cx = cam != null ? cam.transform.position.x : (roof.walkMinX + roof.walkMaxX) * 0.5f;
        float hx = heli != null ? heli.transform.position.x : cx + half * 0.6f;
        // kaksi kopterin tykönä (heron ja kopterin välissä), kaksi reunasta
        string[] types = { "Jakuza", "Tanya", "Jakuza", "Tanya" };
        float[] xs = { hx - 3.5f, hx - 6f, cx - half + 0.8f, cx + half - 0.8f };
        float[] ys = { 0.35f, 0.7f, 0.5f, 0.3f };
        for (int i = 0; i < types.Length; i++)
        {
            float y = Mathf.Lerp(roof.maxDepthY, roof.minDepthY, ys[i]);
            float x = Mathf.Clamp(xs[i], roof.walkMinX + 0.5f, roof.walkMaxX - 0.5f);
            var e = Enemy.SpawnHelper(types[i], new Vector3(x, y, 0f), types[i] + " (katon vartija " + (i + 1) + ")");
            if (e != null) { roofWave.Add(e); if (types[i] == "Jakuza") roofYakuza.Add(e); }
        }
        var door = FindDoor("Thaitown: Katolta käytävälle");
        if (door != null && roofWave.Count > 0) { door.guards = roofWave.ToArray(); door.guardsLockedText = "Ovi on lukossa. Voita ensin vartijat."; }
    }

    /// Viimeinen katon Jakuza pudottaa kutsun Thaitownin klubille (klubin ovi aukeaa sillä).
    void CheckInviteDrop()
    {
        if (roofYakuza.Count == 0 || Clues.Has(Clues.ClubInvite)) return;
        foreach (var e in roofYakuza) if (e != null && !e.IsDead) return;
        Clues.Add(Clues.ClubInvite, "Kutsu klubille", "Viimeisen yakuzan taskusta löytyi kutsu Thaitownin klubille. Ovimiehet päästävät sillä sisään, ja yläkerrassa odottaa yakuza-pomo.");
    }

    bool RoofGuardsAlive { get { foreach (var e in roofWave) if (e != null && !e.IsDead) return true; return false; } }

    static float HeliBoardOffset(GameObject heli)
    {
        var hull = heli.transform.Find("Runko");
        var sr = hull != null ? hull.GetComponent<SpriteRenderer>() : null;
        return sr != null ? -sr.bounds.size.x * 0.1f : -1f;   // ovi rungon keskellä (pivot keskellä)
    }

    IEnumerator Takeoff()
    {
        var heli = GameObject.Find("Thaitown katon kopteri");
        yield return new WaitForSeconds(0.5f);
        if (heli != null)
        {
            var loops = heli.GetComponentsInChildren<RotorLoop>();
            foreach (var l in loops) l.fps *= 2f;   // roottorit kiihtyvät
            Vector3 p0 = heli.transform.position;
            float t = 0f;
            while (t < 7f)
            {
                t += Time.deltaTime;
                heliBoost = Mathf.Clamp01(t / 3f);
                float k = t * t / 49f;   // nousu kiihtyy
                heli.transform.position = p0 + new Vector3(k * 18f, k * 16f + Mathf.Min(t, 1f) * 0.6f, 0f);
                if (t < 3.5f && CameraFollow.Instance != null) CameraFollow.Shake(0.05f, 0.1f);
                yield return null;
            }
            heli.SetActive(false);
        }
        else yield return new WaitForSeconds(1.5f);
        RoofDone = true;
    }

    // ---------------- opastus ----------------

    void OnGUI()
    {
        var cur = Area.Current;
        if (cur == null || Players.Count == 0 || GameMenu.IsOpen) return;
        string text = null, targetDoor = null;
        string a = cur.areaName;
        if (GustavFled && !RoofDone)
        {
            if (a == "Thaitown sviitti") { text = "Gustav pakeni! Poistu sviitistä käytävälle."; targetDoor = "Thaitown: Sviitistä käytävälle"; }
            else if (a == "Thaitown kattohuoneisto")
            {
                text = "Gustav pakeni katolle! Juokse käytävän päähän.";
                var p = Players.Primary; var r = FindDoor("Thaitown: Käytävän oikea päätyovi katolle"); var l = FindDoor("Thaitown: Käytävän vasen päätyovi katolle");
                if (p != null && r != null && l != null)
                    targetDoor = Mathf.Abs(r.transform.position.x - p.transform.position.x) <= Mathf.Abs(l.transform.position.x - p.transform.position.x)
                        ? "Thaitown: Käytävän oikea päätyovi katolle" : "Thaitown: Käytävän vasen päätyovi katolle";
            }
        }
        else if (RoofDone && (a == "Thaitown katto" || a == "Thaitown kattohuoneisto" || a == "Thaitown hotellin hissi"))
            text = RoofGuardsAlive ? "Gustav pakeni kopterilla! Voita vartijat." : "Gustav pakeni kopterilla! Mene alas hissillä.";
        if (text == null) return;

        float s = Screen.height / 1080f;
        var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = Mathf.RoundToInt(34f * s), fontStyle = FontStyle.Bold };
        var rect = new Rect(0, Screen.height * 0.1f, Screen.width, 60f * s);
        st.normal.textColor = Color.black; var sh = rect; sh.x += 2f * s; sh.y += 2f * s; GUI.Label(sh, text, st);
        st.normal.textColor = new Color(1f, 0.9f, 0.4f); GUI.Label(rect, text, st);

        if (targetDoor != null)
        {
            var d = FindDoor(targetDoor); var pl = Players.Primary;
            if (d != null && pl != null)
            {
                float dx = d.transform.position.x - pl.transform.position.x;
                if (Mathf.Abs(dx) > 3f)
                {
                    var ast = new GUIStyle(st) { alignment = dx > 0f ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, fontSize = Mathf.RoundToInt(80f * s) };
                    var ar = new Rect(40f * s, Screen.height * 0.4f, Screen.width - 80f * s, 120f * s);
                    string arrow = dx > 0f ? ">>" : "<<";
                    ast.normal.textColor = Color.black; var ash = ar; ash.x += 3f * s; ash.y += 3f * s; GUI.Label(ash, arrow, ast);
                    ast.normal.textColor = new Color(1f, 0.9f, 0.4f); GUI.Label(ar, arrow, ast);
                }
            }
        }
    }
}
