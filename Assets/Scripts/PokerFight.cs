using UnityEngine;

/// <summary>
/// Pokerihuoneen tappelu: kun loppuanimaatio (pöytä kaatuu) on päättynyt, ruutu häivytetään hetkeksi mustaan,
/// tausta vaihtuu tyhjään huoneeseen, pelin tuolit ja vastustajat ilmestyvät ja tappelu alkaa.
/// </summary>
public class PokerFight : MonoBehaviour
{
    public SpriteLoop loop;
    public SpriteRenderer background;
    public Sprite fightBackground;
    [Tooltip("Huonekuvan ympärille jatkettu tausta ja sen tappeluversio.")]
    public SpriteRenderer extBackground;
    public Sprite fightExtBackground;
    public Area area;
    [Tooltip("Takaraja tappelun aikana (pöytä on nurin lattialla, sen yli voi kävellä).")]
    public Vector2[] fightDepthLimits;
    [Tooltip("Ilmestyvät tappelun alkaessa (pelin tuolit).")]
    public GameObject[] activate;
    public Enemy[] fighters;
    [Header("Portsari (vartioi baarissa teräsoven edessä, tulee tappeluun)")]
    public Enemy guard;
    [Tooltip("Tappeluasento (vartioasennon tilalle, kun hän tulee tappeluun).")]
    public Sprite[] guardFightIdle;
    public float guardFightIdleFrameTime = 0.26f;
    public Vector2 guardEntry;
    [Tooltip("Viive häivytyksen jälkeen ennen kuin portsari tulee ovesta (s).")]
    public float guardDelay = 2.5f;
    float guardT = -1f;
    public float holdAfterFinale = 0.6f, fadeOut = 0.35f, blackHold = 0.2f, fadeIn = 0.4f;

    int phase;
    float t, alpha;

    void Start()
    {
        foreach (var g in activate) if (g != null) g.SetActive(false);
        foreach (var e in fighters) if (e != null) e.gameObject.SetActive(false);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        switch (phase)
        {
            case 0:
                if (loop != null && loop.FinaleDone) { phase = 1; t = 0f; }
                break;
            case 1:
                t += dt;
                if (t >= holdAfterFinale) { phase = 2; t = 0f; }
                break;
            case 2:   // musta häivytys
                t += dt;
                alpha = fadeOut > 0f ? Mathf.Clamp01(t / fadeOut) : 1f;
                if (alpha >= 1f) { Swap(); phase = 3; t = 0f; }
                break;
            case 3:
                t += dt;
                if (t >= blackHold) { phase = 4; t = 0f; }
                break;
            case 4:
                t += dt;
                alpha = fadeIn > 0f ? 1f - Mathf.Clamp01(t / fadeIn) : 0f;
                if (alpha <= 0f) phase = 5;
                break;
        }
        if (guardT >= 0f)
        {
            guardT += dt;
            if (guardT >= guardDelay) { guardT = -1f; GuardComes(); }
        }
    }

    /// Portsari kuulee metelin: siirtyy baarista teräsovesta pokerihuoneeseen ja käy kaikkien kimppuun.
    void GuardComes()
    {
        if (guard == null || guard.IsDead) return;
        if (guardFightIdle != null && guardFightIdle.Length > 0) { guard.idleSprites = guardFightIdle; guard.idleFrameTime = guardFightIdleFrameTime; }
        guard.transform.position = new Vector3(guardEntry.x, guardEntry.y, 0f);
        guard.fightsEveryone = true;
        guard.gameObject.SetActive(true);
        guard.WakeUp();
    }

    void Swap()
    {
        if (background != null && fightBackground != null) background.sprite = fightBackground;
        if (extBackground != null && fightExtBackground != null) extBackground.sprite = fightExtBackground;
        if (loop != null) loop.HideAll();
        if (area != null && fightDepthLimits != null && fightDepthLimits.Length > 0) area.depthLimits = fightDepthLimits;
        foreach (var g in activate) if (g != null) g.SetActive(true);
        foreach (var e in fighters)
        {
            if (e == null) continue;
            e.gameObject.SetActive(true);
            e.WakeUp();
        }
        if (guard != null) guardT = 0f;
    }

    void OnGUI()
    {
        if (alpha <= 0f) return;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
