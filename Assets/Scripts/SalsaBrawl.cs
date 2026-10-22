using UnityEngine;

/// <summary>
/// Salsafestin rauhalliset vihut (Tom, Puliukko, Samoalainen ym.): tappelu alkaa vain, kun hero lyö jotakuta heistä tai kun alueella on oltu
/// calmSeconds (2 min) ja Puliukko vittuilee Tomille (sanoo repliikkejään). Sen jälkeen kaikki ovat kaikkia vastaan.
/// </summary>
public class SalsaBrawl : MonoBehaviour
{
    public Area area;
    public Enemy[] crowd;
    [Tooltip("Vittuilija (Puliukko): sanoo repliikkejään, kun rauha on kestänyt calmSeconds.")]
    public Enemy instigator;
    public float calmSeconds = 120f;
    [Tooltip("Repliikki (tiedoston nimessä oleva teksti), jonka vittuilija sanoo aina juuri ennen tappelua. Tyhjä = satunnainen repliikki.")]
    public string finalLine = "speak3";
    [Tooltip("Rosvot (BouncerSquad): tulevat vasemmalta mukaan, kun tappelu alkaa.")]
    public BouncerSquad rogues;
    [Tooltip("Seilorit (heron puolella): tulevat oikealta mukaan, kun tappelu alkaa.")]
    public BouncerSquad sailors;

    [Tooltip("Rauhassa Puliukko mutisee silloin tällöin (sekuntia väliin).")]
    public Vector2 chatterEvery = new Vector2(7f, 14f);
    float calm, tauntAt = -1f, nextChatter = -1f;
    bool started, secondTaunt;

    void Update()
    {
        if (started) return;
        // hero (tai kukaan) lyö jotakuta alueella: tappelu alkaa heti
        if (Enemy.LastHit != null && Time.time - Enemy.LastHitTime < 1f)
            if (Enemy.LastHit.isActiveAndEnabled && ((area != null && Area.Current == area) || System.Array.IndexOf(crowd, Enemy.LastHit) >= 0)) { Begin(); return; }   // kenen tahansa lyöminen alueella aloittaa tappelun: kaikki herätetään mukaan
        if (area == null || Area.Current != area || PokerService.BlocksCombat) return;
        calm += Time.deltaTime;
        if (nextChatter < 0f) nextChatter = Time.time + Random.Range(chatterEvery.x, chatterEvery.y);
        if (instigator != null && Time.time >= nextChatter && tauntAt < 0f) { instigator.Speak(); nextChatter = Time.time + Random.Range(chatterEvery.x, chatterEvery.y); }   // Puliukko ei muuten puhu rauhassa (taunt vain jahdissa)
        if (calm >= calmSeconds && tauntAt < 0f)
        {
            tauntAt = Time.time;
            if (instigator != null && !(finalLine != "" && instigator.SpeakByName(finalLine))) instigator.Speak();   // viimeinen vittuilu: aina sama repliikki, jos se löytyy
            if (finalLine != "") secondTaunt = true;   // muuten toinen satunnainen repliikki kolmen sekunnin päästä
        }
        if (tauntAt >= 0f && !secondTaunt && Time.time - tauntAt > 3.5f) { secondTaunt = true; if (instigator != null) instigator.Speak(); }
        if (tauntAt >= 0f && Time.time - tauntAt > 6f) Begin();
    }

    void Begin()
    {
        started = true;
        if (sailors != null) sailors.forceFight = true;
        if (rogues != null) rogues.forceFight = true;   // neljä rosvoa tulee vasemmalta mukaan
        foreach (var e in crowd)
        {
            if (e == null || e.IsDead) continue;
            e.fightsEveryone = true;
            var tb = e.GetComponent<TomBrain>(); if (tb != null) tb.noRest = true;   // Tom tulee kauempaakin mukaan, ei palaa rauhaan
            e.WakeUp();
        }
        // varmistus: kaikki alueen rauhalliset vihut (wakeDistance < 0: rokkimimmit, pensalenkkarit ym.) mukaan, vaikka eivät olisi crowd-listalla (esim. valikon 235 uudelleenajo tyhjensi listan)
        if (area != null)
            foreach (var o in Enemy.All)
            {
                if (o == null || o.IsDead || o.ally || !o.isActiveAndEnabled || o.wakeDistance >= 0f) continue;
                if (o.transform.position.x < area.camMinX - 30f || o.transform.position.x > area.camMaxX + 30f) continue;
                o.fightsEveryone = true; o.WakeUp();
            }
    }
}
