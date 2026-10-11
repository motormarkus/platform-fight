using UnityEngine;

/// <summary>Ikkunoiden takainen maisema liikkuu kameraa hitaammin.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class InteriorWindowParallax : MonoBehaviour
{
    public Area area;
    public Vector2 anchor;
    public Vector2 cameraReference;
    [Range(0f, 1f)] public float factor = 0.06f;
    SpriteRenderer view;

    void Awake() { view = GetComponent<SpriteRenderer>(); }

    void LateUpdate()
    {
        bool show = area != null && Area.Current == area;
        view.enabled = show;
        if (!show) return;
        var cam = Camera.main;
        if (cam == null) return;
        Vector2 offset = ((Vector2)cam.transform.position - cameraReference) * (1f - factor);
        transform.position = new Vector3(anchor.x + offset.x, anchor.y + offset.y, transform.position.z);
    }
}
