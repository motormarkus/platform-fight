using System.Collections;
using UnityEngine;

/// <summary>
/// Hotellin hissin sisätila (Thaitown). Kun hero tulee hissiin (ovelta aulasta tai kattohuoneistokäytävältä), hissi liikkuu hetken
/// (tärinä, ruutu mustaksi) ja vie toiseen päähän: aulasta ylös käytävälle, käytävältä alas aulaan. Suunta päätellään siitä,
/// mistä hissiin tultiin (Door.LastUsed).
/// </summary>
public class ElevatorRide : MonoBehaviour
{
    public Area interior, lobby, corridor;
    [Tooltip("Mihin hero ilmestyy: aulan hissin eteen ja käytävän hissin eteen.")]
    public Vector2 lobbySpawn, corridorSpawn;
    [Tooltip("Hissin matka-aika (s) ovelta saapumisen jälkeen.")]
    public float rideTime = 2.2f, fadeTime = 0.35f;
    bool riding;

    void Update()
    {
        if (riding || interior == null || Area.Current != interior || Players.Count == 0) return;
        riding = true;
        ScreenFader.Host.StartCoroutine(Ride());
    }

    IEnumerator Ride()
    {
        yield return new WaitForSecondsRealtime(0.9f);   // oven saapumispimennys ehtii hälvetä ja ovi palauttaa ohjauksen ennen kuin otamme sen
        var lead = Players.Nearest(transform.position);
        bool up = Door.LastUsed == null || Door.LastUsed.here != corridor;
        Area dest = up ? corridor : lobby;
        Vector2 spawn = up ? corridorSpawn : lobbySpawn;
        foreach (var p in Players.All) if (p != null) p.enabled = false;
        CameraFollow.Shake(0.05f, rideTime);
        yield return new WaitForSecondsRealtime(rideTime);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = t / fadeTime; yield return null; }
        ScreenFader.Alpha = 1f;
        if (dest != null && lead != null)
        {
            dest.Apply(lead);
            Vector3 pos = new Vector3(spawn.x, spawn.y, 0f);
            lead.TeleportTo(pos);
            int n = 0;
            foreach (var p in Players.All)
            {
                if (p == null || p == lead) continue;
                n++; p.TeleportTo(pos + new Vector3(n % 2 == 1 ? -1.3f : 1.3f, 0f, 0f));
            }
            if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(pos.x);
        }
        yield return new WaitForSecondsRealtime(0.15f);
        foreach (var p in Players.All) if (p != null) p.enabled = true;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) { ScreenFader.Alpha = 1f - t / fadeTime; yield return null; }
        ScreenFader.Alpha = 0f;
        riding = false;
    }
}
