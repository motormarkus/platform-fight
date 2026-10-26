using UnityEngine;

/// <summary>
/// Koneen ikkunoiden taustan liike: pilvi lipuu vasemmalle (kone lentää oikealle) omalla nopeudellaan ja palaa oikealta, kun poistuu alueen reunasta.
/// Lisäksi koko tausta (myös taivas: speed 0) keinuu hitaasti pystysuunnassa kuin turbulenssissa.
/// </summary>
public class CloudDrift : MonoBehaviour
{
    public float speed = 2f, minX, maxX, bobAmplitude = 0.08f, bobSpeed = 0.6f, bobPhase;
    float baseY = float.NaN;

    void LateUpdate()
    {
        var p = transform.position;
        if (float.IsNaN(baseY)) baseY = p.y;
        p.x -= speed * Time.deltaTime;
        if (speed > 0f && p.x < minX) p.x = maxX; else if (speed < 0f && p.x > maxX) p.x = minX;
        // kaksi hidasta aaltoa (eri taajuus): epätasainen lentokoneen keinunta
        p.y = baseY + (Mathf.Sin(Time.time * bobSpeed + bobPhase) * 0.7f + Mathf.Sin(Time.time * bobSpeed * 2.3f + bobPhase * 1.7f) * 0.3f) * bobAmplitude;
        transform.position = p;
    }
}
