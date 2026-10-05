using UnityEngine;

/// <summary>
/// Taustakerros, joka liikkuu ruudulla hitaammin kuin katu (esim. meri ja risteilyalus Uccopulcon satamassa).
/// speed = osuus kadun liikkeestä (0 = pysyy ruudulla paikallaan, 1 = liikkuu kadun mukana).
/// Kun kamera on kohdassa camRef, kerros on kohdassa anchor.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float speed = 0.15f;
    public Vector2 anchor;
    public Vector2 camRef;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 c = cam.transform.position;
        float k = 1f - speed;
        transform.position = new Vector3(anchor.x + (c.x - camRef.x) * k, anchor.y + (c.y - camRef.y) * k, transform.position.z);
    }
}
