using UnityEngine;

/// <summary>
/// Baaritappelu: kun yksikin porukasta herää (pelaaja tulee lähelle) tai saa osuman, kaikki heräävät,
/// ja oven vieressä vartioiva portsari tulee hetken päästä mukaan tappelemaan kaikkia vastaan.
/// </summary>
public class BarBrawl : MonoBehaviour
{
    public Enemy[] members;
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
            foreach (var e in members)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                if (e.IsAwake || e.Health < e.maxHealth) { Begin(); break; }
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
    }

    void GuardJoins()
    {
        if (guard == null || guard.IsDead || !guard.isActiveAndEnabled) return;
        if (guardFightIdle != null && guardFightIdle.Length > 0) { guard.idleSprites = guardFightIdle; guard.idleFrameTime = guardFightIdleFrameTime; }
        guard.fightsEveryone = true;
        guard.WakeUp();
    }
}
