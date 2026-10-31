using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maailmanpallo (testi): F8 avaa/sulkee. Pallo on oma kamera + oma 3D-pallo kaukana pelimaailmasta (layer 31), pelin aika pysähtyy auki ollessa.
/// Kohteet ovat leveys-/pituusasteita (Places-listassa), joten uuden kohteen lisäys on yksi rivi.
/// Ohjaus: hiiren veto tai nuolet/oikea tatti pyörittää, klikkaus tai Tab / olkanapit valitsee kohteen, Enter / alanappi "matkustaa" (toistaiseksi vain logi), Esc / F8 sulkee.
/// Kuva: Resources/Kartta/maailma_5400 (equirectangular, vasen reuna = 180° länsi). Luo itsensä ajossa, ei valikkoa.
/// </summary>
public class WorldGlobe : MonoBehaviour
{
    // scene = alueen scene (ladataan tarvittaessa), area = Area.areaName, start = saapumiskohta; tyhjä area = ei vielä kohdetta
    class Place { public string name; public float lat, lon; public Color color; public Transform marker; public string scene, area; public Vector2 start; }

    static readonly Place[] PlacesDef =
    {
        new Place { name = "Bricktown",  lat = 52.5f, lon = -1.9f,  color = new Color(1f, 0.85f, 0.2f), scene = "SampleScene", area = "Katu", start = new Vector2(-32.19f, -1.05f) },   // nyrkkeilysalin edusta
        new Place { name = "Korvatunturi", lat = 66.5f, lon = 25.7f,   color = new Color(0.6f, 0.9f, 1f), scene = "Alppikyla", area = "Alppikylän kiitotie", start = new Vector2(30004f, -1.25f) },
        new Place { name = "Slidaho",    lat = 37.2f, lon = -111.5f,   color = new Color(1f, 0.6f, 0.2f), scene = "Aavikko", area = "Aavikko", start = new Vector2(78013.68f, -1.78f) },   // kopterin laskeutumispaikka
        new Place { name = "Thaitown",   lat = 13.7f, lon = 100.5f, color = new Color(1f, 0.3f, 0.7f), scene = "Thaitown", area = "Thaitown laituri", start = new Vector2(106006.5f, -2.28f) },   // junalaituri
        new Place { name = "Eldoraato",  lat = 5f,    lon = -73.8f,   color = new Color(0.3f, 1f, 0.4f), scene = "Viidakko", area = "Viidakon kiitotie", start = new Vector2(53004f, -1f) },   // kiitotien alku (valikko 305)
        new Place { name = "Uccopulco",  lat = 16.5f, lon = -100f,  color = Color.white, scene = "UccoLaiva", area = "Uccopulco", start = new Vector2(8966f, -1f) },
    };

    const int GlobeLayer = 31;
    const float Radius = 1f;
    static WorldGlobe instance;

