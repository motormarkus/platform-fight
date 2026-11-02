using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Rotko (esim. Kiinan muurin katkos): jos hahmo on maassa x-välillä x0…x1, hän putoaa: menettää osan energiasta (damageFraction) ja palaa rotkon vasemmalle puolelle (respawnX).
/// Ilmassa (rullalaudan hyppy) yli pääsee, kun korkeus on vähintään clearHeight koko ylityksen ajan. Jos bridged on päällä (lankkusilta), lankuilla (laneY) voi kävellä yli; ilman laneY koko rotko on ylitettävissä. bridge-objekti näytetään silloin.</summary>
public class ChasmGap : MonoBehaviour
{
    public Area area;
    public float x0, x1, respawnX;
    [Range(0f, 1f)] public float damageFraction = 0.5f;
    public float clearHeight = 0.35f;
    public bool bridged;
    public GameObject bridge;
    [Tooltip("Sillan lankkujen syvyydet (maailman y): lankulla voi kävellä, sen ulkopuolella rotkossa putoaa. Tyhjä + bridged = koko rotko on ylitettävissä.")]
    public float[] laneY;
    public float laneHalf = 0.35f;

    readonly HashSet<PlayerController> falling = new HashSet<PlayerController>();

    void LateUpdate()
    {
        if (bridge != null && bridge.activeSelf != (bridged || (laneY != null && laneY.Length > 0))) bridge.SetActive(bridged || (laneY != null && laneY.Length > 0));
        if (area == null || Area.Current != area) return;
        bool lanes = laneY != null && laneY.Length > 0;
        bool isBridged = bridged || lanes;   // lankkukaistat = silta (ei tarvitse erillistä bridged-valintaa)
        if (isBridged && !lanes) return;
        foreach (var p in Players.All)
        {
            if (p == null || !p.enabled || falling.Contains(p)) continue;
            float x = p.transform.position.x;
            if (!(x > x0 && x < x1) || p.AirHeight >= clearHeight) continue;
            if (isBridged && OnLane(p.transform.position.y)) continue;
            StartCoroutine(Fall(p));
        }
    }

    bool OnLane(float y)
    {
        foreach (var ly in laneY) if (Mathf.Abs(y - ly) <= laneHalf) return true;
        return false;
    }

    IEnumerator Fall(PlayerController p)
    {
        falling.Add(p);
        p.health = Mathf.Max(1, p.health - Mathf.RoundToInt(p.maxHealth * damageFraction));
        p.enabled = false;
        bool main = p == Players.Primary;
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { if (main) ScreenFader.Alpha = t / 0.3f; yield return null; }
        if (main) ScreenFader.Alpha = 1f;
        p.TeleportTo(new Vector3(respawnX, p.transform.position.y, 0f));
        yield return new WaitForSecondsRealtime(0.15f);
        p.enabled = true;
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { if (main) ScreenFader.Alpha = 1f - t / 0.3f; yield return null; }
        if (main) ScreenFader.Alpha = 0f;
        falling.Remove(p);
    }
}
