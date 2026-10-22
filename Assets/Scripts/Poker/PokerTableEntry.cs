using TotalFist.Poker;
using UnityEngine;

// Attach to a table in ANY bar. Each table supplies its own background/settings asset.
// Jos asetuksessa offerOmaha, pöytään tultaessa valitaan Hold'em tai Pot Limit Omaha.
public sealed class PokerTableEntry : MonoBehaviour
{
    public PokerPresentationSettings settings;
    public Area here;
    public float rangeX = 3, rangeY = 1.5f;
    PlayerController near, chooser;
    string notice;
    bool choosing;
    int choice;           // 0 = Hold'em, 1 = Omaha
    float lastMoveX;
    void Update()
    {
        near = null;
        if (choosing) { UpdateChoice(); return; }
        if (PokerService.BlocksCombat || GameMenu.IsOpen || ShopCounter.IsOpen || Time.timeScale <= 0) return;
        if (here != null && Area.Current != here) return;
        var p = Players.Primary;
        if (p == null || !p.enabled || p.GameOver || p.AirHeight > .05f) return;
        if (Mathf.Abs(p.transform.position.x - transform.position.x) > rangeX || Mathf.Abs(p.transform.position.y - transform.position.y) > rangeY) return;
        near = p;
        if (!CoopInput.Pick(p.playerIndex)) return;
        notice = null;
        if (settings != null && settings.offerOmaha)
        {
            // Pelaaja pysäytetään valinnan ajaksi (näppäinvalinta ei saa laukaista iskuja).
            choosing = true; choice = 0; chooser = p; lastMoveX = 0; p.enabled = false;
            return;
        }
        Open(PokerVariant.Holdem);
    }
    void UpdateChoice()
    {
        if (chooser == null || PokerService.BlocksCombat || GameMenu.IsOpen || Time.timeScale <= 0) { EndChoice(); return; }
        int pi = chooser.playerIndex;
        float mx = CoopInput.Move(pi).x;
        if (Mathf.Abs(mx) > .5f && Mathf.Abs(lastMoveX) <= .5f) choice = choice == 0 ? 1 : 0;
        lastMoveX = mx;
        if (CoopInput.Jump(pi) || CoopInput.Block(pi) || CoopInput.Kick(pi)) { EndChoice(); return; }
        if (CoopInput.PickRaw(pi)) { int picked = choice; EndChoice(); Open(picked == 1 ? PokerVariant.Omaha : PokerVariant.Holdem); }
    }
    void EndChoice()
    {
        if (chooser != null) chooser.enabled = true;
        chooser = null; choosing = false;
    }
    void OnDisable() { if (choosing) EndChoice(); }
    void Open(PokerVariant variant)
    {
        var service = PokerService.Ensure();
        if (!service.Open(settings, false, variant)) notice = service.Error;
    }
    void OnGUI()
    {
        if (!choosing && (near == null || PokerService.BlocksCombat)) return;
        var style = new GUIStyle(GUI.skin.box) { font = Resources.Load<Font>("Valikko/Bangers"), fontSize = Mathf.RoundToInt(Screen.height * .025f), alignment = TextAnchor.MiddleCenter };
        string text;
        if (choosing)
        {
            int holdem = settings != null ? settings.BuyInFor(PokerVariant.Holdem) : 50, omaha = settings != null ? settings.BuyInFor(PokerVariant.Omaha) : 100;
            text = (choice == 0 ? "► " : "   ") + "Texas Hold'em  (sisäänosto " + holdem + " mk)\n" +
                   (choice == 1 ? "► " : "   ") + "Pot Limit Omaha  (sisäänosto " + omaha + " mk)\n" +
                   "Vasen/oikea valitsee · E / toimintanappi aloittaa · hyppy peruu";
        }
        else if (!string.IsNullOrEmpty(notice)) text = notice;
        else if (settings != null && settings.offerOmaha) text = "E / toimintanappi: pelaa pokeria\nHold'em tai Pot Limit Omaha";
        else text = "E / toimintanappi: pelaa Texas Hold'emia\nSisäänosto " + (settings != null ? settings.buyIn : 50) + " mk";
        GUI.Box(new Rect(Screen.width * .2f, Screen.height * (choosing ? .78f : .83f), Screen.width * .6f, Screen.height * (choosing ? .15f : .1f)), text, style);
    }
}
