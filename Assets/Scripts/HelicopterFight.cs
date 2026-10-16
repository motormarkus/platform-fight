using UnityEngine;

/// <summary>Kopterin kaksi aaltoa: rahtitilan jälkeen ohjaamo, lopuksi ovi Aavikkoon.</summary>
public class HelicopterFight : MonoBehaviour
{
    public Area area;
    public GameObject cargoGroup, cockpitGroup;
    public Enemy[] cargoEnemies, cockpitEnemies;
    public Door exitDoor;
    public float cargoRightX, cockpitTriggerX, cockpitLeftX;
    public bool Completed { get; private set; }

    PlayerController player;
    bool cargoStarted, cargoCleared, cockpitStarted;
    float fullWalkMin, fullWalkMax, fullCamMin, fullCamMax;

    void Awake()
    {
        if (cargoGroup != null) cargoGroup.SetActive(false);
        if (cockpitGroup != null) cockpitGroup.SetActive(false);
        if (exitDoor != null) exitDoor.enabled = false;
        if (area == null) { enabled = false; return; }
        fullWalkMin = area.walkMinX; fullWalkMax = area.walkMaxX;
        fullCamMin = area.camMinX; fullCamMax = area.camMaxX;
    }

    void Update()
    {
        if (Area.Current != area || Completed) return;
        if (player == null) player = Players.Primary;
        if (player == null) return;
        if (!cargoStarted)
        {
            cargoStarted = true;
            // Ensimmäistä aaltoa ei voi ohittaa juoksemalla ohjaamoon.
            SetBounds(fullWalkMin, cargoRightX, fullCamMin, fullCamMax);
            Activate(cargoGroup, cargoEnemies);
        }
        if (!cargoCleared)
        {
            if (!Defeated(cargoEnemies)) return;
            cargoCleared = true;
            RestoreBounds();
        }
        if (!cockpitStarted)
        {
            if (player.transform.position.x < cockpitTriggerX) return;
            cockpitStarted = true;
            // Ohjaamosta saa kävellä takaisin; vain poistumisovi odottaa voittoa.
            RestoreBounds();
            Activate(cockpitGroup, cockpitEnemies);
        }
        if (!Defeated(cockpitEnemies)) return;
        Completed = true;
        RestoreBounds();
        if (exitDoor != null) exitDoor.enabled = true;
    }

    static void Activate(GameObject group, Enemy[] enemies)
    {
        if (group != null) group.SetActive(true);
        if (enemies == null) return;
        foreach (var enemy in enemies) if (enemy != null) enemy.WakeUp();
    }

    static bool Defeated(Enemy[] enemies)
    {
        if (enemies == null || enemies.Length == 0) return false;
        // Kuolleet tuhotaan: puuttuva viittaus tarkoittaa jo voitettua vihua.
        foreach (var enemy in enemies) if (enemy != null && !enemy.IsDead) return false;
        return true;
    }

    void RestoreBounds() => SetBounds(fullWalkMin, fullWalkMax, fullCamMin, fullCamMax);

    void SetBounds(float walkMin, float walkMax, float camMin, float camMax)
    {
        area.walkMinX = walkMin; area.walkMaxX = walkMax;
        area.camMinX = camMin; area.camMaxX = camMax;
        if (Area.Current != area || CameraFollow.Instance == null) return;
        CameraFollow.Instance.minX = camMin;
        CameraFollow.Instance.maxX = camMax;
    }
}
