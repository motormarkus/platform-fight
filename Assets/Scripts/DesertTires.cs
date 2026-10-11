using UnityEngine;

/// <summary>Uudelle pelikerralle satunnaisesti sijoitetut, uudelleen poimittavat autonrenkaat.</summary>
public class DesertTires : MonoBehaviour
{
    public Area area;
    public Sprite floorSprite, rollingSprite, airSprite;
    public int count = 5;

    void Start()
    {
        if (area == null) area = GetComponent<Area>();
        if (area == null || floorSprite == null || rollingSprite == null || airSprite == null) return;
        float start = area.walkMinX + 10f, end = area.walkMaxX - 2f;
        if (end <= start) return;
        // Yksi rengas kullekin kadun osuudelle; paikka ja syvyys vaihtelevat pelikerrasta toiseen.
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Lerp(start, end, (i + Random.Range(0.15f, 0.85f)) / count);
            float y = Mathf.Lerp(area.minDepthY + 0.4f, area.maxDepthY - 0.1f, Random.value);
            var r = LifeRing.CreateTire(floorSprite, rollingSprite, airSprite, area, new Vector3(x, y, 0f));
            r.transform.SetParent(transform, true);
        }
    }
}
