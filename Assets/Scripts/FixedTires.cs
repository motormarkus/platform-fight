using UnityEngine;

/// <summary>Kiinteisiin paikkoihin sijoitetut poimittavat autonrenkaat (esim. pomotaistelun päädyissä), luodaan pelin alussa.</summary>
public class FixedTires : MonoBehaviour
{
    public Area area;
    public Sprite floorSprite, rollingSprite, airSprite;
    public Vector2[] spots;

    void Start()
    {
        if (area == null || floorSprite == null || rollingSprite == null || airSprite == null || spots == null) return;
        foreach (var s in spots)
        {
            var r = LifeRing.CreateTire(floorSprite, rollingSprite, airSprite, area, new Vector3(s.x, s.y, 0f));
            r.transform.SetParent(transform, true);
        }
    }
}
