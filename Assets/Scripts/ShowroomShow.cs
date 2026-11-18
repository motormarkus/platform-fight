using UnityEngine;

/// <summary>
/// Vegasin showroomin esitys (valikko 415). Kun hero astuu showroomiin ensimmäistä kertaa:
///  1) valot pimenevät (valokeila lavalla), heron ohjaus lukitaan
///  2) taikuri esittää lavalla molemmat temput (kyyhky ja silinteri, koko rauhallinen esitys alusta loppuun)
///  3) taikuri sanoo repliikin (teksti + valinnainen ääni Resources/Sfx/Taikuri/repliikki)
///  4) valot syttyvät, taikuri herää ja taistelu alkaa
///  5) kun taikuri on kaatunut, Violetti (yleisössä) kertoo häistä ja antaa päätehtävän
/// Yleisö: Violetti, Elaine, Shanice, Pablo ja Maria (ei vihuja).
/// </summary>
public sealed class ShowroomShow : MonoBehaviour
{
    public Area show;
    public Enemy magician;
    public VioletQuest violet;
    [TextArea] public string line = "Hyvät naiset ja herrat... mitään ei ole sitä miltä se näyttää!";
    public string speaker = "TAIKURI";
    [Tooltip("Näytä repliikin teksti myös, kun puheääni on olemassa (teksti pitää vastata ääntä).")] public bool useSubtitle = true;

    enum Step { Waiting, Dark, Show, Line, Fight, After, Done }
    Step step = Step.Waiting;
    float stepAt, dark;                       // dark = 0..1
    static bool seen;                         // esitys näytetty tässä pelissä
    AudioClip lineClip;
    GUIStyle box, nameStyle; Texture2D px, spot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetOnLoad() { seen = false; }
    public static void ResetShow() { seen = false; }

    void Start()
    {
        lineClip = Resources.Load<AudioClip>("Sfx/Taikuri/taikuri_welcome"); if (lineClip == null) lineClip = Resources.Load<AudioClip>("Sfx/Taikuri/repliikki");
        if (seen) { step = Step.Done; if (violet != null) violet.locked = false; }
        else if (violet != null) violet.locked = true;
    }

    void Go(Step s) { step = s; stepAt = Time.time; }

    void Lock(bool on)
    {
        foreach (var p in Players.All)
        {
            if (p == null) continue;
            p.Scripted = on; p.ScriptedMove = Vector2.zero;
        }
    }

    void OnDisable() { if (step == Step.Dark || step == Step.Show || step == Step.Line) Lock(false); }

    void Update()
    {
        if (step == Step.Done) { dark = Mathf.MoveTowards(dark, 0f, Time.deltaTime * 1.5f); return; }
        float t = Time.time - stepAt;
        switch (step)
        {
            case Step.Waiting:
                dark = Mathf.MoveTowards(dark, 0f, Time.deltaTime * 2f);
                if (show != null && Area.Current == show && magician != null && !magician.IsDead && Players.Primary != null && !Players.Primary.GameOver)
                {
                    if (stepAt == 0f) { stepAt = Time.time; break; }
                    if (t > 0.9f) { Lock(true); Go(Step.Dark); }   // pieni viive: ruutu ehtii kirkastua
                }
                else stepAt = 0f;
                break;
            case Step.Dark:
                Lock(true);
                dark = Mathf.MoveTowards(dark, 1f, Time.deltaTime * 1.2f);
                if (dark >= 1f) { magician.RestartCalmShow(); Go(Step.Show); }
                break;
            case Step.Show:
                Lock(true);
                if (t >= magician.CalmShowLength + 0.2f) { Go(Step.Line); if (lineClip != null && magician.Audio != null) magician.Audio.PlayOneShot(lineClip, 1f); }
                break;
            case Step.Line:
                Lock(true);
                if (t >= (lineClip != null ? Mathf.Max(3f, lineClip.length + 0.6f) : 3.6f)) { seen = true; Lock(false); magician.WakeUp(); Go(Step.Fight); }
                break;
            case Step.Fight:
                dark = Mathf.MoveTowards(dark, 0f, Time.deltaTime * 1.5f);
                if (magician == null || magician.IsDead) Go(Step.After);
                break;
            case Step.After:
                dark = Mathf.MoveTowards(dark, 0f, Time.deltaTime * 1.5f);
                if (t > 1.8f)
                {
                    var p = Players.Primary;
                    if (violet != null && p != null && !p.GameOver) violet.BeginTalk(p);
                    Go(Step.Done);
                }
                break;
        }
    }

    void OnGUI()
    {
        bool showText = step == Step.Line && (lineClip == null || useSubtitle);
        if (dark <= 0.01f && !showText) return;
        float W = Screen.width, H = Screen.height;
        if (px == null)
        {
            px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply();
            spot = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.8f, r));
                spot.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
            spot.Apply();
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold }; box.normal.textColor = Color.white;
            nameStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold }; nameStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        }
        if (dark > 0.01f)
        {
            GUI.depth = 10;
            float a = 0.88f * dark;
            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            var cam = Camera.main;
            if (cam != null && magician != null) { var sp = cam.WorldToScreenPoint(magician.transform.position + new Vector3(0f, 1.8f, 0f)); c = new Vector2(sp.x, H - sp.y); }
            float s = H * 1.1f;   // valokeilan koko
            Rect r = new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s);
            GUI.color = new Color(1f, 1f, 1f, dark * 0.88f / 0.88f); GUI.DrawTexture(r, spot);
            GUI.color = new Color(0f, 0f, 0f, a);
            GUI.DrawTexture(new Rect(0f, 0f, W, r.yMin), px); GUI.DrawTexture(new Rect(0f, r.yMax, W, H - r.yMax), px);
            GUI.DrawTexture(new Rect(0f, r.yMin, r.xMin, r.height), px); GUI.DrawTexture(new Rect(r.xMax, r.yMin, W - r.xMax, r.height), px);
            GUI.color = Color.white;
        }
        if (showText)
        {
            box.fontSize = Mathf.RoundToInt(H * 0.034f); nameStyle.fontSize = Mathf.RoundToInt(H * 0.03f);
            float w = Mathf.Min(W * 0.9f, H * 1.25f), x = (W - w) * 0.5f, y = H * 0.74f, h = H * 0.13f;
            GUI.depth = 0;
            GUI.color = new Color(0f, 0f, 0f, 0.82f); GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, h + H * 0.045f + 12), px); GUI.color = Color.white;
            GUI.Label(new Rect(x + H * 0.02f, y - H * 0.005f, w, H * 0.045f), Loc.T(speaker), nameStyle);
            GUI.Label(new Rect(x, y + H * 0.04f, w, h), Loc.T(line), box);
        }
    }
}
