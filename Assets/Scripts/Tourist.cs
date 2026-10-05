using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Istuva turisti, joka nousee tuolilta, kun pelaaja tulee lähelle: nousuanimaatio, hetki seisten,
/// kävely annettuun kohtaan (esim. baarille) ja seisova idle siellä.
/// Kaikki kuvasarjat samassa koordinaatistossa (pivot ruudun alakeskellä); seisovat kuvat siirretään standOffsetY alemmas.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Tourist : MonoBehaviour
{
    public Sprite[] sitLoop;
    public float sitFrameTime = 0.22f;
    public int sitStartFrame;
    public Sprite[] standUp, idle, walk;
    public float standFrameTime = 0.11f, idleFrameTime = 0.16f, walkFrameTime = 0.065f;
    [Tooltip("Seisovien kuvien siirto alaspäin istumakuviin nähden (yksikköä).")]
    public float standOffsetY;
    [Tooltip("Kuinka läheltä (x) pelaaja herättää turistin.")]
    public float triggerDistance = 3.5f;
    [Tooltip("Mihin x:ään kävellään nousun jälkeen (0 = jää paikalleen).")]
    public float walkToX;
    [Tooltip("Mille syvyydelle (y) kävellään, esim. kannen lattialle tuolirivin eteen (0 = sama kuin seistessä).")]
    public float walkToY;
    public float walkSpeed = 1.4f;
    public float idleBeforeWalk = 1.2f;

    [System.Serializable]
    public struct Frame
    {
        public Sprite sprite;
        public float time;
        [Tooltip("true = tanssiruutu (384 leveä), false = suudelmaruutu (512 leveä)")]
        public bool dance;
        [Tooltip("Peilikuva (tanssin täysi kierros peilaamalla).")]
        public bool flip;
    }
    [Header("Suudelma ja tanssi (E seisovan turistin vieressä)")]
    [Tooltip("Lähestyminen, halaus ja suudelma ennen tanssia.")]
    public Frame[] kissIntro;
    [Tooltip("Irrottautuminen tanssin jälkeen.")]
    public Frame[] kissOutro;
    [Tooltip("Tanssin jaksot: kukin alkaa ja loppuu samaan suudelma-asentoon, joten niitä voi ketjuttaa satunnaisesti.")]
    public Frame[] danceSpin, danceHug, danceKiss;
    public AudioClip danceMusic;
    [Tooltip("Tanssin kesto (s): sen jälkeen musiikki häivytetään ja tanssi loppuu.")]
    public float danceDuration = 148f;   // kappale 2:33: häivytys loppuu ennen kappaleen loppua
    public float musicFadeOut = 4f;
    [Tooltip("Kuinka paljon alemmas (kohti kameraa) pari siirtyy suudelmaa ja tanssia varten (yksikköä).")]
    public float danceForward = 0.9f;
    [Tooltip("Romanttinen tunnelma tanssin aikana: punainen sykkivä hehku ruudun reunoilla ja sydämet.")]
    public Color glowColor = new Color(1f, 0.15f, 0.3f, 1f);
    [Range(0f, 1f)] public float glowStrength = 0.4f;
    float mood;                 // 0 = ei tunnelmaa, 1 = täysi
    static Texture2D vignette;
    static Sprite heart;
    [Tooltip("Pariskunnan jalkojen keskikohta tanssiruudussa (peilikuvan kohdistus).")]
    public float danceCenterPx = 206.5f;
    public float danceCellW = 384f;
    [Tooltip("Kuvapisteet (2x tarkkuus, pivot alakeskellä): turistin jalat seisovassa idlessä, pariskuvassa, heron aloitus- ja lopetuskohta.")]
    public Vector2 idleFeetPx = new Vector2(317f, 767f), kissFeetPx = new Vector2(336f, 495f);
    public float kissHeroStartPx = 113f, kissHeroEndPx = 104f;
    public float idleCellH = 768f, kissCellH = 512f;
    public string kissPrompt = "Suutele Auroraa";
    public string stopPrompt = "Lopeta tanssi";
    bool stopRequested, dancing;
    bool near, kissing;
    GUIStyle style;

    enum S { Sit, StandUp, Wait, Walk, Idle }
    S state = S.Sit;
    SpriteRenderer sr;
    float t, sitY;
    PlayerController pc;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sitY = transform.position.y;
        t = sitStartFrame * sitFrameTime;
    }

    static Sprite Loop(Sprite[] s, float t, float ft) => s[(int)(t / ft) % s.Length];

    void Update()
    {
        if (pc == null) pc = FindFirstObjectByType<PlayerController>();
        float dt = Time.deltaTime;
        t += dt;
        switch (state)
        {
            case S.Sit:
                if (sitLoop != null && sitLoop.Length > 0) sr.sprite = Loop(sitLoop, t, sitFrameTime);
                if (pc != null && standUp != null && standUp.Length > 0 && Mathf.Abs(pc.transform.position.x - transform.position.x) < triggerDistance)
                {
                    state = S.StandUp; t = 0f;
                    Vector3 p = transform.position; p.y = sitY - standOffsetY; transform.position = p;
                    sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
                }
                break;
            case S.StandUp:
            {
                int f = (int)(t / standFrameTime);
                if (f >= standUp.Length) { state = S.Wait; t = 0f; break; }
                sr.sprite = standUp[f];
                break;
            }
            case S.Wait:
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                if (t >= idleBeforeWalk) { state = walkToX != 0f && walk != null && walk.Length > 0 ? S.Walk : S.Idle; t = 0f; }
                break;
            case S.Walk:
            {
                Vector3 p = transform.position;
                Vector3 goal = new Vector3(walkToX, walkToY != 0f ? walkToY : p.y, p.z);
                if (Mathf.Abs(goal.x - p.x) > 0.01f) sr.flipX = goal.x < p.x;   // kuvat katsovat oikealle
                p = Vector3.MoveTowards(p, goal, walkSpeed * dt);        // tuolirivistä alas lattialle ja baarille
                transform.position = p;
                sr.sortingOrder = Mathf.RoundToInt(-p.y * 100f);
                sr.sprite = Loop(walk, t, walkFrameTime);
                if ((p - goal).sqrMagnitude < 0.0001f) { state = S.Idle; t = 0f; }
                break;
            }
            case S.Idle:
                if (kissing) break;
                if (idle != null && idle.Length > 0) sr.sprite = Loop(idle, t, idleFrameTime);
                near = false;
                if (pc != null && kissIntro != null && kissIntro.Length > 0 && pc.enabled && pc.IsFree && !pc.Riding)
                {
                    Vector3 q = pc.transform.position, me = transform.position;
                    near = Mathf.Abs(q.x - me.x) < 3.2f && Mathf.Abs(q.y - me.y) < 0.8f;
                    if (near) sr.flipX = q.x < me.x;      // kääntyy heroa kohti
                    if (near && UsePressed()) StartCoroutine(Kiss());
                }
                break;
        }
    }

    static bool UsePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    /// Hero kävelee viereen, halaus ja suudelma, sitten tanssi musiikin tahdissa, kunnes pelaaja lopettaa (E tai liike).
    IEnumerator Kiss()
    {
        kissing = true; near = false; stopRequested = false;
        float k = transform.localScale.x / 100f;           // kuvapikseli maailmassa (sprite 100 px/yks)
        Vector3 me0 = transform.position;
        // tanssipaikka vähän alempana (ei aurinkotuolien päällä), pelaajan kävelyalueen sisällä
        float lowY = me0.y - danceForward;
        if (pc != null) lowY = Mathf.Max(lowY, pc.minDepthY + 0.3f);
        Vector3 me = new Vector3(me0.x, lowY, me0.z);
        sr.flipX = true;                                   // katsoo vasemmalle, hero tulee vasemmalta
        float feetX = me.x - (idleFeetPx.x - 256f) * k, feetY = me.y + (idleCellH - idleFeetPx.y) * k;
        Vector3 cpos = new Vector3(feetX - (kissFeetPx.x - 256f) * k, feetY - (kissCellH - kissFeetPx.y) * k, me.z);
        Vector3 heroStart = new Vector3(cpos.x + (kissHeroStartPx - 256f) * k, me.y, 0f);
        Vector3 heroEnd = new Vector3(cpos.x + (kissHeroEndPx - 256f) * k, me.y, 0f);
        pc.Scripted = true;
        bool heroThere = false, auroraThere = false;
        for (float tt = 0f; tt < 5f && !(heroThere && auroraThere); tt += Time.deltaTime)
        {
            Vector3 d = heroStart - pc.transform.position;
            heroThere = Mathf.Abs(d.x) < 0.12f && Mathf.Abs(d.y) < 0.08f;
            pc.ScriptedMove = heroThere ? Vector2.zero : new Vector2(Mathf.Abs(d.x) < 0.12f ? 0f : Mathf.Sign(d.x), Mathf.Abs(d.y) < 0.08f ? 0f : Mathf.Clamp(d.y * 3f, -1f, 1f));
            // Aurora kävelee alas tanssipaikalle
            Vector3 a = transform.position;
            if (!auroraThere)
            {
                a = Vector3.MoveTowards(a, me, walkSpeed * Time.deltaTime);
                transform.position = a;
                sr.flipX = true;
                if (walk != null && walk.Length > 0) sr.sprite = walk[(int)(tt / walkFrameTime) % walk.Length];
                sr.sortingOrder = Mathf.RoundToInt(-a.y * 100f);
                auroraThere = (a - me).sqrMagnitude < 0.0004f;
                if (auroraThere && idle != null && idle.Length > 0) sr.sprite = idle[0];
            }
            yield return null;
        }
        pc.ScriptedMove = Vector2.zero;
        yield return null;
        pc.enabled = false;
        sr.sortingOrder = Mathf.RoundToInt(-me.y * 100f);
        if (pc.body != null) pc.body.enabled = false;
        if (pc.shadow != null) pc.shadow.enabled = false;

        // kuva oikeaan kohtaan: suudelmaruudut 512 leveitä, tanssiruudut 384 (rajattu samasta), peilikuva keskikohdan ympäri
        void Show(Frame f)
        {
            Vector3 p = cpos;
            if (f.dance)
            {
                p.x += (danceCellW * 0.5f - 256f) * k;
                if (f.flip) p.x += 2f * (danceCenterPx - danceCellW * 0.5f) * k;
            }
            transform.position = p;
            sr.flipX = f.flip;
            sr.sprite = f.sprite;
        }
        foreach (var f in kissIntro) { Show(f); yield return new WaitForSeconds(f.time); }

        // tanssi: suudellen pyörivä kierros ja halaten keinuminen vuorotellen, kunnes aika loppuu tai pelaaja lopettaa
        if (danceMusic != null) MusicPlayer.PlayOverride(danceMusic, 1.5f);
        dancing = true;
        StartCoroutine(Hearts());
        ShipNight.SetNight(true, 10f);          // kuunvalo ja tähdet tanssin ajaksi
        float danceStart = Time.time;
        var cycles = new System.Collections.Generic.List<Frame[]>();
        if (danceSpin != null && danceSpin.Length > 0) cycles.Add(danceSpin);
        if (danceHug != null && danceHug.Length > 0) cycles.Add(danceHug);
        if (cycles.Count == 0 && danceKiss != null && danceKiss.Length > 0) cycles.Add(danceKiss);
        int ci = 0;
        bool TimeUp() => Time.time - danceStart >= danceDuration;
        while (!stopRequested && !TimeUp() && cycles.Count > 0)
        {
            var c = cycles[ci++ % cycles.Count];
            int i = 0;
            for (; i < c.Length && !stopRequested && !TimeUp(); i++)
            {
                Show(c[i]);
                for (float tt = 0f; tt < c[i].time && !stopRequested; tt += Time.deltaTime) yield return null;
            }
            if (i < c.Length)
            {
                if (danceMusic != null) MusicPlayer.StopOverride(musicFadeOut);
                for (i = Mathf.Min(i, c.Length - 1); i >= 0; i--) { Show(c[i]); yield return new WaitForSeconds(stopRequested ? 0.03f : 0.05f); }   // takaisin suudelma-asentoon
            }
        }
        dancing = false;
        if (danceMusic != null) MusicPlayer.StopOverride(musicFadeOut);
        ShipNight.SetNight(false, 6f);
        foreach (var f in kissOutro) { Show(f); yield return new WaitForSeconds(f.time); }

        transform.position = me; sr.flipX = true; t = 0f;
        if (idle != null && idle.Length > 0) sr.sprite = idle[0];
        pc.TeleportTo(heroEnd);
        pc.Face(true);
        if (pc.body != null) pc.body.enabled = true;
        if (pc.shadow != null) pc.shadow.enabled = true;
        pc.enabled = true;
        pc.Scripted = false;
        kissing = false;
    }

    /// Sydämiä nousee parin ympäriltä tanssin ajan.
    IEnumerator Hearts()
    {
        if (heart == null) heart = MakeHeart();
        while (dancing)
        {
            var go = new GameObject("Sydän");
            var r = go.AddComponent<SpriteRenderer>(); r.sprite = heart;
            r.sortingOrder = sr.sortingOrder + 5;
            float sc = Random.Range(0.25f, 0.5f);
            go.transform.localScale = new Vector3(sc, sc, 1f);
            Vector3 c = sr.bounds.center;
            go.transform.position = new Vector3(c.x + Random.Range(-1.4f, 1.4f), sr.bounds.min.y + Random.Range(1.2f, 3.2f), 0f);
            StartCoroutine(Float(go, r));
            yield return new WaitForSeconds(Random.Range(0.35f, 0.8f));
        }
    }

    IEnumerator Float(GameObject go, SpriteRenderer r)
    {
        float life = Random.Range(2.2f, 3.2f), sway = Random.Range(0f, 6f);
        Vector3 p0 = go.transform.position;
        Color col = Color.Lerp(new Color(1f, 0.25f, 0.4f), new Color(1f, 0.55f, 0.75f), Random.value);
        for (float t2 = 0f; t2 < life; t2 += Time.deltaTime)
        {
            float k2 = t2 / life;
            go.transform.position = p0 + new Vector3(Mathf.Sin(sway + t2 * 2.2f) * 0.25f, k2 * 2.2f, 0f);
            col.a = Mathf.Clamp01(Mathf.Min(k2 * 5f, (1f - k2) * 2.5f));
            r.color = col;
            yield return null;
        }
        Destroy(go);
    }

    static Sprite MakeHeart()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x - N / 2f + 0.5f) / (N * 0.42f), v = (y - N / 2f + 0.5f) / (N * 0.42f) - 0.15f;
                float f = Mathf.Pow(u * u + v * v - 1f, 3f) - u * u * v * v * v;   // sydänkäyrä
                float a = Mathf.Clamp01(-f * 40f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
    }

    static Texture2D MakeVignette()
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)) * 0.6f + Mathf.Sqrt(u * u + v * v) * 0.4f;   // reunoilla ja kulmissa
                float a = Mathf.Clamp01((d - 0.62f) / 0.4f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
        tex.SetPixels32(px); tex.Apply();
        return tex;
    }

    void LateUpdate()
    {
        mood = Mathf.MoveTowards(mood, dancing ? 1f : 0f, Time.deltaTime / 2f);
        // tanssin lopetus: E tai mikä tahansa liike
        if (dancing && !stopRequested && (UsePressed() || PlayerController.ReadMoveInput().sqrMagnitude > 0.25f)) stopRequested = true;
    }

    void OnGUI()
    {
        if (mood > 0.001f)
        {
            // punainen sykkivä hehku ruudun reunoilla ja lämmin vaaleanpunainen sävy
            if (vignette == null) vignette = MakeVignette();
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 2.6f);
            GUI.depth = -50;
            GUI.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowStrength * mood * pulse);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignette);
            GUI.color = new Color(1f, 0.4f, 0.55f, 0.04f * mood);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        bool show = (near && !kissing) || (dancing && !stopRequested);
        if (!show) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
        }
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), "[E]  " + Loc.T(dancing ? stopPrompt : kissPrompt), style);
    }
}
