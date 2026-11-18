using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pelin aloituskohta (testaukseen): pelin alkaessa pelaaja siirretään annetulle alueelle ja kohtaan.
/// Poista komponentti käytöstä (tai objekti), niin peli alkaa taas kadun alusta.
/// Jos alue on omassa sceneensä (kenttien jako), annetaan alueen nimi ja scenen nimi: scene ladataan alussa.
/// </summary>
public class GameStart : MonoBehaviour
{
    public Area area;
    public Vector2 position;
    [Tooltip("Alueen nimi, kun alue on omassa sceneensä (area-kenttä tyhjä).")]
    public string areaName;
    [Tooltip("Alueen scenen nimi: ladataan, jos alue ei ole vielä ladattuna.")]
    public string sceneName;
    [Tooltip("Testaus: antaa kaikki pomokyvyt (elämä +70 %, pyörähdyspotku, luurankokutsu, härkä).")]
    public bool grantAllPerks;

    IEnumerator Start()
    {
        var pc = Players.Primary;
        if (pc == null) yield break;
        if (grantAllPerks)
        {
            foreach (var sn in new[] { "Viidakko", "Vegas" })   // luurankokuninkaan ja taikurin mallit
            {
                bool had = SceneManager.GetSceneByName(sn).isLoaded;
                if (!had) { var l = SceneManager.LoadSceneAsync(sn, LoadSceneMode.Additive); if (l != null) while (!l.isDone) yield return null; }
                BossPerks.GrantAll();
                if (!had) { var u = SceneManager.UnloadSceneAsync(sn); if (u != null) while (!u.isDone) yield return null; }
            }
            GameHUD.Popup("KYVYT: luurankokutsu " + (BossPerks.HasSkeletonCall ? "OK" : "PUUTTUU") + ", härkä " + (BossPerks.HasBull ? "OK" : "PUUTTUU"), pc.transform.position + new Vector3(0f, 3f, 0f), new Color(1f, 0.9f, 0.4f));
        }
        if (area == null && !string.IsNullOrEmpty(areaName))
        {
            area = Area.Find(areaName);
            if (area == null && !string.IsNullOrEmpty(sceneName))
            {
                var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load != null) while (!load.isDone) yield return null;
                area = Area.Find(areaName);
            }
            if (area == null) Debug.LogWarning("GameStart: aluetta ei löytynyt: " + areaName);
        }
        if (area != null) area.Apply(pc);
        pc.TeleportTo(new Vector3(position.x, position.y, 0f));
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(position.x);
    }
}
