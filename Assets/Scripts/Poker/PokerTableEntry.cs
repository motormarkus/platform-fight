using UnityEngine;

// Attach to a table in ANY bar. Each table supplies its own background/settings asset.
public sealed class PokerTableEntry : MonoBehaviour
{
    public PokerPresentationSettings settings;
    public Area here;
    public float rangeX = 3, rangeY = 1.5f;
    PlayerController near;
    string notice;
    void Update()
    {
        near = null;
        if (PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen || Time.timeScale <= 0) return;
        if (here != null && Area.Current != here) return;
        var p = Players.Primary;
        if (p == null || !p.enabled || p.GameOver || p.AirHeight > .05f) return;
        if (Mathf.Abs(p.transform.position.x - transform.position.x) > rangeX || Mathf.Abs(p.transform.position.y - transform.position.y) > rangeY) return;
        near = p;
        if (CoopInput.Pick(p.playerIndex))
        {
            var service = PokerService.Ensure();
            if (!service.Open(settings)) notice = service.Error;
        }
    }
    void OnGUI()
    {
        if (near == null || PokerService.BlocksCombat) return;
        var style = new GUIStyle(GUI.skin.box) { font = Resources.Load<Font>("Valikko/Bangers"), fontSize = Mathf.RoundToInt(Screen.height * .025f), alignment = TextAnchor.MiddleCenter };
        GUI.Box(new Rect(Screen.width * .25f, Screen.height * .83f, Screen.width * .5f, Screen.height * .1f),
            string.IsNullOrEmpty(notice) ? "E / toimintanappi: pelaa Texas Hold'emia\nSisäänosto " + (settings != null ? settings.buyIn : 50) + " mk" : notice, style);
    }
}
