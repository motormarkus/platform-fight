using UnityEngine;

/// <summary>
/// Baaritappelu: kun yksikin porukasta herää (pelaaja tulee lähelle) tai saa osuman, kaikki heräävät,
/// ja oven vieressä vartioiva portsari tulee hetken päästä mukaan tappelemaan kaikkia vastaan.
/// </summary>
public class BarBrawl : MonoBehaviour
{
    public Enemy[] members;
    [Tooltip("Alue, jossa tappelu on (aikalaskuri käy vain, kun hero on tällä alueella).")]
    public Area area;
    [Tooltip("Tappelu syttyy itsestään, kun hero on ollut alueella näin monta sekuntia ilman että kukaan on lyönyt (0 = ei käytössä).")]
    public float autoStartSeconds = 0f;
    [Tooltip("Portsarit: lähtevät liikkeelle heti, kun tappelu alkaa.")]
    public BouncerSquad squad;
    float timeInArea;
    [Header("Portsari (vartioi ovella, liittyy tappeluun)")]
    public Enemy guard;
    public Sprite[] guardFightIdle;
    public float guardFightIdleFrameTime = 0.26f;
    public float guardDelay = 1.2f;
    [Tooltip("Tappelun alettua ovesta tulevat (portsari vasemmalta).")]
    public Latecomers latecomers;

    bool started;
    public bool Started => started;
    float guardT = -1f;

    void Update()
    {
        if (!started)
        {
            if (autoStartSeconds > 0f && area != null && Area.Current == area)
            {
                timeInArea += Time.deltaTime;
                if (timeInArea >= autoStartSeconds) { Debug.Log("[BarBrawl] alkoi ajastimesta (" + timeInArea.ToString("F0") + " s alueella)"); Begin(); return; }
            }
            foreach (var e in members)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                if (e.IsAwake || e.Health < e.maxHealth) { Debug.Log("[BarBrawl] alkoi: " + e.name + " hereillä=" + e.IsAwake + " elämä=" + e.Health + "/" + e.maxHealth + " (ajastin " + timeInArea.ToString("F0") + " s)", e); Begin(); break; }
            }
            return;
        }
        if (guardT >= 0f)
        {
            guardT += Time.deltaTime;
            if (guardT >= guardDelay) { guardT = -1f; GuardJoins(); }
        }
    }

    void Begin()
    {
        started = true;
        foreach (var e in members) if (e != null && e.isActiveAndEnabled) e.WakeUp();
        if (guard != null) guardT = 0f;
        if (latecomers != null) latecomers.Begin();
        if (squad != null) squad.forceFight = true;
    }

    void GuardJoins()
    {
        if (guard == null || guard.IsDead || !guard.isActiveAndEnabled) return;
        if (guardFightIdle != null && guardFightIdle.Length > 0) { guard.idleSprites = guardFightIdle; guard.idleFrameTime = guardFightIdleFrameTime; }
        guard.fightsEveryone = true;
        guard.WakeUp();
    }
}
