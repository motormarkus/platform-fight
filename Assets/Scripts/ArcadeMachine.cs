using UnityEngine;

/// <summary>
/// Pelihallin pelikone: kuva Resources/Pelihalli/&lt;spriteName&gt;; E koneen edessä käynnistää pelin. Tähtipyssyllä ei vielä ole omaa koneen kuvaa,
/// joten sen ruutu piirretään seinälle otsikkokuvasta (spriteName tyhjä). Muut koneet ilmoittavat, ettei peliä ole vielä.
/// </summary>
public class ArcadeMachine : MonoBehaviour
{
    public enum Game { StarShooter, ComingSoon }
    public Game game = Game.ComingSoon;
    public string spriteName = "";
    public string machineName = "Pelikone";
    [Tooltip("Koneen korkeus yksikköinä (kuvan korkeus skaalataan tähän).")]
    public float height = 3.1f;
    public float useHalfWidth = 1.7f, useDepth = 1.9f;
    [Tooltip("Vain ilman koneen kuvaa: ruudun leveys ja korkeus yläpuolella.")]
    public float screenWidth = 3.4f, screenLift = 2.7f;
    bool near; float msgAt = -9f; string blockedMsg;
    GUIStyle box;

    void Start()
    {
        if (!string.IsNullOrEmpty(spriteName))
        {
            var t = Resources.Load<Texture2D>("Pelihalli/" + spriteName);
            if (t == null) return;
            var go = new GameObject("Kuva"); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0f), t.height / height, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
            return;
        }
        var title = Resources.Load<Texture2D>("TahtiPyssy/title");
        if (title == null) return;
        float h = screenWidth * title.height / title.width;
        var frame = new GameObject("Kehys"); frame.transform.SetParent(transform, false);
        frame.transform.localPosition = new Vector3(0f, screenLift, 0f);
        var fr = frame.AddComponent<SpriteRenderer>();
        fr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        fr.color = new Color(0.07f, 0.07f, 0.1f); fr.sortingOrder = -9400;
        frame.transform.localScale = new Vector3(screenWidth + 0.35f, h + 0.35f, 1f);
        var scr = new GameObject("Ruutu"); scr.transform.SetParent(transform, false);
        scr.transform.localPosition = new Vector3(0f, screenLift, 0f);
        var sr2 = scr.AddComponent<SpriteRenderer>();
        sr2.sprite = Sprite.Create(title, new Rect(0, 0, title.width, title.height), new Vector2(0.5f, 0.5f), title.width / screenWidth, 0, SpriteMeshType.FullRect);
        sr2.sortingOrder = -9399;
    }

    void Update()
    {
        near = false;
        if (StarShooter.Active || PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen) return;
        var q = Players.Nearest(transform.position);
        if (q == null || q.GameOver || !q.enabled || !q.IsFree) return;
        Vector3 p = q.transform.position, me = transform.position;
        if (Mathf.Abs(p.x - me.x) > useHalfWidth || Mathf.Abs(p.y - me.y) > useDepth) return;
        near = true;
        if (!CoopInput.Pick(q.playerIndex)) return;
        if (game == Game.StarShooter)
        {
            foreach (var b in FindObjectsByType<SalsaBrawl>(FindObjectsSortMode.None))
                if (b != null && b.Started && b.area == Area.Current) { blockedMsg = "Nyt ei ole aikaa pelata: hallissa on tappelu!"; msgAt = Time.unscaledTime; return; }
            blockedMsg = null;
            StarShooter.Begin(q);
        }
        else { blockedMsg = null; msgAt = Time.unscaledTime; }
    }

    void OnGUI()
    {
        if (StarShooter.Active) return;
        bool msg = Time.unscaledTime - msgAt < 2.2f;
        if (!near && !msg) return;
        if (box == null)
        {
            box = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            box.normal.textColor = Color.white;
        }
        float W = Screen.width, H = Screen.height, w = H * 0.8f, h = H * 0.065f;
        string txt = msg ? (blockedMsg != null ? Loc.T(blockedMsg) : Loc.T(machineName) + ": " + Loc.T("peli ei ole vielä valmis.")) : "[E]  " + Loc.T("Pelaa") + " " + Loc.T(machineName);
        GUI.Box(new Rect((W - w) * 0.5f, H * 0.82f, w, h), txt, box);
    }
}
