using UnityEngine;

/// Ikkunoiden takana näkyvä sisätila (Vegasin työmaakonttorin kanttiini): liikkuu hieman kameran suuntaan, jolloin julkisivun ja sisätilan välillä on syvyyttä.
/// Siirtymä rajataan (maxShift), jottei ikkunoihin aukea reunoja.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxInterior : MonoBehaviour
{
    public float factor = 0.03f, maxShift = 0.6f;
    float baseX = float.NaN;

    void LateUpdate()
    {
        var cam = Camera.main; if (cam == null) return;
        if (float.IsNaN(baseX)) baseX = transform.position.x;
        var p = transform.position;
        p.x = baseX + Mathf.Clamp((cam.transform.position.x - baseX) * factor, -maxShift, maxShift);
        transform.position = p;
    }
}
