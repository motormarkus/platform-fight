using UnityEngine;

/// <summary>Tutkittava paikka (esim. avainkaappi): kun pelaaja on kohdalla, ruutuun tulee [E]-kehote; E antaa vihjeen/avaimen (Clues.Add) kerran.
/// Vankilan valvontahuoneen avainkaappi antaa sellin E13 avaimen; kirjaston laatikko antaa vartijahuoneen avaimen.</summary>
public class ClueSpot : MonoBehaviour
{
    public string prompt = "Tutki";
    public string clueId, clueTitle, clueText;
    [Tooltip("Teksti, kun vihje on jo otettu.")]
    public string emptyText = "Täällä ei ole enää mitään.";
    [Tooltip("Pelaajan pitää olla tämän x-matkan sisällä ja tämän syvyysmatkan (y) sisällä.")]
    public float halfWidth = 1.6f, halfDepth = 1.6f;

    [Tooltip("Lukko: tämä vihje (avain) tarvitaan ennen kuin paikan voi tutkia. Tyhjä = ei lukkoa.")]
    public string requiredClue;
    public string lockedText = "Lukossa.";
    [Tooltip("Suljettu kuva (esim. kaapin ovi): näkyy kunnes lukko avataan, sen jälkeen piilotetaan.")]
    public GameObject closedVisual;
    [Tooltip("Rahaa (mk) kaikille pelaajille, kun vihje otetaan (esim. kassakaappi).")]
    public int money;
    [Tooltip("Avautumisanimaatio (esim. kassakaappi): ensimmäinen kuva suljettu, viimeinen auki; soi kerran, kun vihje otetaan.")]
    public SpriteRenderer anim;
    public Sprite[] openFrames;
    public float openFrameTime = 0.09f;
    float openT = -1f;

    PlayerController pc;
    bool near;
    float msgUntil;
    bool lockedMsg;
    GUIStyle style;

    void Update()
    {
        if (PokerService.BlocksCombat) { near = false; return; }
        pc = Players.Nearest(transform.position);
        if (Players.Count > 1)
            foreach (var q in Players.All) if (q != null && !q.GameOver && CoopInput.Pick(q.playerIndex)) { pc = q; break; }
        if (pc == null) { near = false; return; }
        Vector3 p = pc.transform.position;
        near = !pc.Riding && !pc.HoldingLid && Mathf.Abs(p.x - transform.position.x) <= halfWidth && Mathf.Abs(p.y - transform.position.y) <= halfDepth && pc.AirHeight <= 0.05f;
        if (anim != null && openFrames != null && openFrames.Length > 0)
        {
            int f = 0;
            if (!string.IsNullOrEmpty(clueId) && Clues.Has(clueId))
            {
                if (openT >= 0f) { openT += Time.deltaTime; f = Mathf.Min(openFrames.Length - 1, (int)(openT / openFrameTime)); }
                else f = openFrames.Length - 1;
            }
            anim.sprite = openFrames[f];
            anim.sortingOrder = Mathf.RoundToInt(-anim.transform.position.y * 100f);
        }
        if (closedVisual != null) closedVisual.SetActive(!string.IsNullOrEmpty(requiredClue) && !Clues.Open(requiredClue) && !Clues.Has(clueId));
        bool locked = !string.IsNullOrEmpty(requiredClue) && !Clues.Open(requiredClue) && !Clues.Has(clueId);
        if (near && CoopInput.Pick(pc.playerIndex))
        {
            if (locked) { lockedMsg = true; msgUntil = Time.unscaledTime + 3f; }
            else if (!string.IsNullOrEmpty(clueId) && !Clues.Has(clueId))
            {
                Clues.Add(clueId, clueTitle, clueText);
                openT = 0f;
                if (money > 0)
                {
                    foreach (var q in Players.All) if (q != null) q.AddMoney(money);
                    GameHUD.Popup("+" + money + " MK", pc.transform.position + Vector3.up * 3.6f, new Color(1f, 0.9f, 0.35f));
                }
            }
            else msgUntil = Time.unscaledTime + 2.5f;
        }
    }

    void OnGUI()
    {
        if (!near) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            style.normal.textColor = Color.white;
        }
        string text = Time.unscaledTime < msgUntil ? Loc.T(lockedMsg && !Clues.Open(requiredClue) ? lockedText : emptyText) : "[E]  " + Loc.T(prompt);
        float w = Screen.height * 0.45f, h = Screen.height * 0.065f;
        float textW = style.CalcSize(new GUIContent(text)).x + Screen.height * 0.06f;
        if (textW > w) w = Mathf.Min(textW, Screen.width * 0.9f, Screen.height * 0.9f);
        h = Mathf.Max(h, style.CalcHeight(new GUIContent(text), w - Screen.height * 0.02f) + Screen.height * 0.02f);
        GUI.Box(new Rect((Screen.width - w) * 0.5f, Screen.height * 0.82f, w, h), text, style);
    }
}
