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
    public Area area;
    [Tooltip("Takaraja tappelun aikana (pöytä on nurin lattialla, sen yli voi kävellä).")]
    public Vector2[] fightDepthLimits;
    [Tooltip("Ilmestyvät tappelun alkaessa (pelin tuolit).")]
    public GameObject[] activate;
    public Enemy[] fighters;
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
    }

    void Swap()
    {
        if (background != null && fightBackground != null) background.sprite = fightBackground;
        if (loop != null) loop.HideAll();
        if (area != null && fightDepthLimits != null && fightDepthLimits.Length > 0) area.depthLimits = fightDepthLimits;
        foreach (var g in activate) if (g != null) g.SetActive(true);
        foreach (var e in fighters)
        {
            if (e == null) continue;
            e.gameObject.SetActive(true);
            e.WakeUp();
        }
    }

    void OnGUI()
    {
        if (alpha <= 0f) return;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
