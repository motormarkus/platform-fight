using UnityEngine;

/// <summary>Rullalautapaikka: asettaa pelin alussa laudan maahan, jolle pelaaja voi astua. Ei tee uutta lautaa, kun se on viety pois.</summary>
public class BoardSpot : MonoBehaviour
{
    public Sprite boardSprite;
    bool spawned;

    void Update()
    {
        if (spawned || boardSprite == null) return;
        spawned = true;
        var board = LooseBoard.Spawn(boardSprite, transform.position, 0f, null);
        board.persistent = true;
    }
}
