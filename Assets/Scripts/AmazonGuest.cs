using UnityEngine;

/// <summary>Vuoriravintolan amatsoni-vieras: jos amatsoni on jo heron mukana (liittolaisena), tämä toinen ei tule paikalle. Tarkistus tehdään heti ja puolen sekunnin välein.</summary>
public class AmazonGuest : MonoBehaviour
{
    float next;

    void Update()
    {
        if (Time.time < next) return;
        next = Time.time + 0.5f;
        var me = GetComponent<Enemy>();
        foreach (var a in Enemy.All)
            if (a != null && a != me && a.ally && a.displayName == "Amatsoni") { gameObject.SetActive(false); return; }
    }

    void OnEnable() { next = 0f; }
}
