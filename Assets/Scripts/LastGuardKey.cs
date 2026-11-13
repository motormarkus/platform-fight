using System.Collections.Generic;
using UnityEngine;

/// <summary>Kun kaikki ryhmän (esim. "Vartijat: Vankilan valvontahuone") vihut on kaadettu, viimeinen pudottaa avaimen: Clues.Add. Vihut haetaan ajon aikana nimellä.</summary>
public class LastGuardKey : MonoBehaviour
{
    public string groupName = "Vartijat: Vankilan valvontahuone";
    public string clueId = "Vankila: avainkaapin avain", clueTitle = "Avainkaapin avain", clueText = "Valvontahuoneen viimeinen vartija pudotti avainkaapin avaimen.";
    readonly List<Enemy> guards = new List<Enemy>();
    float nextFind;
    bool found;

    void Update()
    {
        if (Clues.Has(clueId)) return;
        if (!found)
        {
            if (Time.time < nextFind) return;
            nextFind = Time.time + 1f;
            var g = GameObject.Find(groupName);
            if (g == null) return;
            guards.AddRange(g.GetComponentsInChildren<Enemy>(true));
            found = guards.Count > 0;
            return;
        }
        foreach (var e in guards) if (e != null && !e.IsDead) return;
        Clues.Add(clueId, clueTitle, clueText);
    }
}
