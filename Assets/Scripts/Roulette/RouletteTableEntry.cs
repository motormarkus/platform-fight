using UnityEngine;

/// <summary>Ruletti-pöytä: lähellä E / toimintanappi avaa ruletin (RouletteView). Liitetään pöydän kohdalle (valikko 408 tekee kasinon ruletti-pöydälle).</summary>
public sealed class RouletteTableEntry : MonoBehaviour
{
    public Area here;
    public float rangeX = 3.2f, rangeY = 2.2f;
    PlayerController near;
    string notice; float noticeUntil;
    void Update()
    {
        near = null;
        if (RouletteView.BlocksCombat || PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen || Time.timeScale <= 0) return;
        if (here != null && Area.Current != here) return;
        var p = Players.Primary;
        if (p == null || !p.enabled || p.GameOver || p.AirHeight > .05f) return;
        if (Mathf.Abs(p.transform.position.x - transform.position.x) > rangeX || Mathf.Abs(p.transform.position.y - transform.position.y) > rangeY) return;
        near = p;
        if (!CoopInput.Pick(p.playerIndex)) return;
        if (p.money <= 0) { notice = "Ei rahaa ruletin pelaamiseen."; noticeUntil = Time.unscaledTime + 2.5f; return; }
        notice = null;
        RouletteView.Open(p);
    }
    void OnGUI()
    {
        if (near == null || RouletteView.BlocksCombat) return;
        bool showNotice = !string.IsNullOrEmpty(notice) && Time.unscaledTime < noticeUntil;
        var style = new GUIStyle(GUI.skin.box) { font = Resources.Load<Font>("Valikko/Bangers"), fontSize = Mathf.RoundToInt(Screen.height * .025f), alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width * .2f, Screen.height * .83f, Screen.width * .6f, Screen.height * .1f), showNotice ? notice : "E / toimintanappi: pelaa ruletti\nEurooppalainen ruletti, kaikki panokset", style);
    }
}
