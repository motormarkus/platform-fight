using UnityEngine;

/// <summary>
/// Pelin aloituskohta (testaukseen): pelin alkaessa pelaaja siirretään annetulle alueelle ja kohtaan.
/// Poista komponentti käytöstä (tai objekti), niin peli alkaa taas kadun alusta.
/// </summary>
public class GameStart : MonoBehaviour
{
    public Area area;
    public Vector2 position;

    void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        if (area != null) area.Apply(pc);
        pc.TeleportTo(new Vector3(position.x, position.y, 0f));
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapTo(position.x);
    }
}
