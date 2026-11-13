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
        if (closedVisual != null) closedVisual.SetActive(!string.IsNullOrEmpty(requiredClue) && !Clues.Open(requiredClue) && !Clues.Has(clueId));
        bool locked = !string.IsNullOrEmpty(requiredClue) && !Clues.Open(requiredClue) && !Clues.Has(clueId);
        if (near && CoopInput.Pick(pc.playerIndex))
        {
            if (locked) { lockedMsg = true; msgUntil = Time.unscaledTime + 3f; }
            else if (!string.IsNullOrEmpty(clueId) && !Clues.Has(clueId)) Clues.Add(clueId, clueTitle, clueText);
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