    Camera cam;
    Transform globe;
    List<Place> places = new List<Place>();
    float lat = 30f, lon = 20f, tLat = 30f, tLon = 20f;
    int selected = 0;
    float prevTimeScale = 1f;
    bool dragging; Vector2 downPos; bool moved;
    GUIStyle labelStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Resources.Load<Texture2D>("Kartta/maailma_5400") == null) return;
        var go = new GameObject("MaailmanpalloOhjain");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<WorldGlobe>();
    }

    static Vector3 Dir(float latDeg, float lonDeg)
    {
        float a = latDeg * Mathf.Deg2Rad, o = lonDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * Mathf.Sin(o), Mathf.Sin(a), -Mathf.Cos(a) * Mathf.Cos(o));
    }

    void Update()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool padMap = pad != null && pad.selectButton.wasPressedThisFrame;   // ohjaimen kartta-nappi (Select / Back)
        bool toggle = (kb != null && kb.f8Key.wasPressedThisFrame) || padMap;
        if (globe == null) { if (toggle && (!padMap || (Time.timeScale > 0f && Players.Primary != null))) Open(); return; }
        if (toggle || (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) { Close(); return; }

        float dt = Time.unscaledDeltaTime;
        // lentokonekursori: vasen tatti / ristiohjain liikuttaa konetta ruudulla, kun kone on kohteen päällä, A (alanappi) matkustaa
        if (pad != null)
        {
            // kone pysyy ruudun keskellä, palloa pyöritetään tatilla; kohde keskellä = valittu
            if ((pad.leftStick.ReadValue() + pad.dpad.ReadValue()).magnitude > 0.2f) padCursor = true;
            cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            planeAngle = 0f;
        }
        hover = -1;
        if (padCursor)
        {
            float bd = 70f * 70f;
            for (int i = 0; i < places.Count; i++)
            {
                if (!Visible(places[i])) continue;
                Vector3 sp = cam.WorldToScreenPoint(places[i].marker.position);
                float d = (new Vector2(sp.x, Screen.height - sp.y) - cursor).sqrMagnitude;
                if (d < bd) { bd = d; hover = i; }
            }
            if (hover >= 0) selected = hover;
        }
        float kx = 0f, ky = 0f;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed) kx -= 1f; if (kb.rightArrowKey.isPressed) kx += 1f;
            if (kb.downArrowKey.isPressed) ky -= 1f; if (kb.upArrowKey.isPressed) ky += 1f;
        }
        if (pad != null) { var s = pad.rightStick.ReadValue() + pad.leftStick.ReadValue() + pad.dpad.ReadValue(); s = Vector2.ClampMagnitude(s, 1f); kx += s.x; ky += s.y; }
        if (Mathf.Abs(kx) + Mathf.Abs(ky) > 0.05f) { tLon += kx * 70f * dt; tLat += ky * 70f * dt; lat = tLat; lon = tLon; }

        var m = Mouse.current;
        if (m != null)
        {
            Vector2 mp = m.position.ReadValue();
            if (m.leftButton.wasPressedThisFrame) { dragging = true; downPos = mp; moved = false; }
            if (dragging && m.leftButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                if ((mp - downPos).sqrMagnitude > 64f) moved = true;
                if (moved) { tLon -= d.x * 0.25f; tLat -= d.y * 0.25f; lat = tLat; lon = tLon; }
            }
            if (m.leftButton.wasReleasedThisFrame)
            {
                if (dragging && !moved) PickAt(mp);
                dragging = false;
            }
        }

        if ((kb != null && kb.tabKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) { padCursor = false; Select((selected + 1) % places.Count); }
        if (pad != null && pad.leftShoulder.wasPressedThisFrame) { padCursor = false; Select((selected + places.Count - 1) % places.Count); }
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        { Travel(places[selected]); if (globe == null) return; }
        if (pad != null && pad.buttonSouth.wasPressedThisFrame)
        {
            if (!padCursor || hover >= 0) { Travel(places[padCursor ? hover : selected]); if (globe == null) return; }
            else { notice = "Vie kone kohteen päälle"; noticeUntil = Time.unscaledTime + 1.5f; }
        }

        tLat = Mathf.Clamp(tLat, -80f, 80f); lat = Mathf.Clamp(lat, -80f, 80f);
        // pehmeä kääntyminen valittuun kohteeseen (kun ei raahata)
        if (selectTarget) { lat = Mathf.Lerp(lat, selLat, 1f - Mathf.Exp(-6f * dt)); lon = Mathf.LerpAngle(lon, selLon, 1f - Mathf.Exp(-6f * dt)); tLat = lat; tLon = lon; if (Mathf.Abs(lat - selLat) < 0.1f && Mathf.Abs(Mathf.DeltaAngle(lon, selLon)) < 0.1f) selectTarget = false; }
        if (Mathf.Abs(kx) + Mathf.Abs(ky) > 0.05f || (dragging && moved)) selectTarget = false;
        globe.rotation = Quaternion.Euler(-lat, 0f, 0f) * Quaternion.Euler(0f, lon, 0f);
        for (int i = 0; i < places.Count; i++)
        {
            float s = i == selected ? 1.8f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f) : 1f;
            places[i].marker.localScale = Vector3.one * 0.045f * s;
        }
    }

    bool selectTarget; float selLat, selLon;
    bool padCursor; Vector2 cursor; float planeAngle; int hover = -1;   // cursor = GUI-koordinaatit (y alaspäin)
    Texture2D planeTex;

    // lentokoneen kuvake koodilla: valkoinen siluetti mustalla reunalla, nokka ylös
    static Texture2D MakePlane()
    {
        const int N = 64;
        Vector2[] poly = { V(0,30), V(3,22), V(3,8), V(28,-8), V(28,-14), V(3,-6), V(3,-20), V(10,-27), V(10,-30), V(0,-27), V(-10,-30), V(-10,-27), V(-3,-20), V(-3,-6), V(-28,-14), V(-28,-8), V(-3,8), V(-3,22) };
        var inside = new bool[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Vector2 p = new Vector2(x - N / 2 + 0.5f, y - N / 2 + 0.5f);
                bool c = false;
                for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                    if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c;
                inside[y * N + x] = c;
            }
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                if (inside[y * N + x]) { px[y * N + x] = Color.white; continue; }
                bool edge = false;
                for (int dy = -2; dy <= 2 && !edge; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < N && ny < N && inside[ny * N + nx]) { edge = true; break; }
                    }
                px[y * N + x] = edge ? new Color(0f, 0f, 0f, 0.9f) : Color.clear;
            }
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        t.SetPixels(px); t.Apply();
        return t;
        Vector2 V(float a, float b) => new Vector2(a, b);
    }
    float noticeUntil; string notice;

    void Travel(Place p)
    {
        if (string.IsNullOrEmpty(p.area)) { notice = p.name + ": ei vielä kenttää"; noticeUntil = Time.unscaledTime + 2f; return; }
        string scene = p.scene, area = p.area; Vector2 start = p.start;
        Close();
        ScreenFader.Host.StartCoroutine(TravelRoutine(scene, area, start));   // ajetaan fader-isännässä: vanha scene puretaan matkalla
    }

    static System.Collections.IEnumerator TravelRoutine(string scene, string areaName, Vector2 start)
    {
        var pc = Players.Primary;
        if (pc == null) yield break;
        foreach (var q in Players.All) if (q != null) q.enabled = false;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = t / 0.35f; yield return null; }
        ScreenFader.Alpha = 1f;

        Area dest = Area.Find(areaName);
        if (dest == null && !string.IsNullOrEmpty(scene))
        {
            var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            if (load != null) while (!load.isDone) yield return null;
            dest = Area.Find(areaName);
        }
        if (dest == null) Debug.LogWarning("[Kartta] aluetta ei löytynyt: " + areaName);
        else dest.Apply(pc);
        var pos = new Vector3(start.x, start.y, 0f);
        pc.TeleportTo(pos);
        int n = 0;
        foreach (var o in Players.All) { if (o == null || o == pc) continue; n++; o.TeleportTo(pos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f)); }
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(start.x);

        yield return new WaitForSecondsRealtime(0.1f);
        if (dest != null) yield return Area.FreeOtherScenes(dest, pc);
        foreach (var q in Players.All) if (q != null) q.enabled = true;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / 0.35f; yield return null; }
        ScreenFader.Alpha = 0f;
    }

    void Select(int i)
    {
        selected = i; selectTarget = true; selLat = places[i].lat; selLon = places[i].lon;
    }

    void PickAt(Vector2 screen)
    {
        int best = -1; float bd = 40f * 40f;
        for (int i = 0; i < places.Count; i++)
        {
            if (!Visible(places[i])) continue;
            Vector3 sp = cam.WorldToScreenPoint(places[i].marker.position);
            float d = ((Vector2)sp - screen).sqrMagnitude;
            if (d < bd) { bd = d; best = i; }
        }
        if (best >= 0) Select(best);
    }

    bool Visible(Place p)
    {
        Vector3 n = (p.marker.position - globe.position).normalized;
        return Vector3.Dot(n, (cam.transform.position - p.marker.position).normalized) > 0.12f;
    }

    void OnGUI()
    {
        if (globe == null) return;
        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        for (int i = 0; i < places.Count; i++)
        {
            if (!Visible(places[i])) continue;
            Vector3 sp = cam.WorldToScreenPoint(places[i].marker.position);
            var r = new Rect(sp.x - 100f, Screen.height - sp.y - 40f, 200f, 28f);
            labelStyle.normal.textColor = Color.black; GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), places[i].name, labelStyle);
            labelStyle.normal.textColor = i == selected ? Color.yellow : Color.white; GUI.Label(r, places[i].name, labelStyle);
        }
        labelStyle.normal.textColor = Color.white;
        if (Time.unscaledTime < noticeUntil) { labelStyle.normal.textColor = new Color(1f, 0.5f, 0.4f); GUI.Label(new Rect(0, Screen.height - 76f, Screen.width, 30f), notice, labelStyle); labelStyle.normal.textColor = Color.white; }
        if (padCursor)
        {
            if (planeTex == null) planeTex = MakePlane();
            float sz = hover >= 0 ? 64f + 6f * Mathf.Sin(Time.unscaledTime * 8f) : 56f;
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(planeAngle, cursor);
            GUI.color = hover >= 0 ? Color.yellow : Color.white;
            GUI.DrawTexture(new Rect(cursor.x - sz * 0.5f, cursor.y - sz * 0.5f, sz, sz), planeTex);
            GUI.color = Color.white; GUI.matrix = old;
        }
        bool padOn = Gamepad.current != null;
        GUI.Label(new Rect(0, Screen.height - 36f, Screen.width, 30f), padOn ? "Tatti = pyöritä palloa, kunnes kohde on koneen alla   A = matkusta   B / Select = sulje" : "Veto = pyöritä   Klikkaus / Tab = valitse   Enter = matkusta   Esc / F8 = sulje", labelStyle);
    }

    void Open()
    {
        var tex = Resources.Load<Texture2D>("Kartta/maailma_5400");
        if (tex == null) return;
        prevTimeScale = Time.timeScale; Time.timeScale = 0f;

        var root = new GameObject("Maailmanpallo");
        root.transform.position = new Vector3(0f, -5000f, 0f);
        globe = root.transform;

        var mf = root.AddComponent<MeshFilter>(); mf.sharedMesh = BuildSphere(96, 48);
        var mr = root.AddComponent<MeshRenderer>();
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
        var mat = new Material(sh);
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        mr.sharedMaterial = mat;
        root.layer = GlobeLayer;

        places.Clear();
        foreach (var d in PlacesDef)
        {
            var p = new Place { name = d.name, lat = d.lat, lon = d.lon, color = d.color, scene = d.scene, area = d.area, start = d.start };
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.layer = GlobeLayer;
            s.transform.SetParent(globe, false);
            s.transform.localPosition = Dir(p.lat, p.lon) * (Radius * 1.01f);
            s.transform.localScale = Vector3.one * 0.045f;
            var mm = new Material(sh); mm.color = p.color; if (mm.HasProperty("_BaseColor")) mm.SetColor("_BaseColor", p.color);
            s.GetComponent<MeshRenderer>().sharedMaterial = mm;
            p.marker = s.transform;
            places.Add(p);
        }

        var cgo = new GameObject("MaailmanpalloKamera");
        cgo.transform.position = new Vector3(0f, -5000f, -4.2f);
        cam = cgo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f, 1f);
        cam.cullingMask = 1 << GlobeLayer;
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 20f;
        cam.depth = 100f;
        cam.allowHDR = false;
        var al = cgo.GetComponent<AudioListener>(); if (al != null) Destroy(al);
        lat = tLat = 20f; lon = tLon = 20f;
        cursor = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f); padCursor = false; hover = -1; planeAngle = 0f;
        Select(1);
    }

    void Close()
    {
        if (globe != null) Destroy(globe.gameObject);
        if (cam != null) Destroy(cam.gameObject);
        globe = null; cam = null; places.Clear();
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
    }

    // oma pallo, jotta tekstuurin suunta tiedetään: u = (pituus+180)/360, v = (leveys+90)/180, itä oikealle kun katsotaan -Z:sta
    static Mesh BuildSphere(int lonSeg, int latSeg)
    {
        var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
        for (int j = 0; j <= latSeg; j++)
        {
            float la = -90f + 180f * j / latSeg;
            for (int i = 0; i <= lonSeg; i++)
            {
                float lo = -180f + 360f * i / lonSeg;
                verts.Add(Dir(la, lo) * Radius);
                uvs.Add(new Vector2((float)i / lonSeg, (float)j / latSeg));
            }
        }
        int row = lonSeg + 1;
        for (int j = 0; j < latSeg; j++)
            for (int i = 0; i < lonSeg; i++)
            {
                int a = j * row + i, b = a + 1, c = a + row, d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        var m = new Mesh { name = "Maailmanpallo", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}
