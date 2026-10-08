using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kiinteä este lattialla (esim. biljardipöytä): suorakulmainen jalanjälki (x ja syvyys), jonka läpi pelaaja ja viholliset
/// eivät kävele. Kuva piirretään jalanjäljen keskisyvyyden mukaan: takana olevat jäävät pöydän taakse, edessä olevat eteen.
/// </summary>
public class Obstacle : MonoBehaviour
{
    public static readonly List<Obstacle> All = new List<Obstacle>();
    public SpriteRenderer body;
    [Tooltip("Jalanjäljen puolileveys (yks) ja syvyys etureunasta (transform = etureunan keskikohta).")]
    public float halfWidth = 2.9f, depth = 0.75f;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void LateUpdate()
    {
        if (body != null) body.sortingOrder = Mathf.RoundToInt(-(transform.position.y + depth * 0.5f) * 100f);
    }

    bool Inside(Vector3 p)
    {
        Vector3 o = transform.position;
        return p.x > o.x - halfWidth && p.x < o.x + halfWidth && p.y > o.y && p.y < o.y + depth;
    }

    /// Uusi paikka p (vanha prev): jos se on esteen sisällä, liike perutaan akseleittain (liukuu reunaa pitkin).
    public static Vector3 Resolve(Vector3 prev, Vector3 p)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var o = All[i];
            if (o == null || !o.isActiveAndEnabled || !o.Inside(p)) continue;
            if (o.Inside(prev)) continue;   // oli jo sisällä (esim. syntyi siihen): ei jumiuteta
            var px = new Vector3(p.x, prev.y, p.z);
            var py = new Vector3(prev.x, p.y, p.z);
            if (!o.Inside(px)) p = px;
            else if (!o.Inside(py)) p = py;
            else p = prev;
        }
        return p;
    }
}
